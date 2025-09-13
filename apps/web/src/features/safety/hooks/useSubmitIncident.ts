// Specialized hook for incident submission with form integration
// Provides enhanced form submission with validation and status tracking

import { useState, useCallback } from 'react';
import { useSubmitIncident } from './useSafetyIncidents';
import type { IncidentFormData, SubmitIncidentRequest } from '../types/safety.types';

/**
 * Enhanced incident submission hook with form integration
 * Handles form data conversion and submission state
 */
export function useSubmitIncidentForm() {
  const [submissionResult, setSubmissionResult] = useState<{
    referenceNumber?: string;
    trackingUrl?: string;
    submittedAt?: string;
  } | null>(null);
  
  const submitMutation = useSubmitIncident();
  
  /**
   * Convert form data to API request format
   */
  const convertFormDataToRequest = useCallback((formData: IncidentFormData, reporterId?: string): SubmitIncidentRequest => {
    // Combine date and time into ISO string
    const incidentDateTime = new Date(`${formData.incidentDate}T${formData.incidentTime}`);
    
    return {
      reporterId: formData.isAnonymous ? undefined : reporterId,
      severity: formData.severity,
      incidentDate: incidentDateTime.toISOString(),
      location: formData.location,
      description: formData.description,
      involvedParties: formData.involvedParties || undefined,
      witnesses: formData.witnesses || undefined,
      isAnonymous: formData.isAnonymous,
      requestFollowUp: formData.requestFollowUp,
      contactEmail: (!formData.isAnonymous && formData.contactEmail) ? formData.contactEmail : undefined,
      contactPhone: (!formData.isAnonymous && formData.contactPhone) ? formData.contactPhone : undefined
    };
  }, []);
  
  /**
   * Submit incident with form data
   */
  const submitIncident = useCallback(async (formData: IncidentFormData, reporterId?: string) => {
    try {
      const request = convertFormDataToRequest(formData, reporterId);
      const result = await submitMutation.mutateAsync(request);
      
      setSubmissionResult({
        referenceNumber: result.referenceNumber,
        trackingUrl: result.trackingUrl,
        submittedAt: result.submittedAt
      });
      
      return result;
    } catch (error) {
      console.error('Form submission failed:', error);
      throw error;
    }
  }, [convertFormDataToRequest, submitMutation]);
  
  /**
   * Reset submission state
   */
  const resetSubmission = useCallback(() => {
    setSubmissionResult(null);
    submitMutation.reset();
  }, [submitMutation]);
  
  return {
    submitIncident,
    resetSubmission,
    submissionResult,
    isSubmitting: submitMutation.isPending,
    error: submitMutation.error,
    isSuccess: submitMutation.isSuccess && !!submissionResult
  };
}