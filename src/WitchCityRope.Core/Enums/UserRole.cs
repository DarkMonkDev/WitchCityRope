namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// User roles within the system
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Standard event attendee
        /// </summary>
        Attendee = 0,

        /// <summary>
        /// Verified community member with additional privileges
        /// </summary>
        Member = 1,

        /// <summary>
        /// Event organizer who can create and manage events
        /// </summary>
        Organizer = 2,

        /// <summary>
        /// Community moderator who can review incidents and vetting
        /// </summary>
        Moderator = 3,

        /// <summary>
        /// System administrator with full access
        /// </summary>
        Administrator = 4
    }
}