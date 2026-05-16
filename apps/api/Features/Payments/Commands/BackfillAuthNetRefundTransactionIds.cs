using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Shared.Extensions;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Payments.Commands;

// ============================================================================
// ONE-OFF BACKFILL
// ----------------------------------------------------------------------------
// This command exists to recover historical Authorize.net refund transaction
// ids that were processed BEFORE PaymentRefund.EncryptedAuthNetRefundTransactionId
// existed. Prior to that fix, RefundService only wrote the Authorize.net refund
// transaction id to the Serilog log and then discarded it — so for past refunds
// (e.g. user "Cepheus", processed 2026-04-13) the only surviving copy of the id
// is in the structured log table logging.application_logs.
//
// Those logs persist for 90 days. This command reads the log rows whose
// message_template matches the "Authorize.net refund completed" line, extracts
// the TicketId + TransactionId from the structured properties, finds matching
// PaymentRefund rows that still have a NULL EncryptedAuthNetRefundTransactionId,
// encrypts the id and stores it.
//
// SAFE TO LEAVE IN PLACE:
//   - Administrator-only (enforced at the endpoint).
//   - Idempotent — it only ever fills NULL columns, never overwrites a value,
//     so re-running it is a no-op for rows already backfilled.
//   - It does NOT touch any money-movement logic; it only writes a previously
//     missing reconciliation identifier onto existing refund rows.
//
// SAFE TO REMOVE once it has been run successfully in every environment and the
// 90-day log retention window for pre-fix refunds has elapsed (after which the
// source log rows are gone anyway).
// ============================================================================

/// <summary>
/// One-off, admin-only, idempotent backfill of historical Authorize.net refund
/// transaction ids from the Serilog application log into PaymentRefund rows.
/// </summary>
public static class BackfillAuthNetRefundTransactionIds
{
    /// <summary>
    /// Exact Serilog message template emitted by RefundService when an Authorize.net
    /// refund completes. Must stay byte-for-byte identical to the template string in
    /// RefundService.ProcessRefundAsync — Serilog stores the raw template (with the
    /// {Placeholder} tokens intact) in the message_template column.
    /// </summary>
    private const string AuthNetRefundCompletedTemplate =
        "Authorize.net refund completed for ticket {TicketId}, refund transaction: {TransactionId}";

    /// <summary>
    /// JSON report returned to the admin describing what the backfill did.
    /// </summary>
    public class BackfillReport
    {
        /// <summary>Number of matching "refund completed" log entries found in logging.application_logs.</summary>
        public int LogEntriesFound { get; set; }

        /// <summary>Number of PaymentRefund rows that were updated with a recovered transaction id.</summary>
        public int RefundRowsUpdated { get; set; }

        /// <summary>Number of matched PaymentRefund rows that already had a transaction id (skipped — idempotent).</summary>
        public int RefundRowsAlreadyPopulated { get; set; }

        /// <summary>
        /// Log entries whose TicketId/TransactionId could not be matched to a PaymentRefund row,
        /// or whose properties could not be parsed. Each string is a short human-readable reason.
        /// </summary>
        public List<string> Unmatched { get; set; } = new();
    }

    /// <summary>
    /// Execute the backfill. Endpoint enforces Administrator role before this runs.
    /// </summary>
    public static async Task<IResult> Execute(
        ApplicationDbContext dbContext,
        IEncryptionService encryptionService,
        ILogger<BackfillReport> logger,
        CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(dbContext, encryptionService, logger, cancellationToken);

        if (!result.IsSuccess)
            return result.ToProblem("Authorize.net Refund Transaction Id Backfill Failed");

        return Results.Ok(result.Value);
    }

