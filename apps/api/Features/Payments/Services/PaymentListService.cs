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
                    .ThenInclude(tt => tt.Sessions);

            // Exclude $0 RSVP ticket purchases from payments analytics
            // RSVPs (TotalPrice == 0) are not financial transactions and should not appear in payment reports
            query = query.Where(tp => tp.TotalPrice > 0);

            // Determine if status filter includes refund-related statuses
            // Refund line items appear when Refunded/PartiallyRefunded are selected (or no filter applied)
            var statusFilterValues = new List<string>();
            bool includeRefundLineItems = true; // Default: show refund line items when no status filter
            if (!string.IsNullOrWhiteSpace(parameters.Statuses))
            {
                statusFilterValues = parameters.Statuses
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpper())
                    .ToList();

                // Show refund line items when Refunded or PartiallyRefunded is in the filter
                includeRefundLineItems = statusFilterValues.Contains("REFUNDED")
                    || statusFilterValues.Contains("PARTIALLYREFUNDED");
            }

            // Get refund data for all ticket purchases (for populating RefundId, RefundDate, RemainingRefundableAmount)
            // Also used to create separate refund line items in the transaction list
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
                    (tp.TicketType != null && tp.TicketType.Sessions.Any() && tp.TicketType.Sessions.Any(s => s.Name.ToLower().Contains(searchTerm))) ||
                    tp.Id.ToString().ToLower().Contains(searchTerm)
                );
            }

            // Apply date range filter (purchase date for purchases, refund date for refund line items)
            DateTime? startDateUtc = null;
            DateTime? endDateUtc = null;
            if (parameters.StartDate.HasValue)
            {
                startDateUtc = parameters.StartDate.Value.ToUniversalTime();
                query = query.Where(tp => tp.PurchaseDate >= startDateUtc);
            }

            if (parameters.EndDate.HasValue)
            {
                endDateUtc = parameters.EndDate.Value.ToUniversalTime().AddDays(1); // Include full end date
                query = query.Where(tp => tp.PurchaseDate < endDateUtc);
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

            // Apply status filter (for ticket purchases only - refund line items handled separately)
            if (statusFilterValues.Count > 0)
            {
                var parsedStatuses = statusFilterValues
                    .Select(s => Enum.TryParse<TicketPurchasePaymentStatus>(s, ignoreCase: true, out var ps) ? ps : (TicketPurchasePaymentStatus?)null)
                    .Where(ps => ps.HasValue)
                    .Select(ps => ps!.Value)
                    .ToList();
                query = query.Where(tp => parsedStatuses.Contains(tp.PaymentStatus));
            }

            // Apply amount range filter (TotalPrice for purchases, refund amount for refund line items)
            if (parameters.MinAmount.HasValue)
            {
                query = query.Where(tp => tp.TotalPrice >= parameters.MinAmount.Value);
            }

            if (parameters.MaxAmount.HasValue)
            {
                query = query.Where(tp => tp.TotalPrice <= parameters.MaxAmount.Value);
            }

            // Execute query without pagination first - we need all matching purchases
            // to combine with refund line items before paginating the combined result
            var ticketPurchases = await query.ToListAsync(cancellationToken);

            // Map purchases to DTOs
            var purchaseDtos = ticketPurchases
                .Select(tp =>
                {
                    var hasRefundData = refundData.TryGetValue(tp.Id, out var refund);
                    var totalRefunded = hasRefundData ? refund!.TotalRefunded : 0m;
                    var remainingRefundable = tp.TotalPrice - totalRefunded;

                    return new PaymentTransactionDto
                    {
                        Id = tp.Id,
                        TicketId = tp.Id,
                        PaymentDate = tp.ProcessedAt ?? tp.PurchaseDate,
                        UserName = !string.IsNullOrEmpty(tp.User!.SceneName)
                            ? tp.User.SceneName
                            : !string.IsNullOrEmpty(tp.User.FirstName)
                                ? $"{tp.User.FirstName} {tp.User.LastName}".Trim()
                                : tp.User.Email ?? "Unknown",
                        UserEmail = tp.User!.Email ?? string.Empty,
                        EventName = tp.TicketType!.Event!.Title,
                        SessionName = tp.TicketType.Sessions.Any()
                            ? string.Join(", ", tp.TicketType.Sessions.OrderBy(s => s.StartTime).Select(s => s.Name))
                            : null,
                        PaymentMethod = tp.PaymentMethod,
                        Amount = tp.TotalPrice,
                        Currency = PaymentConstants.Currency,
                        Status = tp.PaymentStatus.ToString(),
                        // AwaitingManualRefund is explicitly refundable because that is the
                        // whole point of the state — it's waiting for an admin to click Process
                        // Refund (which either issues the real refund via authorize-net API, or
                        // records that the admin refunded externally). M2b — 2026-04-12.
                        IsRefundable = (tp.PaymentStatus == TicketPurchasePaymentStatus.Completed
                                        || tp.PaymentStatus == TicketPurchasePaymentStatus.PartiallyRefunded
                                        || tp.PaymentStatus == TicketPurchasePaymentStatus.AwaitingManualRefund)
                                       && tp.TotalPrice > 0
                                       && remainingRefundable > 0,
                        RefundId = hasRefundData ? refund!.LatestRefundId : null,
                        RefundDate = hasRefundData ? refund!.LatestRefundDate : null,
                        RemainingRefundableAmount = remainingRefundable
                    };
                })
                .ToList();

            // Build refund line items as separate transactions
            // Refunds appear as negative-amount entries so they're visible in the payment ledger
            var refundDtos = new List<PaymentTransactionDto>();

            if (includeRefundLineItems)
            {
                // Get the IDs of all filtered ticket purchases to find their refunds
                var filteredPurchaseIds = ticketPurchases.Select(tp => tp.Id).ToHashSet();

                // Query individual refund records (not grouped) for the filtered purchases
                var individualRefunds = await _db.Set<WitchCityRope.Api.Features.Payments.Entities.PaymentRefund>()
                    .AsNoTracking()
                    .Include(r => r.TicketPurchase)
                        .ThenInclude(tp => tp!.User)
                    .Include(r => r.TicketPurchase)
                        .ThenInclude(tp => tp!.TicketType)
                            .ThenInclude(tt => tt!.Event)
                    .Include(r => r.TicketPurchase)
                        .ThenInclude(tp => tp!.TicketType)
                            .ThenInclude(tt => tt!.Sessions)
                    .Where(r => r.RefundStatus == WitchCityRope.Api.Features.Payments.Models.RefundStatus.Completed
                        && filteredPurchaseIds.Contains(r.TicketPurchaseId))
                    .ToListAsync(cancellationToken);

                // Apply date filter to refund dates if date range is specified
                if (startDateUtc.HasValue)
                    individualRefunds = individualRefunds.Where(r => r.ProcessedAt >= startDateUtc.Value).ToList();
                if (endDateUtc.HasValue)
                    individualRefunds = individualRefunds.Where(r => r.ProcessedAt < endDateUtc.Value).ToList();

                refundDtos = individualRefunds
                    .Select(r =>
                    {
                        var tp = r.TicketPurchase!;
                        return new PaymentTransactionDto
                        {
                            // Use the refund ID so it's a unique row in the table
                            Id = r.Id,
                            // Link back to the original ticket purchase for reference
                            TicketId = r.TicketPurchaseId,
                            // Use the refund processing date, not the original purchase date
                            PaymentDate = r.ProcessedAt,
                            UserName = !string.IsNullOrEmpty(tp.User!.SceneName)
                                ? tp.User.SceneName
                                : !string.IsNullOrEmpty(tp.User.FirstName)
                                    ? $"{tp.User.FirstName} {tp.User.LastName}".Trim()
                                    : tp.User.Email ?? "Unknown",
                            UserEmail = tp.User!.Email ?? string.Empty,
                            EventName = tp.TicketType!.Event!.Title,
                            SessionName = tp.TicketType.Sessions.Any()
                                ? string.Join(", ", tp.TicketType.Sessions.OrderBy(s => s.StartTime).Select(s => s.Name))
                                : null,
                            PaymentMethod = tp.PaymentMethod,
                            // Negative amount to indicate money going out (refund)
                            Amount = -r.RefundAmountValue,
                            Currency = r.RefundCurrency,
                            Status = "Refund",
                            IsRefundable = false,
                            RefundId = r.Id,
                            RefundDate = r.ProcessedAt,
                            RemainingRefundableAmount = 0
                        };
                    })
                    .ToList();
            }

            // Combine purchases and refund line items, then sort and paginate
            var allTransactions = purchaseDtos.Concat(refundDtos).ToList();

            // Apply sorting to the combined list
            var sortBy = parameters.SortBy?.ToLower() ?? "paymentdate";
            var isDescending = parameters.SortDirection?.Equals("Desc", StringComparison.OrdinalIgnoreCase) ?? true;

            allTransactions = sortBy switch
            {
                "amount" => isDescending
                    ? allTransactions.OrderByDescending(t => Math.Abs(t.Amount)).ToList()
                    : allTransactions.OrderBy(t => Math.Abs(t.Amount)).ToList(),
                "username" => isDescending
                    ? allTransactions.OrderByDescending(t => t.UserName).ToList()
                    : allTransactions.OrderBy(t => t.UserName).ToList(),
                "status" => isDescending
                    ? allTransactions.OrderByDescending(t => t.Status).ToList()
                    : allTransactions.OrderBy(t => t.Status).ToList(),
                _ => isDescending
                    ? allTransactions.OrderByDescending(t => t.PaymentDate).ToList()
                    : allTransactions.OrderBy(t => t.PaymentDate).ToList()
            };

            // Total count includes both purchases and refund line items
            var totalCount = allTransactions.Count;

            // Apply pagination to the combined, sorted list
            var skip = (parameters.Page - 1) * parameters.PageSize;
            var results = allTransactions.Skip(skip).Take(parameters.PageSize).ToList();

            var totalPages = parameters.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / parameters.PageSize)
                : 0;

            var response = new PaymentListResponse
            {
                Transactions = results,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalPages = totalPages
            };

            _logger.LogInformation(
                "Retrieved {Count} payment transactions ({PurchaseCount} purchases, {RefundCount} refunds) (page {Page}/{TotalPages})",
                results.Count,
                purchaseDtos.Count,
                refundDtos.Count,
                parameters.Page,
                totalPages);

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
