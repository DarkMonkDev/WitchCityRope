// Custom hook for simplified vetting application
// Handles form submission and status checking

import { useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
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

  // Check for existing application
  const {
    data: existingApplication,
    isLoading: isCheckingApplication,
    error: checkError,
    refetch: recheckApplication
  } = useQuery({
    queryKey: ['vetting', 'my-application'],
    queryFn: simplifiedVettingApi.checkExistingApplication,
    retry: false,
    refetchOnWindowFocus: false,
  });

  // Submit application mutation
  const submitMutation = useMutation({
    mutationFn: (request: SimplifiedCreateApplicationRequest): Promise<SimplifiedApplicationSubmissionResponse> =>
      simplifiedVettingApi.submitApplication(request),
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
    enabled: !!existingApplication,
    retry: false,
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

    // Utility functions
    refreshStatus,
    hasExistingApplication: !!existingApplication,
    canSubmitNew: !existingApplication,

    // Combined loading state
    isLoading: isCheckingApplication || isLoadingStatus || submitMutation.isPending,
  };
};