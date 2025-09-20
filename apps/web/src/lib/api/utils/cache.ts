// Cache key factories for consistent query key management
import type { EventFilters } from '../types/events.types'
import type { MemberFilters } from '../types/members.types'

// Event query keys factory
export const eventKeys = {
  all: ['events'] as const,
  lists: () => [...eventKeys.all, 'list'] as const,
  list: (filters: EventFilters = {}) => [...eventKeys.lists(), filters] as const,
  infiniteList: (filters: Omit<EventFilters, 'page'> = {}) => [...eventKeys.lists(), 'infinite', filters] as const,
  details: () => [...eventKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventKeys.details(), id] as const,
  registrations: (eventId: string) => [...eventKeys.all, 'registrations', eventId] as const,
  participations: (eventId: string) => [...eventKeys.all, 'participations', eventId] as const,
}

// Member query keys factory
export const memberKeys = {
  all: ['members'] as const,
  lists: () => [...memberKeys.all, 'list'] as const,
  list: (filters: MemberFilters = {}) => [...memberKeys.lists(), filters] as const,
  infiniteList: (filters: Omit<MemberFilters, 'page'> = {}) => [...memberKeys.lists(), 'infinite', filters] as const,
  details: () => [...memberKeys.all, 'detail'] as const,
  detail: (id: string) => [...memberKeys.details(), id] as const,
}

// Auth query keys factory
export const authKeys = {
  all: ['auth'] as const,
  user: () => [...authKeys.all, 'user'] as const,
  profile: () => [...authKeys.all, 'profile'] as const,
  me: () => [...authKeys.all, 'me'] as const,
}

// Cache invalidation utilities
export const cacheUtils = {
  // Invalidate all related event queries when event data changes
  invalidateEvents: (queryClient: any, eventId?: string) => {
    queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
    if (eventId) {
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
    }
  },
  
  // Invalidate all related member queries when member data changes
  invalidateMembers: (queryClient: any, memberId?: string) => {
    queryClient.invalidateQueries({ queryKey: memberKeys.lists() })
    if (memberId) {
      queryClient.invalidateQueries({ queryKey: memberKeys.detail(memberId) })
    }
  },
  
  // Clear all auth-related cache on logout
  clearAuth: (queryClient: any) => {
    queryClient.removeQueries({ queryKey: authKeys.all })
    queryClient.clear() // Clear everything for security
  }
}