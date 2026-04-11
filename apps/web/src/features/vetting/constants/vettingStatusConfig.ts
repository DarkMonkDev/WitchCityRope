/**
 * Vetting Status Display Configuration — SINGLE SOURCE OF TRUTH
 * ══════════════════════════════════════════════════════════════
 *
 * This file is the ONE place in the frontend that defines how each vetting
 * status is displayed to users (labels, colors, icons, alert titles, etc.).
 *
 * WHY THIS FILE EXISTS
 * --------------------
 * Before this file existed, multiple components each defined their own
 * local maps of vetting status → display metadata. This caused:
 *
 *   1. The Oct 2025 → Feb 2026 enum rename (Pending → UnderReview,
 *      ApprovedForInterview → InterviewApproved, Vetted → Approved, plus
 *      added FinalReview / Withdrawn) was applied to the backend enum but
 *      only partially propagated to frontend components. The dashboard
 *      VettingAlertBox was left with stale keys like "Pending" and
 *      "ApprovedForInterview" that never matched the real API values.
 *
 *   2. A later "strict-mode cleanup" commit silenced the resulting
 *      TypeScript error with `as keyof typeof alertConfigs`, letting the
 *      broken lookup ship silently — the dashboard alert never rendered
 *      for users in UnderReview or InterviewApproved status.
 *
 *   3. At least three separate frontend enums existed for "vetting status":
 *      the generated one, a local union in ChangeVettingStatusModal, and
 *      a completely different kebab-case enum in vetting.types.ts — none
 *      of which matched each other or the backend.
 *
 * HOW THIS FILE PREVENTS REGRESSION
 * ---------------------------------
 * 1. `VettingStatus` is re-exported from @witchcityrope/shared-types — it
 *    is auto-generated from the C# VettingStatus enum via NSwag/OpenAPI
 *    and is the AUTHORITATIVE type. Do NOT create local unions anywhere.
 *
 * 2. `VETTING_STATUS_CONFIG` uses `Record<VettingStatus, ...>` — this
 *    forces TypeScript to require an entry for EVERY enum value at
 *    compile time. If the backend adds a new status (e.g. "OnVacation"),
 *    this file FAILS TO COMPILE until the config is updated. No silent
 *    fallthrough, no broken UI.
 *
 * 3. All callers go through `getConfigFromStatus()` or `getConfigFromInt()`
 *    helpers, never indexing into the Record directly with arbitrary input.
 *    This prevents the `as keyof typeof` anti-pattern that hid the
 *    original bug.
 *
 * RULES FOR FUTURE AGENTS
 * -----------------------
 * - DO NOT create local unions or enums for vetting status in other files.
 * - DO NOT define status display metadata (labels, colors, etc.) anywhere
 *   except this file. If a component needs to display vetting status,
 *   import VETTING_STATUS_CONFIG and read the fields.
 * - To change an alert title, color, or emoji: edit this file, not the
 *   consuming component.
 * - When the backend enum changes: regenerate shared-types, then update
 *   this file's Record entries. TypeScript will force you to.
 *
 * PHASE 3 INT BRIDGE — REMOVED in Phase 3b-1
 * -------------------------------------------
 * This file previously exported STATUS_INT_TO_STRING, getConfigFromInt(),
 * and isActiveMemberStatus() as a temporary bridge for admin code that
 * consumed DTOs shipping vettingStatus as a raw integer (UserDto,
 * UserPreviewDto, MemberDetails*, etc.). Phase 3b-1 normalized all those
 * DTOs to ship the enum string, removing every frontend consumer of the
 * int bridge, and this file's int helpers were deleted alongside.
 *
 * If you need the "is this user an active member" predicate, use
 * `isActiveMemberStatusString()`. If you need the config for a given
 * status, use `getConfigFromStatus()`.
 *
 * (The ApplicationUser entity still stores VettingStatus as int for now;
 * Phase 3b-2 will normalize that too. Backend service code that reads
 * user.VettingStatus still uses int comparisons until then.)
 *
 * @see apps/api/Features/Vetting/Entities/VettingApplication.cs — backend enum
 * @see packages/shared-types/src/generated/api-types.ts — generated type
 * @see docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 */

