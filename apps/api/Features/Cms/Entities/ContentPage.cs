using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Cms.Entities
{
    [Table("ContentPages", Schema = "cms")]
    public class ContentPage
    {
        // Primary key
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        // Page identification
        [Required]
        [Column("Slug")]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        // Content fields
        [Required]
        [Column("Title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("Content")]
        public string Content { get; set; } = string.Empty;

        // Audit fields
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }

        [Required]
        [Column("CreatedBy")]
        public Guid CreatedBy { get; set; }

        [Required]
        [Column("LastModifiedBy")]
        public Guid LastModifiedBy { get; set; }

        // Publishing state
        [Required]
        [Column("IsPublished")]
        public bool IsPublished { get; set; } = true;

        // Navigation properties
        public ApplicationUser? CreatedByUser { get; set; }
        public ApplicationUser? LastModifiedByUser { get; set; }
        public ICollection<ContentRevision> Revisions { get; set; } = new List<ContentRevision>();

        // Domain methods
        public void UpdateContent(string content, string title, Guid userId, string? changeDescription = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));

            // Create revision BEFORE updating current content
            var revision = new ContentRevision
            {
                ContentPageId = Id,
                Content = Content,      // Old content
                Title = Title,          // Old title
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ChangeDescription = changeDescription ?? "Content updated via web interface"
            };

            Revisions.Add(revision);

            // Update current content
            Content = content;
            Title = title;
            UpdatedAt = DateTime.UtcNow;
            LastModifiedBy = userId;
        }

        public bool CanBeEditedBy(string userId, IEnumerable<string> userRoles)
        {
            // Admin-only editing for MVP
            return userRoles.Contains("Administrator");
        }
    }
}
