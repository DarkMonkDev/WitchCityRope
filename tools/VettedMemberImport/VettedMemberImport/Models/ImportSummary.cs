namespace VettedMemberImport.Models;

/// <summary>
/// Summary of import operation results
/// </summary>
public class ImportSummary
{
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
