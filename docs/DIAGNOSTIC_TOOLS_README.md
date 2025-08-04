# Integration Test Diagnostic Tools

This directory contains several diagnostic tools to help you understand why your integration tests are failing.

## Problem Description

You have 39 out of 139 integration tests failing. The infrastructure (database, migrations, seed data) is working, but the tests are failing on HTTP endpoints.

## What These Tools Do

These diagnostic tools will:

1. **Start the application using the same TestWebApplicationFactory as your integration tests**
2. **Make HTTP requests to the exact endpoints being tested**
3. **Show you the actual HTTP status codes and response content**
4. **Compare what's happening vs what the tests expect**

## Available Tools

### 1. Quick Test Script (Recommended)
```bash
./test-endpoints.sh
```

This script will:
- Run a sample of your integration tests to show current failures
- Create and run a simple diagnostic test
- Show you exactly what HTTP responses your endpoints are returning
- Clean up after itself

### 2. Simple Diagnostic Program
```bash
./run-diagnostic.sh
```

This runs a standalone C# program that:
- Uses the exact same setup as your integration tests
- Tests key endpoints: `/`, `/events`, `/login`, `/dashboard`
- Shows detailed HTTP response information
- Provides analysis of what each response means

### 3. Standalone Diagnostic Files

If you want to examine the code:
- `SimpleDiagnostic.cs` - Simple diagnostic program
- `DiagnosticTest.cs` - More comprehensive diagnostic program
- `*.csproj` files - Project files for building the diagnostics

## How to Use

1. **Start with the quick test:**
   ```bash
   ./test-endpoints.sh
   ```

2. **Look for these common issues:**
   - **500 errors**: Server errors - these are the main problem
   - **404 errors**: Routes not configured or missing
   - **Unexpected redirects**: Authentication/authorization issues
   - **Wrong content**: Content delivery problems

3. **Compare with integration test expectations:**
   - `/`, `/events`, `/login` should return 200 OK
   - `/dashboard`, `/admin/events` should redirect to login (302/301)
   - No endpoint should return 500 errors

## Common Issues to Look For

### Route Configuration Issues
- Missing or incorrectly configured routes
- Blazor routing problems
- Controller endpoint issues

### Authentication/Authorization Problems
- Identity not properly configured
- Cookie authentication issues
- Missing authentication middleware

### Content Delivery Issues
- Static files not found
- Missing wwwroot files
- CSS/JS loading problems

### Database/Infrastructure Issues
- Entity Framework context problems
- Migration issues
- Seed data problems

## Next Steps

After running the diagnostics:

1. **If you see 500 errors:** Check the application logs and exception details
2. **If you see 404 errors:** Check route configuration in your Blazor components
3. **If authentication is wrong:** Check Identity configuration and middleware setup
4. **If content is missing:** Check static file configuration and wwwroot folder

## Example Output

The diagnostic tools will show you something like:

```
Testing: Home page (/)
  Status: 200 OK
  Content starts with: <!DOCTYPE html><html>...
  ✅ OK - Working correctly

Testing: Events page (/events)
  Status: 500 InternalServerError
  🚨 SERVER ERROR - This is the problem!

Testing: Login page (/login)
  Status: 404 NotFound
  ❌ NOT FOUND - Route not configured
```

This immediately tells you that:
- Home page is working
- Events page has a server error (main issue)
- Login page route is not configured

## Files Created

- `test-endpoints.sh` - Main diagnostic script
- `run-diagnostic.sh` - Alternative diagnostic runner
- `SimpleDiagnostic.cs` - Simple diagnostic program
- `DiagnosticTest.cs` - Comprehensive diagnostic program
- `*.csproj` - Project files for building diagnostics
- `DIAGNOSTIC_TOOLS_README.md` - This documentation

## Clean Up

The diagnostic tools clean up after themselves, but if you want to remove them:

```bash
rm -f test-endpoints.sh run-diagnostic.sh quick-diagnostic.sh
rm -f SimpleDiagnostic.cs DiagnosticTest.cs
rm -f SimpleDiagnostic.csproj DiagnosticTest.csproj
rm -f DIAGNOSTIC_TOOLS_README.md
```