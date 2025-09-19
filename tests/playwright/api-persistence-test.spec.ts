import { test, expect } from '@playwright/test';

test('API Direct Persistence Testing', async ({ request }) => {
  console.log('🧪 Testing API persistence directly...');

  // Login first to get authentication
  const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
    data: {
      email: 'admin@witchcityrope.com',
      password: 'Test123!'
    }
  });

  expect(loginResponse.ok()).toBeTruthy();
  console.log('✅ Login successful');

  // Get list of events
  const eventsResponse = await request.get('http://localhost:5655/api/events');
  const eventsData = await eventsResponse.json();

  expect(eventsData.success).toBeTruthy();
  expect(eventsData.data.length).toBeGreaterThan(0);

  const firstEvent = eventsData.data[0];
  const eventId = firstEvent.id;

  console.log(`📝 Testing with event: "${firstEvent.title}" (${eventId})`);

  // Test 1: Check current state
  console.log('📊 Current event data:');
  console.log(`  Sessions: ${firstEvent.sessions?.length || 0}`);
  console.log(`  Ticket Types: ${firstEvent.ticketTypes?.length || 0}`);
  console.log(`  Volunteer Positions: ${firstEvent.volunteerPositions?.length || 'NOT INCLUDED'}`);

  // Test 2: Try to fetch individual event to see if volunteer positions are there
  const singleEventResponse = await request.get(`http://localhost:5655/api/events/${eventId}`);
  const singleEventData = await singleEventResponse.json();

  console.log('🔍 Individual event API response:');
  console.log(`  Has volunteerPositions field: ${singleEventData.data.hasOwnProperty('volunteerPositions')}`);
  console.log(`  Volunteer positions count: ${singleEventData.data.volunteerPositions?.length || 'NOT INCLUDED'}`);

  // Test 3: Check database vs API consistency
  // We know from database there are 33 volunteer positions total
  // Let's see if any events in the API response include volunteer positions
  let eventsWithVolunteers = 0;
  let totalVolunteerPositions = 0;

  for (const event of eventsData.data) {
    if (event.volunteerPositions && Array.isArray(event.volunteerPositions)) {
      eventsWithVolunteers++;
      totalVolunteerPositions += event.volunteerPositions.length;
    }
  }

  console.log('📈 API vs Database Analysis:');
  console.log(`  Database volunteer positions: 33`);
  console.log(`  API events with volunteer positions: ${eventsWithVolunteers}`);
  console.log(`  API total volunteer positions: ${totalVolunteerPositions}`);

  // Test 4: Check if there are any admin endpoints for volunteer positions
  const adminEndpoints = [
    '/api/admin/volunteer-positions',
    '/api/admin/events/volunteer-positions',
    '/api/volunteer-positions',
    '/api/events/volunteer-positions'
  ];

  for (const endpoint of adminEndpoints) {
    try {
      const response = await request.get(`http://localhost:5655${endpoint}`);
      if (response.ok()) {
        const data = await response.json();
        console.log(`✅ Found working endpoint: ${endpoint}`);
        console.log(`   Response: ${JSON.stringify(data).substring(0, 200)}...`);
        break;
      }
    } catch (error) {
      // Endpoint doesn't exist, continue
    }
  }

  // Test 5: Verify sessions and ticket types ARE being returned (to confirm partial fix)
  expect(firstEvent.sessions).toBeDefined();
  expect(Array.isArray(firstEvent.sessions)).toBeTruthy();
  expect(firstEvent.ticketTypes).toBeDefined();
  expect(Array.isArray(firstEvent.ticketTypes)).toBeTruthy();

  console.log('✅ Sessions and ticket types are properly included in API response');

  // Test 6: The critical failure - volunteer positions should be included but aren't
  if (!firstEvent.volunteerPositions || !Array.isArray(firstEvent.volunteerPositions)) {
    console.log('❌ CRITICAL FAILURE: Volunteer positions are NOT included in API response');
    console.log('❌ Backend developer\'s fix is INCOMPLETE');
    console.log('❌ The Event entity navigation property may be missing or not being serialized');

    // This is a critical test failure
    throw new Error('PERSISTENCE FIX INCOMPLETE: Volunteer positions not included in API responses');
  } else {
    console.log('✅ Volunteer positions are properly included in API response');
  }

  console.log('🏁 API persistence testing completed');
});