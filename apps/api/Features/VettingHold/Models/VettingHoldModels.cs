namespace WitchCityRope.Api.Features.VettingHold.Models;

/// <summary>
/// Request to place membership on hold
/// </summary>
public record PlaceMembershipOnHoldRequest(string Reason);

/// <summary>
/// Request to reinstate membership (moves to Final Review)
/// </summary>
public record RequestReinstatementRequest(string Reason);

/// <summary>
/// Response for membership hold/reinstatement operations
/// </summary>
public record MembershipHoldResponse(
    int NewStatus,
    string StatusName,
    DateTime ChangedAt
);

/// <summary>
/// Response with current hold/reinstatement status
/// </summary>
public record VettingHoldStatusResponse(
    int VettingStatus,
    string StatusName,
    bool CanPlaceOnHold,
    bool CanRequestReinstatement,
    DateTime? LastStatusChangeDate
);
