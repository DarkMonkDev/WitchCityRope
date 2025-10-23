/* eslint-disable */
/* tslint:disable */
/**
 * API Client wrapper for type-safe API calls
 * Generated on: 2025-10-23T22:31:39.804Z
 */

import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse,
  RegisterRequest,
  AuthUserResponse,
  UpdateEventRequest,
  ApiResponseOfListOfEventDto,
  ApiResponseOfEventDto,
  AdminDashboardResponse,
  CreateIncidentRequest,
  SubmissionResponse,
  ApiError
} from './api-helpers';

const API_BASE_URL = process.env.VITE_API_BASE_URL || 'http://localhost:5656';

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
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
          message: `HTTP error! status: ${response.status}`,
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
  async getEvents(): Promise<ApiResponseOfListOfEventDto> {
    return this.request<ApiResponseOfListOfEventDto>('/api/events');
  }

  async getEvent(id: string): Promise<ApiResponseOfEventDto> {
    return this.request<ApiResponseOfEventDto>(`/api/events/${id}`);
  }

  async updateEvent(id: string, event: UpdateEventRequest): Promise<ApiResponseOfEventDto> {
    return this.request<ApiResponseOfEventDto>(`/api/events/${id}`, {
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
