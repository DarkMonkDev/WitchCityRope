# Auth Feature - Direct Service Pattern

This feature has been restructured to follow the direct service pattern, moving away from the command/handler pattern.

## Structure

```
Auth/
├── AuthController.cs           # API endpoints for authentication
├── AuthServiceRegistration.cs  # DI registration extension
├── Services/
│   └── AuthService.cs         # Main authentication service
├── Models/
│   ├── LoginRequest.cs        # Login endpoint request DTO
│   ├── LoginResponse.cs       # Login endpoint response DTO
│   ├── RegisterRequest.cs     # Registration endpoint request DTO
│   ├── RegisterResponse.cs    # Registration endpoint response DTO
│   ├── RefreshTokenRequest.cs # Token refresh request DTO
│   └── VerifyEmailRequest.cs  # Email verification request DTO
└── TwoFactor/                 # Two-factor authentication (not yet migrated)
```

## Key Changes

1. **Consolidated Logic**: The separate `LoginCommand` and `RegisterCommand` handlers have been combined into a single `AuthService` class.

2. **Direct Service Pattern**: Instead of using command objects and handlers, the controller directly calls methods on the `AuthService`.

3. **Core Entity Integration**: The service uses the existing `User` entity from `WitchCityRope.Core` with its domain rules:
   - Users must be at least 21 years old
   - Scene names are used as public identifiers
   - Legal names are encrypted for privacy
   - Email addresses use the `EmailAddress` value object

4. **Separation of Concerns**: 
   - Authentication details (password hash, email verification, etc.) are stored separately from the core `User` entity
   - The `UserAuthentication` class handles auth-specific data

## Usage

### Registration in Startup/Program.cs

```csharp
services.AddAuthFeature();
```

### Dependencies

The `AuthService` requires the following dependencies to be registered in the Infrastructure layer:

- `IUserRepository`: Data access for users and authentication
- `IPasswordHasher`: Password hashing and verification
- `IJwtService`: JWT token generation
- `IEmailService`: Email sending for verification
- `IEncryptionService`: Encryption for sensitive data (legal names)

### API Endpoints

- `POST /api/auth/login` - Authenticate with email and password
- `POST /api/auth/register` - Create a new user account
- `POST /api/auth/refresh` - Refresh an expired access token
- `POST /api/auth/verify-email` - Verify email address

## Implementation Notes

1. The service enforces the 21+ age requirement from the domain model
2. Scene names must be unique across the system
3. Legal names are encrypted before storage
4. Email verification is required before users can log in
5. Refresh tokens are stored with expiration dates for security