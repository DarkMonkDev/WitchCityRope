// types/api.types.ts
// TODO: Use generated types from @witchcityrope/shared-types when package is available
// Temporarily using inline types to fix import failures

// UserDto type definition (matches API)
export interface UserDto {
  id?: string;
  email?: string;
  sceneName?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  roles?: string[];
  isActive?: boolean;
  createdAt?: string;
  updatedAt?: string;
  lastLoginAt?: string | null;
}

export type UserRole = 'Administrator' | 'Teacher' | 'VettedMember' | 'GeneralMember' | 'Guest';

export interface EventDto {
  id?: string;
  title?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  registrationCount?: number;
}

export type EventType = 'Workshop' | 'Social' | 'Performance' | 'Other';
export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';

export interface EventListResponse {
  events: EventDto[];
  totalCount: number;
}

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
  capacity: number
  registrationCount?: number
  isRegistrationOpen: boolean
  instructorId: string
  instructor?: UserDto  // Made optional for compatibility
  attendees?: UserDto[] // Made optional for compatibility
  eventType?: 'class' | 'social' // Event type for determining UI logic
  status?: 'Draft' | 'Published' | 'Cancelled' | 'Completed' // Event status
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
  capacity: number
  location?: string
}

export interface UpdateEventData {
  title?: string
  description?: string
  startDate?: string
  endDate?: string
  capacity?: number
  location?: string
}

export interface EventFilters {
  search?: string
  startDate?: string
  endDate?: string
  category?: string
}