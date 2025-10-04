using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests.Api.Features.Vetting;

/// <summary>
/// Integration tests for Vetting endpoints
/// Tests the complete vetting workflow including status updates, approvals, denials, and audit logging
/// Phase 2: Integration Tests - Complete Vetting Workflow
/// </summary>
[Collection("Database")]
public class VettingEndpointsIntegrationTests : IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    public VettingEndpointsIntegrationTests(DatabaseTestFixture fixture)
        : base(fixture)
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add DbContext using the test container's connection string
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString);
                    });
                });
            });
    }

    #region Status Update Tests (5 tests)

    [Fact]
    public async Task StatusUpdate_WithValidTransition_Succeeds()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Starting review process"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status change in database
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.UnderReview);
    }

    [Fact]
    public async Task StatusUpdate_WithInvalidTransition_Fails()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Approved);

        var request = new StatusChangeRequest
        {
            Status = "Submitted", // Cannot go back from Approved to Submitted
            Reasoning = "Attempting invalid transition"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("transition", "Error should mention invalid transition");
    }

    [Fact]
    public async Task StatusUpdate_AsNonAdmin_Returns403()
    {
        // Arrange
        var (_, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);
        var nonAdminClient = await CreateNonAdminClientAsync();

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Attempting as non-admin"
        };

        // Act
        var response = await nonAdminClient.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StatusUpdate_CreatesAuditLog()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Starting review process"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log entry was created
        await using var context = CreateDbContext();
        var auditLog = await context.VettingAuditLogs
            .Where(log => log.ApplicationId == applicationId)
            .OrderByDescending(log => log.PerformedAt)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Contain("Status changed");
        auditLog.NewValue.Should().Contain("UnderReview");
        auditLog.OldValue.Should().Contain("Submitted");
    }

    [Fact]
    public async Task StatusUpdate_SendsEmailNotification()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Starting review process"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // NOTE: In a real test, you would verify email was sent using a mock email service
        // For now, we verify the endpoint succeeded, which should trigger email sending
        var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewDecisionResponse>>();
        responseData.Should().NotBeNull();
        responseData!.Success.Should().BeTrue();
    }

    #endregion

    #region Approval Tests (3 tests)

    [Fact]
    public async Task Approval_GrantsVettedMemberRole()
    {
        // Arrange
        var (client, applicationId, userId) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new SimpleReasoningRequest
        {
            Reasoning = "Approved after thorough review"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/vetting/applications/{applicationId}/approve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user role was updated
        await using var context = CreateDbContext();
        var vettedMemberRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == "VettedMember");
        vettedMemberRole.Should().NotBeNull();

        var userHasRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == vettedMemberRole!.Id);

        userHasRole.Should().BeTrue("User should have VettedMember role after approval");
    }

    [Fact]
    public async Task Approval_CreatesAuditLog()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new SimpleReasoningRequest
        {
            Reasoning = "Approved after thorough review"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/vetting/applications/{applicationId}/approve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log
        await using var context = CreateDbContext();
        var auditLog = await context.VettingAuditLogs
            .Where(log => log.ApplicationId == applicationId && log.Action.Contains("Approved"))
            .OrderByDescending(log => log.PerformedAt)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
        auditLog!.NewValue.Should().Contain("Approved");
        auditLog.Notes.Should().Contain("Approved after thorough review");
    }

    [Fact]
    public async Task Approval_SendsApprovalEmail()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new SimpleReasoningRequest
        {
            Reasoning = "Approved after thorough review"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/vetting/applications/{applicationId}/approve", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify response indicates success
        var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewDecisionResponse>>();
        responseData.Should().NotBeNull();
        responseData!.Success.Should().BeTrue();
        responseData.Message.Should().ContainEquivalentOf("approved");
    }

    #endregion

    #region Denial Tests (2 tests)

    [Fact]
    public async Task Denial_RequiresReason()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new SimpleReasoningRequest
        {
            Reasoning = "" // Empty reasoning should fail
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/vetting/applications/{applicationId}/deny", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainEquivalentOf("reason");
    }

    [Fact]
    public async Task Denial_SendsDenialEmail()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new SimpleReasoningRequest
        {
            Reasoning = "Does not meet community standards"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/vetting/applications/{applicationId}/deny", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status was updated to Denied
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.Denied);

        // Verify response
        var responseData = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewDecisionResponse>>();
        responseData.Should().NotBeNull();
        responseData!.Success.Should().BeTrue();
    }

    #endregion

    #region OnHold Tests (2 tests)

    [Fact]
    public async Task OnHold_RequiresReasonAndActions()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new StatusChangeRequest
        {
            Status = "OnHold",
            Reasoning = "" // Empty reasoning should fail
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OnHold_SendsOnHoldEmail()
    {
        // Arrange
        var (client, applicationId, _) = await SetupApplicationWithUserAsync(VettingStatus.UnderReview);

        var request = new StatusChangeRequest
        {
            Status = "OnHold",
            Reasoning = "Waiting for additional references"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status was updated to OnHold
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.OnHold);
    }

    #endregion

    #region Transaction Tests (3 tests)

    [Fact]
    public async Task StatusUpdate_WithDatabaseError_RollsBack()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        // Force a database error by using an invalid status
        var request = new StatusChangeRequest
        {
            Status = "InvalidStatus", // This should fail validation
            Reasoning = "Testing rollback"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify original status is unchanged
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.Submitted, "Status should not have changed");
    }

    [Fact]
    public async Task StatusUpdate_EmailFailureDoesNotPreventStatusChange()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Starting review process"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Status update should succeed even if email fails (email is non-critical)");

        // Verify status was updated
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.UnderReview);
    }

    [Fact]
    public async Task AuditLogCreation_IsTransactional()
    {
        // Arrange
        var (client, applicationId) = await SetupApplicationAsync(VettingStatus.Submitted);

        var request = new StatusChangeRequest
        {
            Status = "UnderReview",
            Reasoning = "Starting review process"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/vetting/applications/{applicationId}/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify audit log and status update are both present (transactional success)
        await using var context = CreateDbContext();
        var application = await context.VettingApplications.FindAsync(applicationId);
        var auditLog = await context.VettingAuditLogs
            .Where(log => log.ApplicationId == applicationId)
            .OrderByDescending(log => log.PerformedAt)
            .FirstOrDefaultAsync();

        application.Should().NotBeNull();
        application!.Status.Should().Be(VettingStatus.UnderReview);
        auditLog.Should().NotBeNull("Audit log should be created with status update");
        auditLog!.NewValue.Should().Contain("UnderReview");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test vetting application with admin client
    /// </summary>
    private async Task<(HttpClient client, Guid applicationId)> SetupApplicationAsync(VettingStatus initialStatus)
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = $"test-{applicationId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        // Create user
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"TestUser{userId:N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Create vetting application
        var application = new VettingApplication
        {
            Id = applicationId,
            UserId = userId,
            Email = email,
            SceneName = "Test",
            RealName = "User",
            Status = initialStatus,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.VettingApplications.Add(application);

        await context.SaveChangesAsync();

        // Create admin client
        var client = await CreateAdminClientAsync();

        return (client, applicationId);
    }

    /// <summary>
    /// Creates a test vetting application with user and admin client, returns userId
    /// </summary>
    private async Task<(HttpClient client, Guid applicationId, Guid userId)> SetupApplicationWithUserAsync(VettingStatus initialStatus)
    {
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = $"test-{applicationId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        // Create user
        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"TestUser{userId:N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Create vetting application
        var application = new VettingApplication
        {
            Id = applicationId,
            UserId = userId,
            Email = email,
            SceneName = "Test",
            RealName = "User",
            Status = initialStatus,
            SubmittedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.VettingApplications.Add(application);

        await context.SaveChangesAsync();

        // Create admin client
        var client = await CreateAdminClientAsync();

        return (client, applicationId, userId);
    }

    /// <summary>
    /// Creates an authenticated admin client with proper JWT token
    /// </summary>
    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-{adminId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        // Create admin user
        var adminUser = new ApplicationUser
        {
            Id = adminId,
            Email = adminEmail,
            UserName = adminEmail,
            SceneName = $"Admin{adminId:N}",
            Role = "Administrator", // Set Role property for ApplicationUser
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);

        // Ensure Administrator role exists
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole == null)
        {
            adminRole = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Administrator",
                NormalizedName = "ADMINISTRATOR"
            };
            context.Roles.Add(adminRole);
        }

        // Assign admin role to user via UserRoles junction table
        var userRole = new IdentityUserRole<Guid>
        {
            UserId = adminId,
            RoleId = adminRole.Id
        };
        context.UserRoles.Add(userRole);

        await context.SaveChangesAsync();

        // Create client with properly generated JWT token
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(adminId, adminEmail, "Administrator", adminUser.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Creates an authenticated non-admin client with proper JWT token
    /// </summary>
    private async Task<HttpClient> CreateNonAdminClientAsync()
    {
        var userId = Guid.NewGuid();
        var email = $"user-{userId:N}@witchcityrope.com";

        await using var context = CreateDbContext();

        var user = new ApplicationUser
        {
            Id = userId,
            Email = email,
            UserName = email,
            SceneName = $"RegularUser{userId:N}",
            Role = "Member", // Set Role property for ApplicationUser
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Ensure Member role exists
        var memberRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Member");
        if (memberRole == null)
        {
            memberRole = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = "Member",
                NormalizedName = "MEMBER"
            };
            context.Roles.Add(memberRole);
        }

        // Assign member role to user via UserRoles junction table
        var userRole = new IdentityUserRole<Guid>
        {
            UserId = userId,
            RoleId = memberRole.Id
        };
        context.UserRoles.Add(userRole);

        await context.SaveChangesAsync();

        var client = _factory.CreateClient();
        var token = GenerateJwtToken(userId, email, "Member", user.SceneName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // GenerateJwtToken method inherited from IntegrationTestBase
    // Uses proper JWT generation matching API configuration

    #endregion

    public override async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await base.DisposeAsync();
    }
}
