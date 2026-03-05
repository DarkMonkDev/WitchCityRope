import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api/client';
import type { EventSession } from '../components/events/EventSessionsGrid';
import type { SessionDto, CreateEventSessionDto, UpdateEventSessionDto } from '../lib/api/types/event-session-matrix.types';

/**
 * DTO ALIGNMENT STRATEGY COMPLIANT
 * Types imported from event-session-matrix.types.ts which uses auto-generated SessionDto
 */

// Type alias for backward compatibility (used throughout this file as EventSessionDto)
type EventSessionDto = SessionDto;

// Context type for optimistic update rollback
interface OptimisticSessionContext {
  previousSession: EventSession | undefined;
}

// Query keys
export const eventSessionKeys = {
  all: ['event-sessions'] as const,
  lists: () => [...eventSessionKeys.all, 'list'] as const,
  list: (eventId: string) => [...eventSessionKeys.lists(), eventId] as const,
  details: () => [...eventSessionKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventSessionKeys.details(), id] as const,
};

// Transform DTO to component interface
const transformEventSession = (dto: EventSessionDto): EventSession => ({
  id: dto.id,
  sessionIdentifier: dto.sessionIdentifier,
  name: dto.name,
  startDate: dto.startDate,
  endDate: dto.endDate, // End date for multi-day sessions (derived from EndTime on backend)
  startTime: dto.startTime,
  endTime: dto.endTime,
  capacity: dto.capacity,
  registrationCount: dto.registrationCount,
});

// Fetch event sessions for a specific event
export function useEventSessions(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventSessionKeys.list(eventId),
    queryFn: async (): Promise<EventSession[]> => {
      const { data } = await apiClient.get<EventSessionDto[]>(`/api/events/${eventId}/sessions`);
      if (!data) return [];
      return data.map(transformEventSession);
    },
    enabled: !!eventId && enabled,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

// Fetch single event session
export function useEventSession(sessionId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventSessionKeys.detail(sessionId),
    queryFn: async (): Promise<EventSession> => {
      const { data } = await apiClient.get<EventSessionDto>(`/api/events/sessions/${sessionId}`);
      if (!data) throw new Error('Event session not found');
      return transformEventSession(data);
    },
    enabled: !!sessionId && enabled,
    staleTime: 2 * 60 * 1000,
  });
}

// Create event session
export function useCreateEventSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionData: CreateEventSessionDto): Promise<EventSession> => {
      const { data } = await apiClient.post<EventSessionDto>('/api/events/sessions', sessionData);
      if (!data) throw new Error('Failed to create event session');
      return transformEventSession(data);
    },
    onSuccess: (newSession: EventSession, variables: CreateEventSessionDto) => {
      // Invalidate the event sessions list
      if (variables.eventId) {
        queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(variables.eventId) });
      }

      // Add to cache
      if (newSession.id) {
        queryClient.setQueryData(eventSessionKeys.detail(newSession.id), newSession);
      }

      // Invalidate related event data
      if (variables.eventId) {
        queryClient.invalidateQueries({ queryKey: ['events', 'detail', variables.eventId] });
      }

      console.log('Event session created successfully:', newSession.name);
    },
    onError: (error: Error) => {
      console.error('Create event session failed:', error);
    },
  });
}

