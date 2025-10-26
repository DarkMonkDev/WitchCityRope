import { useQuery } from '@tanstack/react-query';
import { getEventVolunteerPositions, getUserVolunteerShifts } from '../api/volunteerApi';

export const useVolunteerPositions = (eventId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: ['volunteerPositions', eventId],
    queryFn: () => getEventVolunteerPositions(eventId),
    enabled: enabled && !!eventId,
    select: (data) => data.data || []
  });
};

export const useUserVolunteerShifts = (enabled: boolean = true) => {
  return useQuery({
    queryKey: ['userVolunteerShifts'],
    queryFn: getUserVolunteerShifts,
    enabled,
    select: (data) => data.data || [],
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true
  });
};
