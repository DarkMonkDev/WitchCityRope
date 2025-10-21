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
                return Results.Json(new ApiResponse<List<VolunteerAssignmentDto>>
                {
                    Success = false,
                    Data = null,
                    Error = "Invalid position ID format",
                    Message = "Position ID must be a valid GUID"
                }, statusCode: 400);
            }

            var (success, assignments, error) = await assignmentService.GetPositionSignupsAsync(
                positionGuid,
                cancellationToken);

            if (success && assignments != null)
            {
                return Results.Ok(new ApiResponse<List<VolunteerAssignmentDto>>
                {
                    Success = true,
                    Data = assignments,
                    Message = assignments.Count == 0
                        ? "No members assigned to this volunteer position"
                        : $"Retrieved {assignments.Count} volunteer assignment(s)"
                });
            }

            var statusCode = error == "Volunteer position not found" ? 404 : 500;

            return Results.Json(new ApiResponse<List<VolunteerAssignmentDto>>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to retrieve volunteer assignments",
                Message = "Unable to retrieve assignments for this position"
            }, statusCode: statusCode);
        })
        .WithName("GetVolunteerPositionSignups")
        .WithSummary("Get members assigned to a volunteer position")
        .WithDescription(
            "Returns list of members currently assigned to a volunteer position with their contact information. " +
            "Requires Admin or SafetyTeam role. Shows scene name, email, FetLife, Discord, and signup status.")
        .WithTags("Volunteer Assignment")
        .Produces<ApiResponse<List<VolunteerAssignmentDto>>>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

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
                return Results.Json(new ApiResponse<VolunteerAssignmentDto>
                {
                    Success = false,
                    Data = null,
                    Error = "Invalid position ID format",
                    Message = "Position ID must be a valid GUID"
                }, statusCode: 400);
            }

            if (request.UserId == Guid.Empty)
            {
                return Results.Json(new ApiResponse<VolunteerAssignmentDto>
                {
                    Success = false,
                    Data = null,
                    Error = "Invalid user ID",
                    Message = "User ID is required and must be a valid GUID"
                }, statusCode: 400);
            }

            var (success, assignment, error) = await assignmentService.AssignMemberToPositionAsync(
                positionGuid,
                request.UserId,
                cancellationToken);

            if (success && assignment != null)
            {
                return Results.Created(
                    $"/api/volunteer-signups/{assignment.SignupId}",
                    new ApiResponse<VolunteerAssignmentDto>
                    {
                        Success = true,
                        Data = assignment,
                        Message = "Successfully assigned member to volunteer position. " +
                                "User has been automatically RSVPed to the event if not already registered."
                    });
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

            return Results.Json(new ApiResponse<VolunteerAssignmentDto>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to assign member to volunteer position",
                Message = "Unable to complete volunteer assignment"
            }, statusCode: statusCode);
        })
        .WithName("AssignMemberToVolunteerPosition")
        .WithSummary("Assign a member to a volunteer position")
        .WithDescription(
            "Assigns a member to a volunteer position. Requires Admin or SafetyTeam role. " +
            "Checks position capacity and existing participations. Auto-RSVPs user to event if needed. " +
            "Returns 409 Conflict if position is full or user already assigned.")
        .WithTags("Volunteer Assignment")
        .Produces<ApiResponse<VolunteerAssignmentDto>>(201)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

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
                return Results.Json(new ApiResponse<object>
                {
                    Success = false,
                    Data = null,
                    Error = "Invalid signup ID format",
                    Message = "Signup ID must be a valid GUID"
                }, statusCode: 400);
            }

            var (success, error) = await assignmentService.RemoveAssignmentAsync(
                signupGuid,
                cancellationToken);

            if (success)
            {
                return Results.Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = null,
                    Message = "Successfully removed member assignment from volunteer position"
                });
            }

            var statusCode = error switch
            {
                "Volunteer signup not found" => 404,
                "Volunteer signup is already cancelled" => 409,
                "Cannot remove assignment after volunteer has checked in" => 409,
                _ => 500
            };

            return Results.Json(new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to remove volunteer assignment",
                Message = "Unable to remove assignment"
            }, statusCode: statusCode);
        })
        .WithName("RemoveVolunteerAssignment")
        .WithSummary("Remove a member assignment from a volunteer position")
        .WithDescription(
            "Removes a member assignment from a volunteer position by cancelling the signup. " +
            "Requires Admin or SafetyTeam role. Only allows removal if volunteer has not checked in yet. " +
            "Updates position slots count. Returns 409 Conflict if already cancelled or checked in.")
        .WithTags("Volunteer Assignment")
        .Produces<ApiResponse<object>>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

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
                return Results.Json(new ApiResponse<List<UserSearchResultDto>>
                {
                    Success = false,
                    Data = null,
                    Error = "Search query is required",
                    Message = "Provide a search query with at least 3 characters"
                }, statusCode: 400);
            }

            var (success, users, error) = await assignmentService.SearchUsersAsync(
                q,
                cancellationToken);

            if (success && users != null)
            {
                return Results.Ok(new ApiResponse<List<UserSearchResultDto>>
                {
                    Success = true,
                    Data = users,
                    Message = users.Count == 0
                        ? $"No active members found matching '{q}'"
                        : $"Found {users.Count} member(s) matching '{q}'"
                });
            }

            var statusCode = error == "Search query must be at least 3 characters" ? 400 : 500;

            return Results.Json(new ApiResponse<List<UserSearchResultDto>>
            {
                Success = false,
                Data = null,
                Error = error ?? "Failed to search users",
                Message = "Unable to complete user search"
            }, statusCode: statusCode);
        })
        .WithName("SearchUsers")
        .WithSummary("Search for active members")
        .WithDescription(
            "Search all active members by scene name, real name, email, or Discord name. " +
            "Requires Admin or SafetyTeam role. Minimum 3 characters required. " +
            "Returns up to 50 results ordered by scene name. Excludes inactive users.")
        .WithTags("Volunteer Assignment", "Users")
        .Produces<ApiResponse<List<UserSearchResultDto>>>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(500);
    }
}
