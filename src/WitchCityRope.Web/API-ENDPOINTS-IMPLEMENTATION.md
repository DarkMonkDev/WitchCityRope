# API Endpoints Implementation Summary

## Overview
This document summarizes the API endpoints that have been implemented to satisfy the integration test requirements.

## Implemented Endpoints

### 1. Events Controller (`/api/events`)

#### GET /api/events
- **Description**: Get all events with pagination
- **Authorization**: Not required
- **Parameters**: 
  - `page` (optional, default: 1)
  - `pageSize` (optional, default: 20)
- **Response**: List of events

#### POST /api/events
- **Description**: Create a new event
- **Authorization**: Required
- **Request Body**: `CreateEventRequest`
- **Response**: `CreateEventResponse` with event ID

#### GET /api/events/{id}
- **Description**: Get specific event by ID
- **Authorization**: Not required
- **Supports**: Both GUID and integer IDs for backward compatibility
- **Response**: Event details

#### POST /api/events/{id}/rsvp
- **Description**: RSVP to an event
- **Authorization**: Required
- **Request Body**: `RsvpRequest` (optional)
- **Supports**: Both GUID and integer IDs for backward compatibility
- **Response**: `RsvpResponse` or `RegistrationResult`

#### GET /api/events/upcoming
- **Description**: Get top 5 upcoming events
- **Authorization**: Not required
- **Response**: List of upcoming events

### 2. User Controller (`/api/user`)

#### GET /api/user/profile
- **Description**: Get the authenticated user's profile
- **Authorization**: Required
- **Response**: `UserDto` with user profile information

### 3. Users Controller (`/api/users`)

#### GET /api/users/me/rsvps
- **Description**: Get the authenticated user's RSVPs
- **Authorization**: Required
- **Response**: List of `RsvpDto` objects

### 4. Health Check Controller (`/api/healthcheck`)

#### GET /api/healthcheck/endpoints
- **Description**: Get a list of all available API endpoints
- **Authorization**: Not required
- **Response**: JSON object listing all endpoints with their requirements

## Authentication

All endpoints marked with "Authorization: Required" need a valid JWT token in the Authorization header:
```
Authorization: Bearer <JWT_TOKEN>
```

## Response Formats

### Success Responses
- **200 OK**: Successful GET requests
- **201 Created**: Successful POST requests that create resources
- **204 No Content**: Successful requests with no response body

### Error Responses
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Missing or invalid authentication
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server-side errors

## Testing

A test script is available at `test-api-endpoints.sh` to verify the endpoints are working correctly.

## Notes

1. The API uses both GUID and integer IDs for events to maintain backward compatibility
2. All authenticated endpoints extract the user ID from the JWT claims
3. Error responses include descriptive error messages in JSON format
4. The ApiClient service handles the actual communication with the backend API service