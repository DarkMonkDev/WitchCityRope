// Custom hook for simplified vetting application
// Handles form submission and status checking

import { useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { useAuthStore } from '../../../stores/authStore';
import { simplifiedVettingApi, getSimplifiedVettingErrorMessage } from '../api/simplifiedVettingApi';
import type {
  SimplifiedCreateApplicationRequest,
  SimplifiedApplicationSubmissionResponse,
  SimplifiedApplicationStatus
} from '../types/simplified-vetting.types';

/**
 * Hook for managing simplified vetting application process
 */
export const useSimplifiedVettingApplication = () => {
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  // Check for existing application
  const {
    data: existingApplication,
    isLoading: isCheckingApplication,
    error: checkError,
    refetch: recheckApplication
  } = useQuery({
    queryKey: ['vetting', 'my-application'],
    queryFn: simplifiedVettingApi.checkExistingApplication,
    enabled: !!user && isAuthenticated, // Only run when authenticated
    retry: false,
    refetchOnWindowFocus: false,
    // Handle auth errors gracefully
    throwOnError: (error: any) => {
      // Don't throw 401 errors - let the UI handle auth state
      return error?.response?.status !== 401;
    }
  });

  // Submit application mutation
  const submitMutation = useMutation({
    mutationFn: (request: SimplifiedCreateApplicationRequest): Promise<SimplifiedApplicationSubmissionResponse> => {
      // Pre-flight authentication check
      if (!isAuthenticated || !user) {
        throw new Error('You must be logged in to submit an application. Please login or create an account first.');
      }
      return simplifiedVettingApi.submitApplication(request);
    },
    onSuccess: (response) => {
      // Invalidate application check query to reflect new status
      queryClient.invalidateQueries({ queryKey: ['vetting', 'my-application'] });

      notifications.show({
        title: 'Application Submitted Successfully!',
        message: `Your application #${response.applicationNumber} has been submitted. Check your email for confirmation.`,
        color: 'green',
        autoClose: 10000,
      });

      return response;
    },
    onError: (error) => {
      const errorMessage = getSimplifiedVettingErrorMessage(error);

      notifications.show({
        title: 'Submission Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 8000,
      });

      throw error;
    }
  });

  // Get application status
  const {
    data: applicationStatus,
    isLoading: isLoadingStatus,
    error: statusError,
    refetch: refetchStatus
  } = useQuery({
    queryKey: ['vetting', 'application-status'],
    queryFn: simplifiedVettingApi.getMyApplicationStatus,
    enabled: !!existingApplication && !!user && isAuthenticated,
    retry: false,
    // Handle auth errors gracefully
    throwOnError: (error: any) => {
      return error?.response?.status !== 401;
    }
  });

  // Submit application helper
  const submitApplication = useCallback(
    (request: SimplifiedCreateApplicationRequest) => {
      return submitMutation.mutateAsync(request);
    },
    [submitMutation]
  );

  // Refresh application status
  const refreshStatus = useCallback(() => {
    if (existingApplication) {
      refetchStatus();
    } else {
      recheckApplication();
    }
  }, [existingApplication, refetchStatus, recheckApplication]);

  return {
    // Application submission
    submitApplication,
    isSubmitting: submitMutation.isPending,
    submissionError: submitMutation.error,

    // Existing application check
    existingApplication,
    isCheckingApplication,
    checkError,

    // Application status
    applicationStatus: applicationStatus || existingApplication,
    isLoadingStatus,
    statusError,

    // Authentication state
    isAuthenticated,
    user,

    // Utility functions
    refreshStatus,
    hasExistingApplication: !!existingApplication,
    canSubmitNew: !existingApplication && isAuthenticated && !!user,

    // Combined loading state
    isLoading: isCheckingApplication || isLoadingStatus || submitMutation.isPending,
  };
};