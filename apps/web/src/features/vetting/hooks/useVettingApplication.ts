// React hook for vetting application submission and management
import { useState, useCallback } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingApi, getVettingErrorMessage } from '../api/vettingApi';
import type { 
  CreateApplicationRequest, 
  ApplicationSubmissionResponse
} from '../types/api-types';
import type { ApplicationFormState } from '../types/api-types';
import { transformFormStateToApiRequest } from '../types/api-types';

/**
 * Hook for managing vetting application submission
 */
export const useVettingApplication = () => {
  const queryClient = useQueryClient();
  const [isDraftSaving, setIsDraftSaving] = useState(false);
  
  // Submit application mutation
  const submitApplicationMutation = useMutation({
    mutationFn: (applicationData: CreateApplicationRequest) => 
      vettingApi.submitApplication(applicationData),
    onSuccess: (response: ApplicationSubmissionResponse) => {
      notifications.show({
        title: 'Application Submitted Successfully!',
        message: `Your application #${response.applicationNumber} has been submitted. Check your email for confirmation details.`,
        color: 'green',
        autoClose: 8000,
      });
      
      // Clear any draft data after successful submission
      if (typeof window !== 'undefined') {
        localStorage.removeItem('vetting-application-draft');
        localStorage.removeItem('vetting-draft-token');
      }
      
      return response;
    },
    onError: (error) => {
      const errorMessage = getVettingErrorMessage(error);
      notifications.show({
        title: 'Submission Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 10000,
      });
    }
  });

  // Save draft mutation
  const saveDraftMutation = useMutation({
    mutationFn: (draftData: Partial<CreateApplicationRequest> & { email: string }) =>
      vettingApi.saveDraft(draftData),
    onSuccess: (response) => {
      // Store draft token for future retrieval
      if (typeof window !== 'undefined') {
        localStorage.setItem('vetting-draft-token', response.draftId);
        localStorage.setItem('vetting-draft-email', (draftData as any).email || '');
        localStorage.setItem('vetting-draft-expires', response.expiresAt);
      }
      
      notifications.show({
        title: 'Draft Saved',
        message: 'Your application draft has been saved automatically.',
        color: 'blue',
        autoClose: 3000,
      });
    },
    onError: (error) => {
      console.error('Failed to save draft:', error);
      // Don't show user notification for draft save failures to avoid spam
    }
  });

  // Load draft query
  const { data: draftData, isLoading: isDraftLoading } = useQuery({
    queryKey: ['vetting-draft'],
    queryFn: async () => {
      if (typeof window === 'undefined') return null;
      
      const draftToken = localStorage.getItem('vetting-draft-token');
      const email = localStorage.getItem('vetting-draft-email');
      
      if (!draftToken || !email) return null;
      
      try {
        return await vettingApi.loadDraft(email, draftToken);
      } catch (error) {
        // Draft might be expired or invalid, clear local storage
        localStorage.removeItem('vetting-draft-token');
        localStorage.removeItem('vetting-draft-email');
        localStorage.removeItem('vetting-draft-expires');
        return null;
      }
    },
    enabled: typeof window !== 'undefined',
    retry: false
  });

  // Auto-save draft functionality
  const autoSaveDraft = useCallback(
    async (formData: Partial<CreateApplicationRequest>, email: string) => {
      if (!email || isDraftSaving) return;
      
      setIsDraftSaving(true);
      
      try {
        // FormData is already in API format, just add email
        const draftData = { ...formData, email };
        await saveDraftMutation.mutateAsync(draftData);
      } catch (error) {
        // Silently handle auto-save errors
        console.error('Auto-save failed:', error);
      } finally {
        setIsDraftSaving(false);
      }
    },
    [isDraftSaving, saveDraftMutation]
  );

  // Manual save draft
  const saveDraft = useCallback(
    (formData: Partial<CreateApplicationRequest>, email: string) => {
      const draftData = { ...formData, email };
      return saveDraftMutation.mutate(draftData);
    },
    [saveDraftMutation]
  );

  // Submit application
  const submitApplication = useCallback(
    (formData: ApplicationFormState) => {
      const requestData = transformFormStateToApiRequest(formData);
      return submitApplicationMutation.mutate(requestData);
    },
    [submitApplicationMutation]
  );

  // Clear draft
  const clearDraft = useCallback(async () => {
    if (typeof window === 'undefined') return;
    
    const draftToken = localStorage.getItem('vetting-draft-token');
    const email = localStorage.getItem('vetting-draft-email');
    
    if (draftToken && email) {
      try {
        await vettingApi.deleteDraft(draftToken, email);
      } catch (error) {
        console.error('Failed to delete draft from server:', error);
      }
    }
    
    // Clear local storage regardless of server response
    localStorage.removeItem('vetting-draft-token');
    localStorage.removeItem('vetting-draft-email');
    localStorage.removeItem('vetting-draft-expires');
    localStorage.removeItem('vetting-application-draft');
    
    queryClient.invalidateQueries({ queryKey: ['vetting-draft'] });
  }, [queryClient]);

  return {
    // Application submission
    submitApplication,
    isSubmitting: submitApplicationMutation.isPending,
    submissionResult: submitApplicationMutation.data,
    submissionError: submitApplicationMutation.error,
    
    // Draft management
    saveDraft,
    autoSaveDraft,
    clearDraft,
    draftData,
    isDraftLoading,
    isDraftSaving,
    
    // Loading states
    isLoading: submitApplicationMutation.isPending || isDraftLoading,
  };
};

// No longer needed - form data already matches API request format