using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

/// <summary>
/// Service for managing email templates (global defaults and event-specific customizations)
/// </summary>
public interface IEmailTemplateService
{
    // ========================================
    // Global Templates (Admin-only)
    // ========================================

    /// <summary>
    /// Get all global email templates for a specific category
    /// </summary>
    Task<Result<List<GlobalEmailTemplateDto>>> GetGlobalTemplatesByCategoryAsync(
        EmailCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single global email template by ID
    /// </summary>
    Task<Result<GlobalEmailTemplateDto>> GetGlobalTemplateByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a global email template (increments version)
    /// </summary>
    Task<Result<GlobalEmailTemplateDto>> UpdateGlobalTemplateAsync(
        Guid id,
        UpdateGlobalTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);

    // ========================================
    // Event Templates (Copy-on-Edit Pattern)
    // ========================================

    /// <summary>
    /// Get all event email templates (merged global + event-specific overrides)
    /// Returns global templates with IsCustomized=false and event overrides with IsCustomized=true
    /// </summary>
    Task<Result<List<EventEmailTemplateDto>>> GetEventTemplatesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific event email template by type
    /// Returns global template if no event-specific override exists
    /// </summary>
    Task<Result<EventEmailTemplateDto>> GetEventTemplateByTypeAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update an event-specific email template override (copy-on-edit)
    /// Creates new EventEmailTemplate record on first save, updates existing on subsequent saves
    /// </summary>
    Task<Result<EventEmailTemplateDto>> UpdateEventTemplateAsync(
        Guid eventId,
        string templateType,
        UpdateEventTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an event-specific email template override (reset-to-default)
    /// Removes EventEmailTemplate record, future GETs will return global template
    /// </summary>
    Task<Result> DeleteEventTemplateAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default);

    // ========================================
    // Ad Hoc Emails (Bulk Send + History)
    // ========================================

    /// <summary>
    /// Send an ad-hoc bulk email via SendGrid
    /// Creates audit trail in SentAdHocEmails table
    /// </summary>
    Task<Result<SentAdHocEmailDto>> SendAdHocEmailAsync(
        SendAdHocEmailRequest request,
        Guid sentByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ad-hoc email send history, optionally filtered by event
    /// </summary>
    Task<Result<List<SentAdHocEmailDto>>> GetAdHocEmailHistoryAsync(
        Guid? eventId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific sent ad-hoc email by ID
    /// </summary>
    Task<Result<SentAdHocEmailDto>> GetAdHocEmailByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result type for service operations
/// </summary>
public class Result
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; } = string.Empty;

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Generic result type for service operations returning data
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string Error { get; set; } = string.Empty;

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
