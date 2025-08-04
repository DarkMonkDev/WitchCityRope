// Lighthouse CI configuration for performance and accessibility testing
// Integrates with the CI/CD pipeline for comprehensive quality checks

module.exports = {
  ci: {
    collect: {
      // URLs to test
      url: [
        'http://localhost:5651',
        'http://localhost:5651/login',
        'http://localhost:5651/register',
        'http://localhost:5651/events',
        'http://localhost:5651/admin/dashboard'
      ],
      
      // Collection settings
      numberOfRuns: process.env.CI ? 2 : 3,
      startServerCommand: process.env.CI ? null : 'echo "Server should be running"',
      startServerReadyPattern: 'ready',
      startServerReadyTimeout: 30000,
      
      // Chrome settings optimized for CI
      chromePath: process.env.CHROME_PATH,
      settings: {
        chromeFlags: [
          '--no-sandbox',
          '--disable-dev-shm-usage',
          '--disable-web-security',
          '--disable-features=TranslateUI',
          '--headless'
        ],
        
        // Skip certain audits that are less relevant for this application
        skipAudits: [
          'canonical', // We don't use canonical URLs
          'robots-txt', // Not applicable for member-only site
          'offline-start', // PWA offline functionality not implemented
          'installable-manifest' // Not a PWA
        ],
        
        // Performance budget
        budgets: [{
          resourceSizes: [{
            resourceType: 'script',
            budget: 500 // 500KB max for JS
          }, {
            resourceType: 'stylesheet',  
            budget: 100 // 100KB max for CSS
          }, {
            resourceType: 'image',
            budget: 300 // 300KB max for images
          }],
          
          resourceCounts: [{
            resourceType: 'third-party',
            budget: 10 // Max 10 third-party resources
          }]
        }]
      }
    },
    
    assert: {
      // Performance assertions
      assertions: {
        'categories:performance': ['error', { minScore: 0.7 }],
        'categories:accessibility': ['error', { minScore: 0.9 }],
        'categories:best-practices': ['error', { minScore: 0.8 }],
        'categories:seo': ['warn', { minScore: 0.7 }],
        
        // Specific metric assertions
        'first-contentful-paint': ['warn', { maxNumericValue: 2000 }],
        'largest-contentful-paint': ['error', { maxNumericValue: 4000 }],
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'total-blocking-time': ['warn', { maxNumericValue: 300 }],
        
        // Accessibility assertions
        'color-contrast': 'error',
        'heading-order': 'error',
        'aria-valid-attr': 'error',
        'button-name': 'error',
        'link-name': 'error',
        'image-alt': 'error'
      }
    },
    
    upload: {
      target: process.env.CI ? 'temporary-public-storage' : 'filesystem',
      
      // For CI environments with GitHub Actions
      ...(process.env.GITHUB_ACTIONS && {
        target: 'temporary-public-storage',
        githubAppToken: process.env.LHCI_GITHUB_APP_TOKEN,
        githubToken: process.env.GITHUB_TOKEN
      }),
      
      // For local development
      ...(!process.env.CI && {
        outputDir: './lighthouse-results',
        reportFilenamePattern: '%%HOSTNAME%%-%%PATHNAME%%-%%DATETIME%%.report.json'
      })
    },
    
    server: {
      // Don't start a server in CI - Docker handles it
      ...(process.env.CI ? {} : {
        command: 'echo "Using existing server"',
        port: 5651,
        timeout: 10000
      })
    }
  }
};

// Environment-specific overrides
if (process.env.NODE_ENV === 'development') {
  // More lenient settings for development
  module.exports.ci.assert.assertions['categories:performance'][1].minScore = 0.6;
  module.exports.ci.collect.numberOfRuns = 1;
}

if (process.env.LIGHTHOUSE_STRICT === 'true') {
  // Stricter settings for production deployment
  module.exports.ci.assert.assertions['categories:performance'][1].minScore = 0.9;
  module.exports.ci.assert.assertions['categories:accessibility'][1].minScore = 0.95;
}