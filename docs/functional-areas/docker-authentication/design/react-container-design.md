# React Container Design for Docker Implementation

<!-- Last Updated: 2025-08-17 -->
<!-- Version: 1.0 -->
<!-- Owner: React Developer Agent -->
<!-- Status: Active -->

## Overview

This document provides comprehensive design specifications for containerizing the React application with Vite build tool, preserving hot reload functionality for development while optimizing for production deployment. The design supports multi-stage builds, proper authentication integration, and optimal performance.

## Current React Application Context

### Existing Setup
- **Build Tool**: Vite 5.3.1 with React plugin
- **Port**: 5173 (development server)
- **API Proxy**: localhost:5653 → localhost:5655 (actual API port)
- **Dependencies**: Chakra UI, TanStack Query, React Router v7, Zustand
- **Authentication**: HttpOnly cookies + JWT pattern
- **Styling**: Tailwind CSS v4 + Chakra UI

### Key Requirements
- Preserve Vite HMR (Hot Module Replacement) in development
- Multi-stage builds for optimal production images
- Authentication cookie handling across container boundaries
- Environment variable injection for different stages
- Performance optimization for production

## 1. DOCKERFILE DESIGN (Multi-stage)

### Complete Multi-Stage Dockerfile

```dockerfile
# apps/web/Dockerfile

###########################################
# Base Node.js Image
###########################################
FROM node:20-alpine AS base
WORKDIR /app

# Install dependencies needed for node-gyp and native modules
RUN apk add --no-cache \
    python3 \
    make \
    g++ \
    libc6-compat

# Copy package files
COPY package*.json ./
COPY tsconfig.json ./

###########################################
# Dependencies Stage
###########################################
FROM base AS deps

# Install all dependencies (including devDependencies for build)
RUN npm ci --include=dev

###########################################
# Development Stage
###########################################
FROM base AS development

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules

# Copy source code
COPY . .

# Expose development port and HMR port
EXPOSE 5173

# Development environment variables
ENV NODE_ENV=development
ENV VITE_HOST=0.0.0.0
ENV VITE_PORT=5173

# Start Vite development server
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173"]

###########################################
# Build Stage
###########################################
FROM base AS builder

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules

# Copy source code
COPY . .

# Build arguments for environment configuration
ARG VITE_API_BASE_URL
ARG VITE_APP_TITLE="WitchCityRope"
ARG VITE_APP_VERSION="1.0.0"

# Set build environment
ENV NODE_ENV=production
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_TITLE=$VITE_APP_TITLE
ENV VITE_APP_VERSION=$VITE_APP_VERSION

# Build application
RUN npm run build

# Verify build output
RUN ls -la dist/

###########################################
# Production Stage
###########################################
FROM nginx:alpine AS production

# Install security tools
RUN apk add --no-cache curl

# Copy custom nginx configuration
COPY docker/nginx/nginx.conf /etc/nginx/nginx.conf
COPY docker/nginx/default.conf /etc/nginx/conf.d/default.conf

# Copy built application from builder stage
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration templates
COPY docker/nginx/templates/ /etc/nginx/templates/

# Create nginx user for security
RUN addgroup -S nginx_app && adduser -S nginx_app -G nginx_app

# Set ownership and permissions
RUN chown -R nginx_app:nginx_app /usr/share/nginx/html
RUN chown -R nginx_app:nginx_app /var/cache/nginx
RUN chown -R nginx_app:nginx_app /var/log/nginx

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

###########################################
# Test Stage (for CI/CD)
###########################################
FROM base AS test

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules

# Copy source code
COPY . .

# Run tests
RUN npm run test -- --run --coverage

# Run linting
RUN npm run lint

# Run type checking
RUN npx tsc --noEmit
```

## 2. VITE HOT RELOAD CONFIGURATION

### Container-Optimized Vite Configuration

