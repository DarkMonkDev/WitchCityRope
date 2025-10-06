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
   * Returns null if no application exists or user is not authenticated
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
      // 401 means user is not authenticated - return null gracefully
      // This allows the UI to handle the auth state properly
      if (error.response?.status === 401) {
        return null;
      }
      throw error;
    }
  },

  /**
   * Submit a simplified vetting application
   */
  async submitApplication(request: SimplifiedCreateApplicationRequest): Promise<SimplifiedApplicationSubmissionResponse> {
    try {
      console.log('simplifiedVettingApi.submitApplication: Submitting application:', {
        request,
        requestStringified: JSON.stringify(request, null, 2),
        url: '/api/vetting/applications/simplified'
      });

      const { data} = await apiClient.post<ApiResponse<SimplifiedApplicationSubmissionResponse>>(
        '/api/vetting/applications/simplified',
        request
      );

      console.log('simplifiedVettingApi.submitApplication: API response:', {
        hasData: !!data.data,
        data: data.data
      });

      if (!data.data) {
        throw new Error(data.error || 'Failed to submit application');
      }

      return data.data;
    } catch (error: any) {
      console.error('simplifiedVettingApi.submitApplication: FULL ERROR DETAILS:', {
        status: error.response?.status,
        message: error.message,
        responseData: error.response?.data,
        fullError: error.response?.data?.error || error.response?.data?.Error,
        details: error.response?.data?.details || error.response?.data?.Details,
        validationErrors: error.response?.data?.errors || error.response?.data?.Errors,
        title: error.response?.data?.title,
        type: error.response?.data?.type,
        allResponseData: JSON.stringify(error.response?.data, null, 2)
      });

      // Add context to authentication errors
      if (error.response?.status === 401) {
        throw new Error('Authentication expired. Please login again and retry.');
      }
      // Re-throw other errors to be handled by the error message function
      throw error;
    }
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
  // Handle different types of error objects
  const status = error.response?.status || error.status;
  const message = error.message || error.response?.data?.message || error.response?.data?.error;

  if (status === 401) {
    return 'You must be logged in to submit an application. Please login or create an account first.';
  }

  if (status === 403) {
    return 'You do not have permission to submit an application. Please contact support if you believe this is an error.';
  }

  if (status === 409) {
    return 'You already have a submitted application. Only one application is allowed per person.';
  }

  if (status === 422) {
    return 'Please check your application for errors and try again. All required fields must be filled out correctly.';
  }

  if (status === 429) {
    return 'Too many requests. Please wait a moment and try again.';
  }

  if (status >= 500) {
    return 'A server error occurred. Please try again later or contact support if the problem persists.';
  }

  // Network errors
  if (error.code === 'NETWORK_ERROR' || message?.includes('Network Error')) {
    return 'Network connection error. Please check your internet connection and try again.';
  }

  // Timeout errors
  if (error.code === 'ECONNABORTED' || message?.includes('timeout')) {
    return 'Request timed out. Please try again.';
  }

  // Return the original message if available, otherwise a generic error
  return message || 'An unexpected error occurred. Please try again.';
};