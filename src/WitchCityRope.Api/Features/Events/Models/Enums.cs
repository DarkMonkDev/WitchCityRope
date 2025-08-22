namespace WitchCityRope.Api.Features.Events.Models;

public enum EventType
{
    Class,
    Workshop,
    Party,
    Performance,
    Meetup,
    Other
}

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

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    PayPal,
    Venmo,
    Cash // For in-person events
}

public enum RegistrationStatus
{
    Confirmed,
    Waitlisted,
    PaymentPending,
    RequiresVetting
}

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Completed
}