```typescript
// apps/web/vite.config.ts (Enhanced for containers)
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig(({ mode }) => {
  const isProduction = mode === 'production'
  const isDevelopment = mode === 'development'
  
  return {
    plugins: [
      react({
        // Enable Fast Refresh in development
        fastRefresh: isDevelopment,
      })
    ],
    
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
        '@components': path.resolve(__dirname, './src/components'),
        '@pages': path.resolve(__dirname, './src/pages'),
        '@hooks': path.resolve(__dirname, './src/hooks'),
        '@utils': path.resolve(__dirname, './src/utils'),
        '@services': path.resolve(__dirname, './src/services'),
        '@types': path.resolve(__dirname, './src/types'),
      },
    },
    
    // Development server configuration
    server: {
      host: '0.0.0.0', // Required for container access
      port: 5173,
      strictPort: true, // Fail if port is not available
      
      // HMR Configuration for containers
      hmr: {
        port: 5173,
        host: 'localhost', // HMR WebSocket host
      },
      
      // File watching configuration for containers
      watch: {
        usePolling: true, // Required for Docker volume mounts
        interval: 100, // Polling interval in milliseconds
        ignored: ['**/node_modules/**', '**/dist/**'],
      },
      
      // API proxy configuration
      proxy: {
        '/api': {
          target: process.env.VITE_API_BASE_URL || 'http://api:8080',
          changeOrigin: true,
          secure: false,
          configure: (proxy, options) => {
            // Custom proxy configuration for containers
            proxy.on('error', (err, req, res) => {
              console.log('Proxy error:', err);
            });
          },
        },
      },
    },
    
    // Build configuration
    build: {
      outDir: 'dist',
      sourcemap: !isProduction,
      
      // Optimization settings
      rollupOptions: {
        output: {
          // Manual chunk splitting for better caching
          manualChunks: {
            vendor: ['react', 'react-dom'],
            router: ['react-router-dom'],
            ui: ['@chakra-ui/react'],
            query: ['@tanstack/react-query'],
            forms: ['react-hook-form', 'zod'],
          },
        },
      },
      
      // Bundle size analysis
      reportCompressedSize: isProduction,
      chunkSizeWarningLimit: 1000,
    },
    
    // Development optimizations
    optimizeDeps: {
      include: [
        'react',
        'react-dom',
        'react-router-dom',
        '@chakra-ui/react',
        '@tanstack/react-query',
      ],
    },
    
    // Environment variable handling
    define: {
      __APP_VERSION__: JSON.stringify(process.env.VITE_APP_VERSION || '1.0.0'),
    },
  }
})
```

### WebSocket Configuration for HMR

```typescript
// src/vite-env.d.ts (Enhanced with HMR types)
/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  readonly VITE_APP_TITLE: string
  readonly VITE_APP_VERSION: string
  readonly VITE_HOST: string
  readonly VITE_PORT: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

// HMR type extensions for container development
declare global {
  interface Window {
    __VITE_HMR_CONFIG__: {
      host: string
      port: number
    }
  }
}
```

## 3. ENVIRONMENT CONFIGURATIONS

### Development Configuration

```bash
# apps/web/.env.development
NODE_ENV=development
VITE_API_BASE_URL=http://localhost:5655
VITE_APP_TITLE=WitchCityRope (Dev)
VITE_APP_VERSION=1.0.0-dev
VITE_HOST=0.0.0.0
VITE_PORT=5173

# Development flags
VITE_ENABLE_DEBUG=true
VITE_ENABLE_DEVTOOLS=true
VITE_LOG_LEVEL=debug
```

```bash
# apps/web/.env.development.local (for container overrides)
# API URL for container-to-container communication
VITE_API_BASE_URL=http://api:8080

# Container-specific settings
VITE_CONTAINER_MODE=true
VITE_FILE_WATCHING=polling
```

### Test Configuration

```bash
# apps/web/.env.test
NODE_ENV=test
VITE_API_BASE_URL=http://localhost:5655
VITE_APP_TITLE=WitchCityRope (Test)
VITE_APP_VERSION=1.0.0-test

# Test-specific settings
VITE_ENABLE_MSW=true
VITE_TEST_USER_EMAIL=admin@witchcityrope.com
VITE_TEST_USER_PASSWORD=Test123!
```

