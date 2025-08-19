// features/members/api/mutations.ts
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../../api/client'
import { queryKeys } from '../../../api/queryKeys'
import type { UserDto } from '@witchcityrope/shared-types'

interface UpdateMemberStatusData {
  id: string
  status: 'vetted' | 'unvetted'
  reason?: string
}

interface UpdateProfileData {
  firstName?: string
  lastName?: string
  sceneName?: string
  bio?: string
}

// CSS-only optimistic updates (simple changes)
export function useUpdateProfile() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async (data: UpdateProfileData) => {
      const response = await api.put('/api/members/profile', data)
      return response.data
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.currentUser() })
    },
  })
}

// Cache-based optimistic updates (complex changes)
export function useUpdateMemberStatus() {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: async ({ id, status, reason }: UpdateMemberStatusData): Promise<UserDto> => {
      const response = await api.put(`/api/members/${id}/status`, { status, reason })
      return response.data
    },
    onMutate: async ({ id, status }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.user(id) })
      
      // Snapshot previous value
      const previousUser = queryClient.getQueryData(queryKeys.user(id)) as UserDto | undefined
      
      // Optimistically update user
      if (previousUser) {
        queryClient.setQueryData(queryKeys.user(id), (old: UserDto) => ({
          ...old,
          isVetted: status === 'vetted',
        }))
      }
      
      // Return rollback context
      return { previousUser, id }
    },
    onError: (error, variables, context) => {
      // Rollback on error
      if (context?.previousUser) {
        queryClient.setQueryData(
          queryKeys.user(context.id),
          context.previousUser
        )
      }
      console.error('Status update failed:', error)
    },
    onSettled: (data, error, variables) => {
      // Always refetch to ensure server state
      queryClient.invalidateQueries({ queryKey: queryKeys.user(variables.id) })
      queryClient.invalidateQueries({ queryKey: queryKeys.users() })
    },
  })
}