# API Integration Research

*Generated on August 13, 2025*

## Overview
This document researches API integration strategies, HTTP clients, and server state management for the WitchCityRope migration from Blazor Server to React.

## Current WitchCityRope API Architecture

### Existing API Implementation
- **Architecture**: Separate API service (ASP.NET Core Minimal API)
- **Communication Pattern**: Web service → HTTP → API service
- **Port Configuration**: API runs on localhost:5653, Web on localhost:5651
- **Authentication**: JWT-based with refresh token pattern
- **Data Format**: JSON with typed DTOs
- **Error Handling**: Standardized ApiResponse wrapper

### Current API Endpoints Analysis
```csharp
// Authentication endpoints
POST /auth/login
POST /auth/register
POST /auth/refresh
POST /auth/logout
POST /auth/verify-email

// User management
GET /admin/users
POST /admin/users
PUT /admin/users/{id}
DELETE /admin/users/{id}

// Event management
GET /events
POST /events
PUT /events/{id}
DELETE /events/{id}
POST /events/{id}/register

// Dashboard and analytics
GET /dashboard/stats
GET /dashboard/activity

// Member management
GET /members
GET /members/{id}
PUT /members/{id}

// Vetting system
GET /vetting/applications
POST /vetting/applications
PUT /vetting/applications/{id}
```

### Current Error Handling Pattern
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
}
```

## HTTP Client Comparison (2025)

### **Recommended: Axios (Feature-Rich)**

#### **Why Axios for WitchCityRope**
- **Complex Application**: WitchCityRope has dozens of API endpoints and complex interactions
- **Authentication**: Built-in interceptor support for JWT token management
- **Error Handling**: Automatic error throwing for 4xx/5xx responses
- **Developer Experience**: Intuitive API and extensive feature set
- **Community**: Large ecosystem with plugins and extensions

#### **Axios Implementation**
```typescript
import axios, { AxiosInstance, AxiosError } from 'axios';
import { useAuthStore } from '@/stores/authStore';

// Create axios instance with base configuration
const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5653',
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json',
    },
    withCredentials: true, // Include cookies for authentication
  });

  // Request interceptor for authentication
  client.interceptors.request.use(
    (config) => {
      const token = useAuthStore.getState().accessToken;
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Response interceptor for error handling and token refresh
  client.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config;
      
      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;
        
        try {
          await useAuthStore.getState().refreshToken();
          const newToken = useAuthStore.getState().accessToken;
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return client(originalRequest);
        } catch (refreshError) {
          useAuthStore.getState().logout();
          window.location.href = '/auth/login';
          return Promise.reject(refreshError);
        }
      }
      
      return Promise.reject(error);
    }
  );

  return client;
};

export const apiClient = createApiClient();
```

#### **Retry Logic with axios-retry**
```typescript
import axiosRetry from 'axios-retry';

// Configure automatic retries
axiosRetry(apiClient, {
  retries: 3,
  retryDelay: (retryCount) => {
    return retryCount * 1000; // Progressive delay: 1s, 2s, 3s
  },
  retryCondition: (error) => {
    // Retry on network errors or 5xx responses
    return axiosRetry.isNetworkOrIdempotentRequestError(error) ||
           (error.response?.status >= 500);
  },
  onRetry: (retryCount, error, requestConfig) => {
    console.log(`Retrying request ${requestConfig.url} (attempt ${retryCount})`);
  }
});
```

### **Alternative: Fetch API (Lightweight)**

#### **When to Consider Fetch**
- **Bundle Size**: Critical for performance-sensitive applications
- **Simple Requirements**: Basic HTTP operations without complex features
- **Native Support**: No external dependencies

#### **Enhanced Fetch Implementation**
```typescript
// Enhanced fetch wrapper with Axios-like features
class ApiClient {
  private baseURL: string;
  private defaultHeaders: Record<string, string>;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
    this.defaultHeaders = {
      'Content-Type': 'application/json',
    };
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    
    const config: RequestInit = {
      ...options,
      headers: {
        ...this.defaultHeaders,
        ...options.headers,
      },
      credentials: 'include',
    };

