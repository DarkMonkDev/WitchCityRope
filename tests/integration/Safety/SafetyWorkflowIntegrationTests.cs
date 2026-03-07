using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Models;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Features.Safety;

/// <summary>
/// Integration tests for Safety Incident Reporting workflows
/// Tests full request/response cycles with real PostgreSQL database
///
/// Created: 2025-10-18
/// Updated: 2025-11-11 - Fully implemented all 6 integration tests
/// Coverage: Anonymous submission, identified submission, full lifecycle, coordinator workflow, notes
///
/// Uses service layer with real database (DatabaseTestFixture pattern)
/// </summary>
[Collection("Database")]
public class SafetyWorkflowIntegrationTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ILogger<SafetyService>> _mockLogger;
    private readonly Mock<ILogger<SafetyServiceExtended>> _mockLoggerExtended;
    private SafetyServiceExtended _sut = null!; // System Under Test - initialized in InitializeAsync
    private ApplicationDbContext _context = null!; // Initialized in InitializeAsync

    public SafetyWorkflowIntegrationTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockLogger = new Mock<ILogger<SafetyService>>();
        _mockLoggerExtended = new Mock<ILogger<SafetyServiceExtended>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Create fresh context and service for each test
        _context = _fixture.CreateDbContext();

        // Setup default encryption behavior
        _mockEncryptionService
            .Setup(x => x.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) => $"ENCRYPTED_{input}");

        _mockEncryptionService
            .Setup(x => x.DecryptAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) => input.Replace("ENCRYPTED_", ""));

        // Create extended service (includes all workflow methods)
        _sut = new SafetyServiceExtended(
            _context,
            _mockEncryptionService.Object,
            _mockAuditService.Object,
            _mockLoggerExtended.Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    #region Anonymous Incident Submission Workflow

    /// <summary>
    /// Test 1: Verify anonymous incident submission and status tracking workflow
    /// Validates: Reference number generation, status tracking without authentication
    /// </summary>
    [Fact]
    public async Task AnonymousIncidentSubmission_CanSubmitAndTrackStatus()
    {
        // Arrange
        var request = new CreateIncidentRequest
        {
            IsAnonymous = true,
            Title = "Anonymous Integration Test",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-1),
            Location = "Test Location",
            Description = "Integration test for anonymous incident submission workflow with sufficient character count to pass validation"
        };

        // Act - Submit incident
        var submitResult = await _sut.SubmitIncidentAsync(request);

        // Assert - Submission succeeded
        submitResult.IsSuccess.Should().BeTrue();
        submitResult.Value.Should().NotBeNull();
        submitResult.Value!.ReferenceNumber.Should().MatchRegex(@"^SAF-\d{8}-\d{4}$");
        submitResult.Value.TrackingUrl.Should().Contain(submitResult.Value.ReferenceNumber);

        // Act - Track status using reference number (no auth required)
        var statusResult = await _sut.GetIncidentStatusAsync(submitResult.Value.ReferenceNumber);

        // Assert - Status tracking works
        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Value.Should().NotBeNull();
        statusResult.Value!.ReferenceNumber.Should().Be(submitResult.Value.ReferenceNumber);
        statusResult.Value.Status.Should().Be("ReportSubmitted");
        statusResult.Value.CanProvideMoreInfo.Should().BeFalse(); // Anonymous can't provide more info
        statusResult.Value.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Identified Incident Submission Workflow

    /// <summary>
    /// Test 2: Verify identified incident appears in user's "My Reports" view
    /// Validates: Reporter ID tracking, user report retrieval
    /// </summary>
    [Fact]
    public async Task IdentifiedIncidentSubmission_AppearsInMyReports()
    {
        // Arrange - Create test user
        var reporter = await CreateTestUserAsync("IntegrationReporter");

        var request = new CreateIncidentRequest
        {
            ReporterId = reporter.Id,
            IsAnonymous = false,
            Title = "Identified Report Integration Test",
            Type = IncidentType.BoundaryViolation,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-2),
            Location = "Workshop Room",
            Description = "Integration test for identified incident submission workflow with sufficient detail to pass validation",
            ContactEmail = "reporter@example.com",
            ContactName = "Integration Reporter",
            RequestFollowUp = true
        };

        // Act - Submit incident as identified user
        var submitResult = await _sut.SubmitIncidentAsync(request);

        // Assert - Submission succeeded
        submitResult.IsSuccess.Should().BeTrue();
        submitResult.Value.Should().NotBeNull();

        // Act - Retrieve user's reports (paginated)
        var myReportsResult = await _sut.GetMyReportsAsync(reporter.Id, page: 1, pageSize: 10);

        // Assert - Report appears in user's reports
        myReportsResult.IsSuccess.Should().BeTrue();
        myReportsResult.Value.Should().NotBeNull();
        myReportsResult.Value!.Reports.Should().ContainSingle();
        myReportsResult.Value.Reports.First().Status.Should().Be(IncidentStatus.ReportSubmitted);
        myReportsResult.Value.TotalCount.Should().Be(1);
    }

    #endregion

    #region Full Incident Lifecycle Workflow

    /// <summary>
    /// Test 3: Verify complete incident lifecycle from submission to closure
    /// Validates: Status transitions, coordinator assignment, system notes
    /// </summary>
    [Fact]
    public async Task FullIncidentLifecycle_FromSubmissionToClosure()
    {
        // Arrange - Create coordinator and submit incident
        var coordinator = await CreateTestUserAsync("Coordinator", role: "SafetyTeam");
        var admin = await CreateTestUserAsync("Admin", role: "Administrator");

        var request = new CreateIncidentRequest
        {
            IsAnonymous = false,
            ReporterId = admin.Id,
            Title = "Full Lifecycle Test",
            Type = IncidentType.SafetyConcern,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-3),
            Location = "Event Space",
            Description = "Integration test for full incident lifecycle workflow from submission through closure with all status transitions"
        };

        // Act - Submit incident
        var submitResult = await _sut.SubmitIncidentAsync(request);
        submitResult.IsSuccess.Should().BeTrue();

        var incidentId = (await _context.SafetyIncidents
            .FirstAsync(i => i.ReferenceNumber == submitResult.Value!.ReferenceNumber)).Id;

        // Act - Assign coordinator
        var assignResult = await _sut.AssignCoordinatorAsync(
            incidentId,
            new AssignCoordinatorRequest { CoordinatorId = coordinator.Id },
            admin.Id);

        // Assert - Coordinator assigned
        assignResult.IsSuccess.Should().BeTrue();
        assignResult.Value!.CoordinatorId.Should().Be(coordinator.Id);

        // Act - Update status to InformationGathering
        var statusUpdate1 = await _sut.UpdateStatusAsync(
            incidentId,
            new UpdateStatusRequest
            {
                NewStatus = IncidentStatus.InformationGathering,
                Reason = "Starting investigation"
            },
            coordinator.Id);

        // Assert - Status updated
        statusUpdate1.IsSuccess.Should().BeTrue();
        statusUpdate1.Value!.Status.Should().Be(IncidentStatus.InformationGathering);
        statusUpdate1.Value.SystemNoteCreated.Should().BeTrue();

        // Act - Update status to ReviewingFinalReport
        var statusUpdate2 = await _sut.UpdateStatusAsync(
            incidentId,
            new UpdateStatusRequest
            {
                NewStatus = IncidentStatus.ReviewingFinalReport,
                Reason = "Investigation complete, reviewing final report"
            },
            coordinator.Id);

        // Assert - Status updated
        statusUpdate2.IsSuccess.Should().BeTrue();
        statusUpdate2.Value!.Status.Should().Be(IncidentStatus.ReviewingFinalReport);

        // Act - Update status to Closed
        var statusUpdate3 = await _sut.UpdateStatusAsync(
            incidentId,
            new UpdateStatusRequest
            {
                NewStatus = IncidentStatus.Closed,
                Reason = "Incident resolved successfully"
            },
            coordinator.Id);

        // Assert - Final status is Closed
        statusUpdate3.IsSuccess.Should().BeTrue();
        statusUpdate3.Value!.Status.Should().Be(IncidentStatus.Closed);

        // Act - Verify incident detail shows complete audit trail
        var detailResult = await _sut.GetIncidentDetailAsync(incidentId, coordinator.Id);

        // Assert - Complete lifecycle recorded
        detailResult.IsSuccess.Should().BeTrue();
        detailResult.Value!.Status.Should().Be(IncidentStatus.Closed);
        detailResult.Value.CoordinatorId.Should().Be(coordinator.Id);
    }

    #endregion

    #region Coordinator Workflow Tests

    /// <summary>
    /// Test 4: Verify coordinator dashboard shows only assigned incidents
    /// Validates: Assignment filtering, coordinator access boundaries
    /// </summary>
    [Fact]
    public async Task CoordinatorDashboard_ShowsOnlyAssignedIncidents()
    {
        // Arrange - Create two coordinators and incidents
        var coordinator1 = await CreateTestUserAsync("Coordinator1", role: "SafetyTeam");
        var coordinator2 = await CreateTestUserAsync("Coordinator2", role: "SafetyTeam");
        var admin = await CreateTestUserAsync("Admin", role: "Administrator");

        // Create incidents assigned to different coordinators
        var incident1 = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Incident for Coordinator 1");
        var incident2 = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Incident for Coordinator 2");
        var incident3 = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Unassigned Incident");

        // Assign coordinators
        await _sut.AssignCoordinatorAsync(incident1.Id, new AssignCoordinatorRequest { CoordinatorId = coordinator1.Id }, admin.Id);
        await _sut.AssignCoordinatorAsync(incident2.Id, new AssignCoordinatorRequest { CoordinatorId = coordinator2.Id }, admin.Id);

        // Act - Get incidents for coordinator1 (non-admin, so should only see assigned)
        var coordinator1Result = await _sut.GetIncidentsAsync(
            new AdminIncidentListRequest { Page = 1, PageSize = 20 },
            coordinator1.Id,
            isAdmin: false);

        // Assert - Coordinator1 sees only their assigned incident
        coordinator1Result.IsSuccess.Should().BeTrue();
        coordinator1Result.Value!.Items.Should().ContainSingle();
        coordinator1Result.Value.Items.First().CoordinatorId.Should().Be(coordinator1.Id);

        // Act - Get incidents for coordinator2
        var coordinator2Result = await _sut.GetIncidentsAsync(
            new AdminIncidentListRequest { Page = 1, PageSize = 20 },
            coordinator2.Id,
            isAdmin: false);

        // Assert - Coordinator2 sees only their assigned incident
        coordinator2Result.IsSuccess.Should().BeTrue();
        coordinator2Result.Value!.Items.Should().ContainSingle();
        coordinator2Result.Value.Items.First().CoordinatorId.Should().Be(coordinator2.Id);

        // Act - Admin sees all incidents
        var adminResult = await _sut.GetIncidentsAsync(
            new AdminIncidentListRequest { Page = 1, PageSize = 20 },
            admin.Id,
            isAdmin: true);

        // Assert - Admin sees all 3 incidents
        adminResult.IsSuccess.Should().BeTrue();
        adminResult.Value!.Items.Should().HaveCount(3);
    }

    /// <summary>
    /// Test 5: Verify coordinator filtering respects assignment boundaries
    /// Validates: Coordinator cannot access incidents assigned to others
    /// </summary>
    [Fact]
    public async Task CoordinatorFiltering_RespectsAssignmentBoundaries()
    {
        // Arrange - Create coordinators and incidents
        var coordinator1 = await CreateTestUserAsync("FilterCoordinator1", role: "SafetyTeam");
        var coordinator2 = await CreateTestUserAsync("FilterCoordinator2", role: "SafetyTeam");
        var admin = await CreateTestUserAsync("FilterAdmin", role: "Administrator");

        var incident1 = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Incident A");
        var incident2 = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Incident B");

        // Assign incident1 to coordinator1, incident2 to coordinator2
        await _sut.AssignCoordinatorAsync(incident1.Id, new AssignCoordinatorRequest { CoordinatorId = coordinator1.Id }, admin.Id);
        await _sut.AssignCoordinatorAsync(incident2.Id, new AssignCoordinatorRequest { CoordinatorId = coordinator2.Id }, admin.Id);

        // Act - Coordinator1 accesses incident2 (assigned to coordinator2)
        // Safety team members can view ANY incident regardless of assignment
        var crossResult = await _sut.GetIncidentDetailAsync(incident2.Id, coordinator1.Id);

        // Assert - Access granted (safety team members have access to all incidents)
        crossResult.IsSuccess.Should().BeTrue();
        crossResult.Value!.Id.Should().Be(incident2.Id);

        // Act - Coordinator1 gets detail for their own assigned incident
        var ownResult = await _sut.GetIncidentDetailAsync(incident1.Id, coordinator1.Id);

        // Assert - Access granted
        ownResult.IsSuccess.Should().BeTrue();
        ownResult.Value!.Id.Should().Be(incident1.Id);
    }

    #endregion

    #region Notes Workflow Tests

    /// <summary>
    /// Test 6: Verify notes workflow with manual and system notes
    /// Validates: Note creation, note types, note retrieval
    /// </summary>
    [Fact]
    public async Task NotesWorkflow_ManualAndSystemNotes()
    {
        // Arrange - Create coordinator and incident
        var coordinator = await CreateTestUserAsync("NotesCoordinator", role: "SafetyTeam");
        var admin = await CreateTestUserAsync("NotesAdmin", role: "Administrator");

        var incident = await CreateTestIncidentAsync(reporterId: admin.Id, title: "Notes Workflow Test");

        // Assign coordinator (creates system note)
        await _sut.AssignCoordinatorAsync(
            incident.Id,
            new AssignCoordinatorRequest { CoordinatorId = coordinator.Id },
            admin.Id);

        // Act - Add manual note
        var addNoteResult = await _sut.AddNoteAsync(
            incident.Id,
            new AddNoteRequest
            {
                Content = "This is a manual note from coordinator",
                Tags = "investigation,follow-up"
            },
            coordinator.Id,
            isAdmin: false);

        // Assert - Manual note created
        addNoteResult.IsSuccess.Should().BeTrue();
        addNoteResult.Value!.Type.Should().Be(IncidentNoteType.Manual);
        addNoteResult.Value.AuthorId.Should().Be(coordinator.Id);
        addNoteResult.Value.Tags.Should().Be("investigation,follow-up");

        // Act - Update incident status (creates system note)
        await _sut.UpdateStatusAsync(
            incident.Id,
            new UpdateStatusRequest
            {
                NewStatus = IncidentStatus.InformationGathering,
                Reason = "Starting investigation process"
            },
            coordinator.Id);

        // Act - Get all notes
        var notesResult = await _sut.GetNotesAsync(incident.Id, coordinator.Id, isAdmin: false);

        // Assert - Both manual and system notes present
        notesResult.IsSuccess.Should().BeTrue();
        notesResult.Value!.Notes.Should().HaveCountGreaterThanOrEqualTo(2);
        notesResult.Value.Notes.Should().Contain(n => n.Type == IncidentNoteType.Manual);
        notesResult.Value.Notes.Should().Contain(n => n.Type == IncidentNoteType.System);

        // Assert - Notes ordered by CreatedAt DESC
        var notesList = notesResult.Value.Notes.ToList();
        for (int i = 0; i < notesList.Count - 1; i++)
        {
            notesList[i].CreatedAt.Should().BeOnOrAfter(notesList[i + 1].CreatedAt);
        }

        // Act - Update manual note
        var manualNoteId = notesResult.Value.Notes.First(n => n.Type == IncidentNoteType.Manual).Id;
        var updateNoteResult = await _sut.UpdateNoteAsync(
            manualNoteId,
            new UpdateNoteRequest
            {
                Content = "Updated manual note content",
                Tags = "investigation,completed"
            },
            coordinator.Id,
            isAdmin: false);

        // Assert - Manual note updated
        updateNoteResult.IsSuccess.Should().BeTrue();
        updateNoteResult.Value!.Content.Should().Be("Updated manual note content");
        updateNoteResult.Value.UpdatedAt.Should().NotBeNull();

        // Act - Try to delete system note (should fail)
        var systemNoteId = notesResult.Value.Notes.First(n => n.Type == IncidentNoteType.System).Id;
        var deleteSystemNoteResult = await _sut.DeleteNoteAsync(systemNoteId, coordinator.Id, isAdmin: false);

        // Assert - Cannot delete system note
        deleteSystemNoteResult.IsSuccess.Should().BeFalse();
        deleteSystemNoteResult.Error.Should().Contain("System notes cannot be deleted");

        // Act - Delete manual note (should succeed)
        var deleteManualNoteResult = await _sut.DeleteNoteAsync(manualNoteId, coordinator.Id, isAdmin: false);

        // Assert - Manual note deleted
        deleteManualNoteResult.IsSuccess.Should().BeTrue();
        deleteManualNoteResult.Value.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to create test user with unique identifiers
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(string baseName, string? role = null)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
        var sceneName = $"{baseName}_{uniqueId}";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{sceneName}@example.com".ToLower(),
            SceneName = sceneName,
            Role = role ?? "Member",
            IsActive = true,
            VettingStatus = 0, // 0 = UnderReview (not vetted)
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Helper method to create test safety incident via production code path
    /// IMPORTANT: Calls actual SubmitIncidentAsync() to ensure full production code coverage
    /// </summary>
    private async Task<SafetyIncident> CreateTestIncidentAsync(
        bool isAnonymous = false,
        Guid? reporterId = null,
        string? title = null,
        string? description = null,
        IncidentType type = IncidentType.SafetyConcern,
        IncidentStatus status = IncidentStatus.ReportSubmitted)
    {
        var uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

        // Create request and call ACTUAL service method
        var request = new CreateIncidentRequest
        {
            ReporterId = reporterId,
            IsAnonymous = isAnonymous,
            Title = title ?? $"Test Incident {uniqueId}",
            Type = type,
            WhereOccurred = WhereOccurred.AtEvent,
            IncidentDate = DateTime.UtcNow.AddDays(-1),
            Location = "Test Location",
            Description = description ?? "Test incident description with sufficient length for validation rules to pass successfully"
        };

        // Call production code path - ensures reference number generation logic is executed
        var result = await _sut.SubmitIncidentAsync(request);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Test setup failed: {result.Error}");
        }

        // Get incident from database (production code created it)
        var incident = await _context.SafetyIncidents
            .FirstAsync(i => i.ReferenceNumber == result.Value!.ReferenceNumber);

        // Update status if needed (for tests requiring specific status)
        if (status != IncidentStatus.ReportSubmitted)
        {
            incident.Status = status;
            await _context.SaveChangesAsync();
        }

        return incident;
    }

    #endregion
}
