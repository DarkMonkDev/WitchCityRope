namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Types of events supported by the system
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Educational workshop or class
        /// </summary>
        Workshop,

        /// <summary>
        /// Educational class (alias for Workshop, used in tests)
        /// </summary>
        Class = Workshop,

        /// <summary>
        /// Social gathering or munch
        /// </summary>
        Social,

        /// <summary>
        /// Play party or dungeon event
        /// </summary>
        PlayParty,

        /// <summary>
        /// Performance or demonstration
        /// </summary>
        Performance,

        /// <summary>
        /// Skill share or peer learning
        /// </summary>
        SkillShare,

        /// <summary>
        /// Special event or celebration
        /// </summary>
        Special,

        /// <summary>
        /// Online/virtual event
        /// </summary>
        Virtual,

        /// <summary>
        /// Multi-day event or conference
        /// </summary>
        Conference
    }
}