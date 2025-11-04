namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Service for managing Server-Sent Events (SSE) connections and broadcasting payment notifications to kiosks.
/// </summary>
public interface IPaymentNotificationService
{
    /// <summary>
    /// Creates an async stream of payment notification events for a specific kiosk session.
    /// Implements server-side Server-Sent Events (SSE) pattern.
    /// </summary>
    /// <param name="sessionToken">Kiosk session token identifying the connection</param>
    /// <param name="cancellationToken">Cancellation token for connection lifecycle</param>
    /// <returns>Async enumerable stream of payment notification events</returns>
    IAsyncEnumerable<PaymentNotificationEvent> StreamPaymentEvents(
        string sessionToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Broadcasts a payment completion notification to a specific kiosk session.
    /// Called by PayPal webhook handler or cash payment endpoint.
    /// </summary>
    /// <param name="sessionToken">Kiosk session token to notify</param>
    /// <param name="attendeeId">Attendee who completed payment</param>
    /// <param name="eventId">Event being paid for</param>
    /// <param name="paymentId">Payment record ID</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="paymentMethod">Payment method used (Cash, PayPal)</param>
    /// <returns>True if notification was sent, false if session has no active connection</returns>
    Task<bool> NotifyPaymentComplete(
        string sessionToken,
        Guid attendeeId,
        Guid eventId,
        Guid paymentId,
        decimal amount,
        string paymentMethod);

    /// <summary>
    /// Sends a heartbeat event to keep the SSE connection alive.
    /// Prevents proxy/firewall timeouts and detects broken connections.
    /// </summary>
    /// <param name="sessionToken">Session token to send heartbeat to</param>
    /// <returns>True if heartbeat was sent, false if session has no active connection</returns>
    Task<bool> SendHeartbeat(string sessionToken);

    /// <summary>
    /// Removes an active SSE connection and cleans up resources.
    /// </summary>
    /// <param name="sessionToken">Session token to remove</param>
    void RemoveConnection(string sessionToken);

    /// <summary>
    /// Gets the count of active SSE connections for monitoring.
    /// </summary>
    int GetActiveConnectionCount();

    /// <summary>
    /// Checks if a specific session has an active SSE connection.
    /// </summary>
    bool HasActiveConnection(string sessionToken);
}
