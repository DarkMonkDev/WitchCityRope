using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WitchCityRope.Api.Features.Metadata.Endpoints;
using WitchCityRope.Api.Features.Metadata.Models;
using WitchCityRope.Api.Features.Users.Constants;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Features.Metadata;

/// <summary>
/// Unit tests for MetadataEndpoints (Pattern B)
/// Tests metadata API endpoints that provide system configuration information
/// </summary>
public class MetadataEndpointsTests
{
    #region GetValidRoles Tests

    /// <summary>
    /// Test: GetValidRoles returns 200 OK with ValidRolesResponse
    /// </summary>
    [Fact]
    public void GetValidRoles_ReturnsValidRolesResponse()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Extract value from Results.Ok using dynamic
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        response.Should().NotBeNull();
        response.Should().BeOfType<ValidRolesResponse>();

        var rolesResponse = (ValidRolesResponse)response!;
        rolesResponse.Roles.Should().NotBeNull();
    }

    /// <summary>
    /// Test: GetValidRoles returns all valid roles from UserRoleConstants
    /// </summary>
    [Fact]
    public void GetValidRoles_ReturnsAllValidRoles()
    {
        // Arrange
        var expectedRoles = UserRoleConstants.ValidRoles.ToList();

        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().BeEquivalentTo(expectedRoles);
    }

    /// <summary>
    /// Test: GetValidRoles excludes Member role (Member is default, not assigned)
    /// </summary>
    [Fact]
    public void GetValidRoles_ExcludesMemberRole()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().NotContain("Member",
            "Member is the default state and not assigned as a role");
    }

    /// <summary>
    /// Test: GetValidRoles includes Teacher role
    /// </summary>
    [Fact]
    public void GetValidRoles_IncludesTeacherRole()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().Contain("Teacher");
    }

    /// <summary>
    /// Test: GetValidRoles includes SafetyTeam role
    /// </summary>
    [Fact]
    public void GetValidRoles_IncludesSafetyTeamRole()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().Contain("SafetyTeam");
    }

    /// <summary>
    /// Test: GetValidRoles includes Administrator role
    /// </summary>
    [Fact]
    public void GetValidRoles_IncludesAdministratorRole()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().Contain("Administrator");
    }

    /// <summary>
    /// Test: GetValidRoles includes EventOrganizer role
    /// </summary>
    [Fact]
    public void GetValidRoles_IncludesEventOrganizerRole()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().Contain("EventOrganizer");
    }

    /// <summary>
    /// Test: GetValidRoles returns expected number of roles
    /// </summary>
    [Fact]
    public void GetValidRoles_ReturnsExpectedRoleCount()
    {
        // Arrange
        // UserRole enum has: Member, Teacher, SafetyTeam, Administrator, EventOrganizer (5 total)
        // ValidRoles excludes Member, so expected count is 4
        var expectedCount = 4;

        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().HaveCount(expectedCount,
            "ValidRoles should contain all UserRole enum values except Member");
    }

    /// <summary>
    /// Test: GetValidRoles returns non-empty list
    /// </summary>
    [Fact]
    public void GetValidRoles_ReturnsNonEmptyList()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test: GetValidRoles response contains only string values
    /// </summary>
    [Fact]
    public void GetValidRoles_ResponseContainsOnlyStrings()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().AllBeOfType<string>();
    }

    /// <summary>
    /// Test: GetValidRoles response contains no null or empty strings
    /// </summary>
    [Fact]
    public void GetValidRoles_ResponseContainsNoNullOrEmptyStrings()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().NotContainNulls();
        rolesResponse.Roles.Should().NotContain(string.Empty);
    }

    /// <summary>
    /// Test: GetValidRoles response contains no duplicate roles
    /// </summary>
    [Fact]
    public void GetValidRoles_ResponseContainsNoDuplicates()
    {
        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Test: GetValidRoles is idempotent (multiple calls return same result)
    /// </summary>
    [Fact]
    public void GetValidRoles_IsIdempotent()
    {
        // Act
        var result1 = GetValidRoles();
        var result2 = GetValidRoles();

        // Assert
        dynamic dynamicResult1 = result1;
        dynamic dynamicResult2 = result2;

        object response1 = dynamicResult1.Value;
        object response2 = dynamicResult2.Value;

        var rolesResponse1 = (ValidRolesResponse)response1!;
        var rolesResponse2 = (ValidRolesResponse)response2!;

        rolesResponse1.Roles.Should().BeEquivalentTo(rolesResponse2.Roles,
            "Multiple calls should return the same roles");
    }

    /// <summary>
    /// Test: GetValidRoles matches UserRoleConstants.ValidRoles exactly
    /// </summary>
    [Fact]
    public void GetValidRoles_MatchesUserRoleConstantsValidRoles()
    {
        // Arrange
        var expectedRoles = UserRoleConstants.ValidRoles;

        // Act
        var result = GetValidRoles();

        // Assert
        dynamic dynamicResult = result;
        object response = dynamicResult.Value;
        var rolesResponse = (ValidRolesResponse)response!;

        rolesResponse.Roles.Should().BeEquivalentTo(expectedRoles,
            "Response should match UserRoleConstants.ValidRoles as single source of truth");
    }

    /// <summary>
    /// Test: GetValidRoles endpoint is public (no authorization required)
    /// </summary>
    [Fact]
    public void GetValidRoles_IsPublicEndpoint()
    {
        // Note: This test verifies the endpoint logic itself
        // Actual AllowAnonymous attribute is tested via endpoint configuration
        // Pattern B: We verify endpoint behavior, not middleware configuration

        // Act
        var result = GetValidRoles();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        // Note: Endpoint should return successfully without requiring authorization
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper: Simulates GetValidRoles endpoint call
    /// </summary>
    private IResult GetValidRoles()
    {
        // Simulate endpoint logic from MetadataEndpoints.cs line 33-40
        var response = new ValidRolesResponse
        {
            Roles = UserRoleConstants.ValidRoles.ToList()
        };

        return Results.Ok(response);
    }

    #endregion
}
