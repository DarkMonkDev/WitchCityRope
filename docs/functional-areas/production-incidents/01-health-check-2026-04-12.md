# WitchCityRope Server Health Report

**Environment**: production
**Period**: Last 24 hours
**Generated**: 2026-04-12 20:39 UTC
**Raw data**: `/tmp/wcr-server-check-production-20260412-163854.txt` (ephemeral, rotates after 7 days)

## Executive Summary

Core site is healthy: 37h container uptime, 0 restarts, all health endpoints 200, DB connected, certs valid, certbot renewal clean. **Zero CRITICAL findings.** Two **HIGH** priority items: (1) the proxy-RSVP endpoint is returning 500 to real users when the target principal already has participation (should be 409); (2) `DailyLogSummaryJob` is permanently stuck in Failed state since at least 04/11 due to a `DBNull` type-mapping bug. Four MEDIUM items around payment/attendance drift and missing observability.

## CRITICAL Issues (0)

_None._

## HIGH Priority Issues (2)

### H1: Proxy-RSVP endpoint returns 500 on "principal already participating"

- **What**: `POST /api/events/{id}/proxy-rsvp` returns HTTP 500 when the target principal already has Active/PendingAcceptance participation for the event, instead of returning a 4xx validation response.
- **Evidence**:
  - Section 4.3 / 4.11: two 500s today at 16:34:32 and 16:34:33 UTC, both on event `cae0d3e3-751c-4a04-8629-51f574847299`, real iPhone user-agent (not a scanner)
  - Section 4.5: warning template `Principal {PrincipalUserId} already has Active/PendingAcceptance participation for event {EventId}` — count = 2, same timestamps
  - Section 3.7: raw container log shows `ProxyRsvpService` emitted `WRN` then request logged as 500
- **Frequency**: 2 today; only 500s in the 24h window.
- **Impact**: Real user on mobile attempting proxy RSVP for an authorized contact got a generic server error instead of a user-friendly "already registered" message.
- **Likely Cause**: `ProxyRsvpService` detects the conflict, logs a warning, then throws an unhandled exception rather than returning an error result.
- **Source**: `apps/api/Features/ProxyRsvp/Services/ProxyRsvpService.cs`
- **Recommended Next Step**: Grep `ProxyRsvpService.cs` for the log line; check what happens after logging. Return a typed conflict result the endpoint maps to 409.

### H2: DailyLogSummaryJob permanently Failed — `DBNull` type-mapping error

- **What**: Hangfire recurring job `daily-log-summary` has been retrying and failing for 04/11/2026's summary. All 10 retries exhausted; job 1232 is in permanent `Failed` state.
- **Evidence**:
  - Section 4.3 / 4.4: `Daily log summary job failed for {Date}` × 11 occurrences
  - Section 4.7: `System.InvalidOperationException: The current provider doesn't have a store type mapping for properties of type 'DBNull'.` at `ExecuteSqlRawAsync`
  - Section 6.1: Hangfire job 1232 `Failed`, type `WitchCityRope.Api.Features.Logging.Jobs.DailyLogSummaryJob`
  - Section 6.2: 38 Failed vs 27 Succeeded overall
- **Frequency**: Nightly at 01:00 UTC, failing every execution since at least 04/11.
- **Impact**: `logging.daily_log_summaries` table not populated → no rolled-up observability. No user-facing impact.
- **Likely Cause**: Raw SQL passes `DBNull.Value` as parameter value without explicit `NpgsqlParameter` / `NpgsqlDbType`. Probably a NULL aggregate (COUNT on empty set) flowing into parameter binding.
- **Source**: `apps/api/Features/Logging/Jobs/DailyLogSummaryJob.cs`
- **Recommended Next Step**: Replace `DBNull.Value` parameter with typed `NpgsqlParameter` or `COALESCE(..., 0)` in SQL. Fix before 01:00 UTC tomorrow to avoid another retry cascade.

## MEDIUM Priority Issues (4)

### M1: "Rope Jam - March" event overbooked historically (50 active vs 40 capacity)

- **What**: Event on 2026-03-21 (already occurred) has 50 active attendances (31 RSVPs + 19 tickets) vs capacity 40.
- **Evidence**: Section 9.1.
- **Recommendation**: Historical; not actionable today. Investigate whether capacity enforcement gap exists: capacity lowered after registration, RSVPs not counted against capacity, admin override flow?

### M2: Payment/attendance reconciliation drift (2 items)

- **What**: Two completed `TicketPurchases` have zero active attendances; one completed `PaymentRefund` has stale `PaymentStatus = Completed`.
- **Evidence**:
  - Sections 9.4: purchases `c0c34074-…` ($30, 2026-03-21) and `e4e24d70-…` ($25, 2026-03-19)
  - Section 9.6: refund `2f53d2f1-…` against `e4e24d70-…` marked Completed but purchase still `Completed`
  - Section 3.6 log at 07:38:55: `"Ticket purchase c0c34074-… is not PayPal (method: authorize-net) - skipping automatic refund"`
