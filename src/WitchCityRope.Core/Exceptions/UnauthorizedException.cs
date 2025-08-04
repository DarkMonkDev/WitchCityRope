using System;

namespace WitchCityRope.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation is unauthorized
    /// </summary>
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message = "Unauthorized") : base(message)
        {
        }

        public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}