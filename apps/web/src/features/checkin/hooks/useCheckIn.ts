// CheckIn React Query hooks
// Manages server state for check-in operations with offline support

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { checkinApi } from '../api/checkinApi';
import type { AttendeeSearchParams, CheckInRequest, CheckInResponse, ManualEntryData } from '../types/checkin.types';
import { useOfflineSync } from './useOfflineSync';

// Query keys for caching and invalidation
export const checkinKeys = {
  all: ['checkin'] as const,
  eventAttendees: (eventId: string, params?: Partial<AttendeeSearchParams>) => 
    ['checkin', 'attendees', eventId, params] as const,
  eventDashboard: (eventId: string) => 
    ['checkin', 'dashboard', eventId] as const,
  eventCapacity: (eventId: string) => 
    ['checkin', 'capacity', eventId] as const,
  activeEvents: () => 
    ['checkin', 'events', 'active'] as const,
};

/**
 * Hook for getting event attendees with search and filtering
 * Optimized for mobile interface with real-time updates
 * Uses sessionToken for kiosk mode authentication
 */
export function useEventAttendees(params: AttendeeSearchParams, sessionToken: string, enabled: boolean = true) {
  return useQuery({
    queryKey: checkinKeys.eventAttendees(params.eventId, params),
    queryFn: () => checkinApi.getEventAttendees(params, sessionToken),
    enabled: enabled && !!sessionToken,
    staleTime: 2 * 60 * 1000, // 2 minutes - balance freshness vs performance
    refetchInterval: 5 * 1000, // Auto-refresh every 5 seconds
    refetchOnWindowFocus: true, // Refresh when returning to app
    refetchOnReconnect: true, // Refresh when coming back online
  });
}

/**
 * Hook for processing attendee check-ins
 * Includes offline queuing and optimistic updates
 * Uses sessionToken for kiosk mode authentication
 */
export function useCheckInAttendee(eventId: string, sessionToken: string) {
  const queryClient = useQueryClient();
  const { queueOfflineAction, isOnline } = useOfflineSync();

  return useMutation({
    mutationFn: async (request: CheckInRequest): Promise<CheckInResponse> => {
      // If offline, queue the action
      if (!isOnline) {
        await queueOfflineAction({
          type: 'checkin',
          eventId,
          data: request,
          timestamp: new Date().toISOString()
        });

        // Return optimistic response
        return {
          success: true,
          attendeeId: request.attendeeId,
          checkInTime: request.checkInTime,
          message: 'Check-in queued (offline)',
          currentCapacity: {
            totalCapacity: 0,
            checkedInCount: 0,
            waitlistCount: 0,
            availableSpots: 0,
            isAtCapacity: false,
            canOverride: false
          }
        };
      }

      return checkinApi.checkInAttendee(eventId, request, sessionToken);
    },
    onMutate: async (newCheckIn: CheckInRequest) => {
      // Cancel outgoing refetches to prevent optimistic update conflicts
      await queryClient.cancelQueries({ 
        queryKey: checkinKeys.eventAttendees(eventId) 
      });

      // Optimistically update attendee status
      const previousData = queryClient.getQueryData(
        checkinKeys.eventAttendees(eventId)
      );

      queryClient.setQueryData(
        checkinKeys.eventAttendees(eventId),
        (old: any) => {
          if (!old?.attendees) return old;
          
          return {
            ...old,
            attendees: old.attendees.map((attendee: any) => 
              attendee.attendeeId === newCheckIn.attendeeId
                ? { 
                    ...attendee, 
                    registrationStatus: 'checked-in',
                    checkInTime: newCheckIn.checkInTime
                  }
                : attendee
            ),
            checkedInCount: old.checkedInCount + 1,
            availableSpots: Math.max(0, old.availableSpots - 1)
          };
        }
      );

      return { previousData };
    },
    onSuccess: (data: CheckInResponse, _variables: CheckInRequest) => {
      // Invalidate related queries for fresh data
      queryClient.invalidateQueries({
        queryKey: checkinKeys.eventAttendees(eventId)
      });
      queryClient.invalidateQueries({
        queryKey: checkinKeys.eventDashboard(eventId)
      });
      queryClient.invalidateQueries({
        queryKey: checkinKeys.eventCapacity(eventId)
      });

      // Debug: Log notification trigger
      console.log('🔔 Check-in success (notification disabled for kiosk mode):', data);

      // KIOSK MODE: Notification disabled for streamlined UX
      // const notificationId = `checkin-success-${Date.now()}`;
      // notifications.show({
      //   id: notificationId,
      //   title: 'Check-in Successful',
      //   message: data.message || 'Attendee successfully checked in',
      //   color: 'green',
      //   autoClose: 8000,
      //   withCloseButton: true,
      // });
    },
    onError: (error: Error, _variables: CheckInRequest, context: { previousData: unknown } | undefined) => {
      // Rollback optimistic updates on error
      if (context?.previousData) {
        queryClient.setQueryData(
          checkinKeys.eventAttendees(eventId),
          context.previousData
        );
      }

      // KIOSK MODE: Notification disabled for streamlined UX
      console.error('Check-in failed:', error);
      // notifications.show({
      //   title: 'Check-in Failed',
      //   message: error instanceof Error ? error.message : 'Please try again',
      //   color: 'red',
      //   icon: '❌',
      //   autoClose: 5000,
      // });
    }
  });
}

