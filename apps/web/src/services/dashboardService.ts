import { apiClient } from '../lib/api/client';
import type {
  UserEventDto,
  VettingStatusDto,
  UserProfileDto,
  UpdateProfileDto,
  ChangePasswordDto,
} from '../types/dashboard.types';

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

    const response = await apiClient.get<{ success: boolean; data: UserEventDto[] }>(url);
    return response.data.data || [];
  },

  /**
   * Get user's vetting status for alert display
   * @param userId - User ID
   */
  getVettingStatus: async (userId: string): Promise<VettingStatusDto | null> => {
    const url = `/api/users/${userId}/vetting-status`;

    try {
      const response = await apiClient.get<{ success: boolean; data: VettingStatusDto }>(url);
      return response.data.data || null;
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

    const response = await apiClient.get<{ success: boolean; data: UserProfileDto }>(url);
    return response.data.data;
  },

  /**
   * Update user profile
   * @param userId - User ID
   * @param data - Profile update data
   */
  updateProfile: async (userId: string, data: UpdateProfileDto): Promise<UserProfileDto> => {
    const url = `/api/users/${userId}/profile`;

    // DEBUG: Log the exact payload being sent to API
    console.log('🔍 UPDATE PROFILE REQUEST:', {
      url,
      userId,
      payload: data,
      payloadKeys: Object.keys(data),
      payloadValues: JSON.stringify(data, null, 2)
    });

    const response = await apiClient.put<{ success: boolean; data: UserProfileDto }>(url, data);

    console.log('✅ UPDATE PROFILE RESPONSE:', {
      status: response.status,
      data: response.data
    });

    return response.data.data;
  },

  /**
   * Change user password
   * @param userId - User ID
   * @param data - Password change data
   */
  changePassword: async (userId: string, data: ChangePasswordDto): Promise<boolean> => {
    const url = `/api/users/${userId}/change-password`;

    const response = await apiClient.post<{ success: boolean; data: boolean }>(url, data);
    return response.data.data || response.data.success;
  },
};
