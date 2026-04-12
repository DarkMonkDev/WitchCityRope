---
name: check-production-server
description: Check WitchCityRope production/staging server health, Serilog logs, system stats, and generate a prioritized report of issues. SSHes into the server, queries logging.application_logs, analyzes Docker logs, and researches root causes for critical/high items.
---

# Check Production Server Skill

**Purpose**: SSH into the production (or staging) server, collect all available logs and metrics, then analyze and present a prioritized report of issues found.

**When to Use**:
- Periodic health checks on the production site
- After receiving reports of errors or slow behavior
- Before/after deployments to verify system health
- When investigating user-reported issues
- Proactive monitoring for emerging problems

## SINGLE SOURCE OF TRUTH

**This skill is the ONLY way to run a comprehensive production health check for WitchCityRope.**

---

## Invocation

```
/check-production-server [options]
```

**Options**:
- No arguments: Check production, last 24 hours
- `staging`: Check staging instead
- `48h` or `72h`: Look back further in time
- `quick`: Skip database log queries (faster, Docker logs only)

**Examples**:
```
/check-production-server
/check-production-server staging
/check-production-server 72h
/check-production-server staging 48h
/check-production-server quick
```

---

## How This Skill Works

### Step 1: Collect Data

Run the data-collection script. Parse the user's arguments to determine options:

```bash
# Default: production, 24 hours, full analysis
bash .claude/skills/check-production-server/execute.sh

# Staging environment
bash .claude/skills/check-production-server/execute.sh --env staging

# Custom lookback period
bash .claude/skills/check-production-server/execute.sh --hours 48

# Quick mode (skip DB log queries)
bash .claude/skills/check-production-server/execute.sh --skip-db-logs

# Combined
bash .claude/skills/check-production-server/execute.sh --env staging --hours 72
```

**IMPORTANT**: The script outputs the report file path in the `SKILL_RESULT` JSON. Read that file to get all collected data.

### Step 2: Read the Report File

After `execute.sh` completes, read the output file it created. This file contains ALL raw data from the server organized into sections:

1. **Server System Health** — uptime, CPU, memory, disk, load, open connections
2. **Docker Container Status** — API/Web/Redis state, restart counts, health-check history, resource usage
3. **Application Docker Logs** — API container stdout logs (Serilog compact JSON), grouped by area:
   - Error/Warning logs, error summaries
   - HTTP 4xx/5xx, slow requests
   - Payment/PayPal/ticket-purchase logs
   - Event registration/attendance logs
   - Auth/session/refresh-token logs
   - Vetting workflow logs
   - Database connection issues
   - Restart/crash signals
   - Hangfire job logs
4. **Serilog Database Logs** — structured logs from `logging.application_logs` table, plus:
   - **4.11 Error Forensics - Request Paths & User Agents**: Which pages caused errors and WHO requested them
   - **4.12 Malicious User Agent Detection**: XSS probes, SQL injection, fuzzing (scanner indicator)
   - **4.13 PostgreSQL Exception Details**: Extracts the `Where:` clause — `parameter $N` = bad input, NOT bad stored data
   - **4.14 Error Concentration by IP**: Shows if errors come from one IP (scanner) or many (real bug)
5. **Nginx & Network** — reverse proxy errors, SSL status, 4xx/5xx codes, traffic analysis, plus:
   - **5.12 Scanner Detection - Malicious UAs** (today + yesterday)
   - **5.13 Rate-Limited IPs (429s)** — likely scanners
   - **5.14 500-Error IPs** — which IPs triggered 500 errors
   - **5.15 Full IP Profiles** — for each top error-causing IP: response code breakdown, user agents, total requests
