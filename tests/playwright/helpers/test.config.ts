/**
 * Test configuration for WitchCityRope E2E tests
 * Contains test accounts, URLs, and common settings
 */

export const testConfig = {
  // Base URL
  baseUrl: process.env.BASE_URL || 'http://localhost:5651',
  
  // Test accounts
  accounts: {
    admin: {
      email: 'admin@witchcityrope.com',
      password: 'Test123!',
      displayName: 'Admin User'
    },
    member: {
      email: 'member@witchcityrope.com',
      password: 'Test123!',
      displayName: 'Member User'
    },
    teacher: {
      email: 'teacher@witchcityrope.com',
      password: 'Test123!',
      displayName: 'Teacher User'
    },
    vetted: {
      email: 'vetted@witchcityrope.com',
      password: 'Test123!',
      displayName: 'Vetted User'
    },
    guest: {
      email: 'guest@witchcityrope.com',
      password: 'Test123!',
      displayName: 'Guest User'
    }
  },
  
  // Common URLs
  urls: {
    home: '/',
    login: '/Identity/Account/Login',
    logout: '/Identity/Account/Logout',
    register: '/Identity/Account/Register',
    events: '/events',
    adminDashboard: '/admin',
    adminEvents: '/admin/events',
    adminUsers: '/admin/users',
    memberDashboard: '/member/dashboard',
    memberEvents: '/member/events',
    profile: '/profile',
    admin: '/admin',
    base: ''
  },
  
  // Identity pages (Razor Pages, not Blazor)
  identityPages: [
    '/Identity/Account/Login',
    '/Identity/Account/Logout', 
    '/Identity/Account/Register',
    '/Identity/Account/ForgotPassword',
    '/Identity/Account/ResetPassword',
    '/Identity/Account/ConfirmEmail',
    '/Identity/Account/AccessDenied'
  ],
  
  // Timeouts
  timeouts: {
    navigation: 30000,
    action: 10000,
    assertion: 5000,
    blazorInit: 10000
  },
  
  // Test data patterns
  testData: {
    // Event creation defaults
    event: {
      namePrefix: 'Test Event',
      description: 'This is a test event created by Playwright E2E tests',
      location: 'Test Location',
      capacity: 20,
      price: 10.00
    },
    
    // User registration defaults
    registration: {
      firstNamePrefix: 'Test',
      lastNamePrefix: 'User',
      emailDomain: '@test.witchcityrope.com',
      defaultPassword: 'TestPass123!'
    }
  },
  
  // Screenshot settings
  screenshots: {
    enabled: true,
    onFailure: true,
    path: 'test-results/screenshots'
  },
  
  // Retry settings
  retries: {
    navigation: 3,
    action: 2
  }
};

/**
 * Generate unique test data to avoid conflicts
 */
export function generateTestData() {
  const timestamp = Date.now();
  const random = Math.floor(Math.random() * 1000);
  
  return {
    eventName: `${testConfig.testData.event.namePrefix} ${timestamp}`,
    userName: `${testConfig.testData.registration.firstNamePrefix}${random}`,
    userEmail: `test${timestamp}${testConfig.testData.registration.emailDomain}`,
    uniqueId: `${timestamp}-${random}`
  };
}