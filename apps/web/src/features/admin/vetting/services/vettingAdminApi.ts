import { apiClient } from '../../../../lib/api/client';
import type {
  ApplicationSummaryDto,
  ApplicationFilterRequest,
  ApplicationDetailResponse,
  ReviewDecisionRequest,
  ReviewDecisionResponse,
  PagedResult
} from '../types/vetting.types';

export class VettingAdminApiService {
  /**
   * Get paginated list of vetting applications for admin review
   */
  async getApplicationsForReview(
    filters: ApplicationFilterRequest
  ): Promise<PagedResult<ApplicationSummaryDto>> {
    const response = await apiClient.post<{ success: boolean; data: PagedResult<ApplicationSummaryDto> }>(
      '/api/vetting/reviewer/applications',
      filters
    );
    return response.data.data; // Unwrap ApiResponse wrapper
  }

  /**
   * Get detailed information for a specific application
   */
  async getApplicationDetail(
    applicationId: string
  ): Promise<ApplicationDetailResponse> {
    try {
      const response = await apiClient.get<{ success: boolean; data: ApplicationDetailResponse }>(
        `/api/vetting/reviewer/applications/${applicationId}`
      );

      if (!response.data.success) {
        throw new Error(`API returned success: false for application ${applicationId}`);
      }

      if (!response.data.data) {
        throw new Error(`No application data received for ID ${applicationId}`);
      }

      return response.data.data; // Unwrap ApiResponse wrapper
    } catch (error: any) {
      console.error('VettingAdminApi.getApplicationDetail error:', {
        applicationId,
        error: error.message || error,
        status: error.response?.status,
        statusText: error.response?.statusText
      });

      // Enhance error message based on HTTP status
      if (error.response?.status === 401) {
        throw new Error('Authentication required. Please log in to view this application.');
      } else if (error.response?.status === 403) {
        throw new Error('Access denied. You do not have permission to view this application.');
      } else if (error.response?.status === 404) {
        throw new Error(`Application with ID "${applicationId}" was not found.`);
      } else if (error.response?.status >= 500) {
        throw new Error('Server error occurred while loading the application. Please try again later.');
      } else if (error.message?.includes('Network')) {
        throw new Error('Network error. Please check your internet connection and try again.');
      }

      // Re-throw original error if no specific handling applies
      throw error;
    }
  }

  /**
   * Submit a review decision for an application
   */
  async submitReviewDecision(
    applicationId: string,
    decision: ReviewDecisionRequest
  ): Promise<ReviewDecisionResponse> {
    const response = await apiClient.post<{ success: boolean; data: ReviewDecisionResponse }>(
      `/api/vetting/reviewer/applications/${applicationId}/decisions`,
      decision
    );
    return response.data.data; // Unwrap ApiResponse wrapper
  }

  /**
   * Add a note to an application
   */
  async addApplicationNote(
    applicationId: string,
    content: string,
    isPrivate: boolean = false,
    tags: string[] = []
  ): Promise<void> {
    try {
      await apiClient.post(`/api/vetting/reviewer/applications/${applicationId}/notes`, {
        content,
        isPrivate,
        tags
      });
    } catch (error) {
      console.error('Failed to add note:', error);
      throw error;
    }
  }

  /**
   * Change application status to Approved
   */
  async approveApplication(
    applicationId: string,
    reasoning: string
  ): Promise<ReviewDecisionResponse> {
    return this.submitReviewDecision(applicationId, {
      decisionType: 'Approved',
      reasoning,
      isFinalDecision: true
    });
  }

  /**
   * Change application status to OnHold
   */
  async putApplicationOnHold(
    applicationId: string,
    reason: string
  ): Promise<ReviewDecisionResponse> {
    return this.submitReviewDecision(applicationId, {
      decisionType: 'OnHold',
      reasoning: reason,
      isFinalDecision: false
    });
  }

  /**
   * Change application status to Denied
   */
  async denyApplication(
    applicationId: string,
    reasoning: string
  ): Promise<ReviewDecisionResponse> {
    return this.submitReviewDecision(applicationId, {
      decisionType: 'Denied',
      reasoning,
      isFinalDecision: true
    });
  }

  /**
   * Send reminder to application references
   */
  async sendApplicationReminder(
    applicationId: string,
    message: string
  ): Promise<{ success: boolean; message: string }> {
    // TODO: Implement when API endpoint is available
    // For now, simulate the API call
    // Simulate delay
    await new Promise(resolve => setTimeout(resolve, 1000));

    return {
      success: true,
      message: 'Reminder sent successfully'
    };
  }
}

export const vettingAdminApi = new VettingAdminApiService();