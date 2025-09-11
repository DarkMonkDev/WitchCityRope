namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Types of events supported by the system
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Educational class or workshop - requires ticket purchase (paid)
        /// </summary>
        Class,

        /// <summary>
        /// Social gathering - allows both RSVP (free) and optional ticket purchases
        /// </summary>
        Social
    }
}