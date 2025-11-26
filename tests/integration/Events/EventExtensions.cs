using WitchCityRope.Api.Models;

namespace WitchCityRope.IntegrationTests.Events;

/// <summary>
/// Extensions for Event model used in testing to track email template count
/// </summary>
public static class EventExtensions
{
    // Store template count in a dictionary since we can't extend the Event model with properties
    private static Dictionary<Guid, int> _templateCounts = new();

    public static int GetEmailTemplateCount(this Event evt)
    {
        return _templateCounts.TryGetValue(evt.Id, out var count) ? count : 0;
    }

    public static void SetEmailTemplateCount(this Event evt, int count)
    {
        _templateCounts[evt.Id] = count;
    }
}
