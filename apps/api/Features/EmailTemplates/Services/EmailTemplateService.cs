using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;
using WitchCityRope.Api.Features.EmailTemplates.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

/// <summary>
/// Service for managing email templates with copy-on-edit, merge, and HTML sanitization
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(ApplicationDbContext context, ILogger<EmailTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========================================
    // Global Templates
    // ========================================

    public async Task<Result<List<GlobalEmailTemplateDto>>> GetGlobalTemplatesByCategoryAsync(
        EmailCategory category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var templates = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .Where(t => t.Category == category && t.IsActive)
                .OrderBy(t => t.TemplateType)
                .ToListAsync(cancellationToken);

            var dtos = templates.Select(t => new GlobalEmailTemplateDto
            {
                Id = t.Id,
                Category = t.Category.ToString(),
                TemplateType = t.TemplateType,
                Subject = t.Subject,
                HtmlBody = t.HtmlBody,
                PlainTextBody = t.PlainTextBody,
                Variables = JsonSerializer.Deserialize<string[]>(t.Variables) ?? Array.Empty<string>(),
                IsActive = t.IsActive,
                Version = t.Version,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList();

            _logger.LogInformation("Retrieved {Count} global templates for category {Category}", dtos.Count, category);
            return Result<List<GlobalEmailTemplateDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global templates for category {Category}", category);
            return Result<List<GlobalEmailTemplateDto>>.Failure("Failed to retrieve templates");
        }
    }

    public async Task<Result<GlobalEmailTemplateDto>> GetGlobalTemplateByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (template == null)
            {
                return Result<GlobalEmailTemplateDto>.Failure("Template not found");
            }

            var dto = new GlobalEmailTemplateDto
            {
                Id = template.Id,
                Category = template.Category.ToString(),
                TemplateType = template.TemplateType,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Variables = JsonSerializer.Deserialize<string[]>(template.Variables) ?? Array.Empty<string>(),
                IsActive = template.IsActive,
                Version = template.Version,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Result<GlobalEmailTemplateDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global template {TemplateId}", id);
            return Result<GlobalEmailTemplateDto>.Failure("Failed to retrieve template");
        }
    }

    public async Task<Result<GlobalEmailTemplateDto>> UpdateGlobalTemplateAsync(
        Guid id,
        UpdateGlobalTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await _context.GlobalEmailTemplates
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (template == null)
            {
                return Result<GlobalEmailTemplateDto>.Failure("Template not found");
            }

            // Sanitize HTML before saving
            template.Subject = request.Subject.Trim();
            template.HtmlBody = SanitizeHtml(request.HtmlBody);
            template.PlainTextBody = request.PlainTextBody.Trim();
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = updatedByUserId;
            template.Version++; // Increment version on every update

            _context.GlobalEmailTemplates.Update(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated global template {TemplateId} to version {Version}", id, template.Version);

            var dto = new GlobalEmailTemplateDto
            {
                Id = template.Id,
                Category = template.Category.ToString(),
                TemplateType = template.TemplateType,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Variables = JsonSerializer.Deserialize<string[]>(template.Variables) ?? Array.Empty<string>(),
                IsActive = template.IsActive,
                Version = template.Version,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };

            return Result<GlobalEmailTemplateDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global template {TemplateId}", id);
            return Result<GlobalEmailTemplateDto>.Failure("Failed to update template");
        }
    }

    // ========================================
    // Event Templates (Copy-on-Edit Pattern)
    // ========================================

    public async Task<Result<List<EventEmailTemplateDto>>> GetEventTemplatesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all global Event templates
            var globalTemplates = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .Where(t => t.Category == EmailCategory.Events && t.IsActive)
                .ToListAsync(cancellationToken);

            // Get event-specific overrides
            var eventTemplates = await _context.EventEmailTemplates
                .AsNoTracking()
                .Where(t => t.EventId == eventId)
                .ToListAsync(cancellationToken);

            // Merge: For each global template, check if event override exists
            var result = new List<EventEmailTemplateDto>();
            foreach (var global in globalTemplates)
            {
                var eventOverride = eventTemplates.FirstOrDefault(e => e.TemplateType == global.TemplateType);

                if (eventOverride != null)
                {
                    // Use event-specific template
                    result.Add(new EventEmailTemplateDto
                    {
                        Id = eventOverride.Id,
                        EventId = eventOverride.EventId,
                        TemplateType = eventOverride.TemplateType,
                        Subject = eventOverride.Subject,
                        HtmlBody = eventOverride.HtmlBody,
                        PlainTextBody = eventOverride.PlainTextBody,
                        TargetSessions = eventOverride.TargetSessions,
                        IsCustomized = true,
                        CreatedAt = eventOverride.CreatedAt,
                        UpdatedAt = eventOverride.UpdatedAt
                    });
                }
                else
                {
                    // Use global template (no override)
                    result.Add(new EventEmailTemplateDto
                    {
                        Id = global.Id,
                        EventId = eventId,
                        TemplateType = global.TemplateType,
                        Subject = global.Subject,
                        HtmlBody = global.HtmlBody,
                        PlainTextBody = global.PlainTextBody,
                        TargetSessions = Array.Empty<string>(),
                        IsCustomized = false,
                        CreatedAt = global.CreatedAt,
                        UpdatedAt = global.UpdatedAt
                    });
                }
            }

            _logger.LogInformation(
                "Retrieved {TotalCount} templates for event {EventId}: {CustomizedCount} customized, {GlobalCount} global",
                result.Count,
                eventId,
                result.Count(r => r.IsCustomized),
                result.Count(r => !r.IsCustomized));

            return Result<List<EventEmailTemplateDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event templates for event {EventId}", eventId);
            return Result<List<EventEmailTemplateDto>>.Failure("Failed to retrieve event templates");
        }
    }

    public async Task<Result<EventEmailTemplateDto>> GetEventTemplateByTypeAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for event-specific override first
            var eventTemplate = await _context.EventEmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == templateType, cancellationToken);

            if (eventTemplate != null)
            {
                // Return customized template
                var dto = new EventEmailTemplateDto
                {
                    Id = eventTemplate.Id,
                    EventId = eventTemplate.EventId,
                    TemplateType = eventTemplate.TemplateType,
                    Subject = eventTemplate.Subject,
                    HtmlBody = eventTemplate.HtmlBody,
                    PlainTextBody = eventTemplate.PlainTextBody,
                    TargetSessions = eventTemplate.TargetSessions,
                    IsCustomized = true,
                    CreatedAt = eventTemplate.CreatedAt,
                    UpdatedAt = eventTemplate.UpdatedAt
                };

                return Result<EventEmailTemplateDto>.Success(dto);
            }

            // Fall back to global template
            var globalTemplate = await _context.GlobalEmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Category == EmailCategory.Events && t.TemplateType == templateType && t.IsActive, cancellationToken);

            if (globalTemplate == null)
            {
                return Result<EventEmailTemplateDto>.Failure("Template not found");
            }

            var globalDto = new EventEmailTemplateDto
            {
                Id = globalTemplate.Id,
                EventId = eventId,
                TemplateType = globalTemplate.TemplateType,
                Subject = globalTemplate.Subject,
                HtmlBody = globalTemplate.HtmlBody,
                PlainTextBody = globalTemplate.PlainTextBody,
                TargetSessions = Array.Empty<string>(),
                IsCustomized = false,
                CreatedAt = globalTemplate.CreatedAt,
                UpdatedAt = globalTemplate.UpdatedAt
            };

            return Result<EventEmailTemplateDto>.Success(globalDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event template {TemplateType} for event {EventId}", templateType, eventId);
            return Result<EventEmailTemplateDto>.Failure("Failed to retrieve template");
        }
    }

    public async Task<Result<EventEmailTemplateDto>> UpdateEventTemplateAsync(
        Guid eventId,
        string templateType,
        UpdateEventTemplateRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if event-specific template already exists
            var eventTemplate = await _context.EventEmailTemplates
                .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == templateType, cancellationToken);

            if (eventTemplate == null)
            {
                // Copy-on-Edit: Create new EventEmailTemplate from global template
                var globalTemplate = await _context.GlobalEmailTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Category == EmailCategory.Events && t.TemplateType == templateType && t.IsActive, cancellationToken);

                if (globalTemplate == null)
                {
                    return Result<EventEmailTemplateDto>.Failure($"Global template '{templateType}' not found");
                }

                eventTemplate = new EventEmailTemplate
                {
                    EventId = eventId,
                    GlobalTemplateId = globalTemplate.Id,
                    TemplateType = templateType,
                    Subject = request.Subject.Trim(),
                    HtmlBody = SanitizeHtml(request.HtmlBody),
                    PlainTextBody = request.PlainTextBody.Trim(),
                    TargetSessions = request.TargetSessions ?? Array.Empty<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = updatedByUserId
                };

                _context.EventEmailTemplates.Add(eventTemplate);
                _logger.LogInformation("Created new event template override for {TemplateType} in event {EventId}", templateType, eventId);
            }
            else
            {
                // Update existing override
                eventTemplate.Subject = request.Subject.Trim();
                eventTemplate.HtmlBody = SanitizeHtml(request.HtmlBody);
                eventTemplate.PlainTextBody = request.PlainTextBody.Trim();
                eventTemplate.TargetSessions = request.TargetSessions ?? Array.Empty<string>();
                eventTemplate.UpdatedAt = DateTime.UtcNow;
                eventTemplate.UpdatedBy = updatedByUserId;

                _context.EventEmailTemplates.Update(eventTemplate);
                _logger.LogInformation("Updated existing event template override for {TemplateType} in event {EventId}", templateType, eventId);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var dto = new EventEmailTemplateDto
            {
                Id = eventTemplate.Id,
                EventId = eventTemplate.EventId,
                TemplateType = eventTemplate.TemplateType,
                Subject = eventTemplate.Subject,
                HtmlBody = eventTemplate.HtmlBody,
                PlainTextBody = eventTemplate.PlainTextBody,
                TargetSessions = eventTemplate.TargetSessions,
                IsCustomized = true,
                CreatedAt = eventTemplate.CreatedAt,
                UpdatedAt = eventTemplate.UpdatedAt
            };

            return Result<EventEmailTemplateDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event template {TemplateType} for event {EventId}", templateType, eventId);
            return Result<EventEmailTemplateDto>.Failure("Failed to update event template");
        }
    }

    public async Task<Result> DeleteEventTemplateAsync(
        Guid eventId,
        string templateType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Reset-to-Default: Delete EventEmailTemplate record
            var template = await _context.EventEmailTemplates
                .FirstOrDefaultAsync(t => t.EventId == eventId && t.TemplateType == templateType, cancellationToken);

            if (template == null)
            {
                // Already using default (no override exists)
                return Result.Success();
            }

            _context.EventEmailTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted event template override for {TemplateType} in event {EventId} - reset to global default", templateType, eventId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event template {TemplateType} for event {EventId}", templateType, eventId);
            return Result.Failure("Failed to delete event template");
        }
    }

    // ========================================
    // Ad Hoc Emails (Placeholder - SendGrid integration required)
    // ========================================

    public async Task<Result<SentAdHocEmailDto>> SendAdHocEmailAsync(
        SendAdHocEmailRequest request,
        Guid sentByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate mutually exclusive options
            if (request.Segment.HasValue && request.RecipientEmails != null && request.RecipientEmails.Any())
            {
                return Result<SentAdHocEmailDto>.Failure("Cannot specify both Segment and RecipientEmails");
            }

            if (!request.Segment.HasValue && (request.RecipientEmails == null || !request.RecipientEmails.Any()))
            {
                return Result<SentAdHocEmailDto>.Failure("Must specify either Segment or RecipientEmails");
            }

            List<string> recipientEmails;
            int recipientCount;

            // Build recipient list based on segment or manual list
            if (request.Segment.HasValue)
            {
                var segmentUsers = await GetUsersForSegmentAsync(request.Segment.Value, cancellationToken);
                recipientEmails = segmentUsers.Select(u => u.Email ?? string.Empty).Where(e => !string.IsNullOrEmpty(e)).ToList();
                recipientCount = recipientEmails.Count;

                _logger.LogInformation("Built recipient list for segment {Segment}: {Count} users",
                    request.Segment.Value, recipientCount);
            }
            else
            {
                recipientEmails = request.RecipientEmails!;
                recipientCount = recipientEmails.Count;

                _logger.LogInformation("Using manual recipient list: {Count} emails", recipientCount);
            }

            if (recipientCount == 0)
            {
                return Result<SentAdHocEmailDto>.Failure("No valid recipients found");
            }

            // Sanitize HTML content
            var sanitizedHtml = SanitizeHtml(request.HtmlBody);

            // TODO: Implement SendGrid integration
            // For now, create audit record without sending
            var sentEmail = new SentAdHocEmail
            {
                Id = Guid.NewGuid(),
                Subject = request.Subject,
                HtmlBody = sanitizedHtml,
                PlainTextBody = request.PlainTextBody,
                RecipientGroup = request.RecipientGroup,
                RecipientEmails = recipientEmails.ToArray(),
                RecipientCount = recipientCount,
                EventId = request.EventId,
                SendGridMessageId = string.Empty, // Will be populated when SendGrid is integrated
                DeliveryStatus = "Pending", // Will be updated based on SendGrid response
                SentAt = DateTime.UtcNow,
                SentBy = sentByUserId
            };

            _context.SentAdHocEmails.Add(sentEmail);
            await _context.SaveChangesAsync(cancellationToken);

            var dto = new SentAdHocEmailDto
            {
                Id = sentEmail.Id,
                Subject = sentEmail.Subject,
                RecipientGroup = sentEmail.RecipientGroup,
                RecipientCount = sentEmail.RecipientCount,
                EventId = sentEmail.EventId,
                SendGridMessageId = sentEmail.SendGridMessageId,
                DeliveryStatus = sentEmail.DeliveryStatus,
                SentAt = sentEmail.SentAt
            };

            _logger.LogInformation("Created ad-hoc email audit record {EmailId} for {RecipientCount} recipients",
                sentEmail.Id, recipientCount);

            // TODO: When SendGrid is integrated:
            // 1. Replace variables in HtmlBody and PlainTextBody ({{user_name}}, {{reset_url}}, {{system_url}})
            // 2. Send via SendGrid API with personalization
            // 3. Update DeliveryStatus and SendGridMessageId based on response

            return Result<SentAdHocEmailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ad-hoc email");
            return Result<SentAdHocEmailDto>.Failure("Failed to send email");
        }
    }

    public async Task<Result<List<SentAdHocEmailDto>>> GetAdHocEmailHistoryAsync(
        Guid? eventId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.SentAdHocEmails.AsNoTracking();

            if (eventId.HasValue)
            {
                query = query.Where(e => e.EventId == eventId.Value);
            }

            var emails = await query
                .OrderByDescending(e => e.SentAt)
                .ToListAsync(cancellationToken);

            var dtos = emails.Select(e => new SentAdHocEmailDto
            {
                Id = e.Id,
                Subject = e.Subject,
                RecipientGroup = e.RecipientGroup,
                RecipientCount = e.RecipientCount,
                EventId = e.EventId,
                SendGridMessageId = e.SendGridMessageId,
                DeliveryStatus = e.DeliveryStatus,
                SentAt = e.SentAt
            }).ToList();

            return Result<List<SentAdHocEmailDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ad-hoc email history");
            return Result<List<SentAdHocEmailDto>>.Failure("Failed to retrieve email history");
        }
    }

    public async Task<Result<SentAdHocEmailDto>> GetAdHocEmailByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var email = await _context.SentAdHocEmails
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (email == null)
            {
                return Result<SentAdHocEmailDto>.Failure("Email not found");
            }

            var dto = new SentAdHocEmailDto
            {
                Id = email.Id,
                Subject = email.Subject,
                RecipientGroup = email.RecipientGroup,
                RecipientCount = email.RecipientCount,
                EventId = email.EventId,
                SendGridMessageId = email.SendGridMessageId,
                DeliveryStatus = email.DeliveryStatus,
                SentAt = email.SentAt
            };

            return Result<SentAdHocEmailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ad-hoc email {EmailId}", id);
            return Result<SentAdHocEmailDto>.Failure("Failed to retrieve email");
        }
    }

    // ========================================
    // User Segments (Email Targeting)
    // ========================================

    public async Task<Result<List<UserSegmentDto>>> GetUserSegmentsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var segments = new List<UserSegmentDto>();

            // Get counts for each segment
            foreach (UserSegment segment in Enum.GetValues<UserSegment>())
            {
                var count = await GetSegmentCountAsync(segment, cancellationToken);
                var description = GetSegmentDescription(segment);

                segments.Add(new UserSegmentDto
                {
                    Segment = segment,
                    SegmentName = segment.ToString(),
                    Count = count,
                    Description = description
                });
            }

            _logger.LogInformation("Retrieved user segment counts for {SegmentCount} segments", segments.Count);
            return Result<List<UserSegmentDto>>.Success(segments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user segments");
            return Result<List<UserSegmentDto>>.Failure("Failed to retrieve user segments");
        }
    }

    public async Task<Result<List<UserPreviewDto>>> GetSegmentPreviewAsync(
        UserSegment segment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await GetUsersForSegmentAsync(segment, cancellationToken);

            var preview = users
                .Take(10)
                .Select(u => new UserPreviewDto
                {
                    SceneName = u.SceneName,
                    Email = u.Email ?? string.Empty,
                    VettingStatus = u.VettingStatus,
                    VettingStatusDisplay = GetVettingStatusDisplay(u.VettingStatus),
                    Role = u.Role,
                    EmailConfirmed = u.EmailConfirmed
                })
                .ToList();

            _logger.LogInformation("Retrieved preview of {Count} users for segment {Segment}",
                preview.Count, segment);

            return Result<List<UserPreviewDto>>.Success(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving segment preview for {Segment}", segment);
            return Result<List<UserPreviewDto>>.Failure("Failed to retrieve segment preview");
        }
    }

    // ========================================
    // Private Helper Methods for Segments
    // ========================================

    private async Task<int> GetSegmentCountAsync(UserSegment segment, CancellationToken cancellationToken)
    {
        var query = BuildSegmentQuery(segment);
        return await query.CountAsync(cancellationToken);
    }

    private async Task<List<ApplicationUser>> GetUsersForSegmentAsync(
        UserSegment segment,
        CancellationToken cancellationToken)
    {
        var query = BuildSegmentQuery(segment);
        return await query.ToListAsync(cancellationToken);
    }

    private IQueryable<ApplicationUser> BuildSegmentQuery(UserSegment segment)
    {
        var query = _context.Users.AsNoTracking();

        return segment switch
        {
            UserSegment.AllVettedMembers =>
                query.Where(u => u.VettingStatus == (int)VettingStatus.Approved && u.IsActive),

            UserSegment.AllPreVettedMembers =>
                query.Where(u => u.VettingStatus != (int)VettingStatus.Denied
                    && u.VettingStatus != (int)VettingStatus.OnHold
                    && u.IsActive),

            UserSegment.AllTeachers =>
                query.Where(u => u.Role.Contains("Teacher") && u.IsActive),

            UserSegment.AllDMs =>
                query.Where(u => u.Role.Contains("DungeonMonitor") && u.IsActive),

            UserSegment.AllSafetyTeam =>
                query.Where(u => u.Role.Contains("SafetyTeam") && u.IsActive),

            UserSegment.AllAdmins =>
                query.Where(u => u.Role.Contains("Administrator") && u.IsActive),

            UserSegment.EmailNotVerified =>
                query.Where(u => !u.EmailConfirmed && u.IsActive),

            UserSegment.VettingPending =>
                query.Where(u => u.VettingStatus == (int)VettingStatus.UnderReview && u.IsActive),

            _ => throw new ArgumentException($"Unknown segment: {segment}")
        };
    }

    private static string GetSegmentDescription(UserSegment segment)
    {
        return segment switch
        {
            UserSegment.AllVettedMembers => "All vetted members (VettingStatus == Approved)",
            UserSegment.AllPreVettedMembers => "All pre-vetted members (not Denied or OnHold, active)",
            UserSegment.AllTeachers => "All users with Teacher role",
            UserSegment.AllDMs => "All users with DungeonMonitor role",
            UserSegment.AllSafetyTeam => "All users with SafetyTeam role",
            UserSegment.AllAdmins => "All users with Administrator role",
            UserSegment.EmailNotVerified => "Users with unverified email addresses",
            UserSegment.VettingPending => "Users with vetting applications under review",
            _ => "Unknown segment"
        };
    }

    private static string GetVettingStatusDisplay(int vettingStatus)
    {
        return vettingStatus switch
        {
            0 => "Under Review",
            1 => "Interview Approved",
            2 => "Final Review",
            3 => "Approved",
            4 => "Denied",
            5 => "On Hold",
            6 => "Withdrawn",
            _ => "Unknown"
        };
    }

    // ========================================
    // HTML Sanitization (XSS Prevention)
    // ========================================

    /// <summary>
    /// Sanitize HTML to prevent XSS attacks
    /// Strips dangerous tags: script, iframe, object, embed, form
    /// </summary>
    private static string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        // Strip dangerous tags using regex
        // TODO: Consider using HtmlSanitizer NuGet package for more robust sanitization
        html = Regex.Replace(html, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<object\b[^<]*(?:(?!<\/object>)<[^<]*)*<\/object>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<embed\b[^>]*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<form\b[^<]*(?:(?!<\/form>)<[^<]*)*<\/form>", "", RegexOptions.IgnoreCase);

        return html;
    }
}
