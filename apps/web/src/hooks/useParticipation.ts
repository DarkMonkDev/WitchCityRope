// React Query hooks for RSVP and participation functionality
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { apiClient } from '../lib/api/client';
import {
  ParticipationStatusDto,
  EnhancedParticipationStatusDto,
  CreateRSVPRequest,
  UserParticipationDto
} from '../types/participation.types';
import { debugLog, debugError } from '../utils/debug';

// Query keys for participation data
export const participationKeys = {
  all: ['participation'] as const,
  eventStatus: (eventId: string) => [...participationKeys.all, 'event', eventId] as const,
  userParticipations: () => [...participationKeys.all, 'user'] as const,
};

// Check user's RSVP status for a specific event
// CRITICAL: Requires authentication - only enabled when user is logged in
export function useParticipation(eventId: string, isAuthenticated: boolean, enabled = true) {
  return useQuery<EnhancedParticipationStatusDto>({
    queryKey: participationKeys.eventStatus(eventId),
    queryFn: async (): Promise<EnhancedParticipationStatusDto> => {
      debugLog('🔍 useParticipation: Fetching participation for event:', eventId);
      const { data } = await apiClient.get(`/api/events/${eventId}/participation`);
      debugLog('🔍 useParticipation: API response:', data);

      // Validate API response
      if (!data || typeof data === 'string') {
        throw new Error('Invalid API response: participation endpoint returned unexpected data format');
      }

      return data;
    },
    // CRITICAL: Only fetch when user is authenticated (API endpoint requires [Authorize])
    enabled: enabled && !!eventId && isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true
  });
}

// Create RSVP mutation
export function useCreateRSVP() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateRSVPRequest): Promise<ParticipationStatusDto> => {
      const { data } = await apiClient.post(`/api/events/${request.eventId}/rsvp`, request);
      return data;
    },
    onSuccess: (data, variables) => {
      // Update the participation status cache
      queryClient.setQueryData(
        participationKeys.eventStatus(variables.eventId),
        data
      );

      // Invalidate participation status to trigger re-render in all consuming components
      queryClient.invalidateQueries({
        queryKey: participationKeys.eventStatus(variables.eventId)
      });

      // Invalidate user participations to refresh the list
      queryClient.invalidateQueries({
        queryKey: participationKeys.userParticipations()
      });

      // Invalidate user events for dashboard
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });

      // Invalidate volunteer positions to update ToS checkbox visibility
      queryClient.invalidateQueries({
        queryKey: ['volunteerPositions', variables.eventId]
      });
    },
    onError: (error: any) => {
      debugError('Failed to create RSVP:', error);
    }
  });
}

// Cancel RSVP mutation
export function useCancelRSVP() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ eventId, reason }: { eventId: string; reason?: string }): Promise<void> => {
      await apiClient.delete(`/api/events/${eventId}/rsvp`, {
        params: { reason }
      });
    },
    onSuccess: (_, variables) => {
      // Invalidate participation status to fetch fresh data from API
      queryClient.invalidateQueries({
        queryKey: participationKeys.eventStatus(variables.eventId)
      });

      // Invalidate user participations to refresh the list
      queryClient.invalidateQueries({
        queryKey: participationKeys.userParticipations()
      });

      // Invalidate user events for dashboard
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });

      // Invalidate admin event participations table
      queryClient.invalidateQueries({
        queryKey: ['events', variables.eventId, 'participations']
      });

      // Invalidate volunteer positions to refresh the volunteer opportunities list
      queryClient.invalidateQueries({
        queryKey: ['volunteerPositions', variables.eventId]
      });

      // Invalidate user volunteer shifts to update "You're Volunteering" section
      queryClient.invalidateQueries({
        queryKey: ['userVolunteerShifts']
      });

      // Invalidate event details to refresh registration count and capacity
      queryClient.invalidateQueries({
        queryKey: ['events', 'detail', variables.eventId]
      });

      // Invalidate the participation status itself to trigger refetch
      queryClient.invalidateQueries({
        queryKey: participationKeys.eventStatus(variables.eventId)
      });
    },
    onError: (error: any) => {
      debugError('Failed to cancel RSVP:', error);

      // Extract error message from API response
      const errorMessage = error.response?.data?.detail
        || error.response?.data?.title
        || error.message
        || 'Unable to cancel RSVP. Please try again or contact support.';

      // Show error notification to user
      notifications.show({
        title: 'Cancellation Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000,
      });
    }
  });
}

// Cancel ticket mutation
export function useCancelTicket() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ eventId, reason }: { eventId: string; reason?: string }): Promise<void> => {
      await apiClient.delete(`/api/events/${eventId}/participation`, {
        params: { type: 'ticket', reason }
      });
    },
    onSuccess: (_, variables) => {
      // Invalidate all relevant caches to fetch fresh data
      queryClient.invalidateQueries({
        queryKey: participationKeys.eventStatus(variables.eventId)
      });

      queryClient.invalidateQueries({
        queryKey: participationKeys.userParticipations()
      });

      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });

      queryClient.invalidateQueries({
        queryKey: ['events', variables.eventId, 'participations']
      });

      queryClient.invalidateQueries({
        queryKey: ['volunteerPositions', variables.eventId]
      });

      queryClient.invalidateQueries({
        queryKey: ['userVolunteerShifts']
      });

      queryClient.invalidateQueries({
        queryKey: ['events', 'detail', variables.eventId]
      });
    },
    onError: (error: any) => {
      debugError('Failed to cancel ticket:', error);

      // Extract error message from API response
      const errorMessage = error.response?.data?.detail
        || error.response?.data?.title
        || error.message
        || 'Unable to cancel ticket. Please try again or contact support.';

      // Show error notification to user
      notifications.show({
        title: 'Cancellation Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 7000,
      });
    }
  });
}

// Get user's all participations
export function useUserParticipations(enabled = true) {
  return useQuery<UserParticipationDto[]>({
    queryKey: participationKeys.userParticipations(),
    queryFn: async (): Promise<UserParticipationDto[]> => {
      const { data } = await apiClient.get('/api/user/participations');
      return data;
    },
    enabled,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}