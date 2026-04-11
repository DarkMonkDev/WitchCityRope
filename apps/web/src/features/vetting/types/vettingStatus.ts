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

// API Response wrappers removed - API returns DTOs directly (Pattern B)
// Use VettingStatusDto and MyApplicationStatusResponse directly

// ============================================================================
// Frontend-Only Types (NOT sent to API)
// ============================================================================

/**
 * VettingStatus enum - AUTO-GENERATED from backend
 * @generated from C# VettingStatus enum via OpenAPI
 * DO NOT manually edit - regenerate types if backend changes
 */
export type VettingStatus = components['schemas']['VettingStatus'];

/**
 * Menu visibility decision result
 * Used by useMenuVisibility hook
 */
export interface MenuVisibilityResult {
  shouldShow: boolean;
  reason: string; // For debugging/logging
}

// NOTE (Phase 2h): The `StatusBoxProps` interface was deleted along with
// the VettingStatusBox component and its test file. VettingStatusBox had
// no live consumers after Phase 1 — VettingApplicationPage replaced it
// with VettingAlertBox. The Phase 1 tech-debt doc flagged it for Phase 2
// cleanup and Phase 2h completed the deletion.
//
// NOTE (Phase 2a): `shouldHideMenuForStatus` was moved to
// apps/web/src/features/vetting/constants/vettingStatusConfig.ts so all
// status-related business rules live in the single-source config file.
// Import from there:
//
//   import { shouldHideMenuForStatus } from '../constants/vettingStatusConfig';