6. **Hangfire Background Jobs** — failed jobs, recurring job status (daily-backup, daily-log-summary, log-retention-cleanup, refresh-token-cleanup, event-email-scheduler)
7. **Health Endpoints** — public and internal, response times, key pages
8. **Environment & Configuration** — container images, .env keys, Vault token status
9. **Data Integrity Audits** — WCR-specific business-logic correctness checks:
   - **9.1 Event Capacity vs Active Attendees** — overbooked events (active count > Capacity)
   - **9.2 Session Capacity vs Ticketed Registrations** — oversold sessions in multi-session events
   - **9.3 Vetting Status Drift** — `VettingApplications.WorkflowStatus` ≠ `Users.VettingStatus`
   - **9.4 Orphaned Completed Ticket Purchases** — payment completed but no active attendance created
   - **9.5 Active Attendance Without Completed Payment** — inverse of 9.4
   - **9.6 Completed Refunds With Stale Ticket Status** — refund done but `PaymentStatus` not updated

### Step 3: Analyze and Categorize

Review ALL sections and categorize every issue found into priority levels.

#### Priority Definitions

| Priority | Criteria | Examples |
|----------|----------|---------|
| **CRITICAL** | Active data loss, payments failing, site down, security breach | Unhandled exceptions in checkout/payments, DB connection failures, container crash loops, SSL expired, Hangfire completely stuck |
| **HIGH** | Degraded functionality, intermittent errors affecting users | 500 errors on key pages, slow response times (>5s), repeatedly failing Hangfire jobs, vetting status drift affecting active users, overbooked events |
| **MEDIUM** | Potential problems, non-critical warnings, performance concerns | Increasing error rates, disk space warnings, deprecated API usage, log table growing large, isolated audit discrepancies |
| **LOW** | Cosmetic issues, optimization opportunities, minor warnings | Non-critical 404s, verbose logging, minor config improvements |

#### Warning Count Anomaly Detection (MANDATORY)

**After reviewing error-level items, you MUST scan section 4.5 (Warning Logs Grouped by Template) for anomalies.** High-frequency warnings often indicate real problems hiding below the error threshold.

**Rules for escalating warnings to HIGH:**
1. **Any single warning template with >500 occurrences in the lookback period** — Report at minimum as MEDIUM, investigate whether it indicates a real user-impacting problem. If it involves authentication, sessions, Data Protection, cookies, or payment processing, escalate to HIGH.
2. **Session/auth warnings** (e.g., "Error unprotecting the session cookie", "Error unprotecting the authentication cookie", token deserialization failures, refresh-token-cleanup anomalies) — Always flag at HIGH when count >100. These indicate Data Protection key issues or container-restart impacts on user sessions.
3. **Security-adjacent warnings** (unauthorized access, CSRF failures, lockout events, rate-limit hits) — Always flag at HIGH when count >50.
4. **New warning templates** that didn't appear in previous reports — Flag at MEDIUM for visibility.

The warning section is as important as the error section. Do NOT skip it.

#### Certbot Renewal Check (MANDATORY)

**You MUST check section 5.5b (Certbot Renewal Service Status) for failures.** A "timer is active" does NOT mean renewal is working — the timer just schedules the attempt. Verify `certbot.service` is NOT in a `failed` state.

**Rules:**
1. If `certbot.service` shows `failed` or `exit-code` — this is **CRITICAL**. Certs will expire and the site will go down.
2. Check the "RECENT FAILURES" section for which specific cert is failing and why.
3. Common causes: DNS record missing for a domain on the cert (NXDOMAIN), HTTP challenge unreachable, rate limits.
4. Do NOT report "auto-renewal is working" based solely on the timer being active. Only report it as working if the service status is clean (no recent failures).
5. For any cert expiring within 14 days where renewal is failing, escalate to CRITICAL.

### Step 4: Research Critical and High Items

For each **CRITICAL** and **HIGH** priority item found:

#### Step 4a: READ THE ACTUAL ERROR DETAILS FIRST (MANDATORY)

**Before ANY analysis or hypothesis, you MUST read the full error details from `logging.application_logs`.** Do NOT guess at root causes from error summaries alone.

