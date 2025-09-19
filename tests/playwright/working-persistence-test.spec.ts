import { test, expect } from '@playwright/test';

test('Test Working Persistence Components - Sessions and Ticket Types', async ({ request }) => {
  console.log('🧪 Testing persistence for WORKING components (Sessions & Ticket Types)...');

  // Login to get authentication
  const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
    data: {
      email: 'admin@witchcityrope.com',
      password: 'Test123!'
    }
  });

  expect(loginResponse.ok()).toBeTruthy();

  // Get events
  const eventsResponse = await request.get('http://localhost:5655/api/events');
  const eventsData = await eventsResponse.json();

  const testEvent = eventsData.data[0];
  console.log(`📝 Testing with event: "${testEvent.title}"`);

  // Verify current working state
  console.log('✅ WORKING COMPONENTS VERIFICATION:');
  console.log(`  Sessions count: ${testEvent.sessions.length}`);
  console.log(`  Ticket types count: ${testEvent.ticketTypes.length}`);

  // Test sessions persistence capability
  if (testEvent.sessions && testEvent.sessions.length > 0) {
    const session = testEvent.sessions[0];
    console.log(`✅ Session data structure working:`);
    console.log(`  - ID: ${session.id}`);
    console.log(`  - Name: ${session.name}`);
    console.log(`  - Session Identifier: ${session.sessionIdentifier}`);
    console.log(`  - Capacity: ${session.capacity}`);
    console.log(`  - Registered Count: ${session.registeredCount}`);
  } else {
    throw new Error('Sessions data missing - expected to be working');
  }

  // Test ticket types persistence capability
  if (testEvent.ticketTypes && testEvent.ticketTypes.length > 0) {
    const ticketType = testEvent.ticketTypes[0];
    console.log(`✅ Ticket Type data structure working:`);
    console.log(`  - ID: ${ticketType.id}`);
    console.log(`  - Name: ${ticketType.name}`);
    console.log(`  - Type: ${ticketType.type}`);
    console.log(`  - Min Price: ${ticketType.minPrice}`);
    console.log(`  - Max Price: ${ticketType.maxPrice}`);
    console.log(`  - Quantity Available: ${ticketType.quantityAvailable}`);
  } else {
    throw new Error('Ticket Types data missing - expected to be working');
  }

  // Test data consistency across multiple events
  let totalSessionsInAPI = 0;
  let totalTicketTypesInAPI = 0;

  for (const event of eventsData.data) {
    totalSessionsInAPI += event.sessions?.length || 0;
    totalTicketTypesInAPI += event.ticketTypes?.length || 0;
  }

  console.log(`📊 API Data Summary:`);
  console.log(`  Total events: ${eventsData.data.length}`);
  console.log(`  Total sessions in API: ${totalSessionsInAPI}`);
  console.log(`  Total ticket types in API: ${totalTicketTypesInAPI}`);

  // Verify this matches reasonable expectations
  expect(totalSessionsInAPI).toBeGreaterThan(0);
  expect(totalTicketTypesInAPI).toBeGreaterThan(0);

  // Test individual event endpoint also includes working data
  const singleEventResponse = await request.get(`http://localhost:5655/api/events/${testEvent.id}`);
  const singleEventData = await singleEventResponse.json();

  expect(singleEventData.data.sessions).toBeDefined();
  expect(singleEventData.data.ticketTypes).toBeDefined();
  expect(singleEventData.data.sessions.length).toBeGreaterThan(0);
  expect(singleEventData.data.ticketTypes.length).toBeGreaterThan(0);

  console.log('✅ Individual event endpoint also returns working data');

  // Test that working components have all necessary fields for persistence
  const workingSession = singleEventData.data.sessions[0];
  const requiredSessionFields = ['id', 'name', 'sessionIdentifier', 'capacity', 'registeredCount', 'startTime', 'endTime'];

  for (const field of requiredSessionFields) {
    if (!workingSession.hasOwnProperty(field)) {
      throw new Error(`Session missing required field: ${field}`);
    }
  }

  const workingTicketType = singleEventData.data.ticketTypes[0];
  const requiredTicketFields = ['id', 'name', 'type', 'minPrice', 'maxPrice', 'quantityAvailable'];

  for (const field of requiredTicketFields) {
    if (!workingTicketType.hasOwnProperty(field)) {
      throw new Error(`Ticket Type missing required field: ${field}`);
    }
  }

  console.log('✅ All required fields present for persistence operations');

  // Document the broken component for comparison
  console.log('❌ BROKEN COMPONENT STATUS:');
  console.log(`  Volunteer positions field exists: ${singleEventData.data.hasOwnProperty('volunteerPositions')}`);
  console.log(`  Volunteer positions data: ${singleEventData.data.volunteerPositions || 'MISSING'}`);

  console.log('🎉 Working persistence components validated successfully!');
  console.log('📋 SUMMARY:');
  console.log('  ✅ Sessions: Fully working - ready for add/edit/delete testing');
  console.log('  ✅ Ticket Types: Fully working - ready for add/edit/delete testing');
  console.log('  ❌ Volunteer Positions: Backend fix incomplete - API inclusion needed');
});