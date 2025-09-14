// Auth types - use generated types from @witchcityrope/shared-types
export type { 
  UserDto, 
  LoginRequest, 
  LoginResponse
} from '@witchcityrope/shared-types'

// Local types not covered by generated types yet
export interface RegisterCredentials {
  email: string
  password: string
  sceneName: string
}

// Query keys for type safety
export type AuthQueryKey = 
  | ['auth']
  | ['auth', 'user']
  | ['auth', 'profile']
  | ['auth', 'me']