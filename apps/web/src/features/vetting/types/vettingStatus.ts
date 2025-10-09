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

/**
 * VettingStatus enum - AUTO-GENERATED from backend
 * String union type from OpenAPI spec
 */
export type VettingStatus = components['schemas']['VettingStatus'];

/**
 * Application status information from API - AUTO-GENERATED
 * Matches ApplicationStatusInfo C# DTO
 */
export type ApplicationStatusInfo = components['schemas']['ApplicationStatusInfo'];

/**
 * My application status response from GET /api/vetting/status - AUTO-GENERATED
 * Matches MyApplicationStatusResponse C# DTO
 */
export type MyApplicationStatusResponse = components['schemas']['MyApplicationStatusResponse'];

/**
 * API response wrapper for vetting status - AUTO-GENERATED
 * Matches MyApplicationStatusResponseApiResponse from backend
 */
export type VettingStatusApiResponse = components['schemas']['MyApplicationStatusResponseApiResponse'];

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
