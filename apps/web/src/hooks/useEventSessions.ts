import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api/client';
import type { ApiResponse } from '../lib/api/types/api.types';
import type { EventSession } from '../components/events/EventSessionsGrid';

// Extended types for the Event Session Matrix
export interface EventSessionDto {
  id: string;
  eventId: string;
  sessionIdentifier: string; // S1, S2, S3, etc.
  name: string;
  date: string; // ISO date string
  startTime: string; // HH:MM format
  endTime: string; // HH:MM format
  capacity: number;
  registeredCount: number;
  isRequired: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEventSessionDto {
  eventId: string;
  sessionIdentifier: string;
  name: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  isRequired?: boolean;
}

export interface UpdateEventSessionDto {
  id: string;
  name?: string;
  date?: string;
  startTime?: string;
  endTime?: string;
  capacity?: number;
  isRequired?: boolean;
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
  date: dto.date,
  startTime: dto.startTime,
  endTime: dto.endTime,
  capacity: dto.capacity,
  registeredCount: dto.registeredCount,
});

// Fetch event sessions for a specific event
export function useEventSessions(eventId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: eventSessionKeys.list(eventId),
    queryFn: async (): Promise<EventSession[]> => {
      const { data } = await apiClient.get<ApiResponse<EventSessionDto[]>>(`/api/events/${eventId}/sessions`);
      if (!data.data) return [];
      return data.data.map(transformEventSession);
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
      const { data } = await apiClient.get<ApiResponse<EventSessionDto>>(`/api/events/sessions/${sessionId}`);
      if (!data.data) throw new Error('Event session not found');
      return transformEventSession(data.data);
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
      const { data } = await apiClient.post<ApiResponse<EventSessionDto>>('/api/events/sessions', sessionData);
      if (!data.data) throw new Error('Failed to create event session');
      return transformEventSession(data.data);
    },
    onSuccess: (newSession, variables) => {
      // Invalidate the event sessions list
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(variables.eventId) });
      
      // Add to cache
      queryClient.setQueryData(eventSessionKeys.detail(newSession.id), newSession);
      
      // Invalidate related event data
      queryClient.invalidateQueries({ queryKey: ['events', 'detail', variables.eventId] });
      
      console.log('Event session created successfully:', newSession.name);
    },
    onError: (error) => {
      console.error('Create event session failed:', error);
    },
  });
}

// Update event session
export function useUpdateEventSession() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionData: UpdateEventSessionDto): Promise<EventSession> => {
      const { data } = await apiClient.put<ApiResponse<EventSessionDto>>(`/api/events/sessions/${sessionData.id}`, sessionData);
      if (!data.data) throw new Error('Failed to update event session');
      return transformEventSession(data.data);
    },
    onMutate: async (updatedSession) => {
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
    onError: (err, updatedSession, context) => {
      // Rollback on error
      if (context?.previousSession) {
        queryClient.setQueryData(eventSessionKeys.detail(updatedSession.id), context.previousSession);
      }
      console.error('Update event session failed, rolling back:', err);
    },
    onSettled: (_data, _error, updatedSession) => {
      // Refetch to ensure consistency
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.detail(updatedSession.id) });
    },
    onSuccess: (updatedSession) => {
      // Find the event ID from current queries to invalidate the list
      const queries = queryClient.getQueriesData({ queryKey: eventSessionKeys.lists() });
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
    onSuccess: (deletedId) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: eventSessionKeys.detail(deletedId) });
      
      // Invalidate all session lists (we don't know which event it belonged to)
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.lists() });
      
      // Invalidate event details (capacity calculations might change)
      queryClient.invalidateQueries({ queryKey: ['events'] });
      
      console.log('Event session deleted successfully');
    },
    onError: (error) => {
      console.error('Delete event session failed:', error);
    },
  });
}

// Bulk operations
export function useBulkCreateEventSessions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (sessionsData: CreateEventSessionDto[]): Promise<EventSession[]> => {
      const { data } = await apiClient.post<ApiResponse<EventSessionDto[]>>('/api/events/sessions/bulk', sessionsData);
      if (!data.data) throw new Error('Failed to create event sessions');
      return data.data.map(transformEventSession);
    },
    onSuccess: (newSessions, variables) => {
      if (newSessions.length > 0) {
        const eventId = variables[0]?.eventId;
        if (eventId) {
          // Invalidate the event sessions list
          queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(eventId) });
          
          // Add each session to cache
          newSessions.forEach(session => {
            queryClient.setQueryData(eventSessionKeys.detail(session.id), session);
          });
          
          // Invalidate related event data
          queryClient.invalidateQueries({ queryKey: ['events', 'detail', eventId] });
        }
      }
      
      console.log('Bulk event sessions created successfully:', newSessions.length);
    },
    onError: (error) => {
      console.error('Bulk create event sessions failed:', error);
    },
  });
}

// Reorder sessions (for managing session identifiers S1, S2, S3, etc.)
export function useReorderEventSessions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: { eventId: string; sessionIds: string[] }): Promise<EventSession[]> => {
      const { data } = await apiClient.put<ApiResponse<EventSessionDto[]>>(`/api/events/${payload.eventId}/sessions/reorder`, {
        sessionIds: payload.sessionIds,
      });
      if (!data.data) throw new Error('Failed to reorder event sessions');
      return data.data.map(transformEventSession);
    },
    onSuccess: (reorderedSessions, variables) => {
      // Invalidate the event sessions list
      queryClient.invalidateQueries({ queryKey: eventSessionKeys.list(variables.eventId) });
      
      // Update individual session caches
      reorderedSessions.forEach(session => {
        queryClient.setQueryData(eventSessionKeys.detail(session.id), session);
      });
      
      console.log('Event sessions reordered successfully');
    },
    onError: (error) => {
      console.error('Reorder event sessions failed:', error);
    },
  });
}