/* eslint-disable */
/* tslint:disable */
/**
 * Helper functions for working with the generated API types
 * Generated on: 2025-08-19T07:04:09.881Z
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
export type CreateEventRequest = schemas['CreateEventRequest'];
export type EventListResponse = schemas['EventListResponse'];
export type UserRole = schemas['UserRole'];
export type EventType = schemas['EventType'];
export type EventStatus = schemas['EventStatus'];

// API operation types
export type GetCurrentUserResponse = ApiResponse<'/api/auth/me', 'get'>;
export type LoginApiRequest = ApiRequestBody<'/api/auth/login', 'post'>;
export type LoginApiResponse = ApiResponse<'/api/auth/login', 'post'>;
export type GetEventsResponse = ApiResponse<'/api/events', 'get'>;
export type CreateEventApiRequest = ApiRequestBody<'/api/events', 'post'>;
export type CreateEventApiResponse = ApiResponse<'/api/events', 'post'>;

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
