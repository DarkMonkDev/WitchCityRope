// React Query hooks for RSVP and participation functionality
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api/client';
import {
  ParticipationStatusDto,
  CreateRSVPRequest,
  UserParticipationDto
} from '../types/participation.types';

// Query keys for participation data
export const participationKeys = {
  all: ['participation'] as const,
  eventStatus: (eventId: string) => [...participationKeys.all, 'event', eventId] as const,
  userParticipations: () => [...participationKeys.all, 'user'] as const,
};

// Check user's RSVP status for a specific event
export function useParticipation(eventId: string, enabled = true) {
  return useQuery<ParticipationStatusDto>({
    queryKey: participationKeys.eventStatus(eventId),
    queryFn: async (): Promise<ParticipationStatusDto> => {
      console.log('🔍 useParticipation: Fetching participation for event:', eventId);
      const { data } = await apiClient.get(`/api/events/${eventId}/participation`);
      console.log('🔍 useParticipation: API response:', data);

      // Validate API response
      if (!data || typeof data === 'string') {
        throw new Error('Invalid API response: participation endpoint returned unexpected data format');
      }

      return data;
    },
    enabled: enabled && !!eventId,
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

      // Invalidate user participations to refresh the list
      queryClient.invalidateQueries({
        queryKey: participationKeys.userParticipations()
      });

      // Invalidate user events for dashboard
      queryClient.invalidateQueries({
        queryKey: ['user-events']
      });
    },
    onError: (error: any) => {
      console.error('Failed to create RSVP:', error);
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
      // Update the participation status cache to reflect cancellation
      queryClient.setQueryData(
        participationKeys.eventStatus(variables.eventId),
        (old: ParticipationStatusDto | undefined): ParticipationStatusDto => ({
          hasRSVP: false,
          hasTicket: old?.hasTicket || false,
          rsvp: old?.rsvp ? {
            ...old.rsvp,
            status: 'Cancelled' as any,
            canceledAt: new Date().toISOString(),
            cancelReason: variables.reason
          } : null,
          ticket: old?.ticket || null,
          canRSVP: true,
          canPurchaseTicket: old?.canPurchaseTicket || true,
          capacity: old?.capacity ? {
            ...old.capacity,
            current: old.capacity.current - 1,
            available: old.capacity.available + 1
          } : undefined
        })
      );

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
    },
    onError: (error: any) => {
      console.error('Failed to cancel RSVP:', error);
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
      // Update the participation status cache to reflect ticket cancellation
      queryClient.setQueryData(
        participationKeys.eventStatus(variables.eventId),
        (old: ParticipationStatusDto | undefined): ParticipationStatusDto => ({
          hasRSVP: old?.hasRSVP || false,
          hasTicket: false,
          rsvp: old?.rsvp || null,
          ticket: old?.ticket ? {
            ...old.ticket,
            status: 'Cancelled' as any,
            canceledAt: new Date().toISOString(),
            cancelReason: variables.reason
          } : null,
          canRSVP: old?.canRSVP || true,
          canPurchaseTicket: true,
          capacity: old?.capacity ? {
            ...old.capacity,
            current: old.capacity.current - 1,
            available: old.capacity.available + 1
          } : undefined
        })
      );

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
    },
    onError: (error: any) => {
      console.error('Failed to cancel ticket:', error);
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