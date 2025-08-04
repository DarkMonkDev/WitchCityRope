/**
 * API Test Configuration
 * Contains API-specific settings and utilities
 */

export const apiConfig = {
  // API Base URLs
  urls: {
    api: process.env.API_URL || 'http://localhost:5653',
    apiPath: '/api',
    healthCheck: '/health'
  },

  // API Endpoints
  endpoints: {
    // Auth
    login: '/auth/login',
    logout: '/auth/logout',
    refresh: '/auth/refresh',
    
    // Events
    events: '/events',
    adminEvents: '/admin/events',
    eventDetails: (id: string) => `/events/${id}`,
    eventRsvp: (id: string) => `/events/${id}/rsvp`,
    eventRsvps: (id: string) => `/events/${id}/rsvps`,
    myEventRsvp: (id: string) => `/events/${id}/rsvps/me`,
    
    // User
    userProfile: '/users/me',
    userRsvps: '/users/me/rsvps',
    userTickets: '/users/me/tickets',
    
    // Admin
    adminUsers: '/admin/users',
    adminReports: '/admin/reports'
  },

  // Request defaults
  requestDefaults: {
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    timeout: 30000
  },

  // Test event templates
  eventTemplates: {
    social: {
      eventType: 'Social',
      capacity: 30,
      pricingType: 'Free',
      status: 'Published',
      requiresVetting: false,
      duration: 3 // hours
    },
    workshop: {
      eventType: 'Workshop',
      capacity: 20,
      pricingType: 'Paid',
      status: 'Published',
      requiresVetting: true,
      duration: 2 // hours
    },
    performance: {
      eventType: 'Performance',
      capacity: 100,
      pricingType: 'Paid',
      status: 'Published',
      requiresVetting: false,
      duration: 2 // hours
    }
  },

  // Response expectations
  expectations: {
    maxResponseTime: 5000, // ms
    successStatusCodes: [200, 201, 204],
    authRequiredCodes: [401, 403]
  }
};

/**
 * Helper to build full API URL
 */
export function buildApiUrl(endpoint: string): string {
  const baseUrl = apiConfig.urls.api;
  const apiPath = apiConfig.urls.apiPath;
  
  // If endpoint already includes /api, don't duplicate
  if (endpoint.startsWith('/api')) {
    return `${baseUrl}${endpoint}`;
  }
  
  // Otherwise, add /api prefix
  return `${baseUrl}${apiPath}${endpoint}`;
}

/**
 * Helper to create event data with defaults
 */
export function createEventData(overrides: Partial<any> = {}, template: 'social' | 'workshop' | 'performance' = 'social'): any {
  const templateData = apiConfig.eventTemplates[template];
  const now = new Date();
  const startDate = new Date(now);
  startDate.setDate(startDate.getDate() + 7);
  startDate.setHours(19, 0, 0, 0);
  
  const endDate = new Date(startDate);
  endDate.setHours(startDate.getHours() + templateData.duration, 0, 0, 0);

  return {
    name: `Test ${templateData.eventType} Event - ${Date.now()}`,
    description: `<p>Test ${templateData.eventType.toLowerCase()} event created by automated tests</p>`,
    eventType: templateData.eventType,
    startDateTime: startDate.toISOString(),
    endDateTime: endDate.toISOString(),
    venue: 'Test Venue',
    capacity: templateData.capacity,
    pricingType: templateData.pricingType,
    status: templateData.status,
    requiresVetting: templateData.requiresVetting,
    individualPrice: templateData.pricingType === 'Paid' ? 20 : 0,
    couplePrice: templateData.pricingType === 'Paid' ? 35 : 0,
    ...overrides
  };
}

/**
 * Helper to format API errors for logging
 */
export async function formatApiError(response: any): Promise<string> {
  const status = response.status();
  const statusText = response.statusText();
  let body = '';
  
  try {
    const text = await response.text();
    if (text) {
      try {
        const json = JSON.parse(text);
        body = JSON.stringify(json, null, 2);
      } catch {
        body = text;
      }
    }
  } catch {
    body = 'Unable to read response body';
  }
  
  return `API Error: ${status} ${statusText}\n${body}`;
}

/**
 * Helper to retry API calls
 */
export async function retryApiCall<T>(
  fn: () => Promise<T>,
  retries: number = 3,
  delay: number = 1000
): Promise<T> {
  let lastError: any;
  
  for (let i = 0; i < retries; i++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error;
      if (i < retries - 1) {
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }
  
  throw lastError;
}