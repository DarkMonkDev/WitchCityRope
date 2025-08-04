using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Extensions;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Repositories;
using WitchCityRope.Infrastructure.Services;

namespace WitchCityRope.Api.Features.Admin.Members
{
    [ApiController]
    [Route("api/admin/members")]
    [Authorize(Roles = "Administrator,Moderator")]
    public class AdminMembersController : ControllerBase
    {
        private readonly IMemberManagementService _memberService;
        private readonly IUserNoteRepository _noteRepository;
        private readonly ILogger<AdminMembersController> _logger;

        public AdminMembersController(
            IMemberManagementService memberService,
            IUserNoteRepository noteRepository,
            ILogger<AdminMembersController> logger)
        {
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a paginated list of members with optional filtering and sorting.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedMemberResult), 200)]
        public async Task<IActionResult> GetMembers([FromQuery] MemberSearchRequest request)
        {
            try
            {
                var result = await _memberService.SearchMembersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving members");
                return StatusCode(500, new { message = "An error occurred while retrieving members" });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific member.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MemberDetailViewModel), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMember(Guid id)
        {
            try
            {
                var member = await _memberService.GetMemberDetailsAsync(id);
                return Ok(member);
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { message = "Member not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member {MemberId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving member details" });
            }
        }

        /// <summary>
        /// Updates a member's information.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberDto dto)
        {
            try
            {
                var currentUserId = User.GetUserId();
                await _memberService.UpdateMemberAsync(id, dto, currentUserId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("not found"))
                    return NotFound(new { message = ex.Message });
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member {MemberId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the member" });
            }
        }

        /// <summary>
        /// Gets member statistics for the admin dashboard.
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(MemberStatsDto), 200)]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _memberService.GetMemberStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member stats");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        /// <summary>
        /// Gets all notes for a specific member.
        /// </summary>
        [HttpGet("{id}/notes")]
        [ProducesResponseType(typeof(List<UserNoteDto>), 200)]
        public async Task<IActionResult> GetMemberNotes(Guid id)
        {
            try
            {
                var notes = await _noteRepository.GetUserNotesAsync(id);
                var noteDtos = new List<UserNoteDto>();
                
                foreach (var note in notes)
                {
                    noteDtos.Add(new UserNoteDto
                    {
                        Id = note.Id,
                        UserId = note.UserId,
                        NoteType = note.NoteType.ToString(),
                        Content = note.Content,
                        CreatedByName = "System", // Will be populated by service in production
                        CreatedAt = note.CreatedAt,
                        UpdatedAt = note.UpdatedAt,
                        CanEdit = note.CreatedById == User.GetUserId() || User.IsInRole("Administrator"),
                        CanDelete = note.CreatedById == User.GetUserId() || User.IsInRole("Administrator")
                    });
                }
                
                return Ok(noteDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notes for member {MemberId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving notes" });
            }
        }

        /// <summary>
        /// Adds a new note for a member.
        /// </summary>
        [HttpPost("{id}/notes")]
        [ProducesResponseType(typeof(UserNoteDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> AddNote(Guid id, [FromBody] CreateUserNoteDto dto)
        {
            try
            {
                if (dto.UserId != id)
                {
                    dto.UserId = id; // Ensure correct user ID
                }

                var currentUserId = User.GetUserId();
                var noteDto = await _memberService.AddMemberNoteAsync(id, dto, currentUserId);
                
                return CreatedAtAction(
                    nameof(GetMemberNotes), 
                    new { id }, 
                    noteDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note for member {MemberId}", id);
                return StatusCode(500, new { message = "An error occurred while adding the note" });
            }
        }

        /// <summary>
        /// Updates an existing note.
        /// </summary>
        [HttpPut("notes/{noteId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] UpdateUserNoteDto dto)
        {
            try
            {
                var currentUserId = User.GetUserId();
                await _memberService.UpdateMemberNoteAsync(noteId, dto, currentUserId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note {NoteId}", noteId);
                return StatusCode(500, new { message = "An error occurred while updating the note" });
            }
        }

        /// <summary>
        /// Deletes a note (soft delete).
        /// </summary>
        [HttpDelete("notes/{noteId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteNote(Guid noteId)
        {
            try
            {
                var currentUserId = User.GetUserId();
                await _memberService.DeleteMemberNoteAsync(noteId, currentUserId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", noteId);
                return StatusCode(500, new { message = "An error occurred while deleting the note" });
            }
        }
    }
}