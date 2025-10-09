# Custom ESLint Rules

## Overview

This directory contains custom ESLint rules specific to the WitchCityRope project. These rules enforce architectural patterns and prevent common mistakes that have caused production issues.

## Rules

### `no-silent-api-errors`

**Created**: 2025-10-09

**Purpose**: Prevent silent API error handling patterns that mask real problems from users and developers.

**Context**: After discovering and removing 5 instances of silent 404 handlers that masked API/database issues, we need automated detection to prevent developers from reintroducing these patterns.

---

## Rule: `no-silent-api-errors`

### What This Rule Catches

This rule detects and flags four problematic patterns:

#### 1. Silent 404 Returns

**Problem**: Returning empty values when API returns 404, hiding errors from users

```typescript
// ❌ BAD: Silent 404 return
async function getUser(id: string) {
  try {
    const response = await api.get(`/users/${id}`);
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      return; // or return {}; or return [];
    }
    throw error;
  }
}
```

**Why It's Bad**:
- Users see empty state without knowing why
- API/database problems go undetected
- Wrong data types (sequential IDs vs GUIDs) break routing
- Debugging becomes nearly impossible when real issues are masked

**Correct Solutions**:

```typescript
// ✅ GOOD: Throw specific error for UI to handle
async function getUser(id: string) {
  try {
    const response = await api.get(`/users/${id}`);
    return response.data;
  } catch (error: any) {
    if (error.response?.status === 404) {
      throw new Error(`User with ID "${id}" was not found.`);
    }
    throw error;
  }
}

// ✅ GOOD: Return null with justification comment for existence checks
async function checkUserExists(id: string): Promise<boolean> {
  try {
    await api.get(`/users/${id}`);
    return true;
  } catch (error: any) {
    // Intentional: Checking existence
    if (error.response?.status === 404) {
      return false;
    }
    throw error;
  }
}
```

#### 2. Console.warn Instead of Throw

**Problem**: Using console.warn in catch blocks instead of throwing errors

```typescript
// ❌ BAD: console.warn masks errors
async function getApplications() {
  try {
    return await api.get('/applications');
  } catch (error) {
    console.warn('API error:', error);
    return mockData; // Hides real problems!
  }
}
```

**Why It's Bad**:
- Users get fake data instead of error feedback
- Developers don't see failures in error tracking
- Production issues hidden in console logs

**Correct Solutions**:

```typescript
// ✅ GOOD: Log and throw
async function getApplications() {
  try {
    return await api.get('/applications');
  } catch (error) {
    console.error('Failed to fetch applications:', error);
    throw error; // Let error boundary handle it
  }
}

// ✅ GOOD: Log and notify user
async function getApplications() {
  try {
    return await api.get('/applications');
  } catch (error: any) {
    console.error('Failed to fetch applications:', error);

    if (error.response?.status === 401) {
      throw new Error('Authentication required. Please log in.');
    } else if (error.response?.status >= 500) {
      throw new Error('Server error. Please try again later.');
    }

    throw error;
  }
}
```

#### 3. Silent Mutation Errors

**Problem**: React Query mutation onError handlers that don't notify users

```typescript
// ❌ BAD: User not notified of failure
const mutation = useMutation({
  mutationFn: createEvent,
  onError: (error) => {
    console.log(error); // Only logs, no user feedback!
  }
});
```

**Why It's Bad**:
- Users think operation succeeded when it failed
- No feedback or guidance for users
- Poor user experience

**Correct Solutions**:

```typescript
// ✅ GOOD: Notify user with toast
import { notifications } from '@mantine/notifications';

const mutation = useMutation({
  mutationFn: createEvent,
  onError: (error: any) => {
    console.error('Failed to create event:', error);

    notifications.show({
      title: 'Error',
      message: error.message || 'Failed to create event. Please try again.',
      color: 'red',
    });
  },
  onSuccess: () => {
    notifications.show({
      title: 'Success',
      message: 'Event created successfully',
      color: 'green',
    });
  }
});

// ✅ GOOD: Use error state in component
const mutation = useMutation({
  mutationFn: createEvent,
});

// In component
{mutation.error && (
  <Alert color="red" title="Error">
    {mutation.error.message || 'Failed to create event'}
  </Alert>
)}
```

#### 4. Empty Returns in Catch Blocks

**Problem**: Returning empty arrays/objects in catch blocks without justification

```typescript
// ❌ BAD: Empty return masks API failure
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery({
    queryKey: ['applications', filters],
    queryFn: async () => {
      try {
        const result = await api.getApplications(filters);
        if (!result || !result.items || result.items.length === 0) {
          return { items: sampleApplications, ... }; // MASKS PROBLEMS!
        }
        return result;
      } catch (error) {
        return { items: [], totalCount: 0 }; // HIDES ERRORS!
      }
    }
  });
}
```

