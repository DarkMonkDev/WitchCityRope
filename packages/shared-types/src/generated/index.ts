// Auto-generated API types and client
export * from './api-types';
export * from './api-helpers';
export * from './api-client';
export * from './version';

import type { components } from './api-types';

// Extended types for compatibility with current frontend usage
export type ExtendedUserDto = components['schemas']['UserDto'] & {
  emailConfirmed?: boolean;
  phoneNumber?: string | null;
};

// Re-export extended types
export type { ExtendedUserDto as UserDto };