### Production Configuration

```bash
# apps/web/.env.production
NODE_ENV=production
VITE_APP_TITLE=WitchCityRope
VITE_APP_VERSION=1.0.0

# Production flags (no debug info)
VITE_ENABLE_DEBUG=false
VITE_ENABLE_DEVTOOLS=false
VITE_LOG_LEVEL=error

# Analytics and monitoring
VITE_ENABLE_ANALYTICS=true
VITE_SENTRY_DSN=${SENTRY_DSN}
```

## 4. BUILD OPTIMIZATION

### Docker Build Context and Optimization

```dockerfile
# .dockerignore for React app
node_modules
npm-debug.log*
yarn-debug.log*
yarn-error.log*
.git
.gitignore
README.md
.env
.env.local
.env.development.local
.env.test.local
.env.production.local
coverage
.nyc_output
.cache
dist
build

# Development files
*.tsbuildinfo
.vscode
.idea
*.swp
*.swo
*~

# OS files
.DS_Store
Thumbs.db
```

### Multi-Stage Build Optimization

```bash
# Build script with caching optimization
#!/bin/bash
# docker/scripts/build-react.sh

set -e

echo "🏗️ Building React application with optimized caching..."

# Build development image
docker build \
  --target development \
  --cache-from witchcityrope/web:dev-cache \
  --tag witchcityrope/web:dev \
  --file apps/web/Dockerfile \
  apps/web

# Build production image with build args
docker build \
  --target production \
  --cache-from witchcityrope/web:builder-cache \
  --cache-from witchcityrope/web:prod-cache \
  --build-arg VITE_API_BASE_URL=$VITE_API_BASE_URL \
  --build-arg VITE_APP_VERSION=$VITE_APP_VERSION \
  --tag witchcityrope/web:latest \
  --file apps/web/Dockerfile \
  apps/web

echo "✅ React application build complete"
```

## 5. NGINX CONFIGURATION (Production)

### Main Nginx Configuration

```nginx
# docker/nginx/nginx.conf
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log notice;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
    use epoll;
    multi_accept on;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Logging format
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';

    access_log /var/log/nginx/access.log main;

    # Performance settings
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 100M;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types
        text/plain
        text/css
        text/xml
        text/javascript
        application/json
        application/javascript
        application/xml+rss
        application/atom+xml
        image/svg+xml;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Content Security Policy
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ${API_BASE_URL};" always;

    include /etc/nginx/conf.d/*.conf;
}
```

### Site Configuration

```nginx
# docker/nginx/default.conf
server {
    listen 80;
    listen [::]:80;
    server_name localhost;

    root /usr/share/nginx/html;
    index index.html;

    # Security headers for API communication
    location /api/ {
        # Proxy to API service
        proxy_pass ${API_BASE_URL}/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # CORS headers for browser requests
        add_header Access-Control-Allow-Origin $http_origin always;
        add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS" always;
        add_header Access-Control-Allow-Headers "Authorization, Content-Type, X-Requested-With" always;
        add_header Access-Control-Allow-Credentials true always;

        # Handle preflight requests
        if ($request_method = 'OPTIONS') {
            add_header Access-Control-Allow-Origin $http_origin;
            add_header Access-Control-Allow-Methods "GET, POST, PUT, DELETE, OPTIONS";
            add_header Access-Control-Allow-Headers "Authorization, Content-Type, X-Requested-With";
            add_header Access-Control-Allow-Credentials true;
            add_header Content-Length 0;
            add_header Content-Type text/plain;
            return 204;
        }
    }

    # React application routes
    location / {
        try_files $uri $uri/ /index.html;
        
        # Cache static assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
        
        # Don't cache the main HTML file
        location = /index.html {
            add_header Cache-Control "no-cache, no-store, must-revalidate";
            add_header Pragma "no-cache";
            add_header Expires "0";
        }
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 '{"status":"healthy","service":"react-app"}';
        add_header Content-Type application/json;
    }

    # Security: deny access to sensitive files
    location ~ /\. {
        deny all;
    }

    location ~ \.(conf|log)$ {
        deny all;
    }

    # Custom error pages
    error_page 404 /index.html;
    error_page 500 502 503 504 /50x.html;
    
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
```

