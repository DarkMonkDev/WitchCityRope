namespace WitchCityRope.Api.Features.Events.Models;

// NOTE: EventType, PaymentMethod, and RegistrationStatus are defined in WitchCityRope.Core.Enums
// Use those instead of local duplicates to avoid namespace conflicts

public enum EventSortBy
{
    StartDate,
    Title,
    Price,
    AvailableSpots
}

public enum SortDirection
{
    Ascending,
    Descending
}

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Completed
}