    // Add authentication token
    const token = useAuthStore.getState().accessToken;
    if (token) {
      config.headers = {
        ...config.headers,
        Authorization: `Bearer ${token}`,
      };
    }

    try {
      const response = await this.fetchWithRetry(url, config);
      
      if (!response.ok) {
        if (response.status === 401) {
          await this.handleUnauthorized();
          // Retry with new token
          return this.request(endpoint, options);
        }
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      return response.json();
    } catch (error) {
      console.error('API request failed:', error);
      throw error;
    }
  }

  private async fetchWithRetry(
    url: string,
    options: RequestInit,
    retries = 3
  ): Promise<Response> {
    try {
      return await fetch(url, options);
    } catch (error) {
      if (retries > 0) {
        await new Promise(resolve => setTimeout(resolve, 1000));
        return this.fetchWithRetry(url, options, retries - 1);
      }
      throw error;
    }
  }

  private async handleUnauthorized() {
    try {
      await useAuthStore.getState().refreshToken();
    } catch (error) {
      useAuthStore.getState().logout();
      window.location.href = '/auth/login';
    }
  }

  // API methods
  get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  put<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }
}

export const apiClient = new ApiClient(
  process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5653'
);
```

## Server State Management (2025)

### **Recommended: TanStack Query**

#### **Why TanStack Query for WitchCityRope**
- **Complex Data Requirements**: Admin dashboards, event management, user profiles
- **Caching Strategy**: Minimize API calls for better performance
- **Real-time Updates**: Event registrations, member status changes
- **Optimistic Updates**: Better UX for form submissions
- **Developer Experience**: Excellent TypeScript support and dev tools

#### **TanStack Query Implementation**
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from './apiClient';

// Query keys factory for consistency
export const queryKeys = {
  all: ['wcr'] as const,
  events: () => [...queryKeys.all, 'events'] as const,
  event: (id: string) => [...queryKeys.events(), id] as const,
  users: () => [...queryKeys.all, 'users'] as const,
  user: (id: string) => [...queryKeys.users(), id] as const,
  dashboard: () => [...queryKeys.all, 'dashboard'] as const,
  vetting: () => [...queryKeys.all, 'vetting'] as const,
};

// Event management hooks
export const useEvents = (filters?: EventFilters) => {
  return useQuery({
    queryKey: [...queryKeys.events(), filters],
    queryFn: () => apiClient.get<Event[]>('/events', { params: filters }),
    staleTime: 5 * 60 * 1000, // Consider fresh for 5 minutes
    refetchOnWindowFocus: false,
  });
};

export const useEvent = (id: string) => {
  return useQuery({
    queryKey: queryKeys.event(id),
    queryFn: () => apiClient.get<Event>(`/events/${id}`),
    enabled: !!id,
  });
};

export const useCreateEvent = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (eventData: CreateEventRequest) =>
      apiClient.post<Event>('/events', eventData),
    onSuccess: (newEvent) => {
      // Invalidate events list to trigger refetch
      queryClient.invalidateQueries({ queryKey: queryKeys.events() });
      
      // Optimistically update event cache
      queryClient.setQueryData(queryKeys.event(newEvent.id), newEvent);
    },
    onError: (error) => {
      console.error('Failed to create event:', error);
      // Show error notification
    },
  });
};

// User management with optimistic updates
export const useUpdateUser = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: ({ id, userData }: { id: string; userData: UpdateUserRequest }) =>
      apiClient.put<User>(`/admin/users/${id}`, userData),
    
    // Optimistic update for better UX
    onMutate: async ({ id, userData }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.user(id) });
      
      const previousUser = queryClient.getQueryData(queryKeys.user(id));
      
      queryClient.setQueryData(queryKeys.user(id), (old: User) => ({
        ...old,
        ...userData,
      }));
      
      return { previousUser };
    },
    
    onError: (error, variables, context) => {
      // Rollback optimistic update
      if (context?.previousUser) {
        queryClient.setQueryData(
          queryKeys.user(variables.id),
          context.previousUser
        );
      }
    },
    
    onSettled: (data, error, { id }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user(id) });
    },
  });
};

// Dashboard data with automatic refresh
export const useDashboardStats = () => {
  return useQuery({
    queryKey: queryKeys.dashboard(),
    queryFn: () => apiClient.get<DashboardStats>('/dashboard/stats'),
    refetchInterval: 30 * 1000, // Refresh every 30 seconds
    refetchIntervalInBackground: true,
  });
};
```

