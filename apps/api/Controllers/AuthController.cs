// ARCHIVED: AuthController.cs - Migrated to Features/Authentication/Endpoints/AuthenticationEndpoints.cs
// This controller was causing route conflicts with the new minimal API endpoints.
// All functionality has been migrated to the new vertical slice architecture.
//
// Migration completed: 2025-08-22
// Conflicting routes removed:
// - POST /api/auth/login → Features/Authentication/Endpoints/AuthenticationEndpoints.cs
// - POST /api/auth/logout → Features/Authentication/Endpoints/AuthenticationEndpoints.cs  
// - GET /api/auth/user → Features/Authentication/Endpoints/AuthenticationEndpoints.cs
// - POST /api/auth/register → Features/Authentication/Endpoints/AuthenticationEndpoints.cs
// - POST /api/auth/service-token → Features/Authentication/Endpoints/AuthenticationEndpoints.cs
//
// This file is kept as an archive for reference but will not be compiled or registered.