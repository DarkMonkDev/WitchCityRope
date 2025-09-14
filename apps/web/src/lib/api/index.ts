// Export all API utilities for easy importing
export { apiClient } from './client'
export { queryClient } from './queryClient'
export { wsManager } from './websocket'

// Export hooks
export * from './hooks/useAuth'
export * from './hooks/useEvents'
export * from './hooks/useMembers'
export * from './hooks/useOptimistic'

// Export safety hooks
export * from '../../features/safety/hooks/useSafetyIncidents'
export * from '../../features/safety/hooks/useSubmitIncident'

// Export types
export * from './types/api.types'
export type { 
  LoginRequest, 
  RegisterCredentials, 
  LoginResponse, 
  AuthQueryKey 
} from './types/auth.types'
export * from './types/events.types'
export type { 
  UpdateMemberDto, 
  StatusUpdateDto, 
  MemberFilters, 
  MemberQueryKey 
} from './types/members.types'

// Export safety types
export * from '../../features/safety/types/safety.types'

// Export utilities
export * from './utils/cache'
export * from './utils/errors'
export * from './utils/retry'