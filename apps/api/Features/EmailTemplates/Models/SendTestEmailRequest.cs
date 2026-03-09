namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// Request model for sending a test email for a specific template.
/// The email field is the recipient address, and variableOverrides allows
/// overriding saved test data defaults for this specific send.
/// Overrides are automatically saved back to the defaults.
/// </summary>
public class SendTestEmailRequest
{
    /// <summary>
    /// Email address to send the test email to
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional variable value overrides. Keys are variable names WITHOUT braces
    /// (e.g., "scene_name" not "{{scene_name}}"). Values provided here take precedence
    /// over saved defaults and are auto-saved back to defaults after sending.
    /// </summary>
    public Dictionary<string, string>? VariableOverrides { get; set; }
}
