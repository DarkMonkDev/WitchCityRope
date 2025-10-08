/**
 * Vetting Status Types for Conditional Menu Visibility
 * Matches backend VettingStatus enum (0-9)
 */

import type { components } from '@witchcityrope/shared-types';

/**
 * Vetting status enum matching backend exactly
 * Based on VettingApplication.cs enum values
 * Interview completion moves directly to FinalReview (no InterviewCompleted intermediate state)
 */
export enum VettingStatus {
  UnderReview = 0,
  InterviewApproved = 1,
  FinalReview = 2,
  Approved = 3,
  Denied = 4,
  OnHold = 5,
  Withdrawn = 6
}

/**
 * String representation of vetting statuses
 * Backend returns status as string in API responses
 */
export type VettingStatusString =
  | 'UnderReview'
  | 'InterviewApproved'
  | 'FinalReview'
  | 'Approved'
  | 'Denied'
  | 'OnHold'
  | 'Withdrawn';

/**
 * Application status information from API
 * Matches ApplicationStatusInfo C# model from generated types
 */
export interface ApplicationStatusInfo {
  applicationId: string;
  applicationNumber: string | null;
  status: VettingStatusString;
  statusDescription: string | null;
  submittedAt: string; // ISO 8601 timestamp
  lastUpdated: string; // ISO 8601 timestamp
  nextSteps: string | null;
  estimatedDaysRemaining: number | null;
}

/**
 * My application status response from GET /api/vetting/status
 * Matches MyApplicationStatusResponse C# model from generated types
 */
export interface MyApplicationStatusResponse {
  hasApplication: boolean;
  application: ApplicationStatusInfo | null;
}

/**
 * API response wrapper
 * Matches ApiResponse<T> pattern used by backend
 */
export interface VettingStatusApiResponse {
  success: boolean;
  data: MyApplicationStatusResponse | null;
  error?: string | null;
  details?: string | null;
  message?: string | null;
  timestamp: string; // ISO 8601 timestamp
}

/**
 * Menu visibility decision result
 * Used by useMenuVisibility hook
 */
export interface MenuVisibilityResult {
  shouldShow: boolean;
  reason: string; // For debugging/logging
}

/**
 * Status box props for VettingStatusBox component
 */
export interface StatusBoxProps {
  status: VettingStatusString;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}

/**
 * Helper to convert status string to enum value
 */
export const statusStringToEnum = (status: VettingStatusString): VettingStatus => {
  const mapping: Record<VettingStatusString, VettingStatus> = {
    UnderReview: VettingStatus.UnderReview,
    InterviewApproved: VettingStatus.InterviewApproved,
    FinalReview: VettingStatus.FinalReview,
    Approved: VettingStatus.Approved,
    Denied: VettingStatus.Denied,
    OnHold: VettingStatus.OnHold,
    Withdrawn: VettingStatus.Withdrawn
  };
  return mapping[status];
};

/**
 * Helper to convert enum value to status string
 */
export const statusEnumToString = (status: VettingStatus): VettingStatusString => {
  const mapping: Record<VettingStatus, VettingStatusString> = {
    [VettingStatus.UnderReview]: 'UnderReview',
    [VettingStatus.InterviewApproved]: 'InterviewApproved',
    [VettingStatus.FinalReview]: 'FinalReview',
    [VettingStatus.Approved]: 'Approved',
    [VettingStatus.Denied]: 'Denied',
    [VettingStatus.OnHold]: 'OnHold',
    [VettingStatus.Withdrawn]: 'Withdrawn'
  };
  return mapping[status];
};

/**
 * Check if status should hide "How to Join" menu item
 * Business rule: Hide for OnHold (6), Approved (7), Denied (8)
 */
export const shouldHideMenuForStatus = (status: VettingStatusString): boolean => {
  const hideStatuses: VettingStatusString[] = ['OnHold', 'Approved', 'Denied'];
  return hideStatuses.includes(status);
};
