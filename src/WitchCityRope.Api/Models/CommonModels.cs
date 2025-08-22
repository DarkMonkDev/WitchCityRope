namespace WitchCityRope.Api.Models;

// Common models for API endpoints
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int AvailableSpots { get; set; }
    public decimal Price { get; set; }
    public string EventType { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}

public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public decimal? Price { get; set; }
    public bool? IsPublished { get; set; }
}

public class EventRegistrationRequest
{
    public Guid EventId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? SpecialRequirements { get; set; }
}

public class VettingApplicationRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string WhyJoin { get; set; } = string.Empty;
    public List<string> References { get; set; } = new();
    public bool AgreeToRules { get; set; }
}

public class ReviewApplicationRequest
{
    public Guid ApplicationId { get; set; }
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}

public class CheckInRequest
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
}

public class ProcessPaymentRequest
{
    public Guid PaymentId { get; set; }
    public string PaymentToken { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}

public class IncidentReportRequest
{
    public string IncidentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public string? Location { get; set; }
    public List<string>? InvolvedParties { get; set; }
    public bool IsAnonymous { get; set; }
}

public class UpdateProfileRequest
{
    public string? SceneName { get; set; }
    public string? Bio { get; set; }
    public string? Pronouns { get; set; }
    public bool? PublicProfile { get; set; }
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}