/**
 * Vetting Status Types - Using Auto-Generated Types
 *
 * DTO ALIGNMENT STRATEGY - CRITICAL RULES:
 * ════════════════════════════════════════
 * 1. API DTOs (C#) are the SOURCE OF TRUTH
 * 2. TypeScript types are AUTO-GENERATED from OpenAPI spec via @witchcityrope/shared-types
 * 3. NEVER manually create TypeScript interfaces for API response data
 * 4. If a type is missing, expose it in the backend API (add .Produces<> to endpoint)
 * 5. Regenerate types: cd packages/shared-types && npm run generate
 *
 * WHY: Prevents type mismatches, ensures type safety, eliminates manual sync work
 * SEE: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 * ════════════════════════════════════════
 */

import type { components } from '@witchcityrope/shared-types';

// ============================================================================
// Generated API DTOs
// ============================================================================

/**
 * Vetting Status DTO
 * @generated from C# VettingStatusDto via NSwag
 */
export type VettingStatusDto = components['schemas']['VettingStatusDto'];

/**
 * Application Status Information DTO
 * @generated from C# ApplicationStatusInfo via NSwag
 */
export type ApplicationStatusInfo = components['schemas']['ApplicationStatusInfo'];

/**
 * My Application Status Response DTO
 * @generated from C# MyApplicationStatusResponse via NSwag
 */
export type MyApplicationStatusResponse = components['schemas']['MyApplicationStatusResponse'];

/**
 * API Response wrapper for Vetting Status
 * @generated from C# ApiResponseOfVettingStatusDto via NSwag
 */
export type ApiResponseOfVettingStatusDto = components['schemas']['ApiResponseOfVettingStatusDto'];

/**
 * API Response wrapper for My Application Status Response
 * @generated from C# ApiResponseOfMyApplicationStatusResponse via NSwag
 */
export type ApiResponseOfMyApplicationStatusResponse = components['schemas']['ApiResponseOfMyApplicationStatusResponse'];

// ============================================================================
// Type Aliases for Convenience
// ============================================================================

/**
 * Alias for ApiResponseOfMyApplicationStatusResponse
 * Used by useVettingStatus hook
 */
export type VettingStatusApiResponse = ApiResponseOfMyApplicationStatusResponse;

// ============================================================================
// Frontend-Only Types (NOT sent to API)
// ============================================================================

/**
 * VettingStatus enum values (Frontend constants)
 * Note: Backend uses string type, not enum. These are frontend constants for type safety.
 * MUST match backend string values exactly.
 */
export type VettingStatus =
  | 'UnderReview'
  | 'InterviewApproved'
  | 'InterviewCompleted'
  | 'FinalReview'
  | 'Approved'
  | 'Denied'
  | 'OnHold'
  | 'Withdrawn';

/**
 * Menu visibility decision result
 * Used by useMenuVisibility hook
 */
export interface MenuVisibilityResult {
  shouldShow: boolean;
  reason: string; // For debugging/logging
}

/**
 * Status box props for VettingStatusBox component
 */
export interface StatusBoxProps {
  status: VettingStatus;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}

/**
 * Helper: Check if status should hide "How to Join" menu item
 * Business rule: Hide for OnHold, Approved, Denied
 */
export const shouldHideMenuForStatus = (status: VettingStatus): boolean => {
  const hideStatuses: VettingStatus[] = ['OnHold', 'Approved', 'Denied'];
  return hideStatuses.includes(status);
};
