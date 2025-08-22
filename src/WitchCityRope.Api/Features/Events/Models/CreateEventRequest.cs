using System;

namespace WitchCityRope.Api.Features.Events.Models;

public record CreateEventRequest(
    string Title,
    string Description,
    EventType Type,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Location,
    int MaxAttendees,
    decimal Price,
    string[] RequiredSkillLevels,
    string[] Tags,
    bool RequiresVetting,
    string SafetyNotes,
    string EquipmentProvided,
    string EquipmentRequired,
    Guid OrganizerId
);

public record CreateEventResponse(
    Guid EventId,
    string Title,
    string Slug,
    DateTime CreatedAt
);