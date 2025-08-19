import type { PaginationParams } from './api.types'

// Re-export UserDto from generated types
export type { UserDto } from '@witchcityrope/shared-types'

// Member/User models extending generated types
// Note: Using generated UserDto from @witchcityrope/shared-types

export interface UpdateMemberDto {
  id: string
  sceneName?: string
  status?: string
  role?: string
  bio?: string
}

export interface StatusUpdateDto {
  status: string
  reason?: string
}

export interface MemberFilters extends PaginationParams {
  role?: string
  status?: string
  search?: string
  sortBy?: 'sceneName' | 'createdAt' | 'lastLoginAt'
  sortOrder?: 'asc' | 'desc'
}

// Query keys for type safety
export type MemberQueryKey = 
  | ['members']
  | ['members', 'list'] 
  | ['members', 'list', MemberFilters]
  | ['members', 'detail']
  | ['members', 'detail', string]