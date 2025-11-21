using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Service for retrieving and filtering payment transaction lists for admin
/// REWRITTEN: Now queries TicketPurchases table (single source of truth) instead of Payments table
/// </summary>
public interface IPaymentListService
{
    /// <summary>
    /// Get filtered and paginated list of payment transactions from TicketPurchases table
    /// </summary>
    Task<(bool Success, PaymentListResponse? Response, string Error)> GetPaymentListAsync(
        PaymentListQueryParameters parameters,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of payment list service
/// ARCHITECTURE FIX (2025-11-18): Queries TicketPurchases instead of Payments table
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
            // Start with base query - TicketPurchases is the single source of truth for ALL ticket sales
            IQueryable<TicketPurchase> query = _db.Set<TicketPurchase>()
                .AsNoTracking()
                .Include(tp => tp.User)
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt.Event)
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt.Session);

            // Get refund data for all ticket purchases (for populating RefundId, RefundDate, RemainingRefundableAmount)
            // Query PaymentRefunds table to join with TicketPurchases
            var refundData = await _db.Set<WitchCityRope.Api.Features.Payments.Entities.PaymentRefund>()
                .AsNoTracking()
                .Where(r => r.RefundStatus == WitchCityRope.Api.Features.Payments.Models.RefundStatus.Completed)
                .GroupBy(r => r.TicketPurchaseId)
                .Select(g => new
                {
                    TicketPurchaseId = g.Key,
                    TotalRefunded = g.Sum(r => r.RefundAmountValue),
                    LatestRefundId = g.OrderByDescending(r => r.CreatedAt).Select(r => r.Id).FirstOrDefault(),
                    LatestRefundDate = g.OrderByDescending(r => r.CreatedAt).Select(r => r.CreatedAt).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.TicketPurchaseId, cancellationToken);

            // Apply search filter (user name, email, event title)
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(tp =>
                    (tp.User != null && (
                        (tp.User.SceneName != null && tp.User.SceneName.ToLower().Contains(searchTerm)) ||
                        (tp.User.FirstName != null && tp.User.FirstName.ToLower().Contains(searchTerm)) ||
                        (tp.User.LastName != null && tp.User.LastName.ToLower().Contains(searchTerm)) ||
                        (tp.User.Email != null && tp.User.Email.ToLower().Contains(searchTerm))
                    )) ||
                    (tp.TicketType != null && tp.TicketType.Event != null && tp.TicketType.Event.Title.ToLower().Contains(searchTerm)) ||
                    (tp.TicketType != null && tp.TicketType.Session != null && tp.TicketType.Session.Name.ToLower().Contains(searchTerm)) ||
                    tp.Id.ToString().ToLower().Contains(searchTerm)
                );
            }

            // Apply date range filter (purchase date)
            if (parameters.StartDate.HasValue)
            {
                var startDate = parameters.StartDate.Value.ToUniversalTime();
                query = query.Where(tp => tp.PurchaseDate >= startDate);
            }

            if (parameters.EndDate.HasValue)
            {
                var endDate = parameters.EndDate.Value.ToUniversalTime().AddDays(1); // Include full end date
                query = query.Where(tp => tp.PurchaseDate < endDate);
            }

            // Apply payment method filter
            if (!string.IsNullOrWhiteSpace(parameters.PaymentMethods))
            {
                var methods = parameters.PaymentMethods
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(m => m.ToUpper())
                    .ToList();
                query = query.Where(tp => methods.Contains(tp.PaymentMethod.ToUpper()));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(parameters.Statuses))
            {
                var statuses = parameters.Statuses
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpper())
                    .ToList();
                query = query.Where(tp => statuses.Contains(tp.PaymentStatus.ToUpper()));
            }

            // Apply amount range filter (TotalPrice)
            if (parameters.MinAmount.HasValue)
            {
                query = query.Where(tp => tp.TotalPrice >= parameters.MinAmount.Value);
            }

            if (parameters.MaxAmount.HasValue)
            {
                query = query.Where(tp => tp.TotalPrice <= parameters.MaxAmount.Value);
            }

            // Apply sorting before count (for consistency)
            var sortBy = parameters.SortBy?.ToLower() ?? "paymentdate";
            var isDescending = parameters.SortDirection?.Equals("Desc", StringComparison.OrdinalIgnoreCase) ?? true;

            query = sortBy switch
            {
                "amount" => isDescending
                    ? query.OrderByDescending(tp => tp.TotalPrice)
                    : query.OrderBy(tp => tp.TotalPrice),
                "username" => isDescending
                    ? query.OrderByDescending(tp => tp.User!.SceneName ?? tp.User.Email)
                    : query.OrderBy(tp => tp.User!.SceneName ?? tp.User.Email),
                "status" => isDescending
                    ? query.OrderByDescending(tp => tp.PaymentStatus)
                    : query.OrderBy(tp => tp.PaymentStatus),
                _ => isDescending // Default: PaymentDate (PurchaseDate)
                    ? query.OrderByDescending(tp => tp.ProcessedAt ?? tp.PurchaseDate)
                    : query.OrderBy(tp => tp.ProcessedAt ?? tp.PurchaseDate)
            };

            // Get total count after filtering but before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var skip = (parameters.Page - 1) * parameters.PageSize;
            query = query.Skip(skip).Take(parameters.PageSize);

            // Execute query and get ticket purchases
            var ticketPurchases = await query.ToListAsync(cancellationToken);

            // Map to DTOs with refund information
            var results = ticketPurchases
                .Select(tp =>
                {
                    // Get refund info for this ticket purchase (if any)
                    var hasRefundData = refundData.TryGetValue(tp.Id, out var refund);
                    var totalRefunded = hasRefundData ? refund!.TotalRefunded : 0m;
                    var remainingRefundable = tp.TotalPrice - totalRefunded;

                    return new PaymentTransactionDto
                    {
                        Id = tp.Id, // TicketPurchase ID (not separate Payment record)
                        TicketId = tp.Id, // For refund endpoint consistency
                        PaymentDate = tp.ProcessedAt ?? tp.PurchaseDate,

                        // User info - prefer SceneName over FirstName/LastName over Email
                        UserName = !string.IsNullOrEmpty(tp.User!.SceneName)
                            ? tp.User.SceneName
                            : !string.IsNullOrEmpty(tp.User.FirstName)
                                ? $"{tp.User.FirstName} {tp.User.LastName}".Trim()
                                : tp.User.Email ?? "Unknown",
                        UserEmail = tp.User!.Email ?? string.Empty,

                        // Event info
                        EventName = tp.TicketType!.Event!.Title,
                        SessionName = tp.TicketType.Session != null ? tp.TicketType.Session.Name : null,

                        // Payment details from TicketPurchase
                        PaymentMethod = tp.PaymentMethod,
                        Amount = tp.TotalPrice,
                        Currency = "USD", // All prices are USD
                        Status = tp.PaymentStatus,

                        // Refund info - populated from PaymentRefunds table
                        // IsRefundable: Payment can be refunded if:
                        // - Status is Completed or PartiallyRefunded
                        // - Has money remaining to refund (TotalPrice > TotalRefunded)
                        // - Payment method supports refunds (all methods now support refunds via variable refund)
                        IsRefundable = (tp.PaymentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                                        || tp.PaymentStatus.Equals("PartiallyRefunded", StringComparison.OrdinalIgnoreCase))
                                       && tp.TotalPrice > 0
                                       && remainingRefundable > 0,

                        // Latest refund ID (most recent refund for this ticket purchase)
                        RefundId = hasRefundData ? refund!.LatestRefundId : null,

                        // Latest refund date (when most recent refund was processed)
                        RefundDate = hasRefundData ? refund!.LatestRefundDate : null,

                        // Amount remaining that can be refunded (original - total refunded)
                        RemainingRefundableAmount = remainingRefundable
                    };
                })
                .ToList();

            var response = new PaymentListResponse
            {
                Transactions = results,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };

            _logger.LogInformation(
                "Retrieved {Count} payment transactions (page {Page}/{TotalPages})",
                results.Count,
                parameters.Page,
                (totalCount + parameters.PageSize - 1) / parameters.PageSize);

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
