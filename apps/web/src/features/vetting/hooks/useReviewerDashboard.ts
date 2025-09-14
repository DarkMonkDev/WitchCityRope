// React hook for reviewer dashboard functionality
import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingApi, getVettingErrorMessage } from '../api/vettingApi';
import type { 
  ApplicationFilterRequest,
  ApplicationSummaryDto,
  ApplicationDetailResponse,
  ReviewDecisionRequest,
  DashboardFilters,
  DashboardStats
} from '../types/vetting.types';

/**
 * Hook for managing reviewer dashboard
 */
export const useReviewerDashboard = () => {
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<DashboardFilters>({
    status: 'all',
    assignedTo: 'all',
    dateRange: 'last30',
    search: ''
  });

  // Transform UI filters to API request format
  const transformFiltersToRequest = useCallback((uiFilters: DashboardFilters): ApplicationFilterRequest => {
    const request: ApplicationFilterRequest = {
      page: 1,
      pageSize: 20
    };

    if (uiFilters.status !== 'all') {
      request.status = uiFilters.status;
    }

    if (uiFilters.assignedTo === 'unassigned') {
      request.assignedTo = '';
    } else if (uiFilters.assignedTo !== 'all' && uiFilters.assignedTo !== 'mine') {
      request.assignedTo = uiFilters.assignedTo;
    }

    if (uiFilters.search.trim()) {
      request.searchText = uiFilters.search.trim();
    }

    // Handle date ranges
    if (uiFilters.dateRange === 'last7') {
      const sevenDaysAgo = new Date();
      sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 7);
      request.dateFrom = sevenDaysAgo.toISOString().split('T')[0];
    } else if (uiFilters.dateRange === 'custom') {
      if (uiFilters.customDateFrom) {
        request.dateFrom = uiFilters.customDateFrom;
      }
      if (uiFilters.customDateTo) {
        request.dateTo = uiFilters.customDateTo;
      }
    }

    return request;
  }, []);

  // Get dashboard data with applications and stats
  const {
    data: dashboardData,
    isLoading: isDashboardLoading,
    error: dashboardError,
    refetch: refetchDashboard
  } = useQuery({
    queryKey: ['reviewer-dashboard', filters],
    queryFn: async () => {
      const [dashboardData, applicationsData] = await Promise.all([
        vettingApi.getReviewerDashboard(),
        vettingApi.getApplications(transformFiltersToRequest(filters))
      ]);
      
      return {
        stats: dashboardData.stats,
        applications: applicationsData.items,
        pagination: {
          totalCount: applicationsData.totalCount,
          totalPages: applicationsData.totalPages,
          currentPage: applicationsData.page,
          hasNextPage: applicationsData.hasNextPage
        }
      };
    },
    refetchInterval: 30000, // Refresh every 30 seconds
    refetchIntervalInBackground: false
  });

  // Get application detail
  const getApplicationDetail = useCallback(
    (applicationId: string) => {
      return queryClient.fetchQuery({
        queryKey: ['application-detail', applicationId],
        queryFn: () => vettingApi.getApplicationDetail(applicationId),
        staleTime: 5 * 60 * 1000 // 5 minutes
      });
    },
    [queryClient]
  );

  // Submit review decision
  const submitReviewMutation = useMutation({
    mutationFn: ({ applicationId, decision }: {
      applicationId: string;
      decision: ReviewDecisionRequest;
    }) => vettingApi.submitReview(applicationId, decision),
    onSuccess: (updatedApplication, { decision }) => {
      // Invalidate and refetch related queries
      queryClient.invalidateQueries({ queryKey: ['reviewer-dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['application-detail'] });

      const actionMap = {
        approve: 'approved',
        deny: 'denied',
        'request-info': 'requested additional information for',
        'schedule-interview': 'scheduled an interview for',
        reassign: 'reassigned'
      };

      notifications.show({
        title: 'Review Submitted',
        message: `You have ${actionMap[decision.decisionType] || 'updated'} application #${updatedApplication.applicationNumber}.`,
        color: decision.decisionType === 'approve' ? 'green' : 'blue',
        autoClose: 5000,
      });
    },
    onError: (error) => {
      notifications.show({
        title: 'Review Submission Failed',
        message: getVettingErrorMessage(error),
        color: 'red',
        autoClose: 8000,
      });
    }
  });

  // Assign application to reviewer
  const assignApplicationMutation = useMutation({
    mutationFn: ({ applicationId, reviewerId }: {
      applicationId: string;
      reviewerId: string;
    }) => vettingApi.assignApplication(applicationId, reviewerId),
    onSuccess: (updatedApplication) => {
      queryClient.invalidateQueries({ queryKey: ['reviewer-dashboard'] });
      queryClient.setQueryData(
        ['application-detail', updatedApplication.id],
        updatedApplication
      );

      notifications.show({
        title: 'Application Assigned',
        message: `Application #${updatedApplication.applicationNumber} has been assigned.`,
        color: 'blue',
        autoClose: 4000,
      });
    },
    onError: (error) => {
      notifications.show({
        title: 'Assignment Failed',
        message: getVettingErrorMessage(error),
        color: 'red',
        autoClose: 6000,
      });
    }
  });

  // Add review note
  const addNoteMutation = useMutation({
    mutationFn: ({ applicationId, note }: {
      applicationId: string;
      note: { content: string; isPrivate: boolean; tags?: string[] };
    }) => vettingApi.addReviewNote(applicationId, note),
    onSuccess: (updatedApplication) => {
      queryClient.setQueryData(
        ['application-detail', updatedApplication.id],
        updatedApplication
      );

      notifications.show({
        title: 'Note Added',
        message: 'Your review note has been added to the application.',
        color: 'green',
        autoClose: 3000,
      });
    },
    onError: (error) => {
      notifications.show({
        title: 'Failed to Add Note',
        message: getVettingErrorMessage(error),
        color: 'red',
        autoClose: 5000,
      });
    }
  });

  // Update filters
  const updateFilters = useCallback((newFilters: Partial<DashboardFilters>) => {
    setFilters(prev => ({ ...prev, ...newFilters }));
  }, []);

  // Reset filters
  const resetFilters = useCallback(() => {
    setFilters({
      status: 'all',
      assignedTo: 'all',
      dateRange: 'last30',
      search: ''
    });
  }, []);

  // Get filtered applications count
  const getFilteredCount = useCallback((): number => {
    return dashboardData?.applications.length || 0;
  }, [dashboardData]);

  // Get statistics with current filters applied
  const getFilteredStats = useCallback((): Partial<DashboardStats> => {
    if (!dashboardData?.applications) return {};

    const applications = dashboardData.applications;
    const stats = {
      totalApplications: applications.length,
      newApplications: applications.filter(app => app.status === 'submitted').length,
      inReview: applications.filter(app => app.status === 'under-review').length,
      pendingInterview: applications.filter(app => app.status === 'pending-interview').length,
      awaitingReferences: applications.filter(app => 
        app.status === 'references-contacted' && !app.referenceStatus.allReferencesComplete
      ).length
    };

    return stats;
  }, [dashboardData]);

  // Quick actions
  const quickActions = {
    assignToMe: (applicationId: string, myUserId: string) => 
      assignApplicationMutation.mutate({ applicationId, reviewerId: myUserId }),
    
    requestInfo: (applicationId: string, message: string) =>
      submitReviewMutation.mutate({
        applicationId,
        decision: {
          decisionType: 'request-info',
          notes: message
        }
      }),
    
    scheduleInterview: (applicationId: string, notes: string) =>
      submitReviewMutation.mutate({
        applicationId,
        decision: {
          decisionType: 'schedule-interview',
          notes: notes
        }
      })
  };

  return {
    // Data
    applications: dashboardData?.applications || [],
    stats: dashboardData?.stats,
    pagination: dashboardData?.pagination,
    
    // Filters
    filters,
    updateFilters,
    resetFilters,
    
    // Loading states
    isLoading: isDashboardLoading,
    isSubmittingReview: submitReviewMutation.isPending,
    isAssigning: assignApplicationMutation.isPending,
    isAddingNote: addNoteMutation.isPending,
    
    // Actions
    getApplicationDetail,
    submitReview: (applicationId: string, decision: ReviewDecisionRequest) =>
      submitReviewMutation.mutate({ applicationId, decision }),
    assignApplication: (applicationId: string, reviewerId: string) =>
      assignApplicationMutation.mutate({ applicationId, reviewerId }),
    addNote: (applicationId: string, note: { content: string; isPrivate: boolean; tags?: string[] }) =>
      addNoteMutation.mutate({ applicationId, note }),
    
    // Utilities
    getFilteredCount,
    getFilteredStats,
    quickActions,
    refetchDashboard,
    
    // Errors
    error: dashboardError,
    reviewError: submitReviewMutation.error,
    assignmentError: assignApplicationMutation.error
  };
};