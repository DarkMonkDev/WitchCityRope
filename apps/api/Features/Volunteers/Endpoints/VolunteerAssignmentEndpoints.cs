using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Volunteers.Services;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Volunteers.Endpoints;

/// <summary>
/// Admin/Safety Team volunteer position assignment endpoints
/// For managing volunteer assignments, viewing signups, and searching members
/// </summary>
public static class VolunteerAssignmentEndpoints
{
    /// <summary>
    /// Register volunteer assignment endpoints using minimal API pattern
    /// All endpoints require Admin or SafetyTeam roles
    /// </summary>
    public static void MapVolunteerAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/volunteer-positions/{positionId}/signups
        // Returns list of members currently assigned to a volunteer position
        app.MapGet("/api/volunteer-positions/{positionId}/signups",
            [Authorize(Roles = "Administrator,SafetyTeam")] async (
                string positionId,
                VolunteerAssignmentService assignmentService,
                CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(positionId, out var positionGuid))
            {
                return Results.Problem(
                    title: "Invalid Position ID Format",
                    detail: "Position ID must be a valid GUID",
                    statusCode: 400);
            }

            var (success, assignments, error) = await assignmentService.GetPositionSignupsAsync(
                positionGuid,
                cancellationToken);

            if (success && assignments != null)
            {
                return Results.Ok(assignments);
            }

            var statusCode = error == "Volunteer position not found" ? 404 : 500;

            return Results.Problem(
                title: "Failed to Retrieve Volunteer Assignments",
                detail: error ?? "Failed to retrieve volunteer assignments",
                statusCode: statusCode);
        })
        .WithName("GetVolunteerPositionSignups")
        .WithSummary("Get members assigned to a volunteer position")
        .WithDescription(
            "Returns list of members currently assigned to a volunteer position with their contact information. " +
            "Requires Admin or SafetyTeam role. Shows scene name, email, FetLife, Discord, and signup status.")
        .WithTags("Volunteer Assignment")
        .Produces<List<VolunteerAssignmentDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // POST /api/volunteer-positions/{positionId}/signups
        // Assigns a member to a volunteer position
        app.MapPost("/api/volunteer-positions/{positionId}/signups",
            [Authorize(Roles = "Administrator,SafetyTeam")] async (
                string positionId,
                AssignVolunteerRequest request,
                VolunteerAssignmentService assignmentService,
                CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(positionId, out var positionGuid))
            {
                return Results.Problem(
                    title: "Invalid Position ID Format",
                    detail: "Position ID must be a valid GUID",
                    statusCode: 400);
            }

            if (request.UserId == Guid.Empty)
            {
                return Results.Problem(
                    title: "Invalid User ID",
                    detail: "User ID is required and must be a valid GUID",
                    statusCode: 400);
            }

            var (success, assignment, error) = await assignmentService.AssignMemberToPositionAsync(
                positionGuid,
                request.UserId,
                cancellationToken);

            if (success && assignment != null)
            {
                return Results.Created(
                    $"/api/volunteer-signups/{assignment.SignupId}",
                    assignment);
            }

            var statusCode = error switch
            {
                "Volunteer position not found" => 404,
                "User not found" => 404,
                "User is already assigned to this volunteer position" => 409,
                "Volunteer position is already fully staffed" => 409,
                "Cannot assign inactive user to volunteer position" => 400,
                _ => 500
            };

            return Results.Problem(
                title: "Failed to Assign Member",
                detail: error ?? "Failed to assign member to volunteer position",
                statusCode: statusCode);
        })
        .WithName("AssignMemberToVolunteerPosition")
        .WithSummary("Assign a member to a volunteer position")
        .WithDescription(
            "Assigns a member to a volunteer position. Requires Admin or SafetyTeam role. " +
            "Checks position capacity and existing participations. Auto-RSVPs user to event if needed. " +
            "Returns 409 Conflict if position is full or user already assigned.")
        .WithTags("Volunteer Assignment")
        .Produces<VolunteerAssignmentDto>(201)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(500);

        // DELETE /api/volunteer-signups/{signupId}
        // Removes a member assignment from a volunteer position
        app.MapDelete("/api/volunteer-signups/{signupId}",
            [Authorize(Roles = "Administrator,SafetyTeam")] async (
                string signupId,
                VolunteerAssignmentService assignmentService,
                CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(signupId, out var signupGuid))
            {
                return Results.Problem(
                    title: "Invalid Signup ID Format",
                    detail: "Signup ID must be a valid GUID",
                    statusCode: 400);
            }

            var (success, error) = await assignmentService.RemoveAssignmentAsync(
                signupGuid,
                cancellationToken);

            if (success)
            {
                return Results.NoContent();
            }

            var statusCode = error switch
            {
                "Volunteer signup not found" => 404,
                "Volunteer signup is already cancelled" => 409,
                "Cannot remove assignment after volunteer has checked in" => 409,
                _ => 500
            };

            return Results.Problem(
                title: "Failed to Remove Volunteer Assignment",
                detail: error ?? "Failed to remove volunteer assignment",
                statusCode: statusCode);
        })
        .WithName("RemoveVolunteerAssignment")
        .WithSummary("Remove a member assignment from a volunteer position")
        .WithDescription(
            "Removes a member assignment from a volunteer position by cancelling the signup. " +
            "Requires Admin or SafetyTeam role. Only allows removal if volunteer has not checked in yet. " +
            "Updates position slots count. Returns 409 Conflict if already cancelled or checked in.")
        .WithTags("Volunteer Assignment")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(500);

        // GET /api/users/search?q={searchQuery}
        // Search for active members to assign to positions
        app.MapGet("/api/users/search",
            [Authorize(Roles = "Administrator,SafetyTeam")] async (
                string? q,
                VolunteerAssignmentService assignmentService,
                CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Results.Problem(
                    title: "Search Query Required",
                    detail: "Provide a search query with at least 3 characters",
                    statusCode: 400);
            }

            var (success, users, error) = await assignmentService.SearchUsersAsync(
                q,
                cancellationToken);

            if (success && users != null)
            {
                return Results.Ok(users);
            }

            var statusCode = error == "Search query must be at least 3 characters" ? 400 : 500;

            return Results.Problem(
                title: "Failed to Search Users",
                detail: error ?? "Failed to search users",
                statusCode: statusCode);
        })
        .WithName("SearchUsers")
        .WithSummary("Search for active members")
        .WithDescription(
            "Search all active members by scene name, real name, email, or Discord name. " +
            "Requires Admin or SafetyTeam role. Minimum 3 characters required. " +
            "Returns up to 50 results ordered by scene name. Excludes inactive users.")
        .WithTags("Volunteer Assignment", "Users")
        .Produces<List<UserSearchResultDto>>(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(500);
    }
}
