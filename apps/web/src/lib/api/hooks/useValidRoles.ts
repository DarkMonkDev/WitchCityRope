import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';

/**
 * Valid roles response from metadata endpoint
 * Using generated types from @witchcityrope/shared-types
 */
export interface ValidRolesResponse {
  roles?: string[];
}

/**
 * React Query hook to fetch valid user roles from metadata endpoint
 * Returns roles that can be assigned to users in the system
 *
 * API Source of Truth: /api/metadata/valid-roles
 * Backend Constants: UserRoleConstants.ValidRoles
 */
export function useValidRoles() {
  return useQuery({
    queryKey: ['metadata', 'valid-roles'],
    queryFn: async (): Promise<string[]> => {
      const response = await apiClient.get<ValidRolesResponse>('/api/metadata/valid-roles');
      return response.data.roles || [];
    },
    staleTime: 60 * 60 * 1000, // 1 hour - roles rarely change
    gcTime: 24 * 60 * 60 * 1000, // 24 hours
    refetchOnWindowFocus: false,
  });
}

/**
 * Convert roles array to Mantine Select/MultiSelect data format
 */
export function formatRolesForSelect(roles: string[]) {
  return roles.map(role => ({
    value: role,
    label: role === 'SafetyTeam' ? 'Safety Team' : role,
  }));
}
