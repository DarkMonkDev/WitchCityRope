using System;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents an action taken in response to an incident
    /// </summary>
    public class IncidentAction
    {
        // Private constructor for EF Core
        private IncidentAction() 
        { 
            ActionType = null!;
            Description = null!;
            PerformedBy = null!;
        }

        public IncidentAction(string actionType, string description, IUser performedBy)
        {
            if (string.IsNullOrWhiteSpace(actionType))
                throw new ArgumentException("Action type is required", nameof(actionType));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Action description is required", nameof(description));

            if (performedBy == null)
                throw new ArgumentNullException(nameof(performedBy));

            Id = Guid.NewGuid();
            ActionType = actionType;
            Description = description;
            PerformedById = performedBy.Id;
            PerformedBy = performedBy;
            PerformedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public string ActionType { get; private set; }
        
        public string Description { get; private set; }
        
        public Guid PerformedById { get; private set; }
        
        public IUser PerformedBy { get; private set; }
        
        public DateTime PerformedAt { get; private set; }
    }
}