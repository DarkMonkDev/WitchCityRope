# Admin RBAC - Current State Analysis

**Date**: 2026-03-23
**Purpose**: Deep dive into current auth/authorization system to plan role-based admin dashboard access

---

## Current Admin Dashboard Cards (8 total)

| # | Card Title | Route | Current Access |
|---|-----------|-------|----------------|
| 1 | Events Management | `/admin/events` | Administrator only |
| 2 | Member Management | `/admin/members` | Administrator only |
| 3 | Vetting Applications | `/admin/vetting` | Administrator only |
| 4 | Incident Reports | `/admin/safety/incidents` | Administrator only |
| 5 | Email Templates | `/admin/email-templates` | Administrator only |
| 6 | Content Management | `/admin/cms/revisions` | Administrator only |
| 7 | Reports | `/admin/reports` | Administrator only |
| 8 | Settings | `/admin/settings` | Administrator only |

**Source**: `apps/web/src/pages/admin/AdminDashboardPage.tsx`

---

## Current Roles in System

**Source**: `apps/api/Features/Users/Constants/UserRole.cs`

| Role | Enum Value | Purpose | Test Account |
|------|-----------|---------|-------------|
| Member | 0 | Default state (never assigned) | member@, guest@, vetted@ |
| Teacher | 1 | Can create/teach events | teacher@ |
| SafetyTeam | 2 | Safety coordination | coordinator1@, coordinator2@ |
| Administrator | 3 | Full system access | admin@ |
| EventOrganizer | 4 | Organize/manage events | (no dedicated test account) |
| DungeonMonitor | 5 | Monitor play spaces | dm@ |

**Storage**: Comma-separated string in `ApplicationUser.Role` field (e.g., "Teacher,SafetyTeam")

---

## Current Authorization Architecture

### Frontend (React)

1. **Route Protection**: `adminLoader` in `apps/web/src/routes/loaders/adminLoader.ts`
   - Checks `hasRole(user, 'Administrator')` - ONLY Administrator allowed
   - Every `/admin/*` route uses this loader
   - Redirects to `/unauthorized` if role check fails

2. **Navigation Visibility**: `apps/web/src/components/layout/Navigation.tsx`
   - "Admin" link only shows for users with `hasRole(user, 'Administrator')`
   - Both desktop and mobile menu

3. **Role Utilities**: `apps/web/src/utils/roleUtils.ts`
   - `hasRole(user, role)` - Check single role
   - `hasAnyRole(user, roles[])` - Check multiple roles
   - `getUserRoles(user)` - Get all roles as array
   - Already supports multi-role checking

### Backend (C# API)

1. **Most admin endpoints**: `RequireAuthorization(policy => policy.RequireRole(UserRole.Administrator.ToRoleString()))`
2. **Safety endpoints**: Already support multi-role: `policy.RequireRole(UserRole.Administrator.ToRoleString(), UserRole.SafetyTeam.ToRoleString())`
3. **Refund endpoints**: `policy.RequireRole("Administrator", "Teacher")`
4. **JWT Claims**: Role claims are properly set per-role in `JwtService.cs`

---

## Key Observations

### What Already Works Well
- Multi-role infrastructure exists in both frontend (`hasAnyRole`) and backend (`RequireRole` accepts multiple roles)
- Safety endpoints already demonstrate the pattern of allowing SafetyTeam + Administrator
- Role utilities on frontend are well-structured and support the needed checks
- JWT claims properly include all roles from the CSV field

### What Needs to Change
1. **`adminLoader`** - Currently hardcoded to `'Administrator'` only. Needs to allow any "admin-capable" role
2. **`AdminDashboardPage`** - Shows all 8 cards to everyone who accesses it. Needs to filter cards by role
3. **Navigation** - "Admin" link only shows for Administrator. Needs to show for any role with admin access
4. **Backend API endpoints** - Many endpoints are `Administrator`-only but should allow specific roles (e.g., events endpoints should allow EventOrganizer)

### Missing Roles
- **No "Vetter" or "VettingTeam" role** exists in the current system. The old Blazor app had `RequireVettingTeam` policy, but this was not carried forward to the React migration.

### Existing Role: EventOrganizer vs "EventCoordinator"
- The UserRole enum already has `EventOrganizer` (value 4)
- User requested "EventCoordinator" as a new role
- Need to clarify: Should we use the existing `EventOrganizer` or create a new `EventCoordinator`?

---

## Backend Endpoint Authorization Summary

| Feature Area | Current Auth | Should Allow |
|---|---|---|
| Events Management | RequireAuthorization() (any auth user) | Administrator + EventOrganizer/Coordinator |
| Member Management / User endpoints | Administrator only | Administrator only |
| Vetting endpoints | RequireAuthorization() (any auth user) | Administrator + VettingTeam (new role) |
| Safety/Incident endpoints | Administrator + SafetyTeam | Administrator + SafetyTeam (already correct) |
| Email Templates | Administrator only | Administrator only |
| CMS | Administrator only | Administrator only |
| Reports | Administrator only | Administrator only |
| Settings | Administrator only | Administrator only |
| Backup | Administrator only | Administrator only |
| Payments/Refunds | Administrator + Teacher | Administrator + Teacher (already correct) |

---

## Files That Need Modification

### Frontend
- `apps/web/src/routes/loaders/adminLoader.ts` - Allow multiple admin-capable roles
- `apps/web/src/pages/admin/AdminDashboardPage.tsx` - Filter cards by user role
- `apps/web/src/components/layout/Navigation.tsx` - Show Admin link for any admin-capable role

### Backend (potentially)
- `apps/api/Features/Users/Constants/UserRole.cs` - Add VettingTeam role if needed
- `apps/api/Features/Users/Constants/UserRoleConstants.cs` - Updated automatically from enum
- `apps/api/Services/Seeding/UserSeeder.cs` - Add test accounts for new roles
- Various endpoint files - Update RequireAuthorization policies for role-specific access

### No migration needed if
- We only add a new enum value (VettingTeam) - this is stored as a string, no schema change
