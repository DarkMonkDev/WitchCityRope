using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Commands;
using WitchCityRope.Api.Features.Payments.Models.Requests;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Refund endpoints for admin ticket refund operations
/// Follows vertical slice architecture with direct service injection
/// </summary>
public static class RefundEndpoints
{
    /// <summary>
    /// Register refund endpoints using minimal API pattern
    /// </summary>
    public static void MapRefundEndpoints(this IEndpointRouteBuilder app)
    {
        // EXISTING: Admin endpoint for full refund with RSVP cancellation
        app.MapPost("/api/admin/refunds/{ticketId:guid}", RefundTicket.Execute)
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Teacher"))
            .WithName("RefundTicketById")
            .WithSummary("Process PayPal refund for a ticket purchase")
            .WithDescription("Processes a full refund for a PayPal ticket purchase. Optionally removes RSVP. Requires Admin or Teacher role.")
            .WithTags("Admin", "Payments", "Refunds")
            .Produces<RefundTicket.RefundResponse>(200)
            .Produces(400) // Bad request (validation, already refunded, not PayPal)
            .Produces(401) // Unauthorized
            .Produces(403) // Forbidden (wrong role)
            .Produces(404) // Ticket not found
            .Produces(500); // Internal server error (refund failed)

        // NEW: Variable refund endpoint (partial or full, NO RSVP cancellation)
        app.MapPost("/api/payments/transactions/{transactionId:guid}/refund", ProcessVariableRefund.Execute)
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Teacher"))
            .WithName("ProcessVariableRefund")
            .WithSummary("Process variable amount refund for a payment transaction")
            .WithDescription("Processes a partial or full refund for a PayPal payment without canceling RSVP/ticket. Requires Admin or Teacher role.")
            .WithTags("Payments", "Refunds", "Admin")
            .Produces<ProcessVariableRefund.VariableRefundResponse>(200)
            .Produces(400) // Bad request (validation errors, amount exceeds remaining, etc.)
            .Produces(401) // Unauthorized
            .Produces(403) // Forbidden (wrong role)
            .Produces(404) // Transaction not found
            .Produces(500); // Internal server error (refund processing failed)
    }
}
