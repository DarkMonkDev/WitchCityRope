using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Repositories;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for managing user notes.
    /// </summary>
    public class UserNoteRepository : IUserNoteRepository
    {
        private readonly WitchCityRopeIdentityDbContext _context;

        public UserNoteRepository(WitchCityRopeIdentityDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<UserNote>> GetUserNotesAsync(Guid userId, bool includeDeleted = false)
        {
            var query = _context.UserNotes
                .Where(n => n.UserId == userId);

            if (!includeDeleted)
            {
                query = query.Where(n => !n.IsDeleted);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserNote?> GetNoteByIdAsync(Guid noteId)
        {
            return await _context.UserNotes
                .FirstOrDefaultAsync(n => n.Id == noteId);
        }

        public async Task<UserNote> AddNoteAsync(UserNote note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            _context.UserNotes.Add(note);
            await _context.SaveChangesAsync();
            
            // Reload with includes
            return await GetNoteByIdAsync(note.Id) ?? note;
        }

        public async Task UpdateNoteAsync(UserNote note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            _context.UserNotes.Update(note);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNoteAsync(Guid noteId, Guid deletedById)
        {
            var note = await GetNoteByIdAsync(noteId);
            if (note == null)
                throw new InvalidOperationException($"Note with ID {noteId} not found.");

            note.Delete(deletedById);
            await UpdateNoteAsync(note);
        }

        public async Task<int> GetNoteCountAsync(Guid userId, NoteType? noteType = null)
        {
            var query = _context.UserNotes
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (noteType.HasValue)
            {
                query = query.Where(n => n.NoteType == noteType.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<UserNote>> GetNotesByTypeAsync(NoteType noteType, int limit = 100)
        {
            return await _context.UserNotes
                .Where(n => n.NoteType == noteType && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}