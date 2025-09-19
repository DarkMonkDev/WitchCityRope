import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';

export interface TeacherOption {
  id: string;
  name: string;
  email: string;
}

// Fallback teacher data for when API fails
const FALLBACK_TEACHERS: TeacherOption[] = [
  { id: 'teacher-1', name: 'River Moon', email: 'river@example.com' },
  { id: 'teacher-2', name: 'Sage Blackthorne', email: 'sage@example.com' },
  { id: 'teacher-3', name: 'Phoenix Rose', email: 'phoenix@example.com' },
  { id: 'teacher-4', name: 'Willow Craft', email: 'willow@example.com' },
  { id: 'teacher-5', name: 'Raven Night', email: 'raven@example.com' },
];

/**
 * React Query hook to fetch teachers for dropdown options
 * Falls back to mock data if API fails
 */
export function useTeachers(enabled = true) {
  return useQuery({
    queryKey: ['users', 'by-role', 'Teacher'],
    queryFn: async (): Promise<TeacherOption[]> => {
      try {
        console.log('🔍 [DEBUG] Attempting to fetch teachers from API...');
        const response = await apiClient.get<TeacherOption[]>('/api/users/by-role/Teacher');
        console.log('🔍 [DEBUG] Teachers API response:', {
          status: response.status,
          data: response.data,
          dataLength: response.data?.length
        });

        // If we get data from API, use it
        if (response.data && response.data.length > 0) {
          console.log('🔍 [DEBUG] Using real teachers from API');
          return response.data;
        }

        // If API returns empty array, fall back to mock data
        console.log('🔍 [DEBUG] API returned empty teachers, using fallback data');
        return FALLBACK_TEACHERS;
      } catch (error) {
        console.warn('🔍 [DEBUG] Teachers API failed, using fallback data:', error);
        return FALLBACK_TEACHERS;
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
  const formatted = teachers.map(teacher => ({
    value: teacher.id,
    label: teacher.name
  }));
  console.log('🔍 [DEBUG] Formatted teachers for MultiSelect:', formatted);
  return formatted;
}