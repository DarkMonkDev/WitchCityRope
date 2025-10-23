/**
 * Admin Members Management Types
 *
 * Uses generated types from @witchcityrope/shared-types following DTO Alignment Strategy
 * DO NOT create manual interfaces - use generated types from API
 */

import type { components, operations } from '@witchcityrope/shared-types';

// ============================================================================
// API DTO Types - Generated from Backend
// ============================================================================

/**
 * User data transfer object
 * @generated from components['schemas']['UserDto']
 */
export type UserDto = components['schemas']['UserDto'];

/**
 * Paginated user list response
 * @generated from components['schemas']['UserListResponse']
 */
export type UserListResponse = components['schemas']['UserListResponse'];

// ============================================================================
// API Query Parameter Types - Generated from Backend
// ============================================================================

/**
 * Query parameters for GetUsers operation
 * @generated from operations['GetUsers']['parameters']['query']
 */
export type GetUsersParams = operations['GetUsers']['parameters']['query'];

// ============================================================================
// Frontend-Specific Types (NOT sent to API)
// ============================================================================

/**
 * Frontend filter state for members list component
 *
 * This interface is used ONLY for component state management.
 * It is transformed into GetUsersParams before API calls.
 *
 * Key differences from API:
 * - Uses lowercase naming for frontend conventions
 * - Uses 'Asc' | 'Desc' instead of boolean for sortDirection
 * - Combines search/filter state in one object
 *
 * @see GetUsersParams for the actual API parameter type
 */
export interface MemberFilterRequest {
  /** Search term for filtering (maps to SearchTerm in API) */
  searchQuery?: string;
  /** Array of role filters for OR filtering (maps to RoleFilters in API) */
  roleFilters: string[];
  /** Current page number (maps to Page in API) */
  page: number;
  /** Items per page (maps to PageSize in API) */
  pageSize: number;
  /** Field to sort by (maps to SortBy in API) */
  sortBy: string;
  /** Sort direction: 'Asc' or 'Desc' (maps to SortDescending boolean in API) */
  sortDirection: 'Asc' | 'Desc';
}
