# ASP.NET Core Identity Migration Guide

This guide explains how to migrate the WitchCityRope application from custom authentication to ASP.NET Core Identity.

## Overview

The migration introduces ASP.NET Core Identity while preserving all existing user data and maintaining backward compatibility during the transition period.

## Migration Steps

### 1. Database Migration

#### Apply the Identity Tables Migration

First, create the Identity tables in the database:

```bash
# From the Infrastructure project directory
dotnet ef migrations add AddIdentityTables --context WitchCityRopeIdentityDbContext
dotnet ef database update --context WitchCityRopeIdentityDbContext
```

#### Run the Data Migration Script

Execute the SQL data migration script to migrate existing user data:

```bash
psql -U postgres -d witchcityrope_db -f src/WitchCityRope.Infrastructure/Migrations/Identity/DataMigration.sql
```

**Important**: Always backup your database before running migration scripts!

### 2. Update Configuration Files

#### For Blazor Server (Web Project)

Replace the existing `Program.cs` with `Program.Identity.cs`:

```bash
# Backup existing Program.cs
cp src/WitchCityRope.Web/Program.cs src/WitchCityRope.Web/Program.cs.backup

# Use the Identity version
cp src/WitchCityRope.Web/Program.Identity.cs src/WitchCityRope.Web/Program.cs
```

#### For API Project

Replace the existing `Program.cs` with `Program.Identity.cs`:

```bash
# Backup existing Program.cs
cp src/WitchCityRope.Api/Program.cs src/WitchCityRope.Api/Program.cs.backup

# Use the Identity version
cp src/WitchCityRope.Api/Program.Identity.cs src/WitchCityRope.Api/Program.cs
```

### 3. Update Service Registrations

The new configuration files include:

- **Web Project**: Cookie authentication with Identity
- **API Project**: JWT authentication with Identity as the user store

### 4. Testing the Migration

After applying the migration:

1. **Verify User Login**: Test that existing users can still log in with their current passwords
2. **Check Roles**: Verify that user roles are properly mapped
3. **Test Claims**: Ensure custom claims (IsVetted, Team) are preserved
4. **API Authentication**: Test JWT token generation and validation

### 5. Rollback Plan

If issues occur, you can rollback:

1. Restore the database from backup
2. Revert to the original `Program.cs` files
3. Use the original `WitchCityRopeDbContext` instead of `WitchCityRopeIdentityDbContext`

## Key Changes

### Database Schema

- User data migrated to `auth.Users` table (Identity's AspNetUsers)
- Roles stored in `auth.Roles` table (Identity's AspNetRoles)
- User-role mappings in `auth.UserRoles` table
- Custom claims stored in `auth.UserClaims` table

### Authentication Flow

#### Web (Blazor Server)
- Uses cookie authentication via Identity
- `IdentityAuthenticationStateProvider` replaces custom provider
- `IdentityAuthService` handles login/logout operations

#### API
- JWT tokens generated using Identity user data
- Refresh tokens managed alongside Identity
- Role and claim-based authorization preserved

### User Management

- Password hashing uses Identity's password hasher
- Account lockout handled by Identity
- Email confirmation uses Identity's token providers
- Two-factor authentication ready (optional)

## Configuration Details

### Identity Options

```csharp
// Password requirements
options.Password.RequireDigit = true;
options.Password.RequiredLength = 8;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequireLowercase = true;

// Lockout settings
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
options.Lockout.MaxFailedAccessAttempts = 5;

// User settings
options.User.RequireUniqueEmail = true;
options.SignIn.RequireConfirmedEmail = true;
```

### Authorization Policies

All existing policies are preserved and enhanced:

```csharp
// Basic policies
"RequireAuthenticated" - Any authenticated user
"RequireAdmin" - Administrator role

// Team-based policies
"RequireVettingTeam" - Moderator role + VettingTeam claim
"RequireSafetyTeam" - Moderator role + SafetyTeam claim

// Member policies
"RequireVettedMember" - Member role + IsVetted claim
"RequireEventOrganizer" - Administrator or Organizer role
```

## Post-Migration Tasks

1. **Update Password Reset Flow**: Implement email sending for password reset tokens
2. **Email Confirmation**: Implement email sending for confirmation tokens
3. **Two-Factor Authentication**: Optionally enable 2FA for enhanced security
4. **Audit Logging**: Add Identity event logging for security tracking
5. **Performance Testing**: Verify authentication performance under load

## Troubleshooting

### Common Issues

1. **Login Failures**: Check that password hashes were migrated correctly
2. **Missing Roles**: Verify the role migration in `auth.UserRoles` table
3. **JWT Token Issues**: Ensure the JWT configuration matches between Web and API
4. **Cookie Issues**: Check cookie settings and HTTPS requirements

### Verification Queries

```sql
-- Check user migration
SELECT COUNT(*) FROM auth."Users";
SELECT COUNT(*) FROM public."Users";

-- Verify role assignments
SELECT u."Email", u."SceneName", r."Name" as Role
FROM auth."Users" u
JOIN auth."UserRoles" ur ON u."Id" = ur."UserId"
JOIN auth."Roles" r ON ur."RoleId" = r."Id"
LIMIT 10;

-- Check claims
SELECT u."Email", uc."ClaimType", uc."ClaimValue"
FROM auth."Users" u
JOIN auth."UserClaims" uc ON u."Id" = uc."UserId"
WHERE uc."ClaimType" IN ('IsVetted', 'Team');
```

## Benefits of Identity

1. **Security**: Industry-standard password hashing and account protection
2. **Features**: Built-in support for 2FA, external logins, password policies
3. **Compliance**: GDPR-ready with personal data management APIs
4. **Extensibility**: Easy to add new authentication providers
5. **Performance**: Optimized queries and caching

## Next Steps

After successful migration:

1. Remove old authentication tables (after thorough testing)
2. Update documentation for new authentication flow
3. Train support team on Identity features
4. Plan for additional Identity features (2FA, external logins)
5. Set up monitoring for authentication metrics