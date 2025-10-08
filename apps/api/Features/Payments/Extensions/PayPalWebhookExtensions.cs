using System.Text.Json;

namespace WitchCityRope.Api.Features.Payments.Extensions;

/// <summary>
/// Extensions for safely extracting values from PayPal webhook Dictionary&lt;string, object&gt; data
/// Handles JsonElement objects from System.Text.Json deserialization
/// </summary>
public static class PayPalWebhookExtensions
{
    /// <summary>
    /// Safely extracts a string value from the webhook dictionary, handling JsonElement objects
    /// </summary>
    public static string? GetStringValue(this Dictionary<string, object> webhookData, string key)
    {
        if (!webhookData.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            JsonElement jsonElement => jsonElement.GetString(),
            string str => str,
            _ => value?.ToString()
        };
    }

    /// <summary>
    /// Safely extracts a required string value from the webhook dictionary
    /// Throws an exception if the key is missing or the value is null/empty
    /// </summary>
    public static string GetRequiredStringValue(this Dictionary<string, object> webhookData, string key)
    {
        var value = GetStringValue(webhookData, key);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Required webhook property '{key}' is missing or empty");
        return value;
    }

    /// <summary>
    /// Safely extracts a DateTime value from the webhook dictionary
    /// </summary>
    public static DateTime? GetDateTimeValue(this Dictionary<string, object> webhookData, string key)
    {
        var stringValue = GetStringValue(webhookData, key);
        return DateTime.TryParse(stringValue, out var dateTime) ? dateTime : null;
    }

    /// <summary>
    /// Safely extracts a decimal value from the webhook dictionary
    /// </summary>
    public static decimal? GetDecimalValue(this Dictionary<string, object> webhookData, string key)
    {
        if (!webhookData.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number => jsonElement.GetDecimal(),
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String =>
                decimal.TryParse(jsonElement.GetString(), out var result) ? result : null,
            decimal decimalValue => decimalValue,
            string str => decimal.TryParse(str, out var result) ? result : null,
            _ => null
        };
    }

    /// <summary>
    /// Safely extracts a nested object from the webhook dictionary
    /// </summary>
    public static Dictionary<string, object>? GetObjectValue(this Dictionary<string, object> webhookData, string key)
    {
        if (!webhookData.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Object =>
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText()),
            Dictionary<string, object> dict => dict,
            _ => null
        };
    }
}