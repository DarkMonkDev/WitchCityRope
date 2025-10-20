import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../client'
import type {
  MemberDetailsResponse,
  VettingDetailsResponse,
  EventHistoryResponse,
  VolunteerHistoryResponse,
  MemberIncidentsResponse,
  UserNoteResponse,
  CreateUserNoteRequest,
  UpdateMemberStatusRequest,
  UpdateMemberRoleRequest,
} from '../types/member-details.types'
import type { ApiResponse } from '../types/api.types'

// Query keys for caching
export const memberDetailsKeys = {
  all: ['member-details'] as const,
  details: (userId: string) => [...memberDetailsKeys.all, 'details', userId] as const,
  vetting: (userId: string) => [...memberDetailsKeys.all, 'vetting', userId] as const,
  eventHistory: (userId: string, page: number, pageSize: number) =>
    [...memberDetailsKeys.all, 'event-history', userId, page, pageSize] as const,
  volunteerHistory: (userId: string, page: number, pageSize: number) =>
    [...memberDetailsKeys.all, 'volunteer-history', userId, page, pageSize] as const,
  incidents: (userId: string) => [...memberDetailsKeys.all, 'incidents', userId] as const,
  notes: (userId: string) => [...memberDetailsKeys.all, 'notes', userId] as const,
}

// Fetch comprehensive member details
export function useMemberDetails(userId: string, enabled: boolean = true) {
  return useQuery<MemberDetailsResponse>({
    queryKey: memberDetailsKeys.details(userId),
    queryFn: async (): Promise<MemberDetailsResponse> => {
      console.log('[useMemberDetails] Fetching details for user:', userId)
      try {
        const { data } = await apiClient.get<ApiResponse<MemberDetailsResponse>>(
          `/api/users/${userId}/details`
        )
        console.log('[useMemberDetails] Response received:', {
          hasData: !!data.data,
          status: data.success,
        })
        if (!data.data) throw new Error('Member details not found')
        return data.data
      } catch (error: any) {
        console.error('[useMemberDetails] Error fetching details:', {
          status: error.response?.status,
          message: error.message,
        })
        throw error
      }
    },
    enabled: !!userId && enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

// Fetch vetting details and questionnaire
export function useMemberVetting(userId: string, enabled: boolean = true) {
  return useQuery<VettingDetailsResponse>({
    queryKey: memberDetailsKeys.vetting(userId),
    queryFn: async (): Promise<VettingDetailsResponse> => {
      const { data } = await apiClient.get<ApiResponse<VettingDetailsResponse>>(
        `/api/users/${userId}/vetting-details`
      )
      if (!data.data) throw new Error('Vetting details not found')
      return data.data
    },
    enabled: !!userId && enabled,
    staleTime: 10 * 60 * 1000, // 10 minutes (vetting data changes less frequently)
  })
}

// Fetch paginated event history
export function useMemberEventHistory(
  userId: string,
  page: number = 1,
  pageSize: number = 20,
  enabled: boolean = true
) {
  return useQuery<EventHistoryResponse>({
    queryKey: memberDetailsKeys.eventHistory(userId, page, pageSize),
    queryFn: async (): Promise<EventHistoryResponse> => {
      const { data } = await apiClient.get<ApiResponse<EventHistoryResponse>>(
        `/api/users/${userId}/event-history`,
        {
          params: { page, pageSize },
        }
      )
      if (!data.data) throw new Error('Event history not found')
      return data.data
    },
    enabled: !!userId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes
    keepPreviousData: true, // Keep previous data while fetching new page
  })
}

// Fetch paginated volunteer history
export function useMemberVolunteerHistory(
  userId: string,
  page: number = 1,
  pageSize: number = 20,
  enabled: boolean = true
) {
  return useQuery<VolunteerHistoryResponse>({
    queryKey: memberDetailsKeys.volunteerHistory(userId, page, pageSize),
    queryFn: async (): Promise<VolunteerHistoryResponse> => {
      // TODO: Backend endpoint not yet implemented
      // Return empty data for now
      console.log('[useMemberVolunteerHistory] Backend endpoint not yet implemented for user:', userId)
      return {
        volunteers: [],
        totalCount: 0,
        page,
        pageSize,
        totalPages: 0,
      }
    },
    enabled: !!userId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes
    keepPreviousData: true, // Keep previous data while fetching new page
  })
}

// Fetch member incidents
export function useMemberIncidents(userId: string, enabled: boolean = true) {
  return useQuery<MemberIncidentsResponse>({
    queryKey: memberDetailsKeys.incidents(userId),
    queryFn: async (): Promise<MemberIncidentsResponse> => {
      const { data } = await apiClient.get<ApiResponse<MemberIncidentsResponse>>(
        `/api/users/${userId}/incidents`
      )
      if (!data.data) throw new Error('Incidents not found')
      return data.data
    },
    enabled: !!userId && enabled,
    staleTime: 10 * 60 * 1000, // 10 minutes
  })
}

// Fetch member notes (unified notes system)
export function useMemberNotes(userId: string, enabled: boolean = true) {
  return useQuery<UserNoteResponse[]>({
    queryKey: memberDetailsKeys.notes(userId),
    queryFn: async (): Promise<UserNoteResponse[]> => {
      const { data } = await apiClient.get<ApiResponse<UserNoteResponse[]>>(
        `/api/users/${userId}/notes`
      )
      return data.data || []
    },
    enabled: !!userId && enabled,
    staleTime: 1 * 60 * 1000, // 1 minute (notes updated frequently)
  })
}

// Create member note mutation
export function useCreateMemberNote() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: CreateUserNoteRequest
    }): Promise<UserNoteResponse> => {
      const { data } = await apiClient.post<ApiResponse<UserNoteResponse>>(
        `/api/users/${userId}/notes`,
        request
      )
      if (!data.data) throw new Error('Failed to create note')
      return data.data
    },
    onSuccess: (_, variables) => {
      // Invalidate notes query to refetch
      queryClient.invalidateQueries({ queryKey: memberDetailsKeys.notes(variables.userId) })
    },
    onError: (error) => {
      console.error('Failed to create note:', error)
    },
  })
}

// Update member status mutation
export function useUpdateMemberStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: UpdateMemberStatusRequest
    }): Promise<void> => {
      await apiClient.put(`/api/users/${userId}/status`, request)
    },
    onSuccess: (_, variables) => {
      // Invalidate all member details queries
      queryClient.invalidateQueries({ queryKey: memberDetailsKeys.details(variables.userId) })
      queryClient.invalidateQueries({ queryKey: memberDetailsKeys.notes(variables.userId) })
    },
    onError: (error) => {
      console.error('Failed to update status:', error)
    },
  })
}

// Update member role mutation
export function useUpdateMemberRole() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      userId,
      request,
    }: {
      userId: string
      request: UpdateMemberRoleRequest
    }): Promise<void> => {
      await apiClient.put(`/api/users/${userId}/role`, request)
    },
    onSuccess: (_, variables) => {
      // Invalidate member details
      queryClient.invalidateQueries({ queryKey: memberDetailsKeys.details(variables.userId) })
    },
    onError: (error) => {
      console.error('Failed to update role:', error)
    },
  })
}
