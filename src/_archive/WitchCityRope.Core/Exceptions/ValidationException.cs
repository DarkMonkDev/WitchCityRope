using System;
using System.Collections.Generic;
using System.Linq;

namespace WitchCityRope.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : DomainException
    {
        public List<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        public ValidationException(List<string> errors) : base("Validation failed")
        {
            Errors = errors ?? new List<string>();
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
            Errors = new List<string> { message };
        }
    }
}