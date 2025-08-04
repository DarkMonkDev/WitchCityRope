using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;

namespace WitchCityRope.Core.Repositories
{
    /// <summary>
    /// Repository interface for managing user notes.
    /// </summary>
    public interface IUserNoteRepository
    {
        /// <summary>
        /// Gets all notes for a specific user.
        /// </summary>
        /// <param name="userId">The user ID to get notes for.</param>
        /// <param name="includeDeleted">Whether to include soft-deleted notes.</param>
        /// <returns>List of user notes.</returns>
        Task<IEnumerable<UserNote>> GetUserNotesAsync(Guid userId, bool includeDeleted = false);

        /// <summary>
        /// Gets a specific note by ID.
        /// </summary>
        /// <param name="noteId">The note ID.</param>
        /// <returns>The note if found, null otherwise.</returns>
        Task<UserNote?> GetNoteByIdAsync(Guid noteId);

        /// <summary>
        /// Adds a new note.
        /// </summary>
        /// <param name="note">The note to add.</param>
        /// <returns>The added note.</returns>
        Task<UserNote> AddNoteAsync(UserNote note);

        /// <summary>
        /// Updates an existing note.
        /// </summary>
        /// <param name="note">The note to update.</param>
        Task UpdateNoteAsync(UserNote note);

        /// <summary>
        /// Soft deletes a note.
        /// </summary>
        /// <param name="noteId">The note ID to delete.</param>
        /// <param name="deletedById">The user ID performing the deletion.</param>
        Task DeleteNoteAsync(Guid noteId, Guid deletedById);

        /// <summary>
        /// Gets the count of notes for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="noteType">Optional filter by note type.</param>
        /// <returns>The count of notes.</returns>
        Task<int> GetNoteCountAsync(Guid userId, NoteType? noteType = null);

        /// <summary>
        /// Gets notes by type across all users (for admin reports).
        /// </summary>
        /// <param name="noteType">The type of notes to retrieve.</param>
        /// <param name="limit">Maximum number of notes to return.</param>
        /// <returns>List of notes of the specified type.</returns>
        Task<IEnumerable<UserNote>> GetNotesByTypeAsync(NoteType noteType, int limit = 100);
    }
}