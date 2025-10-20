import { apiClient } from '@/lib/api/client';
import type { ApiResponse } from '@/types/api';
import type { VolunteerPosition, VolunteerSignup, VolunteerSignupRequest } from '../types/volunteer.types';

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
