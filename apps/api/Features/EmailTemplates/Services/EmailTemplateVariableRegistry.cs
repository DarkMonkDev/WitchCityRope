using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Services;

/// <summary>
/// Centralized registry of available template variables for each (Category, TemplateType) pair.
/// This replaces the per-row Variables JSONB column on GlobalEmailTemplates.
/// Variables are defined here in code — the single source of truth — rather than stored in the database.
/// </summary>
public static class EmailTemplateVariableRegistry
{
    private static readonly Dictionary<(EmailCategory Category, string TemplateType), string[]> Registry = new()
    {
        // ========================================
        // Events Category (15 templates)
        // ========================================
        // Source: EventEmailService.SendConfirmationAsync()

        [(EmailCategory.Events, "Confirmation")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_date}}", "{{session_time}}",
            "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}",
            "{{ticket_type}}", "{{total_paid}}", "{{confirmation_number}}",
            "{{ticket_sessions_list}}", "{{ticket_sessions_list_text}}",
            "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EmailSchedulerJob.ProcessSessionAsync() — attendee reminders
        // session_name_link wraps session_name in an <a> tag linking to event details page.
        // event_title_link wraps event_title in an <a> tag linking to event details page.
        // Use *_link variants in HTML bodies, plain names in subjects and plain text bodies.
        [(EmailCategory.Events, "Reminder1Week")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_name}}", "{{session_name_link}}",
            "{{session_date}}", "{{session_time}}", "{{event_date}}", "{{event_time}}",
            "{{venue_name}}", "{{venue_address}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        [(EmailCategory.Events, "Reminder1Day")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_name}}", "{{session_name_link}}",
            "{{session_date}}", "{{session_time}}", "{{event_date}}", "{{event_time}}",
            "{{venue_name}}", "{{venue_address}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        [(EmailCategory.Events, "Reminder2Hours")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_name}}", "{{session_name_link}}",
            "{{session_date}}", "{{session_time}}", "{{event_date}}", "{{event_time}}",
            "{{venue_name}}", "{{venue_address}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EventEmailService.SendCancellationAsync()
        [(EmailCategory.Events, "Cancellation")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_date}}", "{{event_date}}",
            "{{custom_message}}", "{{cancelled_sessions_list}}", "{{cancelled_sessions_list_text}}",
            "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EventEmailService.SendRsvpConfirmationEmailAsync()
        [(EmailCategory.Events, "RSVPConfirmation")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_date}}", "{{session_time}}",
            "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}",
            "{{event_details_url}}", "{{event_details_button}}",
            "{{rsvp_sessions_list}}", "{{rsvp_sessions_list_text}}"
        },

        // Source: EventEmailService — RSVP cancellation
        [(EmailCategory.Events, "RSVPCancellation")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_date}}", "{{session_time}}",
            "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}",
            "{{custom_message}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: Not yet wired to sending code
        [(EmailCategory.Events, "SessionChange")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_name}}", "{{session_name_link}}",
            "{{session_date}}", "{{session_time}}", "{{event_date}}", "{{event_time}}",
            "{{custom_message}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: Not yet wired to sending code
        [(EmailCategory.Events, "ThankYou")] = new[]
        {
            "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}", "{{session_name}}", "{{session_name_link}}",
            "{{session_date}}", "{{event_date}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EmailSchedulerJob.ProcessSessionAsync() — volunteer branch
        // volunteer_tasks_list contains a bulleted HTML list of all volunteer assignments.
        // volunteer_tasks_list_text is the plain text equivalent for plain text email bodies.
        [(EmailCategory.Events, "VolunteerReminder")] = new[]
        {
            "{{volunteer_name}}", "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{session_name}}", "{{session_name_link}}", "{{session_date}}", "{{session_time}}",
            "{{event_date}}", "{{event_time}}", "{{volunteer_tasks_list}}", "{{volunteer_tasks_list_text}}",
            "{{venue_name}}", "{{venue_address}}", "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EmailSchedulerJob.ProcessSessionAsync() — volunteer branch
        [(EmailCategory.Events, "VolunteerThankYou")] = new[]
        {
            "{{volunteer_name}}", "{{attendee_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{session_name}}", "{{session_name_link}}", "{{session_date}}", "{{event_date}}",
            "{{volunteer_tasks_list}}", "{{volunteer_tasks_list_text}}",
            "{{event_details_url}}", "{{event_details_button}}"
        },

        // Source: EventEmailService.SendTicketAssignmentNotificationAsync() — sent on ticket assignment
        [(EmailCategory.Events, "TicketAssignmentNotification")] = new[]
        {
            "{{recipient_scene_name}}", "{{delegate_scene_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{event_date}}", "{{event_time}}", "{{event_venue}}", "{{ticket_type_name}}",
            "{{accept_url}}", "{{accept_button}}"
        },

        // Source: EventEmailService.SendRsvpAssignmentNotificationAsync() — sent on proxy RSVP creation
        [(EmailCategory.Events, "RsvpAssignmentNotification")] = new[]
        {
            "{{recipient_scene_name}}", "{{delegate_scene_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{event_date}}", "{{event_time}}", "{{event_venue}}",
            "{{accept_url}}", "{{accept_button}}"
        },

        // Source: EmailSchedulerJob (PendingAssignmentHolders branch) — ticket acceptance reminders
        [(EmailCategory.Events, "TicketAcceptanceReminder")] = new[]
        {
            "{{recipient_scene_name}}", "{{delegate_scene_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{event_date}}", "{{event_time}}", "{{event_start_time}}", "{{event_venue}}",
            "{{ticket_type_name}}", "{{accept_url}}", "{{accept_button}}"
        },

        // Source: EmailSchedulerJob (PendingAssignmentHolders branch) — RSVP acceptance reminders
        [(EmailCategory.Events, "RsvpAcceptanceReminder")] = new[]
        {
            "{{recipient_scene_name}}", "{{delegate_scene_name}}", "{{event_title}}", "{{event_title_link}}",
            "{{event_date}}", "{{event_time}}", "{{event_start_time}}", "{{event_venue}}",
            "{{accept_url}}", "{{accept_button}}"
        },

        // ========================================
        // Vetting Category (6 templates)
        // ========================================
        // Source: VettingEmailService.SendStatusUpdateAsync() sends ALL variables to ALL templates.

        [(EmailCategory.Vetting, "ApplicationReceived")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{submission_date}}",
            "{{application_date}}", "{{status_change_date}}", "{{current_status}}"
        },

        [(EmailCategory.Vetting, "InterviewApproved")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{interview_link}}",
            "{{submission_date}}", "{{application_date}}", "{{status_change_date}}",
            "{{current_status}}", "{{approval_date}}", "{{review_date}}", "{{custom_message}}"
        },

        [(EmailCategory.Vetting, "VettingApproved")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{approval_date}}",
            "{{submission_date}}", "{{application_date}}", "{{status_change_date}}",
            "{{current_status}}", "{{review_date}}", "{{interview_link}}", "{{custom_message}}"
        },

        [(EmailCategory.Vetting, "ApplicationOnHold")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{custom_message}}",
            "{{submission_date}}", "{{application_date}}", "{{status_change_date}}",
            "{{current_status}}", "{{approval_date}}", "{{review_date}}", "{{interview_link}}"
        },

        [(EmailCategory.Vetting, "ApplicationStatusUpdate")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{review_date}}",
            "{{submission_date}}", "{{application_date}}", "{{status_change_date}}",
            "{{current_status}}", "{{approval_date}}", "{{interview_link}}", "{{custom_message}}"
        },

        [(EmailCategory.Vetting, "InterviewReminder")] = new[]
        {
            "{{scene_name}}", "{{application_number}}", "{{submission_date}}",
            "{{application_date}}", "{{status_change_date}}", "{{current_status}}",
            "{{custom_message}}"
        },

        // ========================================
        // Incident Category (4 templates)
        // ========================================
        // Source: IncidentEmailService methods

        [(EmailCategory.Incident, "ReportReceived")] = new[]
        {
            "{{reporter_name}}", "{{incident_number}}", "{{incident_date}}",
            "{{coordinator_name}}", "{{next_steps}}"
        },

        [(EmailCategory.Incident, "StatusUpdate")] = new[]
        {
            "{{reporter_name}}", "{{incident_number}}", "{{status}}", "{{next_steps}}"
        },

        [(EmailCategory.Incident, "AssignmentNotification")] = new[]
        {
            "{{coordinator_name}}", "{{incident_number}}", "{{incident_date}}", "{{next_steps}}"
        },

        [(EmailCategory.Incident, "Resolved")] = new[]
        {
            "{{reporter_name}}", "{{incident_number}}", "{{incident_date}}", "{{next_steps}}"
        },

        // ========================================
        // Admin Category (7 templates)
        // ========================================
        // Source: Admin email services (not all wired yet; variables from seeder definitions)

        [(EmailCategory.Admin, "AccountCreated")] = new[]
        {
            "{{user_name}}", "{{account_email}}"
        },

        [(EmailCategory.Admin, "PasswordReset")] = new[]
        {
            "{{user_name}}", "{{reset_url}}"
        },

        [(EmailCategory.Admin, "RoleChanged")] = new[]
        {
            "{{user_name}}", "{{action_required}}"
        },

        [(EmailCategory.Admin, "SystemNotification")] = new[]
        {
            "{{user_name}}", "{{action_required}}", "{{deadline_date}}"
        },

        [(EmailCategory.Admin, "EmailVerification")] = new[]
        {
            "{{user_name}}", "{{verification_url}}"
        },

        [(EmailCategory.Admin, "RefundConfirmation")] = new[]
        {
            "{{user_name}}", "{{refund_amount}}", "{{original_amount}}", "{{payment_method}}",
            "{{timing_message}}", "{{refund_reason}}", "{{refund_id}}"
        },

        [(EmailCategory.Admin, "NewWebsiteUser")] = new[]
        {
            "{{user_name}}", "{{reset_url}}", "{{system_url}}"
        },

        // ========================================
        // AdHoc Category (1 template)
        // ========================================

        [(EmailCategory.AdHoc, "General")] = new[]
        {
            "{{recipient_name}}", "{{custom_content}}"
        }
    };

    /// <summary>
    /// Gets the available template variables for the given category and template type.
    /// Returns an empty array for unknown template types.
    /// </summary>
    /// <param name="category">The email category (Events, Vetting, Admin, Incident, AdHoc)</param>
    /// <param name="templateType">The template type within the category (e.g., "Confirmation", "ApplicationReceived")</param>
    /// <returns>Array of variable names including {{}} braces, or empty array if not found</returns>
    public static string[] GetVariables(EmailCategory category, string templateType)
    {
        return Registry.TryGetValue((category, templateType), out var variables)
            ? variables
            : Array.Empty<string>();
    }

    /// <summary>
    /// Gets all unique variables across all templates, grouped by category.
    /// Used by the test data editor to show input fields for every possible variable.
    /// Variables are deduplicated within each category and returned without {{}} braces
    /// to match the test data Settings key format (e.g., "attendee_name" not "{{attendee_name}}").
    /// </summary>
    /// <returns>Dictionary mapping category display name to unique variable names (without braces)</returns>
    public static Dictionary<string, string[]> GetAllVariablesGroupedByCategory()
    {
        var result = new Dictionary<string, List<string>>();

        foreach (var ((category, _), variables) in Registry)
        {
            var categoryName = category.ToString();
            if (!result.ContainsKey(categoryName))
                result[categoryName] = new List<string>();

            foreach (var variable in variables)
            {
                // Strip {{}} braces for test data key format
                var stripped = variable.TrimStart('{').TrimEnd('}');
                if (!result[categoryName].Contains(stripped))
                    result[categoryName].Add(stripped);
            }
        }

        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());
    }
}