For each error cluster, query Serilog for:
```sql
-- Get full error details including request paths, user agents, IPs, and exception details
-- Properties JSONB is FLAT (not nested under .Properties) — query directly.
-- RequestPath is a first-class column.
SELECT
  timestamp,
  message,
  exception,
  request_path,
  properties->>'UserAgent' as user_agent,
  properties->>'RemoteIpAddress' as ip,
  properties->>'RequestMethod' as method,
  properties->>'StatusCode' as status_code
FROM logging.application_logs
WHERE level >= 4
  AND timestamp >= '[start_time]' AND timestamp < '[end_time]'
ORDER BY timestamp
LIMIT 50;
```

Read the FULL exception message and stack trace. Key details:
- **Exception type** (e.g., `NpgsqlException`, `InvalidOperationException`, `DbUpdateConcurrencyException`)
- **Where the error occurs** — in stored data, request parameters, middleware, etc.
- **PostgreSQL error details** — the `Where:` line in PostgreSQL errors tells you EXACTLY what caused it (e.g., `unnamed portal parameter $2` = request input, not stored data)
- **Request paths and user agents** associated with the errors

**CRITICAL RULE: NEVER hypothesize about root causes without reading the actual error details. "The error is probably X" is not acceptable — read the logs and KNOW what it is.**

#### Step 4b: Distinguish Bot/Scanner Traffic from Real Errors (MANDATORY)

**Before attributing errors to application bugs or data issues, CHECK if the errors were caused by malicious bot/scanner traffic.**

Signs of scanner/bot-caused errors:
- **Error spike pattern**: Concentrated burst in a short time window (e.g., hundreds of errors in 1-2 hours, then nothing)
- **XSS probe user agents**: `Mozilla' onEvent=X...`, `Mozilla"'><qss...`, `Mozilla<script>...`
- **SQL injection attempts**: `USER_NAME()=`, `UNION SELECT`, `' OR 1=1`
- **Null bytes in parameters**: PostgreSQL `22021: invalid byte sequence` with `Where: unnamed portal parameter` = malformed input, NOT data corruption
- **Vulnerability scanning paths**: `/cgi-bin/`, `/wp-content/`, `/.git/config`, `/admin/`, `/authorize.php`, `/phpmyadmin`
- **Single IP generating most errors**: Cross-reference error IPs with nginx access logs
- **High rate-limit (429) counts**: Check nginx logs for the same IP — if most requests got 429'd, it's a scanner

**If errors are caused by a scanner:**
- Report it as a security/scanner incident, NOT as an application bug
- Identify the scanner IP from nginx access logs
- Report total request count, rate-limited count, and 500 count from the scanner
- Recommend blocking the IP in nginx
- Do NOT recommend code fixes for "data corruption" when the data is actually clean

**To verify data corruption claims:** If an error suggests corrupted stored data, ALWAYS query the actual database to confirm before reporting it. Run a targeted scan of the affected table/columns. If the data is clean, the error was caused by malformed input.

#### Step 4c: Standard Research

1. **Identify the error pattern** — What specific exception/error message is occurring?
2. **Determine frequency** — How often? Is it increasing, stable, or decreasing?
3. **Trace to source code** — Search the codebase for the relevant feature slice:
   - Use `Grep` to find the error message or exception type under `apps/api/Features/`
   - Use `Read` to examine the relevant endpoint/service file
   - Check if the error is in a known area (Payments, Participation, Vetting, Authentication, TicketAssignment)
4. **Assess scope** — How many users are affected? Is it blocking core functionality (checkout, event RSVP, vetting approval)?
5. **Identify likely root cause** — Based on the ACTUAL error details from 4a, not guesswork
6. **Estimate risk** — What happens if we don't fix it? What's the risk of fixing it?
7. **Suggest investigation approach** — What should we look at next?

**IMPORTANT**: Do NOT do a full code review or propose complete fixes. Just identify the likely cause, scope, and risk. The goal is to give the user enough information to decide what to investigate further.

### Step 5: Present the Report

Present findings in this format:

