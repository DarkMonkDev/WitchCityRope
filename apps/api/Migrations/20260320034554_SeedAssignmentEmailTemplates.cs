using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WitchCityRope.Api.Migrations
{
    /// <summary>
    /// Seeds 4 assignment-related email templates:
    /// - TicketAssignmentNotification: Sent when someone purchases a ticket on behalf of another user
    /// - RsvpAssignmentNotification: Sent when someone RSVPs on behalf of another user
    /// - TicketAcceptanceReminder: Time-based reminder 1 day before event for pending ticket assignments
    /// - RsvpAcceptanceReminder: Time-based reminder 1 day before event for pending RSVP assignments
    /// </summary>
    public partial class SeedAssignmentEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Template 1: TicketAssignmentNotification
            // Sent immediately when a delegate purchases a ticket for another user.
            // Recipient is resolved by the sending service, not the scheduler.
            migrationBuilder.Sql(
                """
                INSERT INTO public."GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody",
                    "TriggerType", "SendingEnabled",
                    "TimingOffsetDays", "TimingOffsetHours", "RecipientGroup",
                    "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    1, -- Events category
                    'TicketAssignmentNotification',
                    'Ticket Assigned: {{event_title}}',
                    '{{delegate_scene_name}} purchased a ticket for you to {{event_title}}',
                    '<p style="margin-bottom: 16px;">Hi {{recipient_scene_name}},</p>
                <p style="margin-bottom: 16px;"><strong>{{delegate_scene_name}}</strong> has purchased a ticket for you to attend <strong>{{event_title}}</strong>!</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Event Details</h2>
                <p style="margin-bottom: 16px;">
                <strong>Event:</strong> {{event_title}}<br>
                <strong>Date:</strong> {{event_date}}<br>
                <strong>Time:</strong> {{event_time}}<br>
                <strong>Venue:</strong> {{event_venue}}<br>
                <strong>Ticket Type:</strong> {{ticket_type_name}}</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Accept Your Ticket</h2>
                <p style="margin-bottom: 16px;">To attend this event, you need to accept your ticket. This includes reviewing and agreeing to the event waiver and terms of service.</p>
                <p style="margin-bottom: 24px; text-align: center;">{{accept_button}}</p>
                <p style="margin-bottom: 16px; color: #666;">This ticket will remain available for you to accept until the event begins.</p>
                <p style="margin-bottom: 16px;">If you have any questions, please contact <a href="mailto:events@witchcityrope.com" style="color: #880124;">events@witchcityrope.com</a></p>
                <p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">WitchCityRope &bull; Salem, MA &bull; <a href="https://witchcityrope.com" style="color: #880124;">witchcityrope.com</a></p>',
                    E'Hi {{recipient_scene_name}},\n\n{{delegate_scene_name}} has purchased a ticket for you to attend {{event_title}}!\n\nEVENT DETAILS\n=============\nEvent: {{event_title}}\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{event_venue}}\nTicket Type: {{ticket_type_name}}\n\nACCEPT YOUR TICKET\n==================\nTo attend this event, you need to accept your ticket. This includes reviewing and agreeing to the event waiver and terms of service.\n\nAccept your ticket here: {{accept_url}}\n\nThis ticket will remain available for you to accept until the event begins.\n\nIf you have any questions, please contact events@witchcityrope.com\n\n---\nWitchCityRope - Salem, MA - witchcityrope.com',
                    1, -- FixedEvent trigger
                    true, -- SendingEnabled
                    NULL, -- TimingOffsetDays (not time-based)
                    NULL, -- TimingOffsetHours
                    NULL, -- RecipientGroup (resolved by sending service)
                    true, 1,
                    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC',
                    u."Id"
                FROM public."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                  AND NOT EXISTS (
                    SELECT 1 FROM public."GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'TicketAssignmentNotification'
                  )
                LIMIT 1;
                """);

            // Template 2: RsvpAssignmentNotification
            // Sent immediately when a delegate RSVPs on behalf of another user.
            // Recipient is resolved by the sending service, not the scheduler.
            migrationBuilder.Sql(
                """
                INSERT INTO public."GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody",
                    "TriggerType", "SendingEnabled",
                    "TimingOffsetDays", "TimingOffsetHours", "RecipientGroup",
                    "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    1, -- Events category
                    'RsvpAssignmentNotification',
                    'RSVP Created: {{event_title}}',
                    '{{delegate_scene_name}} RSVP''d for you to {{event_title}}',
                    '<p style="margin-bottom: 16px;">Hi {{recipient_scene_name}},</p>
                <p style="margin-bottom: 16px;"><strong>{{delegate_scene_name}}</strong> has RSVP''d for you to attend <strong>{{event_title}}</strong>!</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Event Details</h2>
                <p style="margin-bottom: 16px;">
                <strong>Event:</strong> {{event_title}}<br>
                <strong>Date:</strong> {{event_date}}<br>
                <strong>Time:</strong> {{event_time}}<br>
                <strong>Venue:</strong> {{event_venue}}</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Accept Your RSVP</h2>
                <p style="margin-bottom: 16px;">To attend this event, you need to accept your RSVP. This includes reviewing and agreeing to the event waiver and terms of service.</p>
                <p style="margin-bottom: 24px; text-align: center;">{{accept_button}}</p>
                <p style="margin-bottom: 16px; color: #666;">This RSVP will remain available for you to accept until the event begins.</p>
                <p style="margin-bottom: 16px;">If you have any questions, please contact <a href="mailto:events@witchcityrope.com" style="color: #880124;">events@witchcityrope.com</a></p>
                <p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">WitchCityRope &bull; Salem, MA &bull; <a href="https://witchcityrope.com" style="color: #880124;">witchcityrope.com</a></p>',
                    E'Hi {{recipient_scene_name}},\n\n{{delegate_scene_name}} has RSVP''d for you to attend {{event_title}}!\n\nEVENT DETAILS\n=============\nEvent: {{event_title}}\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{event_venue}}\n\nACCEPT YOUR RSVP\n=================\nTo attend this event, you need to accept your RSVP. This includes reviewing and agreeing to the event waiver and terms of service.\n\nAccept your RSVP here: {{accept_url}}\n\nThis RSVP will remain available for you to accept until the event begins.\n\nIf you have any questions, please contact events@witchcityrope.com\n\n---\nWitchCityRope - Salem, MA - witchcityrope.com',
                    1, -- FixedEvent trigger
                    true, -- SendingEnabled
                    NULL, -- TimingOffsetDays (not time-based)
                    NULL, -- TimingOffsetHours
                    NULL, -- RecipientGroup (resolved by sending service)
                    true, 1,
                    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC',
                    u."Id"
                FROM public."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                  AND NOT EXISTS (
                    SELECT 1 FROM public."GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'RsvpAssignmentNotification'
                  )
                LIMIT 1;
                """);

            // Template 3: TicketAcceptanceReminder
            // Time-based reminder sent 1 day before event to users with pending ticket assignments.
            // PendingAssignmentHolders (4) recipient group resolves users with PendingAcceptance status.
            migrationBuilder.Sql(
                """
                INSERT INTO public."GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody",
                    "TriggerType", "SendingEnabled",
                    "TimingOffsetDays", "TimingOffsetHours", "RecipientGroup",
                    "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    1, -- Events category
                    'TicketAcceptanceReminder',
                    'Reminder: Accept your ticket for {{event_title}}',
                    'Reminder: Accept your ticket for {{event_title}} - Event is tomorrow!',
                    '<p style="margin-bottom: 16px;">Hi {{recipient_scene_name}},</p>
                <p style="margin-bottom: 16px;"><strong>Your ticket for {{event_title}} is still waiting for your acceptance!</strong></p>
                <p style="margin-bottom: 16px;">{{delegate_scene_name}} purchased a ticket for you, and the event is coming up soon.</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Event Details</h2>
                <p style="margin-bottom: 16px;">
                <strong>Event:</strong> {{event_title}}<br>
                <strong>Date:</strong> {{event_date}}<br>
                <strong>Time:</strong> {{event_start_time}}<br>
                <strong>Venue:</strong> {{event_venue}}<br>
                <strong>Ticket Type:</strong> {{ticket_type_name}}</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Accept Your Ticket Now</h2>
                <p style="margin-bottom: 16px;">To attend this event, you need to accept your ticket and agree to the event waiver and terms of service.</p>
                <p style="margin-bottom: 24px; text-align: center;">{{accept_button}}</p>
                <p style="margin-bottom: 16px; color: #666;">You can accept this ticket right up until the event, but we recommend accepting now to avoid any issues.</p>
                <p style="margin-bottom: 16px;">If you have any questions, please contact <a href="mailto:events@witchcityrope.com" style="color: #880124;">events@witchcityrope.com</a></p>
                <p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">WitchCityRope &bull; Salem, MA &bull; <a href="https://witchcityrope.com" style="color: #880124;">witchcityrope.com</a></p>',
                    E'Hi {{recipient_scene_name}},\n\nYour ticket for {{event_title}} is still waiting for your acceptance!\n\n{{delegate_scene_name}} purchased a ticket for you, and the event is coming up soon.\n\nEVENT DETAILS\n=============\nEvent: {{event_title}}\nDate: {{event_date}}\nTime: {{event_start_time}}\nVenue: {{event_venue}}\nTicket Type: {{ticket_type_name}}\n\nACCEPT YOUR TICKET NOW\n======================\nTo attend this event, you need to accept your ticket and agree to the event waiver and terms of service.\n\nAccept your ticket here: {{accept_url}}\n\nYou can accept this ticket right up until the event, but we recommend accepting now to avoid any issues.\n\nIf you have any questions, please contact events@witchcityrope.com\n\n---\nWitchCityRope - Salem, MA - witchcityrope.com',
                    2, -- TimeBased trigger
                    true, -- SendingEnabled
                    1, -- TimingOffsetDays (1 day before event)
                    0, -- TimingOffsetHours
                    4, -- PendingAssignmentHolders recipient group
                    true, 1,
                    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC',
                    u."Id"
                FROM public."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                  AND NOT EXISTS (
                    SELECT 1 FROM public."GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'TicketAcceptanceReminder'
                  )
                LIMIT 1;
                """);

            // Template 4: RsvpAcceptanceReminder
            // Time-based reminder sent 1 day before event to users with pending RSVP assignments.
            // PendingAssignmentHolders (4) recipient group resolves users with PendingAcceptance status.
            migrationBuilder.Sql(
                """
                INSERT INTO public."GlobalEmailTemplates" (
                    "Id", "Category", "TemplateType", "Title", "Subject",
                    "HtmlBody", "PlainTextBody",
                    "TriggerType", "SendingEnabled",
                    "TimingOffsetDays", "TimingOffsetHours", "RecipientGroup",
                    "IsActive", "Version", "CreatedAt", "UpdatedAt", "UpdatedBy"
                )
                SELECT
                    gen_random_uuid(),
                    1, -- Events category
                    'RsvpAcceptanceReminder',
                    'Reminder: Accept your RSVP for {{event_title}}',
                    'Reminder: Accept your RSVP for {{event_title}} - Event is tomorrow!',
                    '<p style="margin-bottom: 16px;">Hi {{recipient_scene_name}},</p>
                <p style="margin-bottom: 16px;"><strong>Your RSVP for {{event_title}} is still waiting for your acceptance!</strong></p>
                <p style="margin-bottom: 16px;">{{delegate_scene_name}} RSVP''d for you, and the event is coming up soon.</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Event Details</h2>
                <p style="margin-bottom: 16px;">
                <strong>Event:</strong> {{event_title}}<br>
                <strong>Date:</strong> {{event_date}}<br>
                <strong>Time:</strong> {{event_start_time}}<br>
                <strong>Venue:</strong> {{event_venue}}</p>
                <h2 style="color: #880124; margin-top: 24px; margin-bottom: 16px;">Accept Your RSVP Now</h2>
                <p style="margin-bottom: 16px;">To attend this event, you need to accept your RSVP and agree to the event waiver and terms of service.</p>
                <p style="margin-bottom: 24px; text-align: center;">{{accept_button}}</p>
                <p style="margin-bottom: 16px; color: #666;">You can accept this RSVP right up until the event, but we recommend accepting now to avoid any issues.</p>
                <p style="margin-bottom: 16px;">If you have any questions, please contact <a href="mailto:events@witchcityrope.com" style="color: #880124;">events@witchcityrope.com</a></p>
                <p style="margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;">WitchCityRope &bull; Salem, MA &bull; <a href="https://witchcityrope.com" style="color: #880124;">witchcityrope.com</a></p>',
                    E'Hi {{recipient_scene_name}},\n\nYour RSVP for {{event_title}} is still waiting for your acceptance!\n\n{{delegate_scene_name}} RSVP''d for you, and the event is coming up soon.\n\nEVENT DETAILS\n=============\nEvent: {{event_title}}\nDate: {{event_date}}\nTime: {{event_start_time}}\nVenue: {{event_venue}}\n\nACCEPT YOUR RSVP NOW\n====================\nTo attend this event, you need to accept your RSVP and agree to the event waiver and terms of service.\n\nAccept your RSVP here: {{accept_url}}\n\nYou can accept this RSVP right up until the event, but we recommend accepting now to avoid any issues.\n\nIf you have any questions, please contact events@witchcityrope.com\n\n---\nWitchCityRope - Salem, MA - witchcityrope.com',
                    2, -- TimeBased trigger
                    true, -- SendingEnabled
                    1, -- TimingOffsetDays (1 day before event)
                    0, -- TimingOffsetHours
                    4, -- PendingAssignmentHolders recipient group
                    true, 1,
                    NOW() AT TIME ZONE 'UTC', NOW() AT TIME ZONE 'UTC',
                    u."Id"
                FROM public."Users" u
                WHERE u."Email" IN ('admin@witchcityrope.com', 'ropemaster@witchcityrope.com')
                  AND NOT EXISTS (
                    SELECT 1 FROM public."GlobalEmailTemplates"
                    WHERE "Category" = 1 AND "TemplateType" = 'RsvpAcceptanceReminder'
                  )
                LIMIT 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM public."GlobalEmailTemplates"
                WHERE "Category" = 1 AND "TemplateType" = 'TicketAssignmentNotification';
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM public."GlobalEmailTemplates"
                WHERE "Category" = 1 AND "TemplateType" = 'RsvpAssignmentNotification';
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM public."GlobalEmailTemplates"
                WHERE "Category" = 1 AND "TemplateType" = 'TicketAcceptanceReminder';
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM public."GlobalEmailTemplates"
                WHERE "Category" = 1 AND "TemplateType" = 'RsvpAcceptanceReminder';
                """);
        }
    }
}
