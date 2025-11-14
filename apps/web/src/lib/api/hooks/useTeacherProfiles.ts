import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';
import type { components } from '@witchcityrope/shared-types';

type UserProfileDto = components['schemas']['UserProfileDto'];

/**
 * Fetch user profiles for multiple teacher IDs
 * Returns array of teacher profiles with bio information
 * Pattern B: Direct DTO response
 */
export function useTeacherProfiles(teacherIds: string[] | undefined) {
  return useQuery({
    queryKey: ['users', 'profiles', teacherIds],
    queryFn: async (): Promise<UserProfileDto[]> => {
      if (!teacherIds || teacherIds.length === 0) {
        return [];
      }

      const results = await Promise.all(
        teacherIds.map(async (teacherId) => {
          try {
            const { data } = await apiClient.get<UserProfileDto>(
              `/api/public/users/${teacherId}/profile`
            );
            return data || null;
          } catch (error) {
            console.error(`Failed to fetch teacher profile for ${teacherId}:`, error);
            return null;
          }
        })
      );

      // Filter out null results
      return results.filter((teacher): teacher is UserProfileDto => teacher !== null);
    },
    enabled: !!(teacherIds && teacherIds.length > 0),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
}
