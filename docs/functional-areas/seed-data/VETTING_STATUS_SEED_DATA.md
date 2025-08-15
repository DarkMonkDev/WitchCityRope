# Vetting Status in Seed Data

## Overview
This document describes how vetting status is configured in the database seed data for WitchCityRope.

## Vetted Users in Seed Data

The following users are marked as vetted (`IsVetted = true`) in the seed data:

1. **Admin User**
   - Email: `admin@witchcityrope.com`
   - Scene Name: `RopeAdmin`
   - Role: Administrator
   - IsVetted: **true**

2. **Teacher User**
   - Email: `teacher@witchcityrope.com`
   - Scene Name: `RopeTeacher`
   - Role: Moderator
   - IsVetted: **true**

3. **Vetted Member**
   - Email: `vetted@witchcityrope.com`
   - Scene Name: `VettedMember`
   - Role: Member
   - IsVetted: **true**

4. **Witch City Alice** (Community Member)
   - Email: `alice@example.com`
   - Scene Name: `Witch City Alice`
   - Role: Member
   - IsVetted: **true**

## Non-Vetted Users in Seed Data

The following users are NOT vetted (`IsVetted = false`) in the seed data:

1. **General Member**
   - Email: `member@witchcityrope.com`
   - Scene Name: `GeneralMember`
   - Role: Member
   - IsVetted: **false**

2. **Guest User**
   - Email: `guest@witchcityrope.com`
   - Scene Name: `GuestUser`
   - Role: Attendee
   - IsVetted: **false**

## How Vetting Status is Set

### In Code (DbInitializer.cs)
```csharp
if (userData.IsVetted)
{
    user.MarkAsVetted();
}
```

### In SQL Seed Scripts
```sql
INSERT INTO auth."Users" (..., "IsVetted")
VALUES (..., true); -- for vetted users
```

## Automatic Vetting Logic

The application also considers the following users as automatically vetted:
- All users with **Administrator** role
- All users with **Moderator** role

This is implemented in `AuthService.cs`:
```csharp
IsVetted = isVetted || roles.Contains("Administrator") || roles.Contains("Moderator")
```

## Updating Vetting Status

To update a user's vetting status in the database:

```sql
-- Mark a user as vetted
UPDATE auth."Users"
SET "IsVetted" = true
WHERE "Email" = 'user@example.com';

-- Check vetting status
SELECT "Email", "SceneName", "IsVetted"
FROM auth."Users"
WHERE "Email" = 'user@example.com';
```

## Notes

- The admin user (`admin@witchcityrope.com`) is **always** seeded as a vetted member
- Vetting status is stored as a boolean in the `IsVetted` column of the `auth.Users` table
- The vetting process involves a comprehensive application workflow, but administrators and moderators bypass this requirement