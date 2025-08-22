// types/api.types.ts
// Use generated types from @witchcityrope/shared-types
import type { UserDto } from '@witchcityrope/shared-types'

export type { 
  UserDto, 
  UserRole, 
  EventDto,
  EventType,
  EventStatus,
  EventListResponse
} from '@witchcityrope/shared-types'

export interface ApiError {
  message: string
  status: number
  code?: string
  details?: Record<string, string[]>
}

// Legacy Event interface - migrate to use EventDto from generated types
export interface Event {
  id: string
  title: string
  description: string
  startDate: string
  endDate: string
  maxAttendees: number
  currentAttendees: number
  isRegistrationOpen: boolean
  instructorId: string
  instructor?: UserDto  // Made optional for compatibility
  attendees?: UserDto[] // Made optional for compatibility
}

export interface EventRegistration {
  id: string
  eventId: string
  userId: string
  registeredAt: string
  status: RegistrationStatus
}

export type RegistrationStatus = 'Confirmed' | 'Cancelled' | 'Waitlisted'

export interface PaginatedResponse<T> {
  data: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}

export interface CreateEventData {
  title: string
  description: string
  startDate: string
  endDate: string
  maxAttendees: number
  location?: string
}

export interface UpdateEventData {
  title?: string
  description?: string
  startDate?: string
  endDate?: string
  maxAttendees?: number
  location?: string
}

export interface EventFilters {
  search?: string
  startDate?: string
  endDate?: string
  category?: string
}