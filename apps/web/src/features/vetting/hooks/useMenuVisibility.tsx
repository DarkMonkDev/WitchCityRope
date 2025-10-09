/**
 * useMenuVisibility Hook
 * Determines whether "How to Join" menu should be visible based on vetting status
 *
 * Business Logic:
 * - Show menu: User not authenticated OR no application OR active application statuses
 * - Hide menu: User has application with status OnHold, Approved, or Denied
 *
 * Fail-open approach: If status fetch fails or is loading, show the menu
 */
import { useVettingStatus } from './useVettingStatus';
import { useIsAuthenticated } from '../../../stores/authStore';
import { shouldHideMenuForStatus } from '../types/vettingStatus';
import type { MenuVisibilityResult } from '../types/vettingStatus';

/**
 * Hook to determine "How to Join" menu visibility
 *
 * @returns MenuVisibilityResult with shouldShow boolean and reason string
 *
 * @example
 * ```typescript
 * const { shouldShow, reason } = useMenuVisibility();
 *
 * {shouldShow && (
 *   <NavLink to="/how-to-join">How to Join</NavLink>
 * )}
 * ```
 */
export const useMenuVisibility = (): MenuVisibilityResult => {
  const isAuthenticated = useIsAuthenticated();
  const { data: statusData, isLoading, error } = useVettingStatus();

  // Rule 1: Unauthenticated users should always see "How to Join"
  if (!isAuthenticated) {
    return {
      shouldShow: true,
      reason: 'User not authenticated - show join option'
    };
  }

  // Rule 2: Loading state - fail-open (show menu)
  if (isLoading) {
    return {
      shouldShow: true,
      reason: 'Loading - show menu (fail-open)'
    };
  }

  // Rule 3: Error state - fail-open (show menu)
  if (error) {
    return {
      shouldShow: true,
      reason: 'Error fetching status - show menu (fail-open)'
    };
  }

  // Rule 4: No application data or user has no application
  if (!statusData || !statusData.hasApplication || !statusData.application) {
    return {
      shouldShow: true,
      reason: 'No application exists - show join option'
    };
  }

  // Rule 5: Check application status against hide rules
  const status = statusData.application.status;

  // Type guard: Ensure status is a valid VettingStatus string
  if (!status) {
    return {
      shouldShow: true,
      reason: 'No status on application - show menu (fail-open)'
    };
  }

  const shouldHide = shouldHideMenuForStatus(status as import('../types/vettingStatus').VettingStatus);

  if (shouldHide) {
    return {
      shouldShow: false,
      reason: `Status ${status} requires menu to be hidden`
    };
  }

  // Rule 6: All other statuses show the menu
  return {
    shouldShow: true,
    reason: `Status ${status} allows menu visibility`
  };
};
