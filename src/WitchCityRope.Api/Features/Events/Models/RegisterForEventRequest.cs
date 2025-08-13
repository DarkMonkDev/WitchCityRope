using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Api.Features.Events.Models;

public record RegisterForEventRequest(
    Guid EventId,
    Guid UserId,
    string? DietaryRestrictions,
    string? AccessibilityNeeds,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    PaymentMethod PaymentMethod,
    string? PaymentToken // For card payments
);

public record RegisterForEventResponse(
    Guid RegistrationId,
    RegistrationStatus Status,
    int? WaitlistPosition,
    decimal AmountCharged,
    string ConfirmationCode
);