**Why It's Bad**:
- API failures look like "no data" to users
- Production incidents harder to detect
- Users see confusing empty states

**Correct Solutions**:

```typescript
// ✅ GOOD: Let errors propagate to React Query error handling
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery({
    queryKey: ['applications', filters],
    queryFn: async () => {
      console.log('Fetching applications with filters:', filters);

      try {
        const result = await api.getApplications(filters);

        console.log('API response received:', {
          hasResult: !!result,
          itemCount: result?.items?.length || 0,
          totalCount: result?.totalCount || 0
        });

        // Return actual API result or proper empty result
        if (!result) {
          console.warn('API returned null/undefined result');
          return {
            items: [],
            totalCount: 0,
            pageSize: filters.pageSize,
            pageNumber: filters.page,
            totalPages: 0
          };
        }

        return result;
      } catch (error: any) {
        console.error('API call failed:', {
          error: error.message || error,
          status: error.response?.status,
          filters
        });

        // Enhance error message based on HTTP status
        if (error.response?.status === 401) {
          throw new Error('Authentication required. Please log in.');
        } else if (error.response?.status === 403) {
          throw new Error('Access denied. Insufficient permissions.');
        }

        // Re-throw the error to let React Query handle it properly
        throw error;
      }
    },
    throwOnError: true, // Ensure errors are thrown
  });
}

// Component handles errors explicitly
const { data, isLoading, error, refetch } = useVettingApplications(filters);

if (error) {
  return (
    <Paper p="xl" radius="md">
      <Text c="red" ta="center">
        Error loading applications: {error.message}
      </Text>
      <Group justify="center" mt="md">
        <Button onClick={() => refetch()}>
          Try Again
        </Button>
      </Group>
    </Paper>
  );
}
```

---

## Configuration

### Basic Setup

Add the custom rule to your ESLint config:

```javascript
// eslint.config.js
import noSilentApiErrors from './eslint-rules/no-silent-api-errors.js';

export default [
  {
    plugins: {
      'local': {
        rules: {
          'no-silent-api-errors': noSilentApiErrors,
        }
      }
    },
    rules: {
      'local/no-silent-api-errors': 'error',
    }
  }
];
```

### Advanced Configuration

Configure allowed exceptions:

```javascript
rules: {
  'local/no-silent-api-errors': ['error', {
    // Functions allowed to return null/empty on 404 (existence checks)
    allowedFunctions: [
      'checkUserExists',
      'checkExistingApplication',
      'getVettingStatus',
      'findOptionalResource'
    ],

    // Allow console.warn in development (not recommended)
    allowConsoleInDev: false
  }]
}
```

---

## Handling Legitimate Cases

### Existence Checks

When you need to check if a resource exists (not fetch it), you can return null on 404:

```typescript
// ✅ GOOD: Justified null return for existence check
async function checkExistingApplication(userId: string): Promise<boolean> {
  try {
    await api.get(`/applications/${userId}`);
    return true;
  } catch (error: any) {
    // Intentional: Checking existence
    if (error.response?.status === 404) {
      return false;
    }
    throw error;
  }
}
```

**Two ways to satisfy the rule**:

1. **Add to allowedFunctions** in ESLint config
2. **Add justification comment** above the return statement:
   ```typescript
   // Intentional: Checking existence
   if (error.response?.status === 404) {
     return null;
   }
   ```

Accepted justification patterns:
- `// Intentional: ...`
- `// Checking existence`
- `// Expected behavior: ...`
- `// By design: ...`
- `// Legitimate null return: ...`

### Optional Resource Fetching

When fetching optional data that may not exist:

```typescript
// ✅ GOOD: Explicit null return with justification
async function getOptionalUserProfile(userId: string): Promise<Profile | null> {
  try {
    const response = await api.get(`/profiles/${userId}`);
    return response.data;
  } catch (error: any) {
    // Intentional: Profile is optional, null means no profile exists
    if (error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}

// Component handles null case explicitly
const profile = await getOptionalUserProfile(userId);
if (profile === null) {
  return <Text>No profile found. Create one to get started!</Text>;
}
```

---

## Testing Your Code

### Running ESLint

```bash
# Check all files
npm run lint

# Auto-fix issues (if fixable)
npm run lint -- --fix

# Check specific file
npx eslint src/services/myService.ts
```

### Common Fixes

**For silent 404 returns:**
- Throw specific error with helpful message
- Add justification comment if legitimate
- Add function to allowedFunctions config

