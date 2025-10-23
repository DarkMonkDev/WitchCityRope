/**
 * CMS Types - Using Auto-Generated Types
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
 * Content Page DTO
 * @generated from C# ContentPageDto via NSwag
 */
export type ContentPageDto = components['schemas']['ContentPageDto'];

/**
 * CMS Page Summary DTO (for list views)
 * @generated from C# CmsPageSummaryDto via NSwag
 */
export type CmsPageSummaryDto = components['schemas']['CmsPageSummaryDto'];

/**
 * Update Content Page Request DTO
 * @generated from C# UpdateContentPageRequest via NSwag
 */
export type UpdateContentPageRequest = components['schemas']['UpdateContentPageRequest'];

/**
 * Content Revision DTO (for revision history)
 * @generated from C# ContentRevisionDto via NSwag
 */
export type ContentRevisionDto = components['schemas']['ContentRevisionDto'];

// ============================================================================
// Frontend-Only Types (NOT sent to API)
// ============================================================================

/**
 * CMS Page Filters (Frontend state - NOT sent to API)
 * Used for client-side filtering and search
 */
export interface CmsPageFilters {
  searchQuery?: string;
  showOnlyPublished?: boolean;
  sortBy?: 'title' | 'updatedAt' | 'revisionCount';
  sortDirection?: 'asc' | 'desc';
}

/**
 * CMS Editor State (Frontend-only)
 * Used to manage editor UI state
 */
export interface CmsEditorState {
  isEditing: boolean;
  isDirty: boolean;
  lastSavedAt?: string;
  autoSaveEnabled: boolean;
}
