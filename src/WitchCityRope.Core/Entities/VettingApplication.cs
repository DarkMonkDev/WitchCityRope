using System;
using System.Collections.Generic;
using System.Linq;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a vetting application for new community members
    /// Business Rules:
    /// - Applications require references from existing members
    /// - Multiple reviewers assess each application
    /// - Applicants must be at least 21 years old (validated through User entity)
    /// </summary>
    public class VettingApplication
    {
        private readonly List<string> _references = new();
        private readonly List<VettingReview> _reviews = new();

        // Private constructor for EF Core
        private VettingApplication() { }

        public VettingApplication(
            IUser applicant,
            string experienceLevel,
            string interests,
            string safetyKnowledge,
            IEnumerable<string> references)
        {
            if (applicant == null)
                throw new ArgumentNullException(nameof(applicant));

            if (string.IsNullOrWhiteSpace(experienceLevel))
                throw new ArgumentException("Experience level is required", nameof(experienceLevel));

            if (string.IsNullOrWhiteSpace(interests))
                throw new ArgumentException("Interests are required", nameof(interests));

            if (string.IsNullOrWhiteSpace(safetyKnowledge))
                throw new ArgumentException("Safety knowledge is required", nameof(safetyKnowledge));

            ValidateReferences(references);

            Id = Guid.NewGuid();
            ApplicantId = applicant.Id;
            Applicant = applicant;
            ExperienceLevel = experienceLevel;
            Interests = interests;
            SafetyKnowledge = safetyKnowledge;
            Status = VettingStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            _references.AddRange(references);
        }
        
        /// <summary>
        /// Alternative constructor for creating from command/API usage
        /// </summary>
        public VettingApplication(
            IUser applicant,
            string experienceLevel,
            string experienceDescription,
            string interests,
            string safetyKnowledge,
            string consentUnderstanding,
            string whyJoin,
            IEnumerable<string> references)
        {
            if (applicant == null)
                throw new ArgumentNullException(nameof(applicant));

            if (string.IsNullOrWhiteSpace(experienceLevel))
                throw new ArgumentException("Experience level is required", nameof(experienceLevel));

            if (string.IsNullOrWhiteSpace(interests))
                throw new ArgumentException("Interests are required", nameof(interests));

            if (string.IsNullOrWhiteSpace(safetyKnowledge))
                throw new ArgumentException("Safety knowledge is required", nameof(safetyKnowledge));

            ValidateReferences(references);

            Id = Guid.NewGuid();
            ApplicantId = applicant.Id;
            Applicant = applicant;
            ExperienceLevel = experienceLevel;
            ExperienceDescription = experienceDescription;
            Interests = interests;
            SafetyKnowledge = safetyKnowledge;
            ConsentUnderstanding = consentUnderstanding;
            WhyJoin = whyJoin;
            Status = VettingStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            _references.AddRange(references);
        }

        public Guid Id { get; private set; }
        
        public Guid ApplicantId { get; private set; }
        
        public IUser Applicant { get; private set; }
        
        public string ExperienceLevel { get; private set; }
        
        public string Interests { get; private set; }
        
        public string SafetyKnowledge { get; private set; }
        
        public string ExperienceDescription { get; private set; }
        
        public string ConsentUnderstanding { get; private set; }
        
        public string WhyJoin { get; private set; }
        
        public VettingStatus Status { get; private set; }
        
        public DateTime SubmittedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        public DateTime? ReviewedAt { get; private set; }
        
        public string? DecisionNotes { get; private set; }
        
        public IReadOnlyCollection<string> References => _references.AsReadOnly();
        
        public IReadOnlyCollection<VettingReview> Reviews => _reviews.AsReadOnly();

        /// <summary>
        /// Adds a review from a vetting team member
        /// </summary>
        public void AddReview(VettingReview review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (Status != VettingStatus.UnderReview)
                throw new DomainException("Application must be under review to add reviews");

            _reviews.Add(review);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Moves application to under review status
        /// </summary>
        public void StartReview()
        {
            if (Status != VettingStatus.Submitted)
                throw new DomainException("Only submitted applications can be moved to review");

            Status = VettingStatus.UnderReview;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Approves the vetting application
        /// </summary>
        public void Approve(string? notes = null)
        {
            if (Status != VettingStatus.UnderReview)
                throw new DomainException("Only applications under review can be approved");

            if (!HasSufficientReviews())
                throw new DomainException("Application requires at least 2 reviews before approval");

            Status = VettingStatus.Approved;
            DecisionNotes = notes;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Rejects the vetting application
        /// </summary>
        public void Reject(string reason)
        {
            if (Status != VettingStatus.UnderReview)
                throw new DomainException("Only applications under review can be rejected");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Rejection reason is required", nameof(reason));

            Status = VettingStatus.Rejected;
            DecisionNotes = reason;
            ReviewedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Requests additional information from the applicant
        /// </summary>
        public void RequestMoreInfo(string infoRequest)
        {
            if (Status != VettingStatus.UnderReview)
                throw new DomainException("Only applications under review can request more info");

            if (string.IsNullOrWhiteSpace(infoRequest))
                throw new ArgumentException("Information request details are required", nameof(infoRequest));

            Status = VettingStatus.MoreInfoRequested;
            DecisionNotes = infoRequest;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates application with additional information
        /// </summary>
        public void ProvideAdditionalInfo(string additionalInfo)
        {
            if (Status != VettingStatus.MoreInfoRequested)
                throw new DomainException("Additional info can only be provided when requested");

            if (string.IsNullOrWhiteSpace(additionalInfo))
                throw new ArgumentException("Additional information is required", nameof(additionalInfo));

            // Append to existing fields or store separately as needed
            SafetyKnowledge += $"\n\nAdditional Information: {additionalInfo}";
            Status = VettingStatus.UnderReview;
            UpdatedAt = DateTime.UtcNow;
        }

        private void ValidateReferences(IEnumerable<string> references)
        {
            if (references == null || !references.Any())
                throw new DomainException("At least one reference is required");

            foreach (var reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference))
                    throw new DomainException("Reference cannot be empty");
            }
        }

        private bool HasSufficientReviews()
        {
            return _reviews.Count >= 2;
        }
    }

    /// <summary>
    /// Represents a review of a vetting application
    /// </summary>
    public class VettingReview
    {
        // Private constructor for EF Core
        private VettingReview() 
        { 
            Reviewer = null!;
            Notes = null!;
        }

        public VettingReview(IUser reviewer, bool recommendation, string notes)
        {
            if (reviewer == null)
                throw new ArgumentNullException(nameof(reviewer));

            if (string.IsNullOrWhiteSpace(notes))
                throw new ArgumentException("Review notes are required", nameof(notes));

            Id = Guid.NewGuid();
            ReviewerId = reviewer.Id;
            Reviewer = reviewer;
            Recommendation = recommendation;
            Notes = notes;
            ReviewedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        public Guid ReviewerId { get; private set; }
        
        public IUser Reviewer { get; private set; }
        
        public bool Recommendation { get; private set; }
        
        public string Notes { get; private set; }
        
        public DateTime ReviewedAt { get; private set; }
    }
}