using System;

namespace WitchCityRope.Core.Entities;

public class ContentPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool HasBeenEdited { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    
    public void UpdateContent(string content, string title, string userId, string? metaDescription = null, string? metaKeywords = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));
            
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        Content = content;
        Title = title;
        HasBeenEdited = true;
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = userId;
        MetaDescription = metaDescription;
        MetaKeywords = metaKeywords;
    }
    
    public static ContentPage CreateDefault(string slug, string title)
    {
        return new ContentPage
        {
            Slug = slug,
            Title = title,
            Content = "Default Text Place Holder",
            HasBeenEdited = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastModifiedBy = "system"
        };
    }
}