/**
 * Hook for creating manual entries (walk-in attendees)
 * Includes validation and capacity checks
 * Uses sessionToken for kiosk mode authentication
 */
export function useCreateManualEntry(eventId: string, sessionToken: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      staffMemberId,
      manualEntryData,
      notes
    }: {
      staffMemberId: string;
      manualEntryData: ManualEntryData;
      notes?: string;
    }) => checkinApi.createManualEntry(eventId, staffMemberId, manualEntryData, sessionToken, notes),
    onSuccess: (data: CheckInResponse) => {
      // Invalidate attendee lists to show new entry
      queryClient.invalidateQueries({
        queryKey: checkinKeys.eventAttendees(eventId)
      });
      queryClient.invalidateQueries({
        queryKey: checkinKeys.eventDashboard(eventId)
      });

      // KIOSK MODE: Notification disabled for streamlined UX
      console.log('Manual entry created:', data);
      // notifications.show({
      //   title: 'Manual Entry Created',
      //   message: `${data.message} - Walk-in successfully checked in`,
      //   color: 'green',
      //   icon: '👤',
      //   autoClose: 4000,
      // });
    },
    onError: (error: Error) => {
      // KIOSK MODE: Notification disabled for streamlined UX
      console.error('Manual entry failed:', error);
      // notifications.show({
      //   title: 'Manual Entry Failed',
      //   message: error instanceof Error ? error.message : 'Failed to create manual entry',
      //   color: 'red',
      //   icon: '❌',
      //   autoClose: 5000,
      // });
    }
  });
}

/**
 * Hook for getting real-time event dashboard data
 * Includes capacity, recent check-ins, and sync status
 * Uses sessionToken for kiosk mode authentication
 */
export function useEventDashboard(eventId: string, sessionToken: string, enabled: boolean = true) {
  return useQuery({
    queryKey: checkinKeys.eventDashboard(eventId),
    queryFn: () => checkinApi.getEventDashboard(eventId, sessionToken),
    enabled: enabled && !!sessionToken,
    staleTime: 1 * 60 * 1000, // 1 minute for dashboard freshness
    refetchInterval: 30 * 1000, // Auto-refresh every 30 seconds
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
  });
}

/**
 * Hook for getting current event capacity
 * Used for real-time capacity monitoring
 * Uses sessionToken for kiosk mode authentication
 */
export function useEventCapacity(eventId: string, sessionToken: string, enabled: boolean = true) {
  return useQuery({
    queryKey: checkinKeys.eventCapacity(eventId),
    queryFn: () => checkinApi.getEventCapacity(eventId, sessionToken),
    enabled: enabled && !!sessionToken,
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: 60 * 1000, // Refresh every minute
  });
}

/**
 * Hook for getting active events list
 * Used for event selection in staff interface
 */
export function useActiveEvents(enabled: boolean = true) {
  return useQuery({
    queryKey: checkinKeys.activeEvents(),
    queryFn: () => checkinApi.getActiveEvents(),
    enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
  });
}

/**
 * Hook for exporting attendance data
 * Uses sessionToken for kiosk mode authentication
 */
export function useExportAttendance(eventId: string, sessionToken: string) {
  return useMutation({
    mutationFn: (format: 'csv' | 'pdf' = 'csv') =>
      checkinApi.exportAttendance(eventId, sessionToken, format),
    onSuccess: (blob: Blob, format: 'csv' | 'pdf') => {
      // Trigger download
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `attendance-${eventId}.${format}`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      // KIOSK MODE: Notification disabled for streamlined UX
      console.log('Export complete');
      // notifications.show({
      //   title: 'Export Complete',
      //   message: `Attendance data exported successfully`,
      //   color: 'green',
      //   icon: '📤',
      //   autoClose: 3000,
      // });
    },
    onError: (error: Error) => {
      // KIOSK MODE: Notification disabled for streamlined UX
      console.error('Export failed:', error);
      // notifications.show({
      //   title: 'Export Failed',
      //   message: error instanceof Error ? error.message : 'Failed to export data',
      //   color: 'red',
      //   icon: '❌',
      //   autoClose: 5000,
      // });
    }
  });
}