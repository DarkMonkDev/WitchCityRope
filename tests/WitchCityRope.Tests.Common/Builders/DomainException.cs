using System;

namespace WitchCityRope.Tests.Common.Builders
{
    /// <summary>
    /// Domain exception for test builder validation
    /// This will be replaced by the actual Core.Exceptions.DomainException during implementation
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }

        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}