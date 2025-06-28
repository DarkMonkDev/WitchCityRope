using System;

namespace WitchCityRope.Api.Exceptions
{
    /// <summary>
    /// Exception thrown when a resource conflict occurs (HTTP 409)
    /// </summary>
    public class ConflictException : Exception
    {
        public ConflictException() : base()
        {
        }

        public ConflictException(string message) : base(message)
        {
        }

        public ConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}