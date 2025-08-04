import { test, expect, APIRequestContext } from '@playwright/test';

const API_BASE_URL = process.env.API_URL || 'http://localhost:5653';

// Test credentials
const adminCredentials = {
  email: 'admin@witchcityrope.org',
  password: 'AdminP@ssw0rd!'
};

test.describe('Event API Diagnostics', () => {
  let apiContext: APIRequestContext;
  let authData: any = null;

  test.beforeAll(async ({ playwright }) => {
    apiContext = await playwright.request.newContext({
      baseURL: API_BASE_URL,
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });
  });

  test.afterAll(async () => {
    await apiContext.dispose();
  });

  test('should check API health', async () => {
    try {
      const response = await apiContext.get('/health');
      console.log(`Health check response status: ${response.status()}`);
      
      if (response.ok()) {
        const text = await response.text();
        console.log('Health response:', text);
      }
      
      expect(response.status()).toBeLessThan(500);
    } catch (error: any) {
      console.log('Health check error:', error.message);
    }
  });

  test('should authenticate admin user', async () => {
    const response = await apiContext.post('/api/login', {
      data: adminCredentials
    });

    console.log(`Login response status: ${response.status()}`);
    console.log('Login response headers:', response.headers());

    if (response.ok()) {
      authData = await response.json();
      console.log('Login successful:', {
        accessToken: authData.accessToken ? 'Present' : 'Missing',
        refreshToken: authData.refreshToken ? 'Present' : 'Missing',
        email: authData.email,
        roles: authData.roles,
        userId: authData.userId
      });
      
      expect(authData.accessToken).toBeTruthy();
    } else {
      const text = await response.text();
      console.log('Login failed:', text);
      expect(response.ok()).toBeTruthy();
    }
  });

  test('should get public events without auth', async () => {
    const response = await apiContext.get('/api/events');
    console.log(`Get events response status: ${response.status()}`);

    if (response.ok()) {
      const data = await response.json();
      console.log('Events retrieved:', {
        totalCount: data.totalCount || 0,
        eventsCount: data.events ? data.events.length : 0,
        pageNumber: data.pageNumber,
        pageSize: data.pageSize
      });

      if (data.events && data.events.length > 0) {
        console.log('First event:', data.events[0]);
      }
    } else {
      const text = await response.text();
      console.log('Get events failed:', text);
    }
  });

  test('should get events with authentication', async ({ }) => {
    test.skip(!authData?.accessToken, 'No auth token available');

    const response = await apiContext.get('/api/events', {
      headers: {
        'Authorization': `Bearer ${authData.accessToken}`
      }
    });

    console.log(`Authenticated get events response status: ${response.status()}`);
    expect(response.ok()).toBeTruthy();

    const data = await response.json();
    expect(data).toHaveProperty('events');
  });

  test('should create an event', async ({ }) => {
    test.skip(!authData?.accessToken, 'No auth token available');

    const futureDate = new Date();
    futureDate.setDate(futureDate.getDate() + 7);
    futureDate.setHours(19, 0, 0, 0);
    
    const endDate = new Date(futureDate);
    endDate.setHours(22, 0, 0, 0);

    const eventData = {
      title: `Test Event ${Date.now()}`,
      description: 'This is a test event created by the diagnostic script',
      startDate: futureDate.toISOString(),
      endDate: endDate.toISOString(),
      location: 'Test Location',
      maxAttendees: 50,
      ticketPrice: 0,
      eventType: 'Social',
      status: 'Published',
      isFeatured: false,
      showInPublicList: true
    };

    console.log('Creating event with data:', eventData);

    const response = await apiContext.post('/api/events', {
      data: eventData,
      headers: {
        'Authorization': `Bearer ${authData.accessToken}`
      }
    });

    console.log(`Create event response status: ${response.status()}`);

    if (response.ok()) {
      const createdEvent = await response.json();
      console.log('Event created successfully:', createdEvent);
      
      // Verify the created event
      await test.step('Verify created event', async () => {
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        const getResponse = await apiContext.get(`/api/events/${createdEvent.eventId || createdEvent.id}`, {
          headers: {
            'Authorization': `Bearer ${authData.accessToken}`
          }
        });
        
        expect(getResponse.ok()).toBeTruthy();
        const retrievedEvent = await getResponse.json();
        console.log('Retrieved event:', retrievedEvent);
      });
    } else {
      const errorText = await response.text();
      console.log('Event creation failed:', errorText);
      
      // Try to parse validation errors
      try {
        const errorData = JSON.parse(errorText);
        if (errorData.errors) {
          console.log('Validation errors:', errorData.errors);
        }
      } catch {
        // Not JSON error response
      }
    }
  });

  test('should get admin events', async ({ }) => {
    test.skip(!authData?.accessToken, 'No auth token available');

    const response = await apiContext.get('/api/admin/events', {
      headers: {
        'Authorization': `Bearer ${authData.accessToken}`
      }
    });

    console.log(`Admin events response status: ${response.status()}`);

    if (response.ok()) {
      const data = await response.json();
      console.log('Admin events retrieved:', {
        count: Array.isArray(data) ? data.length : 0
      });

      if (Array.isArray(data) && data.length > 0) {
        console.log('First admin event:', data[0]);
      }
    } else {
      const text = await response.text();
      console.log('Admin events failed:', text);
    }
  });

  test('should handle API errors gracefully', async () => {
    // Test 404 endpoint
    const response404 = await apiContext.get('/api/nonexistent');
    expect(response404.status()).toBe(404);

    // Test unauthorized access
    const responseUnauth = await apiContext.get('/api/admin/events');
    expect(responseUnauth.status()).toBe(401);

    // Test invalid data
    const responseBadData = await apiContext.post('/api/events', {
      data: { invalid: 'data' },
      headers: authData?.accessToken ? {
        'Authorization': `Bearer ${authData.accessToken}`
      } : {}
    });
    expect(responseBadData.status()).toBeGreaterThanOrEqual(400);
    expect(responseBadData.status()).toBeLessThan(500);
  });

  test.describe('Rate Limiting and Performance', () => {
    test('should handle multiple concurrent requests', async ({ }) => {
      const requests = Array(5).fill(null).map(() => 
        apiContext.get('/api/events')
      );

      const responses = await Promise.all(requests);
      const statuses = responses.map(r => r.status());
      
      console.log('Concurrent request statuses:', statuses);
      expect(statuses.every(status => status < 500)).toBeTruthy();
    });

    test('should measure API response times', async () => {
      const endpoints = ['/health', '/api/events'];
      
      for (const endpoint of endpoints) {
        const start = Date.now();
        const response = await apiContext.get(endpoint);
        const duration = Date.now() - start;
        
        console.log(`${endpoint}: ${response.status()} (${duration}ms)`);
        expect(duration).toBeLessThan(5000); // Should respond within 5 seconds
      }
    });
  });
});