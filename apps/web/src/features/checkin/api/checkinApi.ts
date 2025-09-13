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
 * Uses cookie-based authentication for protected endpoints
 * Optimized for mobile usage with offline sync support
 */
export const checkinApi = {
  /**
   * Get attendees for event check-in interface
   * Optimized for mobile display with pagination
   */
  async getEventAttendees(params: AttendeeSearchParams): Promise<CheckInAttendeesResponse> {
    const { eventId, ...queryParams } = params;
    const { data } = await apiClient.get<ApiResponse<CheckInAttendeesResponse>>(
      `/api/checkin/events/${eventId}/attendees`,
      { params: queryParams }
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load event attendees');
    }
    
    return data.data;
  },

  /**
   * Process attendee check-in
   * Handles capacity validation and audit logging
   */
  async checkInAttendee(eventId: string, request: CheckInRequest): Promise<CheckInResponse> {
    const { data } = await apiClient.post<ApiResponse<CheckInResponse>>(
      `/api/checkin/events/${eventId}/checkin`,
      request
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to process check-in');
    }
    
    return data.data;
  },

  /**
   * Get real-time dashboard data for event
   * Cached for performance with real-time updates
   */
  async getEventDashboard(eventId: string): Promise<CheckInDashboard> {
    const { data } = await apiClient.get<ApiResponse<CheckInDashboard>>(
      `/api/checkin/events/${eventId}/dashboard`
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load event dashboard');
    }
    
    return data.data;
  },

  /**
   * Synchronize offline check-in data
   * Handles conflict resolution and data integrity
   */
  async syncOfflineData(eventId: string, request: SyncRequest): Promise<SyncResponse> {
    const { data } = await apiClient.post<ApiResponse<SyncResponse>>(
      `/api/checkin/events/${eventId}/sync`,
      request
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to sync offline data');
    }
    
    return data.data;
  },

  /**
   * Create manual entry for walk-in attendee
   * Includes temporary registration creation
   */
  async createManualEntry(
    eventId: string, 
    staffMemberId: string,
    manualEntryData: ManualEntryData,
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

    return this.checkInAttendee(eventId, request);
  },

  /**
   * Get current event capacity information
   * Used for real-time capacity monitoring
   */
  async getEventCapacity(eventId: string): Promise<CapacityInfo> {
    const { data } = await apiClient.get<ApiResponse<CapacityInfo>>(
      `/api/checkin/events/${eventId}/capacity`
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to get event capacity');
    }
    
    return data.data;
  },

  /**
   * Export attendance data for organizers
   * Role-based access control enforced
   */
  async exportAttendance(eventId: string, format: 'csv' | 'pdf' = 'csv'): Promise<Blob> {
    const response = await apiClient.get(
      `/api/checkin/events/${eventId}/export`,
      { 
        params: { format },
        responseType: 'blob'
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
    const { data } = await apiClient.get<ApiResponse<Array<{ 
      id: string; 
      title: string; 
      startDate: string; 
      status: string 
    }>>>(
      '/api/checkin/events/active'
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to get active events');
    }
    
    return data.data;
  }
};