## 6. ENVIRONMENT VARIABLE HANDLING

### Environment Variable Strategy

```typescript
// src/config/environment.ts
interface EnvironmentConfig {
  // API Configuration
  apiBaseUrl: string
  
  // Application metadata
  appTitle: string
  appVersion: string
  
  // Feature flags
  enableDebug: boolean
  enableDevTools: boolean
  
  // Authentication
  jwtStorageKey: string
  
  // Container-specific
  containerMode: boolean
}

export const env: EnvironmentConfig = {
  // API URL with fallback
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  
  // App metadata
  appTitle: import.meta.env.VITE_APP_TITLE || 'WitchCityRope',
  appVersion: import.meta.env.VITE_APP_VERSION || '1.0.0',
  
  // Feature flags
  enableDebug: import.meta.env.VITE_ENABLE_DEBUG === 'true',
  enableDevTools: import.meta.env.VITE_ENABLE_DEVTOOLS === 'true',
  
  // Security
  jwtStorageKey: 'wcr_auth_token',
  
  // Container detection
  containerMode: import.meta.env.VITE_CONTAINER_MODE === 'true',
}

// Runtime environment validation
export const validateEnvironment = (): void => {
  const required = ['VITE_API_BASE_URL']
  
  for (const key of required) {
    if (!import.meta.env[key]) {
      throw new Error(`Missing required environment variable: ${key}`)
    }
  }
}

// Development helpers
export const isDevelopment = import.meta.env.DEV
export const isProduction = import.meta.env.PROD
export const isTest = import.meta.env.MODE === 'test'
```

### Build-time vs Runtime Variables

```typescript
// src/utils/config.ts - Runtime configuration
export class ConfigService {
  private config: Record<string, string> = {}

  constructor() {
    // Load build-time variables
    this.config = {
      API_BASE_URL: import.meta.env.VITE_API_BASE_URL,
      APP_VERSION: import.meta.env.VITE_APP_VERSION,
      // Add other build-time variables
    }
  }

  // Method to update runtime configuration
  updateConfig(newConfig: Partial<Record<string, string>>): void {
    this.config = { ...this.config, ...newConfig }
  }

  get(key: string, defaultValue?: string): string {
    return this.config[key] || defaultValue || ''
  }

  // Specific getters for type safety
  get apiBaseUrl(): string {
    return this.get('API_BASE_URL')
  }

  get appVersion(): string {
    return this.get('APP_VERSION')
  }
}

export const configService = new ConfigService()
```

## 7. AUTHENTICATION INTEGRATION

### Container-Aware Authentication Service

