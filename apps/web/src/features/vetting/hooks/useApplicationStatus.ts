// React hook for application status tracking
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingApi, getVettingErrorMessage } from '../api/vettingApi';
import type { ApplicationStatusResponse } from '../types/vetting.types';

/**
 * Hook for checking application status by tracking token
 */
export const useApplicationStatus = (trackingToken?: string) => {
  const queryClient = useQueryClient();

  // Query application status
  const {
    data: statusData,
    isLoading,
    error,
    refetch
  } = useQuery({
    queryKey: ['vetting-application-status', trackingToken],
    queryFn: () => {
      if (!trackingToken) {
        throw new Error('Tracking token is required');
      }
      return vettingApi.getApplicationStatus(trackingToken);
    },
    enabled: !!trackingToken,
    retry: (failureCount, error: any) => {
      // Don't retry for 404 (not found) or 403 (expired token)
      if (error?.response?.status === 404 || error?.response?.status === 403) {
        return false;
      }
      return failureCount < 3;
    },
    // Poll for updates every 5 minutes if status is in progress
    refetchInterval: (data) => {
      if (!data) return false;
      
      // Keep polling for active statuses
      const activeStatuses = [
        'submitted',
        'references-contacted',
        'under-review',
        'pending-interview',
        'pending-additional-info'
      ];
      
      return activeStatuses.includes(data.status) ? 5 * 60 * 1000 : false; // 5 minutes
    },
    refetchIntervalInBackground: true
  });

  // Manual refresh
  const refreshStatus = () => {
    refetch();
    notifications.show({
      title: 'Status Updated',
      message: 'Application status has been refreshed.',
      color: 'blue',
      autoClose: 3000,
    });
  };

  // Check if status indicates application is complete (final state)
  const isComplete = (statusData as any)?.status === 'approved' || (statusData as any)?.status === 'denied' || (statusData as any)?.status === 'withdrawn';

  // Check if additional information is needed
  const needsAdditionalInfo = (statusData as any)?.status === 'pending-additional-info';

  // Get user-friendly status message
  const getStatusMessage = (): string => {
    if (!statusData) return '';

    switch ((statusData as any)?.status) {
      case 'submitted':
        return 'Your application has been received and is in the queue for review.';
      case 'references-contacted':
        return 'We have contacted your references for verification. This process typically takes 3-5 business days.';
      case 'under-review':
        return 'Your application is currently being reviewed by our vetting team.';
      case 'pending-interview':
        return 'An interview has been scheduled or is being arranged. You will receive contact details soon.';
      case 'pending-additional-info':
        return 'We need some additional information to complete your application review.';
      case 'approved':
        return 'Congratulations! Your application has been approved. Welcome to our community!';
      case 'denied':
        return 'Your application was not approved at this time. You may reapply after 6 months.';
      case 'withdrawn':
        return 'Your application has been withdrawn as requested.';
      default:
        return (statusData as any)?.statusDescription || 'Status unknown.';
    }
  };

  // Get estimated time remaining
  const getTimeEstimate = (): string => {
    if (!(statusData as any)?.estimatedDaysRemaining) return '';
    
    const days = (statusData as any)?.estimatedDaysRemaining;
    if (days <= 1) return 'Decision expected within 1 day';
    if (days <= 7) return `Decision expected within ${days} days`;
    if (days <= 14) return `Decision expected within ${Math.ceil(days / 7)} ${Math.ceil(days / 7) === 1 ? 'week' : 'weeks'}`;
    return `Decision expected within ${Math.ceil(days / 7)} weeks`;
  };

  // Get progress percentage for UI
  const getProgressPercentage = (): number => {
    return (statusData as any)?.progress?.progressPercentage || 0;
  };

  // Get current phase name
  const getCurrentPhase = (): string => {
    return (statusData as any)?.progress?.currentPhase || 'Unknown';
  };

  return {
    // Data
    statusData,
    
    // Loading states
    isLoading,
    error,
    
    // Status helpers
    isComplete,
    needsAdditionalInfo,
    getStatusMessage,
    getTimeEstimate,
    getProgressPercentage,
    getCurrentPhase,
    
    // Actions
    refreshStatus,
    
    // Query controls
    refetch
  };
};

/**
 * Hook for tracking multiple applications (for admin/reviewer use)
 */
export const useMultipleApplicationStatus = (trackingTokens: string[]) => {
  const queries = trackingTokens.map(token => ({
    queryKey: ['vetting-application-status', token],
    queryFn: () => vettingApi.getApplicationStatus(token),
    retry: 1,
    enabled: !!token
  }));

  // Use parallel queries for multiple status checks
  const results = useQuery({
    queryKey: ['multiple-application-status', ...trackingTokens],
    queryFn: async () => {
      const promises = trackingTokens.map(token => 
        token ? vettingApi.getApplicationStatus(token) : Promise.resolve(null)
      );
      
      const results = await Promise.allSettled(promises);
      return results.map((result, index) => ({
        token: trackingTokens[index],
        status: result.status === 'fulfilled' ? result.value : null,
        error: result.status === 'rejected' ? result.reason : null
      }));
    },
    enabled: trackingTokens.length > 0
  });

  return {
    results: results.data || [],
    isLoading: results.isLoading,
    error: results.error
  };
};

/**
 * Hook for managing status page URL parameters
 */
export const useStatusPageParams = () => {
  // Extract tracking token from URL or localStorage
  const getTrackingToken = (): string | undefined => {
    if (typeof window === 'undefined') return undefined;
    
    // First, check URL parameters
    const urlParams = new URLSearchParams(window.location.search);
    const urlToken = urlParams.get('token');
    
    if (urlToken) {
      // Store in localStorage for future visits
      localStorage.setItem('vetting-status-token', urlToken);
      return urlToken;
    }
    
    // Fall back to localStorage
    return localStorage.getItem('vetting-status-token') || undefined;
  };

  // Clear stored token (for logout or starting new application)
  const clearStoredToken = () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('vetting-status-token');
    }
  };

  // Update URL with tracking token (for sharing)
  const updateUrlWithToken = (token: string) => {
    if (typeof window === 'undefined') return;
    
    const url = new URL(window.location.href);
    url.searchParams.set('token', token);
    window.history.replaceState({}, '', url.toString());
  };

  return {
    getTrackingToken,
    clearStoredToken,
    updateUrlWithToken
  };
};