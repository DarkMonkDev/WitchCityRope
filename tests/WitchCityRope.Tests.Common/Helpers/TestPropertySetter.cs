using System;
using System.Reflection;

namespace WitchCityRope.Tests.Common.Helpers
{
    /// <summary>
    /// Helper class to set readonly properties via reflection for testing purposes
    /// </summary>
    public static class TestPropertySetter
    {
        /// <summary>
        /// Sets a property value using reflection, even if it has a private setter
        /// </summary>
        public static void SetProperty<T>(T obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            
            var type = obj.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            
            if (property == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on type '{type.Name}'");
            }
            
            // Try to set the property directly first
            if (property.CanWrite)
            {
                property.SetValue(obj, value);
                return;
            }
            
            // If direct setting fails, look for a backing field
            var backingField = type.GetField($"<{propertyName}>k__BackingField", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (backingField != null)
            {
                backingField.SetValue(obj, value);
                return;
            }
            
            // Try alternative backing field naming conventions
            backingField = type.GetField($"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (backingField != null)
            {
                backingField.SetValue(obj, value);
                return;
            }
            
            throw new InvalidOperationException(
                $"Unable to set property '{propertyName}' on type '{type.Name}'. " +
                "No public setter or backing field found.");
        }
        
        /// <summary>
        /// Sets multiple properties at once
        /// </summary>
        public static void SetProperties<T>(T obj, params (string propertyName, object value)[] properties)
        {
            foreach (var (propertyName, value) in properties)
            {
                SetProperty(obj, propertyName, value);
            }
        }
    }
}