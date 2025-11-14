import type { components } from '@witchcityrope/shared-types';
import { apiClient } from '../lib/api/client';

// Re-export generated types
export type PlaceMembershipOnHoldRequest = components['schemas']['PlaceMembershipOnHoldRequest'];
export type RequestReinstatementRequest = components['schemas']['RequestReinstatementRequest'];
export type VettingHoldStatusResponse = components['schemas']['VettingHoldStatusResponse'];
export type MembershipHoldResponse = components['schemas']['MembershipHoldResponse'];
// API Response wrappers removed - API returns DTOs directly (Pattern B)

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
    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.put<MembershipHoldResponse>(
      `/api/users/${userId}/vetting/hold`,
      { reason }
    );

    return response.data;
  },

  /**
   * Request membership reinstatement
   * Changes status from OnHold → FinalReview (for admin approval)
   */
  requestReinstatement: async (
    userId: string,
    reason: string
  ): Promise<MembershipHoldResponse> => {
    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.put<MembershipHoldResponse>(
      `/api/users/${userId}/vetting/reinstate`,
      { reason }
    );

    return response.data;
  },

  /**
   * Get current hold/reinstatement status
   * Returns available actions and current status
   */
  getHoldStatus: async (userId: string): Promise<VettingHoldStatusResponse> => {
    // Pattern B: Direct DTO response (no ApiResponse wrapper)
    const response = await apiClient.get<VettingHoldStatusResponse>(
      `/api/users/${userId}/vetting/hold-status`
    );

    return response.data;
  },
};
