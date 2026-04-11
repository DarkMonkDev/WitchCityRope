using WitchCityRope.Api.Features.Vetting.Entities;

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
/// Response for membership hold/reinstatement operations.
///
/// Phase 3b-1: NewStatus type changed from int to the VettingStatus enum.
/// Ships as a JSON string via JsonStringEnumConverter.
/// </summary>
public record MembershipHoldResponse(
    VettingStatus NewStatus,
    string StatusName,
    DateTime ChangedAt
);

/// <summary>
/// Response with current hold/reinstatement status.
///
/// Phase 3b-1: VettingStatus type changed from int to the VettingStatus
/// enum. Ships as a JSON string via JsonStringEnumConverter.
/// </summary>
public record VettingHoldStatusResponse(
    VettingStatus VettingStatus,
    string StatusName,
    bool CanPlaceOnHold,
    bool CanRequestReinstatement,
    DateTime? LastStatusChangeDate
);
