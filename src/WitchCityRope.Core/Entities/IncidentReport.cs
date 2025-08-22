using System;
using System.Collections.Generic;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Exceptions;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents an incident report for safety and community management
    /// Business Rules:
    /// - Reports can be anonymous
    /// - Reports are confidential and encrypted
    /// - Multiple moderators review each report
    /// - Follow-up actions are tracked
    /// </summary>
    public class IncidentReport
    {
        private readonly List<IncidentReview> _reviews = new();
        private readonly List<IncidentAction> _actions = new();

        // Private constructor for EF Core
        private IncidentReport() 
        { 
            Description = null!;
            ResolutionNotes = null!;
            ReferenceNumber = null!;
            Location = null!;
            PreferredContactMethod = null!;
            Reporter = null!;
            Event = null!;
            AssignedTo = null!;
        }

        public IncidentReport(
            User reporter,
            Event relatedEvent,
            string description,
            IncidentSeverity severity,
            IncidentType incidentType,
            string location,
            DateTime incidentDate,
            bool isAnonymous = false,
            bool requestFollowUp = false,
            string? preferredContactMethod = null)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Incident description is required", nameof(description));

            Id = Guid.NewGuid();
            
            if (!isAnonymous)
            {
                if (reporter == null)
                    throw new ArgumentNullException(nameof(reporter));
                
                ReporterId = reporter.Id;
                Reporter = reporter;
            }
            
            EventId = relatedEvent?.Id;
            Event = relatedEvent;
            Description = description;
            Severity = severity;
            IncidentType = incidentType;
            Location = location ?? string.Empty;
            IncidentDate = incidentDate;
            IsAnonymous = isAnonymous;
            RequestFollowUp = requestFollowUp;
            PreferredContactMethod = preferredContactMethod;
            Status = IncidentStatus.Submitted;
            ReportedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            ReferenceNumber = GenerateReferenceNumber(incidentType);
        }

        public Guid Id { get; private set; }
        
        public Guid? ReporterId { get; private set; }
        
        public User Reporter { get; private set; }
        
        public Guid? EventId { get; private set; }
        
        public Event Event { get; private set; }
        
        /// <summary>
        /// Encrypted description of the incident
        /// </summary>
        public string Description { get; private set; }
        
        public IncidentSeverity Severity { get; private set; }
        
        public bool IsAnonymous { get; private set; }
        
        public IncidentStatus Status { get; private set; }
        
        public DateTime ReportedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public DateTime? ResolvedAt { get; private set; }
        
        public string ResolutionNotes { get; private set; }
        
        /// <summary>
        /// Reference number for tracking the incident
        /// </summary>
        public string ReferenceNumber { get; private set; }
        
        /// <summary>
        /// Type of incident
        /// </summary>
        public IncidentType IncidentType { get; private set; }
        
        /// <summary>
        /// Location where the incident occurred
        /// </summary>
        public string Location { get; private set; }
        
        /// <summary>
        /// Date when the incident occurred
        /// </summary>
        public DateTime IncidentDate { get; private set; }
        
        /// <summary>
        /// Indicates if follow-up was requested
        /// </summary>
        public bool RequestFollowUp { get; private set; }
        
        /// <summary>
        /// Preferred contact method for follow-up
        /// </summary>
        public string PreferredContactMethod { get; private set; }
        
        /// <summary>
        /// User assigned to handle the incident
        /// </summary>
        public Guid? AssignedToId { get; private set; }
        
        public User AssignedTo { get; private set; }
        
        public IReadOnlyCollection<IncidentReview> Reviews => _reviews.AsReadOnly();
        
        public IReadOnlyCollection<IncidentAction> Actions => _actions.AsReadOnly();

        /// <summary>
        /// Acknowledges receipt of the incident report
        /// </summary>
        public void Acknowledge()
        {
            if (Status != IncidentStatus.Submitted)
                throw new DomainException("Only submitted reports can be acknowledged");

            Status = IncidentStatus.Acknowledged;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Begins investigation of the incident
        /// </summary>
        public void StartInvestigation()
        {
            if (Status != IncidentStatus.Acknowledged)
                throw new DomainException("Report must be acknowledged before investigation");

            Status = IncidentStatus.UnderInvestigation;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds a review from a moderator
        /// </summary>
        public void AddReview(IncidentReview review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (Status != IncidentStatus.UnderInvestigation)
                throw new DomainException("Reviews can only be added during investigation");

            _reviews.Add(review);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records an action taken in response to the incident
        /// </summary>
        public void RecordAction(IncidentAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (Status != IncidentStatus.UnderInvestigation && Status != IncidentStatus.ActionTaken)
                throw new DomainException("Actions can only be recorded during or after investigation");

            _actions.Add(action);
            Status = IncidentStatus.ActionTaken;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Resolves the incident with notes
        /// </summary>
        public void Resolve(string resolutionNotes)
        {
            if (Status == IncidentStatus.Resolved)
                throw new DomainException("Incident is already resolved");

            if (string.IsNullOrWhiteSpace(resolutionNotes))
                throw new ArgumentException("Resolution notes are required", nameof(resolutionNotes));

            Status = IncidentStatus.Resolved;
            ResolutionNotes = resolutionNotes;
            ResolvedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the severity of the incident
        /// </summary>
        public void UpdateSeverity(IncidentSeverity newSeverity, string reason)
        {
            if (Status == IncidentStatus.Resolved)
                throw new DomainException("Cannot update severity of resolved incidents");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason for severity change is required", nameof(reason));

            Severity = newSeverity;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Assigns the incident to a user for handling
        /// </summary>
        public void AssignTo(User assignee)
        {
            if (assignee == null)
                throw new ArgumentNullException(nameof(assignee));
                
            AssignedToId = assignee.Id;
            AssignedTo = assignee;
            UpdatedAt = DateTime.UtcNow;
        }
        
        private string GenerateReferenceNumber(IncidentType type)
        {
            var typePrefix = type switch
            {
                IncidentType.SafetyViolation => "SAF",
                IncidentType.ConsentViolation => "CON",
                IncidentType.EquipmentFailure => "EQP",
                IncidentType.Injury => "INJ",
                IncidentType.HarassmentOrBullying => "HAR",
                IncidentType.PolicyViolation => "POL",
                _ => "OTH"
            };

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{typePrefix}-{timestamp}-{random}";
        }
    }
}
