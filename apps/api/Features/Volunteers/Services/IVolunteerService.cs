using WitchCityRope.Api.Features.Volunteers.Models;

namespace WitchCityRope.Api.Features.Volunteers.Services;

/// <summary>
/// Interface for managing volunteer positions and signups
/// </summary>
public interface IVolunteerService
{
    Task<(bool success, List<VolunteerPositionDto>? positions, string? error)> GetEventVolunteerPositionsAsync(
        string eventId,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<(bool success, VolunteerSignupDto? signup, string? error)> SignupForPositionAsync(
        string positionId,
        string userId,
        VolunteerSignupRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool success, List<UserVolunteerShiftDto>? shifts, string? error)> GetUserVolunteerShiftsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(bool success, string? error)> CancelVolunteerSignupAsync(
        string signupId,
        string userId,
        CancellationToken cancellationToken = default);
}
