using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using VettedMemberImport.Models;

namespace VettedMemberImport.Services;

/// <summary>
/// Service to read and parse CSV file exported from Google Sheet
/// </summary>
public class CsvReader
{
    public List<CsvRow> ReadCsvFile(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvHelper.CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null, // Don't throw on missing fields
            HeaderValidated = null // Don't validate headers
        });

        // Map CSV columns to CsvRow properties
        csv.Context.RegisterClassMap<CsvRowMap>();

        var records = csv.GetRecords<CsvRow>().ToList();
        return records;
    }
}

/// <summary>
/// Maps CSV columns to CsvRow properties
/// Handles column name variations from Google Sheet export
/// </summary>
public class CsvRowMap : ClassMap<CsvRow>
{
    public CsvRowMap()
    {
        // Core applicant information
        Map(m => m.ApplicationDate).Name("App Submitted", "ApplicationDate", "Submitted", "Date Submitted");
        Map(m => m.SceneName).Name("Vettee's nickname", "Vettees nickname", "SceneName", "Nickname", "Scene Name");
        Map(m => m.FetLifeHandle).Name("FL handles (specify if current, alternative or previous)", "FL handles", "FetLifeHandle", "FetLife").Optional();
        Map(m => m.Pronouns).Name("Vettee's pronouns", "Vettees pronouns", "Pronouns").Optional();
        Map(m => m.Email).Name("Vettee's email", "Vettees email", "Email");

        // Additional information
        Map(m => m.SponsorInfo).Name("Sponsor's nickname and email (if applicable)", "Sponsor's nickname and email", "SponsorInfo").Optional();
        Map(m => m.Vettor).Name("Assigned Vettor", "Vettor").Optional();
        Map(m => m.Notes).Name("Relevant notes", "Notes").Optional();

        // Motivation and background
        Map(m => m.MotivationDescription).Name("Description of the aplicant and motivation to join", "Description of the applicant and motivation to join", "MotivationDescription", "Description").Optional();
        Map(m => m.InstagramHandle).Name("IG handles (specify if current, alternative or previous)", "IG handles", "InstagramHandle").Optional();
        Map(m => m.OtherHandles).Name("Othert handles (specify platform and if current or previous)", "Other handles (specify platform and if current or previous)", "Other handles", "OtherHandles").Optional();

        // Critical mapping columns
        Map(m => m.RelationshipWithSponsor).Name("Relationship with Sponsor or how did they learn about Dark Alchemy", "RelationshipWithSponsor").Optional();
        Map(m => m.FitForDarkAlchemy).Name("Fit for Dark Alchemy", "FitForDarkAlchemy").Optional();
        Map(m => m.References).Name("References (additional if sponsored)", "References").Optional();
    }
}
