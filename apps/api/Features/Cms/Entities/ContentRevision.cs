using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Cms.Entities
{
    [Table("ContentRevisions", Schema = "cms")]
    public class ContentRevision
    {
        // Primary key
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        // Foreign key to parent page
        [Required]
        [Column("ContentPageId")]
        public int ContentPageId { get; set; }

        // Content snapshot
        [Required]
        [Column("Content")]
        public string Content { get; set; } = string.Empty;

        [Required]
        [Column("Title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Audit fields
        [Required]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("CreatedBy")]
        public Guid CreatedBy { get; set; }

        // Optional change description
        [Column("ChangeDescription")]
        [MaxLength(500)]
        public string? ChangeDescription { get; set; }

        // Navigation properties
        public ContentPage? ContentPage { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
    }
}
