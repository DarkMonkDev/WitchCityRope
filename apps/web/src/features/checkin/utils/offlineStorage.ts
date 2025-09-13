// Offline storage utility for CheckIn system
// Manages local storage for offline capability and sync operations

import type {
  OfflineAttendeeData,
  OfflineQueue,
  PendingCheckIn,
  SyncConflict,
  CheckInAttendee,
  CapacityInfo
} from '../types/checkin.types';

// Storage keys
const STORAGE_KEYS = {
  ATTENDEE_DATA: 'checkin_attendee_data',
  OFFLINE_QUEUE: 'checkin_offline_queue',
  DEVICE_ID: 'checkin_device_id',
  LAST_SYNC: 'checkin_last_sync',
  SYNC_CONFLICTS: 'checkin_sync_conflicts'
} as const;

// Storage expiry (24 hours for attendee data)
const ATTENDEE_DATA_EXPIRY = 24 * 60 * 60 * 1000;

/**
 * Offline storage manager for CheckIn system
 * Handles localStorage operations with error handling and data validation
 */
export const offlineStorage = {
  /**
   * Get or generate unique device ID for sync tracking
   */
  getDeviceId(): string {
    try {
      let deviceId = localStorage.getItem(STORAGE_KEYS.DEVICE_ID);
      if (!deviceId) {
        deviceId = `device-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        localStorage.setItem(STORAGE_KEYS.DEVICE_ID, deviceId);
      }
      return deviceId;
    } catch (error) {
      console.error('Failed to get device ID:', error);
      // Fallback to session-based ID
      return `session-${Date.now()}`;
    }
  },

  /**
   * Store attendee data with expiry timestamp
   */
  async setAttendeeData(eventId: string, attendees: CheckInAttendee[], capacity: CapacityInfo): Promise<void> {
    try {
      const data: OfflineAttendeeData = {
        eventId,
        attendees,
        lastSync: new Date().toISOString(),
        capacity
      };

      const storageData = {
        data,
        timestamp: Date.now(),
        expiry: Date.now() + ATTENDEE_DATA_EXPIRY
      };

      localStorage.setItem(
        `${STORAGE_KEYS.ATTENDEE_DATA}_${eventId}`,
        JSON.stringify(storageData)
      );
    } catch (error) {
      console.error('Failed to store attendee data:', error);
      // If storage is full, try to clear old data
      this.clearExpiredData();
      throw error;
    }
  },

  /**
   * Get stored attendee data if not expired
   */
  async getAttendeeData(eventId: string): Promise<OfflineAttendeeData | null> {
    try {
      const stored = localStorage.getItem(`${STORAGE_KEYS.ATTENDEE_DATA}_${eventId}`);
      if (!stored) return null;

      const { data, expiry } = JSON.parse(stored);
      
      // Check if data has expired
      if (Date.now() > expiry) {
        localStorage.removeItem(`${STORAGE_KEYS.ATTENDEE_DATA}_${eventId}`);
        return null;
      }

      return data;
    } catch (error) {
      console.error('Failed to get attendee data:', error);
      return null;
    }
  },

  /**
   * Get offline queue for pending actions
   */
  async getOfflineQueue(): Promise<OfflineQueue> {
    try {
      const stored = localStorage.getItem(STORAGE_KEYS.OFFLINE_QUEUE);
      if (!stored) {
        return {
          pendingActions: [],
          conflicts: []
        };
      }

      return JSON.parse(stored);
    } catch (error) {
      console.error('Failed to get offline queue:', error);
      return {
        pendingActions: [],
        conflicts: []
      };
    }
  },

  /**
   * Add action to offline queue
   */
  async addPendingAction(action: PendingCheckIn): Promise<void> {
    try {
      const queue = await this.getOfflineQueue();
      queue.pendingActions.push(action);
      localStorage.setItem(STORAGE_KEYS.OFFLINE_QUEUE, JSON.stringify(queue));
    } catch (error) {
      console.error('Failed to add pending action:', error);
      throw error;
    }
  },

  /**
   * Remove specific pending actions by local IDs
   */
  async removePendingActions(localIds: string[]): Promise<void> {
    try {
      const queue = await this.getOfflineQueue();
      queue.pendingActions = queue.pendingActions.filter(
        action => !localIds.includes(action.localId)
      );
      localStorage.setItem(STORAGE_KEYS.OFFLINE_QUEUE, JSON.stringify(queue));
    } catch (error) {
      console.error('Failed to remove pending actions:', error);
      throw error;
    }
  },

  /**
   * Add sync conflict for manual resolution
   */
  async addSyncConflict(conflict: SyncConflict): Promise<void> {
    try {
      const queue = await this.getOfflineQueue();
      queue.conflicts.push(conflict);
      localStorage.setItem(STORAGE_KEYS.OFFLINE_QUEUE, JSON.stringify(queue));
    } catch (error) {
      console.error('Failed to add sync conflict:', error);
      throw error;
    }
  },

  /**
   * Remove resolved conflicts
   */
  async removeConflicts(localIds: string[]): Promise<void> {
    try {
      const queue = await this.getOfflineQueue();
      queue.conflicts = queue.conflicts.filter(
        conflict => !localIds.includes(conflict.localId)
      );
      localStorage.setItem(STORAGE_KEYS.OFFLINE_QUEUE, JSON.stringify(queue));
    } catch (error) {
      console.error('Failed to remove conflicts:', error);
      throw error;
    }
  },

  /**
   * Clear entire offline queue
   */
  async clearOfflineQueue(): Promise<void> {
    try {
      localStorage.removeItem(STORAGE_KEYS.OFFLINE_QUEUE);
    } catch (error) {
      console.error('Failed to clear offline queue:', error);
      throw error;
    }
  },

  /**
   * Get last sync timestamp
   */
  async getLastSyncTimestamp(): Promise<string | null> {
    try {
      return localStorage.getItem(STORAGE_KEYS.LAST_SYNC);
    } catch (error) {
      console.error('Failed to get last sync timestamp:', error);
      return null;
    }
  },

  /**
   * Set last sync timestamp
   */
  async setLastSyncTimestamp(timestamp: string): Promise<void> {
    try {
      localStorage.setItem(STORAGE_KEYS.LAST_SYNC, timestamp);
    } catch (error) {
      console.error('Failed to set last sync timestamp:', error);
      throw error;
    }
  },

  /**
   * Clear expired data to free up storage space
   */
  clearExpiredData(): void {
    try {
      const keys = Object.keys(localStorage);
      const now = Date.now();

      keys.forEach(key => {
        if (key.startsWith(STORAGE_KEYS.ATTENDEE_DATA)) {
          try {
            const stored = localStorage.getItem(key);
            if (stored) {
              const { expiry } = JSON.parse(stored);
              if (now > expiry) {
                localStorage.removeItem(key);
              }
            }
          } catch {
            // If we can't parse it, remove it
            localStorage.removeItem(key);
          }
        }
      });
    } catch (error) {
      console.error('Failed to clear expired data:', error);
    }
  },

  /**
   * Get storage usage information
   */
  getStorageInfo(): {
    used: number;
    total: number;
    percentage: number;
    canStore: boolean;
  } {
    try {
      let used = 0;
      const keys = Object.keys(localStorage);
      
      keys.forEach(key => {
        if (key.startsWith('checkin_')) {
          used += (localStorage.getItem(key) || '').length;
        }
      });

      // Rough estimate of available storage (5MB typical limit)
      const estimated_total = 5 * 1024 * 1024;
      const percentage = (used / estimated_total) * 100;

      return {
        used,
        total: estimated_total,
        percentage,
        canStore: percentage < 80 // Conservative threshold
      };
    } catch (error) {
      console.error('Failed to get storage info:', error);
      return {
        used: 0,
        total: 0,
        percentage: 0,
        canStore: false
      };
    }
  },

  /**
   * Test if localStorage is available and working
   */
  isStorageAvailable(): boolean {
    try {
      const test = '__storage_test__';
      localStorage.setItem(test, test);
      localStorage.removeItem(test);
      return true;
    } catch {
      return false;
    }
  },

  /**
   * Emergency clear all CheckIn data
   */
  emergencyClear(): void {
    try {
      const keys = Object.keys(localStorage);
      keys.forEach(key => {
        if (key.startsWith('checkin_')) {
          localStorage.removeItem(key);
        }
      });
      console.log('Emergency clear completed');
    } catch (error) {
      console.error('Emergency clear failed:', error);
    }
  },

  /**
   * Update attendee status locally (for optimistic updates)
   */
  async updateAttendeeStatus(
    eventId: string, 
    attendeeId: string, 
    status: string, 
    checkInTime?: string
  ): Promise<void> {
    try {
      const data = await this.getAttendeeData(eventId);
      if (!data) return;

      const updatedAttendees = data.attendees.map(attendee => 
        attendee.attendeeId === attendeeId
          ? { 
              ...attendee, 
              registrationStatus: status as any,
              checkInTime: checkInTime || attendee.checkInTime
            }
          : attendee
      );

      await this.setAttendeeData(eventId, updatedAttendees, data.capacity);
    } catch (error) {
      console.error('Failed to update attendee status locally:', error);
    }
  }
};