import { apiClient } from '@/lib/api/client';
import type { components } from '@witchcityrope/shared-types';
import type { VolunteerPosition, VolunteerSignup, VolunteerSignupRequest } from '../types/volunteer.types';

// Type from generated API types
type UserVolunteerShiftDto = components['schemas']['UserVolunteerShiftDto'];

/**
 * Get volunteer positions for an event
 */
export const getEventVolunteerPositions = async (
  eventId: string
): Promise<VolunteerPosition[]> => {
  const { data } = await apiClient.get<VolunteerPosition[]>(
    `/api/events/${eventId}/volunteer-positions`
  );
  return data;
};

/**
 * Sign up for a volunteer position
 */
export const signupForVolunteerPosition = async (
  positionId: string,
  request: VolunteerSignupRequest
): Promise<VolunteerSignup> => {
  const { data } = await apiClient.post<VolunteerSignup>(
    `/api/volunteer-positions/${positionId}/signup`,
    request
  );
  return data;
};

/**
 * Get current user's volunteer shifts (upcoming shifts with event details)
 * Uses the new backend endpoint that returns UserVolunteerShiftDto with all required fields
 */
export const getUserVolunteerShifts = async (): Promise<UserVolunteerShiftDto[]> => {
  const { data } = await apiClient.get<UserVolunteerShiftDto[]>(
    '/api/user/volunteer-shifts'
  );
  return data;
};
