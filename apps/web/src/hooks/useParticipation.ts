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
      try {
        console.log('🔍 useParticipation: Fetching participation for event:', eventId);
        const { data } = await apiClient.get(`/api/events/${eventId}/participation`);
        console.log('🔍 useParticipation: API response:', data);

        // If API returns empty string or invalid data, return mock
        if (!data || typeof data === 'string') {
          console.warn('🔍 useParticipation: Invalid API response, using mock data');
          return {
            hasRSVP: false,
            hasTicket: false,
            rsvp: null,
            ticket: null,
            canRSVP: true,
            canPurchaseTicket: true,
            capacity: {
              total: 20,
              current: 5,
              available: 15
            }
          };
        }

        return data;
      } catch (error: any) {
        // If endpoint doesn't exist yet, return mock data for development
        if (error.response?.status === 404) {
          console.warn('🔍 useParticipation: 404 - Participation endpoint not found, using mock data');
          return {
            hasRSVP: false,
            hasTicket: false,
            rsvp: null,
            ticket: null,
            canRSVP: true,
            canPurchaseTicket: true,
            capacity: {
              total: 20,
              current: 5,
              available: 15
            }
          };
        }
        console.error('🔍 useParticipation: Error fetching participation:', error);
        throw error;
      }
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
      try {
        const { data } = await apiClient.post(`/api/events/${request.eventId}/rsvp`, request);
        return data;
      } catch (error: any) {
        // Mock response for development
        if (error.response?.status === 404) {
          console.warn('RSVP endpoint not found, using mock response');
          // Return updated participation status showing user is now RSVP'd
          return {
            hasRSVP: true,
            hasTicket: false,
            rsvp: {
              id: `rsvp-${Date.now()}`,
              status: 'Active' as any,
              createdAt: new Date().toISOString()
            },
            ticket: null,
            canRSVP: false,  // Can't RSVP again
            canPurchaseTicket: true,  // Can still purchase ticket
            capacity: {
              total: 20,
              current: 6,
              available: 14
            }
          };
        }
        throw error;
      }
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
    },
    onError: (error: any) => {
      console.error('Failed to create RSVP:', error);
    }
  });
}

// Cancel participation mutation
export function useCancelRSVP() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ eventId, reason }: { eventId: string; reason?: string }): Promise<void> => {
      try {
        await apiClient.delete(`/api/events/${eventId}/rsvp`, {
          params: { reason }
        });
      } catch (error: any) {
        // Mock response for development
        if (error.response?.status === 404) {
          console.warn('Cancel RSVP endpoint not found, using mock response');
          return;
        }
        throw error;
      }
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
    },
    onError: (error: any) => {
      console.error('Failed to cancel RSVP:', error);
    }
  });
}

// Get user's all participations
export function useUserParticipations(enabled = true) {
  return useQuery<UserParticipationDto[]>({
    queryKey: participationKeys.userParticipations(),
    queryFn: async (): Promise<UserParticipationDto[]> => {
      try {
        const { data } = await apiClient.get('/api/user/participations');
        return data;
      } catch (error: any) {
        // Mock data for development
        if (error.response?.status === 404) {
          console.warn('User participations endpoint not found, using mock data');
          return [
            {
              id: 'participation-1',
              eventId: 'event-1',
              eventTitle: 'Introduction to Rope Bondage',
              eventDate: '2025-02-15T19:00:00Z',
              eventLocation: 'WitchCityRope Studio',
              participationType: 'RSVP' as any,
              status: 'Active' as any,
              createdAt: '2025-01-19T10:00:00Z'
            },
            {
              id: 'participation-2',
              eventId: 'event-2',
              eventTitle: 'Advanced Suspension Techniques',
              eventDate: '2025-03-01T18:00:00Z',
              eventLocation: 'WitchCityRope Studio',
              participationType: 'Ticket' as any,
              status: 'Active' as any,
              amount: 75,
              createdAt: '2025-01-18T15:30:00Z'
            }
          ];
        }
        throw error;
      }
    },
    enabled,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}