using System;
using System.ComponentModel.DataAnnotations;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Validation
{
    /// <summary>
    /// Validates that end date is after start date
    /// </summary>
    public class EndDateAfterStartDateAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public EndDateAfterStartDateAttribute(string startDatePropertyName)
        {
            _startDatePropertyName = startDatePropertyName;
            ErrorMessage = EventValidationConstants.DATE_RANGE_INVALID;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateTime endDate)
                return ValidationResult.Success;

            var startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null)
                return new ValidationResult($"Property {_startDatePropertyName} not found");

            var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDateValue is not DateTime startDate)
                return ValidationResult.Success;

            if (endDate <= startDate)
                return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that start date is in the future
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = EventValidationConstants.START_DATE_FUTURE;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateTime date)
                return ValidationResult.Success;

            if (date <= DateTime.Now)
                return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates event duration is within acceptable limits
    /// </summary>
    public class ValidEventDurationAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public ValidEventDurationAttribute(string startDatePropertyName)
        {
            _startDatePropertyName = startDatePropertyName;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateTime endDate)
                return ValidationResult.Success;

            var startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null)
                return ValidationResult.Success;

            var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDateValue is not DateTime startDate)
                return ValidationResult.Success;

            var duration = endDate - startDate;
            
            if (duration.TotalMinutes < EventValidationConstants.MIN_EVENT_DURATION_MINUTES)
                return new ValidationResult(EventValidationConstants.EVENT_DURATION_TOO_SHORT, new[] { validationContext.MemberName! });

            if (duration.TotalHours > EventValidationConstants.MAX_EVENT_DURATION_HOURS)
                return new ValidationResult(EventValidationConstants.EVENT_DURATION_TOO_LONG, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that registration window closes before event starts
    /// </summary>
    public class ValidRegistrationWindowAttribute : ValidationAttribute
    {
        private readonly string _startDatePropertyName;

        public ValidRegistrationWindowAttribute(string startDatePropertyName)
        {
            _startDatePropertyName = startDatePropertyName;
            ErrorMessage = EventValidationConstants.REGISTRATION_WINDOW_INVALID;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateTime registrationCloses)
                return ValidationResult.Success;

            var startDateProperty = validationContext.ObjectType.GetProperty(_startDatePropertyName);
            if (startDateProperty == null)
                return ValidationResult.Success;

            var startDateValue = startDateProperty.GetValue(validationContext.ObjectInstance);
            if (startDateValue is not DateTime startDate)
                return ValidationResult.Success;

            if (registrationCloses >= startDate)
                return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates pricing based on pricing type
    /// </summary>
    public class RequiredForPricingTypeAttribute : ValidationAttribute
    {
        private readonly string _pricingTypePropertyName;
        private readonly PricingType _requiredForType;

        public RequiredForPricingTypeAttribute(string pricingTypePropertyName, PricingType requiredForType)
        {
            _pricingTypePropertyName = pricingTypePropertyName;
            _requiredForType = requiredForType;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var pricingTypeProperty = validationContext.ObjectType.GetProperty(_pricingTypePropertyName);
            if (pricingTypeProperty == null)
                return ValidationResult.Success;

            var pricingTypeValue = pricingTypeProperty.GetValue(validationContext.ObjectInstance);
            if (pricingTypeValue is not PricingType pricingType)
                return ValidationResult.Success;

            if (pricingType == _requiredForType)
            {
                if (value == null || (value is decimal decimalValue && decimalValue <= 0))
                {
                    var message = _requiredForType switch
                    {
                        PricingType.Fixed => EventValidationConstants.INDIVIDUAL_PRICE_REQUIRED_FOR_FIXED,
                        PricingType.SlidingScale => validationContext.MemberName?.Contains("Minimum") == true 
                            ? EventValidationConstants.MINIMUM_PRICE_REQUIRED_FOR_SLIDING
                            : validationContext.MemberName?.Contains("Suggested") == true
                            ? EventValidationConstants.SUGGESTED_PRICE_REQUIRED_FOR_SLIDING
                            : EventValidationConstants.MAXIMUM_PRICE_REQUIRED_FOR_SLIDING,
                        _ => "This field is required for the selected pricing type"
                    };
                    return new ValidationResult(message, new[] { validationContext.MemberName! });
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates sliding scale price ordering (min <= suggested <= max)
    /// </summary>
    public class ValidSlidingScalePricesAttribute : ValidationAttribute
    {
        private readonly string _minimumPricePropertyName;
        private readonly string _suggestedPricePropertyName;
        private readonly string _maximumPricePropertyName;

        public ValidSlidingScalePricesAttribute(string minimumPricePropertyName, string suggestedPricePropertyName, string maximumPricePropertyName)
        {
            _minimumPricePropertyName = minimumPricePropertyName;
            _suggestedPricePropertyName = suggestedPricePropertyName;
            _maximumPricePropertyName = maximumPricePropertyName;
            ErrorMessage = EventValidationConstants.SLIDING_SCALE_ORDER_INVALID;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var minProperty = validationContext.ObjectType.GetProperty(_minimumPricePropertyName);
            var suggestedProperty = validationContext.ObjectType.GetProperty(_suggestedPricePropertyName);
            var maxProperty = validationContext.ObjectType.GetProperty(_maximumPricePropertyName);

            if (minProperty == null || suggestedProperty == null || maxProperty == null)
                return ValidationResult.Success;

            var minValue = minProperty.GetValue(validationContext.ObjectInstance) as decimal?;
            var suggestedValue = suggestedProperty.GetValue(validationContext.ObjectInstance) as decimal?;
            var maxValue = maxProperty.GetValue(validationContext.ObjectInstance) as decimal?;

            // Only validate if all values are present
            if (!minValue.HasValue || !suggestedValue.HasValue || !maxValue.HasValue)
                return ValidationResult.Success;

            if (minValue > suggestedValue || suggestedValue > maxValue)
                return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that instructor is required for workshop events
    /// </summary>
    public class RequiredForEventTypeAttribute : ValidationAttribute
    {
        private readonly string _eventTypePropertyName;
        private readonly EventType _requiredForType;

        public RequiredForEventTypeAttribute(string eventTypePropertyName, EventType requiredForType)
        {
            _eventTypePropertyName = eventTypePropertyName;
            _requiredForType = requiredForType;
            ErrorMessage = _requiredForType == EventType.Workshop 
                ? EventValidationConstants.INSTRUCTOR_REQUIRED_FOR_WORKSHOP
                : $"This field is required for {_requiredForType} events";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var eventTypeProperty = validationContext.ObjectType.GetProperty(_eventTypePropertyName);
            if (eventTypeProperty == null)
                return ValidationResult.Success;

            var eventTypeValue = eventTypeProperty.GetValue(validationContext.ObjectInstance);
            if (eventTypeValue is not EventType eventType)
                return ValidationResult.Success;

            if (eventType == _requiredForType)
            {
                if (value == null || (value is Guid guidValue && guidValue == Guid.Empty))
                    return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });
            }

            return ValidationResult.Success;
        }
    }
}