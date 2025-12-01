using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Primary service interface for check-in operations
/// Follows established patterns from SafetyService and AuthenticationService
/// </summary>
public interface ICheckInService
{
    /// <summary>
    /// Get attendees for event session with optional filtering
    /// Optimized for mobile display and search
    /// Filters attendees to only show those eligible for the specific session
    /// </summary>
    Task<Result<CheckInAttendeesResponse>> GetEventAttendeesAsync(
        Guid eventId,
        Guid sessionId,
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process check-in for attendee
    /// Handles capacity validation and audit logging
    /// </summary>
    Task<Result<CheckInResponse>> CheckInAttendeeAsync(
        CheckInRequest request,
        string sessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time dashboard data for event
    /// Cached for performance with real-time updates
    /// </summary>
    Task<Result<DashboardResponse>> GetEventDashboardAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create manual entry for walk-in attendee
    /// Includes temporary registration creation
    /// </summary>
    Task<Result<CheckInResponse>> CreateManualEntryAsync(
        Guid eventId,
        ManualEntryData request,
        string sessionToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a door cash payment for an attendee
    /// Creates a TicketPurchase record with staff attribution
    /// </summary>
    Task<Result<CashPaymentResponse>> RecordCashPaymentAsync(
        Guid eventId,
        CashPaymentRequest request,
        CancellationToken cancellationToken = default);
}