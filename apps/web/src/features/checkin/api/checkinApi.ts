// CheckIn API Service Layer
// Handles all HTTP requests to CheckIn System backend
// Mobile-first design with offline capability support

import { apiClient } from '../../../lib/api/client';
import type { ApiResponse, PaginatedResponse } from '../../../lib/api/types/api.types';
import type {
  CheckInAttendeesResponse,
  CheckInRequest,
  CheckInResponse,
  CheckInDashboard,
  SyncRequest,
  SyncResponse,
  AttendeeSearchParams,
  ManualEntryData,
  CapacityInfo
} from '../types/checkin.types';

/**
 * CheckIn API client following established patterns
 * Uses session token authentication for kiosk mode (NO user authentication required)
 * Optimized for mobile usage with offline sync support
 */
export const checkinApi = {
  /**
   * Get attendees for event check-in interface
   * Optimized for mobile display with pagination
   * Sends X-CheckIn-Token header for authentication
   */
  async getEventAttendees(params: AttendeeSearchParams, sessionToken: string): Promise<CheckInAttendeesResponse> {
    const { eventId, ...queryParams} = params;
    const { data } = await apiClient.get<CheckInAttendeesResponse>(
      `/api/checkin/events/${eventId}/attendees`,
      {
        params: queryParams,
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || !data.attendees) {
      throw new Error('Failed to load event attendees');
    }

    return data;
  },

  /**
   * Process attendee check-in
   * Handles capacity validation and audit logging
   * Sends X-CheckIn-Token header for authentication
   */
  async checkInAttendee(eventId: string, request: CheckInRequest, sessionToken: string): Promise<CheckInResponse> {
    const { data } = await apiClient.post<CheckInResponse>(
      `/api/checkin/events/${eventId}/checkin`,
      request,
      {
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || !data.success) {
      throw new Error('Failed to process check-in');
    }

    return data;
  },

  /**
   * Get real-time dashboard data for event
   * Cached for performance with real-time updates
   * Sends X-CheckIn-Token header for authentication
   */
  async getEventDashboard(eventId: string, sessionToken: string): Promise<CheckInDashboard> {
    const { data } = await apiClient.get<CheckInDashboard>(
      `/api/checkin/events/${eventId}/dashboard`,
      {
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || !data.eventId) {
      throw new Error('Failed to load event dashboard');
    }

    return data;
  },

  /**
   * Synchronize offline check-in data
   * Handles conflict resolution and data integrity
   * Sends X-CheckIn-Token header for authentication
   */
  async syncOfflineData(eventId: string, request: SyncRequest, sessionToken: string): Promise<SyncResponse> {
    const { data } = await apiClient.post<SyncResponse>(
      `/api/checkin/events/${eventId}/sync`,
      request,
      {
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || !data.success) {
      throw new Error('Failed to sync offline data');
    }

    return data;
  },

  /**
   * Create manual entry for walk-in attendee
   * Includes temporary registration creation
   * Sends X-CheckIn-Token header for authentication
   */
  async createManualEntry(
    eventId: string,
    staffMemberId: string,
    manualEntryData: ManualEntryData,
    sessionToken: string,
    notes?: string
  ): Promise<CheckInResponse> {
    const request: CheckInRequest = {
      attendeeId: 'temp-' + Date.now(), // Temporary ID for manual entries
      checkInTime: new Date().toISOString(),
      staffMemberId,
      notes,
      isManualEntry: true,
      manualEntryData
    };

    return this.checkInAttendee(eventId, request, sessionToken);
  },

  /**
   * Get current event capacity information
   * Used for real-time capacity monitoring
   * Sends X-CheckIn-Token header for authentication
   */
  async getEventCapacity(eventId: string, sessionToken: string): Promise<CapacityInfo> {
    const { data } = await apiClient.get<CapacityInfo>(
      `/api/checkin/events/${eventId}/capacity`,
      {
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || data.totalCapacity === undefined) {
      throw new Error('Failed to get event capacity');
    }

    return data;
  },

  /**
   * Export attendance data for organizers
   * Token-based access control enforced
   * Sends X-CheckIn-Token header for authentication
   */
  async exportAttendance(eventId: string, sessionToken: string, format: 'csv' | 'pdf' = 'csv'): Promise<Blob> {
    const response = await apiClient.get(
      `/api/checkin/events/${eventId}/export`,
      {
        params: { format },
        responseType: 'blob',
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    return response.data;
  },

  /**
   * Check connection status to server
   * Used for offline/online status detection
   */
  async ping(): Promise<boolean> {
    try {
      await apiClient.get('/api/checkin/ping', { timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  },

  /**
   * Get list of active events for check-in selection
   * Staff interface for selecting which event to check-in
   */
  async getActiveEvents(): Promise<Array<{ id: string; title: string; startDate: string; status: string }>> {
    const { data } = await apiClient.get<Array<{
      id: string;
      title: string;
      startDate: string;
      status: string
    }>>(
      '/api/checkin/events/active'
    );

    if (!data || !Array.isArray(data)) {
      throw new Error('Failed to get active events');
    }

    return data;
  },

  /**
   * Record cash payment at the door
   * Part of streamlined check-in workflow for social events
   * Sends X-CheckIn-Token header for authentication
   */
  async recordCashPayment(
    eventId: string,
    attendeeId: string,
    amount: number,
    sessionToken: string,
    notes?: string
  ): Promise<{ success: boolean; paymentId: string; message: string }> {
    const { data } = await apiClient.post<{
      success: boolean;
      paymentId: string;
      message: string
    }>(
      `/api/kiosk/events/${eventId}/payments/cash`,
      {
        attendeeId,
        amount,
        paymentMethod: 'Cash',
        notes
      },
      {
        headers: {
          'X-CheckIn-Token': sessionToken
        }
      }
    );

    if (!data || !data.success) {
      throw new Error(data?.message || 'Failed to record cash payment');
    }

    return data;
  },

  /**
   * Generate QR code payment URL for digital payment
   * Attendee scans QR code to complete payment
   * Real-time notification via SSE when payment completes
   */
  generatePaymentQR(
    eventId: string,
    attendeeId: string
  ): string {
    // Generate payment URL that attendee will scan
    const baseUrl = window.location.origin;
    return `${baseUrl}/events/${eventId}/purchase?attendeeId=${attendeeId}&returnUrl=checkin`;
  }
};