namespace WitchCityRope.Api.DTOs;

/// <summary>
/// Data transfer object for venue information.
/// Used in API responses for listing and viewing venues.
/// </summary>
public class VenueDto
{
    /// <summary>
    /// Unique identifier for the venue
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Venue name (required, unique)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directions to the venue (optional)
    /// </summary>
    public string? Directions { get; set; }

    /// <summary>
    /// Additional venue information for attendees (capacity, parking, amenities, etc.)
    /// </summary>
    public string? VenueInformation { get; set; }

    /// <summary>
    /// General location information (city, state)
    /// Safe for public display to all users
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Whether the venue is active (soft delete flag)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the venue was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the venue was last updated (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request model for creating a new venue.
/// </summary>
public class CreateVenueRequest
{
    /// <summary>
    /// Venue name (required, max 100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directions to the venue (optional, max 2000 characters, supports HTML)
    /// </summary>
    public string? Directions { get; set; }

    /// <summary>
    /// Additional venue information for attendees (optional, max 1000 characters)
    /// </summary>
    public string? VenueInformation { get; set; }

    /// <summary>
    /// General location (optional, max 100 characters)
    /// Example: "Salem, MA"
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// Request model for updating an existing venue.
/// </summary>
public class UpdateVenueRequest
{
    /// <summary>
    /// Venue name (required, max 100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Directions to the venue (optional, max 2000 characters, supports HTML)
    /// </summary>
    public string? Directions { get; set; }

    /// <summary>
    /// Additional venue information for attendees (optional, max 1000 characters)
    /// </summary>
    public string? VenueInformation { get; set; }

    /// <summary>
    /// General location (optional, max 100 characters)
    /// Example: "Salem, MA"
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Whether the venue is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
