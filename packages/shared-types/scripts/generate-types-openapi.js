#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

async function checkApiHealth() {
    console.log('📡 Checking API availability...');
    
    // Try common API ports in order
    const apiPorts = ['5656', '5655', '5653'];
    
    for (const port of apiPorts) {
        try {
            const response = await fetch(`http://localhost:${port}/health`);
            if (response.ok) {
                console.log(`✅ API is running and healthy on port ${port}`);
                return port;
            }
        } catch (error) {
            console.log(`❌ No API found on port ${port}`);
        }
    }
    
    console.error('❌ API is not running on any expected port (5656, 5655, 5653)');
    console.error('Please start the API first using: ./dev.sh or cd apps/api && dotnet run');
    return false;
}

async function generateTypes() {
    console.log('🔄 Generating TypeScript types from API...');
    
    // Check which port the API is running on
    const apiPort = await checkApiHealth();
    const useTestFile = !apiPort;
    
    // Ensure generated directory exists
    const generatedDir = path.join(__dirname, '../src/generated');
    if (!fs.existsSync(generatedDir)) {
        fs.mkdirSync(generatedDir, { recursive: true });
    }

    // Generate types using openapi-typescript
    console.log('🏗️ Generating types with openapi-typescript...');
    
    const swaggerSource = useTestFile 
        ? path.join(__dirname, '../test-swagger.json')
        : `http://localhost:${apiPort}/swagger/v1/swagger.json`;
    
    const outputPath = path.join(__dirname, '../src/generated/api-types.ts');
    
    console.log(`📄 Using swagger source: ${swaggerSource}`);
    console.log(`📁 Output path: ${outputPath}`);
    
    return new Promise((resolve, reject) => {
        const args = ['openapi-typescript', swaggerSource, '--output', outputPath];
        console.log(`Running: npx ${args.join(' ')}`);
        
        const openapi = spawn('npx', args, {
            cwd: path.join(__dirname, '..'),
            stdio: 'inherit'
        });

        openapi.on('close', (code) => {
            if (code !== 0) {
                reject(new Error(`openapi-typescript generation failed with code ${code}`));
            } else {
                console.log('✅ openapi-typescript generation completed successfully');
                resolve();
            }
        });

        openapi.on('error', (error) => {
            reject(error);
        });
    });
}

async function createHelpers() {
    console.log('🔧 Creating helper functions...');
    
    const helpersContent = `/* eslint-disable */
/* tslint:disable */
/**
 * Helper functions for working with the generated API types
 * Generated on: ${new Date().toISOString()}
 */

import type { paths, components } from './api-types';

// Extract response types
export type ApiResponse<T extends keyof paths, M extends keyof paths[T]> = 
  paths[T][M] extends { responses: { 200: { content: { 'application/json': infer U } } } } 
    ? U 
    : never;

// Extract request body types
export type ApiRequestBody<T extends keyof paths, M extends keyof paths[T]> = 
  paths[T][M] extends { requestBody: { content: { 'application/json': infer U } } } 
    ? U 
    : never;

// Schema types
export type schemas = components['schemas'];

// Common types
export type UserDto = schemas['UserDto'];
export type EventDto = schemas['EventDto'];
export type LoginRequest = schemas['LoginRequest'];
export type LoginResponse = schemas['LoginResponse'];
export type RegisterRequest = schemas['RegisterRequest'];
export type AuthUserResponse = schemas['AuthUserResponse'];
export type UpdateEventRequest = schemas['UpdateEventRequest'];
export type EventDtoListApiResponse = schemas['EventDtoListApiResponse'];
export type EventDtoApiResponse = schemas['EventDtoApiResponse'];

// Dashboard types
export type AdminDashboardResponse = schemas['AdminDashboardResponse'];
export type ApplicationDetailResponse = schemas['ApplicationDetailResponse'];
export type ApplicationSummaryDto = schemas['ApplicationSummaryDto'];

// Safety types
export type CreateIncidentRequest = schemas['CreateIncidentRequest'];
export type IncidentResponse = schemas['IncidentResponse'];
export type SubmissionResponse = schemas['SubmissionResponse'];

// Check-in types
export type CheckInRequest = schemas['CheckInRequest'];
export type ManualEntryData = schemas['ManualEntryData'];

// Health types
export type HealthResponse = schemas['HealthResponse'];
export type DetailedHealthResponse = schemas['DetailedHealthResponse'];

// API operation types
export type GetCurrentUserResponse = ApiResponse<'/api/auth/current-user', 'get'>;
export type LoginApiRequest = ApiRequestBody<'/api/auth/login', 'post'>;
export type LoginApiResponse = ApiResponse<'/api/auth/login', 'post'>;
export type GetEventsResponse = ApiResponse<'/api/events', 'get'>;
export type UpdateEventApiRequest = ApiRequestBody<'/api/events/{id}', 'put'>;
export type UpdateEventApiResponse = ApiResponse<'/api/events/{id}', 'put'>;

// Type guards
export const isUserDto = (obj: any): obj is UserDto => {
  return obj && typeof obj === 'object' && 'id' in obj && 'email' in obj;
};

export const isEventDto = (obj: any): obj is EventDto => {
  return obj && typeof obj === 'object' && 'id' in obj && 'title' in obj;
};

// Error handling
export interface ApiError {
  message: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export const createApiError = (message: string, status?: number, errors?: Record<string, string[]>): ApiError => ({
  message,
  status,
  errors
});

export const getErrorMessage = (error: any): string => {
  if (error && typeof error === 'object') {
    if ('message' in error) return error.message;
    if ('title' in error) return error.title;
  }
  return 'An unknown error occurred';
};
`;

    const helpersPath = path.join(__dirname, '../src/generated/api-helpers.ts');
    fs.writeFileSync(helpersPath, helpersContent);
    
    console.log('✅ Helper functions created');
}

