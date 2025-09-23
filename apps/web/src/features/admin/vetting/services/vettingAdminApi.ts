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
    const response = await apiClient.get<{ success: boolean; data: ApplicationDetailResponse }>(
      `/api/vetting/reviewer/applications/${applicationId}`
    );
    return response.data.data; // Unwrap ApiResponse wrapper
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
    console.log('Reminder would be sent:', { applicationId, message });

    // Simulate delay
    await new Promise(resolve => setTimeout(resolve, 1000));

    return {
      success: true,
      message: 'Reminder sent successfully'
    };
  }
}

export const vettingAdminApi = new VettingAdminApiService();