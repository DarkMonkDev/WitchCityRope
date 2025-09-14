using System.Text.Json.Serialization;

namespace WitchCityRope.Api.Features.Payments.Models.PayPal;

/// <summary>
/// PayPal webhook event model for deserializing incoming webhook payloads
/// Maps PayPal's snake_case JSON properties to C# PascalCase properties
/// </summary>
public class PayPalWebhookEvent
{
    /// <summary>
    /// Unique identifier for the webhook event
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The event type that triggered the webhook
    /// Examples: PAYMENT.CAPTURE.COMPLETED, CHECKOUT.ORDER.APPROVED
    /// </summary>
    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// When the webhook event was created
    /// </summary>
    [JsonPropertyName("create_time")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// Type of the resource that triggered the webhook
    /// Examples: capture, order, refund
    /// </summary>
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the event structure
    /// </summary>
    [JsonPropertyName("event_version")]
    public string EventVersion { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable summary of the event
    /// </summary>
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// The actual resource data that triggered the webhook
    /// This will contain different properties depending on the resource_type
    /// </summary>
    [JsonPropertyName("resource")]
    public Dictionary<string, object> Resource { get; set; } = new();

    /// <summary>
    /// Links for related operations
    /// </summary>
    [JsonPropertyName("links")]
    public List<PayPalLink> Links { get; set; } = new();
}