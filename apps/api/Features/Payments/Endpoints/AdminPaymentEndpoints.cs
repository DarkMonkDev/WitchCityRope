using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Users.Constants;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Admin payment management endpoints using minimal API pattern
/// </summary>
public static class AdminPaymentEndpoints
{
    /// <summary>
    /// Register admin payment endpoints
    /// </summary>
    public static void MapAdminPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/admin/payments - List all payment transactions with filtering
        app.MapGet("/api/admin/payments", async (
            [AsParameters] PaymentListQueryParameters parameters,
            IPaymentListService paymentListService,
            CancellationToken cancellationToken) =>
            {
                var (success, response, error) = await paymentListService.GetPaymentListAsync(parameters, cancellationToken);

                return success
                    ? Results.Ok(response)
                    : Results.Problem(
                        title: "Get Payments Failed",
                        detail: error,
                        statusCode: 500);
            })
            .RequireAuthorization(policy => policy.RequireRole(
                UserRole.Administrator.ToRoleString(),
                UserRole.Teacher.ToRoleString()))
            .WithName("GetAdminPayments")
            .WithSummary("Get paginated list of payment transactions (admin/teacher only)")
            .WithDescription("Returns a paginated list of payment transactions with filtering by search term, date range, payment methods, statuses, and amount range. Supports sorting and pagination.")
            .WithTags("Admin", "Payments")
            .Produces<PaymentListResponse>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);
    }
}