// Update event session
export function useUpdateEventSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionData: UpdateEventSessionDto): Promise<EventSession> => {
      const { data } = await apiClient.put<EventSessionDto>(`/api/events/sessions/${sessionData.id}`, sessionData);
      if (!data) throw new Error('Failed to update event session');
      return transformEventSession(data);
    },
    onMutate: async (updatedSession: UpdateEventSessionDto) => {
      // Cancel outgoing queries
      await queryClient.cancelQueries({ queryKey: eventSessionKeys.detail(updatedSession.id) });

      // Snapshot previous value
      const previousSession = queryClient.getQueryData(eventSessionKeys.detail(updatedSession.id)) as EventSession | undefined;

      // Optimistically update
      queryClient.setQueryData(eventSessionKeys.detail(updatedSession.id), (old: EventSession | undefined) => {
        if (!old) return old;
        return { ...old, ...updatedSession };
      });

      return { previousSession };
    },
    onError: (err: Error, updatedSession: UpdateEventSessionDto, context: OptimisticSessionContext | undefined) => {
      // Rollback on error
      if (context?.previousSession) {
        queryClient.setQueryData(eventSessionKeys.detail(updatedSession.id), context.previousSession);
      }
      console.error('Update event session failed, rolling back:', err);
    },
    onSettled: (_data: EventSession | undefined, _error: Error | null, updatedSession: UpdateEventSessionDto) => {
      // Refetch to ensure consistency
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.detail(updatedSession.id) });
    },
    onSuccess: (updatedSession: EventSession) => {
      // Find the event ID from current queries to invalidate the list
      const queries = (queryClient as any).getQueriesData({ queryKey: eventSessionKeys.lists() });
      for (const [queryKey] of queries) {
        if (queryKey.length >= 3) {
          const eventId = queryKey[2] as string;
          queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(eventId) });
        }
      }

      console.log('Event session updated successfully:', updatedSession.name);
    },
  });
}

// Delete event session
export function useDeleteEventSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionId: string): Promise<string> => {
      await apiClient.delete(`/api/events/sessions/${sessionId}`);
      return sessionId;
    },
    onSuccess: (deletedId: string) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: eventSessionKeys.detail(deletedId) });

      // Invalidate all session lists (we don't know which event it belonged to)
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.lists() });

      // Invalidate event details (capacity calculations might change)
      queryClient.invalidateQueries({ queryKey: ['events'] });

      console.log('Event session deleted successfully');
    },
    onError: (error: Error) => {
      console.error('Delete event session failed:', error);
    },
  });
}

// Bulk operations
export function useBulkCreateEventSessions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionsData: CreateEventSessionDto[]): Promise<EventSession[]> => {
      const { data } = await apiClient.post<EventSessionDto[]>('/api/events/sessions/bulk', sessionsData);
      if (!data) throw new Error('Failed to create event sessions');
      return data.map(transformEventSession);
    },
    onSuccess: (newSessions: EventSession[], variables: CreateEventSessionDto[]) => {
      if (newSessions.length > 0) {
        const eventId = variables[0]?.eventId;
        if (eventId) {
          // Invalidate the event sessions list
          queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(eventId) });

          // Add each session to cache
          newSessions.forEach((session: EventSession) => {
            if (session.id) {
              queryClient.setQueryData(eventSessionKeys.detail(session.id), session);
            }
          });

          // Invalidate related event data
          queryClient.invalidateQueries({ queryKey: ['events', 'detail', eventId] });
        }
      }

      console.log('Bulk event sessions created successfully:', newSessions.length);
    },
    onError: (error: Error) => {
      console.error('Bulk create event sessions failed:', error);
    },
  });
}

// Reorder sessions (for managing session identifiers S1, S2, S3, etc.)
export function useReorderEventSessions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: { eventId: string; sessionIds: string[] }): Promise<EventSession[]> => {
      const { data } = await apiClient.put<EventSessionDto[]>(`/api/events/${payload.eventId}/sessions/reorder`, {
        sessionIds: payload.sessionIds,
      });
      if (!data) throw new Error('Failed to reorder event sessions');
      return data.map(transformEventSession);
    },
    onSuccess: (reorderedSessions: EventSession[], variables: { eventId: string; sessionIds: string[] }) => {
      // Invalidate the event sessions list
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(variables.eventId) });

      // Update individual session caches
      reorderedSessions.forEach((session: EventSession) => {
        if (session.id) {
          queryClient.setQueryData(eventSessionKeys.detail(session.id), session);
        }
      });

      console.log('Event sessions reordered successfully');
    },
    onError: (error: Error) => {
      console.error('Reorder event sessions failed:', error);
    },
  });
}
