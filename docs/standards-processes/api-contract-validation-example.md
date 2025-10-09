# API Contract Validation - Real-World Example

## The Original Bug: `/ticket` vs `/participation`

### Background

During event ticket cancellation implementation, the frontend developer created an endpoint call to cancel a ticket:

```typescript
// Frontend: src/services/eventService.ts
export async function cancelEventTicket(eventId: string) {
  const response = await axios.delete(`/api/events/${eventId}/ticket`);
  return response.data;
}
```

The backend developer, following REST conventions, implemented:

```csharp
// Backend: Features/Events/EventsEndpoints.cs
[HttpDelete("/api/events/{eventId}/participation")]
public async Task<IActionResult> CancelParticipation(Guid eventId) {
    // Implementation...
}
```

### The Problem

**Frontend called**: `DELETE /api/events/{eventId}/ticket`
**Backend provided**: `DELETE /api/events/{eventId}/participation`

**Result**: 404 Not Found - Ticket cancellation feature completely broken in production.

### Discovery Timeline

1. **Week 1**: Feature developed and tested locally (mock API)
2. **Week 2**: Deployed to staging - users report "Cancel Ticket doesn't work"
3. **Week 3**: Bug investigation - frontend logs show 404 errors
4. **Week 4**: Root cause identified - endpoint path mismatch
5. **Week 5**: Fixed and redeployed

**Total Impact**: 4 weeks of broken functionality, user frustration, wasted development time

## How API Contract Validation Prevents This

### Setup (One-Time)

1. **Install Dependencies**:
   ```bash
   # Backend - already configured
   # Program.cs has Swagger enabled

   # Frontend
   cd apps/web
   npm install openapi-typescript --save-dev
   ```

2. **Configure Scripts**:
   ```json
   // package.json
   {
     "scripts": {
       "generate:api-types": "openapi-typescript ../../apps/api/openapi.json -o ./src/types/generated/api.d.ts",
       "validate:api-contract": "node ../../scripts/validate-api-contract.js"
     }
   }
   ```

### Daily Workflow

#### Backend Developer Workflow

1. **Implement Endpoint**:
   ```csharp
   // Features/Events/EventsEndpoints.cs
   endpoints.MapDelete("/api/events/{eventId}/participation", async (
       Guid eventId,
       [FromQuery] string? reason,
       HttpContext context) =>
   {
       // Implementation...
   })
   .WithName("CancelParticipation")
   .WithSummary("Cancel participation in event")
   .WithTags("Participation")
   .RequireAuthorization();
   ```

2. **Export OpenAPI Spec**:
   ```bash
   # Automatic on build, or manually:
   ./scripts/export-openapi-spec.sh

   # Output:
   # ✅ OpenAPI spec exported to: ./apps/api/openapi.json
   # 📊 Spec contains 67 endpoint paths
   ```

3. **Commit**:
   ```bash
   git add apps/api/
   git add apps/api/openapi.json  # Include spec!
   git commit -m "feat: Add event participation cancellation endpoint"
   ```

#### Frontend Developer Workflow

1. **Pull Latest Changes**:
   ```bash
   git pull origin main
   # Backend's openapi.json is now updated
   ```

2. **Generate TypeScript Types**:
   ```bash
   cd apps/web
   npm run generate:api-types

   # Output:
   # ✅ Generated TypeScript types from OpenAPI spec
   # → src/types/generated/api.d.ts
   ```

3. **Implement Feature** (WRONG way - for demonstration):
   ```typescript
   // src/services/eventService.ts
   export async function cancelEventTicket(eventId: string) {
     // ❌ BUG: Using wrong endpoint path
     const response = await axios.delete(`/api/events/${eventId}/ticket`);
     return response.data;
   }
   ```

