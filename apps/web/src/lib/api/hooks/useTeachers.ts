import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';

export interface TeacherOption {
  id: string;
  name: string;
  email: string;
}

/**
 * React Query hook to fetch teachers for dropdown options
 * Returns actual teachers from database - no mock data
 */
export function useTeachers(enabled = true) {
  return useQuery({
    queryKey: ['users', 'by-role', 'Teacher'],
    queryFn: async (): Promise<TeacherOption[]> => {
      try {
        const response = await apiClient.get<TeacherOption[]>('/api/users/by-role/Teacher');
        // Return whatever the API provides (could be empty array)
        return response.data || [];
      } catch (error) {
        console.error('Failed to fetch teachers:', error);
        // Return empty array on error - no fake data
        return [];
      }
    },
    enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false
  });
}

/**
 * Convert teachers to Mantine MultiSelect data format
 */
export function formatTeachersForMultiSelect(teachers: TeacherOption[]) {
  return teachers.map(teacher => ({
    value: teacher.id,
    label: teacher.name
  }));
}