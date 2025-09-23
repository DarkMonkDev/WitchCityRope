import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { vettingAdminApi } from '../../../../../../apps/web/src/features/admin/vetting/services/vettingAdminApi';
import { apiClient } from '../../../../../../apps/web/src/lib/api/client';
import type {
  ApplicationFilterRequest,
  ApplicationSummaryDto,
  ApplicationDetailResponse,
  ReviewDecisionRequest,
  ReviewDecisionResponse,
  PagedResult
} from '../../../../../../apps/web/src/features/admin/vetting/types/vetting.types';

// Mock the API client
vi.mock('../../../../../../apps/web/src/lib/api/client', () => ({
  apiClient: {
    post: vi.fn(),
    get: vi.fn(),
  },
}));

const mockApiClient = vi.mocked(apiClient);

describe('VettingAdminApiService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('getApplicationsForReview', () => {
    const mockFilters: ApplicationFilterRequest = {
      page: 1,
      pageSize: 25,
      statusFilters: ['UnderReview'],
      priorityFilters: [],
      experienceLevelFilters: [],
      skillsFilters: [],
      searchQuery: '',
      sortBy: 'SubmittedAt',
      sortDirection: 'Desc'
    };

    const mockApplications: ApplicationSummaryDto[] = [
      {
        id: 'app-1',
        applicationNumber: 'APP001',
        status: 'UnderReview',
        submittedAt: '2025-09-20T10:00:00Z',
        lastActivityAt: '2025-09-20T10:00:00Z',
        sceneName: 'TestUser1',
        experienceLevel: 'Beginner',
        yearsExperience: 1,
        isAnonymous: false,
        assignedReviewerName: null,
        reviewStartedAt: null,
        priority: 1,
        daysInCurrentStatus: 2,
        referenceStatus: {
          totalReferences: 2,
          contactedReferences: 1,
          respondedReferences: 0,
          allReferencesComplete: false
        },
        hasRecentNotes: false,
        hasPendingActions: true,
        interviewScheduledFor: null,
        skillsTags: ['Rope']
      }
    ];

    const mockPagedResult: PagedResult<ApplicationSummaryDto> = {
      items: mockApplications,
      totalCount: 1,
      page: 1,
      pageSize: 25,
      totalPages: 1
    };

    it('successfully fetches applications with filters', async () => {
      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockPagedResult
        }
      });

      const result = await vettingAdminApi.getApplicationsForReview(mockFilters);

      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications',
        mockFilters
      );
      expect(result).toEqual(mockPagedResult);
    });

    it('handles API errors gracefully', async () => {
      const mockError = new Error('Network error');
      mockApiClient.post.mockRejectedValue(mockError);

      await expect(vettingAdminApi.getApplicationsForReview(mockFilters))
        .rejects.toThrow('Network error');
    });

    it('sends correct request structure', async () => {
      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockPagedResult
        }
      });

      const filters: ApplicationFilterRequest = {
        page: 2,
        pageSize: 50,
        statusFilters: ['UnderReview', 'Approved'],
        priorityFilters: [1, 2],
        experienceLevelFilters: ['Beginner'],
        skillsFilters: ['Rope', 'Bondage'],
        searchQuery: 'test user',
        sortBy: 'RealName',
        sortDirection: 'Asc'
      };

      await vettingAdminApi.getApplicationsForReview(filters);

      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications',
        filters
      );
    });
  });

  describe('getApplicationDetail', () => {
    const mockApplicationDetail: ApplicationDetailResponse = {
      id: 'app-1',
      applicationNumber: 'APP001',
      status: 'UnderReview',
      submittedAt: '2025-09-20T10:00:00Z',
      lastActivityAt: '2025-09-20T10:00:00Z',
      fullName: 'John Doe',
      sceneName: 'TestUser1',
      pronouns: 'he/him',
      email: 'john.doe@example.com',
      experienceLevel: 'Beginner',
      yearsExperience: 1,
      experienceDescription: 'I have been interested in rope bondage for about a year.',
      safetyKnowledge: 'I understand the basics of safe rope practices.',
      consentUnderstanding: 'Consent is paramount in all activities.',
      whyJoinCommunity: 'I want to learn from experienced practitioners.',
      skillsInterests: ['Rope', 'Bondage'],
      expectationsGoals: 'Learn advanced techniques.',
      agreesToGuidelines: true,
      isAnonymous: false,
      agreesToTerms: true,
      consentToContact: true,
      assignedReviewerName: null,
      reviewStartedAt: null,
      priority: 1,
      interviewScheduledFor: null,
      references: [],
      notes: [],
      decisions: []
    };

    it('successfully fetches application detail', async () => {
      mockApiClient.get.mockResolvedValue({
        data: {
          success: true,
          data: mockApplicationDetail
        }
      });

      const result = await vettingAdminApi.getApplicationDetail('app-1');

      expect(mockApiClient.get).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications/app-1'
      );
      expect(result).toEqual(mockApplicationDetail);
    });

    it('handles application not found error', async () => {
      const mockError = new Error('Application not found');
      mockApiClient.get.mockRejectedValue(mockError);

      await expect(vettingAdminApi.getApplicationDetail('nonexistent'))
        .rejects.toThrow('Application not found');
    });
  });

  describe('submitReviewDecision', () => {
    const mockDecision: ReviewDecisionRequest = {
      decisionType: 'Approved',
      reasoning: 'Application looks good',
      isFinalDecision: true,
      proposedInterviewTime: undefined
    };

    const mockDecisionResponse: ReviewDecisionResponse = {
      decisionId: 'decision-1',
      decisionType: 'Approved',
      submittedAt: '2025-09-22T10:00:00Z',
      newApplicationStatus: 'Approved',
      confirmationMessage: 'Decision submitted successfully',
      actionsTriggered: []
    };

    it('successfully submits review decision', async () => {
      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockDecisionResponse
        }
      });

      const result = await vettingAdminApi.submitReviewDecision('app-1', mockDecision);

      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications/app-1/decisions',
        mockDecision
      );
      expect(result).toEqual(mockDecisionResponse);
    });

    it('handles validation errors', async () => {
      const validationError = {
        response: {
          status: 400,
          data: {
            success: false,
            error: 'Invalid decision type'
          }
        }
      };
      mockApiClient.post.mockRejectedValue(validationError);

      await expect(vettingAdminApi.submitReviewDecision('app-1', mockDecision))
        .rejects.toEqual(validationError);
    });
  });

  describe('approveApplication', () => {
    it('submits approval decision with correct parameters', async () => {
      const mockDecisionResponse: ReviewDecisionResponse = {
        decisionId: 'decision-1',
        decisionType: 'Approved',
        submittedAt: '2025-09-22T10:00:00Z',
        newApplicationStatus: 'Approved',
        confirmationMessage: 'Application approved successfully',
        actionsTriggered: []
      };

      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockDecisionResponse
        }
      });

      const result = await vettingAdminApi.approveApplication('app-1', 'Application meets all criteria');

      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications/app-1/decisions',
        {
          decisionType: 'Approved',
          reasoning: 'Application meets all criteria',
          isFinalDecision: true
        }
      );
      expect(result).toEqual(mockDecisionResponse);
    });
  });

  describe('putApplicationOnHold', () => {
    it('submits on hold decision with correct parameters', async () => {
      const mockDecisionResponse: ReviewDecisionResponse = {
        decisionId: 'decision-1',
        decisionType: 'OnHold',
        submittedAt: '2025-09-22T10:00:00Z',
        newApplicationStatus: 'OnHold',
        confirmationMessage: 'Application put on hold successfully',
        actionsTriggered: []
      };

      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockDecisionResponse
        }
      });

      const result = await vettingAdminApi.putApplicationOnHold('app-1', 'Need additional references');

      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/vetting/reviewer/applications/app-1/decisions',
        {
          decisionType: 'OnHold',
          reasoning: 'Need additional references',
          isFinalDecision: false
        }
      );
      expect(result).toEqual(mockDecisionResponse);
    });
  });

  describe('sendApplicationReminder', () => {
    it('simulates sending reminder successfully', async () => {
      const result = await vettingAdminApi.sendApplicationReminder('app-1', 'Please provide additional information');

      expect(result).toEqual({
        success: true,
        message: 'Reminder sent successfully'
      });
    });

    it('includes delay to simulate API call', async () => {
      const startTime = Date.now();
      await vettingAdminApi.sendApplicationReminder('app-1', 'Test message');
      const endTime = Date.now();

      // Should take at least 1000ms due to simulated delay
      expect(endTime - startTime).toBeGreaterThanOrEqual(1000);
    });
  });

  describe('addApplicationNote', () => {
    it('logs note addition for future implementation', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      await vettingAdminApi.addApplicationNote('app-1', 'This is a test note', false, ['tag1']);

      expect(consoleSpy).toHaveBeenCalledWith(
        'Note would be added:',
        {
          applicationId: 'app-1',
          content: 'This is a test note',
          isPrivate: false,
          tags: ['tag1']
        }
      );

      consoleSpy.mockRestore();
    });

    it('uses default parameters correctly', async () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      await vettingAdminApi.addApplicationNote('app-1', 'Note without optional params');

      expect(consoleSpy).toHaveBeenCalledWith(
        'Note would be added:',
        {
          applicationId: 'app-1',
          content: 'Note without optional params',
          isPrivate: false,
          tags: []
        }
      );

      consoleSpy.mockRestore();
    });
  });

  describe('error handling', () => {
    it('handles network errors consistently', async () => {
      const networkError = new Error('Network unavailable');
      mockApiClient.post.mockRejectedValue(networkError);

      await expect(vettingAdminApi.getApplicationsForReview({
        page: 1,
        pageSize: 25,
        statusFilters: [],
        priorityFilters: [],
        experienceLevelFilters: [],
        skillsFilters: [],
        searchQuery: '',
        sortBy: 'SubmittedAt',
        sortDirection: 'Desc'
      })).rejects.toThrow('Network unavailable');
    });

    it('handles authentication errors', async () => {
      const authError = {
        response: {
          status: 401,
          data: {
            success: false,
            error: 'Unauthorized'
          }
        }
      };
      mockApiClient.get.mockRejectedValue(authError);

      await expect(vettingAdminApi.getApplicationDetail('app-1'))
        .rejects.toEqual(authError);
    });

    it('handles server errors', async () => {
      const serverError = {
        response: {
          status: 500,
          data: {
            success: false,
            error: 'Internal server error'
          }
        }
      };
      mockApiClient.post.mockRejectedValue(serverError);

      await expect(vettingAdminApi.submitReviewDecision('app-1', {
        decisionType: 'Approved',
        reasoning: 'Test',
        isFinalDecision: true
      })).rejects.toEqual(serverError);
    });
  });

  describe('response data unwrapping', () => {
    it('correctly unwraps ApiResponse wrapper for applications list', async () => {
      const mockData = {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 25,
        totalPages: 0
      };

      mockApiClient.post.mockResolvedValue({
        data: {
          success: true,
          data: mockData,
          message: 'Success'
        }
      });

      const result = await vettingAdminApi.getApplicationsForReview({
        page: 1,
        pageSize: 25,
        statusFilters: [],
        priorityFilters: [],
        experienceLevelFilters: [],
        skillsFilters: [],
        searchQuery: '',
        sortBy: 'SubmittedAt',
        sortDirection: 'Desc'
      });

      expect(result).toEqual(mockData);
    });

    it('correctly unwraps ApiResponse wrapper for application detail', async () => {
      const mockDetail = {
        id: 'app-1',
        applicationNumber: 'APP001',
        status: 'UnderReview'
        // ... other properties would be here
      };

      mockApiClient.get.mockResolvedValue({
        data: {
          success: true,
          data: mockDetail,
          message: 'Success'
        }
      });

      const result = await vettingAdminApi.getApplicationDetail('app-1');

      expect(result).toEqual(mockDetail);
    });
  });
});
