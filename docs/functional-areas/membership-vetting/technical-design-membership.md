# Admin Members Management - Technical Design

## Architecture Overview
This document provides the detailed technical design for implementing the Admin Members Management feature, following Clean Architecture principles with clear separation of concerns.

## 1. Domain Layer (Core Project)

### 1.1 New Entities

#### UserNote Entity
```csharp
// Location: /src/WitchCityRope.Core/Entities/UserNote.cs
namespace WitchCityRope.Core.Entities;

public class UserNote : BaseEntity
{
    // Properties defined in database-changes.md
}
```

### 1.2 Domain Events
```csharp
// Location: /src/WitchCityRope.Core/Events/UserNoteEvents.cs
public record UserNoteCreatedEvent(Guid NoteId, Guid UserId, Guid CreatedById) : IDomainEvent;
public record UserNoteUpdatedEvent(Guid NoteId, Guid UserId, Guid UpdatedById) : IDomainEvent;
public record UserNoteDeletedEvent(Guid NoteId, Guid UserId, Guid DeletedById) : IDomainEvent;
```

### 1.3 DTOs

#### Member List DTOs
```csharp
// Location: /src/WitchCityRope.Core/DTOs/MemberDtos.cs
namespace WitchCityRope.Core.DTOs;

public class MemberListDto
{
    public Guid Id { get; set; }
    public string SceneName { get; set; }
    public string RealName { get; set; } // Decrypted for admin view
    public string? FetLifeName { get; set; }
    public string Email { get; set; }
    public DateTime DateJoined { get; set; }
    public int EventsAttended { get; set; }
    public string MembershipStatus { get; set; } // "Vetted", "Unvetted", "Pending"
    public string Role { get; set; }
    public bool IsActive { get; set; }
}

public class MemberSearchRequest
{
    public string? SearchTerm { get; set; }
    public string VettingStatus { get; set; } = "vetted"; // all, vetted, unvetted
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string SortBy { get; set; } = "sceneName";
    public string SortDirection { get; set; } = "asc";
}

public class PagedMemberResult
{
    public List<MemberListDto> Members { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

#### Member Detail DTOs
```csharp
public class MemberDetailDto
{
    public Guid Id { get; set; }
    public string SceneName { get; set; }
    public string RealName { get; set; }
    public string? FetLifeName { get; set; }
    public string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Pronouns { get; set; }
    public string? PronouncedName { get; set; }
    public string Role { get; set; }
    public bool IsVetted { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalEventsAttended { get; set; }
    public decimal TotalSpent { get; set; }
    
    // Computed properties
    public int Age => DateTime.Today.Year - DateOfBirth.Year;
    public string Status => GetMembershipStatus();
    
    private string GetMembershipStatus()
    {
        if (!IsActive) return "Inactive";
        if (IsVetted) return "Vetted";
        return "Unvetted";
    }
}

public class MemberEventHistoryDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public string EventType { get; set; }
    public string RegistrationStatus { get; set; }
    public decimal? PricePaid { get; set; }
    public bool DidAttend { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? RefundReason { get; set; }
}

public class UpdateMemberDto
{
    public string? SceneName { get; set; }
    public string? FetLifeName { get; set; }
    public string? Pronouns { get; set; }
    public string? PronouncedName { get; set; }
    public string? Role { get; set; }
    public bool? IsVetted { get; set; }
    public bool? IsActive { get; set; }
}
```

## 2. Infrastructure Layer

### 2.1 Repository Implementations

#### MemberRepository
```csharp
// Location: /src/WitchCityRope.Infrastructure/Repositories/MemberRepository.cs
namespace WitchCityRope.Infrastructure.Repositories;

public interface IMemberRepository
{
    Task<PagedMemberResult> GetMembersAsync(MemberSearchRequest request);
    Task<MemberDetailDto?> GetMemberDetailAsync(Guid memberId);
    Task<List<MemberEventHistoryDto>> GetMemberEventHistoryAsync(Guid memberId);
    Task<MemberStatsDto> GetMemberStatsAsync();
    Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto);
}

public class MemberRepository : IMemberRepository
{
    private readonly WitchCityRopeDbContext _context;
    private readonly IEncryptionService _encryptionService;
    
