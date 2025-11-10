import type { components } from '@witchcityrope/shared-types';
import { apiClient } from '../lib/api/client';

// Re-export generated types
export type PlaceMembershipOnHoldRequest = components['schemas']['PlaceMembershipOnHoldRequest'];
export type RequestReinstatementRequest = components['schemas']['RequestReinstatementRequest'];
export type VettingHoldStatusResponse = components['schemas']['VettingHoldStatusResponse'];
export type MembershipHoldResponse = components['schemas']['MembershipHoldResponse'];
export type ApiResponseOfMembershipHoldResponse = components['schemas']['ApiResponseOfMembershipHoldResponse'];
export type ApiResponseOfVettingHoldStatusResponse = components['schemas']['ApiResponseOfVettingHoldStatusResponse'];

/**
 * Vetting Hold API Service
 * Handles membership hold and reinstatement operations
 */
export const vettingHoldService = {
  /**
   * Place user's membership on hold
   * Changes status from Approved → OnHold
   * Cancels all future social event RSVPs
   */
  placeMembershipOnHold: async (
    userId: string,
    reason: string
  ): Promise<MembershipHoldResponse> => {
    const response = await apiClient.put<ApiResponseOfMembershipHoldResponse>(
      `/api/users/${userId}/vetting/hold`,
      { reason }
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to place membership on hold');
    }

    return response.data.data;
  },

  /**
   * Request membership reinstatement
   * Changes status from OnHold → FinalReview (for admin approval)
   */
  requestReinstatement: async (
    userId: string,
    reason: string
  ): Promise<MembershipHoldResponse> => {
    const response = await apiClient.put<ApiResponseOfMembershipHoldResponse>(
      `/api/users/${userId}/vetting/reinstate`,
      { reason }
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to request reinstatement');
    }

    return response.data.data;
  },

  /**
   * Get current hold/reinstatement status
   * Returns available actions and current status
   */
  getHoldStatus: async (userId: string): Promise<VettingHoldStatusResponse> => {
    const response = await apiClient.get<ApiResponseOfVettingHoldStatusResponse>(
      `/api/users/${userId}/vetting/hold-status`
    );

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to get hold status');
    }

    return response.data.data;
  },
};
