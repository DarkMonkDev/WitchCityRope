using System;
using System.Collections.Generic;

namespace WitchCityRope.Api.Features.Events.Models;

public record ListEventsRequest(
    DateTime? StartDateFrom,
    DateTime? StartDateTo,
    EventType? Type,
    string[]? Tags,
    string[]? SkillLevels,
    bool? HasAvailability,
    bool? RequiresVetting,
    string? SearchTerm,
    EventSortBy SortBy = EventSortBy.StartDate,
    SortDirection SortDirection = SortDirection.Ascending,
    int Page = 1,
    int PageSize = 20
);

public record ListEventsResponse(
    List<EventSummaryDto> Events,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record EventSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    EventType Type,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Location,
    int MaxAttendees,
    int CurrentAttendees,
    int AvailableSpots,
    decimal Price,
    List<string> Tags,
    List<string> RequiredSkillLevels,
    bool RequiresVetting,
    string OrganizerName,
    string? ThumbnailUrl
);

public record GetFeaturedEventsRequest(
    int Count = 6
);

public record GetFeaturedEventsResponse(
    List<EventSummaryDto> Events
);