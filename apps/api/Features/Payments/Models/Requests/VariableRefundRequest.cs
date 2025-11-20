using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Payments.Models.Requests;

/// <summary>
/// Request DTO for variable amount refund operation (partial or full refunds)
/// Auto-generated as TypeScript interface by NSwag
/// </summary>
public class VariableRefundRequest
{
    /// <summary>
    /// Refund amount to process (must be > 0 and <= original transaction amount minus any previous refunds)
    /// Supports partial refunds - does not require full transaction amount
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "RefundAmount must be greater than 0")]
    public decimal RefundAmount { get; set; }

    /// <summary>
    /// Reason for the refund (required for audit trail, minimum 10 characters)
    /// </summary>
    [Required]
    [MinLength(10, ErrorMessage = "RefundReason must be at least 10 characters")]
    public string RefundReason { get; set; } = string.Empty;
}
