using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Vetting.Validators;

namespace WitchCityRope.Api.Features.Vetting.Endpoints;

/// <summary>
/// Vetting System API endpoints
/// Handles application submission, status checking, and reviewer operations
/// Follows WitchCityRope API patterns with Result pattern and proper error handling
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VettingController : ControllerBase
{
    private readonly IVettingService _vettingService;
    private readonly IValidator<CreateApplicationRequest> _applicationValidator;
    private readonly ILogger<VettingController> _logger;

    public VettingController(
        IVettingService vettingService,
        IValidator<CreateApplicationRequest> applicationValidator,
        ILogger<VettingController> logger)
    {
        _vettingService = vettingService;
        _applicationValidator = applicationValidator;
        _logger = logger;
    }

    /// <summary>
    /// Submit new vetting application (public endpoint)
    /// </summary>
    [HttpPost("applications")]
    [ProducesResponseType(typeof(ApplicationSubmissionResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> SubmitApplicationAsync(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received application submission request");

            // Validate request
            var validationResult = await _applicationValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new { 
                    Field = e.PropertyName, 
                    Error = e.ErrorMessage 
                }).ToList();

                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "One or more validation errors occurred",
                    Status = 400,
                    Extensions = { { "errors", errors } }
                });
            }

            // Process application
            var result = await _vettingService.SubmitApplicationAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Application submitted successfully: {ApplicationNumber}", 
                    result.Value?.ApplicationNumber);
                return Ok(result.Value);
            }

            _logger.LogWarning("Application submission failed: {Error}", result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = result.Error,
                Detail = result.Details,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during application submission");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while processing your application",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Check application status using secure token (public endpoint)
    /// </summary>
    [HttpGet("applications/status/{statusToken}")]
    [ProducesResponseType(typeof(ApplicationStatusResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetApplicationStatusAsync(
        [FromRoute] string statusToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(statusToken))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid status token",
                    Detail = "Status token is required",
                    Status = 400
                });
            }

            var result = await _vettingService.GetApplicationStatusAsync(statusToken, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Application status lookup failed: {Error}", result.Error);
            return NotFound(new ProblemDetails
            {
                Title = result.Error,
                Detail = result.Details,
                Status = 404
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during status lookup for token {StatusToken}", statusToken);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while checking application status",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get applications for reviewer dashboard (requires VettingReviewer role)
    /// </summary>
    [HttpPost("reviewer/applications")]
    [Authorize(Roles = "VettingReviewer,VettingAdmin")]
    [ProducesResponseType(typeof(PagedResult<ApplicationSummaryDto>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetApplicationsForReviewAsync(
        [FromBody] ApplicationFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get reviewer ID from claims (assumes JWT contains reviewer information)
            var reviewerIdClaim = User.FindFirst("ReviewerId")?.Value;
            if (string.IsNullOrEmpty(reviewerIdClaim) || !Guid.TryParse(reviewerIdClaim, out var reviewerId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid reviewer",
                    Detail = "Reviewer information not found in authentication token",
                    Status = 400
                });
            }

            // Validate pagination
            if (filter.Page < 1)
                filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100)
                filter.PageSize = 25;

            var result = await _vettingService.GetApplicationsForReviewAsync(reviewerId, filter, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            _logger.LogWarning("Applications query failed for reviewer {ReviewerId}: {Error}", reviewerId, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = result.Error,
                Detail = result.Details,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during applications query");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while retrieving applications",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Get full application details for review (requires VettingReviewer role)
    /// </summary>
    [HttpGet("reviewer/applications/{applicationId}")]
    [Authorize(Roles = "VettingReviewer,VettingAdmin")]
    [ProducesResponseType(typeof(ApplicationDetailResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> GetApplicationDetailAsync(
        [FromRoute] Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get reviewer ID from claims
            var reviewerIdClaim = User.FindFirst("ReviewerId")?.Value;
            if (string.IsNullOrEmpty(reviewerIdClaim) || !Guid.TryParse(reviewerIdClaim, out var reviewerId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid reviewer",
                    Detail = "Reviewer information not found in authentication token",
                    Status = 400
                });
            }

            var result = await _vettingService.GetApplicationDetailAsync(applicationId, reviewerId, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            var statusCode = result.Error switch
            {
                "Application not found" => 404,
                "Access denied" => 403,
                _ => 400
            };

            return StatusCode(statusCode, new ProblemDetails
            {
                Title = result.Error,
                Detail = result.Details,
                Status = statusCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving application {ApplicationId}", applicationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while retrieving application details",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Submit review decision (requires VettingReviewer role)
    /// </summary>
    [HttpPost("reviewer/applications/{applicationId}/decisions")]
    [Authorize(Roles = "VettingReviewer,VettingAdmin")]
    [ProducesResponseType(typeof(ReviewDecisionResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> SubmitReviewDecisionAsync(
        [FromRoute] Guid applicationId,
        [FromBody] ReviewDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get reviewer ID from claims
            var reviewerIdClaim = User.FindFirst("ReviewerId")?.Value;
            if (string.IsNullOrEmpty(reviewerIdClaim) || !Guid.TryParse(reviewerIdClaim, out var reviewerId))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid reviewer",
                    Detail = "Reviewer information not found in authentication token",
                    Status = 400
                });
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Reasoning))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = "Decision reasoning is required",
                    Status = 400
                });
            }

            var result = await _vettingService.SubmitReviewDecisionAsync(applicationId, request, reviewerId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Review decision submitted for application {ApplicationId} by reviewer {ReviewerId}", 
                    applicationId, reviewerId);
                return Ok(result.Value);
            }

            var statusCode = result.Error switch
            {
                "Application not found" => 404,
                "Access denied" => 403,
                _ => 400
            };

            return StatusCode(statusCode, new ProblemDetails
            {
                Title = result.Error,
                Detail = result.Details,
                Status = statusCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error submitting decision for application {ApplicationId}", applicationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An unexpected error occurred while submitting the decision",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(200)]
    public IActionResult HealthCheck()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}