async function createClientWrapper() {
    console.log('🔧 Creating API client wrapper...');
    
    const clientContent = `/* eslint-disable */
/* tslint:disable */
/**
 * API Client wrapper for type-safe API calls
 * Generated on: ${new Date().toISOString()}
 */

import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse,
  RegisterRequest,
  AuthUserResponse,
  UpdateEventRequest,
  EventDtoListApiResponse,
  EventDtoApiResponse,
  AdminDashboardResponse,
  CreateIncidentRequest,
  SubmissionResponse,
  ApiError
} from './api-helpers';

const API_BASE_URL = process.env.VITE_API_BASE_URL || 'http://localhost:5656';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl.replace(/\\/$/, '');
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = \`\${this.baseUrl}\${endpoint}\`;
    
    const defaultHeaders = {
      'Content-Type': 'application/json',
    };

    const config: RequestInit = {
      ...options,
      headers: {
        ...defaultHeaders,
        ...options.headers,
      },
      credentials: 'include', // Important for cookie-based auth
    };

    try {
      const response = await fetch(url, config);
      
      if (!response.ok) {
        const error: ApiError = {
          message: \`HTTP error! status: \${response.status}\`,
          status: response.status
        };
        
        try {
          const errorData = await response.json();
          error.message = errorData.message || error.message;
          error.errors = errorData.errors;
        } catch (e) {
          // If we can't parse error response, use default message
        }
        
        throw error;
      }

      // Handle empty responses (204 No Content)
      if (response.status === 204) {
        return {} as T;
      }

      return await response.json();
    } catch (error) {
      if (error instanceof Error) {
        throw {
          message: error.message,
          status: 0
        } as ApiError;
      }
      throw error;
    }
  }

  // Auth endpoints
  async getCurrentUser(): Promise<AuthUserResponse> {
    return this.request<AuthUserResponse>('/api/auth/current-user');
  }

  async getUserFromCookie(): Promise<AuthUserResponse> {
    return this.request<AuthUserResponse>('/api/auth/user');
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    return this.request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  async register(data: RegisterRequest): Promise<AuthUserResponse> {
    return this.request<AuthUserResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async logout(): Promise<void> {
    await this.request<void>('/api/auth/logout', {
      method: 'POST',
    });
  }

  async refreshToken(): Promise<void> {
    await this.request<void>('/api/auth/refresh', {
      method: 'POST',
    });
  }

  // Events endpoints
  async getEvents(): Promise<EventDtoListApiResponse> {
    return this.request<EventDtoListApiResponse>('/api/events');
  }

  async getEvent(id: string): Promise<EventDtoApiResponse> {
    return this.request<EventDtoApiResponse>(\`/api/events/\${id}\`);
  }

  async updateEvent(id: string, event: UpdateEventRequest): Promise<EventDtoApiResponse> {
    return this.request<EventDtoApiResponse>(\`/api/events/\${id}\`, {
      method: 'PUT',
      body: JSON.stringify(event),
    });
  }

  // Health endpoints
  async getHealth(): Promise<any> {
    return this.request<any>('/api/health');
  }

  async getDetailedHealth(): Promise<any> {
    return this.request<any>('/api/health/detailed');
  }

  // Safety endpoints
  async submitIncident(incident: CreateIncidentRequest): Promise<SubmissionResponse> {
    return this.request<SubmissionResponse>('/api/safety/incidents', {
      method: 'POST',
      body: JSON.stringify(incident),
    });
  }

  // User profile endpoints
  async getUserProfile(): Promise<UserDto> {
    return this.request<UserDto>('/api/users/profile');
  }

  // Dashboard endpoints (admin only)
  async getSafetyDashboard(): Promise<AdminDashboardResponse> {
    return this.request<AdminDashboardResponse>('/api/safety/admin/dashboard');
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
export { ApiClient };
export type { ApiError };
`;

    const clientPath = path.join(__dirname, '../src/generated/api-client.ts');
    fs.writeFileSync(clientPath, clientContent);
    
    console.log('✅ API client wrapper created');
}