### **Alternative: SWR (Simpler)**

#### **When to Consider SWR**
- **Simplicity**: Smaller bundle size and simpler API
- **Performance**: Excellent caching with minimal overhead
- **Smaller Team**: Less complex data requirements

#### **SWR Implementation**
```typescript
import useSWR, { mutate } from 'swr';
import { apiClient } from './apiClient';

// Generic fetcher function
const fetcher = (url: string) => apiClient.get(url);

// Event hooks with SWR
export const useEvents = (filters?: EventFilters) => {
  const queryString = filters ? `?${new URLSearchParams(filters).toString()}` : '';
  const { data, error, isLoading } = useSWR<Event[]>(
    `/events${queryString}`,
    fetcher,
    {
      revalidateOnFocus: false,
      refreshInterval: 30000, // 30 seconds
    }
  );

  return {
    events: data,
    isLoading,
    isError: error,
  };
};

export const useCreateEvent = () => {
  const createEvent = async (eventData: CreateEventRequest) => {
    const newEvent = await apiClient.post<Event>('/events', eventData);
    
    // Revalidate events list
    mutate('/events');
    
    return newEvent;
  };

  return { createEvent };
};
```

## API Service Layer Architecture

### **Type-Safe API Client**
```typescript
// API response types matching C# ApiResponse
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

// Domain entity types
interface User {
  id: string;
  email: string;
  sceneName: string;
  roles: UserRole[];
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

interface Event {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  capacity: number;
  registrations: Registration[];
  status: EventStatus;
  prerequisites?: string;
  instructorId: string;
}

// API service classes
class AuthService {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<ApiResponse<LoginResponse>>(
      '/auth/login',
      credentials
    );
    return response.data;
  }

  async register(userData: RegisterRequest): Promise<RegisterResponse> {
    const response = await apiClient.post<ApiResponse<RegisterResponse>>(
      '/auth/register',
      userData
    );
    return response.data;
  }

  async refreshToken(): Promise<string> {
    const response = await apiClient.post<ApiResponse<{ token: string }>>(
      '/auth/refresh'
    );
    return response.data.token;
  }
}

class EventService {
  async getEvents(filters?: EventFilters): Promise<Event[]> {
    const response = await apiClient.get<ApiResponse<Event[]>>('/events', {
      params: filters,
    });
    return response.data;
  }

  async createEvent(eventData: CreateEventRequest): Promise<Event> {
    const response = await apiClient.post<ApiResponse<Event>>(
      '/events',
      eventData
    );
    return response.data;
  }

  async updateEvent(id: string, eventData: UpdateEventRequest): Promise<Event> {
    const response = await apiClient.put<ApiResponse<Event>>(
      `/events/${id}`,
      eventData
    );
    return response.data;
  }

  async registerForEvent(eventId: string, registrationData: EventRegistrationRequest): Promise<Registration> {
    const response = await apiClient.post<ApiResponse<Registration>>(
      `/events/${eventId}/register`,
      registrationData
    );
    return response.data;
  }
}

class UserService {
  async getUsers(filters?: UserFilters): Promise<User[]> {
    const response = await apiClient.get<ApiResponse<User[]>>('/admin/users', {
      params: filters,
    });
    return response.data;
  }

  async updateUser(id: string, userData: UpdateUserRequest): Promise<User> {
    const response = await apiClient.put<ApiResponse<User>>(
      `/admin/users/${id}`,
      userData
    );
    return response.data;
  }

  async deleteUser(id: string): Promise<void> {
    await apiClient.delete(`/admin/users/${id}`);
  }
}

// Export service instances
export const authService = new AuthService();
export const eventService = new EventService();
export const userService = new UserService();
```

