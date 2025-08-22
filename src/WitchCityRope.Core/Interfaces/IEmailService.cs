using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Interfaces
{
    /// <summary>
    /// Service for sending email notifications
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email to a single recipient
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <param name="isHtml">Whether the body contains HTML</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendEmailAsync(EmailAddress to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Sends an email to multiple recipients
        /// </summary>
        /// <param name="to">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <param name="isHtml">Whether the body contains HTML</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendBulkEmailAsync(IEnumerable<EmailAddress> to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Sends an email using a predefined template
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="templateName">Name of the email template</param>
        /// <param name="templateData">Data to populate the template</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendTemplateEmailAsync(EmailAddress to, string templateName, object templateData);

        /// <summary>
        /// Sends a registration confirmation email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="sceneName">User's scene name</param>
        /// <param name="eventTitle">Title of the event</param>
        /// <param name="eventDate">Date of the event</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendRegistrationConfirmationAsync(EmailAddress to, string sceneName, string eventTitle, DateTime eventDate);

        /// <summary>
        /// Sends a cancellation confirmation email
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="sceneName">User's scene name</param>
        /// <param name="eventTitle">Title of the event</param>
        /// <param name="refundAmount">Refund amount if applicable</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendCancellationConfirmationAsync(EmailAddress to, string sceneName, string eventTitle, Money? refundAmount = null);

        /// <summary>
        /// Sends a vetting application status update
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="sceneName">User's scene name</param>
        /// <param name="status">Application status</param>
        /// <param name="notes">Additional notes or feedback</param>
        /// <returns>True if sent successfully</returns>
        Task<bool> SendVettingStatusUpdateAsync(EmailAddress to, string sceneName, string status, string? notes = null);
    }
}