---

## WitchCityRope Server Health Report
**Environment**: [production/staging]
**Period**: Last [N] hours
**Generated**: [timestamp]

### Executive Summary
[2-3 sentences: Overall health status. Number of items found by priority. Any immediate action items.]

### CRITICAL Issues ([count])

#### C1: [Issue Title]
- **What**: [Brief description of the problem]
- **Evidence**: [Specific log entries, error counts, or metrics]
- **Frequency**: [How often, trend direction]
- **Impact**: [Who/what is affected]
- **Likely Cause**: [Based on code research]
- **Source**: [file:line if identified]
- **Risk if Unaddressed**: [What could happen]
- **Recommended Next Step**: [What to investigate or do]

### HIGH Priority Issues ([count])

#### H1: [Issue Title]
[Same format as CRITICAL]

### MEDIUM Priority Issues ([count])

#### M1: [Issue Title]
- **What**: [Brief description]
- **Evidence**: [Key data points]
- **Recommendation**: [Brief suggested action]

### LOW Priority Issues ([count])

#### L1: [Issue Title]
- **What**: [Brief description]
- **Recommendation**: [Brief suggested action]

### System Health Summary

| Metric | Value | Status |
|--------|-------|--------|
| Server Uptime | [value] | [OK/Warning/Critical] |
| CPU Load | [value] | [OK/Warning/Critical] |
| Memory Usage | [value] | [OK/Warning/Critical] |
| Disk Usage | [value] | [OK/Warning/Critical] |
| API Container | [value] | [OK/Warning/Critical] |
| Web Container | [value] | [OK/Warning/Critical] |
| Redis Container | [value] | [OK/Warning/Critical] |
| Public /api/health | [value] | [OK/Warning/Critical] |
| Homepage Response | [value] | [OK/Warning/Critical] |
| SSL Certificates | [value] | [OK/Warning/Critical] |
| Certbot Renewal | [value] | [OK/Warning/Critical] |
| Vault Token | [value] | [OK/Warning/Critical] |

### Background Jobs Status

| Job | Cron | Last Run | Next Run | Notes |
|-----|------|----------|----------|-------|
| daily-backup | | | | |
| daily-log-summary | | | | |
| log-retention-cleanup | | | | |
| refresh-token-cleanup | | | | |
| event-email-scheduler | | | | |

### Data Integrity Audit Summary

| Audit | Issues Found | Notes |
|-------|-------------|-------|
| 9.1 Event Capacity vs Attendees | [count] | |
| 9.2 Session Capacity vs Tickets | [count] | |
| 9.3 Vetting Status Drift | [count] | |
| 9.4 Orphaned Ticket Purchases | [count] | |
| 9.5 Active Attendance w/o Payment | [count] | |
| 9.6 Stale Refund Reconciliation | [count] | |

### Scope and Effort Assessment

| Issue | Effort Estimate | Risk Level | Recommendation |
|-------|----------------|------------|----------------|
| C1: [title] | [Small/Medium/Large] | [Low/Medium/High] | [Fix now / Investigate / Monitor] |
| H1: [title] | [Small/Medium/Large] | [Low/Medium/High] | [Fix now / Investigate / Monitor] |

### Step 6: Save Report to Production Incidents

After presenting the report to the user, **save a copy** to the production incidents folder for historical tracking.

**Directory**: `docs/functional-areas/production-incidents/`
**Index file**: `docs/functional-areas/production-incidents/00-index.md`

1. **Determine the next incident number** by reading `00-index.md` and finding the highest existing number. If the directory or index doesn't exist yet, create it with `NN=01` and an initial index table (columns: `#`, `Date`, `Severity`, `Title`, `Environment`, `Status`).
2. **Create a new file** named `NN-health-check-YYYY-MM-DD.md` (e.g., `02-health-check-2026-04-12.md`)
3. **Write the full report** (the same content presented to the user in Step 5) to this file
4. **Update `00-index.md`** to add a row to the incident log table with the date, severity (based on highest priority issue found), title, environment, and status

