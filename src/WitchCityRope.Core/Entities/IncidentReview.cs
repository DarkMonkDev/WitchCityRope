using System;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a moderator's review of an incident report
    /// </summary>
    public class IncidentReview
    {
        // Private constructor for EF Core
        private IncidentReview() { }

        public IncidentReview(User reviewer, string findings, IncidentSeverity recommendedSeverity)
        {
            if (reviewer == null)
                throw new ArgumentNullException(nameof(reviewer));

            if (string.IsNullOrWhiteSpace(findings))
                throw new ArgumentException("Review findings are required", nameof(findings));

            Id = Guid.NewGuid();
            ReviewerId = reviewer.Id;
            Reviewer = reviewer;
            Findings = findings;
            RecommendedSeverity = recommendedSeverity;
            ReviewedAt = DateTime.UtcNow;
            RecommendAction = false; // Default to false, can be set separately if needed
        }

        public Guid Id { get; private set; }
        
        public Guid ReviewerId { get; private set; }
        
        public User Reviewer { get; private set; }
        
        /// <summary>
        /// Encrypted findings from the reviewer
        /// </summary>
        public string Findings { get; private set; }
        
        /// <summary>
        /// The reviewer's assessment of the incident severity
        /// </summary>
        public IncidentSeverity RecommendedSeverity { get; private set; }
        
        /// <summary>
        /// Whether the reviewer recommends action be taken
        /// </summary>
        public bool RecommendAction { get; private set; }
        
        public DateTime ReviewedAt { get; private set; }
        
        // Method to set the recommendation for action
        public void SetRecommendAction(bool recommendAction)
        {
            RecommendAction = recommendAction;
        }
    }
}