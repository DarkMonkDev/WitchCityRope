# API Contract Validation

## Overview

This document describes the OpenAPI-based contract validation system that prevents frontend/backend API mismatches before they reach production.

**Problem Solved**: The ticket cancellation bug occurred because the frontend called `/api/events/{id}/ticket` but the backend only had `/api/events/{id}/participation`. OpenAPI validation catches these mismatches automatically.

## Architecture

```
┌─────────────────────┐
│   Backend API       │
│   (C# .NET 9)       │
└──────────┬──────────┘
           │
           │ Swagger generates
           ▼
┌─────────────────────┐
│  openapi.json       │  ◄─── Source of Truth
│  OpenAPI 3.0 Spec   │
└──────────┬──────────┘
           │
           ├─────────────────────────┐
           │                         │
           ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│  TypeScript Types   │   │  Contract Validator │
│  (Frontend)         │   │  (CI/CD Check)      │
└─────────────────────┘   └─────────────────────┘
```

## Components

### 1. OpenAPI Specification Generation

**Location**: `/apps/api/openapi.json` (generated)

The backend API automatically generates an OpenAPI 3.0 specification using Swagger/NSwag:

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
```

**Export Script**: `/scripts/export-openapi-spec.sh`

```bash
# Export OpenAPI spec from running API
./scripts/export-openapi-spec.sh

# Output: apps/api/openapi.json
```

**When to Export**:
- After adding new endpoints
- After changing endpoint paths or methods
- After modifying DTOs
- Before running frontend validation

### 2. TypeScript Type Generation

**Location**: `/apps/web/src/types/generated/api.d.ts` (generated)

Generate TypeScript types from the OpenAPI spec to ensure type safety:

```bash
# From apps/web directory
npm run generate:api-types
```

This uses `openapi-typescript` to create TypeScript interfaces matching the backend DTOs exactly.

**Usage in Frontend**:

```typescript
import type { paths } from '@/types/generated/api';

// Type-safe API call
type EventResponse = paths['/api/events/{id}']['get']['responses']['200']['content']['application/json'];

async function getEvent(id: string): Promise<EventResponse> {
  const response = await fetch(`/api/events/${id}`);
  return response.json();
}
```

### 3. Contract Validation Script

**Location**: `/scripts/validate-api-contract.js`

Validates that all frontend API calls match endpoints defined in the OpenAPI spec.

```bash
# From project root
npm run validate:api-contract
```

**What It Checks**:
- ✅ HTTP method matches (GET, POST, PUT, DELETE)
- ✅ Endpoint path exists in spec
- ✅ Route parameters match
- ❌ Catches typos like `/ticket` vs `/participation`
- ❌ Detects removed endpoints still used in frontend

**Example Output**:

```
🔍 API Contract Validation
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Loading OpenAPI specification...
   Found 61 endpoints in spec

📁 Scanning frontend code...
   Found 147 source files to analyze

🔎 Extracting API calls from frontend...
   Found 89 API calls

✅ Validating API calls against spec...

📊 Validation Results
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Matched: 87
❌ Mismatched: 2

❌ API Contract Mismatches Found:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. DELETE /api/events/1/ticket
   File: /apps/web/src/services/eventService.ts:156
   💡 Did you mean one of these?
      DELETE /api/events/{id}/participation (90% match)
         Cancel user's participation in an event

2. POST /api/events/1/ticket
   File: /apps/web/src/services/eventService.ts:134
   💡 Did you mean one of these?
      POST /api/events/{id}/participation (90% match)
         Register user for an event

🔧 How to Fix:
   1. Check if the endpoint exists in the backend
   2. Update frontend to use correct endpoint path
   3. Or implement missing endpoint in backend
   4. Re-export OpenAPI spec: ./scripts/export-openapi-spec.sh

❌ Validation FAILED
```

## Workflow

### Development Workflow

1. **Backend Developer Adds Endpoint**:
   ```bash
   # Add new endpoint in C#
   # Code, test, commit

   # Export OpenAPI spec
   ./scripts/export-openapi-spec.sh
   git add apps/api/openapi.json
   git commit -m "feat: Add new event endpoint"
   ```

2. **Frontend Developer Uses Endpoint**:
   ```bash
   # Pull latest changes
   git pull

   # Generate TypeScript types
   cd apps/web
   npm run generate:api-types

   # Use types in code
   # Import from '@/types/generated/api'

   # Validate contract
   npm run validate:api-contract
   ```

### CI/CD Integration

Add to your build pipeline:

```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  validate-api-contract:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Start API
        run: ./dev.sh

      - name: Export OpenAPI Spec
        run: ./scripts/export-openapi-spec.sh

      - name: Install Frontend Dependencies
        run: cd apps/web && npm ci

      - name: Validate API Contract
        run: cd apps/web && npm run validate:api-contract
```

### Pre-commit Hook

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash

# Check if OpenAPI spec is committed with API changes
if git diff --cached --name-only | grep -q "apps/api/"; then
    if ! git diff --cached --name-only | grep -q "apps/api/openapi.json"; then
        echo "❌ ERROR: API changes detected but openapi.json not updated"
        echo "   Run: ./scripts/export-openapi-spec.sh"
        exit 1
    fi
fi

# Validate contract before commit
cd apps/web && npm run validate:api-contract
```