import type { components } from '@witchcityrope/shared-types';

// ============================================================================
// Core Type — re-export from generated shared-types
// ============================================================================

/**
 * VettingStatus — the authoritative type, auto-generated from the C# enum.
 *
 * The generated union is nullable (status field on DTOs is optional), so we
 * strip `undefined` here — consumers get the non-null form, and helper
 * functions below handle the null/undefined cases explicitly.
 */
export type VettingStatus = NonNullable<components['schemas']['VettingStatus']>;

// ============================================================================
// Display configuration types
// ============================================================================

/**
 * Display metadata for a single vetting status. Pure data — no JSX.
 *
 * Components that render alerts with custom link JSX (e.g. VettingAlertBox
 * for InterviewApproved's scheduling link) set `hasCustomLink: true` and
 * the component handles the inline <Anchor> rendering.
 */
export interface VettingStatusDisplay {
  /** Short label for badges and dropdowns, e.g. "Interview Approved" */
  label: string;

  /** Longer label for titles and descriptions, e.g. "Approved for Interview" */
  longLabel: string;

  /** Mantine theme color key for Alert/Badge color prop */
  color: string;

  /** Badge-specific color (may differ from alert color in some themes) */
  badgeColor: string;

  /** Emoji for user-facing alerts (small visual cue) */
  emoji: string;

  /** Plain-text description of what this status means */
  description: string;

  /**
   * Dashboard alert configuration. Null means no alert is shown for this
   * status (e.g. Approved — the happy state has no alert).
   */
  dashboardAlert: DashboardAlertConfig | null;
}

/**
 * Dashboard alert display configuration.
 */
export interface DashboardAlertConfig {
  /** Alert title shown prominently */
  title: string;

  /**
   * Default plain-text message. Used when `hasCustomLink` is false OR when
   * the DTO field needed for the custom link (e.g. interviewScheduleUrl) is
   * missing at runtime. Components should fall back to this.
   */
  message: string;

  /**
   * When true, the component renders custom JSX (typically an <Anchor> link)
   * instead of the plain `message`. The component is responsible for
   * reading the relevant DTO field (interviewScheduleUrl for
   * InterviewApproved, reapplyInfoUrl for Denied) and rendering the link.
   */
  hasCustomLink: boolean;
}

// ============================================================================
// THE Configuration — Record<> enforces exhaustiveness
// ============================================================================

/**
 * Display configuration for every vetting status.
 *
 * The Record<VettingStatus, ...> type forces TypeScript to require an entry
 * for every enum value. If the backend enum adds a new value, this object
 * fails to compile until the new entry is added. This is intentional — it
 * prevents the silent-fallthrough bug class that caused the original issue.
 */
