namespace WitchCityRope.Api.Enums;

/// <summary>
/// Defines the different types of events that can be created in the system.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Educational workshop or class event.
    /// </summary>
    Class = 1,

    /// <summary>
    /// Social gathering or party event.
    /// </summary>
    Social = 2,

    /// <summary>
    /// Performance or demonstration event.
    /// </summary>
    Performance = 3,

    /// <summary>
    /// Special event category for unique or one-off events.
    /// </summary>
    Special = 4,

    /// <summary>
    /// Online or virtual event.
    /// </summary>
    Virtual = 5,

    /// <summary>
    /// Multi-day intensive training event.
    /// </summary>
    Intensive = 6
}