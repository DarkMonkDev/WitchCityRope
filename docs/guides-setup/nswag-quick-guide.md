# NSwag Quick Guide: How to Update Types When API Changes
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Architecture Team -->
<!-- Status: Active -->

## 🚀 Quick Start

**When API Changes**: Run `npm run generate:types` to update TypeScript interfaces automatically.

**Golden Rule**: NEVER manually create DTO interfaces. Always use generated types.

## Step-by-Step Process

### Step 1: Backend Developer Updates C# DTO

```csharp
// Add or modify DTO in packages/contracts/
// Example: packages/contracts/DTOs/Users/UserDto.cs

namespace WitchCityRope.Contracts.DTOs.Users
{
    /// <summary>
    /// User profile information
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// User's scene name (display name)
        /// </summary>
        public string SceneName { get; set; }
        
        /// <summary>
        /// Account creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Last login timestamp (null if never logged in)
        /// </summary>
        public DateTime? LastLoginAt { get; set; }
        
        /// <summary>
        /// User roles for authorization
        /// </summary>
        public List<string> Roles { get; set; }
        
        // NEW PROPERTY ADDED
        /// <summary>
        /// User's membership level
        /// </summary>
        public MembershipLevel MembershipLevel { get; set; }
    }
}
```

### Step 2: Update API Controller with OpenAPI Annotations

```csharp
// In apps/api/Features/Users/UsersController.cs

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Gets user profile by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile data</returns>
    /// <response code="200">Returns user profile</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        // Implementation
    }
}
```

### Step 3: Frontend Developer Regenerates Types

```bash
# 1. Ensure API is running
docker-compose up -d api
# OR
dotnet run --project apps/api

# 2. Wait for API to be ready (check health endpoint)
curl http://localhost:5653/health

# 3. Generate fresh TypeScript types
npm run generate:types

# 4. Generated files updated automatically:
# - packages/shared-types/src/generated/api-client.ts
# - packages/shared-types/src/models/users/user.ts
```

### Step 4: Use Generated Types in Components

```typescript
// Import generated types (NEVER create manual interfaces)
import { User, MembershipLevel } from '@witchcityrope/shared-types';

// Component using generated types
interface UserProfileProps {
  user: User; // This type is automatically generated and up-to-date
}

export function UserProfile({ user }: UserProfileProps) {
  return (
    <div>
      <h2>{user.sceneName}</h2>
      <p>Member since: {new Date(user.createdAt).toLocaleDateString()}</p>
      <p>Membership Level: {user.membershipLevel}</p> {/* New property automatically available */}
      {user.lastLoginAt && (
        <p>Last login: {new Date(user.lastLoginAt).toLocaleDateString()}</p>
      )}
    </div>
  );
}
```

## NSwag Configuration

### Location: `packages/shared-types/scripts/nswag.json`

```json
{
  "runtime": "Net80",
  "documentGenerator": {
    "fromDocument": {
      "url": "http://localhost:5653/swagger/v1/swagger.json"
    }
  },
  "codeGenerators": {
    "openApiToTypeScript": {
      "className": "ApiClient",
      "typeScriptVersion": 5.0,
      "template": "Fetch",
      "promiseType": "Promise",
      "dateTimeType": "string",
      "nullValue": "Undefined",
      "generateClientClasses": true,
      "generateOptionalParameters": true,
      "exportTypes": true,
      "markOptionalProperties": true,
      "typeStyle": "Interface",
      "output": "src/generated/api-client.ts"
    }
  }
}
```

### Generation Script: `packages/shared-types/scripts/generate-types.sh`

```bash
#!/bin/bash
echo "🔄 Generating TypeScript types from API..."

# Check API availability
echo "📡 Checking API availability..."
if ! curl -f http://localhost:5653/health > /dev/null 2>&1; then
    echo "❌ API is not running. Please start the API first:"
    echo "   docker-compose up -d api"
    echo "   OR"
    echo "   dotnet run --project apps/api"
    exit 1
fi

# Generate types
echo "🏠 Generating types with NSwag..."
cd packages/shared-types
npx nswag run scripts/nswag.json

# Post-process
echo "🔧 Post-processing generated types..."
node scripts/post-process.js

# Validate
echo "✅ Validating generated types..."
npx tsc --noEmit

echo "🎉 Type generation completed successfully!"
echo "Generated types are available in packages/shared-types/src/generated/"
```

