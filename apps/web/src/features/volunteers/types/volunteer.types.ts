/**
 * Volunteer Types - Using Auto-Generated Types
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
 * Volunteer Position DTO (full detail with session info)
 * @generated from C# VolunteerPositionDto2 via NSwag
 * Note: VolunteerPositionDto2 includes extended fields like eventId, sessionName, hasUserSignedUp
 */
export type VolunteerPositionDto = components['schemas']['VolunteerPositionDto2'];

/**
 * Volunteer Signup DTO
 * @generated from C# VolunteerSignupDto via NSwag
 */
export type VolunteerSignupDto = components['schemas']['VolunteerSignupDto'];

/**
 * Volunteer Assignment DTO (for admin assignment operations)
 * @generated from C# VolunteerAssignmentDto via NSwag
 */
export type VolunteerAssignmentDto = components['schemas']['VolunteerAssignmentDto'];

// ============================================================================
// Frontend-Only Types (NOT sent to API)
// ============================================================================

/**
 * Request to sign up for a volunteer position
 * Frontend form data structure
 */
export interface VolunteerSignupRequest {
  // Empty - no additional data required for signup
  // User ID is inferred from authentication context
}

/**
 * Volunteer Position Filters (Frontend state - NOT sent to API)
 * Used for client-side filtering and UI state management
 */
export interface VolunteerPositionFilters {
  showOnlyAvailable?: boolean;
  sessionId?: string | null;
  requiresExperience?: boolean | null;
  searchQuery?: string;
}

// ============================================================================
// Type Aliases for Backwards Compatibility
// ============================================================================

/**
 * Alias for VolunteerPositionDto
 * @deprecated Use VolunteerPositionDto directly
 */
export type VolunteerPosition = VolunteerPositionDto;

/**
 * Alias for VolunteerSignupDto
 * @deprecated Use VolunteerSignupDto directly
 */
export type VolunteerSignup = VolunteerSignupDto;
