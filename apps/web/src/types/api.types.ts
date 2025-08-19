// types/api.types.ts
export interface ApiError {
  message: string
  status: number
  code?: string
  details?: Record<string, string[]>
}

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  sceneName: string
  role: UserRole
  isVetted: boolean
  createdAt: string
  updatedAt: string
}

export type UserRole = 'Admin' | 'Teacher' | 'VettedMember' | 'GeneralMember' | 'Guest'

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
  instructor: User
  attendees: User[]
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