## Common Workflows

### Adding New DTO Property

1. **Backend**: Add property to C# DTO with XML documentation
2. **Backend**: Update OpenAPI annotations if needed
3. **Frontend**: Run `npm run generate:types`
4. **Frontend**: New property automatically available in TypeScript

### Changing Property Name (Breaking Change)

1. **Planning**: 30-day notice to frontend team
2. **Backend**: Create new property, mark old as obsolete
3. **Frontend**: Run `npm run generate:types`
4. **Frontend**: Update code to use new property name
5. **Backend**: Remove obsolete property after frontend migration

### Adding New Endpoint

1. **Backend**: Create controller action with OpenAPI annotations
2. **Frontend**: Run `npm run generate:types`
3. **Frontend**: New API client method automatically available

## Troubleshooting

### API Not Available
```bash
# Check if API is running
curl http://localhost:5653/health

# Start API if needed
docker-compose up -d api
```

### Generation Fails
```bash
# Check NSwag is installed
npx nswag version

# Clear and reinstall if needed
rm -rf node_modules package-lock.json
npm install
```

### TypeScript Compilation Errors
```bash
# Check generated types
npx tsc --noEmit packages/shared-types/src/generated/api-client.ts

# May need to regenerate if API structure changed
npm run generate:types
```

### Missing Properties in Generated Types
- Check C# DTO has proper XML documentation
- Verify OpenAPI annotations on controller actions
- Ensure property is public with getter/setter

## Package Structure

```
packages/shared-types/
├── src/
│   ├── generated/              # Generated by NSwag (DO NOT EDIT)
│   │   └── api-client.ts       # Main generated file
│   ├── models/                 # Organized generated types
│   │   ├── users/              # User-related types
│   │   ├── events/             # Event-related types
│   │   └── common/             # Shared types
│   ├── enums/                  # Generated enums
│   └── api/                    # API client interfaces
├── scripts/
│   ├── generate-types.sh      # Main generation script
│   ├── nswag.json             # NSwag configuration
│   └── post-process.js        # Post-processing utilities
└── package.json
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
# .github/workflows/type-generation.yml
name: Validate TypeScript Types

on:
  pull_request:
    paths:
      - 'packages/contracts/**'
      - 'apps/api/**'

jobs:
  check-types:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      - name: Start API
        run: |
          dotnet run --project apps/api &
          sleep 30
      - name: Generate types
        run: npm run generate:types
      - name: Check for changes
        run: |
          if [ -n "$(git status --porcelain packages/shared-types/)" ]; then
            echo "::error::Generated types are out of sync!"
            echo "Run 'npm run generate:types' locally and commit changes."
            git diff packages/shared-types/
            exit 1
          fi
```

## Emergency Procedures

### API Breaking Change Without Notice

1. **Immediate**: Contact Architecture Review Board
2. **Generate**: Run `npm run generate:types` to see impact
3. **Assess**: Review TypeScript compilation errors
4. **Fix**: Update frontend code to handle breaking changes
5. **Document**: Record incident for process improvement

### Generation Pipeline Failure

1. **Check**: API health endpoint
2. **Verify**: NSwag configuration
3. **Manual**: Review OpenAPI specification at `/swagger`
4. **Escalate**: Contact Backend Team Lead if API issues
5. **Fallback**: Use previous generated types temporarily

## Related Documentation

- **Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
- **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
- **Quick Reference**: `/docs/guides-setup/dto-quick-reference.md`
- **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

---

**Remember**: The whole point of NSwag is to eliminate manual TypeScript interface creation and prevent alignment issues. When in doubt, regenerate types!

*Last updated: 2025-08-19 - Created comprehensive NSwag workflow guide*