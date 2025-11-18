using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Service for retrieving and filtering payment transaction lists for admin
/// </summary>
public interface IPaymentListService
{
    /// <summary>
    /// Get filtered and paginated list of payment transactions
    /// </summary>
    Task<(bool Success, PaymentListResponse? Response, string Error)> GetPaymentListAsync(
        PaymentListQueryParameters parameters,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of payment list service
/// </summary>
public class PaymentListService : IPaymentListService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentListService> _logger;

    public PaymentListService(
        ApplicationDbContext db,
        ILogger<PaymentListService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, PaymentListResponse? Response, string Error)> GetPaymentListAsync(
        PaymentListQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Start with base query - join all necessary entities
            var query = _db.Set<Entities.Payment>()
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Refunds)
                .Join(
                    _db.Set<TicketPurchase>(),
                    payment => payment.EventRegistrationId,
                    ticket => ticket.Id,
                    (payment, ticket) => new { Payment = payment, Ticket = ticket })
                .Join(
                    _db.Set<TicketType>(),
                    pt => pt.Ticket.TicketTypeId,
                    ticketType => ticketType.Id,
                    (pt, ticketType) => new { pt.Payment, pt.Ticket, TicketType = ticketType })
                .Join(
                    _db.Set<Event>(),
                    ptt => ptt.TicketType.EventId,
                    evt => evt.Id,
                    (ptt, evt) => new { ptt.Payment, ptt.Ticket, ptt.TicketType, Event = evt })
                .GroupJoin(
                    _db.Set<Session>(),
                    ptte => ptte.TicketType.SessionId,
                    session => session.Id,
                    (ptte, sessions) => new { ptte.Payment, ptte.Ticket, ptte.TicketType, ptte.Event, Session = sessions.FirstOrDefault() });

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(x =>
                    (x.Payment.User != null && (
                        (x.Payment.User.SceneName != null && x.Payment.User.SceneName.ToLower().Contains(searchTerm)) ||
                        (x.Payment.User.FirstName != null && x.Payment.User.FirstName.ToLower().Contains(searchTerm)) ||
                        (x.Payment.User.LastName != null && x.Payment.User.LastName.ToLower().Contains(searchTerm)) ||
                        (x.Payment.User.Email != null && x.Payment.User.Email.ToLower().Contains(searchTerm))
                    )) ||
                    x.Event.Title.ToLower().Contains(searchTerm) ||
                    (x.Session != null && x.Session.Name.ToLower().Contains(searchTerm)) ||
                    x.Payment.Id.ToString().ToLower().Contains(searchTerm)
                );
            }

            // Apply date range filter
            if (parameters.StartDate.HasValue)
            {
                var startDate = parameters.StartDate.Value.ToUniversalTime();
                query = query.Where(x => x.Payment.ProcessedAt >= startDate);
            }

            if (parameters.EndDate.HasValue)
            {
                var endDate = parameters.EndDate.Value.ToUniversalTime().AddDays(1); // Include full end date
                query = query.Where(x => x.Payment.ProcessedAt < endDate);
            }

            // Apply payment method filter
            if (!string.IsNullOrWhiteSpace(parameters.PaymentMethods))
            {
                var methods = parameters.PaymentMethods
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(m => m.ToUpper())
                    .ToList();
                query = query.Where(x => methods.Contains(x.Payment.PaymentMethodType.ToString().ToUpper()));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(parameters.Statuses))
            {
                var statuses = parameters.Statuses
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpper())
                    .ToList();
                query = query.Where(x => statuses.Contains(x.Payment.Status.ToString().ToUpper()));
            }

            // Apply amount range filter
            if (parameters.MinAmount.HasValue)
            {
                query = query.Where(x => x.Payment.AmountValue >= parameters.MinAmount.Value);
            }

            if (parameters.MaxAmount.HasValue)
            {
                query = query.Where(x => x.Payment.AmountValue <= parameters.MaxAmount.Value);
            }

            // Apply sorting before count (for consistency)
            var sortBy = parameters.SortBy?.ToLower() ?? "paymentdate";
            var isDescending = parameters.SortDirection?.Equals("Desc", StringComparison.OrdinalIgnoreCase) ?? true;

            query = sortBy switch
            {
                "amount" => isDescending
                    ? query.OrderByDescending(x => x.Payment.AmountValue)
                    : query.OrderBy(x => x.Payment.AmountValue),
                "username" => isDescending
                    ? query.OrderByDescending(x => x.Payment.User!.SceneName ?? x.Payment.User.Email)
                    : query.OrderBy(x => x.Payment.User!.SceneName ?? x.Payment.User.Email),
                "status" => isDescending
                    ? query.OrderByDescending(x => x.Payment.Status)
                    : query.OrderBy(x => x.Payment.Status),
                _ => isDescending // Default: PaymentDate
                    ? query.OrderByDescending(x => x.Payment.ProcessedAt ?? x.Payment.CreatedAt)
                    : query.OrderBy(x => x.Payment.ProcessedAt ?? x.Payment.CreatedAt)
            };

            // Get total count after filtering but before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var skip = (parameters.Page - 1) * parameters.PageSize;
            query = query.Skip(skip).Take(parameters.PageSize);

            // Execute query and map to DTOs
            var results = await query
                .Select(x => new PaymentTransactionDto
                {
                    Id = x.Payment.Id,
                    TicketId = x.Ticket.Id,
                    PaymentDate = x.Payment.ProcessedAt ?? x.Payment.CreatedAt,

                    // User info - prefer SceneName over FirstName/LastName over Email
                    UserName = !string.IsNullOrEmpty(x.Payment.User!.SceneName)
                        ? x.Payment.User.SceneName
                        : !string.IsNullOrEmpty(x.Payment.User.FirstName)
                            ? $"{x.Payment.User.FirstName} {x.Payment.User.LastName}".Trim()
                            : x.Payment.User.Email ?? "Unknown",
                    UserEmail = x.Payment.User!.Email ?? string.Empty,

                    // Event info
                    EventName = x.Event.Title,
                    SessionName = x.Session != null ? x.Session.Name : null,

                    // Payment details
                    PaymentMethod = x.Payment.PaymentMethodType.ToString(),
                    Amount = x.Payment.AmountValue,
                    Currency = x.Payment.Currency,
                    Status = x.Payment.Status.ToString(),

                    // Refund info - inline check for refundability
                    IsRefundable = x.Payment.PaymentMethodType == PaymentMethodType.PayPal
                                   && x.Payment.Status == PaymentStatus.Completed
                                   && x.Payment.ProcessedAt != null
                                   && !x.Payment.Refunds.Any(),
                    RefundId = x.Payment.Refunds.Any() ? x.Payment.Refunds.First().Id : (Guid?)null,
                    RefundDate = x.Payment.RefundedAt
                })
                .ToListAsync(cancellationToken);

            var response = new PaymentListResponse
            {
                Transactions = results,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };

            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment list with filters: {SearchTerm}, {StartDate}, {EndDate}",
                parameters.SearchTerm, parameters.StartDate, parameters.EndDate);
            return (false, null, "Failed to retrieve payment transactions");
        }
    }
}