## Error Handling Strategy

### **Global Error Handling**
```typescript
// Error types
interface ApiError {
  message: string;
  status: number;
  code?: string;
  details?: any;
}

// Error boundary for API errors
class ApiErrorBoundary extends Component<
  { children: ReactNode; fallback?: ComponentType<{ error: Error }> },
  { hasError: boolean; error?: Error }
> {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error to monitoring service
    console.error('API Error caught by boundary:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      const FallbackComponent = this.props.fallback || DefaultErrorFallback;
      return <FallbackComponent error={this.state.error} />;
    }

    return this.props.children;
  }
}

// Error notification hook
export const useErrorHandler = () => {
  const showToast = useToast();

  const handleError = useCallback((error: ApiError | Error) => {
    if (error instanceof Error) {
      showToast({
        title: 'Error',
        description: error.message,
        status: 'error',
      });
    } else {
      showToast({
        title: 'API Error',
        description: error.message,
        status: 'error',
      });
    }
  }, [showToast]);

  return { handleError };
};
```

### **Retry and Circuit Breaker Patterns**
```typescript
// Circuit breaker for API resilience
class CircuitBreaker {
  private failures = 0;
  private nextAttempt = Date.now();
  private state: 'CLOSED' | 'OPEN' | 'HALF_OPEN' = 'CLOSED';

  constructor(
    private failureThreshold = 5,
    private resetTimeout = 60000
  ) {}

  async call<T>(fn: () => Promise<T>): Promise<T> {
    if (this.state === 'OPEN') {
      if (this.nextAttempt <= Date.now()) {
        this.state = 'HALF_OPEN';
      } else {
        throw new Error('Circuit breaker is OPEN');
      }
    }

    try {
      const result = await fn();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  private onSuccess() {
    this.failures = 0;
    this.state = 'CLOSED';
  }

  private onFailure() {
    this.failures++;
    if (this.failures >= this.failureThreshold) {
      this.state = 'OPEN';
      this.nextAttempt = Date.now() + this.resetTimeout;
    }
  }
}

const apiCircuitBreaker = new CircuitBreaker();

// Use circuit breaker in API calls
export const robustApiCall = async <T>(fn: () => Promise<T>): Promise<T> => {
  return apiCircuitBreaker.call(fn);
};
```

## Real-Time Integration

### **WebSocket Integration with TanStack Query**
```typescript
// WebSocket connection for real-time updates
class WebSocketManager {
  private ws: WebSocket | null = null;
  private listeners = new Map<string, Function[]>();

  connect() {
    this.ws = new WebSocket(process.env.NEXT_PUBLIC_WS_URL || 'ws://localhost:5653/ws');
    
    this.ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      this.notifyListeners(data.type, data.payload);
    };
  }

  subscribe(eventType: string, callback: Function) {
    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, []);
    }
    this.listeners.get(eventType)!.push(callback);
  }

  private notifyListeners(eventType: string, payload: any) {
    const callbacks = this.listeners.get(eventType) || [];
    callbacks.forEach(callback => callback(payload));
  }
}

// Real-time updates hook
export const useRealTimeUpdates = () => {
  const queryClient = useQueryClient();
  const wsManager = useRef(new WebSocketManager());

  useEffect(() => {
    const ws = wsManager.current;
    ws.connect();

    // Listen for event updates
    ws.subscribe('event_updated', (event: Event) => {
      queryClient.setQueryData(queryKeys.event(event.id), event);
      queryClient.invalidateQueries({ queryKey: queryKeys.events() });
    });

    // Listen for user updates
    ws.subscribe('user_updated', (user: User) => {
      queryClient.setQueryData(queryKeys.user(user.id), user);
      queryClient.invalidateQueries({ queryKey: queryKeys.users() });
    });

    return () => {
      // Cleanup WebSocket connection
    };
  }, [queryClient]);
};
```

