// features/members/api/queries.ts
import { useQuery, useInfiniteQuery } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { UserDto, PaginatedResponse } from '../../../types/api.types'

interface MemberFilters {
  search?: string
  role?: string
  isVetted?: boolean
  limit?: number
}

export function useMembers(filters: MemberFilters = {}) {
  return useQuery({
    queryKey: [...queryKeys.users(), 'members', filters],
    queryFn: async (): Promise<UserDto[]> => {
      const response = await api.get('/api/members', { params: filters })
      return response.data
    },
    staleTime: 15 * 60 * 1000, // 15 minutes - member profiles stable
  })
}

export function useInfiniteMembers(filters: MemberFilters = {}) {
  return useInfiniteQuery({
    queryKey: [...queryKeys.users(), 'infinite', filters],
    queryFn: async ({ pageParam = 1 }): Promise<PaginatedResponse<UserDto>> => {
      const response = await api.get('/api/members', {
        params: { page: pageParam, pageSize: 20, ...filters }
      })
      return response.data
    },
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      return lastPage.hasNext ? lastPage.page + 1 : undefined
    },
    maxPages: 10,
    staleTime: 15 * 60 * 1000,
  })
}

export function useMember(userId: string) {
  return useQuery({
    queryKey: queryKeys.user(userId),
    queryFn: async (): Promise<UserDto> => {
      const response = await api.get(`/api/members/${userId}`)
      return response.data
    },
    enabled: !!userId,
    staleTime: 15 * 60 * 1000,
  })
}