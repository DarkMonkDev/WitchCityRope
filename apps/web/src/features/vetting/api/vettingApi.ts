// Vetting API Service Layer
// Handles all HTTP requests to Vetting System backend

import { apiClient } from '../../../lib/api/client';
import type { ApiResponse, PaginatedResponse } from '../../../lib/api/types/api.types';
import type {
  CreateApplicationRequest,
  ApplicationSubmissionResponse,
  ApplicationStatusResponse,
  ApplicationSummaryDto,
  ApplicationDetailResponse,
  ApplicationFilterRequest,
  ReviewDecisionRequest,
  PagedResult,
  DashboardStats
} from '../types/vetting.types';

/**
 * Vetting API client following established patterns
 * Uses cookie-based authentication for protected endpoints
 */
export const vettingApi = {
  // ===== PUBLIC ENDPOINTS (No Authentication Required) =====

  /**
   * Submit a new vetting application (anonymous or authenticated)
   */
  async submitApplication(request: CreateApplicationRequest): Promise<ApplicationSubmissionResponse> {
    const { data } = await apiClient.post<ApiResponse<ApplicationSubmissionResponse>>(
      '/api/vetting/applications',
      request
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to submit application');
    }
    
    return data.data;
  },

  /**
   * Get application status by tracking token (public endpoint)
   */
  async getApplicationStatus(trackingToken: string): Promise<ApplicationStatusResponse> {
    const { data } = await apiClient.get<ApiResponse<ApplicationStatusResponse>>(
      `/api/vetting/applications/status/${trackingToken}`
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Application not found');
    }
    
    return data.data;
  },

  // ===== REVIEWER ENDPOINTS (VettingReviewer Role Required) =====

  /**
   * Get reviewer dashboard with applications and stats
   */
  async getReviewerDashboard(): Promise<{
    applications: ApplicationSummaryDto[];
    stats: DashboardStats;
  }> {
    const { data } = await apiClient.get<ApiResponse<{
      applications: ApplicationSummaryDto[];
      stats: DashboardStats;
    }>>('/api/vetting/reviewer/dashboard');
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load reviewer dashboard');
    }
    
    return data.data;
  },

  /**
   * Get filtered list of applications for reviewers
   */
  async getApplications(filters: ApplicationFilterRequest): Promise<PagedResult<ApplicationSummaryDto>> {
    const { data } = await apiClient.get<ApiResponse<PagedResult<ApplicationSummaryDto>>>(
      '/api/vetting/reviewer/applications',
      { params: filters }
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load applications');
    }
    
    return data.data;
  },

  /**
   * Get detailed application information for review
   */
  async getApplicationDetail(applicationId: string): Promise<ApplicationDetailResponse> {
    const { data } = await apiClient.get<ApiResponse<ApplicationDetailResponse>>(
      `/api/vetting/reviewer/applications/${applicationId}`
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to get application details');
    }
    
    return data.data;
  },

  /**
   * Submit review decision for application
   */
  async submitReview(applicationId: string, decision: ReviewDecisionRequest): Promise<ApplicationDetailResponse> {
    const { data } = await apiClient.post<ApiResponse<ApplicationDetailResponse>>(
      `/api/vetting/reviewer/applications/${applicationId}/review`,
      decision
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to submit review');
    }
    
    return data.data;
  },

  /**
   * Assign application to reviewer
   */
  async assignApplication(applicationId: string, reviewerId: string): Promise<ApplicationDetailResponse> {
    const { data } = await apiClient.patch<ApiResponse<ApplicationDetailResponse>>(
      `/api/vetting/reviewer/applications/${applicationId}/assign`,
      { reviewerId }
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to assign application');
    }
    
    return data.data;
  },

  /**
   * Add review note to application
   */
  async addReviewNote(applicationId: string, note: {
    content: string;
    isPrivate: boolean;
    tags?: string[];
  }): Promise<ApplicationDetailResponse> {
    const { data } = await apiClient.post<ApiResponse<ApplicationDetailResponse>>(
      `/api/vetting/reviewer/applications/${applicationId}/notes`,
      note
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to add review note');
    }
    
    return data.data;
  },

  // ===== ADMIN ENDPOINTS (VettingAdmin Role Required) =====

  /**
   * Get comprehensive vetting analytics
   */
  async getAnalytics(params: {
    dateFrom?: string;
    dateTo?: string;
    granularity?: 'day' | 'week' | 'month';
  } = {}): Promise<{
    stats: DashboardStats;
    trends: any[];
    reviewerPerformance: any[];
  }> {
    const { data } = await apiClient.get<ApiResponse<{
      stats: DashboardStats;
      trends: any[];
      reviewerPerformance: any[];
    }>>('/api/vetting/admin/analytics', { params });
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load analytics');
    }
    
    return data.data;
  },

  /**
   * Get all reviewers and their workload
   */
  async getReviewers(): Promise<{
    id: string;
    name: string;
    email: string;
    currentWorkload: number;
    maxWorkload: number;
    averageReviewTime: number;
    totalReviews: number;
    isActive: boolean;
  }[]> {
    const { data } = await apiClient.get<ApiResponse<{
      id: string;
      name: string;
      email: string;
      currentWorkload: number;
      maxWorkload: number;
      averageReviewTime: number;
      totalReviews: number;
      isActive: boolean;
    }[]>>('/api/vetting/admin/reviewers');
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to load reviewers');
    }
    
    return data.data;
  },

  /**
   * Bulk assign applications to reviewers
   */
  async bulkAssignApplications(assignments: {
    applicationId: string;
    reviewerId: string;
  }[]): Promise<{ successCount: number; errors: string[] }> {
    const { data } = await apiClient.post<ApiResponse<{
      successCount: number;
      errors: string[];
    }>>('/api/vetting/admin/bulk-assign', { assignments });
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to bulk assign applications');
    }
    
    return data.data;
  },

  /**
   * Send reminder emails to references
   */
  async sendReferenceReminders(applicationId: string): Promise<{
    remindersSent: number;
    errors: string[];
  }> {
    const { data } = await apiClient.post<ApiResponse<{
      remindersSent: number;
      errors: string[];
    }>>(`/api/vetting/admin/applications/${applicationId}/remind-references`);
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to send reference reminders');
    }
    
    return data.data;
  },

  /**
   * Export applications data for reporting
   */
  async exportApplications(filters: ApplicationFilterRequest & {
    format: 'csv' | 'excel';
    includePersonalInfo: boolean;
  }): Promise<Blob> {
    const response = await apiClient.get(
      '/api/vetting/admin/export',
      { 
        params: filters,
        responseType: 'blob'
      }
    );
    
    return response.data;
  },

  // ===== DRAFT MANAGEMENT =====

  /**
   * Save application draft (requires email for identification)
   */
  async saveDraft(draftData: Partial<CreateApplicationRequest> & { email: string }): Promise<{
    draftId: string;
    lastSaved: string;
    expiresAt: string;
  }> {
    const { data } = await apiClient.post<ApiResponse<{
      draftId: string;
      lastSaved: string;
      expiresAt: string;
    }>>('/api/vetting/drafts', draftData);
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to save draft');
    }
    
    return data.data;
  },

  /**
   * Load application draft by email and draft token
   */
  async loadDraft(email: string, draftToken: string): Promise<Partial<CreateApplicationRequest>> {
    const { data } = await apiClient.get<ApiResponse<Partial<CreateApplicationRequest>>>(
      `/api/vetting/drafts/${draftToken}`,
      { params: { email } }
    );
    
    if (!data.data) {
      throw new Error(data.error || 'Draft not found or expired');
    }
    
    return data.data;
  },

  /**
   * Delete application draft
   */
  async deleteDraft(draftToken: string, email: string): Promise<void> {
    await apiClient.delete(`/api/vetting/drafts/${draftToken}`, {
      params: { email }
    });
  },

  // ===== REFERENCE MANAGEMENT =====

  /**
   * Get reference form by reference token (public endpoint)
   */
  async getReferenceForm(referenceToken: string): Promise<{
    applicantName: string;
    referenceRequest: {
      name: string;
      email: string;
      relationship: string;
    };
    isExpired: boolean;
    hasResponded: boolean;
  }> {
    const { data } = await apiClient.get<ApiResponse<{
      applicantName: string;
      referenceRequest: {
        name: string;
        email: string;
        relationship: string;
      };
      isExpired: boolean;
      hasResponded: boolean;
    }>>(`/api/vetting/references/${referenceToken}/form`);
    
    if (!data.data) {
      throw new Error(data.error || 'Reference form not found or expired');
    }
    
    return data.data;
  },

  /**
   * Submit reference response (public endpoint)
   */
  async submitReferenceResponse(referenceToken: string, response: {
    relationshipDuration: string;
    experienceAssessment: string;
    safetyConcerns?: string;
    communityReadiness: string;
    recommendation: string;
    additionalComments?: string;
  }): Promise<{ success: boolean; message: string }> {
    const { data } = await apiClient.post<ApiResponse<{
      success: boolean;
      message: string;
    }>>(`/api/vetting/references/${referenceToken}/response`, response);
    
    if (!data.data) {
      throw new Error(data.error || 'Failed to submit reference response');
    }
    
    return data.data;
  }
};

/**
 * API error handling utility
 */
export class VettingApiError extends Error {
  constructor(
    message: string,
    public statusCode?: number,
    public errorCode?: string
  ) {
    super(message);
    this.name = 'VettingApiError';
  }
}

/**
 * Transform API errors to user-friendly messages
 */
export const getVettingErrorMessage = (error: any): string => {
  if (error instanceof VettingApiError) {
    return error.message;
  }
  
  if (error.response?.status === 401) {
    return 'You are not authorized to perform this action. Please log in.';
  }
  
  if (error.response?.status === 403) {
    return 'You do not have permission to access this information.';
  }
  
  if (error.response?.status === 404) {
    return 'The requested application or resource was not found.';
  }
  
  if (error.response?.status === 429) {
    return 'Too many requests. Please wait a moment and try again.';
  }
  
  if (error.response?.status >= 500) {
    return 'A server error occurred. Please try again later or contact support.';
  }
  
  return error.message || 'An unexpected error occurred. Please try again.';
};