using System;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a general-purpose note about a user in the system.
    /// </summary>
    public class UserNote : BaseEntity
    {
        public Guid UserId { get; private set; }
        public NoteType NoteType { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public Guid CreatedById { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        public Guid? DeletedById { get; private set; }

        // Navigation properties should be defined in Infrastructure layer, not Core
        // Core layer should only have ID references

        // Private constructor for EF Core
        private UserNote() { }

        // Factory method for creating new notes
        public static UserNote Create(
            Guid userId,
            NoteType noteType,
            string content,
            Guid createdById)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (createdById == Guid.Empty)
                throw new ArgumentException("Created by ID cannot be empty", nameof(createdById));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Note content cannot be empty", nameof(content));
            if (content.Length > 5000)
                throw new ArgumentException("Note content cannot exceed 5000 characters", nameof(content));

            return new UserNote
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NoteType = noteType,
                Content = content.Trim(),
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        /// <summary>
        /// Updates the content of the note.
        /// </summary>
        public void Update(string newContent)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot update a deleted note");
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Note content cannot be empty", nameof(newContent));
            if (newContent.Length > 5000)
                throw new ArgumentException("Note content cannot exceed 5000 characters", nameof(newContent));

            Content = newContent.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Soft deletes the note.
        /// </summary>
        public void Delete(Guid deletedById)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Note is already deleted");
            if (deletedById == Guid.Empty)
                throw new ArgumentException("Deleted by ID cannot be empty", nameof(deletedById));

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedById = deletedById;
        }

        /// <summary>
        /// Restores a soft-deleted note.
        /// </summary>
        public void Restore()
        {
            if (!IsDeleted)
                throw new InvalidOperationException("Note is not deleted");

            IsDeleted = false;
            DeletedAt = null;
            DeletedById = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Types of notes that can be created for users.
    /// </summary>
    public enum NoteType
    {
        General = 0,
        Vetting = 1,
        Incident = 2,
        Event = 3,
        Administrative = 4,
        Safety = 5,
        Community = 6
    }
}