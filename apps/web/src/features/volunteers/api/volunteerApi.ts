import { apiClient } from '@/lib/api/client';
import type { ApiResponse } from '@/lib/api/types/api.types';
import type { components } from '@witchcityrope/shared-types';
import type { VolunteerPosition, VolunteerSignup, VolunteerSignupRequest } from '../types/volunteer.types';

// Type from generated API types
type UserVolunteerShiftDto = components['schemas']['UserVolunteerShiftDto'];

/**
 * Get volunteer positions for an event
 */
export const getEventVolunteerPositions = async (
  eventId: string
): Promise<ApiResponse<VolunteerPosition[]>> => {
  const response = await apiClient.get<ApiResponse<VolunteerPosition[]>>(
    `/api/events/${eventId}/volunteer-positions`
  );
  return response.data;
};

/**
 * Sign up for a volunteer position
 */
export const signupForVolunteerPosition = async (
  positionId: string,
  request: VolunteerSignupRequest
): Promise<ApiResponse<VolunteerSignup>> => {
  const response = await apiClient.post<ApiResponse<VolunteerSignup>>(
    `/api/volunteer-positions/${positionId}/signup`,
    request
  );
  return response.data;
};

/**
 * Get current user's volunteer shifts (upcoming shifts with event details)
 * Uses the new backend endpoint that returns UserVolunteerShiftDto with all required fields
 */
export const getUserVolunteerShifts = async (): Promise<ApiResponse<UserVolunteerShiftDto[]>> => {
  const response = await apiClient.get<ApiResponse<UserVolunteerShiftDto[]>>(
    '/api/user/volunteer-shifts'
  );
  return response.data;
};
