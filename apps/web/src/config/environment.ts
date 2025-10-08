/**
 * Centralized Environment Configuration
 * 
 * This file provides environment-based configuration for all React application components.
 * It eliminates hard-coded ports and URLs throughout the codebase.
 * 
 * Usage:
 * ```typescript
 * import { config } from '@/config/environment';
 * const apiUrl = config.api.baseUrl;
 * ```
 */

export interface EnvironmentConfig {
  app: {
    title: string;
    version: string;
    environment: string;
    isDevelopment: boolean;
    isProduction: boolean;
  };
  ports: {
    web: number;
    api: number;
    database: number;
  };
  api: {
    baseUrl: string;
    internalPort: number;
    timeout: number;
  };
  features: {
    debug: boolean;
    devtools: boolean;
    msw: boolean;
    mockData: boolean;
    analytics: boolean;
  };
  auth: {
    cookieName: string;
    cookieDomain: string;
    cookieSecure: boolean;
  };
}

/**
 * Load environment configuration with fallbacks
 */
function loadEnvironmentConfig(): EnvironmentConfig {
  const env = import.meta.env;
  
  return {
    app: {
      title: env.VITE_APP_TITLE || 'WitchCityRope',
      version: env.VITE_APP_VERSION || '1.0.0',
      environment: env.VITE_APP_ENVIRONMENT || 'development',
      isDevelopment: env.MODE === 'development',
      isProduction: env.MODE === 'production',
    },
    ports: {
      web: parseInt(env.VITE_PORT || '5173', 10),
      api: parseInt(env.VITE_API_PORT || '5655', 10),
      database: parseInt(env.VITE_DB_PORT || '5433', 10),
    },
    api: {
      baseUrl: env.VITE_API_BASE_URL || 'http://localhost:5655',
      internalPort: parseInt(env.VITE_API_INTERNAL_PORT || '8080', 10),
      timeout: parseInt(env.VITE_API_TIMEOUT || '30000', 10),
    },
    features: {
      debug: env.VITE_ENABLE_DEBUG === 'true',
      devtools: env.VITE_ENABLE_DEVTOOLS === 'true',
      msw: env.VITE_ENABLE_MSW === 'true',
      mockData: env.VITE_ENABLE_MOCK_DATA === 'true',
      analytics: env.VITE_ENABLE_ANALYTICS === 'true',
    },
    auth: {
      cookieName: env.VITE_AUTH_COOKIE_NAME || 'WitchCityRope.Auth',
      cookieDomain: env.VITE_AUTH_COOKIE_DOMAIN || 'localhost',
      cookieSecure: env.VITE_AUTH_COOKIE_SECURE === 'true',
    },
  };
}

/**
 * Global configuration instance
 */
export const config = loadEnvironmentConfig();

/**
 * Helper functions for common operations
 */
export const configHelpers = {
  /**
   * Get full web application URL
   */
  getWebUrl: (path: string = '') => {
    const protocol = config.app.isDevelopment ? 'http' : 'https';
    const port = config.app.isDevelopment ? `:${config.ports.web}` : '';
    return `${protocol}://localhost${port}${path}`;
  },

  /**
   * Get full API URL with optional endpoint
   */
  getApiUrl: (endpoint: string = '') => {
    return `${config.api.baseUrl}${endpoint}`;
  },

  /**
   * Check if feature is enabled
   */
  isFeatureEnabled: (feature: keyof EnvironmentConfig['features']) => {
    return config.features[feature];
  },

  /**
   * Get database connection URL
   */
  getDatabaseUrl: () => {
    return `localhost:${config.ports.database}`;
  },
};

/**
 * Development helpers for debugging
 */
if (config.features.debug && config.app.isDevelopment) {
  console.log('🔧 Environment Configuration Loaded:', {
    environment: config.app.environment,
    webUrl: configHelpers.getWebUrl(),
    apiUrl: configHelpers.getApiUrl(),
    databaseUrl: configHelpers.getDatabaseUrl(),
    features: Object.entries(config.features)
      .filter(([, enabled]) => enabled)
      .map(([feature]) => feature),
  });
}