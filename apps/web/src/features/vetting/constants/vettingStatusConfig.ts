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
 * PHASE 3 TEMPORARY BRIDGE (STATUS_INT_TO_STRING)
 * ------------------------------------------------
 * Some backend DTOs (UserDto, MemberDetails*) currently serialize
 * vettingStatus as a raw int instead of the enum string. This is
 * architecturally inconsistent and will be normalized in Phase 3 of the
 * vetting status cleanup. Until then, admin code that consumes those
 * DTOs uses `getConfigFromInt()` which goes through `STATUS_INT_TO_STRING`.
 *
 * When Phase 3 lands:
 *   - All backend DTOs will ship vettingStatus as the enum string
 *   - STATUS_INT_TO_STRING can be deleted
 *   - getConfigFromInt() can be deleted
 *   - All call sites will use getConfigFromStatus() only
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
// Temporary int-to-string bridge (Phase 3 will delete this)
// ============================================================================

/**
 * Integer → enum string mapping.
 *
 * TEMPORARY — exists only because some backend DTOs still serialize
 * vettingStatus as a raw int (UserDto, MemberDetailsDto, etc.) rather than
 * the enum string. Admin frontend code that consumes those DTOs uses
 * `getConfigFromInt()` which delegates through this map.
 *
 * The integer values come from apps/api/Features/Vetting/Entities/VettingApplication.cs.
 * If those values ever change on the backend, this map must be updated in lockstep.
 *
 * DELETE THIS MAP in Phase 3 once all backend DTOs use the enum type.
 */
export const STATUS_INT_TO_STRING: Record<number, VettingStatus> = {
  0: 'UnderReview',
  1: 'InterviewApproved',
  2: 'FinalReview',
  3: 'Approved',
  4: 'Denied',
  5: 'OnHold',
  6: 'Withdrawn',
};

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
    label: 'Interview Approved',
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

/**
 * Get display config from a raw integer vetting status.
 *
 * TEMPORARY — exists only for admin code consuming UserDto / MemberDetailsDto
 * which still ship vettingStatus as int. Will be removed in Phase 3 of the
 * vetting status cleanup once all backend DTOs use the enum type.
 *
 * Returns null for null/undefined/out-of-range input. Valid integers map to
 * their corresponding VettingStatus string via STATUS_INT_TO_STRING, then
 * look up the config.
 *
 * @param value The raw integer vetting status (or null/undefined)
 * @returns The display config, or null if value is missing or invalid
 */
export function getConfigFromInt(
  value: number | null | undefined
): VettingStatusDisplay | null {
  if (value == null) return null;
  const key = STATUS_INT_TO_STRING[value];
  return key ? VETTING_STATUS_CONFIG[key] : null;
}
