import { apiClient } from '../lib/api/client';
import type {
  UserEventDto,
  VettingStatusDto,
  UserProfileDto,
  UpdateProfileDto,
  ChangePasswordDto,
} from '../types/dashboard.types';
import { debugLog } from '../utils/debug';

/**
 * Dashboard API service
 * Handles all API calls for user dashboard functionality
 *
 * CRITICAL: Uses httpOnly cookie authentication (BFF pattern)
 * No manual token management needed
 */
export const dashboardService = {
  /**
   * Get user's registered events
   * @param userId - User ID
   * @param includePast - Include past events (default: false)
   */
  getUserEvents: async (userId: string, includePast = false): Promise<UserEventDto[]> => {
    const params = new URLSearchParams();
    if (includePast) {
      params.append('includePast', 'true');
    }
    const queryString = params.toString();
    const url = `/api/users/${userId}/events${queryString ? `?${queryString}` : ''}`;

    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.get<UserEventDto[]>(url);
    return response.data || [];
  },

  /**
   * Get user's vetting status for alert display
   * @param userId - User ID
   */
  getVettingStatus: async (userId: string): Promise<VettingStatusDto | null> => {
    const url = `/api/users/${userId}/vetting-status`;

    try {
      // Pattern B: Direct DTO response (no ApiResponse wrapper)
      const response = await apiClient.get<VettingStatusDto>(url);
      return response.data || null;
    } catch (error: any) {
      // If user is already vetted, API may return 404 or null
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Get user profile data
   * @param userId - User ID
   */
  getProfile: async (userId: string): Promise<UserProfileDto> => {
    const url = `/api/users/${userId}/profile`;

    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.get<UserProfileDto>(url);
    return response.data;
  },

  /**
   * Update user profile
   * @param userId - User ID
   * @param data - Profile update data
   */
  updateProfile: async (userId: string, data: UpdateProfileDto): Promise<UserProfileDto> => {
    const url = `/api/users/${userId}/profile`;

    // DEBUG: Log the exact payload being sent to API
    debugLog('🔍 UPDATE PROFILE REQUEST:', {
      url,
      userId,
      payload: data,
      payloadKeys: Object.keys(data),
      payloadValues: JSON.stringify(data, null, 2)
    });

    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.put<UserProfileDto>(url, data);

    debugLog('✅ UPDATE PROFILE RESPONSE:', {
      status: response.status,
      data: response.data
    });

    return response.data;
  },

  /**
   * Change user password
   * @param userId - User ID
   * @param data - Password change data
   */
  changePassword: async (userId: string, data: ChangePasswordDto): Promise<boolean> => {
    const url = `/api/users/${userId}/change-password`;

    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    // For boolean responses, API returns true/false directly
    const response = await apiClient.post<boolean>(url, data);
    return response.data;
  },
};
