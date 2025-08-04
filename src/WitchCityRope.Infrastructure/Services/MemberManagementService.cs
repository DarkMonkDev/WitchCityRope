using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Repositories;
using WitchCityRope.Core.Services;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.Enums;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Service for managing member-related operations.
    /// </summary>
    public interface IMemberManagementService
    {
        Task<PagedMemberResult> SearchMembersAsync(MemberSearchRequest request);
        Task<MemberDetailViewModel> GetMemberDetailsAsync(Guid memberId);
        Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto, Guid updatedById);
        Task<MemberStatsDto> GetMemberStatsAsync();
        Task<UserNoteDto> AddMemberNoteAsync(Guid memberId, CreateUserNoteDto dto, Guid createdById);
        Task UpdateMemberNoteAsync(Guid noteId, UpdateUserNoteDto dto, Guid updatedById);
        Task DeleteMemberNoteAsync(Guid noteId, Guid deletedById);
    }

    public class MemberManagementService : IMemberManagementService
    {
        private readonly IMemberRepository _memberRepository;
        private readonly IUserNoteRepository _noteRepository;
        private readonly UserManager<WitchCityRopeUser> _userManager;
        private readonly ILogger<MemberManagementService> _logger;
        private readonly IMemoryCache _cache;
        private const string STATS_CACHE_KEY = "member_stats";
        private const int STATS_CACHE_MINUTES = 5;

        public MemberManagementService(
            IMemberRepository memberRepository,
            IUserNoteRepository noteRepository,
            UserManager<WitchCityRopeUser> userManager,
            ILogger<MemberManagementService> logger,
            IMemoryCache cache)
        {
            _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
            _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<PagedMemberResult> SearchMembersAsync(MemberSearchRequest request)
        {
            try
            {
                _logger.LogInformation("Searching members with criteria: {@Request}", request);
                return await _memberRepository.GetMembersAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching members");
                throw;
            }
        }

        public async Task<MemberDetailViewModel> GetMemberDetailsAsync(Guid memberId)
        {
            try
            {
                _logger.LogInformation("Getting details for member {MemberId}", memberId);

                var member = await _memberRepository.GetMemberDetailAsync(memberId);
                if (member == null)
                {
                    _logger.LogWarning("Member {MemberId} not found", memberId);
                    throw new InvalidOperationException($"Member {memberId} not found");
                }

                // Fetch related data in parallel
                var notesTask = GetMemberNotesWithUserInfoAsync(memberId);
                var eventsTask = _memberRepository.GetMemberEventHistoryAsync(memberId);
                var incidentsTask = _memberRepository.GetMemberIncidentsAsync(memberId);

                await Task.WhenAll(notesTask, eventsTask, incidentsTask);

                return new MemberDetailViewModel
                {
                    Member = member,
                    Notes = await notesTask,
                    EventHistory = await eventsTask,
                    Incidents = await incidentsTask
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member details for {MemberId}", memberId);
                throw;
            }
        }

        public async Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto, Guid updatedById)
        {
            try
            {
                _logger.LogInformation("User {UpdatedById} updating member {MemberId}", updatedById, memberId);

                // Validate scene name if changing
                if (!string.IsNullOrWhiteSpace(dto.SceneName))
                {
                    var isTaken = await _memberRepository.IsSceneNameTakenAsync(dto.SceneName, memberId);
                    if (isTaken)
                    {
                        throw new InvalidOperationException($"Scene name '{dto.SceneName}' is already taken");
                    }
                }

                await _memberRepository.UpdateMemberAsync(memberId, dto);
                
                // Invalidate stats cache
                _cache.Remove(STATS_CACHE_KEY);

                _logger.LogInformation("Member {MemberId} updated successfully", memberId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member {MemberId}", memberId);
                throw;
            }
        }

        public async Task<MemberStatsDto> GetMemberStatsAsync()
        {
            try
            {
                // Try to get from cache first
                if (_cache.TryGetValue(STATS_CACHE_KEY, out MemberStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var stats = await _memberRepository.GetMemberStatsAsync();

                // Cache the results
                _cache.Set(STATS_CACHE_KEY, stats, TimeSpan.FromMinutes(STATS_CACHE_MINUTES));

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member stats");
                throw;
            }
        }

        public async Task<UserNoteDto> AddMemberNoteAsync(Guid memberId, CreateUserNoteDto dto, Guid createdById)
        {
            try
            {
                _logger.LogInformation("User {CreatedById} adding note for member {MemberId}", createdById, memberId);

                // Parse note type
                if (!Enum.TryParse<NoteType>(dto.NoteType, out var noteType))
                {
                    throw new ArgumentException($"Invalid note type: {dto.NoteType}");
                }

                var note = UserNote.Create(memberId, noteType, dto.Content, createdById);
                var savedNote = await _noteRepository.AddNoteAsync(note);

                // Get creator info
                var creator = await _userManager.FindByIdAsync(createdById.ToString());
                
                return new UserNoteDto
                {
                    Id = savedNote.Id,
                    UserId = savedNote.UserId,
                    NoteType = savedNote.NoteType.ToString(),
                    Content = savedNote.Content,
                    CreatedByName = creator?.SceneName ?? "Unknown",
                    CreatedAt = savedNote.CreatedAt,
                    UpdatedAt = savedNote.UpdatedAt,
                    CanEdit = true,
                    CanDelete = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note for member {MemberId}", memberId);
                throw;
            }
        }

        public async Task UpdateMemberNoteAsync(Guid noteId, UpdateUserNoteDto dto, Guid updatedById)
        {
            try
            {
                _logger.LogInformation("User {UpdatedById} updating note {NoteId}", updatedById, noteId);

                var note = await _noteRepository.GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException($"Note {noteId} not found");
                }

                // Only creator or admin can update
                if (note.CreatedById != updatedById)
                {
                    var updater = await _userManager.FindByIdAsync(updatedById.ToString());
                    if (updater?.Role != UserRole.Administrator && updater?.Role != UserRole.Moderator)
                    {
                        throw new UnauthorizedAccessException("You can only edit notes you created");
                    }
                }

                note.Update(dto.Content);
                await _noteRepository.UpdateNoteAsync(note);

                _logger.LogInformation("Note {NoteId} updated successfully", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {NoteId}", noteId);
                throw;
            }
        }

        public async Task DeleteMemberNoteAsync(Guid noteId, Guid deletedById)
        {
            try
            {
                _logger.LogInformation("User {DeletedById} deleting note {NoteId}", deletedById, noteId);

                var note = await _noteRepository.GetNoteByIdAsync(noteId);
                if (note == null)
                {
                    throw new InvalidOperationException($"Note {noteId} not found");
                }

                // Only creator or admin can delete
                if (note.CreatedById != deletedById)
                {
                    var deleter = await _userManager.FindByIdAsync(deletedById.ToString());
                    if (deleter?.Role != UserRole.Administrator && deleter?.Role != UserRole.Moderator)
                    {
                        throw new UnauthorizedAccessException("You can only delete notes you created");
                    }
                }

                await _noteRepository.DeleteNoteAsync(noteId, deletedById);

                _logger.LogInformation("Note {NoteId} deleted successfully", noteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", noteId);
                throw;
            }
        }

        private async Task<List<UserNoteDto>> GetMemberNotesWithUserInfoAsync(Guid memberId)
        {
            var notes = await _noteRepository.GetUserNotesAsync(memberId);
            var notesList = notes.ToList();

            if (!notesList.Any())
                return new List<UserNoteDto>();

            // Get unique creator IDs
            var creatorIds = notesList.Select(n => n.CreatedById).Distinct().ToList();
            
            // Fetch user info for all creators
            var creators = new Dictionary<Guid, WitchCityRopeUser>();
            foreach (var creatorId in creatorIds)
            {
                var user = await _userManager.FindByIdAsync(creatorId.ToString());
                if (user != null)
                    creators[creatorId] = user;
            }

            // Map to DTOs
            return notesList.Select(n => new UserNoteDto
            {
                Id = n.Id,
                UserId = n.UserId,
                NoteType = n.NoteType.ToString(),
                Content = n.Content,
                CreatedByName = creators.ContainsKey(n.CreatedById) 
                    ? creators[n.CreatedById].SceneName 
                    : "Unknown",
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                CanEdit = true, // Will be determined by the UI based on current user
                CanDelete = true // Will be determined by the UI based on current user
            }).ToList();
        }
    }
}