using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Kiosk-specific payment endpoints for check-in workflow.
/// Supports Server-Sent Events (SSE) for real-time payment notifications and cash payment recording.
///
/// Security model:
/// - Uses check-in session tokens (not user authentication)
/// - Session tokens are validated via ISessionTokenService
/// - Tokens are scoped to specific events and have expiration
/// </summary>
[ApiController]
[Route("api/kiosk")]
public class KioskPaymentEndpoints : ControllerBase
{
    private readonly IPaymentNotificationService _notificationService;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KioskPaymentEndpoints> _logger;

    public KioskPaymentEndpoints(
        IPaymentNotificationService notificationService,
        ISessionTokenService sessionTokenService,
        ApplicationDbContext dbContext,
        ILogger<KioskPaymentEndpoints> logger)
    {
        _notificationService = notificationService;
        _sessionTokenService = sessionTokenService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Server-Sent Events (SSE) stream for real-time payment notifications.
    ///
    /// This endpoint establishes a persistent HTTP connection that streams payment completion events
    /// to the kiosk interface. When an attendee completes a QR code payment on their phone,
    /// the kiosk automatically receives a notification via this stream.
    ///
    /// Connection lifecycle:
    /// - Opens: Kiosk connects and receives "connected" event
    /// - Active: Heartbeat events sent every 15 seconds (keep-alive)
    /// - Notification: Payment completion triggers "payment_complete" event
    /// - Closes: Client disconnect or 30-minute timeout
    ///
    /// Security:
    /// - Requires valid session token in "X-CheckIn-Token" header
    /// - Token must not be expired or revoked
    /// - Token must exist in database
    ///
    /// Response format (text/event-stream):
    /// ```
    /// event: connected
    /// data: {"type":"connected","sessionToken":"abc...","timestamp":"2025-11-03T10:00:00Z","message":"SSE connection established"}
    ///
    /// event: payment_complete
    /// data: {"type":"payment_complete","attendeeId":"...","eventId":"...","paymentId":"...","amount":20.00,"paymentMethod":"PayPal","timestamp":"...","message":"Payment of $20.00 completed via PayPal"}
    ///
    /// event: heartbeat
    /// data: {"type":"heartbeat","sessionToken":"abc...","timestamp":"2025-11-03T10:00:15Z","message":"Connection alive"}
    /// ```
    /// </summary>
    /// <param name="sessionToken">Kiosk session token from URL path</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    [HttpGet("payment-stream/{sessionToken}")]
    [AllowAnonymous] // Authentication via session token header
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task PaymentStream(
        string sessionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate session token exists and is valid
            var tokenFromHeader = HttpContext.Request.Headers["X-CheckIn-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(tokenFromHeader))
            {
                _logger.LogWarning("SSE connection attempt without X-CheckIn-Token header");
                HttpContext.Response.StatusCode = 401;
                await HttpContext.Response.WriteAsync("Missing X-CheckIn-Token header");
                return;
            }

            // Validate token
            var validationResult = await _sessionTokenService.ValidateTokenAsync(tokenFromHeader, cancellationToken);
            if (!validationResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Invalid session token for SSE connection: {Reason}",
                    validationResult.Error);

                HttpContext.Response.StatusCode = 401;
                await HttpContext.Response.WriteAsync(validationResult.Error);
                return;
            }

            // Validate token matches URL parameter (security check)
            if (tokenFromHeader != sessionToken)
            {
                _logger.LogWarning(
                    "Session token mismatch: Header token does not match URL parameter");

                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsync("Session token mismatch");
                return;
            }

            _logger.LogInformation(
                "Starting SSE stream for session token {SessionToken}",
                sessionToken);

            // Configure response for Server-Sent Events
            HttpContext.Response.StatusCode = 200;
            HttpContext.Response.ContentType = "text/event-stream";
            HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
            HttpContext.Response.Headers.Append("Connection", "keep-alive");
            HttpContext.Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering

            // Start streaming events
            await foreach (var paymentEvent in _notificationService.StreamPaymentEvents(sessionToken, cancellationToken))
            {
                // Serialize event data to JSON
                var eventData = JsonSerializer.Serialize(paymentEvent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                // Write SSE formatted message
                // Format: event: <type>\ndata: <json>\n\n
                await HttpContext.Response.WriteAsync($"event: {paymentEvent.Type}\n");
                await HttpContext.Response.WriteAsync($"data: {eventData}\n\n");
                await HttpContext.Response.Body.FlushAsync(cancellationToken);

                _logger.LogDebug(
                    "Sent SSE event {EventType} to session {SessionToken}",
                    paymentEvent.Type, sessionToken);
            }

            _logger.LogInformation(
                "SSE stream ended for session token {SessionToken}",
                sessionToken);
        }
        catch (OperationCanceledException)
        {
            // Normal disconnection (client closed browser, navigated away, etc.)
            _logger.LogInformation(
                "SSE connection cancelled for session {SessionToken}",
                sessionToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in SSE stream for session {SessionToken}",
                sessionToken);
        }
    }

    /// <summary>
    /// Record cash payment at the door.
    ///
    /// This endpoint allows check-in staff to record cash payments for attendees who
    /// pay at the event instead of purchasing tickets online.
    ///
    /// Business rules:
    /// - Social events: Payment OPTIONAL (attendees can check in with just RSVP)
    /// - Workshops/Classes: This endpoint should NOT be used (all workshop attendees must pre-purchase tickets)
    /// - Payment links to attendee via attendeeId parameter
    /// - Staff member is tracked via session token creator
    /// - Optional notes field for special circumstances (discounts, payment plan, etc.)
    ///
    /// After successful payment:
    /// - Payment record created in database
    /// - Linked to attendee and event
    /// - Staff member ID recorded (audit trail)
    /// - NO SSE notification sent (cash payments are immediate confirmation)
    ///
    /// Security:
    /// - Requires valid session token in "X-CheckIn-Token" header
    /// - Token must be for the correct event (matches eventId parameter)
    /// </summary>
    /// <param name="eventId">Event ID being paid for</param>
    /// <param name="request">Cash payment details</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    [HttpPost("events/{eventId:guid}/payments/cash")]
    [AllowAnonymous] // Authentication via session token header
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CashPaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashPaymentResponse>> RecordCashPayment(
        Guid eventId,
        [FromBody] CashPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate session token
            var sessionToken = HttpContext.Request.Headers["X-CheckIn-Token"].FirstOrDefault();
            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogWarning("Cash payment attempt without X-CheckIn-Token header");
                return Unauthorized("Missing X-CheckIn-Token header");
            }

            var tokenValidation = await _sessionTokenService.ValidateTokenAsync(sessionToken, cancellationToken);
            if (!tokenValidation.IsSuccess)
            {
                _logger.LogWarning(
                    "Invalid session token for cash payment: {Reason}",
                    tokenValidation.Error);

                return Unauthorized(tokenValidation.Error);
            }

            var tokenData = tokenValidation.Value; // Result<TokenValidationResult>.Value contains event ID and staff ID

            // Validate token is for this event
            if (tokenData.EventId != eventId)
            {
                _logger.LogWarning(
                    "Session token event mismatch: Token for event {TokenEventId}, payment for event {RequestEventId}",
                    tokenData.EventId, eventId);

                return BadRequest("Session token is not valid for this event");
            }

            // Validate request
            if (request.Amount < 0.01m)
            {
                return BadRequest("Amount must be at least $0.01");
            }

            if (request.Amount > 1000m)
            {
                return BadRequest("Amount cannot exceed $1,000.00");
            }

            // Retrieve session token entity for audit logging
            var sessionTokenEntity = await _dbContext.CheckInSessionTokens
                .FirstOrDefaultAsync(t => t.Token == sessionToken, cancellationToken);

            if (sessionTokenEntity == null)
            {
                _logger.LogWarning("Session token not found in database: {Token}", sessionToken);
                return Unauthorized("Invalid session token");
            }

            // Verify attendee exists and is registered for this event
            var attendee = await _dbContext.Set<EventAttendee>()
                .FirstOrDefaultAsync(
                    a => a.UserId == request.AttendeeId && a.EventId == eventId,
                    cancellationToken);

            if (attendee == null)
            {
                _logger.LogWarning(
                    "Attendee {AttendeeId} not found or not registered for event {EventId}",
                    request.AttendeeId, eventId);

                return NotFound("Attendee not found or not registered for this event");
            }

            // Find or create "Door Sale" ticket type for this event
            var doorSaleTicketType = await _dbContext.TicketTypes
                .FirstOrDefaultAsync(tt =>
                    tt.EventId == eventId &&
                    tt.Name == "Door Sale",
                    cancellationToken);

            if (doorSaleTicketType == null)
            {
                doorSaleTicketType = new TicketType
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    Name = "Door Sale",
                    Description = "Door admission - cash payment",
                    PricingType = PricingType.Fixed,
                    Price = 0,
                    Available = 999,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.TicketTypes.Add(doorSaleTicketType);
            }

            // Create ticket purchase record
            var ticketPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = doorSaleTicketType.Id,
                UserId = request.AttendeeId,
                Quantity = 1,
                TotalPrice = request.Amount,
                PaymentStatus = "Completed",
                PaymentMethod = "Cash",
                PaymentReference = $"DOOR-{DateTime.UtcNow:yyyyMMddHHmmss}",
                RecordedByStaffId = sessionTokenEntity.CreatedByUserId,
                ProcessedAt = DateTime.UtcNow,
                Notes = request.Notes ?? "Cash payment recorded at door",
                PurchaseDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.TicketPurchases.Add(ticketPurchase);

            // Create EventAttendance record for attendance tracking
            var attendance = new EventAttendance(eventId, request.AttendeeId, AttendanceType.Ticket)
            {
                TicketPurchaseId = ticketPurchase.Id,
                Notes = request.Notes,
                CreatedBy = sessionTokenEntity.CreatedByUserId
            };
            _dbContext.EventAttendances.Add(attendance);

            // Save all records in single transaction
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cash payment {TicketPurchaseId} recorded: Attendee {AttendeeId}, Amount ${Amount}, Event {EventId}",
                ticketPurchase.Id, request.AttendeeId, request.Amount, eventId);

