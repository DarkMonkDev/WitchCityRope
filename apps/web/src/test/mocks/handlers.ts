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

export const handlers = [
  // Authentication endpoints
  // Current user endpoint used by useCurrentUser hook
  http.get('/api/auth/user', () => {
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

  // Support absolute URLs for auth user endpoint
  http.get('http://localhost:5655/api/auth/user', () => {
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

  http.get('http://localhost:5653/api/auth/user', () => {
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

  // Legacy endpoints for backwards compatibility
  // Mock the actual API endpoint that auth store calls: /api/Protected/profile
  http.get('/api/Protected/profile', () => {
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

  // Support absolute URLs for profile endpoint
  http.get('http://localhost:5653/api/Protected/profile', () => {
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

  http.get('http://localhost:5655/api/Protected/profile', () => {
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

  // Logout endpoints - Pascal case to match actual API
  http.post('/api/Auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post('http://localhost:5653/api/Auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  http.post('http://localhost:5655/api/Auth/logout', () => {
    return new HttpResponse(null, { status: 204 })
  }),

  // Login endpoints - Pascal case with proper LoginResponse structure
  http.post('/api/Auth/login', async ({ request }) => {
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

  http.post('http://localhost:5653/api/Auth/login', async ({ request }) => {
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

  http.post('http://localhost:5655/api/Auth/login', async ({ request }) => {
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
  http.post('http://localhost:5651/auth/refresh', () => {
    return new HttpResponse('Unauthorized', { status: 401 })
  }),

  http.post('http://localhost:5655/auth/refresh', () => {
    return new HttpResponse('Unauthorized', { status: 401 })
  }),

  // Protected welcome endpoint
  http.get('http://localhost:5655/api/protected/welcome', () => {
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
  http.post('http://localhost:5651/api/auth/register', async ({ request }) => {
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

  http.post('/api/auth/login', async ({ request }) => {
    const body = await request.json() as any
    if (body.email === 'admin@witchcityrope.com') {
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
    return new HttpResponse(null, { status: 401 })
  }),

  // Events endpoints - support both relative and absolute URLs
  http.get('/api/events/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      title: 'Test Event',
      description: 'A test event for API validation',
      startDate: '2025-08-20T19:00:00Z',
      endDate: '2025-08-20T21:00:00Z',
      maxAttendees: 20,
      currentAttendees: 5,
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

  http.get('/api/events', ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    
    const events = [
      {
        id: '1',
        title: 'Test Event 1',
        description: 'First test event',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        maxAttendees: 20,
        currentAttendees: 5,
        isRegistrationOpen: true,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Test Event 2',
        description: 'Second test event',
        startDate: '2025-08-21T19:00:00Z',
        endDate: '2025-08-21T21:00:00Z',
        maxAttendees: 15,
        currentAttendees: 10,
        isRegistrationOpen: true,
        instructorId: '1',
      },
    ] as Event[]

    // Return paginated or simple response based on request
    if (url.searchParams.has('page')) {
      return HttpResponse.json({
        data: events,
        page,
        pageSize,
        totalCount: 25,
        totalPages: 2,
        hasNext: page < 2,
        hasPrevious: page > 1,
      } as PaginatedResponse<Event>)
    }

    return HttpResponse.json(events)
  }),

  // Events with absolute URLs
  http.get('http://localhost:5655/api/events/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      title: 'Test Event',
      description: 'A test event for API validation',
      startDate: '2025-08-20T19:00:00Z',
      endDate: '2025-08-20T21:00:00Z',
      maxAttendees: 20,
      currentAttendees: 5,
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

  http.get('http://localhost:5655/api/events', ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    
    const events = [
      {
        id: '1',
        title: 'Test Event 1',
        description: 'First test event',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        maxAttendees: 20,
        currentAttendees: 5,
        isRegistrationOpen: true,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Test Event 2',
        description: 'Second test event',
        startDate: '2025-08-21T19:00:00Z',
        endDate: '2025-08-21T21:00:00Z',
        maxAttendees: 15,
        currentAttendees: 10,
        isRegistrationOpen: true,
        instructorId: '1',
      },
    ] as Event[]

    // Return paginated or simple response based on request
    if (url.searchParams.has('page')) {
      return HttpResponse.json({
        data: events,
        page,
        pageSize,
        totalCount: 25,
        totalPages: 2,
        hasNext: page < 2,
        hasPrevious: page > 1,
      } as PaginatedResponse<Event>)
    }

    return HttpResponse.json(events)
  }),

  // Events - Keep relative paths for other tests
  http.get('/api/events/:id', ({ params }) => {
    return HttpResponse.json({
      id: params.id,
      title: 'Test Event',
      description: 'A test event for API validation',
      startDate: '2025-08-20T19:00:00Z',
      endDate: '2025-08-20T21:00:00Z',
      maxAttendees: 20,
      currentAttendees: 5,
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

  http.get('/api/events', ({ request }) => {
    const url = new URL(request.url)
    const page = parseInt(url.searchParams.get('page') || '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') || '20')
    
    const events = [
      {
        id: '1',
        title: 'Test Event 1',
        description: 'First test event',
        startDate: '2025-08-20T19:00:00Z',
        endDate: '2025-08-20T21:00:00Z',
        maxAttendees: 20,
        currentAttendees: 5,
        isRegistrationOpen: true,
        instructorId: '1',
      },
      {
        id: '2',
        title: 'Test Event 2',
        description: 'Second test event',
        startDate: '2025-08-21T19:00:00Z',
        endDate: '2025-08-21T21:00:00Z',
        maxAttendees: 15,
        currentAttendees: 10,
        isRegistrationOpen: true,
        instructorId: '1',
      },
    ] as Event[]

    // Return paginated or simple response based on request
    if (url.searchParams.has('page')) {
      return HttpResponse.json({
        data: events,
        page,
        pageSize,
        totalCount: 25,
        totalPages: 2,
        hasNext: page < 2,
        hasPrevious: page > 1,
      } as PaginatedResponse<Event>)
    }

    return HttpResponse.json(events)
  }),

  http.post('/api/events', async ({ request }) => {
    const body = await request.json() as any
    return HttpResponse.json({
      id: 'new-event-id',
      ...(body as object),
      currentAttendees: 0,
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
      title: 'Updated Test Event',
      ...(body as object),
      currentAttendees: 5,
      isRegistrationOpen: true,
    } as Event)
  }),

  http.delete('/api/events/:id', () => {
    return new HttpResponse(null, { status: 204 })
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

  http.put('/api/members/:id/status', async ({ params, request }) => {
    const body = await request.json() as any
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
]