## Handling Breaking Changes

### Adding New Endpoints

1. Add endpoint in backend
2. Export OpenAPI spec
3. Generate TypeScript types
4. Implement frontend code
5. Validate contract
6. Commit all changes together

### Changing Existing Endpoints

⚠️ **BREAKING CHANGE PROTOCOL**:

1. **Deprecate old endpoint** (keep for 30 days):
   ```csharp
   [Obsolete("Use POST /api/events/{id}/participation instead")]
   [HttpPost("/api/events/{id}/ticket")]
   public async Task<IActionResult> LegacyRegister(int id) {
       // Redirect to new endpoint
       return await Register(id);
   }
   ```

2. **Add new endpoint** alongside old one
3. **Update OpenAPI spec**
4. **Update frontend** to use new endpoint
5. **Validate contract** to ensure no calls to old endpoint
6. **Remove old endpoint** after deprecation period

### Removing Endpoints

1. Search frontend for endpoint usage:
   ```bash
   grep -r "/api/old-endpoint" apps/web/src/
   ```

2. Remove all frontend references
3. Remove backend endpoint
4. Export OpenAPI spec
5. Validate contract (should pass with 0 calls to removed endpoint)

## Troubleshooting

### Validation Fails with "No similar endpoints found"

**Cause**: Frontend calling endpoint that doesn't exist in backend

**Fix**:
1. Check if endpoint exists: Review `/apps/api/openapi.json`
2. Check spelling/capitalization
3. Verify HTTP method (GET vs POST)
4. Implement missing endpoint if needed

### OpenAPI Spec Export Fails

**Cause**: API not running or Swagger not configured

**Fix**:
```bash
# Start API
./dev.sh

# Check API health
curl http://localhost:5655/health-check

# Verify Swagger endpoint
curl http://localhost:5655/swagger/v1/swagger.json

# Re-export
./scripts/export-openapi-spec.sh
```

### TypeScript Type Generation Fails

**Cause**: Invalid OpenAPI spec or missing dependencies

**Fix**:
```bash
# Install/update dependencies
cd apps/web
npm install openapi-typescript@latest

# Validate OpenAPI spec
npx openapi-typescript-validator ../../apps/api/openapi.json

# Regenerate types
npm run generate:api-types
```

### Contract Validation False Positives

**Cause**: Dynamic URLs or non-standard API call patterns

**Solution**: Update validation script patterns in `/scripts/validate-api-contract.js`:

```javascript
// Add custom pattern for your API call style
const patterns = [
  // ... existing patterns
  /yourCustomPattern/g,
];
```

## Real-World Example: Ticket vs Participation Bug

### The Bug

Frontend code:
```typescript
// ❌ WRONG - Endpoint doesn't exist
async function cancelTicket(eventId: number) {
  await axios.delete(`/api/events/${eventId}/ticket`);
}
```

Backend endpoints:
```csharp
// ✅ CORRECT - Actual endpoint
[HttpDelete("/api/events/{id}/participation")]
public async Task<IActionResult> CancelParticipation(int id) { }
```

### Detection with Validation

```bash
$ npm run validate:api-contract

❌ API Contract Mismatches Found:

1. DELETE /api/events/${eventId}/ticket
   File: /apps/web/src/services/eventService.ts:156
   💡 Did you mean:
      DELETE /api/events/{id}/participation (85% match)
         Cancel user's participation in an event

❌ Validation FAILED
```

### The Fix

```typescript
// ✅ FIXED - Use correct endpoint
async function cancelParticipation(eventId: number) {
  await axios.delete(`/api/events/${eventId}/participation`);
}
```

## Best Practices

### DO ✅

- Export OpenAPI spec after every backend change
- Run validation before committing frontend changes
- Use generated TypeScript types for API responses
- Document endpoint changes in commit messages
- Add validation to CI/CD pipeline
- Keep OpenAPI spec in version control

### DON'T ❌

- Manually create TypeScript interfaces for API data
- Skip validation for "quick fixes"
- Assume endpoint exists without checking spec
- Remove endpoints without frontend coordination
- Deploy backend changes without frontend validation
- Ignore validation failures in CI

## Maintenance

### Weekly

- Review validation failures in CI
- Update documentation for new patterns
- Check for deprecated endpoint usage

### Monthly

- Audit OpenAPI spec completeness
- Review and improve validation patterns
- Update type generation tools
- Clean up unused types

### When Upgrading

- Test validation with new .NET version
- Update openapi-typescript package
- Verify Swagger compatibility
- Update documentation

## Related Documentation

- [DTO Alignment Strategy](/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md)
- [API Architecture Overview](/docs/architecture/API-ARCHITECTURE-OVERVIEW.md)
- [Vertical Slice Quick Start](/docs/guides-setup/VERTICAL-SLICE-QUICK-START.md)
- [Coding Standards](/docs/standards-processes/CODING_STANDARDS.md)

## Support

For issues with API contract validation:

1. Check this documentation first
2. Review validation script output
3. Verify OpenAPI spec is current
4. Check CI/CD logs for errors
5. Create issue with validation output

---

**Version**: 1.0.0
**Last Updated**: 2025-10-09
**Owner**: Backend Team
