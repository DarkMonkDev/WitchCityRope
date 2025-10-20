/**
 * Admin Members Management Types
 *
 * Uses generated types from @witchcityrope/shared-types following DTO Alignment Strategy
 * DO NOT create manual interfaces - use generated types from API
 */

import type { components } from '@witchcityrope/shared-types';

// Re-export generated types for convenience
export type UserDto = components['schemas']['UserDto'];
export type UserListResponse = components['schemas']['UserListResponse'];

// Filter request interface for members list
export interface MemberFilterRequest {
  searchQuery?: string;
  roleFilters: string[]; // MultiSelect: array of selected roles
  page: number;
  pageSize: number;
  sortBy: string;
  sortDirection: 'Asc' | 'Desc';
}