**If no CRITICAL or HIGH issues were found**, still save the report but mark severity as "OK" in the index. These clean reports are valuable as baselines.

**Do NOT save the raw data file** (from `/tmp/`) — only the analyzed report.

---

## Infrastructure Reference

### Server Access
- **Host**: `104.131.165.14` (shared DigitalOcean droplet — hosts multiple apps)
- **User**: `witchcity` (limited sudoers: nginx, certbot, systemctl, cp, ln)
- **Vault**: SSH key filename pulled from `secret/shared/digitalocean` → `SSH_KEY_FILENAME`
- **SSH key path**: `$HOME/.ssh/<SSH_KEY_FILENAME>`
- **Vault CLI**: `$HOME/bin/vault` with session token (run `vault login` if expired)

### Containers

| Environment | API | Web | Redis | Compose File |
|-------------|-----|-----|-------|--------------|
| Production | `witchcity-api-prod` (localhost:5001) | `witchcity-web-prod` (localhost:3001) | `witchcity-redis-prod` | `docker-compose.production.yml` |
| Staging | `witchcity-api-staging` (localhost:5002) | `witchcity-web-staging` (localhost:3002) | `witchcity-redis-staging` | `docker-compose.staging.yml` |

- **Production app dir**: `/opt/witchcityrope/production/`
- **Staging app dir**: `/opt/witchcityrope/staging/`
- **Registry**: `registry.digitalocean.com/witchcityrope/`

### Public URLs
- **Production**: `https://witchcityrope.com` (also `www.witchcityrope.com`)
- **Staging**: `https://staging.notfai.com`
- **API**: same domains, served at `/api/*` via nginx reverse proxy

### Database
- **Provider**: DigitalOcean Managed PostgreSQL (not containerized)
- **Port**: typically `25060` (inside the connection string)
- **Vault secret paths**:
  - Production: `secret/projects/witchcityrope/production` → field `PROD_DB_CONNECTION_STRING`
  - Staging: `secret/projects/witchcityrope/staging` → field `STAGING_DB_CONNECTION_STRING`
- **Connection string format**: `Host=…;Port=…;Database=…;Username=…;Password=…;SSL Mode=Require;…`
- `psql` runs **locally** against the managed DB (public access with SSL) — no SSH tunnel needed.

### Application Logging
- **Serilog** writes to both Docker stdout (compact JSON) and PostgreSQL
- **PostgreSQL sink table**: `logging.application_logs`
- **Column names**: snake_case — `timestamp`, `level` (smallint), `level_name`, `message`, `message_template`, `exception`, `source_context`, `properties` (jsonb), `user_id`, `correlation_id`, `request_path`, `machine_name`
- **Level values**: `0=Verbose, 1=Debug, 2=Information, 3=Warning, 4=Error, 5=Fatal`
- **Properties JSONB is flat** (NOT nested under `.Properties`): query with `properties->>'UserAgent'`, not `log_event->'Properties'->>'UserAgent'`
- **RequestPath is a first-class column**, not in `properties`
- **Retention**: 90 days via `LogRetentionCleanupJob` (Hangfire, 3 AM UTC daily)
- **Daily summaries**: `logging.daily_log_summaries` populated by `DailyLogSummaryJob` (1 AM UTC)

### Nginx Logs

**The `witchcity` user CANNOT read nginx logs directly** — they are owned by `www-data:adm`.

**The ONLY way to read nginx logs** is via `sudo /bin/cp` (allowed in sudoers) piping to `/dev/stdout`:
```bash
# CORRECT
ssh_cmd "sudo /bin/cp /var/log/nginx/witchcityrope/production-access.log /dev/stdout | awk '{print \$1}' | sort | uniq -c | sort -rn | head -20"

# WRONG — these all fail with permission denied (not in sudoers):
ssh_cmd "sudo tail /var/log/nginx/..."
ssh_cmd "sudo cat /var/log/nginx/..."
ssh_cmd "sudo awk '...' /var/log/nginx/..."
```

