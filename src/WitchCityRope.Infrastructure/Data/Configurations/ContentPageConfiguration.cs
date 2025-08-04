using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Infrastructure.Data.Configurations;

public class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("ContentPages", "cms");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.HasIndex(x => x.Slug)
            .IsUnique();
            
        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("text");
            
        builder.Property(x => x.HasBeenEdited)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedBy)
            .IsRequired()
            .HasMaxLength(450);
            
        builder.Property(x => x.MetaDescription)
            .HasMaxLength(500);
            
        builder.Property(x => x.MetaKeywords)
            .HasMaxLength(500);
            
        // Foreign key constraint (without navigation property)
        builder.HasIndex(x => x.LastModifiedBy);
            
        // Seed data for initial pages
        var seedDate = new DateTime(2025, 7, 7, 12, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new ContentPage
            {
                Id = 1,
                Slug = "resources",
                Title = "Resources",
                Content = "Default Text Place Holder",
                HasBeenEdited = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                LastModifiedBy = "system"
            },
            new ContentPage
            {
                Id = 2,
                Slug = "private-lessons",
                Title = "Private Lessons",
                Content = "Default Text Place Holder",
                HasBeenEdited = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                LastModifiedBy = "system"
            },
            new ContentPage
            {
                Id = 3,
                Slug = "contact",
                Title = "Contact Us",
                Content = "Default Text Place Holder",
                HasBeenEdited = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                LastModifiedBy = "system"
            }
        );
    }
}