using System;

namespace WitchCityRope.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when there is a conflict with the current state
    /// </summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}