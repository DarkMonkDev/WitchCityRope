<!-- Last Updated: 2026-04-22 -->
<!-- Version: 1.0 -->
<!-- Owner: backend-developer + react-developer (collaborative session) -->
<!-- Status: Shipped to staging via commit a5458644 (deploy 2026-04-22) -->

# Vetting Admin: Bulk Send Reminder + Reminders Column

## Summary

The admin vetting list (`/admin/vetting`) gained a bulk **Send Reminder** action and a new **Reminders** column showing how many interview-reminder emails each applicant has received.

The bulk action mirrors the existing **Put On Hold** pattern: select rows via the checkbox column, click the new orange **SEND REMINDER (N)** button, fill the optional custom message in the modal, submit. Behind the scenes it fans out one POST per application via `Promise.allSettled`, calling the same `/api/vetting/reviewer/applications/{id}/send-reminder` endpoint the single-application page uses.

## Behavior

### Visibility rule

The bulk SEND REMINDER button only appears when the user's selection contains **at least one application in `InterviewApproved` status**. The label shows the count of *eligible* applications, not the total selection — e.g. selecting 2 `UnderReview` rows + 1 `InterviewApproved` row shows `SEND REMINDER (1)` and `PUT ON HOLD (3)` side-by-side.

This matches the backend constraint: `VettingService.SendReminderAsync` rejects any status other than `InterviewApproved` because the email template (`InterviewReminder`) is interview-scheduling-specific.

### Filtering

When the modal opens, only the `InterviewApproved` subset of the current selection is sent in. The recipient list at the top of the modal (`EagerLearner`, `KnotLearner`, `PatientPractitioner` etc.) reflects exactly who will receive the email. Ineligible rows from the user's selection are silently skipped — there's no warning message because the visibility rule already prevents sending to a 100%-ineligible selection.

### Custom message

The optional textarea in the modal sets the `{{custom_message}}` template variable. The same custom message is sent to every recipient in a bulk run — there is no per-recipient customization.

### Result toast

- All sends succeeded → green "Sent interview reminder to N applicant(s)"
- All sends failed → red "Failed to send reminders to N applicant(s)"
- Mixed → yellow "Sent X, failed Y"

The `Promise.allSettled` ensures one failed send doesn't cancel the others.

### Side effects (per successful send, unchanged from single-app behavior)

1. `VettingApplication.RemindersSentCount` incremented
2. `VettingApplication.LastReminderSentAt = DateTime.UtcNow`
3. New `VettingEmailLog` row written (audit trail with `TemplateType="InterviewReminder"`)
4. React Query `vettingKeys.applications()` invalidated → grid refreshes → Reminders column updates

### Reminders column

Renders `application.remindersSentCount` (just the integer, center-aligned, between EMAIL and APPLICATION DATE). Not sortable. Defaults to `0` if the field is undefined (older cached data).

## Architecture decisions

### Why fan-out instead of a bulk endpoint

The backend has unused `BulkReminderRequest` / `BulkOperationResult` DTOs but no endpoint. The existing **Put On Hold** bulk action also fans out via `Promise.all` — we matched that pattern for consistency rather than introducing a one-off bulk endpoint. Per-call success/failure tracking via `Promise.allSettled` gives us the same partial-failure reporting a bulk endpoint would.

If/when the volume of bulk operations grows, this is the natural extension point — wrap the loop in a `POST /api/vetting/reviewer/applications/bulk-send-reminder` and switch the frontend to a single call.

### Why the modal supports both single and bulk modes

`SendReminderModal` accepts either:
- Single mode: `applicationId` + `applicantName` (used on the application detail page)
- Bulk mode: `applicationIds[]` + `applicantNames[]` (used on the admin list page)

This mirrors `OnHoldModal`'s API exactly. We deliberately did NOT split into two separate modal components because the UI delta is small (recipient list display + button-label count) and a single component keeps changes to the template/message field in one place.

### Why selection state lives in `AdminVettingPage`, not `VettingApplicationsList`

The list component used to own the selection set internally and notify the parent via callback. After a successful bulk action the parent had no way to clear the child's checkboxes — they'd stay checked even though the underlying state had moved on. The fix was to lift selection state up to the parent (controlled-component pattern). Now `selectedApplicationIds` is a prop, and clearing the parent's state propagates naturally.

This refactor also fixed the same UX bug on the **Put On Hold** flow.

## Components touched

| File | Change |
|---|---|
| `apps/api/Features/Vetting/Models/ApplicationSummaryDto.cs` | Added `RemindersSentCount` field |
| `apps/api/Features/Vetting/Services/VettingService.cs` | Projected the field in the list query |
| `apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx` | Reminders column + controlled selection props |
| `apps/web/src/features/admin/vetting/components/SendReminderModal.tsx` | Bulk-mode support, fan-out via `Promise.allSettled` |
| `apps/web/src/features/admin/vetting/components/OnHoldModal.tsx` | Added React Query invalidation (closes long-standing TODO) |
| `apps/web/src/pages/admin/AdminVettingPage.tsx` | Owns selection state, renders bulk button + filter |
| `tests/unit/web/features/admin/vetting/VettingApplicationsList.test.tsx` | Updated for controlled component |
| `tests/unit/web/features/admin/vetting/components/OnHoldModal.test.tsx` | Wrapped in `QueryClientProvider` |

## Database / migration

**No schema change required.** The fields `RemindersSentCount` and `LastReminderSentAt` already existed on `VettingApplication` from migration `20260317180718_AddVettingReminderTrackingAndFixEmailLogTemplateType`. This work just exposes the count on the admin list endpoint that didn't previously project it.

## Testing

- `VettingApplicationsList.test.tsx` — 12 passed, 1 skipped (pre-existing) after controlled-component refactor
- End-to-end browser verification on local dev: column renders, button visibility rule works for all selection mixes, modal lists correct subset, counts increment after send, checkboxes auto-clear
- End-to-end browser verification on staging (`https://staging.notfai.com/admin/vetting`): column visible with real production-like data; no actions taken to avoid live emails
- `OnHoldModal.test.tsx` has 10 pre-existing test failures from text drift introduced by commit `395ec740` (months pre-dating this work) — see T-3 in `/docs/technical-debt.md` for context. Not blocking.

## Future work (not scoped here)

1. **Server-side bulk endpoint** if volume demands single-transaction semantics
2. **Per-recipient custom message** if reviewers need to personalize per applicant
3. **Sortable Reminders column** if reviewers want "show me the most-reminded applicants first"
4. **Reminder cooldown** — currently nothing prevents an admin from sending 5 reminders to the same person in a row. Out of scope; flag if becomes a problem.
5. **Fix the 10 pre-existing OnHoldModal test text-drift failures** — separate task, tracked in T-3

## Related

- Single-application send-reminder feature: `apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` line 667
- Email template stored in DB: `GlobalEmailTemplate` row where `Category=Vetting`, `TemplateType="InterviewReminder"`
- Backend service: `VettingService.SendReminderAsync` at `apps/api/Features/Vetting/Services/VettingService.cs:1759`
- Skill that runs the React tests for this work: `bash .claude/skills/run-test-suite/execute.sh --mode react --filter VettingApplicationsList`
