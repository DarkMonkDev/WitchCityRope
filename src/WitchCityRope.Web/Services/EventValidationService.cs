using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Validation;
using WitchCityRope.Web.Models;

namespace WitchCityRope.Web.Services
{
    /// <summary>
    /// Client-side validation service for event forms with complex business rules
    /// </summary>
    public class EventValidationService
    {
        /// <summary>
        /// Validates the event edit view model with business rules
        /// </summary>
        public ValidationResult ValidateEvent(EventEditViewModel model)
        {
            var result = new ValidationResult();

            // Basic required field validation (handled by attributes, but double-check)
            ValidateBasicFields(model, result);
            
            // Date validations
            ValidateDates(model, result);
            
            // Pricing validations
            ValidatePricing(model, result);
            
            // Event type specific validations
            ValidateEventTypeRequirements(model, result);
            
            // Registration window validation
            ValidateRegistrationWindow(model, result);
            
            // Business rule validations
            ValidateBusinessRules(model, result);

            return result;
        }

        private void ValidateBasicFields(EventEditViewModel model, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
                result.AddError(nameof(model.Title), EventValidationConstants.TITLE_REQUIRED);

            if (string.IsNullOrWhiteSpace(model.Description))
                result.AddError(nameof(model.Description), EventValidationConstants.DESCRIPTION_REQUIRED);

            if (string.IsNullOrWhiteSpace(model.Location))
                result.AddError(nameof(model.Location), EventValidationConstants.LOCATION_REQUIRED);

            if (model.Capacity < EventValidationConstants.CAPACITY_MIN || model.Capacity > EventValidationConstants.CAPACITY_MAX)
                result.AddError(nameof(model.Capacity), EventValidationConstants.CAPACITY_RANGE_MESSAGE);
        }

        private void ValidateDates(EventEditViewModel model, ValidationResult result)
        {
            // Check if start date is in the future (for new events)
            if (model.IsNewEvent && model.StartDate <= DateTime.Now)
                result.AddError(nameof(model.StartDate), EventValidationConstants.START_DATE_FUTURE);

            // Check if end date is after start date
            if (model.EndDate <= model.StartDate)
                result.AddError(nameof(model.EndDate), EventValidationConstants.DATE_RANGE_INVALID);

            // Check event duration
            var duration = model.EndDate - model.StartDate;
            if (duration.TotalMinutes < EventValidationConstants.MIN_EVENT_DURATION_MINUTES)
                result.AddError(nameof(model.EndDate), EventValidationConstants.EVENT_DURATION_TOO_SHORT);

            if (duration.TotalHours > EventValidationConstants.MAX_EVENT_DURATION_HOURS)
                result.AddError(nameof(model.EndDate), EventValidationConstants.EVENT_DURATION_TOO_LONG);
        }

        private void ValidatePricing(EventEditViewModel model, ValidationResult result)
        {
            switch (model.PricingType)
            {
                case PricingType.Fixed:
                    if (!model.IndividualPrice.HasValue || model.IndividualPrice <= 0)
                        result.AddError(nameof(model.IndividualPrice), EventValidationConstants.INDIVIDUAL_PRICE_REQUIRED_FOR_FIXED);
                    break;

                case PricingType.SlidingScale:
                    if (!model.MinimumPrice.HasValue || model.MinimumPrice <= 0)
                        result.AddError(nameof(model.MinimumPrice), EventValidationConstants.MINIMUM_PRICE_REQUIRED_FOR_SLIDING);

                    if (!model.SuggestedPrice.HasValue || model.SuggestedPrice <= 0)
                        result.AddError(nameof(model.SuggestedPrice), EventValidationConstants.SUGGESTED_PRICE_REQUIRED_FOR_SLIDING);

                    if (!model.MaximumPrice.HasValue || model.MaximumPrice <= 0)
                        result.AddError(nameof(model.MaximumPrice), EventValidationConstants.MAXIMUM_PRICE_REQUIRED_FOR_SLIDING);

                    // Validate sliding scale order
                    if (model.MinimumPrice.HasValue && model.SuggestedPrice.HasValue && model.MaximumPrice.HasValue)
                    {
                        if (model.MinimumPrice > model.SuggestedPrice || model.SuggestedPrice > model.MaximumPrice)
                            result.AddError(nameof(model.MaximumPrice), EventValidationConstants.SLIDING_SCALE_ORDER_INVALID);
                    }
                    break;

                case PricingType.Free:
                    // No pricing validation needed for free events
                    break;
            }

            // Validate couples pricing relationship
            if (model.IndividualPrice.HasValue && model.CouplesPrice.HasValue)
            {
                if (model.CouplesPrice > model.IndividualPrice * 2.5m) // Allow some flexibility
                    result.AddWarning(nameof(model.CouplesPrice), EventValidationConstants.COUPLES_PRICE_INVALID);
            }
        }

