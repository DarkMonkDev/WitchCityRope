namespace VettedMemberImport.Models;

/// <summary>
/// Represents a row from the CSV export
/// Column names match the Google Sheet columns
/// </summary>
public class CsvRow
{
    // Core applicant information
    public string ApplicationDate { get; set; } = string.Empty; // "App Submitted"
    public string SceneName { get; set; } = string.Empty; // "Vettee's nickname"
    public string FetLifeHandle { get; set; } = string.Empty; // "FL handles"
    public string Pronouns { get; set; } = string.Empty; // "Vettee's pronouns"
    public string Email { get; set; } = string.Empty; // "Vettee's email"

    // Additional information
    public string SponsorInfo { get; set; } = string.Empty; // "Sponsor's nickname and email"
    public string Vettor { get; set; } = string.Empty; // "Assigned Vettor"
    public string VettingStatus { get; set; } = string.Empty; // "Vetting status (specify dates and sign)" - contains vetting notes/history
    public string Notes { get; set; } = string.Empty; // "Relevant notes"

    // Motivation and background
    public string MotivationDescription { get; set; } = string.Empty; // "Description of the aplicant and motivation to join"
    public string InstagramHandle { get; set; } = string.Empty; // "IG handles"
    public string OtherHandles { get; set; } = string.Empty; // "Other handles"

    // Critical mapping columns
    public string RelationshipWithSponsor { get; set; } = string.Empty; // "Relationship with Sponsor or how did they learn about Dark Alchemy"
    public string FitForDarkAlchemy { get; set; } = string.Empty; // "Fit for Dark Alchemy"
    public string References { get; set; } = string.Empty; // "References"
}
