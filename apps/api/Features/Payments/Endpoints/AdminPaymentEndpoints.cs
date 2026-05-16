using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.Payments.Commands;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Shared.Extensions;

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
                    : Results.Problem( // ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION
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

        // POST /api/admin/payments/refunds/backfill-authnet-transaction-ids
        // ONE-OFF BACKFILL — recovers historical Authorize.net refund transaction ids
        // from the Serilog application log into PaymentRefund rows. Administrator-only
        // and idempotent (only fills NULL columns). See BackfillAuthNetRefundTransactionIds
        // for the full rationale and removal criteria. Safe to leave in place.
        app.MapPost("/api/admin/payments/refunds/backfill-authnet-transaction-ids",
                BackfillAuthNetRefundTransactionIds.Execute)
            .RequireAuthorization(policy => policy.RequireRole(
                UserRole.Administrator.ToRoleString()))
            .WithName("BackfillAuthNetRefundTransactionIds")
            .WithSummary("One-off backfill of historical Authorize.net refund transaction ids (admin only)")
            .WithDescription("Reads the Serilog application log for past Authorize.net refund completions and stores the recovered transaction id on any PaymentRefund rows that are missing it. Idempotent — only fills missing values. Administrator role required.")
            .WithTags("Admin", "Payments")
            .Produces<BackfillAuthNetRefundTransactionIds.BackfillReport>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500);
    }
}
