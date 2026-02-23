import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboardService';
import { useUser } from '../stores/authStore';
import type { UpdateProfileDto, ChangePasswordDto, UserEventDto, VettingStatusDto, UserProfileDto } from '../types/dashboard.types';
import { debugLog, debugError } from '../utils/debug';

/**
 * TanStack Query hooks for dashboard functionality
 * All hooks use httpOnly cookie authentication automatically
 */

/**
 * Hook to fetch user's registered events
 * @param includePast - Include past events (default: false)
 */
export const useUserEvents = (includePast = false) => {
  const user = useUser();

  debugLog('🔍 useUserEvents - user:', user);
  debugLog('🔍 useUserEvents - user.id:', user?.id);
  debugLog('🔍 useUserEvents - includePast:', includePast);

  return useQuery<UserEventDto[], Error>({
    queryKey: ['user-events', user?.id, includePast],
    queryFn: async () => {
      debugLog('🔍 Calling dashboardService.getUserEvents with userId:', user!.id);
      try {
        const result = await dashboardService.getUserEvents(user!.id!, includePast);
        debugLog('✅ getUserEvents result:', result);
        return result;
      } catch (error) {
        debugError('❌ getUserEvents error:', error);
        throw error;
      }
    },
    enabled: !!user?.id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch user's vetting status
 * Returns null if user is fully vetted (no alert needed)
 * CRITICAL: Refetches on window focus to show updated vetting status from admin actions
 */
export const useVettingStatus = () => {
  const user = useUser();

  return useQuery<VettingStatusDto | null, Error>({
    queryKey: ['vetting-status', user?.id],
    queryFn: () => dashboardService.getVettingStatus(user!.id!),
    enabled: !!user?.id,
    staleTime: 1 * 60 * 1000, // 1 minute (reduced from 10 minutes)
    refetchOnWindowFocus: true, // Refetch when user returns to tab (CRITICAL for admin updates)
  });
};

/**
 * Hook to fetch user profile data
 * CRITICAL: Refetches on window focus and mount to ensure fresh vetting status
 */
export const useProfile = () => {
  const user = useUser();

  return useQuery<UserProfileDto, Error>({
    queryKey: ['user-profile', user?.id],
    queryFn: () => dashboardService.getProfile(user!.id!),
    enabled: !!user?.id,
    staleTime: 0, // Always consider stale - ensures fresh data on refetch
    refetchOnWindowFocus: true, // Refetch when user returns to tab (CRITICAL for admin updates)
    refetchOnMount: true, // Refetch when component mounts (CRITICAL after logout/login)
  });
};

/**
 * Hook to update user profile
 * Automatically invalidates profile cache on success
 */
export const useUpdateProfile = () => {
  const user = useUser();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateProfileDto) => dashboardService.updateProfile(user!.id!, data),
    onSuccess: () => {
      // Invalidate profile cache to refetch fresh data
      // CRITICAL: Must include user.id in queryKey to match the query key used in useProfile
      queryClient.invalidateQueries({ queryKey: ['user-profile', user?.id] });
    },
  });
};

/**
 * Hook to change user password
 */
export const useChangePassword = () => {
  const user = useUser();

  return useMutation({
    mutationFn: (data: ChangePasswordDto) => dashboardService.changePassword(user!.id!, data),
  });
};
