using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Interfaces;

namespace WitchCityRope.Api.Controllers
{
    [Authorize(Roles = "Administrator,Organizer")]
    [ApiController]
    [Route("api/events/{eventId}/emails")]
    public class EventEmailController : ControllerBase
    {
        private readonly IEventEmailService _emailService;
        private readonly ILogger<EventEmailController> _logger;

        public EventEmailController(
            IEventEmailService emailService,
            ILogger<EventEmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // Template Management

        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<EventEmailTemplateDto>), 200)]
        public async Task<ActionResult<List<EventEmailTemplateDto>>> GetEventEmailTemplates(Guid eventId)
        {
            try
            {
                var templates = await _emailService.GetEventEmailTemplatesAsync(eventId);
                
                var templateDtos = templates.Select(t => new EventEmailTemplateDto
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    Type = t.Type,
                    Subject = t.Subject,
                    Body = t.Body,
                    IsActive = t.IsActive,
                    UpdatedAt = t.UpdatedAt
                }).ToList();
                
                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email templates for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while retrieving email templates");
            }
        }

        [HttpGet("templates/{templateId}")]
        [ProducesResponseType(typeof(EventEmailTemplateDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EventEmailTemplateDto>> GetEmailTemplate(Guid eventId, Guid templateId)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                var templateDto = new EventEmailTemplateDto
                {
                    Id = template.Id,
                    EventId = template.EventId,
                    Type = template.Type,
                    Subject = template.Subject,
                    Body = template.Body,
                    IsActive = template.IsActive,
                    UpdatedAt = template.UpdatedAt
                };
                
                return Ok(templateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email template {TemplateId}", templateId);
                return StatusCode(500, "An error occurred while retrieving the email template");
            }
        }

        [HttpPost("templates")]
        [ProducesResponseType(typeof(EventEmailTemplateDto), 201)]
        public async Task<ActionResult<EventEmailTemplateDto>> CreateEmailTemplate(Guid eventId, [FromBody] SaveEmailTemplateRequest request)
        {
            try
            {
                var template = new EventEmailTemplate(
                    eventId: eventId,
                    type: request.Type,
                    subject: request.Subject,
                    body: request.Body);
                
                var createdTemplate = await _emailService.CreateEmailTemplateAsync(template);
                
                var templateDto = new EventEmailTemplateDto
                {
                    Id = createdTemplate.Id,
                    EventId = createdTemplate.EventId,
                    Type = createdTemplate.Type,
                    Subject = createdTemplate.Subject,
                    Body = createdTemplate.Body,
                    IsActive = createdTemplate.IsActive,
                    UpdatedAt = createdTemplate.UpdatedAt
                };
                
                return CreatedAtAction(nameof(GetEmailTemplate), new { eventId = eventId, templateId = templateDto.Id }, templateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email template for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while creating the email template");
            }
        }

        [HttpPut("templates/{templateId}")]
        [ProducesResponseType(typeof(EventEmailTemplateDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EventEmailTemplateDto>> UpdateEmailTemplate(Guid eventId, Guid templateId, [FromBody] SaveEmailTemplateRequest request)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                template.UpdateContent(request.Subject, request.Body);
                if (request.IsActive && !template.IsActive)
                    template.Activate();
                else if (!request.IsActive && template.IsActive)
                    template.Deactivate();
                
                var updatedTemplate = await _emailService.UpdateEmailTemplateAsync(template);
                
                var templateDto = new EventEmailTemplateDto
                {
                    Id = updatedTemplate.Id,
                    EventId = updatedTemplate.EventId,
                    Type = updatedTemplate.Type,
                    Subject = updatedTemplate.Subject,
                    Body = updatedTemplate.Body,
                    IsActive = updatedTemplate.IsActive,
                    UpdatedAt = updatedTemplate.UpdatedAt
                };
                
                return Ok(templateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email template {TemplateId}", templateId);
                return StatusCode(500, "An error occurred while updating the email template");
            }
        }

        [HttpDelete("templates/{templateId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteEmailTemplate(Guid eventId, Guid templateId)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                await _emailService.DeleteEmailTemplateAsync(templateId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email template {TemplateId}", templateId);
                return StatusCode(500, "An error occurred while deleting the email template");
            }
        }

        // Email Sending

        [HttpPost("send")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<ActionResult<bool>> SendEventEmail(Guid eventId, [FromBody] SendEventEmailRequest request)
        {
            try
            {
                bool result;
                
                if (request.Recipients == EmailRecipientType.Specific && !string.IsNullOrEmpty(request.SpecificUserIds))
                {
                    var recipientEmails = request.SpecificUserIds.Split(',').ToList();
                    result = await _emailService.SendCustomEventEmailAsync(eventId, request.Subject, request.Body, recipientEmails);
                }
                else
                {
                    result = await _emailService.SendCustomEventEmailAsync(eventId, request.Subject, request.Body);
                }
                
                if (request.SaveAsTemplate && !string.IsNullOrEmpty(request.TemplateName))
                {
                    var template = new EventEmailTemplate(
                        eventId: eventId,
                        type: EmailTemplateType.Custom,
                        subject: request.Subject,
                        body: request.Body);
                    
                    await _emailService.CreateEmailTemplateAsync(template);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event email for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while sending the email");
            }
        }

        [HttpPost("send/{templateId}")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<bool>> SendEmailFromTemplate(Guid eventId, Guid templateId)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                var result = await _emailService.SendEventEmailAsync(eventId, templateId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email from template {TemplateId} for event {EventId}", templateId, eventId);
                return StatusCode(500, "An error occurred while sending the email");
            }
        }

        [HttpPost("preview/{templateId}")]
        [ProducesResponseType(typeof(EmailPreviewResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EmailPreviewResponse>> PreviewEmailTemplate(Guid eventId, Guid templateId, [FromQuery] Guid? sampleUserId = null)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                var preview = await _emailService.PreviewEmailTemplateAsync(templateId, sampleUserId);
                
                return Ok(new EmailPreviewResponse
                {
                    Subject = preview.Split('\n')[0], // First line is subject
                    Body = string.Join('\n', preview.Split('\n').Skip(1)) // Rest is body
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing email template {TemplateId}", templateId);
                return StatusCode(500, "An error occurred while previewing the email template");
            }
        }

        [HttpPost("test/{templateId}")]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<bool>> SendTestEmail(Guid eventId, Guid templateId, [FromBody] SendTestEmailRequest request)
        {
            try
            {
                var template = await _emailService.GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                    return NotFound();
                
                var result = await _emailService.SendTestEmailAsync(templateId, request.TestEmail);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email for template {TemplateId}", templateId);
                return StatusCode(500, "An error occurred while sending the test email");
            }
        }

        [HttpGet("recipients")]
        [ProducesResponseType(typeof(EmailRecipientsResponse), 200)]
        public async Task<ActionResult<EmailRecipientsResponse>> GetEmailRecipients(Guid eventId, [FromQuery] EmailTemplateType templateType)
        {
            try
            {
                var recipients = await _emailService.GetEventEmailRecipientsAsync(eventId, templateType);
                var count = await _emailService.GetRecipientCountAsync(eventId, templateType);
                
                return Ok(new EmailRecipientsResponse
                {
                    Recipients = recipients,
                    Count = count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email recipients for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while retrieving email recipients");
            }
        }

        [HttpGet("history")]
        [ProducesResponseType(typeof(List<EmailHistoryDto>), 200)]
        public async Task<ActionResult<List<EmailHistoryDto>>> GetEmailHistory(Guid eventId)
        {
            try
            {
                var history = await _emailService.GetEmailHistoryAsync(eventId);
                
                var historyDtos = history.Select(h => new EmailHistoryDto
                {
                    Id = h.Id,
                    EventId = h.EventId,
                    TemplateId = h.TemplateId,
                    Subject = h.Subject,
                    RecipientCount = h.Recipients.Count,
                    SentAt = h.SentAt,
                    IsSuccessful = h.IsSuccessful,
                    ErrorMessage = h.ErrorMessage
                }).ToList();
                
                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email history for event {EventId}", eventId);
                return StatusCode(500, "An error occurred while retrieving email history");
            }
        }

        [HttpGet("system-templates")]
        [ProducesResponseType(typeof(List<EventEmailTemplateDto>), 200)]
        public async Task<ActionResult<List<EventEmailTemplateDto>>> GetSystemTemplates()
        {
            try
            {
                var templates = await _emailService.GetSystemTemplatesAsync();
                
                var templateDtos = templates.Select(t => new EventEmailTemplateDto
                {
                    Id = t.Id,
                    EventId = t.EventId,
                    Type = t.Type,
                    Subject = t.Subject,
                    Body = t.Body,
                    IsActive = t.IsActive,
                    UpdatedAt = t.UpdatedAt
                }).ToList();
                
                return Ok(templateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system email templates");
                return StatusCode(500, "An error occurred while retrieving system templates");
            }
        }
    }

    // Response DTOs
    public class EmailPreviewResponse
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class SendTestEmailRequest
    {
        public string TestEmail { get; set; } = string.Empty;
    }

    public class EmailRecipientsResponse
    {
        public List<string> Recipients { get; set; } = new();
        public int Count { get; set; }
    }

    public class EmailHistoryDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid? TemplateId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }
}