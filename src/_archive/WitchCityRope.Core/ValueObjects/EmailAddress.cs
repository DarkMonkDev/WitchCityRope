using System;
using System.Text.RegularExpressions;

namespace WitchCityRope.Core.ValueObjects
{
    /// <summary>
    /// Represents a valid email address
    /// Business Rules:
    /// - Must be a valid email format
    /// - Case-insensitive for comparison
    /// - Used for user communication and authentication
    /// </summary>
    public class EmailAddress : IEquatable<EmailAddress>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private EmailAddress(string value)
        {
            Value = value.ToLowerInvariant(); // Store in lowercase for consistency
            DisplayValue = value; // Preserve original casing for display
        }

        public string Value { get; }
        
        public string DisplayValue { get; }

        public static EmailAddress Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email address cannot be empty", nameof(value));

            value = value.Trim();

            if (!IsValidEmail(value))
                throw new DomainException($"Invalid email address format: {value}");

            return new EmailAddress(value);
        }

        private static bool IsValidEmail(string email)
        {
            if (email.Length > 254) // RFC 5321
                return false;

            return EmailRegex.IsMatch(email);
        }

        public string GetDomain()
        {
            var atIndex = Value.IndexOf('@');
            return atIndex >= 0 ? Value.Substring(atIndex + 1) : string.Empty;
        }

        public string GetLocalPart()
        {
            var atIndex = Value.IndexOf('@');
            return atIndex >= 0 ? Value.Substring(0, atIndex) : Value;
        }

        public override string ToString() => DisplayValue;

        public override bool Equals(object? obj)
        {
            return Equals(obj as EmailAddress);
        }

        public bool Equals(EmailAddress? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            // Case-insensitive comparison
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public static bool operator ==(EmailAddress left, EmailAddress right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(EmailAddress left, EmailAddress right)
        {
            return !(left == right);
        }

        public static implicit operator string(EmailAddress email)
        {
            return email?.DisplayValue;
        }

        /// <summary>
        /// Tries to create an EmailAddress from a string
        /// </summary>
        public static bool TryCreate(string value, out EmailAddress email)
        {
            email = null;
            
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            
            if (!IsValidEmail(value))
                return false;

            email = new EmailAddress(value);
            return true;
        }
    }
}