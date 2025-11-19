namespace VettedMemberImport.Entities;

public class VettingAuditLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }
}
