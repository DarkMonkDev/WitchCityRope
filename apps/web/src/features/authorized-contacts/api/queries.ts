/**
 * Authorized Contacts - TanStack Query hooks
 *
 * Query hooks for fetching authorized contact relationships and searching
 * for members by scene name. Follows project patterns from events/api/queries.ts
 * and members/api/queries.ts.
 *
 * All queries use the shared apiClient (Axios instance with CSRF, auth cookies,
 * and RFC 9457 error extraction).
 */
import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import type {
  AuthorizedContactsListDto,
  PrincipalContactDto,
  UserSearchResultDto,
} from '../types/authorizedContact.types'

/**
 * Query key factory for authorized-contacts domain.
 * Follows the pattern from api/queryKeys.ts (e.g., queryKeys.events()).
 */
export const authorizedContactKeys = {
  all: ['authorized-contacts'] as const,
  list: () => [...authorizedContactKeys.all, 'list'] as const,
  principals: (eventId?: string) =>
    eventId
      ? ([...authorizedContactKeys.all, 'principals', eventId] as const)
      : ([...authorizedContactKeys.all, 'principals'] as const),
  search: (query: string) => [...authorizedContactKeys.all, 'search', query] as const,
}

/**
 * Fetch all authorized contacts for the current user.
 * Returns both delegates (people who can act on my behalf)
 * and principals (people I can act for).
 *
 * GET /api/authorized-contacts
 */
export function useAuthorizedContacts() {
  return useQuery<AuthorizedContactsListDto>({
    queryKey: authorizedContactKeys.list(),
    queryFn: async (): Promise<AuthorizedContactsListDto> => {
      const response = await apiClient.get('/api/authorized-contacts')
      return response.data
    },
    staleTime: 5 * 60 * 1000, // 5 minutes - relationships don't change often
  })
}

/**
 * Fetch principal contacts (people who authorized the current user).
 * Used in checkout/RSVP flows to show "buy for" dropdown.
 * Optionally filtered by eventId for vetting-gated events.
 *
 * GET /api/authorized-contacts/principals?eventId={eventId}
 */
export function usePrincipalContacts(eventId?: string) {
  return useQuery<PrincipalContactDto[]>({
    queryKey: authorizedContactKeys.principals(eventId),
    queryFn: async (): Promise<PrincipalContactDto[]> => {
      const params: Record<string, string> = {}
      if (eventId) {
        params.eventId = eventId
      }
      const response = await apiClient.get('/api/authorized-contacts/principals', { params })
      return response.data
    },
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Search for members by scene name for the "Add Contact" autocomplete.
 * Debouncing is handled by the component (300ms); this query only fires
 * when the search string is 2+ characters.
 *
 * Results show scene names only (AD-009 privacy compliance).
 *
 * GET /api/members/search?sceneName={query}
 */
export function useContactSearch(query: string) {
  return useQuery<UserSearchResultDto[]>({
    queryKey: authorizedContactKeys.search(query),
    queryFn: async (): Promise<UserSearchResultDto[]> => {
      const response = await apiClient.get('/api/members/search', {
        params: { sceneName: query },
      })
      return response.data
    },
    // Only fire when query is 2+ characters
    enabled: query.length >= 2,
    // Short stale time since search results are ephemeral
    staleTime: 30 * 1000, // 30 seconds
    // Don't refetch on window focus for search queries
    refetchOnWindowFocus: false,
  })
}