**For console.warn in catch:**
- Change to console.error and throw
- Add user notification (toast/alert)
- Let error boundary handle it

**For silent mutation errors:**
- Add notifications.show() in onError
- Add error display in component UI
- Use mutation.error state

**For empty returns in catch:**
- Let errors propagate to React Query
- Use throwOnError: true
- Handle errors explicitly in component

---

## Examples from Real Code

### Before (BAD)

```typescript
// Real example that was removed from production
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery({
    queryKey: ['applications', filters],
    queryFn: async () => {
      try {
        const result = await api.getApplicationsForReview(filters);
        // If API returns no data, provide sample data
        if (!result || !result.items || result.items.length === 0) {
          return { items: sampleApplications }; // MASKS PROBLEMS!
        }
        return result;
      } catch (error) {
        // Fallback to sample data if API fails
        return { items: sampleApplications }; // HIDES ERRORS!
      }
    }
  });
}

// Result: Users clicked rows and got URLs like /admin/vetting/applications/2
// instead of /admin/vetting/applications/guid-here because sample data
// used sequential IDs (1, 2, 3) instead of actual GUIDs from database
```

### After (GOOD)

```typescript
export function useVettingApplications(filters: ApplicationFilterRequest) {
  return useQuery({
    queryKey: ['applications', filters],
    queryFn: async () => {
      console.log('Fetching applications with filters:', filters);

      try {
        const result = await api.getApplicationsForReview(filters);

        console.log('API response received:', {
          hasResult: !!result,
          itemCount: result?.items?.length || 0,
          totalCount: result?.totalCount || 0
        });

        if (!result) {
          console.warn('API returned null/undefined result');
          return {
            items: [],
            totalCount: 0,
            pageSize: filters.pageSize,
            pageNumber: filters.page,
            totalPages: 0
          };
        }

        return result;
      } catch (error: any) {
        console.error('API call failed:', {
          error: error.message || error,
          status: error.response?.status,
          filters
        });

        if (error.response?.status === 401) {
          throw new Error('Authentication required. Please log in.');
        } else if (error.response?.status === 403) {
          throw new Error('Access denied.');
        }

        throw error; // Let React Query handle it
      }
    },
    throwOnError: true,
  });
}

// Component shows proper error UI
const { data, error } = useVettingApplications(filters);

if (error) {
  return (
    <Alert color="red" title="Error Loading Applications">
      {error.message}
      <Button onClick={() => refetch()}>Try Again</Button>
    </Alert>
  );
}
```

---

## FAQ

### Q: Why is this rule so strict?

**A**: Silent error handling caused real production issues:
- Users saw empty data instead of error messages
- API/database problems went undetected
- Wrong data types (sequential IDs vs GUIDs) broke routing
- Debugging became nearly impossible

### Q: What if I really need to return empty data on error?

**A**: Ask yourself:
1. Is this an existence check? → Add justification comment
2. Is this optional data? → Return null with justification
3. Is this required data? → Throw error and let UI handle it

If you truly need to return empty data, add a justification comment explaining why.

### Q: Can I disable this rule for a file?

**A**: Yes, but only if absolutely necessary:

```typescript
/* eslint-disable local/no-silent-api-errors */
// Code that needs exception
/* eslint-enable local/no-silent-api-errors */
```

**Better**: Fix the code to follow patterns or add justification comments.

### Q: What about development/mock data?

**A**: Mock data is acceptable in:
- Test files (`.test.ts`, `.spec.ts`)
- Storybook stories
- Demo environments with clear labeling

**NEVER** in production hooks, services, or API clients.

---

## Related Documentation

- **Lessons Learned**: `/docs/lessons-learned/react-developer-lessons-learned-part-2.md` - "FALLBACK/MOCK DATA IN PRODUCTION MASKS API FAILURES"
- **Error Handling Guide**: `/docs/standards-processes/development-standards/error-handling-standards.md`
- **React Patterns**: `/docs/standards-processes/development-standards/react-patterns.md`
- **API Integration**: `/docs/guides-setup/api-integration-guide.md`

---

## Rule Maintenance

### Adding New Patterns

If you discover new silent error patterns:

1. Document the pattern in this README
2. Add detection logic to `no-silent-api-errors.js`
3. Add test cases to `__tests__/no-silent-api-errors.test.js`
4. Update lessons learned files

### Reporting Issues

If the rule produces false positives or misses patterns:

1. Create issue with example code
2. Tag with `eslint-rule` label
3. Include expected vs actual behavior

---

## Credits

**Created**: 2025-10-09
**Author**: React Developer Agent
**Context**: P1 task after removing 5 instances of silent 404 handlers
**Purpose**: Prevent reintroduction of error suppression patterns that mask real problems
