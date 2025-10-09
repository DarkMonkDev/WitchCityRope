import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { dashboardService } from '../services/dashboardService';
import { useUser } from '../stores/authStore';
import type { UpdateProfileDto, ChangePasswordDto, UserEventDto, VettingStatusDto, UserProfileDto } from '../types/dashboard.types';

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

  return useQuery<UserEventDto[], Error>({
    queryKey: ['user-events', user?.id, includePast],
    queryFn: () => dashboardService.getUserEvents(user!.id, includePast),
    enabled: !!user?.id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to fetch user's vetting status
 * Returns null if user is fully vetted (no alert needed)
 */
export const useVettingStatus = () => {
  const user = useUser();

  return useQuery<VettingStatusDto | null, Error>({
    queryKey: ['vetting-status', user?.id],
    queryFn: () => dashboardService.getVettingStatus(user!.id),
    enabled: !!user?.id,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Hook to fetch user profile data
 */
export const useProfile = () => {
  const user = useUser();

  return useQuery<UserProfileDto, Error>({
    queryKey: ['user-profile', user?.id],
    queryFn: () => dashboardService.getProfile(user!.id),
    enabled: !!user?.id,
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
    mutationFn: (data: UpdateProfileDto) => dashboardService.updateProfile(user!.id, data),
    onSuccess: () => {
      // Invalidate profile cache to refetch fresh data
      queryClient.invalidateQueries({ queryKey: ['user-profile'] });
    },
  });
};

/**
 * Hook to change user password
 */
export const useChangePassword = () => {
  const user = useUser();

  return useMutation({
    mutationFn: (data: ChangePasswordDto) => dashboardService.changePassword(user!.id, data),
  });
};
