# WitchCityRope Server Health Report

**Environment**: production
**Period**: Last 24 hours
**Generated**: 2026-05-16 05:46 UTC
**Context**: Post-deployment check, immediately after release `54dd7b75` (email catch-up / scheduler-retry fixes)

## Executive Summary

The site is healthy. The deployment of `54dd7b75` (~05:41 UTC) was clean — **zero application errors after the deploy**, all three WitchCityRope containers healthy with 0 restarts, every health endpoint and key page returning 200. No CRITICAL or HIGH issues.

Two findings were investigated in depth:

- **M1 (orphaned ticket purchases) — FALSE POSITIVE.** Both flagged rows are proxy/gift ticket purchases where the recipient's attendance sits in `PendingAcceptance` (Status 6). Audit 9.4 only counts `Status=1` (Active), so it false-flags every unaccepted ticket assignment. No money lost, no data corruption.
- **M2 (awaiting manual refund) — CONFIRMED REAL.** Two customers cancelled Authorize.net ticket purchases, were correctly flagged `AwaitingManualRefund`, but **no refund has been issued** — they are genuinely owed $20 each (waiting 16 and 28 days). This is an operational worklist gap, not a code bug.

Neither is deploy-related; both predate today's release.

## CRITICAL Issues (0)

None.

## HIGH Priority Issues (0)

None.

## MEDIUM Priority Issues (1)

### M2: Two customers owed Authorize.net refunds, never processed (audit 9.7) — RESOLVED

- **What**: Two `TicketPurchases` flagged `PaymentStatus = AwaitingManualRefund`, with **no `PaymentRefunds` row** — meaning the refund has not been issued.

  | Purchase | Customer | Amount | Event | Flagged | Linked attendance |
  |---|---|---|---|---|---|
  | `f37c6d98-b700-4b0f-a486-691bd0b3607c` | `Del_delerium@protonmail.com` | $20.00 | Rope Jam - April | 2026-04-18 (~28 days) | Cancelled (Status 2) |
  | `7b33990f-e78d-4e0f-a376-ff59bdbf1a91` | `jporreca01844@yahoo.com` | $20.00 | Rope Jam - May | 2026-04-30 (~16 days) | Cancelled (Status 2) |

- **Deep-dive findings**:
  - Both purchases were paid via `authorize-net` (real $20 charges, `PaymentReference` `WCR-900223E6` and `WCR-58E541C0`).
  - Both customers later cancelled — the linked `EventAttendances` are `Status = 2` (Cancelled), so the seats were correctly released.
  - On cancellation the system correctly set `PaymentStatus = AwaitingManualRefund` (the BE-12 M2b worklist flag — Authorize.net refunds are not self-service automated).
  - **`PaymentRefunds` has zero rows for either purchase** — no refund was ever initiated. The money has not gone back to the customers.
- **Root cause**: Purely operational. The `AwaitingManualRefund` status worked exactly as designed — it flagged the purchases for an admin to action. Nobody worked the queue. This is not a code defect.
- **Impact**: Two members are out $20 each and have been for 16 and 28 days.
- **Recommended Next Step**: An admin processes both refunds — issue the $20 back through Authorize.net (admin refund action / Authorize.net merchant portal) and the purchase status moves to `Refunded`. Then re-run audit 9.7 to confirm a clear queue.
- **Resolution (2026-05-16)**: An admin processed both refunds via the Admin Payments UI the same day. Both `TicketPurchases` are now `Refunded`; both `PaymentRefunds` rows are `Completed` ($20 each, `WasVoided = false`). Audit 9.7 re-run — the `AwaitingManualRefund` queue is empty. Both refunds ran through the post-deploy `RefundService` code and captured their Authorize.net transaction id — a live verification of the refund-transaction-id storage feature shipped in commit `131179d7`.

## LOW Priority Issues (2)

### L1: Three pre-deploy `OperationCanceledException` 500s (known BE-5)

