import { useQuery } from '@tanstack/react-query';
import { getEventVolunteerPositions } from '../api/volunteerApi';

export const useVolunteerPositions = (eventId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: ['volunteerPositions', eventId],
    queryFn: () => getEventVolunteerPositions(eventId),
    enabled: enabled && !!eventId,
    select: (data) => data.data || []
  });
};