    public async Task<PagedMemberResult> GetMembersAsync(MemberSearchRequest request)
    {
        var query = _context.Users.AsNoTracking();
        
        // Apply vetting filter
        query = request.VettingStatus switch
        {
            "vetted" => query.Where(u => u.IsVetted),
            "unvetted" => query.Where(u => !u.IsVetted),
            _ => query // "all"
        };
        
        // Apply search
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            // Note: Real name search requires decryption, may need optimization
            query = query.Where(u => 
                u.SceneName.ToLower().Contains(searchLower) ||
                u.Email.ToLower().Contains(searchLower) ||
                (u.FetLifeName != null && u.FetLifeName.ToLower().Contains(searchLower)));
        }
        
        // Get total count before pagination
        var totalCount = await query.CountAsync();
        
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
                u.FetLifeName,
                u.Email,
                u.CreatedAt,
                u.Role,
                u.IsVetted,
                u.IsActive,
                EventCount = u.Registrations.Count(r => r.CheckedInAt != null)
            })
            .ToListAsync();
        
        // Decrypt real names and map to DTOs
        var members = users.Select(u => new MemberListDto
        {
            Id = u.Id,
            SceneName = u.SceneName,
            RealName = _encryptionService.Decrypt(u.EncryptedLegalName),
            FetLifeName = u.FetLifeName,
            Email = u.Email,
            DateJoined = u.CreatedAt,
            EventsAttended = u.EventCount,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            MembershipStatus = u.IsVetted ? "Vetted" : "Unvetted"
        }).ToList();
        
        return new PagedMemberResult
        {
            Members = members,
            TotalCount = totalCount,
            CurrentPage = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

### 2.2 Service Layer

#### MemberManagementService
```csharp
// Location: /src/WitchCityRope.Infrastructure/Services/MemberManagementService.cs
namespace WitchCityRope.Infrastructure.Services;

public interface IMemberManagementService
{
    Task<PagedMemberResult> SearchMembersAsync(MemberSearchRequest request);
    Task<MemberDetailViewModel> GetMemberDetailsAsync(Guid memberId);
    Task UpdateMemberAsync(Guid memberId, UpdateMemberDto dto, Guid updatedById);
    Task<MemberStatsDto> GetMemberStatsAsync();
}

public class MemberManagementService : IMemberManagementService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUserNoteRepository _noteRepository;
    private readonly IIncidentRepository _incidentRepository;
    private readonly ILogger<MemberManagementService> _logger;
    private readonly IMemoryCache _cache;
    
    public async Task<MemberDetailViewModel> GetMemberDetailsAsync(Guid memberId)
    {
        var member = await _memberRepository.GetMemberDetailAsync(memberId);
        if (member == null)
            throw new NotFoundException($"Member {memberId} not found");
            
        var tasks = new List<Task>();
        var notes = new List<UserNoteDto>();
        var events = new List<MemberEventHistoryDto>();
        var incidents = new List<IncidentSummaryDto>();
        
        // Parallel fetch related data
        tasks.Add(Task.Run(async () => 
            notes = await _noteRepository.GetUserNotesAsync(memberId)));
        tasks.Add(Task.Run(async () => 
            events = await _memberRepository.GetMemberEventHistoryAsync(memberId)));
        tasks.Add(Task.Run(async () => 
            incidents = await _incidentRepository.GetUserIncidentsAsync(memberId)));
            
        await Task.WhenAll(tasks);
        
        return new MemberDetailViewModel
        {
            Member = member,
            Notes = notes,
            EventHistory = events,
            Incidents = incidents
        };
    }
}
```

## 3. API Layer

### 3.1 Controllers

#### AdminMembersController
```csharp
// Location: /src/WitchCityRope.Api/Controllers/AdminMembersController.cs
namespace WitchCityRope.Api.Controllers;

[ApiController]
[Route("api/admin/members")]
[Authorize(Roles = "Administrator,Moderator")]
public class AdminMembersController : ControllerBase
{
    private readonly IMemberManagementService _memberService;
    private readonly IUserNoteRepository _noteRepository;
    private readonly ICurrentUserService _currentUser;
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedMemberResult), 200)]
    public async Task<IActionResult> GetMembers([FromQuery] MemberSearchRequest request)
    {
        var result = await _memberService.SearchMembersAsync(request);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MemberDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMember(Guid id)
    {
        var member = await _memberService.GetMemberDetailsAsync(id);
        if (member == null)
            return NotFound();
        return Ok(member);
    }
    
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberDto dto)
    {
        await _memberService.UpdateMemberAsync(id, dto, _currentUser.UserId);
        return NoContent();
    }
    
    [HttpGet("{id}/events")]
    [ProducesResponseType(typeof(List<MemberEventHistoryDto>), 200)]
    public async Task<IActionResult> GetMemberEvents(Guid id)
    {
        var events = await _memberService.GetMemberEventHistoryAsync(id);
        return Ok(events);
    }
    
    [HttpGet("{id}/notes")]
    [ProducesResponseType(typeof(List<UserNoteDto>), 200)]
    public async Task<IActionResult> GetMemberNotes(Guid id)
    {
        var notes = await _noteRepository.GetUserNotesAsync(id);
        return Ok(notes);
    }
    
    [HttpPost("{id}/notes")]
    [ProducesResponseType(typeof(UserNoteDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] CreateUserNoteDto dto)
    {
        dto.UserId = id; // Ensure correct user
        var note = await _noteRepository.AddNoteAsync(
            UserNote.Create(id, dto.NoteType, dto.Content, _currentUser.UserId));
        return CreatedAtAction(nameof(GetMemberNotes), new { id }, note);
    }
    
    [HttpPut("notes/{noteId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateNote(Guid noteId, [FromBody] UpdateUserNoteDto dto)
    {
        var note = await _noteRepository.GetNoteByIdAsync(noteId);
        if (note == null)
            return NotFound();
            
        if (note.CreatedById != _currentUser.UserId && !_currentUser.IsAdministrator)
            return Forbid();
            
        note.Update(dto.Content);
        await _noteRepository.UpdateNoteAsync(note);
        return NoContent();
    }
    
    [HttpDelete("notes/{noteId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteNote(Guid noteId)
    {
        var note = await _noteRepository.GetNoteByIdAsync(noteId);
        if (note == null)
            return NotFound();
            
        if (note.CreatedById != _currentUser.UserId && !_currentUser.IsAdministrator)
            return Forbid();
            
        await _noteRepository.DeleteNoteAsync(noteId, _currentUser.UserId);
        return NoContent();
    }
    
    [HttpGet("stats")]
    [ProducesResponseType(typeof(MemberStatsDto), 200)]
    [ResponseCache(Duration = 300)] // Cache for 5 minutes
    public async Task<IActionResult> GetStats()
    {
        var stats = await _memberService.GetMemberStatsAsync();
        return Ok(stats);
    }
}
```

## 4. Web Layer (Blazor)

### 4.1 Page Components

#### Members List Page
```csharp
// Location: /src/WitchCityRope.Web/Pages/Admin/Members/Index.razor
@page "/admin/members"
@attribute [Authorize(Roles = "Administrator,Moderator")]
@inherits MembersListBase

<PageTitle>Member Management - WitchCityRope Admin</PageTitle>

<div class="admin-page">
    <PageHeader Title="Member Management" 
                Subtitle="Manage all community members and their information" />
    
    <MemberStats Stats="@Stats" IsLoading="@IsLoadingStats" />
    
    <div class="filters-section">
        <MemberFilters @bind-VettingStatus="@VettingStatus"
                      @bind-SearchTerm="@SearchTerm"
                      @bind-PageSize="@PageSize"
                      OnFilterChanged="@ApplyFilters" />
    </div>
    
    <div class="grid-section">
        @if (IsLoadingMembers)
        {
            <LoadingSpinner Message="Loading members..." />
        }
        else if (!Members.Any())
        {
            <EmptyState Icon="users" 
                       Message="No members found matching your criteria" />
        }
        else
        {
            <MemberDataGrid Members="@Members"
                           TotalCount="@TotalCount"
                           CurrentPage="@CurrentPage"
                           PageSize="@PageSize"
                           SortBy="@SortBy"
                           SortDirection="@SortDirection"
                           OnSort="@HandleSort"
                           OnPageChange="@HandlePageChange"
                           OnRowClick="@NavigateToMember" />
        }
    </div>
</div>
```

#### Members List Code-Behind
```csharp
// Location: /src/WitchCityRope.Web/Pages/Admin/Members/Index.razor.cs
namespace WitchCityRope.Web.Pages.Admin.Members;

public class MembersListBase : ComponentBase
{
    [Inject] private ApiClient ApiClient { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private ILogger<MembersListBase> Logger { get; set; }
    
    protected List<MemberListDto> Members = new();
    protected MemberStatsDto Stats = new();
    protected bool IsLoadingMembers = true;
    protected bool IsLoadingStats = true;
    
    protected string VettingStatus = "vetted";
    protected string SearchTerm = "";
    protected int PageSize = 100;
    protected int CurrentPage = 1;
    protected int TotalCount = 0;
    protected string SortBy = "sceneName";
    protected string SortDirection = "asc";
    
    private CancellationTokenSource? _searchCancellation;
    private Timer? _debounceTimer;
    
    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(
            LoadMembersAsync(),
            LoadStatsAsync()
        );
    }
    
    protected async Task LoadMembersAsync()
    {
        try
        {
            IsLoadingMembers = true;
            
            var request = new MemberSearchRequest
            {
                VettingStatus = VettingStatus,
                SearchTerm = SearchTerm,
                Page = CurrentPage,
                PageSize = PageSize,
                SortBy = SortBy,
                SortDirection = SortDirection
            };
            
            var result = await ApiClient.GetAsync<PagedMemberResult>(
                $"api/admin/members?{BuildQueryString(request)}");
                
            Members = result.Members;
            TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load members");
            // Show error notification
        }
        finally
        {
            IsLoadingMembers = false;
        }
    }
    
    protected async Task ApplyFilters()
    {
        // Cancel previous search
        _searchCancellation?.Cancel();
        _debounceTimer?.Dispose();
        
        // Debounce search input
        _debounceTimer = new Timer(async _ =>
        {
            _searchCancellation = new CancellationTokenSource();
            CurrentPage = 1; // Reset to first page
            await InvokeAsync(async () =>
            {
                await LoadMembersAsync();
                StateHasChanged();
            });
        }, null, TimeSpan.FromMilliseconds(300), Timeout.InfiniteTimeSpan);
    }
    
    protected void NavigateToMember(MemberListDto member)
    {
        Navigation.NavigateTo($"/admin/members/{member.Id}");
    }
}
```

### 4.2 Component Architecture

#### Syncfusion DataGrid Component
```csharp
// Location: /src/WitchCityRope.Web/Components/Admin/Members/MemberDataGrid.razor
@using Syncfusion.Blazor.Grids

<SfGrid @ref="Grid" 
        DataSource="@Members" 
        AllowPaging="false"
        AllowSorting="true"
        EnableAltRow="true"
        RowHeight="48">
    
    <GridEvents TValue="MemberListDto" 
                RowSelected="@OnRowSelected"
                OnSort="@OnSort" />
    
    <GridColumns>
        <GridColumn Field="@nameof(MemberListDto.SceneName)" 
                    HeaderText="Scene Name" 
                    Width="150">
            <Template>
                @{
                    var member = (context as MemberListDto);
                    <div class="member-name">
                        <strong>@member.SceneName</strong>
                    </div>
                }
            </Template>
        </GridColumn>
        
        <GridColumn Field="@nameof(MemberListDto.RealName)" 
                    HeaderText="Real Name" 
                    Width="150" />
                    
        <GridColumn Field="@nameof(MemberListDto.FetLifeName)" 
                    HeaderText="FetLife" 
                    Width="120">
            <Template>
                @{
                    var member = (context as MemberListDto);
                    if (!string.IsNullOrEmpty(member.FetLifeName))
                    {
                        <a href="https://fetlife.com/@member.FetLifeName" 
                           target="_blank" 
                           @onclick:stopPropagation="true">
                            @member.FetLifeName
                        </a>
                    }
                }
            </Template>
        </GridColumn>
        
        <GridColumn Field="@nameof(MemberListDto.Email)" 
                    HeaderText="Email" 
                    Width="200">
            <Template>
                @{
                    var member = (context as MemberListDto);
                    <a href="mailto:@member.Email" 
                       @onclick:stopPropagation="true">
                        @member.Email
                    </a>
                }
            </Template>
        </GridColumn>
        
        <GridColumn Field="@nameof(MemberListDto.DateJoined)" 
                    HeaderText="Joined" 
                    Width="100" 
                    Format="d" 
                    Type="ColumnType.Date" />
                    
        <GridColumn Field="@nameof(MemberListDto.EventsAttended)" 
                    HeaderText="Events" 
                    Width="80" 
                    TextAlign="TextAlign.Center" />
                    
        <GridColumn Field="@nameof(MemberListDto.MembershipStatus)" 
                    HeaderText="Status" 
                    Width="100">
            <Template>
                @{
                    var member = (context as MemberListDto);
                    <span class="badge badge-@GetStatusClass(member.MembershipStatus)">
                        @member.MembershipStatus
                    </span>
                }
            </Template>
        </GridColumn>
    </GridColumns>
</SfGrid>

@code {
    [Parameter] public List<MemberListDto> Members { get; set; } = new();
    [Parameter] public EventCallback<MemberListDto> OnRowClick { get; set; }
    [Parameter] public EventCallback<SortEventArgs> OnSort { get; set; }
    
    private SfGrid<MemberListDto> Grid;
    
    private async Task OnRowSelected(RowSelectEventArgs<MemberListDto> args)
    {
        await OnRowClick.InvokeAsync(args.Data);
    }
    
    private string GetStatusClass(string status)
    {
        return status.ToLower() switch
        {
            "vetted" => "success",
            "member" => "info",
            "pending" => "warning",
            _ => "secondary"
        };
    }
}
```

## 5. State Management

### 5.1 Member State Service
```csharp
// Location: /src/WitchCityRope.Web/Services/MemberStateService.cs
namespace WitchCityRope.Web.Services;

public class MemberStateService
{
    private readonly ApiClient _apiClient;
    private MemberDetailViewModel? _currentMember;
    
    public event Action? OnStateChanged;
    
    public MemberDetailViewModel? CurrentMember => _currentMember;
    
    public async Task LoadMemberAsync(Guid memberId)
    {
        _currentMember = await _apiClient.GetAsync<MemberDetailViewModel>(
            $"api/admin/members/{memberId}");
        NotifyStateChanged();
    }
    
    public async Task UpdateMemberAsync(UpdateMemberDto dto)
    {
        await _apiClient.PutAsync($"api/admin/members/{_currentMember.Member.Id}", dto);
        await LoadMemberAsync(_currentMember.Member.Id);
    }
    
    public async Task AddNoteAsync(CreateUserNoteDto dto)
    {
        await _apiClient.PostAsync($"api/admin/members/{_currentMember.Member.Id}/notes", dto);
        await LoadMemberAsync(_currentMember.Member.Id);
    }
    
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
```

## 6. Security Considerations

### 6.1 Authorization
- All endpoints require Administrator or Moderator roles
- Real name decryption only for authorized users
- Note editing restricted to creator or admin
- Audit logging for all member updates

### 6.2 Data Protection
- Real names stored encrypted
- PII access logged
- Session timeout for admin pages
- CSRF protection on all forms

## 7. Performance Optimization

### 7.1 Database
- Indexes on search fields
- Compiled queries for common operations
- Pagination to limit data transfer
- Caching for stats and counts

### 7.2 Frontend
- Virtual scrolling for large datasets
- Debounced search input
- Lazy loading for detail tabs
- Component state caching

## 8. Error Handling

### 8.1 API Error Responses
```csharp
public class ApiError
{
    public string Message { get; set; }
    public string Code { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
```

### 8.2 Frontend Error Display
- Toast notifications for errors
- Inline validation messages
- Retry mechanisms for failed requests
- Graceful degradation

## 9. Testing Strategy

### 9.1 Unit Tests
- Domain entity tests
- Service layer tests
- API controller tests
- Component logic tests

### 9.2 Integration Tests
- API endpoint tests
- Database query tests
- Authentication/authorization tests

### 9.3 E2E Tests
- Full user journey tests
- Search and filter tests
- CRUD operation tests
- Performance tests

This technical design provides a complete blueprint for implementing the Admin Members Management feature following Clean Architecture principles and modern development practices.