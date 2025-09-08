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
        /// Basic community member
        /// </summary>
        Member = 1,

        /// <summary>
        /// Vetted community member with additional privileges
        /// </summary>
        VettedMember = 2,

        /// <summary>
        /// Teacher who can create and manage classes
        /// </summary>
        Teacher = 3,

        /// <summary>
        /// System administrator with full access
        /// </summary>
        Admin = 4,

        /// <summary>
        /// Event organizer who can create and manage events
        /// </summary>
        Organizer = 5,

        /// <summary>
        /// Community moderator who can review incidents and vetting
        /// </summary>
        Moderator = 6,

        /// <summary>
        /// Legacy - System administrator with full access
        /// </summary>
        Administrator = 7
    }
}