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
        /// Vetted community member with enhanced privileges
        /// </summary>
        VettedMember = 2,

        /// <summary>
        /// Teacher who can create and manage educational events
        /// </summary>
        Teacher = 3,

        /// <summary>
        /// Event organizer who can create and manage events
        /// </summary>
        Organizer = 4,

        /// <summary>
        /// Community moderator who can review incidents and vetting
        /// </summary>
        Moderator = 5,

        /// <summary>
        /// System administrator with full access
        /// </summary>
        Administrator = 6,

        /// <summary>
        /// Legacy admin role (mapped to Administrator)
        /// </summary>
        Admin = 7
    }
}