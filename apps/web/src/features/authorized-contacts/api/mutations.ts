/**
 * Authorized Contacts - TanStack Query mutation hooks
 *
 * Mutations for adding and removing authorized contact relationships.
 * Follows project patterns from events/api/mutations.ts and members/api/mutations.ts.
 *
 * CSRF tokens are automatically attached by the apiClient request interceptor
 * for all POST/PUT/DELETE requests (see lib/api/client.ts).
 */
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import { authorizedContactKeys } from './queries'
import type {
  AddAuthorizedContactRequest,
  AuthorizedContactDto,
} from '../types/authorizedContact.types'

/**
 * Add a new authorized contact (delegate).
 * The current user (principal) authorizes the target user (delegate)
 * to purchase tickets and RSVP on their behalf.
 *
 * POST /api/authorized-contacts
 * Body: { targetUserId: string }
 *
 * On success: invalidates the authorized contacts list query so the UI
 * reflects the new relationship immediately.
 */
export function useAddAuthorizedContact() {
  const queryClient = useQueryClient()

  return useMutation<AuthorizedContactDto, Error, AddAuthorizedContactRequest>({
    mutationFn: async (data: AddAuthorizedContactRequest): Promise<AuthorizedContactDto> => {
      const response = await apiClient.post('/api/authorized-contacts', data)
      return response.data
    },
    onSuccess: () => {
      // Invalidate the contacts list so the new delegate appears immediately
      queryClient.invalidateQueries({ queryKey: authorizedContactKeys.list() })
    },
    onError: (error: Error) => {
      console.error('Failed to add authorized contact:', error)
    },
  })
}

/**
 * Remove an authorized contact relationship.
 * Only the principal (the person who granted authority) can remove.
 *
 * DELETE /api/authorized-contacts/{contactId}
 *
 * On success: invalidates the authorized contacts list query so the UI
 * reflects the removal immediately.
 */
export function useRemoveAuthorizedContact() {
  const queryClient = useQueryClient()

  return useMutation<void, Error, string>({
    mutationFn: async (contactId: string): Promise<void> => {
      await apiClient.delete(`/api/authorized-contacts/${contactId}`)
    },
    onSuccess: () => {
      // Invalidate the contacts list so the removed delegate disappears
      queryClient.invalidateQueries({ queryKey: authorizedContactKeys.list() })
    },
    onError: (error: Error) => {
      console.error('Failed to remove authorized contact:', error)
    },
  })
}
