using System;

namespace WitchCityRope.Core.ValueObjects
{
    /// <summary>
    /// Represents a scene name used within the community
    /// Business Rules:
    /// - Scene names must be between 2 and 50 characters
    /// - Scene names cannot contain offensive content
    /// - Scene names are case-insensitive for uniqueness
    /// </summary>
    public class SceneName : IEquatable<SceneName>
    {
        private SceneName(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static SceneName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Scene name cannot be empty", nameof(value));

            value = value.Trim();

            if (value.Length < 2)
                throw new DomainException("Scene name must be at least 2 characters long");

            if (value.Length > 50)
                throw new DomainException("Scene name cannot exceed 50 characters");

            // Additional validation for offensive content would go here
            // This would typically involve checking against a list of prohibited terms

            return new SceneName(value);
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
        {
            return Equals(obj as SceneName);
        }

        public bool Equals(SceneName? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            // Case-insensitive comparison for uniqueness
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Value?.ToLowerInvariant().GetHashCode() ?? 0;
        }

        public static bool operator ==(SceneName left, SceneName right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(SceneName left, SceneName right)
        {
            return !(left == right);
        }

        public static implicit operator string(SceneName sceneName)
        {
            return sceneName?.Value;
        }
    }
}