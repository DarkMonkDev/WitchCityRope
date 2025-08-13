namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// User status consolidating previous Status and IsVetted fields
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// New user, not yet reviewed
        /// </summary>
        PendingReview,
        
        /// <summary>
        /// Approved and vetted member
        /// </summary>
        Vetted,
        
        /// <summary>
        /// No vetting application submitted
        /// </summary>
        NoApplication,
        
        /// <summary>
        /// Temporarily suspended/on hold
        /// </summary>
        OnHold,
        
        /// <summary>
        /// Permanently banned
        /// </summary>
        Banned
    }
}