```typescript
// src/services/authService.ts - Enhanced for containers
export class AuthService {
  private baseUrl: string
  private containerMode: boolean

  constructor() {
    this.baseUrl = env.apiBaseUrl
    this.containerMode = env.containerMode
  }

  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    const response = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        // Container-specific headers
        ...(this.containerMode && {
          'X-Container-Request': 'true',
        }),
      },
      body: JSON.stringify(credentials),
      credentials: 'include', // Critical for HttpOnly cookies
    })

    if (!response.ok) {
      throw new Error(`Authentication failed: ${response.status}`)
    }

    const data = await response.json()
    
    // Store JWT in memory (not localStorage)
    this.setToken(data.token)
    
    return data
  }

  private setToken(token: string): void {
    // Store in memory only (XSS protection)
    this.token = token
    
    // Configure axios defaults if using axios
    if (typeof window !== 'undefined') {
      // Browser environment
      this.configureHttpClient(token)
    }
  }

  private configureHttpClient(token: string): void {
    // Set default headers for API requests
    const defaultHeaders = {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    }

    // Apply to fetch wrapper or axios instance
    this.defaultHeaders = defaultHeaders
  }

  async makeAuthenticatedRequest<T>(
    url: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const response = await fetch(`${this.baseUrl}${url}`, {
      ...options,
      headers: {
        ...this.defaultHeaders,
        ...options.headers,
      },
      credentials: 'include', // Include cookies
    })

    if (response.status === 401) {
      // Token expired - redirect to login
      this.logout()
      throw new Error('Authentication required')
    }

    if (!response.ok) {
      throw new Error(`Request failed: ${response.status}`)
    }

    return response.json()
  }

  logout(): void {
    // Clear memory token
    this.token = null
    this.defaultHeaders = {}
    
    // Call logout endpoint to clear server-side session
    fetch(`${this.baseUrl}/api/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    }).catch(err => {
      console.warn('Logout request failed:', err)
    })
  }

  private token: string | null = null
  private defaultHeaders: Record<string, string> = {}
}
```

### Cookie Configuration for Containers

```typescript
// src/config/cookies.ts
export const COOKIE_CONFIG = {
  // Development (container-aware)
  development: {
    secure: false, // HTTP in development
    sameSite: 'lax' as const,
    domain: undefined, // Allow cross-container communication
  },
  
  // Production
  production: {
    secure: true, // HTTPS in production
    sameSite: 'strict' as const,
    domain: process.env.VITE_COOKIE_DOMAIN,
  },
  
  // Common settings
  httpOnly: true, // Set by server
  maxAge: 24 * 60 * 60 * 1000, // 24 hours
}

// Get appropriate cookie config for current environment
export const getCookieConfig = () => {
  return isProduction ? COOKIE_CONFIG.production : COOKIE_CONFIG.development
}
```

## 8. PERFORMANCE OPTIMIZATION

### Image Size Reduction

```dockerfile
# Optimized production stage with size reduction
FROM nginx:alpine AS production