- **What**: 3 error-level logs, all at 2026-05-15 20:35 UTC (before today's deploy), on `/api/events/cae0d3e3.../participation` and `/api/user/assigned-tickets`. Exception: `OperationCanceledException` / Postgres `57014: canceling statement due to user request` — the client disconnected mid-request on the May Rope Jam event page.
- **Evidence**: Sections 4.3, 4.7, 4.9. Errors-by-hour shows the only error hour was 20:00 on 05-15; zero errors after the deploy.
- **Recommendation**: No action — already tracked as tech-debt **BE-5** (service layer treats client cancellations as 500s). Cosmetic log noise.

### L2: Audit 9.4 false-flags pending ticket assignments (M1 root cause)

- **What**: Audit 9.4 ("Orphaned Completed Ticket Purchases") joins `EventAttendances` filtered to `Status = 1` (Active) only. A proxy/gift ticket purchase whose recipient hasn't accepted yet has a `Status = 6` (PendingAcceptance) attendance — a real, correct attendance row — but 9.4 doesn't count it, so the purchase looks "orphaned."
- **Evidence (the two M1 rows, both confirmed false positives)**:
  - `c1991e3b` — Indigo (`indigokink42@gmail.com`) bought a ticket **for** Anna (`grahamcrackeranna@gmail.com`) for the May Rope Jam. Attendance `019e2d3d-17d4` exists, `Status = 6` PendingAcceptance — Anna simply hasn't accepted yet (event is today). Working as designed.
  - `2f7aba6f` — Derek (`derek2652@gmail.com`) bought a ticket **for** user `92df9d52` for the April Rope Jam. Attendance `019d9d86-5843` exists, `Status = 6`. The recipient never accepted; the April event is over. A real-world "gift ticket never claimed" situation — a customer-service question, not a data defect.
- **Resolution**: FIXED 2026-05-16. The audit 9.4 query in `check-production-server/execute.sh` now counts `Status IN (1, 6)` (Active + PendingAcceptance) as valid attendance. Re-ran the corrected query against production — returns 0 rows (both gift tickets correctly excluded). Separately, the CS team may want to follow up with Derek about the unclaimed April ticket (`2f7aba6f`).

## System Health Summary

| Metric | Value | Status |
|--------|-------|--------|
| Server Uptime | 217 days, load 0.47/0.38/0.38 | OK |
| Memory Usage | 4.2Gi/7.8Gi used, 3.6Gi available | OK |
| Disk Usage | 15% of 154G | OK |
| API Container | Up, healthy, 0 restarts, 121.9MiB/2GiB | OK |
| Web Container | Up, healthy, 0 restarts, 5.9MiB/512MiB | OK |
| Redis Container | Up, healthy, 0 restarts | OK |
| Public /api/health | 200, DB connected, 742 users | OK |
| Homepage Response | 200 in ~0.16s | OK |
| SSL Certificates | Valid; certbot last run clean | OK |
| Certbot Renewal | `certbot.service` exited 0/SUCCESS, "no renewal failures" | OK |
| Vault Token | Authenticated | OK |

## Background Jobs Status

No failed Hangfire jobs in the last 24 hours. All 5 recurring jobs registered and scheduled (`daily-backup`, `daily-log-summary`, `log-retention-cleanup`, `refresh-token-cleanup`, `event-email-scheduler`). The `event-email-scheduler` last ran ~05:40 UTC (just before deploy); its next hourly run picks up the new code. Hangfire shows 38 `Failed` jobs lifetime-cumulative but **0 in the last 24h** — historical, tracked under BE-9.

## Data Integrity Audit Summary

| Audit | Issues Found | Notes |
|-------|-------------|-------|
| 9.1 Event Capacity vs Attendees | 0 | No overbooked events |
| 9.2 Session Capacity vs Attendees | 0 | No oversold sessions |
| 9.3 Vetting Status Drift | 0 | Clean |
| 9.4 Orphaned Ticket Purchases | 2 | Both FALSE POSITIVES — see L2 / M1 |
| 9.5 Active Attendance w/o Payment | 0 | Clean |
| 9.6 Stale Refund Reconciliation | 0 | Clean |
| 9.7 Awaiting Manual Refund | 2 | Both CONFIRMED — see M2 |

## Deployment Verification

| Check | Result |
|-------|--------|
| Deployed git SHA | `54dd7b75` (API + Web images both built 2026-05-16 05:41:42 UTC) |
| Errors after deploy (05:41 UTC →) | 0 — API docker logs show only `/health` 200s |
| Container restarts since deploy | 0 |
| Warnings (24h) | 196 — all routine (request logging, JWT auth noise, 2× "Policies field NULL"); none session/Data-Protection/security escalation triggers |

## Side Observations (not action items)

- The May Rope Jam's TicketType is named **"Rope Jam - March"** — a stale name carried verbatim through two event-copy operations (March → April → May). `EventService.CopyEventAsync` copies `TicketType.Name` as-is, so any month embedded in a ticket-type name goes stale on copy. Cosmetic only; mention to the user if month-named ticket types are intentional.

## Scope and Effort Assessment

| Issue | Effort | Risk | Recommendation |
|-------|--------|------|----------------|
| M2: two unprocessed refunds | Small (admin action) | Low | Admin issues the two $20 Authorize.net refunds |
| L1: BE-5 cancellation 500s | — | Low | No action — tracked as BE-5 |
| L2: audit 9.4 false positives | Small (skill query fix) | Low | FIXED 2026-05-16 — 9.4 now counts `Status IN (1,6)` |

---

*Generated by the `check-production-server` skill. Point-in-time snapshot — findings were valid at generation time. M1/M2 deep-dive added 2026-05-16 from read-only production DB queries.*
