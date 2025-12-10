# API Error Handling Standard

**Created**: 2025-12-09
**Status**: Active
**Applies to**: All React frontend code

## Overview

This standard defines how API errors should be handled across the WitchCityRope React frontend. All components and hooks MUST follow this pattern for consistent user experience and maintainable code.

## Architecture Summary

```
API Request → apiClient (interceptor extracts error message) → Component/Hook
                                                                    ↓
                                                         error.message = API message
```

## Key Principle: Single API Client

**There is ONE canonical API client**: `/apps/web/src/lib/api/client.ts`

This client:
- Exports `apiClient` (named export)
- Includes RFC 9457 error message extraction in response interceptor
- Handles CSRF tokens, timeout, and security features
- Is the ONLY axios instance that should be used for API calls

**NEVER**:
- Create additional axios instances
- Import from any other api/client path
- Use raw axios directly

## Error Message Extraction

The `apiClient` interceptor automatically extracts user-friendly messages from RFC 9457 Problem Details responses:

```typescript
// Backend returns RFC 9457 Problem Details:
// {
//   "type": "https://tools.ietf.org/html/rfc9457",
//   "title": "Bad Request",
//   "status": 400,
//   "detail": "Event cannot be changed to Draft status because it started more than 2 hours ago"
// }

// Without extraction (axios default):
error.message === "Request failed with status code 400"

// With extraction (apiClient interceptor):
error.message === "Event cannot be changed to Draft status because it started more than 2 hours ago"
```

## Correct Usage Patterns

### Pattern 1: Using useApiMutation Hook (Recommended)

For new code, use the `useApiMutation` wrapper hook which provides automatic error notifications:

```typescript
import { useApiMutation } from '../../../lib/api'
import { apiClient } from '../../../lib/api/client'

const createEvent = useApiMutation(
  async (data: CreateEventRequest) => {
    const response = await apiClient.post('/api/events', data)
    return response.data
  },
  {
    successMessage: 'Event created successfully',
    onSuccess: () => navigate('/events'),
  }
)
```

### Pattern 2: Direct useMutation with Error Display

When you need custom error handling, the error message is already extracted:

```typescript
import { useMutation } from '@tanstack/react-query'
import { apiClient } from '../../../lib/api/client'
import { notifications } from '@mantine/notifications'

const updateEvent = useMutation({
  mutationFn: async (data) => {
    const response = await apiClient.put(`/api/events/${data.id}`, data)
    return response.data
  },
  onError: (error) => {
    // error.message is already the API's detail/title, not generic axios message
    notifications.show({
      title: 'Error',
      message: error.message, // "Event cannot be changed..."
      color: 'red',
    })
  },
})
```

### Pattern 3: Manual Error Handling

For special cases where you need manual error handling:

```typescript
import { extractErrorMessage } from '../../../lib/api/utils/errors'

try {
  await apiClient.post('/api/events', data)
} catch (error) {
  // Option A: Use error.message directly (interceptor already extracted)
  const message = error.message

  // Option B: Use extractErrorMessage for additional safety/fallback
  const message = extractErrorMessage(error)

  // Show to user
  setError(message)
}
```

## Anti-Patterns (What NOT to Do)

### Anti-Pattern 1: Generic Error Messages

```typescript
// WRONG - Ignores API's helpful error message
onError: (error) => {
  notifications.show({
    message: 'An error occurred', // Generic, unhelpful
  })
}
```

### Anti-Pattern 2: Manual Response Parsing

```typescript
// WRONG - Duplicates interceptor logic
onError: (error) => {
  const message = error.response?.data?.detail || error.response?.data?.title || error.message
  // This is already done by the interceptor!
}
```

### Anti-Pattern 3: Using Wrong API Client

```typescript
// WRONG - Don't import from these paths
import { api } from '../../api/client'  // DELETED
import axios from 'axios'               // Never use raw axios
```

## Migration Guide

If you find code using the old `api` client or incorrect error handling:

1. **Change import**:
   ```typescript
   // Before
   import { api } from '../../api/client'

   // After
   import { apiClient } from '../../lib/api/client'
   ```

2. **Update all usages**:
   ```typescript
   // Before
   api.get('/api/events')

   // After
   apiClient.get('/api/events')
   ```

3. **Simplify error handling**:
   ```typescript
   // Before
   onError: (error) => {
     const data = error.response?.data
     const message = data?.detail || data?.title || data?.message || error.message
     notifications.show({ message })
   }

   // After
   onError: (error) => {
     notifications.show({ message: error.message })
   }
   ```

## Related Documents

- Implementation Plan: `/docs/functional-areas/api-architecture-modernization/new-work/2025-12-09-error-handling-standardization/implementation-plan.md`
- Error Utilities: `/apps/web/src/lib/api/utils/errors.ts`
- API Client Source: `/apps/web/src/lib/api/client.ts`
- useApiMutation Hook: `/apps/web/src/lib/api/hooks/useApiMutation.ts`

## Enforcement

- ESLint rules prevent importing from deprecated paths
- TypeScript types enforce correct patterns
- Code review should verify error handling follows this standard
