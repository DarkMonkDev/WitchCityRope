using WitchCityRope.Api.Features.Payments.Commands;
using WitchCityRope.Api.Features.Payments.Models.Requests;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Refund endpoints for admin ticket refund and cancellation operations.
/// Single unified endpoint supports: partial/full refunds, ticket cancellation,
/// and optional RSVP removal — all in one request.
/// Follows vertical slice architecture with direct service injection.
/// </summary>
public static class RefundEndpoints
{
    /// <summary>
    /// Register refund endpoints using minimal API pattern
    /// </summary>
    public static void MapRefundEndpoints(this IEndpointRouteBuilder app)
    {
        // Unified refund + cancellation endpoint
        // Supports: partial refund, full refund, ticket cancellation, RSVP removal
        // SECURITY: CSRF protection enabled automatically via middleware
        // CRITICAL: Financial transactions must validate CSRF tokens to prevent unauthorized refunds
        app.MapPost("/api/payments/transactions/{transactionId:guid}/refund", ProcessVariableRefund.Execute)
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Teacher"))
            .WithName("ProcessVariableRefund")
            .WithSummary("Process refund and/or cancel ticket for a payment transaction")
            .WithDescription("Processes a partial or full refund, optionally cancels the ticket (revokes event access), and optionally removes the RSVP. Supports $0 cancellations (cancel without refund). Requires Admin or Teacher role.")
            .WithTags("Payments", "Refunds", "Admin")
            .Produces<ProcessVariableRefund.VariableRefundResponse>(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500);
    }
}
