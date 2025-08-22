/**
 * Visual Test Scenarios for WitchCityRope
 * These scenarios define what pages and elements to capture for visual verification
 */

const testScenarios = [
  {
    name: "Landing Page Visual Test",
    description: "Verify the landing page appearance and key sections",
    steps: [
      { 
        url: "/", 
        waitFor: ".hero-section", 
        screenshot: true,
        elements: [".hero-section", ".upcoming-events", ".features-section"]
      },
      { 
        url: "/", 
        selector: ".upcoming-events", 
        screenshot: true,
        description: "Upcoming events section" 
      }
    ]
  },
  
  {
    name: "Authentication Flow",
    description: "Verify all authentication-related pages",
    steps: [
      { 
        url: "/login", 
        screenshot: true,
        waitFor: "form",
        description: "Login page with form"
      },
      { 
        url: "/register", 
        screenshot: true,
        waitFor: "form",
        description: "Registration page"
      },
      { 
        url: "/auth/2fa-setup", 
        screenshot: true,
        description: "Two-factor authentication setup"
      },
      {
        url: "/auth/forgot-password",
        screenshot: true,
        description: "Password reset request"
      }
    ]
  },
  
  {
    name: "Event Management",
    description: "Event listing and detail pages",
    steps: [
      { 
        url: "/events", 
        screenshot: true,
        waitFor: ".event-list",
        description: "Public events listing"
      },
      { 
        url: "/events/1", 
        screenshot: true,
        waitFor: ".event-detail",
        description: "Event detail page with registration"
      },
      { 
        url: "/admin/events", 
        screenshot: true,
        waitFor: ".admin-layout",
        description: "Admin event management",
        requiresAuth: true,
        role: "Admin"
      },
      {
        url: "/admin/events/create",
        screenshot: true,
        description: "Event creation form",
        requiresAuth: true,
        role: "Admin"
      }
    ]
  },
  
  {
    name: "Member Dashboard",
    description: "Member-specific pages and features",
    steps: [
      { 
        url: "/dashboard", 
        screenshot: true,
        waitFor: ".dashboard-content",
        description: "Member dashboard",
        requiresAuth: true
      },
      { 
        url: "/my-events", 
        screenshot: true,
        description: "My tickets and registrations",
        requiresAuth: true
      },
      { 
        url: "/profile", 
        screenshot: true,
        description: "Member profile page",
        requiresAuth: true
      },
      {
        url: "/settings",
        screenshot: true,
        description: "Account settings",
        requiresAuth: true
      }
    ]
  },
  
  {
    name: "Vetting Process",
    description: "Member vetting application flow",
    steps: [
      {
        url: "/vetting/apply",
        screenshot: true,
        waitFor: "form",
        description: "Vetting application form"
      },
      {
        url: "/admin/vetting",
        screenshot: true,
        description: "Admin vetting queue",
        requiresAuth: true,
        role: "Admin"
      },
      {
        url: "/admin/vetting/1",
        screenshot: true,
        description: "Vetting application review",
        requiresAuth: true,
        role: "Admin"
      }
    ]
  },
  
  {
    name: "Check-in System",
    description: "Event check-in interface",
    steps: [
      {
        url: "/events/1/checkin",
        screenshot: true,
        waitFor: ".checkin-interface",
        description: "Mobile-optimized check-in",
        requiresAuth: true,
        role: "Staff"
      }
    ]
  },
  
  {
    name: "Mobile Responsiveness",
    description: "Test responsive design across devices",
    viewports: [
      { width: 375, height: 667, name: "iPhone SE", deviceScaleFactor: 2 },
      { width: 390, height: 844, name: "iPhone 14", deviceScaleFactor: 3 },
      { width: 768, height: 1024, name: "iPad", deviceScaleFactor: 2 },
      { width: 1920, height: 1080, name: "Desktop Full HD", deviceScaleFactor: 1 },
      { width: 1366, height: 768, name: "Laptop", deviceScaleFactor: 1 }
    ],
    pages: [
      { url: "/", description: "Landing page responsive" },
      { url: "/events", description: "Events list responsive" },
      { url: "/dashboard", description: "Dashboard responsive", requiresAuth: true },
      { url: "/events/1/checkin", description: "Check-in mobile view", requiresAuth: true }
    ]
  },
  
  {
    name: "Error Pages",
    description: "Verify error page styling",
    steps: [
      { url: "/404", screenshot: true, description: "404 Not Found" },
      { url: "/403", screenshot: true, description: "403 Forbidden" },
      { url: "/500", screenshot: true, description: "500 Server Error" }
    ]
  },
  
  {
    name: "Dark Theme Support",
    description: "Verify dark theme appearance",
    colorScheme: "dark",
    steps: [
      { url: "/", screenshot: true, description: "Landing page dark theme" },
      { url: "/events", screenshot: true, description: "Events dark theme" },
      { url: "/dashboard", screenshot: true, description: "Dashboard dark theme", requiresAuth: true }
    ]
  },
  
  {
    name: "Form Interactions",
    description: "Capture form states and validation",
    steps: [
      {
        url: "/login",
        interactions: [
          { type: "focus", selector: "#email" },
          { type: "screenshot", name: "login-email-focused" },
          { type: "type", selector: "#email", value: "invalid-email" },
          { type: "blur", selector: "#email" },
          { type: "screenshot", name: "login-validation-error" }
        ]
      },
      {
        url: "/events/1",
        interactions: [
          { type: "click", selector: ".register-button" },
          { type: "waitFor", selector: ".registration-modal" },
          { type: "screenshot", name: "registration-modal-open" }
        ],
        requiresAuth: true
      }
    ]
  },
  
  {
    name: "Accessibility Verification",
    description: "Check accessibility features",
    steps: [
      {
        url: "/",
        accessibility: true,
        checks: ["color-contrast", "heading-order", "alt-text", "form-labels"]
      },
      {
        url: "/events",
        accessibility: true,
        checks: ["keyboard-navigation", "focus-indicators", "aria-labels"]
      }
    ]
  },
  
  {
    name: "Performance Critical Pages",
    description: "Pages that need performance monitoring",
    performance: true,
    pages: [
      { url: "/", metrics: ["FCP", "LCP", "CLS", "FID"] },
      { url: "/events", metrics: ["FCP", "LCP", "TTI"] },
      { url: "/admin/events", metrics: ["FCP", "LCP", "TTI"], requiresAuth: true }
    ]
  }
];

// Helper function to get scenarios by type
const getScenariosByType = (type) => {
  switch(type) {
    case 'critical':
      return testScenarios.filter(s => 
        ['Landing Page Visual Test', 'Authentication Flow', 'Event Management'].includes(s.name)
      );
    case 'mobile':
      return testScenarios.filter(s => s.name === 'Mobile Responsiveness');
    case 'admin':
      return testScenarios.filter(s => 
        s.steps?.some(step => step.role === 'Admin') || s.name.includes('Admin')
      );
    case 'accessibility':
      return testScenarios.filter(s => s.name === 'Accessibility Verification');
    default:
      return testScenarios;
  }
};

// Export for use in other scripts
module.exports = {
  testScenarios,
  getScenariosByType
};