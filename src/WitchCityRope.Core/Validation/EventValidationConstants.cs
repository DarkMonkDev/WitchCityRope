using System;

namespace WitchCityRope.Core.Validation
{
    /// <summary>
    /// Shared validation constants for Event-related validations across all layers
    /// </summary>
    public static class EventValidationConstants
    {
        // Title validation
        public const int TITLE_MAX_LENGTH = 200;
        public const string TITLE_REQUIRED = "Event title is required";
        public const string TITLE_MAX_LENGTH_MESSAGE = "Title must not exceed 200 characters";
        
        // Description validation
        public const int DESCRIPTION_MAX_LENGTH = 4000;
        public const string DESCRIPTION_REQUIRED = "Description is required";
        public const string DESCRIPTION_MAX_LENGTH_MESSAGE = "Description must not exceed 4000 characters";
        
        // Location validation
        public const int LOCATION_MAX_LENGTH = 500;
        public const string LOCATION_REQUIRED = "Location is required";
        public const string LOCATION_MAX_LENGTH_MESSAGE = "Location must not exceed 500 characters";
        
        // Capacity validation
        public const int CAPACITY_MIN = 1;
        public const int CAPACITY_MAX = 1000;
        public const string CAPACITY_REQUIRED = "Capacity is required";
        public const string CAPACITY_RANGE_MESSAGE = "Capacity must be between 1 and 1000";
        
        // Pricing validation
        public const decimal PRICE_MIN = 0;
        public const decimal PRICE_MAX = 10000;
        public const string PRICE_NON_NEGATIVE = "Price cannot be negative";
        public const string PRICE_RANGE_MESSAGE = "Price must be between 0 and 10000";
        
        // Date validation
        public const string START_DATE_REQUIRED = "Start date is required";
        public const string END_DATE_REQUIRED = "End date is required";
        public const string DATE_RANGE_INVALID = "End date must be after start date";
        public const string START_DATE_FUTURE = "Event start date must be in the future";
        public const string REGISTRATION_WINDOW_INVALID = "Registration must close before event starts";
        
        // Refund validation
        public const int REFUND_CUTOFF_MIN = 0;
        public const int REFUND_CUTOFF_MAX = 168; // 1 week
        public const string REFUND_CUTOFF_RANGE_MESSAGE = "Refund cutoff must be between 0 and 168 hours";
        
        // Pricing type specific validation
        public const string INDIVIDUAL_PRICE_REQUIRED_FOR_FIXED = "Individual price is required for fixed pricing";
        public const string MINIMUM_PRICE_REQUIRED_FOR_SLIDING = "Minimum price is required for sliding scale pricing";
        public const string SUGGESTED_PRICE_REQUIRED_FOR_SLIDING = "Suggested price is required for sliding scale pricing";
        public const string MAXIMUM_PRICE_REQUIRED_FOR_SLIDING = "Maximum price is required for sliding scale pricing";
        public const string SLIDING_SCALE_ORDER_INVALID = "Maximum price must be greater than or equal to suggested price, which must be greater than or equal to minimum price";
        
        // Event type validation
        public const string EVENT_TYPE_REQUIRED = "Event type is required";
        public const string ORGANIZER_ID_REQUIRED = "Organizer ID is required";
        
        // Conditional validation messages
        public const string COUPLES_PRICE_INVALID = "Couples price should typically be less than twice the individual price";
        public const string INSTRUCTOR_REQUIRED_FOR_WORKSHOP = "Instructor is required for workshop events";
        
        // Business rule validation
        public const int MIN_EVENT_DURATION_MINUTES = 30;
        public const int MAX_EVENT_DURATION_HOURS = 24;
        public const string EVENT_DURATION_TOO_SHORT = "Event duration must be at least 30 minutes";
        public const string EVENT_DURATION_TOO_LONG = "Event duration cannot exceed 24 hours";
    }
    
    /// <summary>
    /// Validation error codes for programmatic handling
    /// </summary>
    public static class EventValidationErrorCodes
    {
        public const string TITLE_REQUIRED = "EVENT_TITLE_REQUIRED";
        public const string TITLE_TOO_LONG = "EVENT_TITLE_TOO_LONG";
        public const string DESCRIPTION_REQUIRED = "EVENT_DESCRIPTION_REQUIRED";
        public const string DESCRIPTION_TOO_LONG = "EVENT_DESCRIPTION_TOO_LONG";
        public const string CAPACITY_INVALID = "EVENT_CAPACITY_INVALID";
        public const string PRICE_INVALID = "EVENT_PRICE_INVALID";
        public const string DATE_RANGE_INVALID = "EVENT_DATE_RANGE_INVALID";
        public const string START_DATE_PAST = "EVENT_START_DATE_PAST";
        public const string REGISTRATION_WINDOW_INVALID = "EVENT_REGISTRATION_WINDOW_INVALID";
        public const string PRICING_CONFIGURATION_INVALID = "EVENT_PRICING_CONFIGURATION_INVALID";
        public const string INSTRUCTOR_MISSING = "EVENT_INSTRUCTOR_MISSING";
        public const string DURATION_INVALID = "EVENT_DURATION_INVALID";
    }
}