**Allowed sudo commands**: `/usr/sbin/nginx`, `/usr/bin/certbot`, `/bin/systemctl`, `/bin/cp`, `/bin/ln`

**Log file locations** (verified 2026-04-12 against live `/etc/nginx/sites-enabled/` and `/var/log/nginx/` listing):
| Environment | Access Log | Error Log |
|-------------|-----------|-----------|
| Production | `/var/log/nginx/witchcityrope-production-access.log` | `/var/log/nginx/witchcityrope-production-error.log` |
| Staging | _(not written to disk — `notfai-staging` nginx block has `access_log off`)_ | _(no dedicated error log)_ |
| Yesterday | Append `.1` to production path | Same pattern |
| Older | `.2.gz` through `.14.gz` (compressed) | Same pattern |

**Staging caveat**: `staging.witchcityrope.com` is served by the `notfai-staging` nginx site block (aliased alongside `staging.notfai.com`). That block has `access_log off` globally, so nginx access-log sections of the skill will always report empty when `--env staging` is used. That's expected, not a finding.

**Nginx log format**: Standard combined — `$remote_addr ... "$request" $status ... "$http_user_agent"`
- Field 1 (`$1`): client IP
- Field 6 (quote-delimited, `awk -F'"' '{print $6}'`): user agent
- Field 9 (`$9`): HTTP status code

**Note on access logging**: The server's nginx config *may or may not* have access logging enabled for WCR at any given moment. If sections 5.3–5.15 return empty, access logging is likely disabled and the script will report "access logging disabled". That's a finding worth flagging — without it, scanner detection and traffic analysis aren't possible.

### Docker Container Logs
- **Format**: Serilog Compact JSON (`RenderedCompactJsonFormatter`)
- **Level field**: `"@l":"Error"` / `"@l":"Warning"` / `"@l":"Information"` (compact format) OR `"Level":"Error"` (expanded)
- **Message field**: `"@m":"..."` (rendered), `"@mt":"..."` (template)
- **Retention**: Docker json-file driver defaults (check compose overrides)

### Health Endpoints

Verified 2026-04-12 against `apps/api/Features/**/Endpoints/*.cs`:

- `/health` — nginx-level 200 OK (fastest signal that nginx is serving)
- `/api/health` — basic health with DB connectivity, user count (`HealthEndpoints.cs:19`)
- `/api/health/detailed` — includes DatabaseVersion, ActiveUserCount, Environment (`HealthEndpoints.cs:40`)
- `/api/kiosk/payments/health` — kiosk/SSE payment subsystem (`KioskPaymentEndpoints.cs:420`)
- `/api/webhooks/paypal/health` — PayPal webhook endpoint (`WebhookEndpoints.cs:150`)
- `/api/test-helpers/health` — available in non-production builds only (`TestHelperEndpoints.cs:190`)

**Do NOT probe** `/api/payments/health` or `/api/paypal/health` — those paths don't exist. An earlier version of this skill had them and always saw 404.

### Background Jobs (Hangfire)
- **Dashboard**: `https://witchcityrope.com/hangfire` (admin-only; Authentication + Administrator role required)
- **Storage schema**: `hangfire` (lowercase column names: `statename`, `invocationdata`, `createdat`, etc.)
- **Recurring jobs**:
  - `daily-backup` — 2 AM local, DatabaseBackupService
  - `daily-log-summary` — 1 AM UTC, DailyLogSummaryJob
  - `log-retention-cleanup` — 3 AM UTC, LogRetentionCleanupJob (deletes logs >90 days)
  - `refresh-token-cleanup` — 4 AM UTC, RefreshTokenCleanupJob (expired tokens >30 days)
  - `event-email-scheduler` — hourly at :00, EmailSchedulerJob (reminders + thank-you emails)

### Domain Tables Referenced in Audits

