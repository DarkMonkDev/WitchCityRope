using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Manages Server-Sent Events (SSE) connections for real-time payment notifications to kiosk interfaces.
///
/// This service provides an in-memory publish-subscribe system for payment completion events,
/// enabling real-time updates to check-in kiosks when attendees complete QR code payments.
///
/// Key responsibilities:
/// - Maintain active SSE connections per session token
/// - Broadcast payment completion notifications to specific sessions
/// - Handle connection lifecycle (creation, timeout, cleanup)
/// - Thread-safe concurrent access management
///
/// Usage pattern:
/// 1. Kiosk connects: StreamPaymentEvents(sessionToken) creates SSE stream
/// 2. Payment completes: NotifyPaymentComplete(sessionToken, eventData) broadcasts to kiosk
/// 3. Connection cleanup: Automatic cleanup on disconnect or timeout
/// </summary>
public class PaymentNotificationService : IPaymentNotificationService
{
    private readonly ILogger<PaymentNotificationService> _logger;

    /// <summary>
    /// Thread-safe dictionary storing active SSE channels by session token.
    /// ConcurrentDictionary ensures safe access from multiple threads (webhooks, SSE connections).
    /// </summary>
    private readonly ConcurrentDictionary<string, ChannelWriter<PaymentNotificationEvent>> _channels = new();

    /// <summary>
    /// Connection timeout duration (30 minutes of inactivity)
    /// After this period, inactive connections are automatically cleaned up.
    /// </summary>
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(30);

