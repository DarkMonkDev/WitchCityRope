import { useQuery } from '@tanstack/react-query';
import { getEventVolunteerPositions, getUserVolunteerShifts } from '../api/volunteerApi';
import type { VolunteerPosition } from '../types/volunteer.types';
import type { components } from '@witchcityrope/shared-types';

type UserVolunteerShiftDto = components['schemas']['UserVolunteerShiftDto'];

export const useVolunteerPositions = (eventId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: ['volunteerPositions', eventId],
    queryFn: () => getEventVolunteerPositions(eventId),
    enabled: enabled && !!eventId,
    select: (data: VolunteerPosition[]) => data || []
  });
};

export const useUserVolunteerShifts = (enabled: boolean = true) => {
  return useQuery({
    queryKey: ['userVolunteerShifts'],
    queryFn: getUserVolunteerShifts,
    enabled,
    select: (data: UserVolunteerShiftDto[]) => data || [],
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true
  });
};
