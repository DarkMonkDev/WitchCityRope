import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { vettingKeys } from './useVettingApplications';

/**
 * Hook for soft-deleting a vetting application.
 * Invalidates all vetting-related query caches on success
 * so the applications list and stats reflect the deletion.
 *
 * Follows the same invalidation pattern as useApproveApplication and
 * useSubmitReviewDecision, including user-profile and vetting-status
 * cache invalidation since the user's VettingStatus is reset on delete.
 *
 * @param onSuccess - Callback fired after successful deletion (e.g., navigate back to list)
 */
export function useDeleteApplication(onSuccess?: () => void) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (applicationId: string) =>
      vettingAdminApi.deleteApplication(applicationId),
    onSuccess: () => {
      // Invalidate all vetting application queries so the list page refreshes without the deleted app
      queryClient.invalidateQueries({
        queryKey: vettingKeys.applications(),
      });

      // CRITICAL: Invalidate user's profile and vetting status cache
      // When an application is deleted, the user's VettingStatus is reset so they can reapply.
      // We invalidate ALL user-profile and vetting-status queries since we don't have userId in this context.
      queryClient.invalidateQueries({
        queryKey: ['user-profile'],
      });
      queryClient.invalidateQueries({
        queryKey: ['vetting-status'],
      });

      // Call the optional success callback (e.g., navigate back to applications list)
      onSuccess?.();
    },
    onError: (error: any) => {
      // Show error notification matching the pattern used by other vetting hooks
      notifications.show({
        title: 'Error Deleting Application',
        message: error?.detail || error?.message || 'Failed to delete application',
        color: 'red',
      });
    },
  });
}
