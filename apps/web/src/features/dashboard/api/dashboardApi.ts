// Dashboard API Service
// Handles all communication with the dashboard endpoints

import { apiClient } from '../../../lib/api/client';
import type {
  UserDashboardResponse,
  UserEventsResponse,
  UserStatisticsResponse
} from '../types/dashboard.types';

/**
 * Dashboard API service class
 * Provides methods for all dashboard-related API calls
 * Uses httpOnly cookie authentication via apiClient
 */
export class DashboardApiService {
  /**
   * Get user dashboard data
   * @returns Promise resolving to user dashboard response
   */
  async getUserDashboard(): Promise<UserDashboardResponse> {
    try {
      const response = await apiClient.get<UserDashboardResponse>('/api/dashboard');
      return response.data;
    } catch (error) {
      throw this.handleDashboardError(error, 'Failed to load dashboard data');
    }
  }

  /**
   * Get user's upcoming events
   * @param count Number of events to retrieve (default: 3)
   * @returns Promise resolving to user events response
   */
  async getUserEvents(count: number = 3): Promise<UserEventsResponse> {
    try {
      const response = await apiClient.get<UserEventsResponse>('/api/dashboard/events', {
        params: { count }
      });
      return response.data;
    } catch (error) {
      throw this.handleDashboardError(error, 'Failed to load upcoming events');
    }
  }

  /**
   * Get user's membership statistics
   * @returns Promise resolving to user statistics response
   */
  async getUserStatistics(): Promise<UserStatisticsResponse> {
    try {
      const response = await apiClient.get<UserStatisticsResponse>('/api/dashboard/statistics');
      return response.data;
    } catch (error) {
      throw this.handleDashboardError(error, 'Failed to load membership statistics');
    }
  }

  /**
   * Handle and normalize dashboard API errors
   * @param error Raw API error
   * @param defaultMessage Default error message
   * @returns Normalized error
   */
  private handleDashboardError(error: any, defaultMessage: string): Error {
    // Handle axios errors
    if (error.response?.data?.message) {
      return new Error(error.response.data.message);
    }

    // Handle network errors
    if (error.code === 'NETWORK_ERROR' || !error.response) {
      return new Error('Unable to connect to the server. Please check your connection and try again.');
    }

    // Handle 401 unauthorized (will be handled by global interceptor)
    if (error.response?.status === 401) {
      return new Error('You need to log in to access your dashboard.');
    }

    // Handle 404 not found
    if (error.response?.status === 404) {
      return new Error('Dashboard data not found. Please try refreshing the page.');
    }

    // Handle 500 server errors
    if (error.response?.status >= 500) {
      return new Error('Server error occurred. Please try again later.');
    }

    // Default error message
    return new Error(defaultMessage);
  }
}

/**
 * Singleton instance of the dashboard API service
 */
export const dashboardApi = new DashboardApiService();

/**
 * Utility functions for dashboard API operations
 */
export const dashboardApiUtils = {
  /**
   * Check if an error is a network error
   */
  isNetworkError(error: any): boolean {
    return error.code === 'NETWORK_ERROR' || !error.response;
  },

  /**
   * Check if an error is an authentication error
   */
  isAuthError(error: any): boolean {
    return error.response?.status === 401;
  },

  /**
   * Check if an error is a server error
   */
  isServerError(error: any): boolean {
    return error.response?.status >= 500;
  },

  /**
   * Get a user-friendly error message from an API error
   */
  getErrorMessage(error: any): string {
    if (this.isNetworkError(error)) {
      return 'Connection problem. Please check your internet and try again.';
    }

    if (this.isAuthError(error)) {
      return 'Please log in to continue.';
    }

    if (this.isServerError(error)) {
      return 'Server temporarily unavailable. Please try again in a few minutes.';
    }

    return error.message || 'An unexpected error occurred.';
  }
};