export const VETTING_STATUS_CONFIG: Record<VettingStatus, VettingStatusDisplay> = {
  UnderReview: {
    label: 'Under Review',
    longLabel: 'Application Under Review',
    color: 'blue',
    badgeColor: 'indigo',
    emoji: '⏳',
    description: 'Application submitted and currently being reviewed by the vetting team.',
    dashboardAlert: {
      title: 'Application Under Review',
      message:
        "Your membership application is currently under review. We'll notify you via email once it's been reviewed.",
      hasCustomLink: false,
    },
  },

  InterviewApproved: {
    // Label is "Awaiting Interview" rather than "Interview Approved"
    // because that was the label the production admin UI used before
    // Phase 1 (via VettingStatusBadge.tsx and VettingStatusBox.tsx).
    // "Awaiting Interview" is also semantically better for admin
    // contexts — it describes the applicant's current state rather
    // than the action that was taken to get them there.
    label: 'Awaiting Interview',
    longLabel: 'Approved for Interview',
    color: 'green',
    badgeColor: 'teal',
    emoji: '📅',
    description: 'Approved to schedule a vetting interview.',
    dashboardAlert: {
      title: 'Great News! Your Application Has Been Approved',
      // Fallback text if the VettingStatusDto.interviewScheduleUrl is missing
      message: 'Please schedule your vetting interview to complete your membership.',
      // Component reads VettingStatusDto.interviewScheduleUrl and renders an <Anchor>
      hasCustomLink: true,
    },
  },

  FinalReview: {
    label: 'Final Review',
    longLabel: 'Final Review',
    color: 'blue',
    badgeColor: 'blue',
    emoji: '🔎',
    description: 'Interview completed, application in final review before decision.',
    dashboardAlert: {
      title: 'Application in Final Review',
      message:
        "Your interview has been completed and your application is in final review. We'll notify you of the decision soon.",
      hasCustomLink: false,
    },
  },

  Approved: {
    label: 'Approved',
    longLabel: 'Approved',
    color: 'green',
    badgeColor: 'green',
    emoji: '🎉',
    description: 'Vetting complete — full member access granted.',
    // No alert for the happy state — approved users see no notification banner.
    dashboardAlert: null,
  },

  Denied: {
    label: 'Denied',
    longLabel: 'Application Not Approved',
    color: 'red',
    badgeColor: 'red',
    emoji: '❌',
    description: 'Vetting application was not approved.',
    dashboardAlert: {
      title: 'Application Not Approved',
      // Fallback text if the VettingStatusDto.reapplyInfoUrl is missing
      message: 'Your membership application was not approved at this time.',
      // Component reads VettingStatusDto.reapplyInfoUrl and renders an <Anchor>
      hasCustomLink: true,
    },
  },

  OnHold: {
    label: 'On Hold',
    longLabel: 'Membership On Hold',
    color: 'yellow',
    badgeColor: 'yellow',
    emoji: '⏸️',
    description: 'Membership placed on hold.',
    dashboardAlert: {
      title: 'Membership On Hold',
      message:
        "Your membership is currently on hold. Contact us if you'd like to resume your membership.",
      hasCustomLink: false,
    },
  },

  Withdrawn: {
    label: 'Withdrawn',
    longLabel: 'Application Withdrawn',
    color: 'gray',
    badgeColor: 'gray',
    emoji: '🚫',
    description: 'Application was withdrawn by the applicant.',
    dashboardAlert: {
      title: 'Application Withdrawn',
      message:
        'Your application has been withdrawn. If you would like to reapply, please contact us.',
      hasCustomLink: false,
    },
  },
};

// ============================================================================
// Helper functions — the ONLY approved way to look up config
// ============================================================================

/**
 * Get display config from a VettingStatus enum string.
 *
 * Returns null for null/undefined input so callers can handle the
 * "no status yet" case without crashing. Valid status strings are
 * guaranteed to return a config by the Record<> type above.
 *
 * Use this for code consuming VettingStatusDto, UserProfileDto, or any
 * other DTO where vettingStatus ships as the enum string.
 *
 * @param status The status enum string (or null/undefined)
 * @returns The display config, or null if status is missing
 */
export function getConfigFromStatus(
  status: VettingStatus | null | undefined
): VettingStatusDisplay | null {
  if (!status) return null;
  return VETTING_STATUS_CONFIG[status];
}

// getConfigFromInt was removed in Phase 3b-1 along with STATUS_INT_TO_STRING.
// If you need the display config, use getConfigFromStatus(enumString) instead.

// ============================================================================
// Derived helpers for dropdowns, filters, and UI widgets
// ============================================================================

/**
 * Shape of a dropdown / filter option, matching what Mantine's Select and
 * MultiSelect components expect. Components that need to render a list of
 * vetting statuses as selectable values should use this shape so we can
 * switch the underlying UI library later without touching every callsite.
 */
export interface VettingStatusOption {
  value: VettingStatus;
  label: string;
}

/**
 * Get a list of all vetting statuses as {value, label} options.
 *
 * Used by dropdowns, multi-select filters, and any other UI widget that
 * needs to render every status as a selectable option. The list is built
 * from VETTING_STATUS_CONFIG so adding a new status to the config
 * automatically adds it to every dropdown.
 *
 * The order matches the logical progression of an application through
 * the vetting workflow (Under Review → Interview → Final Review →
 * Approved → terminal states).
 *
 * @returns An array of {value, label} objects suitable for Mantine Select
 */
