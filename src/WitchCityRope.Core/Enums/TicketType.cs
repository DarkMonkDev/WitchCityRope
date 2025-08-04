using System;

namespace WitchCityRope.Core.Enums
{
    /// <summary>
    /// Types of tickets available for an event
    /// </summary>
    [Flags]
    public enum TicketType
    {
        /// <summary>
        /// Individual tickets only
        /// </summary>
        Individual = 1,

        /// <summary>
        /// Couples tickets only
        /// </summary>
        Couples = 2,

        /// <summary>
        /// Both individual and couples tickets
        /// </summary>
        Both = Individual | Couples
    }
}