        private void ValidateEventTypeRequirements(EventEditViewModel model, ValidationResult result)
        {
            switch (model.EventType)
            {
                case Core.Enums.EventType.Workshop:
                    if (!model.InstructorId.HasValue || model.InstructorId == Guid.Empty)
                        result.AddError(nameof(model.InstructorId), EventValidationConstants.INSTRUCTOR_REQUIRED_FOR_WORKSHOP);
                    break;

                case Core.Enums.EventType.Social:
                    // Social events might have different requirements
                    break;
            }
        }

        private void ValidateRegistrationWindow(EventEditViewModel model, ValidationResult result)
        {
            if (model.RegistrationClosesAt.HasValue)
            {
                if (model.RegistrationClosesAt >= model.StartDate)
                    result.AddError(nameof(model.RegistrationClosesAt), EventValidationConstants.REGISTRATION_WINDOW_INVALID);
            }

            if (model.RegistrationOpensAt.HasValue && model.RegistrationClosesAt.HasValue)
            {
                if (model.RegistrationOpensAt >= model.RegistrationClosesAt)
                    result.AddError(nameof(model.RegistrationClosesAt), "Registration must close after it opens");
            }
        }

        private void ValidateBusinessRules(EventEditViewModel model, ValidationResult result)
        {
            // Add any additional business rules here
            
            // Example: Weekend events might have different capacity requirements
            if (model.StartDate.DayOfWeek == DayOfWeek.Saturday || model.StartDate.DayOfWeek == DayOfWeek.Sunday)
            {
                if (model.Capacity > 500) // Example business rule
                    result.AddWarning(nameof(model.Capacity), "Weekend events with high capacity may require additional safety measures");
            }

            // Example: Evening events (after 9 PM) might need special consideration
            if (model.StartDate.Hour >= 21)
            {
                result.AddInfo(nameof(model.StartDate), "Evening events may have different attendance patterns");
            }
        }

        /// <summary>
        /// Validates specific field changes for real-time validation
        /// </summary>
        public List<string> ValidateField(EventEditViewModel model, string fieldName)
        {
            var errors = new List<string>();

            switch (fieldName.ToLower())
            {
                case "startdate":
                    if (model.IsNewEvent && model.StartDate <= DateTime.Now)
                        errors.Add(EventValidationConstants.START_DATE_FUTURE);
                    break;

                case "enddate":
                    if (model.EndDate <= model.StartDate)
                        errors.Add(EventValidationConstants.DATE_RANGE_INVALID);
                    
                    var duration = model.EndDate - model.StartDate;
                    if (duration.TotalMinutes < EventValidationConstants.MIN_EVENT_DURATION_MINUTES)
                        errors.Add(EventValidationConstants.EVENT_DURATION_TOO_SHORT);
                    break;

                case "individualprice":
                    if (model.PricingType == PricingType.Fixed && (!model.IndividualPrice.HasValue || model.IndividualPrice <= 0))
                        errors.Add(EventValidationConstants.INDIVIDUAL_PRICE_REQUIRED_FOR_FIXED);
                    break;

                case "instructorid":
                    if (model.EventType == Core.Enums.EventType.Workshop && (!model.InstructorId.HasValue || model.InstructorId == Guid.Empty))
                        errors.Add(EventValidationConstants.INSTRUCTOR_REQUIRED_FOR_WORKSHOP);
                    break;
            }

            return errors;
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public Dictionary<string, List<string>> Errors { get; } = new();
        public Dictionary<string, List<string>> Warnings { get; } = new();
        public Dictionary<string, List<string>> Info { get; } = new();

        public bool IsValid => !Errors.Any();
        public bool HasWarnings => Warnings.Any();
        public bool HasInfo => Info.Any();

        public void AddError(string field, string message)
        {
            if (!Errors.ContainsKey(field))
                Errors[field] = new List<string>();
            Errors[field].Add(message);
        }

        public void AddWarning(string field, string message)
        {
            if (!Warnings.ContainsKey(field))
                Warnings[field] = new List<string>();
            Warnings[field].Add(message);
        }

        public void AddInfo(string field, string message)
        {
            if (!Info.ContainsKey(field))
                Info[field] = new List<string>();
            Info[field].Add(message);
        }

        public List<string> GetErrors(string field)
        {
            return Errors.ContainsKey(field) ? Errors[field] : new List<string>();
        }

        public List<string> GetWarnings(string field)
        {
            return Warnings.ContainsKey(field) ? Warnings[field] : new List<string>();
        }

        public List<string> GetInfo(string field)
        {
            return Info.ContainsKey(field) ? Info[field] : new List<string>();
        }

        public List<string> GetAllErrors()
        {
            return Errors.Values.SelectMany(e => e).ToList();
        }

        public List<string> GetAllWarnings()
        {
            return Warnings.Values.SelectMany(w => w).ToList();
        }
    }
}