using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using WitchCityRope.Api.Services;
using WitchCityRope.Core.DTOs;

namespace WitchCityRope.Api.Tests.Services;

public class VettingServiceTests
{
    private readonly VettingService _vettingService;

    public VettingServiceTests()
    {
        _vettingService = new VettingService();
    }

    #region SubmitApplicationAsync Tests

    [Fact]
    public async Task SubmitApplicationAsync_WhenCalled_ShouldReturnPendingApplication()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new VettingApplicationRequest
        {
            LegalName = "Test User",
            PreferredName = "Test",
            FetlifeName = "testuser",
            ReferenceOneName = "Ref1",
            ReferenceOneContact = "ref1@example.com",
            ReferenceTwoName = "Ref2",
            ReferenceTwoContact = "ref2@example.com",
            Experience = "5 years of rope experience",
            WhyJoin = "Looking forward to joining and contributing to the community",
            AgreeToCommunityGuidelines = true,
            AgreeToPhotoPolicy = true
        };

        // Act
        var result = await _vettingService.SubmitApplicationAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.ApplicationId.Should().NotBeEmpty();
        result.Status.Should().Be("Pending");
        result.Message.Should().Be("Application submitted successfully");
    }

    [Fact]
    public async Task SubmitApplicationAsync_ShouldGenerateUniqueApplicationIds()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var request = new VettingApplicationRequest
        {
            LegalName = "Test User",
            PreferredName = "Test",
            FetlifeName = "testuser",
            ReferenceOneName = "Ref1",
            ReferenceOneContact = "ref1@example.com",
            ReferenceTwoName = "Ref2",
            ReferenceTwoContact = "ref2@example.com",
            Experience = "Some experience",
            WhyJoin = "Interested in joining the community",
            AgreeToCommunityGuidelines = true,
            AgreeToPhotoPolicy = true
        };

        // Act
        var result1 = await _vettingService.SubmitApplicationAsync(request, userId1);
        var result2 = await _vettingService.SubmitApplicationAsync(request, userId2);

        // Assert
        result1.ApplicationId.Should().NotBe(result2.ApplicationId);
    }

    #endregion

    #region GetApplicationsAsync Tests

    [Fact]
    public async Task GetApplicationsAsync_WhenNoApplications_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var status = "Pending";

        // Act
        var result = await _vettingService.GetApplicationsAsync(status, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task GetApplicationsAsync_WithNullStatus_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 20;

        // Act
        var result = await _vettingService.GetApplicationsAsync(null, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageNumber.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 25)]
    [InlineData(3, 50)]
    public async Task GetApplicationsAsync_ShouldRespectPaginationParameters(int page, int pageSize)
    {
        // Act
        var result = await _vettingService.GetApplicationsAsync("Pending", page, pageSize);

        // Assert
        result.PageNumber.Should().Be(page);
        result.PageSize.Should().Be(pageSize);
    }

    #endregion

    #region ReviewApplicationAsync Tests

    [Fact]
    public async Task ReviewApplicationAsync_WhenApproving_ShouldReturnApprovedStatus()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var request = new ReviewApplicationRequest
        {
            Decision = "Approved",
            Notes = "Great application, welcome to the community!"
        };

        // Act
        var result = await _vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);

        // Assert
        result.Should().NotBeNull();
        result.ApplicationId.Should().Be(applicationId);
        result.Status.Should().Be("Approved");
        result.Message.Should().Be("Application reviewed successfully");
    }

    [Fact]
    public async Task ReviewApplicationAsync_WhenRejecting_ShouldReturnRejectedStatus()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var request = new ReviewApplicationRequest
        {
            Decision = "Rejected",
            Notes = "Insufficient references provided"
        };

        // Act
        var result = await _vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);

        // Assert
        result.Should().NotBeNull();
        result.ApplicationId.Should().Be(applicationId);
        result.Status.Should().Be("Rejected");
        result.Message.Should().Be("Application reviewed successfully");
    }

    [Fact]
    public async Task ReviewApplicationAsync_WhenRequestingMoreInfo_ShouldReturnRequestedInfoStatus()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var request = new ReviewApplicationRequest
        {
            Decision = "MoreInfoRequested",
            Notes = "Please provide additional references"
        };

        // Act
        var result = await _vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);

        // Assert
        result.Should().NotBeNull();
        result.ApplicationId.Should().Be(applicationId);
        result.Status.Should().Be("MoreInfoRequested");
        result.Message.Should().Be("Application reviewed successfully");
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    [InlineData("MoreInfoRequested")]
    [InlineData("OnHold")]
    public async Task ReviewApplicationAsync_ShouldAcceptVariousDecisions(string decision)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var request = new ReviewApplicationRequest
        {
            Decision = decision,
            Notes = "Test notes"
        };

        // Act
        var result = await _vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);

        // Assert
        result.Status.Should().Be(decision);
    }

    #endregion

    #region Edge Cases and Future Implementation Tests

    [Fact]
    public async Task SubmitApplicationAsync_WithMinimalData_ShouldStillSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new VettingApplicationRequest
        {
            LegalName = "User",
            PreferredName = "User",
            FetlifeName = "user",
            ReferenceOneName = "Ref",
            ReferenceOneContact = "ref@example.com",
            ReferenceTwoName = "Ref2",
            ReferenceTwoContact = "ref2@example.com",
            Experience = "Some",
            WhyJoin = "Interested",
            AgreeToCommunityGuidelines = true,
            AgreeToPhotoPolicy = true
        };

        // Act
        var result = await _vettingService.SubmitApplicationAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task ReviewApplicationAsync_WithEmptyNotes_ShouldStillSucceed()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var request = new ReviewApplicationRequest
        {
            Decision = "Approved",
            Notes = string.Empty
        };

        // Act
        var result = await _vettingService.ReviewApplicationAsync(applicationId, request, reviewerId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Approved");
    }

    #endregion
}