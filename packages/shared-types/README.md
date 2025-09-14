# @witchcityrope/shared-types

Auto-generated TypeScript types from the WitchCityRope .NET API using OpenAPI specification.

## Overview

This package automatically generates TypeScript types from the running .NET API's OpenAPI/Swagger specification. It provides:

- **Type-safe API interfaces** generated from C# DTOs
- **API client wrapper** with proper error handling
- **Helper functions** and type guards
- **Automatic port discovery** for flexible development setups

## Quick Start

```bash
# Generate types from running API
npm run generate

# Generate and validate types
npm run validate

# Clean and regenerate
npm run generate:force

# Watch for API changes (requires API to be running)
npm run generate:watch
```

## Architecture

### How It Works

1. **Port Discovery**: Automatically detects the running API on ports 5656, 5655, or 5653
2. **Schema Fetching**: Retrieves OpenAPI specification from `/swagger/v1/swagger.json`
3. **Type Generation**: Uses `openapi-typescript` to generate TypeScript interfaces
4. **Helper Creation**: Adds custom helper functions and API client wrapper
5. **Validation**: TypeScript compilation ensures generated types are valid

### Generated Files

```
src/generated/
├── api-types.ts      # Raw OpenAPI-generated types
├── api-helpers.ts    # Helper types and type guards
├── api-client.ts     # API client wrapper
├── version.ts        # Version information
└── index.ts          # Main export file
```

## Usage

### Import Types

```typescript
import { 
  UserDto, 
  EventDto, 
  LoginRequest,
  AdminDashboardResponse 
} from '@witchcityrope/shared-types';
```

### Use API Client

```typescript
import { apiClient } from '@witchcityrope/shared-types';

// Type-safe API calls
const user = await apiClient.getCurrentUser();
const events = await apiClient.getEvents();
```

### Use Type Guards

```typescript
import { isUserDto, isEventDto } from '@witchcityrope/shared-types';

if (isUserDto(data)) {
  // TypeScript knows data is UserDto
  console.log(data.email);
}
```

## Development Workflow

### Adding New API Endpoints

When you add new endpoints to the .NET API:

1. **Ensure DTOs are properly exposed** in the API controllers
2. **Run the API** locally (any port from 5656, 5655, 5653)
3. **Regenerate types**: `npm run generate`
4. **Update helpers** if needed (add new type exports in the generator script)
5. **Test the types**: `npm run validate`

### Port Configuration

The generator automatically tries these ports in order:
- `5656` (current default)
- `5655` (legacy default)
- `5653` (alternative)

Override with environment variable:
```bash
VITE_API_BASE_URL=http://localhost:8080 npm run generate
```

## Troubleshooting

### "API is not running" Error

**Problem**: Generator can't find running API
**Solution**: 
1. Start the API: `cd ../../apps/api && dotnet run`
2. Check the API is responding: `curl http://localhost:5656/health`
3. Verify Swagger is available: `curl http://localhost:5656/swagger/v1/swagger.json`

### Types Not Up-to-Date

**Problem**: Generated types don't include new DTOs
**Solution**:
1. Check DTOs are in API controllers and properly decorated
2. Ensure API is compiled and running with latest changes
3. Force regeneration: `npm run generate:force`

### TypeScript Errors After Generation

**Problem**: Generated types have compilation errors
**Solution**:
1. Check the generation script matches actual API schema
2. Update helper type exports in `scripts/generate-types-openapi.js`
3. Verify all imported types exist in the generated schema

## Configuration

### Environment Variables

```bash
# API base URL (auto-discovered if not set)
VITE_API_BASE_URL=http://localhost:5656

# Alternative discovery ports (comma-separated)
API_DISCOVERY_PORTS=5656,5655,5653

# Skip TypeScript validation during generation
SKIP_TYPE_VALIDATION=false

# Enable debug logging
ENABLE_DEBUG_LOGGING=false
```

### Scripts

- `generate` - Generate types from running API
- `generate:force` - Clean and regenerate types
- `generate:watch` - Watch API files and regenerate on changes
- `build` - Compile TypeScript types
- `validate` - Generate and validate types
- `clean` - Remove generated files

## Integration with CI/CD

For automated type generation in CI/CD:

```yaml
# Example GitHub Action step
- name: Generate API Types
  run: |
    # Start API in background
    cd apps/api && dotnet run --urls http://localhost:5656 &
    # Wait for API to be ready
    sleep 10
    # Generate types
    cd packages/shared-types && npm run generate
```

## Best Practices

1. **Keep types in sync**: Regenerate types after API changes
2. **Use type guards**: Validate API responses at runtime
3. **Version compatibility**: Check `GENERATED_AT` timestamp to ensure freshness
4. **Error handling**: Use the provided `ApiError` type for consistent error handling

## Migration Notes

### From NSwag (Previous)

This package previously used NSwag for type generation. Key changes:

- **Tool**: NSwag → openapi-typescript (better TypeScript support)
- **Port Discovery**: Manual configuration → automatic detection
- **Type Structure**: Different naming conventions
- **API Client**: Enhanced with better error handling and modern endpoints

### Breaking Changes

- Type names may have changed (e.g., `CreateEventRequest` → `UpdateEventRequest`)
- API client method signatures updated for new endpoints
- Import paths remain the same (`@witchcityrope/shared-types`)

## Contributing

When modifying the generation script:

1. **Test with running API**: Ensure script works with actual API
2. **Update type exports**: Add new types to helper file
3. **Validate generation**: Run `npm run validate` after changes
4. **Update documentation**: Keep README in sync with changes