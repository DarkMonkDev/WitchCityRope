// Safety API Service Layer
// Handles all HTTP requests to Safety System backend

import { apiClient } from '../../../lib/api/client';
import type { ApiResponse, PaginatedResponse } from '../../../lib/api/types/api.types';
import type {
  SubmitIncidentRequest,
  SubmissionResponse,
  IncidentStatusResponse,
  SafetyIncidentDto,
  SafetyDashboardResponse,
  IncidentSummaryDto,
  UpdateIncidentRequest,
  SearchIncidentsRequest,
  FrontendIncidentDetails
} from '../types/safety.types';
import { mapIncidentDetailFromBackend } from '../types/safety.types';

/**
 * Safety API client following established patterns
 * Uses cookie-based authentication for protected endpoints
 */
export const safetyApi = {
  /**
   * Submit a new safety incident report (anonymous or authenticated)
   */
  async submitIncident(request: SubmitIncidentRequest): Promise<SubmissionResponse> {
    const { data } = await apiClient.post<ApiResponse<SubmissionResponse>>(
      '/api/safety/incidents',
      request
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to submit incident');
    }
    
    return data.data;
  },

  /**
   * Get incident status by reference number (public endpoint)
   */
  async getIncidentStatus(referenceNumber: string): Promise<IncidentStatusResponse> {
    const { data } = await apiClient.get<ApiResponse<IncidentStatusResponse>>(
      `/api/safety/incidents/${referenceNumber}/status`
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Incident not found');
    }
    
    return data.data;
  },

  /**
   * Get detailed incident information (safety team only)
   * Transforms backend IncidentResponse (assignedTo/assignedUserName) to
   * frontend-friendly format (coordinatorId/coordinatorName)
   */
  async getIncidentDetail(incidentId: string): Promise<FrontendIncidentDetails> {
    const { data } = await apiClient.get<ApiResponse<SafetyIncidentDto>>(
      `/api/safety/admin/incidents/${incidentId}`
    );

    if (!data.data) {
      throw new Error(data.error || 'Failed to get incident details');
    }

    // Transform backend field names to frontend expectations
    return mapIncidentDetailFromBackend(data.data);
  },

  /**
   * Get safety dashboard data (safety team only)
   */
  async getDashboardData(): Promise<SafetyDashboardResponse> {
    const { data } = await apiClient.get<ApiResponse<SafetyDashboardResponse>>(
      '/api/safety/admin/dashboard'
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load dashboard data');
    }
    
    return data.data;
  },

  /**
   * Search and filter incidents (safety team only)
   */
  async searchIncidents(request: SearchIncidentsRequest): Promise<PaginatedResponse<IncidentSummaryDto>> {
    // Map frontend request to backend AdminIncidentListRequest parameter names
    const params = {
      Search: request.searchText || '',
      Status: request.status || '', // Backend supports comma-separated values
      Page: request.page || 1,
      PageSize: request.pageSize || 25,
      SortBy: request.sortBy || 'reportedAt',
      SortOrder: request.sortDirection || 'desc'
    };

    // Backend returns PaginatedIncidentListResponse directly (not wrapped in ApiResponse)
    // with lowercase property names (JSON serialization)
    interface BackendPaginatedResponse {
      items: IncidentSummaryDto[];
      totalCount: number;
      page: number;
      pageSize: number;
      totalPages: number;
    }

    const { data } = await apiClient.get<BackendPaginatedResponse>(
      '/api/safety/admin/incidents',
      { params }
    );

    // Transform backend response to match frontend PaginatedResponse type
    return {
      items: data.items,
      page: data.page,
      limit: data.pageSize,
      total: data.totalCount,
      totalPages: data.totalPages,
      hasNext: data.page < data.totalPages,
      hasPrev: data.page > 1
    };
  },

  /**
   * Update incident status and assignment (safety team only)
   * Transforms backend IncidentResponse to frontend-friendly format
   */
  async updateIncident(incidentId: string, request: UpdateIncidentRequest): Promise<FrontendIncidentDetails> {
    const { data } = await apiClient.patch<ApiResponse<SafetyIncidentDto>>(
      `/api/safety/admin/incidents/${incidentId}`,
      request
    );

    if (!data.data) {
      throw new Error(data.error || 'Failed to update incident');
    }

    // Transform backend field names to frontend expectations
    return mapIncidentDetailFromBackend(data.data);
  },

  /**
   * Get user's own incident reports (authenticated users)
   */
  async getUserReports(): Promise<IncidentSummaryDto[]> {
    const { data } = await apiClient.get<ApiResponse<IncidentSummaryDto[]>>(
      '/api/safety/my-reports'
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to get user reports');
    }
    
    return data.data;
  },

  /**
   * Get safety dashboard data for admin/safety team
   * Backend returns AdminDashboardResponse directly (NOT wrapped in ApiResponse)
   */
  async getSafetyDashboard(): Promise<SafetyDashboardResponse> {
    const { data } = await apiClient.get<SafetyDashboardResponse>(
      '/api/safety/admin/dashboard'
    );

    return data;
  },

  /**
   * Delete an incident (admin only)
   */
  async deleteIncident(incidentId: string): Promise<void> {
    await apiClient.delete(`/api/safety/admin/incidents/${incidentId}`);
  },

  /**
   * Bulk update incidents (admin only)
   */
  async bulkUpdateIncidents(updates: any[]): Promise<void> {
    await apiClient.patch('/api/safety/admin/incidents/bulk', updates);
  },

  /**
   * Export incidents data (admin only)
   */
  async exportIncidents(filters: SearchIncidentsRequest): Promise<Blob> {
    const response = await apiClient.get('/api/safety/admin/incidents/export', {
      params: filters,
      responseType: 'blob'
    });
    return response.data;
  }
};