            // If session token provided, send SSE notification
            if (!string.IsNullOrWhiteSpace(request.SessionToken))
            {
                var notificationSent = await _notificationService.NotifyPaymentComplete(
                    request.SessionToken,
                    request.AttendeeId,
                    eventId,
                    ticketPurchase.Id,
                    request.Amount,
                    "Cash");

                if (notificationSent)
                {
                    _logger.LogInformation(
                        "SSE notification sent for cash payment {TicketPurchaseId}",
                        ticketPurchase.Id);
                }
            }

            // Build response
            var response = new CashPaymentResponse
            {
                Success = true,
                TicketPurchaseId = ticketPurchase.Id,
                Timestamp = ticketPurchase.CreatedAt,
                Message = "Cash payment recorded",
                AttendeeId = request.AttendeeId,
                EventId = eventId,
                Amount = request.Amount,
                Currency = "USD"
            };

            return Ok(response);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Database error recording cash payment for attendee {AttendeeId} at event {EventId}",
                request.AttendeeId, eventId);

            return Problem(
                title: "Database Error",
                detail: "Failed to save payment record. Please try again.",
                statusCode: 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error recording cash payment for attendee {AttendeeId} at event {EventId}",
                request.AttendeeId, eventId);

            return Problem(
                title: "Server Error",
                detail: "An unexpected error occurred while recording payment.",
                statusCode: 500);
        }
    }

    /// <summary>
    /// Health check endpoint for kiosk payment endpoints.
    /// Useful for monitoring SSE infrastructure and debugging connection issues.
    /// </summary>
    [HttpGet("payments/health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        var activeConnections = _notificationService.GetActiveConnectionCount();

        return Ok(new
        {
            status = "healthy",
            service = "kiosk-payments",
            activeConnections = activeConnections,
            timestamp = DateTime.UtcNow.ToString("O")
        });
    }
}

/// <summary>
/// Request model for recording cash payments at the door.
/// </summary>
public class CashPaymentRequest
{
    /// <summary>
    /// Attendee ID who is paying (links to User.Id)
    /// </summary>
    public Guid AttendeeId { get; set; }

    /// <summary>
    /// Payment amount in dollars (minimum $0.01, maximum $1,000.00)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional notes about the payment (discount reason, payment plan, etc.)
    /// Maximum 500 characters
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optional session token for SSE notification
    /// If provided, kiosk will receive real-time payment confirmation
    /// </summary>
    public string? SessionToken { get; set; }
}

/// <summary>
/// Response model for cash payment recording.
/// </summary>
public class CashPaymentResponse
{
    /// <summary>
    /// Whether payment was recorded successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Ticket purchase record ID
    /// </summary>
    public Guid TicketPurchaseId { get; set; }

    /// <summary>
    /// When payment was recorded (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Human-readable message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Attendee ID who paid
    /// </summary>
    public Guid AttendeeId { get; set; }

    /// <summary>
    /// Event ID being paid for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (always USD)
    /// </summary>
    public string Currency { get; set; } = "USD";
}
