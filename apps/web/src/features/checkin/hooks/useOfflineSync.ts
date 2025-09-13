// Offline synchronization hooks for CheckIn system
// Handles network connectivity and data sync for mobile reliability

import { useState, useEffect, useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { checkinApi } from '../api/checkinApi';
import { offlineStorage } from '../utils/offlineStorage';
import type {
  SyncRequest,
  SyncResponse,
  PendingCheckIn,
  OfflineQueue
} from '../types/checkin.types';

interface OfflineAction {
  type: 'checkin' | 'manual-entry';
  eventId: string;
  data: any;
  timestamp: string;
}

/**
 * Hook for managing offline sync operations
 * Handles connectivity detection, queuing, and synchronization
 */
export function useOfflineSync() {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [pendingCount, setPendingCount] = useState(0);
  const [lastSync, setLastSync] = useState<string | null>(null);
  const queryClient = useQueryClient();

  // Monitor network connectivity
  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      // Auto-sync when coming back online
      triggerSync();
    };

    const handleOffline = () => {
      setIsOnline(false);
      notifications.show({
        title: 'Connection Lost',
        message: 'Check-ins will be queued until connection is restored',
        color: 'yellow',
        icon: '📡',
        autoClose: 4000,
      });
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  // Load pending count on mount
  useEffect(() => {
    loadPendingCount();
  }, []);

  const loadPendingCount = useCallback(async () => {
    try {
      const queue = await offlineStorage.getOfflineQueue();
      setPendingCount(queue.pendingActions.length);
    } catch (error) {
      console.error('Failed to load pending count:', error);
    }
  }, []);

  /**
   * Queue an action for offline processing
   */
  const queueOfflineAction = useCallback(async (action: OfflineAction) => {
    try {
      const pendingAction: PendingCheckIn = {
        localId: `offline-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        attendeeId: action.data.attendeeId,
        checkInTime: action.data.checkInTime,
        staffMemberId: action.data.staffMemberId,
        notes: action.data.notes,
        isManualEntry: action.data.isManualEntry,
        manualEntryData: action.data.manualEntryData
      };

      await offlineStorage.addPendingAction(pendingAction);
      setPendingCount(prev => prev + 1);

      notifications.show({
        title: 'Action Queued',
        message: 'Will sync when connection is restored',
        color: 'blue',
        icon: '⏳',
        autoClose: 3000,
      });
    } catch (error) {
      console.error('Failed to queue offline action:', error);
      throw error;
    }
  }, []);

  /**
   * Trigger manual synchronization
   */
  const triggerSync = useCallback(async () => {
    try {
      const queue = await offlineStorage.getOfflineQueue();
      if (queue.pendingActions.length === 0) {
        return;
      }

      // Group actions by event ID for efficient sync
      const actionsByEvent = queue.pendingActions.reduce((acc, action) => {
        // Extract event ID from action data (you may need to adjust this logic)
        const eventId = action.attendeeId.split('-')[0]; // Assuming attendeeId contains eventId
        if (!acc[eventId]) {
          acc[eventId] = [];
        }
        acc[eventId].push(action);
        return acc;
      }, {} as Record<string, PendingCheckIn[]>);

      let totalProcessed = 0;
      const allConflicts = [];

      for (const [eventId, actions] of Object.entries(actionsByEvent)) {
        const syncRequest: SyncRequest = {
          deviceId: offlineStorage.getDeviceId(),
          pendingCheckIns: actions,
          lastSyncTimestamp: lastSync || new Date(0).toISOString()
        };

        try {
          const response = await checkinApi.syncOfflineData(eventId, syncRequest);
          totalProcessed += response.processedCount;
          allConflicts.push(...response.conflicts);

          // Remove successfully processed actions
          await offlineStorage.removePendingActions(
            actions.slice(0, response.processedCount).map(a => a.localId)
          );

          // Update sync timestamp
          setLastSync(response.newSyncTimestamp);
          await offlineStorage.setLastSyncTimestamp(response.newSyncTimestamp);

          // Invalidate related queries to show updated data
          queryClient.invalidateQueries({ 
            queryKey: ['checkin', 'attendees', eventId] 
          });
          queryClient.invalidateQueries({ 
            queryKey: ['checkin', 'dashboard', eventId] 
          });
        } catch (error) {
          console.error(`Failed to sync event ${eventId}:`, error);
        }
      }

      // Update pending count
      await loadPendingCount();

      // Show sync results
      if (totalProcessed > 0) {
        notifications.show({
          title: 'Sync Complete',
          message: `${totalProcessed} check-ins synchronized successfully`,
          color: 'green',
          icon: '✅',
          autoClose: 4000,
        });
      }

      if (allConflicts.length > 0) {
        notifications.show({
          title: 'Sync Conflicts',
          message: `${allConflicts.length} conflicts need manual resolution`,
          color: 'orange',
          icon: '⚠️',
          autoClose: 6000,
        });
      }
    } catch (error) {
      console.error('Sync failed:', error);
      notifications.show({
        title: 'Sync Failed',
        message: 'Unable to synchronize offline data',
        color: 'red',
        icon: '❌',
        autoClose: 5000,
      });
    }
  }, [lastSync, queryClient, loadPendingCount]);

  /**
   * Clear all pending actions (for testing or emergency)
   */
  const clearPendingActions = useCallback(async () => {
    try {
      await offlineStorage.clearOfflineQueue();
      setPendingCount(0);
      notifications.show({
        title: 'Queue Cleared',
        message: 'All pending actions have been cleared',
        color: 'blue',
        icon: '🗑️',
        autoClose: 3000,
      });
    } catch (error) {
      console.error('Failed to clear pending actions:', error);
    }
  }, []);

  /**
   * Get sync status information
   */
  const getSyncStatus = useCallback(async () => {
    const queue = await offlineStorage.getOfflineQueue();
    return {
      isOnline,
      pendingCount: queue.pendingActions.length,
      lastSync: lastSync || 'Never',
      conflictCount: queue.conflicts.length,
      hasConflicts: queue.conflicts.length > 0
    };
  }, [isOnline, lastSync]);

  return {
    isOnline,
    pendingCount,
    lastSync,
    queueOfflineAction,
    triggerSync,
    clearPendingActions,
    getSyncStatus,
    loadPendingCount
  };
}

/**
 * Hook for managing automatic sync attempts
 * Handles retry logic and backoff strategies
 */
export function useAutoSync(eventId: string) {
  const { isOnline, pendingCount, triggerSync } = useOfflineSync();
  const [isAutoSyncing, setIsAutoSyncing] = useState(false);

  // Auto-sync when coming online and has pending actions
  useEffect(() => {
    if (isOnline && pendingCount > 0 && !isAutoSyncing) {
      setIsAutoSyncing(true);
      triggerSync().finally(() => {
        setIsAutoSyncing(false);
      });
    }
  }, [isOnline, pendingCount, triggerSync, isAutoSyncing]);

  // Periodic sync attempt (every 5 minutes when online)
  useEffect(() => {
    if (!isOnline || pendingCount === 0) return;

    const interval = setInterval(async () => {
      if (!isAutoSyncing) {
        setIsAutoSyncing(true);
        await triggerSync();
        setIsAutoSyncing(false);
      }
    }, 5 * 60 * 1000); // 5 minutes

    return () => clearInterval(interval);
  }, [isOnline, pendingCount, triggerSync, isAutoSyncing]);

  return {
    isAutoSyncing
  };
}