# Remove unnecessary packages and clean cache
RUN apk del --purge curl-dev && \
    rm -rf /var/cache/apk/* && \
    rm -rf /tmp/*

# Use multi-stage copy to minimize layers
COPY --from=builder --chown=nginx:nginx /app/dist /usr/share/nginx/html

# Optimize nginx binary
RUN nginx -t && \
    nginx -s reload
```

### Build Performance

```bash
# Build optimization script
#!/bin/bash
# docker/scripts/optimize-build.sh

echo "🚀 Optimizing React build performance..."

# Use BuildKit for better caching
export DOCKER_BUILDKIT=1

# Build with cache mount for node_modules
docker build \
  --cache-mount=type=cache,target=/app/node_modules \
  --cache-mount=type=cache,target=/app/.npm \
  --target production \
  --tag witchcityrope/web:optimized \
  apps/web

# Analyze bundle size
docker run --rm -it \
  -v $(pwd)/apps/web/dist:/app/dist \
  witchcityrope/web:optimized \
  sh -c "du -sh /app/dist/* | sort -h"

echo "✅ Build optimization complete"
```

### Runtime Performance

```typescript
// src/utils/performance.ts
export class PerformanceMonitor {
  private static instance: PerformanceMonitor
  
  static getInstance(): PerformanceMonitor {
    if (!PerformanceMonitor.instance) {
      PerformanceMonitor.instance = new PerformanceMonitor()
    }
    return PerformanceMonitor.instance
  }

  measurePageLoad(): void {
    if (typeof window !== 'undefined' && 'performance' in window) {
      window.addEventListener('load', () => {
        const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
        
        const metrics = {
          // Core Web Vitals
          firstContentfulPaint: this.getFCP(),
          largestContentfulPaint: this.getLCP(),
          cumulativeLayoutShift: this.getCLS(),
          
          // Navigation timing
          domContentLoaded: navigation.domContentLoadedEventEnd - navigation.navigationStart,
          loadComplete: navigation.loadEventEnd - navigation.navigationStart,
          
          // Resource timing
          totalResources: performance.getEntriesByType('resource').length,
        }

        // Send to analytics in production
        if (isProduction) {
          this.sendMetrics(metrics)
        } else {
          console.log('Performance metrics:', metrics)
        }
      })
    }
  }

  private getFCP(): number {
    const fcpEntry = performance.getEntriesByName('first-contentful-paint')[0]
    return fcpEntry ? fcpEntry.startTime : 0
  }

  private getLCP(): number {
    return new Promise((resolve) => {
      new PerformanceObserver((list) => {
        const entries = list.getEntries()
        const lastEntry = entries[entries.length - 1]
        resolve(lastEntry.startTime)
      }).observe({ entryTypes: ['largest-contentful-paint'] })
    })
  }

  private getCLS(): number {
    return new Promise((resolve) => {
      let clsValue = 0
      new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (!(entry as any).hadRecentInput) {
            clsValue += (entry as any).value
          }
        }
        resolve(clsValue)
      }).observe({ entryTypes: ['layout-shift'] })
    })
  }

  private sendMetrics(metrics: any): void {
    // Send to analytics service
    fetch('/api/analytics/performance', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(metrics),
    }).catch(err => console.warn('Failed to send metrics:', err))
  }
}
```

## Docker Compose Integration

### Development Service Configuration

```yaml
# docker-compose.dev.yml (React service)
services:
  web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile
      target: development
    ports:
      - "5173:5173"
    volumes:
      # Source code for hot reload
      - ./apps/web/src:/app/src:cached
      - ./apps/web/public:/app/public:cached
      - ./apps/web/vite.config.ts:/app/vite.config.ts:cached
      - ./apps/web/package.json:/app/package.json:cached
      # Prevent node_modules override
      - web_node_modules:/app/node_modules
    environment:
      - NODE_ENV=development
      - VITE_API_BASE_URL=http://api:8080
      - VITE_HOST=0.0.0.0
      - VITE_PORT=5173
      - VITE_CONTAINER_MODE=true
    depends_on:
      - api
    networks:
      - witchcityrope-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5173/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  web_node_modules:
    driver: local
```

### Production Service Configuration

```yaml
# docker-compose.prod.yml (React service)
services:
  web:
    build:
      context: ./apps/web
      dockerfile: Dockerfile
      target: production
      args:
        VITE_API_BASE_URL: http://api:8080
        VITE_APP_VERSION: ${APP_VERSION:-1.0.0}
    ports:
      - "80:80"
    environment:
      - NODE_ENV=production
      - API_BASE_URL=http://api:8080
    depends_on:
      - api
    networks:
      - witchcityrope-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/health"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 30s
```

## Summary

This React container design provides:

### ✅ **Development Benefits**
- **Fast HMR**: Sub-second hot reload with file polling
- **Container Isolation**: Consistent environment across team
- **API Integration**: Seamless communication with containerized API
- **Debug Support**: Full debugging capabilities in containers

### ✅ **Production Optimizations**
- **Multi-stage Builds**: Minimal production image size (~50MB)
- **Nginx Optimization**: Gzip, caching, security headers
- **Bundle Splitting**: Optimal caching with chunked assets
- **Health Checks**: Comprehensive monitoring and reliability

### ✅ **Security Features**
- **JWT Memory Storage**: XSS protection with memory-only tokens
- **HttpOnly Cookies**: CSRF protection with server-side session
- **Content Security Policy**: Browser security headers
- **Container Security**: Non-root user and minimal attack surface

### ✅ **Performance Features**
- **Build Caching**: Docker layer and npm cache optimization
- **Asset Optimization**: Compression, minification, tree shaking
- **Runtime Monitoring**: Core Web Vitals and performance metrics
- **CDN Ready**: Optimized static asset serving

The design preserves the excellent Vite development experience while providing production-ready containerization with security, performance, and maintainability as key priorities.