## Performance Optimization

### **Request Deduplication**
```typescript
// Automatic request deduplication with TanStack Query
export const useEventWithDeduplication = (id: string) => {
  return useQuery({
    queryKey: queryKeys.event(id),
    queryFn: () => eventService.getEvent(id),
    staleTime: 5 * 60 * 1000,
    // Multiple components requesting same data will share the request
  });
};
```

### **Pagination and Infinite Queries**
```typescript
// Infinite scrolling for large datasets
export const useInfiniteUsers = (filters?: UserFilters) => {
  return useInfiniteQuery({
    queryKey: [...queryKeys.users(), filters],
    queryFn: ({ pageParam = 0 }) =>
      userService.getUsers({ ...filters, page: pageParam, pageSize: 20 }),
    getNextPageParam: (lastPage, pages) => {
      return lastPage.length === 20 ? pages.length : undefined;
    },
    initialPageParam: 0,
  });
};
```

## Migration Strategy

### **Phase 1: HTTP Client Setup (Week 1)**
1. **Install Dependencies**: Axios, TanStack Query, axios-retry
2. **Create API Client**: Base configuration with interceptors
3. **Authentication Integration**: JWT token management
4. **Error Handling**: Global error boundaries and handlers

### **Phase 2: Core API Integration (Week 2-3)**
1. **Authentication APIs**: Login, register, refresh token
2. **User Management**: Admin user operations
3. **Event Management**: CRUD operations for events
4. **Dashboard APIs**: Stats and analytics

### **Phase 3: Advanced Features (Week 4-5)**
1. **Optimistic Updates**: Better UX for form submissions
2. **Real-Time Updates**: WebSocket integration
3. **Caching Strategy**: Fine-tune cache invalidation
4. **Performance Optimization**: Request deduplication, pagination

### **Phase 4: Production Readiness (Week 6)**
1. **Error Monitoring**: Integration with error tracking
2. **Performance Monitoring**: API call analytics
3. **Load Testing**: Ensure scalability
4. **Documentation**: API integration guide

## Recommended Technology Stack

### **Primary Recommendation**
```typescript
const apiStack = {
  httpClient: 'Axios with interceptors',
  serverState: 'TanStack Query',
  retryLogic: 'axios-retry',
  errorHandling: 'Global error boundaries + toast notifications',
  typeScript: 'Full type safety with interface definitions',
  realTime: 'WebSocket integration with query invalidation'
};
```

### **Benefits for WitchCityRope**
1. **Robust Error Handling**: Automatic retry and fallback mechanisms
2. **Performance**: Intelligent caching and request deduplication
3. **Developer Experience**: Excellent TypeScript support and dev tools
4. **Scalability**: Handles complex data relationships efficiently
5. **Real-Time**: WebSocket integration for live updates

## Conclusion

For the WitchCityRope migration, **Axios + TanStack Query** provides the optimal balance of features, performance, and developer experience:

1. **Enterprise-Grade**: Robust error handling and retry mechanisms
2. **Performance**: Intelligent caching and optimistic updates
3. **Type Safety**: Full TypeScript integration with existing API
4. **Real-Time**: WebSocket support for live event updates
5. **Maintainability**: Clean separation of concerns and testable code

**Key Success Factors**:
- Maintain compatibility with existing .NET API
- Implement proper error handling and user feedback
- Optimize for WitchCityRope's data-heavy admin interfaces
- Ensure real-time updates for event registrations and user management
- Focus on performance for community-scale usage patterns

This approach ensures seamless integration with the existing API while providing modern React patterns for optimal user experience and developer productivity.