    /// <summary>
    /// Core backfill logic, separated from the HTTP shell so it returns a
    /// <see cref="Result{T}"/> per the error-handling standard.
    /// </summary>
    private static async Task<Result<BackfillReport>> RunAsync(
        ApplicationDbContext dbContext,
        IEncryptionService encryptionService,
        ILogger<BackfillReport> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = new BackfillReport();

            // logging.application_logs is mapped as a read-only EF entity (ApplicationLog)
            // specifically for admin query purposes — query it through the DbSet rather
            // than hand-rolling raw SQL. Filter by the exact message template so we only
            // pull the "refund completed" rows. Properties is JSONB stored as a string;
            // we parse TicketId / TransactionId out of it in memory below.
            var logRows = await dbContext.ApplicationLogs
                .AsNoTracking()
                .Where(l => l.MessageTemplate == AuthNetRefundCompletedTemplate
                            && l.Properties != null)
                .Select(l => new { l.Id, l.Properties })
                .ToListAsync(cancellationToken);

            report.LogEntriesFound = logRows.Count;

            if (logRows.Count == 0)
            {
                logger.LogInformation(
                    "AuthNet refund transaction id backfill: no matching log entries found.");
                return Result<BackfillReport>.Success(report);
            }

            // Extract (TicketPurchaseId, TransactionId) pairs from the structured log properties.
            // A given ticket could appear in more than one log row (e.g. multiple partial
            // refunds); we keep all pairs and match each against refund rows below.
            var extracted = new List<(Guid TicketPurchaseId, string TransactionId)>();

            foreach (var row in logRows)
            {
                try
                {
                    using var doc = JsonDocument.Parse(row.Properties!);
                    var rootObj = doc.RootElement;

                    if (!rootObj.TryGetProperty("TicketId", out var ticketIdElem)
                        || !rootObj.TryGetProperty("TransactionId", out var transactionIdElem))
                    {
                        report.Unmatched.Add(
                            $"Log entry {row.Id}: missing TicketId or TransactionId property.");
                        continue;
                    }

                    var ticketIdStr = ticketIdElem.ToString();
                    var transactionId = transactionIdElem.ToString();

                    if (!Guid.TryParse(ticketIdStr, out var ticketPurchaseId))
                    {
                        report.Unmatched.Add(
                            $"Log entry {row.Id}: TicketId '{ticketIdStr}' is not a valid GUID.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(transactionId))
                    {
                        report.Unmatched.Add(
                            $"Log entry {row.Id}: TransactionId is empty.");
                        continue;
                    }

                    extracted.Add((ticketPurchaseId, transactionId));
                }
                catch (JsonException)
                {
                    // Don't leak parser internals — log row id is enough to investigate.
                    report.Unmatched.Add(
                        $"Log entry {row.Id}: properties JSON could not be parsed.");
                }
            }

            if (extracted.Count == 0)
            {
                logger.LogWarning(
                    "AuthNet refund transaction id backfill: {LogCount} log entries found but none yielded a usable TicketId/TransactionId pair.",
                    logRows.Count);
                return Result<BackfillReport>.Success(report);
            }

            // Load every refund for the affected ticket purchases in one query so we can
            // match in memory. Tracked (no AsNoTracking) because we intend to update them.
            var ticketPurchaseIds = extracted.Select(e => e.TicketPurchaseId).Distinct().ToList();

            var refunds = await dbContext.PaymentRefunds
                .Where(r => ticketPurchaseIds.Contains(r.TicketPurchaseId))
                .ToListAsync(cancellationToken);

            foreach (var (ticketPurchaseId, transactionId) in extracted)
            {
                // Find refund rows for this ticket purchase. IDEMPOTENT: only rows whose
                // EncryptedAuthNetRefundTransactionId is still NULL are eligible — rows
                // already populated (by the live code path or a previous backfill run)
                // are counted as "already populated" and left untouched.
                var candidateRefunds = refunds
                    .Where(r => r.TicketPurchaseId == ticketPurchaseId)
                    .ToList();

                if (candidateRefunds.Count == 0)
                {
                    report.Unmatched.Add(
                        $"Ticket purchase {ticketPurchaseId}: log has a transaction id but no PaymentRefund row exists.");
                    continue;
                }

                var encrypted = await encryptionService.EncryptAsync(transactionId);

                foreach (var refund in candidateRefunds)
                {
                    if (!string.IsNullOrEmpty(refund.EncryptedAuthNetRefundTransactionId))
                    {
                        report.RefundRowsAlreadyPopulated++;
                        continue;
                    }

                    refund.EncryptedAuthNetRefundTransactionId = encrypted;
                    report.RefundRowsUpdated++;
                }
            }

            if (report.RefundRowsUpdated > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "AuthNet refund transaction id backfill complete: {LogEntriesFound} log entries, "
                + "{RefundRowsUpdated} refund rows updated, {RefundRowsAlreadyPopulated} already populated, "
                + "{UnmatchedCount} unmatched.",
                report.LogEntriesFound, report.RefundRowsUpdated,
                report.RefundRowsAlreadyPopulated, report.Unmatched.Count);

            return Result<BackfillReport>.Success(report);
        }
        catch (Exception ex)
        {
            // Never leak ex.Message to the wire — log it server-side, return a static string.
            logger.LogError(ex, "Error running Authorize.net refund transaction id backfill.");
            return Result<BackfillReport>.Infrastructure(
                "An error occurred while running the backfill. See server logs for details.");
        }
    }
}
