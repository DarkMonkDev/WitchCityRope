// test/mocks/handlers.ts
import { http, HttpResponse } from 'msw'
import type { Event, PaginatedResponse } from '../../types/api.types'

// Use NSwag generated UserDto structure - aligned with API
type UserDto = {
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

// Environment-based API URL configuration - NO MORE HARD-CODED PORTS
const getApiBaseUrl = () => {
  if (typeof window !== 'undefined' && window.location) {
    // Browser environment - use VITE environment variables
    return import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655';
  }
  // Test environment fallback
  return 'http://localhost:5655';
};

const API_BASE_URL = getApiBaseUrl();

export const handlers = [
  // Authentication endpoints
  // Current user endpoint used by useCurrentUser hook
  // NOTE: API returns UserDto directly, not wrapped in {success, data}
  http.get('/api/auth/user', () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    } as UserDto)
  }),

  // Support absolute URLs for auth user endpoint (environment-based)
  // NOTE: API returns UserDto directly, not wrapped in {success, data}
  http.get(`${API_BASE_URL}/api/auth/user`, () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    } as UserDto)
  }),

  // Legacy endpoints for backwards compatibility
  // Mock the actual API endpoint that auth store calls
  http.get('/api/protected/profile', () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    } as UserDto)
  }),

  // Support absolute URLs for profile endpoint (environment-based)
  http.get(`${API_BASE_URL}/api/protected/profile`, () => {
    return HttpResponse.json({
      id: '1',
      email: 'admin@witchcityrope.com',
      sceneName: 'TestAdmin',
      firstName: null,
      lastName: null,
      roles: ['Admin'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    } as UserDto)
  }),

  // Logout endpoints - lowercase to match actual API
  http.post('/api/auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post(`${API_BASE_URL}/api/auth/logout`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Login endpoints - lowercase with proper LoginResponse structure
  http.post('/api/auth/login', async ({ request }) => {
    const body = await request.json() as any
    if (body.email === 'admin@witchcityrope.com' && body.password === 'Test123!') {
      return HttpResponse.json({
        success: true,
        user: {
          id: '1',
          email: body.email as string,
          sceneName: 'TestAdmin',
          firstName: null,
          lastName: null,
          roles: ['Admin'],
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z',
          lastLoginAt: '2025-08-19T10:00:00Z'
        } as UserDto,
        message: 'Login successful'
      })
    }
    return HttpResponse.json({
      error: 'Invalid credentials'
    }, { status: 401 })
  }),

  http.post(`${API_BASE_URL}/api/auth/login`, async ({ request }) => {
    const body = await request.json() as any
    if (body.email === 'admin@witchcityrope.com' && body.password === 'Test123!') {
      return HttpResponse.json({
        success: true,
        user: {
          id: '1',
          email: body.email as string,
          sceneName: 'TestAdmin',
          firstName: null,
          lastName: null,
          roles: ['Admin'],
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z',
          lastLoginAt: '2025-08-19T10:00:00Z'
        } as UserDto,
        message: 'Login successful'
      })
    }
    return HttpResponse.json({
      error: 'Invalid credentials'
    }, { status: 401 })
  }),

  // Auth refresh endpoint for interceptor
  http.post(`${API_BASE_URL}/auth/refresh`, () => {
    return new HttpResponse('Unauthorized', { status: 401 })
  }),

  // Protected welcome endpoint (relative URL for authService)
  http.get('/api/protected/welcome', () => {
    return HttpResponse.json({
      message: 'Welcome to the protected area!',
      user: {
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      } as UserDto,
      serverTime: new Date().toISOString()
    })
  }),

  // Protected welcome endpoint (absolute URL for backward compatibility)
  http.get(`${API_BASE_URL}/api/protected/welcome`, () => {
    return HttpResponse.json({
      message: 'Welcome to the protected area!',
      user: {
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      } as UserDto,
      serverTime: new Date().toISOString()
    })
  }),

  // Register endpoint
  http.post(`${API_BASE_URL}/api/auth/register`, async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      success: true,
      user: {
        id: 'new-user-id',
        email: body.email as string,
        sceneName: body.sceneName as string,
        firstName: null,
        lastName: null,
        roles: ['GeneralMember'],
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        lastLoginAt: new Date().toISOString()
      } as UserDto,
      message: 'Registration successful'
    })
  }),

  // CSRF / Antiforgery Token endpoints
  // Backend: GET /api/antiforgery/token returns 200 OK and sets XSRF-TOKEN cookie
  // Frontend: Reads cookie and sends as X-CSRF-TOKEN header in state-changing requests
  http.get('/api/antiforgery/token', () => {
    // Mock CSRF token initialization
    // In unit tests, we can't set actual cookies, but we return 200 OK to simulate success
    return new HttpResponse(null, {
      status: 200,
      headers: {
        'Set-Cookie': 'XSRF-TOKEN=mock-csrf-token-for-unit-tests; Path=/; SameSite=Strict'
      }
    })
  }),

  http.get(`${API_BASE_URL}/api/antiforgery/token`, () => {
    return new HttpResponse(null, {
      status: 200,
      headers: {
        'Set-Cookie': 'XSRF-TOKEN=mock-csrf-token-for-unit-tests; Path=/; SameSite=Strict'
      }
    })
  }),

  // Logout endpoint (both relative and absolute URL support)
  http.post('/api/auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post(`${API_BASE_URL}/api/auth/logout`, () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Keep legacy relative paths for backward compatibility
  http.get('/auth/me', () => {
    return HttpResponse.json({
      success: true,
      data: {
        id: '1',
        email: 'admin@witchcityrope.com',
        sceneName: 'TestAdmin',
        firstName: null,
        lastName: null,
        roles: ['Admin'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      } as UserDto
    })
  }),

  http.post('/auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),


  // Events endpoints - CLEANED UP with proper API response format and no duplicates
  
  // Individual event endpoint (both relative and absolute URL support)
  http.get('/api/events/:id', ({ params }) => {
    return HttpResponse.json({
      success: true,
      data: {
        id: params.id,
        title: 'Test Event',
        description: 'A test event for API validation',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        capacity: 20,
        registrationCount: 5,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorId: '1',
        instructor: {
          id: '1',
          sceneName: 'TestInstructor',
          email: 'instructor@test.com',
          createdAt: '2025-08-19T00:00:00Z',
          lastLoginAt: '2025-08-19T10:00:00Z'
        },
        attendees: [],
      } as Event
    })
  }),

  http.get(`${API_BASE_URL}/api/events/:id`, ({ params }) => {
    return HttpResponse.json({
      success: true,
      data: {
        id: params.id,
        title: 'Test Event',
        description: 'A test event for API validation',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        capacity: 20,
        registrationCount: 5,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorId: '1',
        instructor: {
          id: '1',
          sceneName: 'TestInstructor',
          email: 'instructor@test.com',
          createdAt: '2025-08-19T00:00:00Z',
          lastLoginAt: '2025-08-19T10:00:00Z'
        },
        attendees: [],
      } as Event
    })
  }),

  // Events list endpoint (both relative and absolute URL support)
  // Pattern B: API returns Event[] directly, NOT wrapped in {success, data}
  http.get('/api/events', () => {

    const events = [
      {
        id: '1',
        title: 'Rope Bondage Fundamentals',
        description: 'Learn the basics of safe rope bondage with experienced instructors',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        capacity: 20,
        registrationCount: 5,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Community Social Night',
        description: 'Join fellow community members for socializing and light play',
        startDate: '2025-08-21T19:00:00Z',
        endDate: '2025-08-21T21:00:00Z',
        capacity: 15,
        registrationCount: 10,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: true,
        requireTicketPurchase: false,
        vettedMembersOnly: false,
        instructorId: '1',
      },
    ] as Event[]

    // Pattern B: Return array directly, NO wrapper
    // EventsList component checks Array.isArray(data) - see line 23
    return HttpResponse.json(events)
  }),

  http.get(`${API_BASE_URL}/api/events`, () => {

    const events = [
      {
        id: '1',
        title: 'Rope Bondage Fundamentals',
        description: 'Learn the basics of safe rope bondage with experienced instructors',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        capacity: 20,
        registrationCount: 5,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Community Social Night',
        description: 'Join fellow community members for socializing and light play',
        startDate: '2025-08-21T19:00:00Z',
        endDate: '2025-08-21T21:00:00Z',
        capacity: 15,
        registrationCount: 10,
        isRegistrationOpen: true,
        status: 'Published',
        allowRsvps: true,
        requireTicketPurchase: false,
        vettedMembersOnly: false,
        instructorId: '1',
      },
    ] as Event[]

    // Pattern B: Return array directly, NO wrapper
    // EventsList component checks Array.isArray(data) - see line 23
    return HttpResponse.json(events)
  }),

  http.post('/api/events', async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      id: 'new-event-id',
      title: body.title || 'New Event',
      description: body.description || 'Event description',
      startDate: body.startDate || new Date().toISOString(),
      endDate: body.endDate || new Date().toISOString(),
      capacity: body.capacity || 20,
      registrationCount: 0,
      isRegistrationOpen: true,
      instructorId: '1',
      instructor: {
        id: '1',
        sceneName: 'TestInstructor',
        email: 'instructor@test.com',
        createdAt: '2025-08-19T00:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      },
      attendees: [],
    } as Event)
  }),

  http.put('/api/events/:id', async ({ params, request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      id: params.id,
      title: body.title || 'Updated Test Event',
      description: body.description || 'Updated description',
      startDate: body.startDate || new Date().toISOString(),
      endDate: body.endDate || new Date().toISOString(),
      capacity: body.capacity || 20,
      registrationCount: 5,
      isRegistrationOpen: true,
      instructorId: '1',
    } as Event)
  }),

  http.delete('/api/events/:id', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Event copy endpoint
  http.post('/api/events/:id/copy', async ({ params, request }) => {
    const body = await request.json() as { newStartDate: string; newTitle: string }

    return HttpResponse.json({
      id: `copied-${params.id}`,
      title: body.newTitle,
      description: 'Full copied event description',
      startDate: body.newStartDate,
      endDate: new Date(new Date(body.newStartDate).getTime() + 2 * 60 * 60 * 1000).toISOString(),
      capacity: 20,
      registrationCount: 0,
      isRegistrationOpen: false,
      instructorId: '1',
      allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
      status: 'Draft',
    } as Event)
  }),

  // Event Registration
  http.post('/api/events/:eventId/registration', async ({ params, request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      id: `reg-${params.eventId}`,
      eventId: params.eventId,
      userId: '1',
      registeredAt: new Date().toISOString(),
      status: body.action === 'register' ? 'Confirmed' : 'Cancelled',
    })
  }),

  // Members
  http.get('/api/members', ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    
    const members = [
      {
        id: '1',
        email: 'member1@test.com',
        sceneName: 'TestMember1',
        firstName: null,
        lastName: null,
        roles: ['GeneralMember'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      },
      {
        id: '2',
        email: 'member2@test.com',
        sceneName: 'TestMember2',
        firstName: null,
        lastName: null,
        roles: ['VettedMember'],
        isActive: true,
        createdAt: '2025-08-19T00:00:00Z',
        updatedAt: '2025-08-19T10:00:00Z',
        lastLoginAt: '2025-08-19T10:00:00Z'
      },
    ] as UserDto[]

    // Return paginated or simple response based on request
    if (url.searchParams.has('page')) {
      return HttpResponse.json({
        data: members,
        page,
        pageSize,
        totalCount: 50,
        totalPages: 3,
        hasNext: page < 3,
        hasPrevious: page > 1,
      } as PaginatedResponse<UserDto>)
    }

    return HttpResponse.json(members)
  }),

  http.get('/api/members/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      email: 'member@test.com',
      sceneName: 'TestMember',
      firstName: null,
      lastName: null,
      roles: ['GeneralMember'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    } as UserDto)
  }),

  http.put('/api/members/:id/status', async ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      email: 'member@test.com',
      sceneName: 'TestMember',
      firstName: null,
      lastName: null,
      roles: ['GeneralMember'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: new Date().toISOString(),
      lastLoginAt: new Date().toISOString()
    } as UserDto)
  }),

  http.put('/api/members/profile', async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      id: '1',
      email: 'user@test.com',
      sceneName: body.sceneName || 'TestUser',
      firstName: null,
      lastName: null,
      roles: ['GeneralMember'],
      isActive: true,
      createdAt: '2025-08-19T00:00:00Z',
      updatedAt: new Date().toISOString(),
      lastLoginAt: new Date().toISOString()
    } as UserDto)
  }),

  // Dashboard endpoints
  // CRITICAL: Match actual backend structure - returns minimal dashboard DTO, not full UserDto
  http.get('/api/dashboard', () => {
    return HttpResponse.json({
      sceneName: 'TestAdmin',
      role: 'Administrator',
      vettingStatus: 'Approved',
      hasVettingApplication: true,
      isVetted: true,
      email: 'admin@witchcityrope.com',
      joinDate: '2025-08-19T00:00:00Z',
      pronouns: 'they/them'
    })
  }),

  http.get(`${API_BASE_URL}/api/dashboard`, () => {
    return HttpResponse.json({
      sceneName: 'TestAdmin',
      role: 'Administrator',
      vettingStatus: 'Approved',
      hasVettingApplication: true,
      isVetted: true,
      email: 'admin@witchcityrope.com',
      joinDate: '2025-08-19T00:00:00Z',
      pronouns: 'they/them'
    })
  }),

  // CRITICAL: Match actual backend structure - returns dashboard event DTO with registration info
  http.get('/api/dashboard/events', ({ request }) => {
    const url = new URL(request.url)
    const count = parseInt(url.searchParams.get('count') || '3')

    const events = [
      {
        id: '1',
        title: 'Upcoming Workshop',
        startDate: new Date(Date.now() + 86400000).toISOString(),
        endDate: new Date(Date.now() + 90000000).toISOString(),
        location: 'Main Workshop Room',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorName: 'TestInstructor',
        registrationStatus: 'Ticket Purchased',
        ticketId: 'ticket-1',
        confirmationCode: 'TEST1234'
      },
      {
        id: '2',
        title: 'Community Social Night',
        startDate: new Date(Date.now() + 172800000).toISOString(),
        endDate: new Date(Date.now() + 176400000).toISOString(),
        location: 'Community Space',
        allowRsvps: true,
        requireTicketPurchase: false,
        vettedMembersOnly: false,
        instructorName: '',
        registrationStatus: 'RSVP Confirmed',
        ticketId: 'rsvp-2',
        confirmationCode: 'RSVP5678'
      }
    ].slice(0, count)

    return HttpResponse.json({
      upcomingEvents: events
    })
  }),

  http.get(`${API_BASE_URL}/api/dashboard/events`, ({ request }) => {
    const url = new URL(request.url)
    const count = parseInt(url.searchParams.get('count') || '3')

    const events = [
      {
        id: '1',
        title: 'Upcoming Workshop',
        startDate: new Date(Date.now() + 86400000).toISOString(),
        endDate: new Date(Date.now() + 90000000).toISOString(),
        location: 'Main Workshop Room',
        allowRsvps: false,
        requireTicketPurchase: true,
        vettedMembersOnly: false,
        instructorName: 'TestInstructor',
        registrationStatus: 'Ticket Purchased',
        ticketId: 'ticket-1',
        confirmationCode: 'TEST1234'
      },
      {
        id: '2',
        title: 'Community Social Night',
        startDate: new Date(Date.now() + 172800000).toISOString(),
        endDate: new Date(Date.now() + 176400000).toISOString(),
        location: 'Community Space',
        allowRsvps: true,
        requireTicketPurchase: false,
        vettedMembersOnly: false,
        instructorName: '',
        registrationStatus: 'RSVP Confirmed',
        ticketId: 'rsvp-2',
        confirmationCode: 'RSVP5678'
      }
    ].slice(0, count)

    return HttpResponse.json({
      upcomingEvents: events
    })
  }),

  // CRITICAL: Match actual backend structure - returns user-centric statistics, not org-wide stats
  http.get('/api/dashboard/statistics', () => {
    return HttpResponse.json({
      isVerified: true,
      eventsAttended: 2,
      monthsAsMember: 1,
      recentEvents: 3,
      joinDate: '2025-08-19T00:00:00Z',
      vettingStatus: 'Approved',
      nextInterviewDate: null,
      upcomingRegistrations: 5,
      cancelledRegistrations: 0
    })
  }),

  http.get(`${API_BASE_URL}/api/dashboard/statistics`, () => {
    return HttpResponse.json({
      isVerified: true,
      eventsAttended: 2,
      monthsAsMember: 1,
      recentEvents: 3,
      joinDate: '2025-08-19T00:00:00Z',
      vettingStatus: 'Approved',
      nextInterviewDate: null,
      upcomingRegistrations: 5,
      cancelledRegistrations: 0
    })
  }),

  // Vetting Status endpoints
  http.get('/api/vetting/status', () => {
    return HttpResponse.json({
      success: true,
      data: {
        hasApplication: false,
        application: null
      }
    })
  }),

  http.get(`${API_BASE_URL}/api/vetting/status`, () => {
    return HttpResponse.json({
      success: true,
      data: {
        hasApplication: false,
        application: null
      }
    })
  }),

  // User Participations endpoints
  http.get('/api/user/participations', () => {
    return HttpResponse.json([
      {
        id: 'participation-1',
        eventId: '1',
        eventTitle: 'Rope Bondage Fundamentals',
        eventStartDate: '2025-08-20T19:00:00Z',
        eventEndDate: '2025-08-20T21:00:00Z',
        eventLocation: 'Salem Community Center',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-08-15T10:00:00Z',
        notes: null,
        canCancel: true
      }
    ])
  }),

  http.get(`${API_BASE_URL}/api/user/participations`, () => {
    return HttpResponse.json([
      {
        id: 'participation-1',
        eventId: '1',
        eventTitle: 'Rope Bondage Fundamentals',
        eventStartDate: '2025-08-20T19:00:00Z',
        eventEndDate: '2025-08-20T21:00:00Z',
        eventLocation: 'Salem Community Center',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-08-15T10:00:00Z',
        notes: null,
        canCancel: true
      }
    ])
  }),

  // OPTIONS preflight for participations (CORS)
  http.options(`${API_BASE_URL}/api/user/participations`, () => {
    return new HttpResponse(null, { status: 200 })
  }),

  // Safety / Incident Management endpoints
  // Get available coordinators
  http.get('/api/safety/admin/users/coordinators', () => {
    return HttpResponse.json([
      { id: '1', sceneName: 'Admin User', fullName: 'Alice Smith', role: 'Admin', activeIncidentCount: 2 },
      { id: '2', sceneName: 'Safety Coordinator', fullName: 'Bob Johnson', role: 'Teacher', activeIncidentCount: 1 },
      { id: '3', sceneName: 'RopeTeacher', fullName: 'Carol Martinez', role: 'Teacher', activeIncidentCount: 0 }
    ])
  }),

  http.get(`${API_BASE_URL}/api/safety/admin/users/coordinators`, () => {
    return HttpResponse.json([
      { id: '1', sceneName: 'Admin User', fullName: 'Alice Smith', role: 'Admin', activeIncidentCount: 2 },
      { id: '2', sceneName: 'Safety Coordinator', fullName: 'Bob Johnson', role: 'Teacher', activeIncidentCount: 1 },
      { id: '3', sceneName: 'RopeTeacher', fullName: 'Carol Martinez', role: 'Teacher', activeIncidentCount: 0 }
    ])
  }),

  // Assign coordinator to incident
  http.post('/api/safety/admin/incidents/:incidentId/assign', () => {
    return HttpResponse.json({ success: true })
  }),

  http.post(`${API_BASE_URL}/api/safety/admin/incidents/:incidentId/assign`, () => {
    return HttpResponse.json({ success: true })
  }),

  // Venue endpoints
  // Get active venues (relative URL)
  http.get('/api/admin/venues/active', () => {
    return HttpResponse.json({
      data: [
        {
          id: 'venue-1',
          name: 'Salem Community Center',
          address: '123 Main St',
          city: 'Salem',
          state: 'MA',
          zipCode: '01970',
          capacity: 100,
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z'
        },
        {
          id: 'venue-2',
          name: 'Witch City Workshop Space',
          address: '456 Essex St',
          city: 'Salem',
          state: 'MA',
          zipCode: '01970',
          capacity: 50,
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z'
        }
      ]
    })
  }),

  // Get active venues (absolute URL)
  http.get(`${API_BASE_URL}/api/admin/venues/active`, () => {
    return HttpResponse.json({
      data: [
        {
          id: 'venue-1',
          name: 'Salem Community Center',
          address: '123 Main St',
          city: 'Salem',
          state: 'MA',
          zipCode: '01970',
          capacity: 100,
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z'
        },
        {
          id: 'venue-2',
          name: 'Witch City Workshop Space',
          address: '456 Essex St',
          city: 'Salem',
          state: 'MA',
          zipCode: '01970',
          capacity: 50,
          isActive: true,
          createdAt: '2025-08-19T00:00:00Z',
          updatedAt: '2025-08-19T10:00:00Z'
        }
      ]
    })
  }),

  // OPTIONS preflight for venues endpoint (CORS)
  http.options('/api/admin/venues/active', () => {
    return new HttpResponse(null, { status: 200 })
  }),

  http.options(`${API_BASE_URL}/api/admin/venues/active`, () => {
    return new HttpResponse(null, { status: 200 })
  }),

  // Event participations endpoints (relative URL)
  http.get('/api/admin/events/:eventId/participations', ({ params }) => {
    // Return mock participations for testing
    // Alice has BOTH RSVP and Ticket (2 separate records)
    // Bob has just a Ticket (1 record)
    // Charlie has just an RSVP (1 record)
    const mockParticipations = [
      // Alice's RSVP record
      {
        id: 'attendance-1',
        eventId: params.eventId,
        userId: 'user-1',
        userSceneName: 'Alice Wonderland',
        userEmail: 'alice@example.com',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-11-01T10:00:00Z',
        notes: 'Looking forward to it!',
        canCancel: true,
        metadata: null,
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: null,
        sessionNames: 'All Sessions'
      },
      // Alice's Ticket record (same user, different participation type)
      {
        id: 'attendance-2',
        eventId: params.eventId,
        userId: 'user-1',
        userSceneName: 'Alice Wonderland',
        userEmail: 'alice@example.com',
        participationType: 'Ticket',
        status: 'Active',
        participationDate: '2025-11-01T10:05:00Z',
        notes: null,
        canCancel: true,
        metadata: '{"price":25.00,"paymentMethod":"PayPal"}',
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: 'Suggested Donation',
        sessionNames: 'All Sessions'
      },
      // Bob has just a ticket (class event)
      {
        id: 'attendance-3',
        eventId: params.eventId,
        userId: 'user-2',
        userSceneName: 'Bob Builder',
        userEmail: 'bob@example.com',
        participationType: 'Ticket',
        status: 'Active',
        participationDate: '2025-11-02T10:00:00Z',
        notes: null,
        canCancel: true,
        metadata: '{"price":35.00,"paymentMethod":"Stripe"}',
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: 'Standard Ticket',
        sessionNames: 'Session 1'
      },
      // Charlie has just RSVP (social event, no donation)
      {
        id: 'attendance-4',
        eventId: params.eventId,
        userId: 'user-3',
        userSceneName: 'Charlie Chaplin',
        userEmail: 'charlie@example.com',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-11-03T10:00:00Z',
        notes: 'Can\'t wait!',
        canCancel: true,
        metadata: null,
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: null,
        sessionNames: 'All Sessions'
      }
    ]

    return HttpResponse.json({
      data: mockParticipations
    })
  }),

  // Event participations endpoints (absolute URL)
  http.get(`${API_BASE_URL}/api/admin/events/:eventId/participations`, ({ params }) => {
    // Return mock participations for testing
    // Alice has BOTH RSVP and Ticket (2 separate records)
    // Bob has just a Ticket (1 record)
    // Charlie has just an RSVP (1 record)
    const mockParticipations = [
      // Alice's RSVP record
      {
        id: 'attendance-1',
        eventId: params.eventId,
        userId: 'user-1',
        userSceneName: 'Alice Wonderland',
        userEmail: 'alice@example.com',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-11-01T10:00:00Z',
        notes: 'Looking forward to it!',
        canCancel: true,
        metadata: null,
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: null,
        sessionNames: 'All Sessions'
      },
      // Alice's Ticket record (same user, different participation type)
      {
        id: 'attendance-2',
        eventId: params.eventId,
        userId: 'user-1',
        userSceneName: 'Alice Wonderland',
        userEmail: 'alice@example.com',
        participationType: 'Ticket',
        status: 'Active',
        participationDate: '2025-11-01T10:05:00Z',
        notes: null,
        canCancel: true,
        metadata: '{"price":25.00,"paymentMethod":"PayPal"}',
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: 'Suggested Donation',
        sessionNames: 'All Sessions'
      },
      // Bob has just a ticket (class event)
      {
        id: 'attendance-3',
        eventId: params.eventId,
        userId: 'user-2',
        userSceneName: 'Bob Builder',
        userEmail: 'bob@example.com',
        participationType: 'Ticket',
        status: 'Active',
        participationDate: '2025-11-02T10:00:00Z',
        notes: null,
        canCancel: true,
        metadata: '{"price":35.00,"paymentMethod":"Stripe"}',
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: 'Standard Ticket',
        sessionNames: 'Session 1'
      },
      // Charlie has just RSVP (social event, no donation)
      {
        id: 'attendance-4',
        eventId: params.eventId,
        userId: 'user-3',
        userSceneName: 'Charlie Chaplin',
        userEmail: 'charlie@example.com',
        participationType: 'RSVP',
        status: 'Active',
        participationDate: '2025-11-03T10:00:00Z',
        notes: 'Can\'t wait!',
        canCancel: true,
        metadata: null,
        hasCheckedIn: false,
        checkInTime: null,
        ticketTypeName: null,
        sessionNames: 'All Sessions'
      }
    ]

    return HttpResponse.json({
      data: mockParticipations
    })
  }),
]