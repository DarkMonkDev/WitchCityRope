using Microsoft.AspNetCore.Authorization;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Payments.Commands;

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
        // Admin endpoint: Refund ticket by ticketId
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
    }
}
