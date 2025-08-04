using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.DTOs;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Repositories;
using WitchCityRope.Infrastructure.Data;
using WitchCityRope.Infrastructure.Identity;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for member management operations.
    /// </summary>
    public class MemberRepository : IMemberRepository
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<MemberRepository> _logger;

        public MemberRepository(
            WitchCityRopeIdentityDbContext context,
            IEncryptionService encryptionService,
            ILogger<MemberRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedMemberResult> GetMembersAsync(MemberSearchRequest request)
        {
            try
            {
                var query = _context.Users.AsNoTracking();

                // Apply vetting filter
                query = request.VettingStatus?.ToLower() switch
                {
                    "vetted" => query.Where(u => u.IsVetted),
                    "unvetted" => query.Where(u => !u.IsVetted),
                    _ => query // "all" or null
                };

                // Apply search - note that real name search requires post-filtering due to encryption
                List<WitchCityRopeUser>? allUsers = null;
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    
                    // For real name search, we need to decrypt, so fetch all matching users first
                    allUsers = await query.ToListAsync();
                    
                    // Filter in memory after decryption
                    var filteredUsers = new List<WitchCityRopeUser>();
                    foreach (var u in allUsers)
                    {
                        var decryptedName = await _encryptionService.DecryptAsync(u.EncryptedLegalName);
                        if (u.SceneName.Value.ToLower().Contains(searchLower) ||
                            u.Email.ToLower().Contains(searchLower) ||
                            decryptedName.ToLower().Contains(searchLower))
                        {
                            filteredUsers.Add(u);
                        }
                    }

                    // Get IDs of filtered users
                    var filteredIds = filteredUsers.Select(u => u.Id).ToList();
                    query = _context.Users.Where(u => filteredIds.Contains(u.Id));
                }

                // Get total count before pagination
                var totalCount = allUsers?.Count ?? await query.CountAsync();

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "realname" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.EncryptedLegalName)
                        : query.OrderBy(u => u.EncryptedLegalName),
                    "email" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "datejoined" => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    // FetLifeName doesn't exist, skip this sort option
                    _ => request.SortDirection == "desc"
                        ? query.OrderByDescending(u => u.SceneName)
                        : query.OrderBy(u => u.SceneName)
                };

                // Apply pagination
                var skip = (request.Page - 1) * request.PageSize;
                var users = await query
                    .Skip(skip)
                    .Take(request.PageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.SceneName,
                        u.EncryptedLegalName,
                        // u.FetLifeName, - property doesn't exist
                        u.Email,
                        u.CreatedAt,
                        u.Role,
                        u.IsVetted,
                        u.IsActive,
                        EventCount = _context.Tickets.Count(t => t.UserId == u.Id && t.CheckedInAt != null)
                    })
                    .ToListAsync();

                // Decrypt real names and map to DTOs
                var members = new List<MemberListDto>();
                foreach (var u in users)
                {
                    var decryptedName = await _encryptionService.DecryptAsync(u.EncryptedLegalName);
                    members.Add(new MemberListDto
                    {
                        Id = u.Id,
                        SceneName = u.SceneName,
                        RealName = decryptedName,
                        FetLifeName = null, // Property doesn't exist on WitchCityRopeUser
                        Email = u.Email,
                        DateJoined = u.CreatedAt,
                        EventsAttended = u.EventCount,
                        Role = u.Role.ToString(),
                        IsActive = u.IsActive,
                        MembershipStatus = u.IsVetted ? "Vetted" : "Unvetted"
                    });
                }

                return new PagedMemberResult
                {
                    Members = members,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving members");
                throw;
            }
        }

        public async Task<MemberDetailDto?> GetMemberDetailAsync(Guid memberId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == memberId)
                .Select(u => new
                {
                    u.Id,
                    u.SceneName,
                    u.EncryptedLegalName,
                    // u.FetLifeName, - property doesn't exist
                    u.Email,
                    u.DateOfBirth,
                    u.Pronouns,
                    u.PronouncedName,
                    u.Role,
                    u.IsVetted,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                    EventsAttended = _context.Tickets.Count(t => t.UserId == u.Id && t.CheckedInAt != null),
                    TotalSpent = _context.Tickets
                        .Where(t => t.UserId == u.Id && t.Payment != null && t.Payment.Status == Core.Enums.PaymentStatus.Completed)
                        .Sum(t => (decimal?)t.Payment!.Amount.Amount) ?? 0m
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            var decryptedRealName = await _encryptionService.DecryptAsync(user.EncryptedLegalName);
            
            return new MemberDetailDto
            {
                Id = user.Id,
                SceneName = user.SceneName,
                RealName = decryptedRealName,
                FetLifeName = null, // Property doesn't exist on WitchCityRopeUser
                Email = user.Email,
                DateOfBirth = user.DateOfBirth,
                Pronouns = user.Pronouns,
                PronouncedName = user.PronouncedName,
                Role = user.Role.ToString(),
                IsVetted = user.IsVetted,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalEventsAttended = user.EventsAttended,
                TotalSpent = user.TotalSpent
            };
        }

        public async Task<List<MemberEventHistoryDto>> GetMemberEventHistoryAsync(Guid memberId)
        {
            var registrations = await _context.Tickets
                .AsNoTracking()
                .Include(r => r.Event)
                .Include(r => r.Payment)
                .Where(r => r.UserId == memberId)
                .OrderByDescending(r => r.Event.StartDate)
                .Select(r => new MemberEventHistoryDto
                {
                    EventId = r.EventId,
                    EventName = r.Event.Title,
                    EventDate = r.Event.StartDate,
                    EventType = r.Event.EventType.ToString(),
                    RegistrationStatus = r.Status.ToString(), // TODO: Update DTO property name to TicketStatus
                    PricePaid = r.Payment != null ? r.Payment.Amount.Amount : null,
                    DidAttend = r.CheckedInAt.HasValue,
                    CheckedInAt = r.CheckedInAt,
                    RefundReason = r.CancellationReason
                })
                .ToListAsync();

            return registrations;
        }

        public async Task<MemberStatsDto> GetMemberStatsAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfToday = now.Date;

            var stats = await _context.Users
                .GroupBy(u => 1) // Group all into one group for aggregation
                .Select(g => new MemberStatsDto
                {
                    TotalMembers = g.Count(),
                    VettedMembers = g.Count(u => u.IsVetted),
                    ActiveToday = g.Count(u => u.LastLoginAt >= startOfToday),
                    NewThisMonth = g.Count(u => u.CreatedAt >= startOfMonth),
                    CalculatedAt = now
                })
                .FirstOrDefaultAsync();

            return stats ?? new MemberStatsDto { CalculatedAt = now };
        }

        public async Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto)
        {
            var user = await _context.Users.FindAsync(memberId);
            if (user == null)
                throw new InvalidOperationException($"User with ID {memberId} not found.");

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.SceneName))
                user.UpdateSceneName(SceneName.Create(dto.SceneName));

            // FetLifeName property doesn't exist on WitchCityRopeUser

            if (dto.Pronouns != null && !string.IsNullOrWhiteSpace(dto.Pronouns))
                user.UpdatePronouns(dto.Pronouns);

            if (dto.PronouncedName != null && !string.IsNullOrWhiteSpace(dto.PronouncedName))
                user.UpdatePronouncedName(dto.PronouncedName);

            if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, out var role))
            {
                // Role property has internal setter, can only promote
                if (role > user.Role)
                    user.PromoteToRole(role);
                // Note: Demotion would require a different approach
            }

            if (dto.IsVetted.HasValue && dto.IsVetted.Value && !user.IsVetted)
                user.MarkAsVetted();
            // Note: No method to unmark as vetted

            if (dto.IsActive.HasValue)
            {
                if (dto.IsActive.Value && !user.IsActive)
                    user.Reactivate();
                else if (!dto.IsActive.Value && user.IsActive)
                    user.Deactivate();
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsSceneNameTakenAsync(string sceneName, Guid? excludeUserId = null)
        {
            var query = _context.Users.Where(u => u.SceneName == sceneName);
            
            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync();
        }

        public async Task<List<MemberListDto>> GetEventAttendeesAsync(Guid eventId)
        {
            var attendees = await _context.Tickets
                .AsNoTracking()
                // Ticket doesn't have User navigation property
                .Where(t => t.EventId == eventId && t.CheckedInAt != null)
                .Select(r => new MemberListDto
                {
                    // Need to join with Users table separately
                    Id = r.UserId,
                    SceneName = string.Empty, // Will be populated after join
                    RealName = string.Empty,
                    FetLifeName = null,
                    Email = string.Empty,
                    DateJoined = DateTime.UtcNow,
                    EventsAttended = 0,
                    Role = string.Empty,
                    IsActive = true,
                    MembershipStatus = string.Empty
                })
                .ToListAsync();

            // Need to fetch user data and decrypt real names
            var userIds = attendees.Select(a => a.Id).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();
                
            foreach (var attendee in attendees)
            {
                var user = users.FirstOrDefault(u => u.Id == attendee.Id);
                if (user != null)
                {
                    attendee.SceneName = user.SceneName.Value;
                    attendee.RealName = await _encryptionService.DecryptAsync(user.EncryptedLegalName);
                    attendee.Email = user.Email;
                    attendee.DateJoined = user.CreatedAt;
                    attendee.Role = user.Role.ToString();
                    attendee.IsActive = user.IsActive;
                    attendee.MembershipStatus = user.IsVetted ? "Vetted" : "Unvetted";
                }
            }

            return attendees;
        }

        public async Task<List<IncidentSummaryDto>> GetMemberIncidentsAsync(Guid memberId)
        {
            // Get incidents where the member is involved
            var incidents = await _context.IncidentReports
                .AsNoTracking()
                // IncidentReport doesn't have InvolvedUserIds property
                // Only check if user is the reporter
                .Where(i => i.ReporterId == memberId)
                .OrderByDescending(i => i.IncidentDate)
                .Select(i => new IncidentSummaryDto
                {
                    Id = i.Id,
                    IncidentDate = i.IncidentDate,
                    IncidentType = i.IncidentType.ToString(),
                    Severity = i.Severity.ToString(),
                    Status = i.Status.ToString(),
                    ReferenceNumber = i.ReferenceNumber
                })
                .ToListAsync();

            return incidents;
        }
    }
}