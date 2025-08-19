export const queryKeys = {
  // Authentication
  auth: () => ['auth'] as const,
  currentUser: () => [...queryKeys.auth(), 'current-user'] as const,
  
  // Users
  users: () => ['users'] as const,
  user: (id: string) => [...queryKeys.users(), id] as const,
  userEvents: (id: string) => [...queryKeys.user(id), 'events'] as const,
  
  // Events
  events: () => ['events'] as const,
  event: (id: string) => [...queryKeys.events(), id] as const,
  eventAttendees: (id: string) => [...queryKeys.event(id), 'attendees'] as const,
  infiniteEvents: (filters: any) => [...queryKeys.events(), 'infinite', filters] as const,
  
  // Event Registrations
  registrations: () => ['registrations'] as const,
  userRegistrations: (userId: string) => [...queryKeys.registrations(), 'user', userId] as const,
  eventRegistrations: (eventId: string) => [...queryKeys.registrations(), 'event', eventId] as const,
} as const