- **Recommendation**: When cancellation can't auto-refund (authorize-net path), flip `PaymentStatus` to `RefundPending` or `CancelledAwaitingManualRefund`. When `PaymentRefunds` completes, update `TicketPurchases.PaymentStatus` → `Refunded`/`PartiallyRefunded`. Backfill the two rows.

### M3: Nginx access logging disabled (observability gap)

- **What**: All nginx access-log sections (5.3–5.15) returned zero data, but nginx is active and serving traffic.
- **Evidence**: Section 5.6: "Total requests: 0, Unique IPs: 0". Web container logs show actual requests. Sudoers `/bin/cp` works (section 5.1 ran).
- **Recommendation**: SSH and run `sudo /bin/cp /var/log/nginx/witchcityrope/production-access.log /dev/stdout | head`. If empty/missing, check `/etc/nginx/sites-enabled/witchcityrope-production` for `access_log` setting. Without access logs, scanner detection and traffic analysis are blind.

### M4: `/api/payments/health` endpoint returns 404

- **What**: SKILL.md documents this endpoint; live API doesn't expose it.
- **Evidence**: Section 7.6: `/api/payments/health -> 404`.
- **Recommendation**: Either add the endpoint or remove from docs. Basic `/api/health` covers overall status.

## LOW Priority Issues (1)

### L1: Nginx duplicate protocol-options warnings at reload time

- **What**: `protocol options redefined for [::]:443` warnings in multiple sites-enabled configs.
- **Recommendation**: Add `http2` only to the first `listen 443 ssl` across sites-enabled files.

## Warning-Count Anomaly Scan

Reviewed section 4.5. No template exceeded escalation thresholds.

- `HTTP {…} responded {StatusCode}` — 196 (below 500): normal
- `JWT authentication failed` — 10 (below 100): normal session-expiry noise
- `Principal already has participation` — 2 → escalated to H1
- All others ≤ 2

## Certbot Renewal Check

- Service: `inactive (dead)` with `status=0/SUCCESS` — clean exit
- Recent failures: "no renewal failures"
- Nearest cert: 2026-05-13 (30 days, `accounting.notfai.com`); `witchcityrope.com` valid 61 days
- **Verdict: renewal HEALTHY.**

## System Health Summary

| Metric | Value | Status |
|--------|-------|--------|
| Server Uptime | 183 days, load 0.31 | OK |
| CPU Load | 0.31 / 4 cores | OK |
| Memory Usage | 3.9G / 7.8G (50%), 0 swap | OK |
| Disk Usage | 25G / 154G (17%) | OK |
| API Container | running, healthy, 37h, 0 restarts, 12% mem | OK |
| Web Container | running, healthy, 37h, 0 restarts, 1% mem | OK |
| Redis Container | running, healthy, 37h, 0 restarts, 1% mem | OK |
| Public /api/health | 200 Healthy, 144ms, 726 users / 157 active | OK |
| Homepage Response | 200 in 139ms | OK |
| SSL Certificates | all valid, earliest 30 days | OK |
| Certbot Renewal | clean, no failures | OK |
| Vault Token | expires 2028-01-27 | OK |
| Nginx Access Logging | empty / disabled | Warning (M3) |

## Background Jobs Status

| Job | Cron | Last Result | Next Run (UTC) |
|-----|------|-------------|----------------|
| daily-backup | `0 2 * * *` | Succeeded (1233) | 2026-04-13 06:00 |
| daily-log-summary | `0 1 * * *` | **FAILED (1232)** | 2026-04-13 05:00 |
| log-retention-cleanup | `0 3 * * *` | Succeeded (1236) | 2026-04-13 07:00 |
| refresh-token-cleanup | `0 4 * * *` | Succeeded (1238) | 2026-04-13 08:00 |
| event-email-scheduler | `0 * * * *` | Succeeded (1254) | 2026-04-13 00:00 |

## Data Integrity Audit Summary

| Audit | Issues |
|-------|--------|
| 9.1 Event Capacity vs Attendees | 1 (M1 — historical) |
| 9.2 Session Capacity vs Tickets | 0 |
| 9.3 Vetting Status Drift | 0 |
| 9.4 Orphaned Ticket Purchases | 2 (M2) |
| 9.5 Active Attendance w/o Payment | 0 |
| 9.6 Stale Refund Reconciliation | 1 (M2) |

## Scope and Effort Assessment

| Issue | Effort | Risk | Recommendation |
|-------|--------|------|----------------|
| H1: proxy-RSVP 500 | Small | Low | Fix now — one service + endpoint touch |
| H2: DailyLogSummaryJob DBNull | Small | Low | Fix before 01:00 UTC tomorrow |
| M1: Rope Jam overbook | Medium | Low | Investigate capacity logic; past event, no rush |
| M2: Payment/refund sync | Medium | Medium | Fix cancel/refund paths; backfill two rows |
| M3: Nginx access log | Small | Medium | Toggle on; verify scanner detection |
| M4: /api/payments/health 404 | Small | Low | Remove from docs or add endpoint |
| L1: Nginx protocol-opts | Small | Low | Cleanup during next config pass |
