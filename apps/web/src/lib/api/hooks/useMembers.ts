import { useQuery, useMutation, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { apiClient } from '../client'
import { memberKeys, cacheUtils } from '../utils/cache'
import type {
  UserDto,
  UpdateMemberDto,
  StatusUpdateDto,
  MemberFilters
} from '../types/members.types'
import type { PaginatedResponse } from '../types/api.types'

// Context type for optimistic member update rollback
interface OptimisticMemberContext {
  previousMember: UserDto | undefined
}

// Variables type for status update
type StatusUpdateVariables = { id: string; status: string; reason?: string }

// Fetch members list with filters
export function useMembers(filters: MemberFilters = {}) {
  return useQuery({
    queryKey: memberKeys.list(filters),
    queryFn: async (): Promise<UserDto[]> => {
      const { data } = await apiClient.get<UserDto[]>('/api/members', {
        params: filters,
      })
      return data || []
    },
    staleTime: 10 * 60 * 1000, // 10 minutes for member data
  })
}

// Infinite query for member directory with pagination
export function useInfiniteMembers(filters: Omit<MemberFilters, 'page'> = {}) {
  return useInfiniteQuery({
    queryKey: memberKeys.infiniteList(filters),
    queryFn: async ({ pageParam = 1 }): Promise<PaginatedResponse<UserDto>> => {
      const { data } = await apiClient.get<PaginatedResponse<UserDto>>('/api/members', {
        params: { ...filters, page: pageParam, limit: 20 },
      })
      return data || { items: [], page: 1, limit: 20, total: 0, totalPages: 0, hasNext: false, hasPrev: false }
    },
    getNextPageParam: (lastPage: PaginatedResponse<UserDto>) =>
      lastPage.hasNext ? lastPage.page + 1 : undefined,
    initialPageParam: 1,
    staleTime: 10 * 60 * 1000,
  })
}

// Fetch single member
export function useMember(id: string, enabled: boolean = true) {
  return useQuery({
    queryKey: memberKeys.detail(id),
    queryFn: async (): Promise<UserDto> => {
      const { data } = await apiClient.get<UserDto>(`/api/members/${id}`)
      if (!data) throw new Error('Member not found')
      return data
    },
    enabled: !!id && enabled,
    staleTime: 10 * 60 * 1000,
  })
}

// Update member mutation
export function useUpdateMember() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (memberData: UpdateMemberDto): Promise<UserDto> => {
      const { data } = await apiClient.put<UserDto>(`/api/members/${memberData.id}`, memberData)
      if (!data) throw new Error('Failed to update member')
      return data
    },
    onMutate: async (updatedMember: UpdateMemberDto) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: memberKeys.detail(updatedMember.id) })

      // Snapshot previous value
      const previousMember = queryClient.getQueryData(memberKeys.detail(updatedMember.id)) as UserDto | undefined

      // Optimistically update cache
      queryClient.setQueryData(memberKeys.detail(updatedMember.id), (old: UserDto | undefined) => {
        if (!old) return old
        return { ...old, ...updatedMember }
      })

      return { previousMember }
    },
    onError: (err: Error, updatedMember: UpdateMemberDto, context: OptimisticMemberContext | undefined) => {
      // Rollback on error
      if (context?.previousMember) {
        queryClient.setQueryData(memberKeys.detail(updatedMember.id), context.previousMember)
      }
      console.error('Update member failed, rolling back:', err)
    },
    onSettled: (_data: UserDto | undefined, _error: Error | null, updatedMember: UpdateMemberDto) => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: memberKeys.detail(updatedMember.id) })
      cacheUtils.invalidateMembers(queryClient, updatedMember.id)
    },
  })
}

// Update member status with optimistic updates
export function useUpdateMemberStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, status, reason }: StatusUpdateVariables): Promise<UserDto> => {
      const payload: StatusUpdateDto = { status, reason }
      const { data } = await apiClient.put<UserDto>(`/api/members/${id}/status`, payload)
      if (!data) throw new Error('Failed to update member status')
      return data
    },
    onMutate: async ({ id, status }: StatusUpdateVariables) => {
      await queryClient.cancelQueries({ queryKey: memberKeys.detail(id) })

      const previousMember = queryClient.getQueryData(memberKeys.detail(id)) as UserDto | undefined

      // Optimistically update member status
      queryClient.setQueryData(memberKeys.detail(id), (old: UserDto | undefined) => {
        if (!old) return old
        return { ...old, status: status as any }
      })

      return { previousMember }
    },
    onError: (err: Error, { id }: StatusUpdateVariables, context: OptimisticMemberContext | undefined) => {
      if (context?.previousMember) {
        queryClient.setQueryData(memberKeys.detail(id), context.previousMember)
      }
      console.error('Status update failed, rolling back:', err)
    },
    onSettled: (_data: UserDto | undefined, _error: Error | null, { id }: StatusUpdateVariables) => {
      queryClient.invalidateQueries({ queryKey: memberKeys.detail(id) })
      cacheUtils.invalidateMembers(queryClient, id)
    },
  })
}

// Bulk operations for admin use
export function useBulkUpdateMembers() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (updates: { memberIds: string[]; updates: Partial<UserDto> }): Promise<UserDto[]> => {
      const { data } = await apiClient.put<UserDto[]>('/api/members/bulk', updates)
      return data || []
    },
    onSuccess: (updatedMembers: UserDto[]) => {
      // Update individual member caches
      updatedMembers.forEach((member: UserDto) => {
        queryClient.setQueryData(memberKeys.detail(member.id || ''), member)
      })

      // Invalidate lists
      cacheUtils.invalidateMembers(queryClient)
      console.log(`Bulk updated ${updatedMembers.length} members`)
    },
    onError: (error: Error) => {
      console.error('Bulk update failed:', error)
    },
  })
}

// Member search hook with debouncing
export function useMemberSearch(searchTerm: string, enabled: boolean = true) {
  return useQuery({
    queryKey: [...memberKeys.lists(), 'search', searchTerm],
    queryFn: async (): Promise<UserDto[]> => {
      if (!searchTerm.trim()) return []

      const { data } = await apiClient.get<UserDto[]>('/api/members/search', {
        params: { q: searchTerm, limit: 10 },
      })
      return data || []
    },
    enabled: enabled && searchTerm.length >= 2,
    staleTime: 30 * 1000, // 30 seconds for search results
  })
}
