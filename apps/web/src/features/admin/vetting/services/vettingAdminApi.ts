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
   * Pattern B: Direct DTO response
   */
  async getApplicationsForReview(
    filters: ApplicationFilterRequest
  ): Promise<PagedResult<ApplicationSummaryDto>> {
    const response = await apiClient.post<PagedResult<ApplicationSummaryDto>>(
      '/api/vetting/reviewer/applications',
      filters
    );
    return response.data;
  }

  /**
   * Get detailed information for a specific application
   * Pattern B: Direct DTO response
   */
  async getApplicationDetail(
    applicationId: string
  ): Promise<ApplicationDetailResponse> {
    try {
      const response = await apiClient.get<ApplicationDetailResponse>(
        `/api/vetting/reviewer/applications/${applicationId}`
      );

      return response.data;
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
   * Pattern B: Direct DTO response
   */
  async submitReviewDecision(
    applicationId: string,
    decision: ReviewDecisionRequest
  ): Promise<ReviewDecisionResponse> {
    const response = await apiClient.post<ReviewDecisionResponse>(
      `/api/vetting/reviewer/applications/${applicationId}/decisions`,
      decision
    );
    return response.data;
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
   * Update applicant contact/identity information on a vetting application.
   * Updates the VettingApplication entity only, not the User entity.
   * Used by the inline edit feature on the admin vetting detail page.
   */
  async updateApplicantInfo(
    applicationId: string,
    data: {
      sceneName: string;
      firstName?: string | null;
      lastName?: string | null;
      email: string;
      pronouns?: string | null;
      fetLifeHandle?: string | null;
      otherNames?: string | null;
    }
  ): Promise<ApplicationDetailResponse> {
    const response = await apiClient.put<ApplicationDetailResponse>(
      `/api/vetting/reviewer/applications/${applicationId}/applicant-info`,
      data
    );
    return response.data;
  }

  /**
   * Send interview reminder email to applicant using the InterviewReminder template.
   * The custom message replaces the {{custom_message}} variable in the template.
   */
  async sendApplicationReminder(
    applicationId: string,
    customMessage?: string
  ): Promise<{ remindersSentCount: number; lastReminderSentAt: string | null }> {
    const response = await apiClient.post<{ remindersSentCount: number; lastReminderSentAt: string | null }>(
      `/api/vetting/reviewer/applications/${applicationId}/send-reminder`,
      { customMessage }
    );
    return response.data;
  }
}

export const vettingAdminApi = new VettingAdminApiService();