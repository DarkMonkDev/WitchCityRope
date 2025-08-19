/* EXAMPLE FILE: How to use the generated API types and client */

import { 
  apiClient, 
  type UserDto, 
  type LoginRequest, 
  type LoginResponse,
  type EventDto,
  type CreateEventRequest,
  type EventListResponse,
  type ApiError,
  getErrorMessage
} from '@witchcityrope/shared-types';

// Example 1: Type-safe login using generated types
export async function loginExample(email: string, password: string): Promise<UserDto | null> {
  try {
    const loginRequest: LoginRequest = {
      email,
      password,
      rememberMe: false
    };

    const response: LoginResponse = await apiClient.login(loginRequest);
    
    if (response.success && response.user) {
      return response.user; // Fully typed as UserDto
    }
    
    return null;
  } catch (error) {
    const apiError = error as ApiError;
    console.error('Login failed:', getErrorMessage(apiError));
    throw error;
  }
}

// Example 2: Type-safe event creation
export async function createEventExample(eventData: {
  title: string;
  description?: string;
  startDateTime: Date;
  endDateTime: Date;
  capacity: number;
  eventType: 'Workshop' | 'Performance' | 'Social' | 'Private';
}): Promise<EventDto> {
  try {
    // Transform Date objects to ISO strings as expected by API
    const createRequest: CreateEventRequest = {
      title: eventData.title,
      description: eventData.description || null,
      startDateTime: eventData.startDateTime.toISOString(),
      endDateTime: eventData.endDateTime.toISOString(),
      capacity: eventData.capacity,
      eventType: eventData.eventType
    };

    // TypeScript will enforce the correct request/response types
    const newEvent: EventDto = await apiClient.createEvent(createRequest);
    
    return newEvent;
  } catch (error) {
    const apiError = error as ApiError;
    console.error('Event creation failed:', getErrorMessage(apiError));
    throw error;
  }
}

// Example 3: Type-safe event fetching with pagination
export async function getEventsExample(
  page: number = 1, 
  pageSize: number = 10
): Promise<{ events: EventDto[], totalCount: number }> {
  try {
    const response: EventListResponse = await apiClient.getEvents({ page, pageSize });
    
    return {
      events: response.events || [],
      totalCount: response.totalCount || 0
    };
  } catch (error) {
    const apiError = error as ApiError;
    console.error('Failed to fetch events:', getErrorMessage(apiError));
    throw error;
  }
}

// Example 4: Type guard usage
export function isValidUser(data: any): data is UserDto {
  return data && 
         typeof data.id === 'string' && 
         typeof data.email === 'string' &&
         data.email.includes('@');
}

// Example 5: Working with enums from generated types
export function getEventTypeDisplay(eventType: EventDto['eventType']): string {
  switch (eventType) {
    case 'Workshop':
      return '🎓 Workshop';
    case 'Performance':
      return '🎭 Performance';
    case 'Social':
      return '🎉 Social Event';
    case 'Private':
      return '🔒 Private Session';
    default:
      return '📅 Event';
  }
}

// Example 6: Using the API client in React components with TanStack Query
export const userQueryKey = ['user'] as const;

export async function getCurrentUser(): Promise<UserDto> {
  return apiClient.getCurrentUser();
}

// This would be used in a React component like:
// const { data: user, error, isLoading } = useQuery({
//   queryKey: userQueryKey,
//   queryFn: getCurrentUser,
//   retry: false, // Don't retry auth failures
// });

/* 
MIGRATION NOTES:

1. Replace manual interface definitions with generated types:
   - User -> UserDto
   - Event -> EventDto  
   - LoginRequest, CreateEventRequest, etc. are now auto-generated

2. Use the apiClient for all API calls instead of manual fetch/axios:
   - Automatic error handling
   - Built-in type safety
   - Cookie-based authentication handling

3. Update existing API service files to use generated types:
   - Import from '@witchcityrope/shared-types'
   - Update function signatures to use generated types
   - Use type guards for runtime validation

4. Benefits of the new approach:
   - Types are always in sync with API
   - Compile-time errors when API changes
   - Better IDE support and autocomplete
   - Standardized error handling
   - No more manual type maintenance
*/