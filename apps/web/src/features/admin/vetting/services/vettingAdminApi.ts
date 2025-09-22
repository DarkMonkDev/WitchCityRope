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
    const { data } = await apiClient.post<PagedResult<ApplicationSummaryDto>>(
      '/api/vetting/reviewer/applications',
      filters
    );
    return data;
  }

  /**
   * Get detailed information for a specific application
   */
  async getApplicationDetail(
    applicationId: string
  ): Promise<ApplicationDetailResponse> {
    const { data } = await apiClient.get<ApplicationDetailResponse>(
      `/api/vetting/reviewer/applications/${applicationId}`
    );
    return data;
  }

  /**
   * Submit a review decision for an application
   */
  async submitReviewDecision(
    applicationId: string,
    decision: ReviewDecisionRequest
  ): Promise<ReviewDecisionResponse> {
    const { data } = await apiClient.post<ReviewDecisionResponse>(
      `/api/vetting/reviewer/applications/${applicationId}/decisions`,
      decision
    );
    return data;
  }

  /**
   * Add a note to an application (placeholder for future implementation)
   */
  async addApplicationNote(
    applicationId: string,
    content: string,
    isPrivate: boolean = false,
    tags: string[] = []
  ): Promise<void> {
    // TODO: Implement when API endpoint is available
    console.log('Note would be added:', { applicationId, content, isPrivate, tags });
  }
}

export const vettingAdminApi = new VettingAdminApiService();