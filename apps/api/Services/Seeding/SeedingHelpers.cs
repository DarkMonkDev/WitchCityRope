namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Shared utility methods for seeding operations.
/// Provides common functionality used across multiple seeders to eliminate code duplication.
/// </summary>
public static class SeedingHelpers
{
    /// <summary>
    /// Generates a realistic PayPal Order ID for seed data.
    /// Format matches real PayPal order IDs: 17-character alphanumeric string.
    /// Example: "8XY12345AB678901Z"
    /// </summary>
    public static string GeneratePayPalOrderId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var orderId = new char[17];

        for (int i = 0; i < 17; i++)
        {
            orderId[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(orderId);
    }

    /// <summary>
    /// Generates a realistic PayPal Capture ID for seed data.
    /// Format matches real PayPal capture IDs: 17-character alphanumeric string.
    /// Example: "2AB12345CD678901E"
    /// </summary>
    public static string GeneratePayPalCaptureId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var captureId = new char[17];

        for (int i = 0; i < 17; i++)
        {
            captureId[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(captureId);
    }

    /// <summary>
    /// Generates a realistic PayPal Payer ID for seed data.
    /// Format: "PAYER" prefix + 10 random alphanumeric characters.
    /// Example: "PAYER1A2B3C4D5E"
    /// </summary>
    public static string GeneratePayPalPayerId()
    {
        return $"PAYER{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
    }

    /// <summary>
    /// Gets a random payment method for ticket purchases.
    /// Distribution: 50% PayPal, 30% Venmo, 20% Cash
    /// </summary>
    public static string GetRandomPaymentMethod()
    {
        var random = Random.Shared.Next(0, 10);
        return random switch
        {
            >= 0 and < 5 => "PayPal",  // 50%
            >= 5 and < 8 => "Venmo",   // 30%
            _ => "Cash"                 // 20%
        };
    }

    /// <summary>
    /// Gets a random payment method for social event donations specifically.
    /// Distribution: 67% PayPal, 33% Cash (social events typically cash at door or PayPal online)
    /// </summary>
    public static string GetRandomSocialEventPaymentMethod()
    {
        return Random.Shared.Next(0, 3) == 0 ? "Cash" : "PayPal";
    }

    /// <summary>
    /// Gets random purchase notes for realistic data.
    /// Most purchases have no notes (empty strings), occasional member comments.
    /// </summary>
    public static string GetRandomPurchaseNotes()
    {
        var notes = new[]
        {
            "", "", "", "", "", "", // Most purchases have no notes (60%)
            "First time attending!",
            "Vegetarian meal preference",
            "Mobility assistance needed",
            "Paid sliding scale minimum",
            "Group purchase for partners"
        };
        return notes[Random.Shared.Next(notes.Length)];
    }

    /// <summary>
    /// Gets random RSVP notes for social events.
    /// Returns varied, realistic notes that members might leave when RSVPing.
    /// </summary>
    public static string GetRandomRsvpNotes()
    {
        var notes = new[]
        {
            "Looking forward to it!",
            "Bringing a friend",
            "First time!",
            "Excited to join!",
            "Can't wait!",
            "New member here",
            "Looking forward to meeting everyone",
            "See you there!",
            "Count me in!",
            "Thanks for organizing this!",
            "Will be there!",
            "Happy to participate",
            "Looking forward to connecting",
            "Excited for this event",
            "Glad to RSVP!"
        };
        return notes[Random.Shared.Next(notes.Length)];
    }
}
