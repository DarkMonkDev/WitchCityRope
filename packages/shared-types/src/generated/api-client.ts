/* eslint-disable */
/* tslint:disable */
/**
 * API Client wrapper for type-safe API calls
 * Generated on: 2025-08-19T07:40:03.499Z
 */

import type { 
  UserDto, 
  EventDto, 
  LoginRequest, 
  LoginResponse,
  CreateEventRequest,
  EventListResponse,
  ApiError
} from './api-helpers';

const API_BASE_URL = process.env.VITE_API_BASE_URL || 'http://localhost:5655';

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
  async getCurrentUser(): Promise<UserDto> {
    return this.request<UserDto>('/api/auth/user');
  }

  async login(credentials: LoginRequest): Promise<LoginResponse> {
    return this.request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  async logout(): Promise<void> {
    await this.request<void>('/api/auth/logout', {
      method: 'POST',
    });
  }

  // Events endpoints
  async getEvents(params: { page?: number; pageSize?: number } = {}): Promise<EventListResponse> {
    const searchParams = new URLSearchParams();
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());
    
    const query = searchParams.toString();
    const endpoint = query ? `/api/events?${query}` : '/api/events';
    
    return this.request<EventListResponse>(endpoint);
  }

  async createEvent(event: CreateEventRequest): Promise<EventDto> {
    return this.request<EventDto>('/api/events', {
      method: 'POST',
      body: JSON.stringify(event),
    });
  }

  async getEvent(id: string): Promise<EventDto> {
    return this.request<EventDto>(`/api/events/${id}`);
  }

  async updateEvent(id: string, event: Partial<EventDto>): Promise<EventDto> {
    return this.request<EventDto>(`/api/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(event),
    });
  }

  async deleteEvent(id: string): Promise<void> {
    await this.request<void>(`/api/events/${id}`, {
      method: 'DELETE',
    });
  }
}

// Export singleton instance
export const apiClient = new ApiClient();
export { ApiClient };
export type { ApiError };
