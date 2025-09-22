// Simplified Vetting API Service
// Handles API calls for the simplified vetting application form

import { apiClient } from '../../../lib/api/client';
import type { ApiResponse } from '../../../lib/api/types/api.types';
import type {
  SimplifiedCreateApplicationRequest,
  SimplifiedApplicationSubmissionResponse,
  SimplifiedApplicationStatus
} from '../types/simplified-vetting.types';

/**
 * Simplified vetting API service
 * Focused on the streamlined application process
 */
export const simplifiedVettingApi = {
  /**
   * Check if user already has an application
   * Returns null if no application exists
   */
  async checkExistingApplication(): Promise<SimplifiedApplicationStatus | null> {
    try {
      const { data } = await apiClient.get<ApiResponse<SimplifiedApplicationStatus>>(
        '/api/vetting/my-application'
      );

      return data.data || null;
    } catch (error: any) {
      // 404 means no application exists, which is expected
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Submit a simplified vetting application
   */
  async submitApplication(request: SimplifiedCreateApplicationRequest): Promise<SimplifiedApplicationSubmissionResponse> {
    const { data } = await apiClient.post<ApiResponse<SimplifiedApplicationSubmissionResponse>>(
      '/api/vetting/applications/simplified',
      request
    );

    if (!data.data) {
      throw new Error(data.error || 'Failed to submit application');
    }

    return data.data;
  },

  /**
   * Get application status for the current user
   */
  async getMyApplicationStatus(): Promise<SimplifiedApplicationStatus> {
    const { data } = await apiClient.get<ApiResponse<SimplifiedApplicationStatus>>(
      '/api/vetting/my-application'
    );

    if (!data.data) {
      throw new Error(data.error || 'Application not found');
    }

    return data.data;
  },
};

/**
 * Transform API errors to user-friendly messages
 */
export const getSimplifiedVettingErrorMessage = (error: any): string => {
  if (error.response?.status === 401) {
    return 'You must be logged in to submit an application.';
  }

  if (error.response?.status === 403) {
    return 'You do not have permission to submit an application.';
  }

  if (error.response?.status === 409) {
    return 'You already have a submitted application. Only one application is allowed per person.';
  }

  if (error.response?.status === 422) {
    return 'Please check your application for errors and try again.';
  }

  if (error.response?.status === 429) {
    return 'Too many requests. Please wait a moment and try again.';
  }

  if (error.response?.status >= 500) {
    return 'A server error occurred. Please try again later or contact support.';
  }

  return error.message || 'An unexpected error occurred. Please try again.';
};