All domain tables use **PascalCase** names and columns, in the `public` schema. Queries must double-quote identifiers (e.g., `"EventAttendances"."Status"`).

- `Events(Id, Title, StartDate, Capacity, ...)`
- `Sessions(Id, EventId, Name, StartTime, EndTime, Capacity)`
- `EventAttendances(Id, EventId, SessionId, UserId, TicketPurchaseId, AttendanceType, Status, CreatedAt, ...)`
  - AttendanceType: `1=RSVP, 2=Ticket`
  - Status: `1=Active, 2=Cancelled, 3=<other cancelled>, 4=Waitlisted, 5=PendingPayment, 6=PendingAcceptance`
- `TicketPurchases(Id, UserId, TicketTypeId, TotalPrice, PaymentStatus, PaymentMethod, ProcessedAt, PaymentReference)`
  - PaymentStatus (text): `Pending | Completed | Confirmed | Failed | PartiallyRefunded | Refunded`
- `PaymentRefunds(Id, TicketPurchaseId, RefundStatus, RefundAmountValue, ProcessedAt)`
  - RefundStatus (int): `0=Processing, 1=Completed, 2=Failed, 3=Cancelled`
- `VettingApplications(Id, UserId, WorkflowStatus, SubmittedAt, IsDeleted)`
- `Users(Id, UserName, VettingStatus, ...)` (AspNetUsers renamed to `Users`)
  - Vetting enum on both tables: `0=UnderReview, 1=InterviewApproved, 2=FinalReview, 3=Approved, 4=Denied, 5=OnHold, 6=Withdrawn`

### Key Application Areas to Watch
- **Payment Processing**: PayPal integration, webhooks (`Features/Webhooks/`, `Features/Payments/`)
- **Event Participation**: RSVP + Ticket flows (`Features/Participation/`)
- **Ticket Assignment**: PendingAcceptance flow (`Features/TicketAssignment/`)
- **Vetting**: Status transitions (`Features/Vetting/`)
- **Authentication**: ASP.NET Core Identity, refresh tokens (`Features/Authentication/`)
- **Check-In**: Session-based check-in flows (`Features/CheckIn/`)
- **Background Jobs**: Hangfire recurring jobs (`Program.cs:646-684`)

---

## Troubleshooting the Skill

### Script fails at Vault init
- Run `$HOME/bin/vault login` to reauthenticate
- Verify `$VAULT_ADDR` or use default `https://vault.monksafterdark.com`

### Script fails to connect to SSH
- SSH key filename from Vault may have changed — check `secret/shared/digitalocean`
- Key file permissions: `chmod 600 ~/.ssh/<filename>`
- Server may be down: check DigitalOcean dashboard

### No database logs found
- `logging.application_logs` might be empty or the query timed out. Verify table exists: `\dt logging.*`
- Connection string field name may differ from `PROD_DB_CONNECTION_STRING` / `STAGING_DB_CONNECTION_STRING` — check vault with `vault kv get secret/projects/witchcityrope/<env>`
- `psql` not installed locally: `apt install postgresql-client` (or equivalent)

### Nginx sections return "Could not read … access logging may be disabled"
- Access logging may genuinely be off for WCR on the server. Report this as a **MEDIUM** finding under observability — without access logs, scanner detection and traffic analysis are blind.
- If logs exist but sudoers is misconfigured, the server-side setup script at `docs/functional-areas/deployment/2025-01-13-digitalocean-deployment/implementation/setup-scripts/04-ssl-setup.sh` has the canonical nginx config.

### Health endpoints return errors
- Container may be restarting (check restart count in section 2.2)
- Database connection may be down (section 4.9)
- Nginx may not be routing correctly (section 5.2)

---

## Maintenance

**This skill is the single source of truth for WitchCityRope production server health checks.**

**To update the check procedure:**
1. Update `execute.sh` for data-collection changes
2. Update THIS file for analysis/presentation changes
3. DO NOT duplicate check procedures in other docs
