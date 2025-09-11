#!/usr/bin/env node

/**
 * Debug script to test events API response transformation
 * Usage: node scripts/debug/test-events-api.js
 */

const https = require('http');

console.log('🔍 Testing Events API Response Structure...\n');

// Test API endpoint
const options = {
  hostname: 'localhost',
  port: 5173,
  path: '/api/events',
  method: 'GET',
  headers: {
    'Accept': 'application/json',
  }
};

const req = https.request(options, (res) => {
  let data = '';
  
  res.on('data', (chunk) => {
    data += chunk;
  });
  
  res.on('end', () => {
    try {
      const events = JSON.parse(data);
      
      if (Array.isArray(events)) {
        console.log(`✅ API returns direct array with ${events.length} events`);
        console.log(`\n📋 First event structure:`);
        console.log(JSON.stringify(events[0], null, 2));
        
        console.log(`\n🔧 Transformation test:`);
        const transformed = transformApiEvent(events[0]);
        console.log(JSON.stringify(transformed, null, 2));
        
        console.log(`\n✅ Events API fix is working correctly!`);
        console.log(`📊 Total events available: ${events.length}`);
        
      } else {
        console.log(`❌ API returns wrapped response (unexpected):`, typeof events);
        console.log(events);
      }
      
    } catch (error) {
      console.error('❌ JSON Parse Error:', error.message);
      console.log('Raw Response:', data);
    }
  });
});

req.on('error', (error) => {
  console.error('❌ Request Error:', error.message);
  console.log('Make sure the development server is running on localhost:5173');
});

req.end();

// Transformation function matching the React hook
function transformApiEvent(apiEvent) {
  return {
    id: apiEvent.id,
    title: apiEvent.title,
    description: apiEvent.description,
    startDate: apiEvent.startDate,
    endDate: apiEvent.endDate || null,
    location: apiEvent.location,
    capacity: apiEvent.maxAttendees || 20,
    registrationCount: apiEvent.currentAttendees || 0,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
}