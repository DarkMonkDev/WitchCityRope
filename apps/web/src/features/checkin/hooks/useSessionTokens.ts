import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { sessionTokenApi } from '../api/sessionTokenApi';
import type { GenerateTokenRequest, RevokeTokenRequest } from '../api/sessionTokenApi';

/**
 * React Query hooks for session token management
 *
 * These hooks provide a clean interface for managing check-in session tokens
 * with automatic cache invalidation and error handling.
 */

/**
 * Hook to generate a new check-in session token
 *
 * Automatically invalidates active tokens query for the event after success.
 *
 * @returns Mutation object with mutate function and loading/error states
 *
 * @example
 * const generateToken = useGenerateSessionToken();
 * generateToken.mutate({ eventId: '123', expiresInHours: 24 });
 */
export const useGenerateSessionToken = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: GenerateTokenRequest) => sessionTokenApi.generateToken(request),
    onSuccess: (_, variables) => {
      // Invalidate active tokens query for this event
      queryClient.invalidateQueries({
        queryKey: ['sessionTokens', 'event', variables.eventId],
      });
    },
  });
};

/**
 * Hook to revoke an existing session token
 *
 * Automatically invalidates all active token queries after success.
 *
 * @returns Mutation object with mutate function and loading/error states
 *
 * @example
 * const revokeToken = useRevokeSessionToken();
 * revokeToken.mutate({ token: 'abc123...' });
 */
export const useRevokeSessionToken = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RevokeTokenRequest) => sessionTokenApi.revokeToken(request),
    onSuccess: () => {
      // Invalidate all active token queries (we don't know which event this token belonged to)
      queryClient.invalidateQueries({
        queryKey: ['sessionTokens'],
      });
    },
  });
};

/**
 * Hook to fetch active session tokens for an event
 *
 * @param eventId - Event ID to fetch tokens for
 * @param enabled - Whether to run the query (default: true)
 * @returns Query object with token data and loading/error states
 *
 * @example
 * const { data: tokens, isLoading } = useActiveSessionTokens('event-123');
 */
export const useActiveSessionTokens = (eventId: string, enabled = true) => {
  return useQuery({
    queryKey: ['sessionTokens', 'event', eventId],
    queryFn: () => sessionTokenApi.listActiveTokens(eventId),
    enabled,
  });
};