4. **Validate Contract** (CATCHES THE BUG):
   ```bash
   npm run validate:api-contract
   ```

   **Output**:
   ```
   🔍 API Contract Validation
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   📋 Loading OpenAPI specification...
      Found 67 endpoints in spec

   📁 Scanning frontend code...
      Found 308 source files to analyze

   🔎 Extracting API calls from frontend...
      Found 89 API calls

   ✅ Validating API calls against spec...

   📊 Validation Results
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   ✅ Matched: 88
   ❌ Mismatched: 1

   ❌ API Contract Mismatches Found:
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   1. DELETE /api/events/${eventId}/ticket
      File: /apps/web/src/services/eventService.ts:156
      💡 Did you mean one of these?
         DELETE /api/events/{eventId}/participation (90% match)
            Cancel participation in event

   🔧 How to Fix:
      1. Check if the endpoint exists in the backend
      2. Update frontend to use correct endpoint path
      3. Or implement missing endpoint in backend
      4. Re-export OpenAPI spec: ./scripts/export-openapi-spec.sh

   ❌ Validation FAILED
   ```

   **BUG CAUGHT IMMEDIATELY!** ✅

5. **Fix the Bug**:
   ```typescript
   // src/services/eventService.ts
   export async function cancelParticipation(eventId: string, reason?: string) {
     // ✅ CORRECT: Using actual endpoint path
     const params = reason ? `?reason=${encodeURIComponent(reason)}` : '';
     const response = await axios.delete(`/api/events/${eventId}/participation${params}`);
     return response.data;
   }
   ```

6. **Validate Again**:
   ```bash
   npm run validate:api-contract

   # Output:
   # ✅ Matched: 89
   # ❌ Mismatched: 0
   # ✅ All API calls match the OpenAPI specification!
   ```

7. **Commit with Confidence**:
   ```bash
   git add src/services/eventService.ts
   git commit -m "feat: Implement event participation cancellation"
   git push
   ```

### CI/CD Integration

Add to `.github/workflows/ci.yml`:

```yaml
name: CI

on: [push, pull_request]

jobs:
  api-contract-validation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Start Docker Containers
        run: docker-compose up -d

      - name: Wait for API
        run: |
          timeout 60 bash -c 'until curl -f http://localhost:5655/health-check; do sleep 2; done'

      - name: Export OpenAPI Spec
        run: ./scripts/export-openapi-spec.sh

      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '20'

      - name: Install Frontend Dependencies
        run: cd apps/web && npm ci

      - name: Validate API Contract
        run: cd apps/web && npm run validate:api-contract
```

**Result**: Build fails immediately if frontend calls non-existent endpoints!

## Comparison: Before vs After

### Before API Contract Validation

| Phase | Time | Issues |
|-------|------|--------|
| Development | 1 day | No issues (mocked API) |
| Code Review | 1 day | Not caught (reviewers assume endpoint exists) |
| Staging Deploy | 1 day | Users report broken feature |
| Bug Investigation | 2-3 days | Find root cause in logs |
| Fix & Redeploy | 1 day | Finally working |
| **Total** | **6-7 days** | **Feature broken for 5 days** |

### After API Contract Validation

| Phase | Time | Issues |
|-------|------|--------|
| Development | 1 day | Validation catches bug before commit |
| Fix | 2 minutes | Update endpoint path |
| Code Review | 1 day | CI shows green build |
| Deploy | 1 day | Works first try |
| **Total** | **2 days** | **Zero downtime** |

**Savings**: 4-5 days, zero broken functionality in production

## Additional Benefits

### 1. Type Safety

Generated TypeScript types ensure response data matches backend DTOs:

```typescript
// Auto-generated from OpenAPI spec
import type { paths } from '@/types/generated/api';

type CancelResponse = paths['/api/events/{eventId}/participation']['delete']['responses']['204'];

// TypeScript error if response structure changes!
```

### 2. Documentation

OpenAPI spec serves as living documentation:

```bash
# View endpoint documentation
curl http://localhost:5655/swagger/index.html
```

### 3. API Client Generation

Generate fully typed API clients:

```bash
npm run generate:api-types

# Use in code:
import { apiClient } from '@/lib/apiClient';

// Fully typed, autocomplete works!
await apiClient.DELETE('/api/events/{eventId}/participation', {
  params: { path: { eventId } }
});
```

### 4. Breaking Change Detection

Validation catches when backend removes endpoints still used by frontend:

```
❌ API Contract Mismatches Found:

1. POST /api/events/{eventId}/register
   File: /apps/web/src/services/eventService.ts:89
   ⚠️  No similar endpoints found in OpenAPI spec

   This endpoint may have been removed!
   Check with backend team before deploying.
```

## Real-World Scenarios

### Scenario 1: Typo in Endpoint Path

**Frontend**:
```typescript
await axios.get('/api/evnets/upcoming'); // Typo: evnets
```

**Validation Output**:
```
❌ GET /api/evnets/upcoming
   💡 Did you mean:
      GET /api/events/upcoming (90% match)
```

### Scenario 2: Wrong HTTP Method

**Frontend**:
```typescript
await axios.post('/api/events/{id}'); // Should be PUT
```

**Validation Output**:
```
❌ POST /api/events/{id}
   💡 Did you mean:
      PUT /api/events/{id} (100% path match)
         Update event details
```

### Scenario 3: Removed Endpoint

**Backend**: Removes `/api/events/{id}/tickets` in favor of `/api/events/{id}/participation`

**Validation Output**:
```
❌ DELETE /api/events/{id}/tickets
   ⚠️  No similar endpoints found in OpenAPI spec

   This endpoint may have been removed!
   Check git history: git log --all -- "**/*tickets*"
```

## Best Practices

### DO ✅

1. **Always run validation before committing**:
   ```bash
   npm run validate:api-contract && git commit
   ```

2. **Include openapi.json in commits**:
   ```bash
   git add apps/api/openapi.json
   ```

3. **Generate types after pulling**:
   ```bash
   git pull && npm run generate:api-types
   ```

4. **Add validation to CI/CD pipeline**

5. **Use generated TypeScript types in API calls**

### DON'T ❌

1. ❌ Skip validation for "quick fixes"
2. ❌ Manually create TypeScript interfaces for API responses
3. ❌ Ignore validation failures
4. ❌ Commit frontend changes without checking backend changes
5. ❌ Deploy without green CI build

## Troubleshooting

### Validation Shows False Positives

**Issue**: Validation reports mismatches for valid endpoints

**Cause**: OpenAPI spec is stale

**Fix**:
```bash
./scripts/export-openapi-spec.sh
npm run validate:api-contract
```

### TypeScript Types Don't Match Runtime Data

**Issue**: Generated types don't match actual API responses

**Cause**: Backend DTOs changed but spec not updated

**Fix**:
```bash
# Backend: Restart API to regenerate spec
./dev.sh

# Frontend: Regenerate types
./scripts/export-openapi-spec.sh
cd apps/web && npm run generate:api-types
```

### CI Validation Fails but Local Passes

**Issue**: CI reports mismatches not seen locally

**Cause**: Different openapi.json versions

**Fix**:
```bash
# Ensure openapi.json is committed
git status apps/api/openapi.json

# Pull latest and retry
git pull
npm run validate:api-contract
```

## Conclusion

API contract validation transforms a 4-5 day production bug into a 2-minute fix during development. The `/ticket` vs `/participation` bug would have been caught immediately, saving:

- ⏰ **4-5 days** of developer time
- 😤 **Zero user frustration** with broken features
- 💰 **Cost savings** from reduced debugging
- 🚀 **Faster feature delivery** with confidence

**Investment**: 1 hour setup, 30 seconds per validation
**Return**: Prevent every endpoint mismatch bug before production

## Related Documentation

- [API Contract Validation Guide](/docs/standards-processes/api-contract-validation.md)
- [DTO Alignment Strategy](/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)

---

**Last Updated**: 2025-10-09