export function getStatusOptions(): VettingStatusOption[] {
  // Explicit order rather than Object.entries to guarantee logical workflow
  // sequence. The Record<> above doesn't guarantee iteration order and we
  // want the dropdown to read as a progression the user can follow.
  const order: VettingStatus[] = [
    'UnderReview',
    'InterviewApproved',
    'FinalReview',
    'Approved',
    'Denied',
    'OnHold',
    'Withdrawn',
  ];
  return order.map((status) => ({
    value: status,
    label: VETTING_STATUS_CONFIG[status].label,
  }));
}

/**
 * Statuses that indicate an application still needs reviewer attention.
 *
 * Used by the admin dashboard stats and the reviewer filter defaults —
 * anywhere code needs to answer "which applications should a reviewer
 * look at right now?" Exported as a constant so the answer has a single
 * source of truth instead of being inlined in each caller.
 *
 * NOTE: InterviewApproved is intentionally NOT included — once a candidate
 * has been approved for interview, the next action belongs to the
 * applicant (scheduling), not the reviewer.
 */
export const STATUSES_REQUIRING_REVIEW: readonly VettingStatus[] = [
  'UnderReview',
  'FinalReview',
] as const;

/**
 * Default checked statuses for the admin vetting list page's
 * MultiSelect filter on initial load. Broader than
 * STATUSES_REQUIRING_REVIEW because the list view also wants to show
 * applications in InterviewApproved state — those don't need reviewer
 * action but are still "in progress" and worth showing by default so
 * admins don't have to add them manually to see the active workflow.
 *
 * Used by VettingApplicationsList.tsx. If the product defines a new
 * "in progress" status in the future, add it here rather than inlining
 * the array in the component.
 */
export const DEFAULT_VETTING_LIST_FILTERS: readonly VettingStatus[] = [
  'UnderReview',
  'InterviewApproved',
  'FinalReview',
] as const;

/**
 * Statuses that represent a "still in progress" member from the admin
 * dashboard's perspective, used to count "active members" on the admin
 * dashboard card. Includes anyone mid-workflow plus fully-approved members.
 *
 * This replaces the magic-int comparison pattern in AdminDashboardPage
 * (`member.vettingStatus === 0 || === 1 || === 3`) with a named helper
 * that's readable without consulting the enum definition.
 */
const ACTIVE_MEMBER_STATUSES: readonly VettingStatus[] = [
  'UnderReview',
  'InterviewApproved',
  'Approved',
] as const;

/**
 * Check whether a VettingStatus string represents an "active member"
 * for admin dashboard stats purposes.
 *
 * The old `isActiveMemberStatus` (int variant) was deleted in Phase 3b-1
 * alongside the STATUS_INT_TO_STRING bridge. All frontend consumers of
 * admin user DTOs now receive vettingStatus as the enum string, so this
 * string-typed function is the only variant that's needed.
 *
 * @param status The VettingStatus enum string
 * @returns true if the status represents an active member
 */
export function isActiveMemberStatusString(
  status: VettingStatus | null | undefined
): boolean {
  if (!status) return false;
  return ACTIVE_MEMBER_STATUSES.includes(status);
}

/**
 * Check whether a vetting status should cause the "How to Join" menu
 * item to be hidden from the user's main navigation.
 *
 * Business rule: hide the "How to Join" link for users whose application
 * is in a terminal state (Approved, Denied) or paused (OnHold). These
 * users either don't need to apply (Approved) or can't currently act on
 * an application (Denied/OnHold).
 *
 * Moved to this config file in Phase 2a so all status-related business
 * rules live in one place. Previously lived in features/vetting/types/
 * vettingStatus.ts which was the wrong layer (types file, not helpers).
 *
 * @param status The user's current vetting status
 * @returns true if the menu item should be hidden
 */
export function shouldHideMenuForStatus(status: VettingStatus): boolean {
  const hideStatuses: VettingStatus[] = ['OnHold', 'Approved', 'Denied'];
  return hideStatuses.includes(status);
}
