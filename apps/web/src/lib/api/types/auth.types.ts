// Auth types extending existing service types
export interface LoginCredentials {
  email: string
  password: string
}

export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

export interface LoginResponse {
  user: UserDto
  token: string
}

export interface UserDto {
  id: string
  email: string
  sceneName: string
  role?: string
  status?: string
  createdAt: string
  lastLoginAt?: string
}

// Query keys for type safety
export type AuthQueryKey = 
  | ['auth']
  | ['auth', 'user']
  | ['auth', 'profile']
  | ['auth', 'me']