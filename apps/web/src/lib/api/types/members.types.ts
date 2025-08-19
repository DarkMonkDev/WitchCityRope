import type { PaginationParams } from './api.types'

// Member/User models extending existing auth types
export interface UserDto {
  id: string
  email: string
  sceneName: string
  status?: 'active' | 'pending' | 'suspended' | 'vetted'
  role?: 'admin' | 'teacher' | 'vetted_member' | 'general_member' | 'guest'
  createdAt: string
  lastLoginAt?: string
  profilePicture?: string
  bio?: string
}

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