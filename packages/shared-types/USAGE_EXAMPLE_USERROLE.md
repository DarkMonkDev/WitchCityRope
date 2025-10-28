# UserRole Enum Usage Examples

## Auto-Generated from C# Enum

The `UserRole` type is **automatically generated** from the C# `UserRole` enum in `/apps/api/Features/Users/Constants/UserRole.cs`.

## Generated TypeScript Type

```typescript
// Automatically generated in packages/shared-types/src/generated/api-types.ts
type UserRole = "Member" | "Teacher" | "SafetyTeam" | "Administrator" | "CheckInStaff" | "EventOrganizer";
```

## How to Use in Frontend

### Import the Type

```typescript
import type { components } from '@witchcityrope/shared-types';

// Extract the UserRole type
type UserRole = components['schemas']['UserRole'];
```

### Example 1: Type-Safe Role Checks

**BEFORE (Hardcoded - WRONG):**
```typescript
const isAdmin = user?.role === 'Administrator' // Typo risk: "Admin" vs "Administrator"
```

**AFTER (Type-Safe - CORRECT):**
```typescript
import type { components } from '@witchcityrope/shared-types';
type UserRole = components['schemas']['UserRole'];

const isAdmin = (role: UserRole): boolean => {
  return role === 'Administrator'; // TypeScript ensures this is valid
};

// TypeScript will error if you typo:
// const isAdmin = role === 'Admin'; // ❌ Type error: "Admin" not in UserRole
```

### Example 2: Role Dropdowns

**BEFORE (Hardcoded - WRONG):**
```typescript
const roleOptions = [
  { value: 'Teacher', label: 'Teacher' },
  { value: 'Administrator', label: 'Administrator' },
  { value: 'SafetyTeam', label: 'Safety Team' }, // Inconsistent format
];
```

**AFTER (Type-Safe - CORRECT):**
```typescript
import type { components } from '@witchcityrope/shared-types';
type UserRole = components['schemas']['UserRole'];
type UserRoleDto = components['schemas']['UserRoleDto'];

// Fetch roles from API (includes display names)
const { data } = useQuery({
  queryKey: ['availableRoles'],
  queryFn: async () => {
    const response = await fetch('/api/users/roles/available');
    return response.json() as { roles: UserRoleDto[] };
  }
});

// Use in dropdown
const roleOptions = data.roles.map(role => ({
  value: role.role, // TypeScript knows this is UserRole
  label: role.displayName, // "Safety Team" not "SafetyTeam"
}));
```

### Example 3: Authorization Checks

**BEFORE (Hardcoded - WRONG):**
```typescript
if (user.role === 'Administrator') { // Typo: might be "Admin"
  // Show admin features
}
```

**AFTER (Type-Safe - CORRECT):**
```typescript
import type { components } from '@witchcityrope/shared-types';
type UserRole = components['schemas']['UserRole'];

const hasRole = (userRole: UserRole, allowedRoles: UserRole[]): boolean => {
  return allowedRoles.includes(userRole);
};

// TypeScript enforces valid role names
if (hasRole(user.role, ['Administrator', 'Teacher'])) {
  // Show features for admins and teachers
}

// This would cause a TypeScript error:
// if (hasRole(user.role, ['Admin'])) { // ❌ Error: "Admin" not in UserRole
```

## Benefits

✅ **Compile-Time Safety**: Typos caught by TypeScript compiler
✅ **Auto-Completion**: IDEs suggest valid roles
✅ **Single Source of Truth**: C# enum is the authority
✅ **Refactor-Safe**: Rename in C#, regenerate types, all references update
✅ **No Manual Sync**: Types always match backend

## Regenerating Types

When the C# enum changes, regenerate TypeScript types:

```bash
cd packages/shared-types
npm run generate
```

This automatically updates all TypeScript consumers.

## Related Files

- **C# Enum**: `/apps/api/Features/Users/Constants/UserRole.cs` (source of truth)
- **Generated TS**: `/packages/shared-types/src/generated/api-types.ts` (auto-generated)
- **API Endpoint**: `GET /api/users/roles/available` (returns all roles with display names)
