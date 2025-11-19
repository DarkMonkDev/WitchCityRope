using System.Globalization;

namespace VettedMemberImport.Services;

/// <summary>
/// Service to parse various date formats from Google Sheet CSV
/// Handles formats like "7/11", "07/11", "7/11/22", "07/11/2022"
/// </summary>
public class DateParser
{
    private readonly int _defaultYear = 2022;

    public DateTime? ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        // Clean up the string
        dateStr = dateStr.Trim();

        // Try standard date formats
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            // If year is missing or very small, assume default year
            if (parsedDate.Year < 2000)
            {
                parsedDate = new DateTime(_defaultYear, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        // Try common formats explicitly
        string[] formats = {
            "M/d",           // 7/11
            "MM/dd",         // 07/11
            "M/d/yy",        // 7/11/22
            "MM/dd/yy",      // 07/11/22
            "M/d/yyyy",      // 7/11/2022
            "MM/dd/yyyy",    // 07/11/2022
            "yyyy-MM-dd",    // 2022-07-11
            "M-d-yy",        // 7-11-22
            "MM-dd-yy"       // 07-11-22
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                // If format doesn't include year, use default year
                if (!format.Contains("y"))
                {
                    parsedDate = new DateTime(_defaultYear, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Utc);
                }
                else if (parsedDate.Year < 2000)
                {
                    // Handle 2-digit year (22 → 2022)
                    parsedDate = new DateTime(2000 + parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Utc);
                }
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }
        }

        return null;
    }

    /// <summary>
    /// Infer decision date from notes or fall back to submitted date + 30 days
    /// </summary>
    public DateTime? InferDecisionDate(string? notes, DateTime submittedAt)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return submittedAt.AddDays(30);
        }

        // Look for date patterns in notes
        // Pattern: "accepted 07/21/22" or "Interview held & accepted 07/21/22"
        var datePattern = @"(\d{1,2}[/-]\d{1,2}(?:[/-]\d{2,4})?)";
        var matches = System.Text.RegularExpressions.Regex.Matches(notes, datePattern);

        if (matches.Count > 0)
        {
            // Use the last date mentioned (likely the decision date)
            var lastDateStr = matches[matches.Count - 1].Groups[1].Value;
            var parsedDate = ParseDate(lastDateStr);
            if (parsedDate.HasValue)
            {
                return parsedDate.Value;
            }
        }

        // Fallback: 30 days after submission
        return submittedAt.AddDays(30);
    }
}