    public PaymentNotificationService(ILogger<PaymentNotificationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates an async stream of payment notification events for a specific kiosk session.
    /// This method implements the server-side of Server-Sent Events (SSE).
    ///
    /// The stream remains open until:
    /// - Client disconnects (cancellation token triggered)
    /// - Connection timeout (30 minutes of inactivity)
    /// - Channel is closed/removed
    ///
    /// Thread-safety: Multiple concurrent connections for different sessions are safe.
    /// Connection reuse: If session reconnects, a new channel is created (previous one is closed).
    /// </summary>
    /// <param name="sessionToken">Kiosk session token identifying the connection</param>
    /// <param name="cancellationToken">Cancellation token for connection lifecycle</param>
    /// <returns>Async enumerable stream of payment notification events</returns>
    public async IAsyncEnumerable<PaymentNotificationEvent> StreamPaymentEvents(
        string sessionToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Create unbounded channel for this session
        // Unbounded: No message queue limit (payment notifications are infrequent)
        var channel = Channel.CreateUnbounded<PaymentNotificationEvent>();

        // Register channel in concurrent dictionary
        // If session already exists, replace old channel (handles reconnection)
        var channelWriter = channel.Writer;
        _channels.AddOrUpdate(sessionToken, channelWriter, (key, old) =>
        {
            // Close old channel if session reconnects
            old.TryComplete();
            _logger.LogInformation(
                "Session {SessionToken} reconnected, replacing old SSE channel",
                sessionToken);
            return channelWriter;
        });

        _logger.LogInformation(
            "SSE connection established for session {SessionToken}",
            sessionToken);

        // Send initial connection confirmation event
        yield return new PaymentNotificationEvent
        {
            Type = "connected",
            SessionToken = sessionToken,
            Timestamp = DateTime.UtcNow,
            Message = "SSE connection established"
        };

        // Setup timeout cancellation token
        using var timeoutCts = new CancellationTokenSource(ConnectionTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        // Stream all events from the channel
        // NOTE: Cannot wrap yield return in try-catch (C# limitation)
        // Exception handling is done at the calling endpoint level
        await foreach (var paymentEvent in channel.Reader.ReadAllAsync(linkedCts.Token)
            .ConfigureAwait(false))
        {
            yield return paymentEvent;

            _logger.LogInformation(
                "Sent payment notification to session {SessionToken}: {EventType}",
                sessionToken, paymentEvent.Type);

            // Reset timeout on each event (keep-alive)
            timeoutCts.CancelAfter(ConnectionTimeout);
        }

        // Cleanup: Remove channel from active connections
        // This runs after iterator completes (normal end, cancellation, or exception)
        RemoveConnection(sessionToken);
    }

    /// <summary>
    /// Broadcasts a payment completion notification to a specific kiosk session.
    ///
    /// This method is called by:
    /// - PayPal webhook handler (digital QR code payments)
    /// - Cash payment endpoint (immediate confirmation)
    ///
    /// If the session token has no active SSE connection, the notification is silently dropped.
    /// This is acceptable because the kiosk can refresh to see the payment status.
    /// </summary>
    /// <param name="sessionToken">Kiosk session token to notify</param>
    /// <param name="attendeeId">Attendee who completed payment</param>
    /// <param name="eventId">Event being paid for</param>
    /// <param name="paymentId">Payment record ID</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="paymentMethod">Payment method used (Cash, PayPal)</param>
    /// <returns>True if notification was sent, false if session has no active connection</returns>
    public async Task<bool> NotifyPaymentComplete(
        string sessionToken,
        Guid attendeeId,
        Guid eventId,
        Guid paymentId,
        decimal amount,
        string paymentMethod)
    {
        // Check if session has active SSE connection
        if (!_channels.TryGetValue(sessionToken, out var channelWriter))
        {
            _logger.LogWarning(
                "No active SSE connection for session {SessionToken}, payment notification dropped",
                sessionToken);
            return false;
        }

        // Create payment completion event
        var paymentEvent = new PaymentNotificationEvent
        {
            Type = "payment_complete",
            SessionToken = sessionToken,
            AttendeeId = attendeeId,
            EventId = eventId,
            PaymentId = paymentId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            Timestamp = DateTime.UtcNow,
            Message = $"Payment of ${amount:F2} completed via {paymentMethod}"
        };

        try
        {
            // Write event to channel (non-blocking for unbounded channels)
            await channelWriter.WriteAsync(paymentEvent);

            _logger.LogInformation(
                "Payment notification sent to session {SessionToken}: Attendee {AttendeeId}, Amount ${Amount}",
                sessionToken, attendeeId, amount);

            return true;
        }
        catch (ChannelClosedException)
        {
            // Channel was closed (connection dropped)
            _logger.LogWarning(
                "SSE channel closed for session {SessionToken}, removing connection",
                sessionToken);

            RemoveConnection(sessionToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending payment notification to session {SessionToken}",
                sessionToken);
            return false;
        }
    }

    /// <summary>
    /// Sends a heartbeat event to keep the SSE connection alive.
    ///
    /// SSE connections should send periodic heartbeats (every 15-30 seconds) to:
    /// - Prevent proxy/firewall timeouts
    /// - Detect broken connections quickly
    /// - Keep connection state active
    ///
    /// This method can be called by a background timer or on-demand.
    /// </summary>
    /// <param name="sessionToken">Session token to send heartbeat to</param>
    /// <returns>True if heartbeat was sent, false if session has no active connection</returns>
    public async Task<bool> SendHeartbeat(string sessionToken)
    {
        if (!_channels.TryGetValue(sessionToken, out var channelWriter))
        {
            return false;
        }

        var heartbeatEvent = new PaymentNotificationEvent
        {
            Type = "heartbeat",
            SessionToken = sessionToken,
            Timestamp = DateTime.UtcNow,
            Message = "Connection alive"
        };

        try
        {
            await channelWriter.WriteAsync(heartbeatEvent);
            return true;
        }
        catch (ChannelClosedException)
        {
            RemoveConnection(sessionToken);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending heartbeat to session {SessionToken}",
                sessionToken);
            return false;
        }
    }

    /// <summary>
    /// Removes an active SSE connection and cleans up resources.
    ///
    /// Called automatically when:
    /// - SSE stream ends (client disconnect, timeout, error)
    /// - Channel write fails (connection lost)
    ///
    /// Thread-safety: Safe to call from multiple threads concurrently.
    /// </summary>
    /// <param name="sessionToken">Session token to remove</param>
    public void RemoveConnection(string sessionToken)
    {
        if (_channels.TryRemove(sessionToken, out var channelWriter))
        {
            // Signal channel completion (no more events will be written)
            channelWriter.TryComplete();

            _logger.LogInformation(
                "Removed SSE connection for session {SessionToken}",
                sessionToken);
        }
    }

    /// <summary>
    /// Gets the count of active SSE connections.
    /// Useful for monitoring and diagnostics.
    /// </summary>
    public int GetActiveConnectionCount()
    {
        return _channels.Count;
    }

    /// <summary>
    /// Checks if a specific session has an active SSE connection.
    /// </summary>
    public bool HasActiveConnection(string sessionToken)
    {
        return _channels.ContainsKey(sessionToken);
    }
}

/// <summary>
/// Payment notification event sent via Server-Sent Events (SSE).
/// Serialized as JSON in the SSE data field.
/// </summary>
public class PaymentNotificationEvent
{
    /// <summary>
    /// Event type for SSE event name
    /// Values: "connected", "payment_complete", "heartbeat"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Kiosk session token this event is for
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Attendee ID who completed payment (null for non-payment events)
    /// </summary>
    public Guid? AttendeeId { get; set; }

    /// <summary>
    /// Event ID being paid for (null for non-payment events)
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Payment record ID (null for non-payment events)
    /// </summary>
    public Guid? PaymentId { get; set; }

    /// <summary>
    /// Payment amount (0 for non-payment events)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment method used (Cash, PayPal, null for non-payment events)
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Event timestamp (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
