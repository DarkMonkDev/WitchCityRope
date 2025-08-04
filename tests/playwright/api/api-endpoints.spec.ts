import { test, expect, APIRequestContext } from '@playwright/test';
import { testConfig } from '../helpers/test.config';

const API_BASE_URL = process.env.API_URL || 'http://localhost:5653/api';

test.describe('API Endpoints Tests', () => {
  let apiContext: APIRequestContext;
  let authToken: string;

  test.beforeAll(async ({ playwright }) => {
    // Create API context
    apiContext = await playwright.request.newContext({
      baseURL: API_BASE_URL,
      extraHTTPHeaders: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
      },
    });

    // Get auth token
    const loginResponse = await apiContext.post('/auth/login', {
      data: {
        email: testConfig.accounts.vetted.email,
        password: testConfig.accounts.vetted.password
      }
    });

    expect(loginResponse.ok()).toBeTruthy();
    const loginData = await loginResponse.json();
    authToken = loginData.accessToken || loginData.token;
    expect(authToken).toBeTruthy();
  });

  test.afterAll(async () => {
    await apiContext.dispose();
  });

  test('should get user RSVPs', async () => {
    const response = await apiContext.get('/users/me/rsvps', {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    expect(response.ok()).toBeTruthy();
    const rsvps = await response.json();
    expect(Array.isArray(rsvps)).toBeTruthy();

    // Log RSVP details if any exist
    if (rsvps.length > 0) {
      console.log(`Found ${rsvps.length} RSVPs:`);
      rsvps.forEach((rsvp: any, index: number) => {
        console.log(`  ${index + 1}. Event: ${rsvp.eventTitle}`);
        console.log(`     Date: ${rsvp.eventDate}`);
        console.log(`     Status: ${rsvp.status}`);
        console.log(`     RSVP Date: ${rsvp.rsvpedAt || rsvp.rsvpDate}`);
      });
    }
  });

  test('should get user tickets', async () => {
    const response = await apiContext.get('/users/me/tickets', {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    expect(response.ok()).toBeTruthy();
    const tickets = await response.json();
    expect(Array.isArray(tickets)).toBeTruthy();

    // Log ticket details if any exist
    if (tickets.length > 0) {
      console.log(`Found ${tickets.length} tickets:`);
      tickets.forEach((ticket: any, index: number) => {
        console.log(`  ${index + 1}. Event: ${ticket.eventTitle}`);
        console.log(`     Date: ${ticket.eventDate}`);
        console.log(`     Status: ${ticket.status}`);
        console.log(`     Purchased: ${ticket.purchasedAt}`);
      });
    }
  });

  test('should check specific event RSVP status', async () => {
    const eventId = '5f3f685a-1b07-4468-a927-e26292468312';
    const response = await apiContext.get(`/events/${eventId}/rsvps/me`, {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    });

    if (response.status() === 404) {
      console.log('No RSVP found for this event');
    } else {
      expect(response.ok()).toBeTruthy();
      const myRsvp = await response.json();
      console.log('My RSVP for Monthly Rope Jam:');
      console.log(`   Status: ${myRsvp.status}`);
      console.log(`   RSVP ID: ${myRsvp.id || myRsvp.rsvpId}`);
    }
  });

  test('should handle authentication errors', async () => {
    // Test without auth token
    const response = await apiContext.get('/users/me/rsvps');
    expect(response.status()).toBe(401);
  });

  test('should handle invalid auth token', async () => {
    const response = await apiContext.get('/users/me/rsvps', {
      headers: {
        'Authorization': 'Bearer invalid-token'
      }
    });
    expect(response.status()).toBe(401);
  });
});