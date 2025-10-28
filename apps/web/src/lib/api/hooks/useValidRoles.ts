import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client';
import type { components } from '@witchcityrope/shared-types';

/**
 * Valid Roles Response
 * @generated from C# ValidRolesResponse via NSwag
 */
export type ValidRolesResponse = components['schemas']['ValidRolesResponse'];

/**
 * React Query hook to fetch valid user roles from metadata endpoint
 * Returns roles that can be assigned to users in the system
 *
 * API Source of Truth: /api/metadata/valid-roles
 * Backend Constants: UserRoleConstants.ValidRoles
 */
export function useValidRoles() {
  return useQuery<string[]>({
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
 * Matches backend FormatRoleDisplayName logic in UserEndpoints.cs
 *
 * Note: For a more robust solution with descriptions, consider using
 * the /api/users/roles/available endpoint which provides UserRoleDto[]
 * with display names and descriptions from the backend.
 */
export function formatRolesForSelect(roles: string[]) {
  const formatLabel = (role: string): string => {
    switch (role) {
      case 'SafetyTeam':
        return 'Safety Team';
      case 'CheckInStaff':
        return 'Check-In Staff';
      case 'EventOrganizer':
        return 'Event Organizer';
      default:
        return role;
    }
  };

  return roles.map(role => ({
    value: role,
    label: formatLabel(role),
  }));
}