async function updateIndexFile() {
    const indexContent = `// Auto-generated API types and client
export * from './api-types';
export * from './api-helpers';
export * from './api-client';
export * from './version';
`;
    
    const indexPath = path.join(__dirname, '../src/generated/index.ts');
    fs.writeFileSync(indexPath, indexContent);
}

async function createVersionFile() {
    const versionFile = path.join(__dirname, '../src/generated/version.ts');
    const packageJson = JSON.parse(fs.readFileSync(path.join(__dirname, '../package.json'), 'utf8'));
    
    const versionContent = `// Auto-generated version information
export const SHARED_TYPES_VERSION = '${packageJson.version}';
export const GENERATED_AT = '${new Date().toISOString()}';
export const API_VERSION = 'v1';

// Runtime version checking utility
export function checkApiCompatibility(serverVersion: string): boolean {
    // For now, just check if major version matches
    const [major] = API_VERSION.substring(1).split('.');
    const [serverMajor] = serverVersion.substring(1).split('.');
    return major === serverMajor;
}
`;
    
    fs.writeFileSync(versionFile, versionContent);
    console.log('✅ Version file created');
}

async function validateTypes() {
    console.log('✅ Validating generated types...');
    
    return new Promise((resolve, reject) => {
        const tsc = spawn('npx', ['tsc', '--noEmit'], {
            cwd: path.join(__dirname, '..'),
            stdio: 'inherit'
        });

        tsc.on('close', (code) => {
            if (code !== 0) {
                console.warn('⚠️ TypeScript validation found issues, but continuing...');
                // Don't fail on TypeScript errors during generation
                resolve();
            } else {
                console.log('✅ TypeScript validation passed');
                resolve();
            }
        });

        tsc.on('error', (error) => {
            console.warn('⚠️ TypeScript validation skipped:', error.message);
            resolve();
        });
    });
}

async function main() {
    try {
        await generateTypes();
        await createHelpers();
        await createClientWrapper();
        await createVersionFile();
        await updateIndexFile();
        await validateTypes();
        
        console.log('🎉 Type generation completed successfully!');
        console.log('📁 Generated types are available in src/generated/');
        console.log('📁 Main exports: api-types.ts, api-helpers.ts, api-client.ts');
    } catch (error) {
        console.error('❌ Type generation failed:', error.message);
        process.exit(1);
    }
}

// Allow running directly
if (require.main === module) {
    main();
}

module.exports = { generateTypes, checkApiHealth };