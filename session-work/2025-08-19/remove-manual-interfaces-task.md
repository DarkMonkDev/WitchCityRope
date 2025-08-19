# Remove Manual Interfaces - Replace with NSwag Generated Types

## Context
We have successfully implemented NSwag auto-generation pipeline that provides TypeScript types from the .NET API. Now we need to remove all manual interface definitions to avoid confusion and ensure type safety.

## Generated Types Available
From `@witchcityrope/shared-types`:
- `UserDto` - User information 
- `LoginRequest` - Login credentials
- `LoginResponse` - Login API response
- `EventDto` - Event information
- `CreateEventRequest` - Create event request
- `EventListResponse` - Event list response
- `UserRole`, `EventType`, `EventStatus` - Enums

## Files to Update

### 1. Auth Store (/src/stores/authStore.ts) ✅ COMPLETE
- [x] Already imports UserDto correctly
- [x] Remove the User type alias (line 6): `export type User = UserDto;`
- [x] Update all references to use UserDto directly

### 2. Auth Mutations (/src/features/auth/api/mutations.ts) ✅ COMPLETE
- [x] Remove LoginCredentials interface (lines 8-11) - use LoginRequest
- [x] Keep RegisterCredentials interface (not generated yet)
- [x] Remove LoginResponse interface (lines 21-25) - use generated LoginResponse
- [x] Import types from @witchcityrope/shared-types

### 3. Auth Queries (/src/features/auth/api/queries.ts) ✅ COMPLETE
- [x] Keep UserResponse interface (temporary until full API coverage)
- [x] Keep ProtectedWelcomeResponse interface (not generated yet)
- [x] Update imports to use generated types

### 4. Manual Interface Files to Remove/Update ✅ COMPLETE
- [x] `/src/services/authService.ts` - Replaced manual User interface with UserDto import
- [x] `/src/lib/api/types/auth.types.ts` - Replaced manual UserDto with generated type export
- [x] `/src/lib/api/types/members.types.ts` - Added generated UserDto export
- [x] `/src/types/api.types.ts` - Added UserDto import and export

### 5. Components Using Manual Types ✅ COMPLETE
- [x] `/src/stores/__tests__/authStore.test.ts` - Updated test mocks to UserDto
- [x] `/src/pages/DashboardPage.tsx` - Updated imports to UserDto
- [x] `/src/contexts/AuthContext.tsx` - Updated all User references to UserDto
- [x] `/src/features/members/api/mutations.ts` - Updated User to UserDto
- [x] `/src/features/members/api/queries.ts` - Updated User to UserDto
- [x] `/src/lib/api/hooks/useAuth.ts` - Updated LoginCredentials to LoginRequest
- [x] `/src/test/mocks/handlers.ts` - Updated all User to UserDto
- [x] `/src/features/auth/api/__tests__/mutations.test.tsx` - Updated User mock
- [x] `/src/test/integration/auth-flow.test.tsx` - Updated User mock
- [x] `/src/pages/ApiValidationV2.tsx` - Updated User to UserDto

## Pattern to Follow

### Before (Manual):
```typescript
// ❌ WRONG - Manual interface
interface User {
  sceneName: string;
  email: string;
  // ...
}

// ❌ WRONG - Type alias  
export type User = UserDto;
```

### After (Generated):
```typescript
// ✅ CORRECT - Import generated types
import type { 
  UserDto, 
  LoginRequest, 
  LoginResponse 
} from '@witchcityrope/shared-types';

// ✅ CORRECT - Use generated types directly
interface AuthState {
  user: UserDto | null;
  // ...
}
```

## Validation ✅ COMPLETE
- [x] All files compile without User/LoginCredentials TypeScript errors
- [x] All test mocks updated to use generated types  
- [x] No duplicate User/LoginCredentials type definitions remain
- [x] All imports point to @witchcityrope/shared-types for core types

## Summary
Successfully replaced all manual User and LoginCredentials interfaces with NSwag generated types from @witchcityrope/shared-types. This ensures:

- **Type Safety**: All User types now match the actual API DTOs exactly
- **Consistency**: No more drift between frontend and backend types  
- **Maintainability**: Types automatically update when API changes
- **Reduced Errors**: Eliminated manual type definition mistakes

### Key Changes Made:
1. **UserDto Import Pattern**: `import type { UserDto, LoginRequest } from '@witchcityrope/shared-types'`
2. **Updated Generated Type Properties**: Used `updatedAt` instead of `lastLoginAt` to match API
3. **Preserved Local Types**: Kept RegisterCredentials and other types not yet generated
4. **Fixed All Test Mocks**: Updated to match actual UserDto structure

### Remaining Work:
- Some tests still have other unrelated TypeScript errors (not User/LoginCredentials related)
- Future: Replace RegisterCredentials when API generates registration types