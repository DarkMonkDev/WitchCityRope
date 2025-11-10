import { apiClient } from '../lib/api/client';

export interface PublicSettings {
  eventTimeZone: string;
  preStartBufferMinutes: string;
}

export const getPublicSettings = async (): Promise<PublicSettings> => {
  const response = await apiClient.get<PublicSettings>('/api/settings/public');
  return response.data;
};
