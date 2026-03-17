using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Reports.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Reports.Services;

/// <summary>
/// Implementation of admin report data aggregation.
/// All queries use AsNoTracking since reports are read-only.
/// </summary>
public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        ApplicationDbContext db,
        ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<UserSummaryResponse> GetUserSummaryAsync(CancellationToken cancellationToken)
    {
        // Each count is a separate query to keep the SQL simple and indexable.
        // These are lightweight COUNT queries that PostgreSQL handles efficiently.
        var totalUsers = await _db.Users
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var verifiedUsers = await _db.Users
            .AsNoTracking()
            .CountAsync(u => u.EmailConfirmed, cancellationToken);

        // VettingStatus == 3 means Approved (see ApplicationUser.IsVetted computed property)
        var vettedMembers = await _db.Users
            .AsNoTracking()
            .CountAsync(u => u.VettingStatus == 3, cancellationToken);

        // HasVettingApplication is a denormalized flag set when a vetting application is created.
        // Avoids an expensive JOIN to the VettingApplications table.
        var vettingApplicationsSubmitted = await _db.Users
            .AsNoTracking()
            .CountAsync(u => u.HasVettingApplication, cancellationToken);

        _logger.LogInformation(
            "User summary generated: {TotalUsers} total, {VerifiedUsers} verified, {VettedMembers} vetted",
            totalUsers, verifiedUsers, vettedMembers);

        return new UserSummaryResponse
        {
            TotalUsers = totalUsers,
            VerifiedUsers = verifiedUsers,
            UnverifiedUsers = totalUsers - verifiedUsers,
            VettedMembers = vettedMembers,
            VettingApplicationsSubmitted = vettingApplicationsSubmitted
        };
    }

    /// <inheritdoc/>
    public async Task<DailyTransactionsResponse> GetDailyTransactionsAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken)
    {
        // Convert DateOnly boundaries to DateTime (UTC) for querying PurchaseDate (which is DateTime)
        var startUtc = dateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endUtc = dateTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Query ticket purchases in the date range, grouped by date.
        // We project into an anonymous type in the database query to minimize data transfer.
        var purchasesByDay = await _db.TicketPurchases
            .AsNoTracking()
            .Where(tp => tp.PurchaseDate >= startUtc && tp.PurchaseDate < endUtc)
            .GroupBy(tp => tp.PurchaseDate.Date)
            .Select(g => new
            {
                // g.Key is the date component of PurchaseDate (midnight UTC)
                Date = g.Key,
                CompletedCount = g.Count(tp =>
                    tp.PaymentStatus == TicketPurchasePaymentStatus.Completed ||
                    tp.PaymentStatus == TicketPurchasePaymentStatus.Confirmed),
                CompletedAmount = g
                    .Where(tp =>
                        tp.PaymentStatus == TicketPurchasePaymentStatus.Completed ||
                        tp.PaymentStatus == TicketPurchasePaymentStatus.Confirmed)
                    .Sum(tp => tp.TotalPrice),
                FailedCount = g.Count(tp =>
                    tp.PaymentStatus == TicketPurchasePaymentStatus.Failed),
                IncompleteCount = g.Count(tp =>
                    tp.PaymentStatus == TicketPurchasePaymentStatus.Pending)
            })
            .ToListAsync(cancellationToken);

        // Index purchase data by DateOnly for O(1) lookups during zero-fill
        var purchaseLookup = purchasesByDay.ToDictionary(
            p => DateOnly.FromDateTime(p.Date),
            p => p);

        // Query DailyLogSummary for cc_failure entries.
        // These track credit card failures that may never create a TicketPurchase record
        // (e.g., validation failures before order creation).
        var ccFailuresByDay = await _db.DailyLogSummaries
            .AsNoTracking()
            .Where(d => d.Category == "cc_failure"
                && d.Date >= dateFrom
                && d.Date <= dateTo)
            .ToDictionaryAsync(d => d.Date, cancellationToken);

        // Zero-fill: generate every date in the range so the chart has a continuous X-axis.
        // Days with no transactions will have 0 for all counts.
        var days = new List<DailyTransactionDay>();
        for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
        {
            var hasPurchaseData = purchaseLookup.TryGetValue(date, out var purchaseData);
            var hasCcFailureData = ccFailuresByDay.TryGetValue(date, out var ccFailureData);

            var ticketFailedCount = hasPurchaseData ? purchaseData!.FailedCount : 0;
            var logFailedCount = hasCcFailureData ? ccFailureData!.Count : 0;

            days.Add(new DailyTransactionDay
            {
                Date = date,
                CompletedCount = hasPurchaseData ? purchaseData!.CompletedCount : 0,
                CompletedAmount = hasPurchaseData ? purchaseData!.CompletedAmount : 0m,
                // Use the higher of the two failure sources:
                // TicketPurchase tracks failures that created a record,
                // DailyLogSummary tracks failures that may have been rejected before record creation
                FailedCount = Math.Max(ticketFailedCount, logFailedCount),
                IncompleteCount = hasPurchaseData ? purchaseData!.IncompleteCount : 0
            });
        }

        _logger.LogInformation(
            "Daily transactions report generated for {DateFrom} to {DateTo}: {DayCount} days",
            dateFrom, dateTo, days.Count);

        return new DailyTransactionsResponse { Days = days };
    }
}
