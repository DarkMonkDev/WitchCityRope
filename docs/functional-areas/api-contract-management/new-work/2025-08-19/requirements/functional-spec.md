# Functional Specification: API Contract Management System
<!-- Last Updated: 2025-08-19 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Spec Agent -->
<!-- Status: Draft -->

## Technical Overview

This specification creates a comprehensive API Contract Management system to prevent frontend/backend API mismatches that occurred despite having fully tested endpoints. The system establishes a single source of truth for all API contracts, automated validation gates, and mandatory agent workflow requirements to ensure API contract consistency across the React + .NET WitchCityRope platform.

## Architecture Discovery Results

### Documents Reviewed:
- **backend-lessons-learned.md**: Lines 18-48 - Found NSwag auto-generation requirements and DTO alignment strategy
- **critical-process-failures-2025-08-19.md**: Lines 8-27 - Found API contract mismatch root causes
- **critical-analysis-missed-nswag-solution.md**: Lines 1-533 - Complete analysis of why existing solutions were missed
- **architecture-discovery-process.md**: Lines 30-183 - Found mandatory architecture discovery requirements
- **domain-layer-architecture.md**: Lines 725-997 - Found complete NSwag implementation specification
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 85-213 - Found existing API contract strategy
- **api-authentication-extracted.md**: Lines 34-448 - Found working authentication endpoints that were ignored

### Existing Solutions Found:
- **NSwag Pipeline**: Complete auto-generation from C# DTOs to TypeScript (lines 725-997 in domain-layer-architecture.md)
- **Authentication Endpoints**: Fully tested and documented API endpoints (api-authentication-extracted.md)
- **Architecture Discovery Process**: Mandatory pre-implementation validation (architecture-discovery-process.md)
- **DTO Alignment Strategy**: Complete strategy for preventing manual interface creation (DTO-ALIGNMENT-STRATEGY.md)

### Verification Statement:
"Confirmed that comprehensive API contract management infrastructure already exists but was not being enforced, leading to costly duplication and mismatches. This specification builds on existing architecture to create enforcement mechanisms."

## Microservices Architecture

**CRITICAL**: This is a Web+API microservices architecture:
- **Web Service** (React + Vite): UI/Auth at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React → HTTP → API → Database (NEVER React → Database directly)

### Component Structure
```
/docs/functional-areas/api-contract-management/
├── registry/
│   ├── api-contract-registry.md      # Central contract registry
│   ├── endpoint-ownership.md         # Clear ownership tracking
│   └── version-tracking.md           # Change management
├── validation/
│   ├── build-time-validation.md      # CI/CD integration
│   ├── runtime-validation.md         # Live contract checking
│   └── contract-testing.md           # Automated test patterns
├── agent-workflows/
│   ├── pre-api-development-checklist.md
│   ├── validation-gates.md
│   └── escalation-procedures.md
├── documentation-standards/
│   ├── api-documentation-requirements.md
│   ├── reference-patterns.md
│   └── update-procedures.md
└── tools/
    ├── nswag-maintenance.md
    ├── swagger-requirements.md
    └── ci-cd-integration.md
```

### Service Architecture
- **Web Service**: React components import ONLY from @witchcityrope/shared-types (generated)
- **API Service**: C# DTOs with comprehensive OpenAPI annotations as source of truth
- **Contract Generation**: NSwag auto-generates TypeScript from OpenAPI specification

## Data Models

### API Contract Registry Schema
```sql
-- Central registry table (conceptual - tracked in documentation)
CREATE TABLE api_contracts (
    id UUID PRIMARY KEY,
    endpoint_path VARCHAR(255) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    service_owner VARCHAR(50) NOT NULL, -- 'web' or 'api'
    dto_name VARCHAR(100),
    openapi_spec JSONB,
    last_updated TIMESTAMP DEFAULT NOW(),
    breaking_change_notice_date TIMESTAMP,
    deprecated_date TIMESTAMP,
    removal_date TIMESTAMP,
    documentation_path VARCHAR(500),
    test_coverage BOOLEAN DEFAULT FALSE,
    contract_hash VARCHAR(64), -- For change detection
    UNIQUE(endpoint_path, http_method)
);
```

### Contract Validation Metadata
```typescript
// Generated contract validation metadata
export interface ContractValidationMeta {
  endpoint: string;
  method: string;
  contractHash: string;
  lastGenerated: string;
  dtoTypes: string[];
  requiredValidations: ValidationRule[];
  breakingChanges: BreakingChange[];
}

export interface ValidationRule {
  type: 'required-field' | 'type-match' | 'enum-values';
  field: string;
  rule: string;
  severity: 'error' | 'warning';
}
```

## API Contract Registry

### 1. Central Source of Truth

#### Location and Structure
```
/docs/functional-areas/api-contract-management/registry/
├── api-contract-registry.md          # Master registry of all API endpoints
├── authentication-contracts.md       # Authentication endpoint contracts
├── user-management-contracts.md      # User management endpoint contracts
├── event-management-contracts.md     # Event endpoint contracts
└── contract-templates/
    ├── endpoint-template.md           # Standard endpoint documentation template
    ├── dto-specification-template.md  # DTO documentation template
    └── breaking-change-template.md    # Breaking change notification template
```

#### Contract Registry Format
Each endpoint must be documented with:
```markdown
## POST /api/auth/login

### Contract ID: AUTH_LOGIN_V1
### Owner: API Service (apps/api)
### DTO: LoginRequest → UserResponse
### OpenAPI Hash: sha256:abc123...
### Last Updated: 2025-08-19
### Status: ACTIVE

#### Request Contract
```typescript
interface LoginRequest {
  email: string;        // Required, email format
  password: string;     // Required, min 8 chars
}
```

#### Response Contract
```typescript
interface ApiResponse<UserDto> {
  success: boolean;
  data?: UserDto;
  message?: string;
  error?: string;
}
```

#### OpenAPI Specification
- Location: /api/swagger/v1/swagger.json
- Generated: Automatic via dotnet swagger
- NSwag Config: packages/shared-types/scripts/nswag.json

#### Testing Requirements
- Unit Tests: ✅ AuthControllerTests.cs
- Integration Tests: ✅ AuthIntegrationTests.cs
- E2E Tests: ✅ authentication.spec.ts (Playwright)
- Contract Tests: ✅ auth-contract.test.ts

#### Usage Examples
```typescript
// Frontend usage - ALWAYS use generated types
import { LoginRequest, UserDto, ApiResponse } from '@witchcityrope/shared-types';

const loginUser = async (request: LoginRequest): Promise<ApiResponse<UserDto>> => {
  return await apiClient.post<ApiResponse<UserDto>>('/api/auth/login', request);
};
```

#### Breaking Change History
- None (baseline endpoint)

#### Related Endpoints
- POST /api/auth/register
- GET /api/auth/user
- POST /api/auth/logout
```

### 2. Version Tracking and Change Management

#### Change Classification
```typescript
enum ChangeType {
  BREAKING = 'breaking',      // Requires 30-day notice
  ADDITIVE = 'additive',      // Safe, immediate deployment
  DEPRECATION = 'deprecation', // 90-day notice before removal
  REMOVAL = 'removal'         // Final step after deprecation period
}

interface ContractChange {
  changeId: string;
  endpoint: string;
  changeType: ChangeType;
  description: string;
  noticeDate: string;
  effectiveDate: string;
  migrationGuide?: string;
  automatedMigration: boolean;
}
```

#### Breaking Change Process
1. **30-Day Notice Required** - Document in contract registry
2. **Migration Guide Creation** - Step-by-step upgrade instructions
3. **Automated Migration** - Update generation scripts where possible
4. **Stakeholder Notification** - Frontend team, QA team, DevOps team
5. **Rollback Plan** - Documented procedure for reverting changes

### 3. Clear Ownership and Maintenance Responsibilities

#### Service Ownership Matrix
```markdown
| Service | Owns | Responsibilities |
|---------|------|------------------|
| API Service | C# DTOs, OpenAPI spec, business logic | Maintain DTO accuracy, comprehensive OpenAPI annotations, breaking change notices |
| Web Service | React components, HTTP clients | Import ONLY generated types, handle API responses correctly, report integration issues |
| Shared Types | Generated TypeScript interfaces | NSwag configuration, generation scripts, type validation |
| DevOps | CI/CD pipeline, contract validation | Build-time validation, deployment gates, monitoring |
| QA | Contract testing, validation | Contract test automation, breaking change validation |
```

#### Maintenance Schedules
- **Daily**: Automated contract hash validation in CI/CD
- **Weekly**: Manual review of contract registry for inconsistencies  
- **Monthly**: Breaking change impact assessment
- **Quarterly**: Full contract audit and cleanup

## Agent Workflow Requirements

### 1. Mandatory Pre-API Development Checklist

#### Phase 0: Architecture Discovery (MANDATORY)
```markdown
## BEFORE ANY API-RELATED DEVELOPMENT

### Step 1: Contract Registry Review
- [ ] Read /docs/functional-areas/api-contract-management/registry/api-contract-registry.md
- [ ] Search for existing endpoints covering your requirements
- [ ] Check endpoint ownership and status (ACTIVE/DEPRECATED/PLANNED)
- [ ] Verify no conflicts with planned changes

### Step 2: Existing Implementation Discovery
- [ ] Search functional areas for related API work: `grep -r "api/" docs/functional-areas/`
- [ ] Check vertical slice implementations: Look for "api-authentication-extracted.md" patterns
- [ ] Review lessons learned: Check backend-lessons-learned.md for API patterns
- [ ] Validate no duplicate work: Confirm API endpoints don't already exist

### Step 3: NSwag Pipeline Validation
- [ ] Read domain-layer-architecture.md lines 725-997 for NSwag implementation
- [ ] Read DTO-ALIGNMENT-STRATEGY.md lines 85-213 for auto-generation requirements
- [ ] Verify packages/shared-types/ structure exists
- [ ] Check npm run generate:types script functionality

### Step 4: Documentation Requirements Check
- [ ] Review API documentation standards in documentation-standards/
- [ ] Check OpenAPI annotation requirements for C# DTOs
- [ ] Verify Swagger/OpenAPI specification generation works
- [ ] Confirm TypeScript generation pipeline is operational
```

#### API Development Validation Gates
```markdown
## VALIDATION GATES - MUST PASS BEFORE PROCEEDING

### Gate 1: No Duplication
- [ ] **VERIFIED**: No existing endpoint serves this purpose
- [ ] **DOCUMENTED**: Why existing endpoints are insufficient
- [ ] **APPROVED**: Architecture team sign-off if deviating from existing patterns

### Gate 2: Contract Registry Updated
- [ ] **REGISTERED**: New endpoint added to contract registry
- [ ] **SPECIFIED**: Complete DTO specification documented
- [ ] **REVIEWED**: OpenAPI annotations comprehensive and accurate

### Gate 3: Generation Pipeline Ready
- [ ] **TESTED**: NSwag generation produces correct TypeScript
- [ ] **VALIDATED**: Generated types match C# DTOs exactly
- [ ] **INTEGRATED**: CI/CD pipeline includes contract validation

### Gate 4: Testing Requirements Met
- [ ] **UNIT**: Controller unit tests with DTO validation
- [ ] **INTEGRATION**: Full API integration tests
- [ ] **CONTRACT**: Dedicated contract testing (API + Frontend)
- [ ] **E2E**: End-to-end user workflow tests
```

### 2. Specific Validation Gates

#### Pre-Implementation Gates
1. **Contract Existence Check**: Must verify endpoint doesn't already exist
2. **Architecture Alignment**: Must align with existing API patterns
3. **Generation Pipeline Test**: Must validate TypeScript generation works
4. **Documentation Complete**: Must have comprehensive OpenAPI annotations

#### Pre-Deployment Gates
1. **Contract Tests Pass**: All contract tests must pass
2. **Type Generation Success**: NSwag generation must succeed without errors
3. **Breaking Change Validation**: Any breaking changes properly documented
4. **Integration Test Coverage**: API + Frontend integration tests passing

#### Post-Deployment Gates
1. **Runtime Validation**: Live API matches documented contract
2. **Frontend Integration**: React components using generated types correctly
3. **Performance Benchmarks**: API response times within SLA
4. **Monitoring Active**: Contract compliance monitoring in place

### 3. Clear Escalation Paths

#### Escalation Matrix
```markdown
| Issue Type | First Contact | Escalation | Timeline |
|------------|---------------|------------|----------|
| Contract Mismatch | Backend Developer | API Team Lead | 2 hours |
| Breaking Change Conflict | Frontend Developer | Architecture Team | 4 hours |
| Generation Pipeline Failure | DevOps Engineer | Technical Lead | 1 hour |
| Performance SLA Breach | QA Team | Product Owner | 8 hours |
| Security Concern | Security Team | CTO | Immediate |
```

#### Escalation Procedures
1. **Level 1**: Developer-to-developer resolution
2. **Level 2**: Team lead involvement with architecture review
3. **Level 3**: Cross-team coordination with project management
4. **Level 4**: Executive escalation with business impact assessment

## Documentation Standards

### 1. API Contract Documentation Location

#### Primary Documentation
```
/docs/functional-areas/api-contract-management/registry/
├── api-contract-registry.md           # Master index of all contracts
├── [domain]-contracts.md              # Domain-specific contract details
└── contract-templates/                # Standard documentation templates
```

#### Supporting Documentation
```
/docs/architecture/react-migration/
├── domain-layer-architecture.md       # NSwag implementation details (lines 725-997)
├── DTO-ALIGNMENT-STRATEGY.md         # Auto-generation requirements (lines 85-213)
└── migration-plan.md                 # Overall API strategy (lines 11-21)
```

#### Generated Documentation
```
packages/shared-types/
├── src/generated/api-client.ts        # Generated TypeScript types and client
├── docs/api-documentation.md          # Auto-generated API documentation
└── validation/contract-metadata.json  # Contract validation metadata
```

### 2. Reference Patterns in Code

#### C# DTO Documentation Requirements
```csharp
/// <summary>
/// User login request containing authentication credentials
/// Contract: AUTH_LOGIN_V1
/// Generated: UserDto response type
/// Breaking Changes: None planned
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address - must be valid email format
    /// Required: true
    /// Validation: EmailAddress attribute
    /// Example: "user@example.com"
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password - minimum 8 characters
    /// Required: true  
    /// Validation: StringLength attribute
    /// Security: Never logged or exposed
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
```

#### TypeScript Import Requirements
```typescript
// ✅ CORRECT - Always import from generated types
import { 
  LoginRequest, 
  UserDto, 
  ApiResponse 
} from '@witchcityrope/shared-types';

// ❌ WRONG - Never create manual interfaces
interface LoginRequest { // DON'T DO THIS
  email: string;
  password: string;
}
```

#### API Endpoint Documentation
```csharp
/// <summary>
/// Authenticate user with email and password
/// </summary>
/// <param name="request">Login credentials</param>
/// <returns>User data and authentication status</returns>
/// <response code="200">Login successful - returns user data</response>
/// <response code="400">Invalid request data</response>
/// <response code="401">Authentication failed</response>
/// <response code="429">Too many login attempts</response>
[HttpPost("login")]
[ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(typeof(ApiResponse<object>), 401)]
[ProducesResponseType(typeof(ApiResponse<object>), 429)]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
```

### 3. Update Procedures

#### When API Changes Occur
1. **Update C# DTO**: Modify DTO with proper OpenAPI annotations
2. **Update Contract Registry**: Document change in registry with change type
3. **Regenerate Types**: Run `npm run generate:types` 
4. **Update Tests**: Modify contract tests and integration tests
5. **Update Documentation**: Update endpoint documentation
6. **Notify Stakeholders**: Send breaking change notices if applicable

#### Documentation Review Process
1. **Developer**: Creates/updates contract documentation
2. **Peer Review**: Another developer reviews accuracy
3. **Architecture Review**: Ensures alignment with patterns
4. **QA Review**: Validates testability and completeness
5. **Documentation Merge**: Changes integrated into main documentation

## Validation Processes

### 1. Build-Time Validation

#### CI/CD Pipeline Integration
```yaml
# .github/workflows/api-contract-validation.yml
name: API Contract Validation

on: [push, pull_request]

jobs:
  contract-validation:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Start API Service
        run: |
          cd apps/api
          dotnet run &
          sleep 30  # Wait for API to start
          
      - name: Validate OpenAPI Specification
        run: |
          curl -f http://localhost:5653/swagger/v1/swagger.json > api-spec.json
          npx swagger-parser validate api-spec.json
          
      - name: Generate TypeScript Types
        run: |
          cd packages/shared-types
          npm run generate:types
          
      - name: Validate Generated Types
        run: |
          cd packages/shared-types
          npx tsc --noEmit
          
      - name: Run Contract Tests
        run: |
          npm run test:contracts
          
      - name: Validate Contract Hash Changes
        run: |
          node scripts/validate-contract-changes.js
```

#### Contract Hash Validation
```javascript
// scripts/validate-contract-changes.js
const crypto = require('crypto');
const fs = require('fs');

function generateContractHash(openApiSpec) {
  // Generate deterministic hash from OpenAPI spec
  const content = JSON.stringify(openApiSpec, Object.keys(openApiSpec).sort());
  return crypto.createHash('sha256').update(content).digest('hex');
}

function validateContractChanges() {
  const currentSpec = JSON.parse(fs.readFileSync('api-spec.json'));
  const currentHash = generateContractHash(currentSpec);
  
  const registryHash = getRegistryHash('contract-registry.json');
  
  if (currentHash !== registryHash) {
    console.error('❌ Contract hash mismatch - API changes not documented in registry');
    process.exit(1);
  }
  
  console.log('✅ Contract validation passed');
}
```

### 2. Runtime Validation

#### Contract Compliance Monitoring
```typescript
// Runtime contract validation middleware
export class ContractValidationMiddleware {
  async validateRequest(req: Request, endpoint: string) {
    const contractSpec = await this.getContractSpec(endpoint);
    const isValid = this.validateAgainstSchema(req.body, contractSpec.requestSchema);
    
    if (!isValid) {
      this.logContractViolation(endpoint, 'request', req.body, contractSpec);
      throw new ContractViolationError('Request does not match API contract');
    }
  }
  
  async validateResponse(res: Response, endpoint: string) {
    const contractSpec = await this.getContractSpec(endpoint);
    const isValid = this.validateAgainstSchema(res.body, contractSpec.responseSchema);
    
    if (!isValid) {
      this.logContractViolation(endpoint, 'response', res.body, contractSpec);
      // Log but don't fail in production
    }
  }
}
```

#### Live Contract Monitoring
```csharp
// API service contract monitoring
public class ContractMonitoringMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Capture request
        var request = await CaptureRequest(context.Request);
        
        await next(context);
        
        // Capture response  
        var response = await CaptureResponse(context.Response);
        
        // Validate against OpenAPI spec
        var endpoint = GetEndpointKey(context.Request);
        await ValidateContractCompliance(endpoint, request, response);
    }
    
    private async Task ValidateContractCompliance(string endpoint, object request, object response)
    {
        var spec = await _contractRegistry.GetSpecAsync(endpoint);
        var violations = _validator.Validate(request, response, spec);
        
        if (violations.Any())
        {
            _logger.LogWarning("Contract violations detected: {Violations}", violations);
            _metrics.IncrementContractViolations(endpoint, violations.Count);
        }
    }
}
```

### 3. Contract Testing Requirements

#### Contract Test Structure
```typescript
// tests/contract/auth-endpoints.contract.test.ts
describe('Authentication API Contracts', () => {
  test('POST /api/auth/login - matches OpenAPI specification', async () => {
    // Arrange
    const validRequest: LoginRequest = {
      email: 'test@example.com',
      password: 'TestPassword123!'
    };
    
    // Act
    const response = await apiClient.post('/api/auth/login', validRequest);
    
    // Assert - Response structure
    expect(response.data).toMatchSchema(ApiResponseSchema);
    expect(response.data.data).toMatchSchema(UserDtoSchema);
    
    // Assert - Contract compliance
    const contractSpec = await getContractSpec('AUTH_LOGIN_V1');
    expect(response).toMatchOpenApiSpec(contractSpec);
  });
  
  test('POST /api/auth/login - handles invalid request gracefully', async () => {
    // Test contract error handling
    const invalidRequest = { email: 'invalid' };
    
    const response = await apiClient.post('/api/auth/login', invalidRequest);
    
    expect(response.status).toBe(400);
    expect(response.data.success).toBe(false);
    expect(response.data.error).toBeDefined();
  });
});
```

#### Automated Contract Test Generation
```javascript
// scripts/generate-contract-tests.js
function generateContractTests(openApiSpec) {
  const endpoints = extractEndpoints(openApiSpec);
  
  endpoints.forEach(endpoint => {
    generatePositiveTests(endpoint);
    generateNegativeTests(endpoint);
    generateEdgeCaseTests(endpoint);
  });
}

function generatePositiveTests(endpoint) {
  const testCode = `
test('${endpoint.method} ${endpoint.path} - valid request', async () => {
  const request = ${generateValidRequest(endpoint.requestSchema)};
  const response = await apiClient.${endpoint.method.toLowerCase()}('${endpoint.path}', request);
  
  expect(response).toMatchSchema(${endpoint.responseSchema});
});`;
  
  writeTestFile(endpoint, testCode);
}
```

## Change Management

### 1. Breaking Change Process

#### Breaking Change Classification
```typescript
enum BreakingChangeType {
  FIELD_REMOVED = 'field_removed',           // 30-day notice
  FIELD_TYPE_CHANGED = 'field_type_changed', // 30-day notice  
  FIELD_REQUIRED = 'field_made_required',    // 30-day notice
  ENDPOINT_REMOVED = 'endpoint_removed',     // 90-day notice
  RESPONSE_STRUCTURE = 'response_structure_changed', // 30-day notice
  VALIDATION_STRICTER = 'validation_made_stricter'   // 14-day notice
}
```

#### Breaking Change Workflow
1. **Impact Assessment** (1-2 days)
   - Identify affected endpoints and consumers
   - Estimate migration effort and timeline
   - Assess business impact and urgency

2. **Notice Period** (14-90 days based on change type)
   - Document change in contract registry
   - Notify all stakeholders via established channels
   - Create detailed migration guide

3. **Migration Support** (Throughout notice period)
   - Provide sample code and migration examples  
   - Offer technical support for integration questions
   - Update documentation and generated types

4. **Implementation** (Deployment day)
   - Deploy API changes
   - Validate no unexpected impacts
   - Monitor for increased error rates

5. **Verification** (1 week post-deployment)
   - Confirm all consumers migrated successfully
   - Remove deprecated functionality if planned
   - Update contract registry status

### 2. Communication Requirements

#### Stakeholder Notification Matrix
```markdown
| Change Type | Frontend Team | QA Team | DevOps | Product Owner | Timeline |
|-------------|---------------|---------|---------|---------------|----------|
| Breaking Change | Email + Slack | Email | Email + Incident | Meeting | 30 days prior |
| New Endpoint | Slack | Slack | PR Review | Optional | Next sprint |
| Deprecation | Email + Docs | Email | PR Review | Meeting | 90 days prior |
| Performance Change | Slack | Slack | Email | Optional | 7 days prior |
```

#### Communication Templates
```markdown
## Breaking Change Notice Template

**Subject**: [BREAKING CHANGE] API Contract Change - {Endpoint} - Effective {Date}

### Summary
Brief description of the breaking change and why it's necessary.

### Impact Assessment  
- **Affected Endpoints**: List of endpoints
- **Affected Services**: Frontend, mobile app, integrations
- **Migration Effort**: Estimated hours/days
- **Business Impact**: User-facing changes

### Migration Guide
Step-by-step instructions for updating code:

1. Update imports to use new types from @witchcityrope/shared-types
2. Modify request/response handling logic
3. Update tests to reflect new contract
4. Validate integration still works

### Timeline
- **Notice Date**: {Date}
- **Migration Support Available**: {Date range} 
- **Effective Date**: {Date}
- **Old Endpoint Removal**: {Date} (if applicable)

### Support
- Technical questions: Slack #api-support
- Migration assistance: Book time with API team
- Escalation: Email api-team@witchcityrope.com
```

### 3. Migration Strategies

#### Versioned API Strategy
```csharp
// Support multiple API versions during transition
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> LoginV1([FromBody] LoginRequestV1 request)
    {
        // Legacy implementation
    }
    
    [HttpPost("login")] 
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> LoginV2([FromBody] LoginRequestV2 request)
    {
        // New implementation
    }
}
```

#### Backward Compatibility Helpers
```csharp
public class ApiCompatibilityService
{
    public async Task<TNew> ConvertLegacyRequest<TOld, TNew>(TOld oldRequest)
    {
        // Convert legacy request format to new format
        return _mapper.Map<TNew>(oldRequest);
    }
    
    public async Task<TOld> ConvertNewResponse<TNew, TOld>(TNew newResponse)  
    {
        // Convert new response format back to legacy format
        return _mapper.Map<TOld>(newResponse);
    }
}
```

#### Gradual Migration Support
```typescript
// Frontend wrapper for gradual migration
export class ApiMigrationClient {
  async loginUser(request: LoginRequest | LoginRequestV2): Promise<UserDto> {
    // Detect request version and route appropriately
    if (isLegacyRequest(request)) {
      return await this.callLegacyEndpoint('/api/v1/auth/login', request);
    } else {
      return await this.callNewEndpoint('/api/v2/auth/login', request);
    }
  }
  
  private isLegacyRequest(request: any): request is LoginRequest {
    // Logic to detect legacy request format
    return 'oldField' in request;
  }
}
```

## Tool Integration

### 1. NSwag Pipeline Maintenance

#### Configuration Management
```json
// packages/shared-types/scripts/nswag.json
{
  "runtime": "Net80",
  "defaultVariables": {
    "baseUrl": "http://localhost:5653"
  },
  "documentGenerator": {
    "fromDocument": {
      "url": "$(baseUrl)/swagger/v1/swagger.json",
      "output": null
    }
  },
  "codeGenerators": {
    "openApiToTypeScript": {
      "className": "ApiClient",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 5.0,
      "template": "Fetch",
      "generateClientClasses": true,
      "generateClientInterfaces": true,
      "exportTypes": true,
      "generateDtoTypes": true,
      "operationGenerationMode": "MultipleClientsFromOperationId",
      "markOptionalProperties": true,
      "typeStyle": "Interface",
      "generateDefaultValues": true,
      "importRequiredTypes": true,
      "output": "src/generated/api-client.ts"
    }
  }
}
```

#### Maintenance Scripts
```bash
#!/bin/bash
# scripts/maintain-nswag-pipeline.sh

# Validate NSwag configuration
echo "🔍 Validating NSwag configuration..."
npx nswag validate packages/shared-types/scripts/nswag.json

# Check API availability
echo "📡 Checking API health..."
curl -f http://localhost:5653/health || {
  echo "❌ API not available - start API first"
  exit 1
}

# Generate types
echo "🏗️ Generating TypeScript types..."
cd packages/shared-types
npm run generate:types

# Validate generated types
echo "✅ Validating generated types..."
npx tsc --noEmit

# Run contract tests
echo "🧪 Running contract tests..."
npm run test:contracts

echo "🎉 NSwag pipeline maintenance completed"
```

#### Health Monitoring
```typescript
// scripts/nswag-health-check.js
export class NSwagHealthCheck {
  async checkPipeline(): Promise<HealthCheckResult> {
    const checks = [
      this.checkApiAvailability(),
      this.checkOpenApiSpec(),
      this.checkNSwagConfig(),
      this.checkGeneratedTypes(),
      this.checkTypeValidation()
    ];
    
    const results = await Promise.allSettled(checks);
    
    return {
      status: results.every(r => r.status === 'fulfilled') ? 'healthy' : 'unhealthy',
      checks: results.map(this.formatCheckResult),
      timestamp: new Date().toISOString()
    };
  }
  
  private async checkApiAvailability(): Promise<CheckResult> {
    try {
      const response = await fetch('http://localhost:5653/health');
      return { name: 'API Availability', status: 'pass', duration: response.duration };
    } catch (error) {
      return { name: 'API Availability', status: 'fail', error: error.message };
    }
  }
}
```

### 2. Swagger/OpenAPI Requirements

#### OpenAPI Documentation Standards
```csharp
// Required OpenAPI configuration for all APIs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WitchCityRope API",
        Version = "v1",
        Description = "RESTful API for WitchCityRope platform",
        Contact = new OpenApiContact
        {
            Name = "API Team",
            Email = "api-team@witchcityrope.com"
        }
    });
    
    // Include XML documentation for all endpoints
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    
    // Add JWT authentication scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    // Add contract metadata
    c.SchemaFilter<ContractMetadataSchemaFilter>();
    c.OperationFilter<ContractMetadataOperationFilter>();
});
```

#### DTO Annotation Requirements
```csharp
/// <summary>
/// User registration request
/// Contract-ID: AUTH_REGISTER_V1
/// Breaking-Changes: None planned
/// Related-DTOs: UserDto, ApiResponse
/// </summary>
[JsonObject(Title = "RegisterRequest")]
public class RegisterRequest
{
    /// <summary>
    /// Email address for the new account
    /// </summary>
    /// <example>user@example.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for the new account (minimum 8 characters)
    /// </summary>
    /// <example>SecurePassword123!</example>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Scene name for community identification (optional)
    /// </summary>
    /// <example>RopeArtist2024</example>
    [StringLength(50, ErrorMessage = "Scene name cannot exceed 50 characters")]
    [JsonPropertyName("sceneName")]
    public string? SceneName { get; set; }
}
```

### 3. CI/CD Integration Points

#### GitHub Actions Workflow
```yaml
# .github/workflows/api-contract-validation.yml
name: API Contract Management

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  validate-contracts:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: testpassword
          POSTGRES_DB: witchcityrope_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '18'
        cache: 'npm'
        
    - name: Install dependencies
      run: |
        cd packages/shared-types
        npm ci
        
    - name: Start API service
      run: |
        cd apps/api
        dotnet run &
        sleep 30  # Wait for API startup
        
    - name: Validate OpenAPI specification
      run: |
        curl -f http://localhost:5653/swagger/v1/swagger.json > openapi-spec.json
        npx swagger-parser validate openapi-spec.json
        
    - name: Generate TypeScript types
      run: |
        cd packages/shared-types
        npm run generate:types
        
    - name: Validate TypeScript compilation
      run: |
        cd packages/shared-types
        npx tsc --noEmit
        
    - name: Run contract tests
      run: |
        npm run test:contracts
        
    - name: Validate contract registry
      run: |
        node scripts/validate-contract-registry.js
        
    - name: Check breaking changes
      run: |
        node scripts/check-breaking-changes.js
        
  contract-monitoring:
    needs: validate-contracts
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - name: Update contract registry
      run: |
        node scripts/update-contract-registry.js
        
    - name: Generate contract documentation
      run: |
        node scripts/generate-contract-docs.js
        
    - name: Commit updated contracts
      uses: stefanzweifel/git-auto-commit-action@v4
      with:
        commit_message: 'docs: Update API contract registry [skip ci]'
        file_pattern: 'docs/functional-areas/api-contract-management/registry/*'
```

#### Deployment Gates
```yaml
# Deployment pipeline with contract validation gates
deploy:
  needs: validate-contracts
  runs-on: ubuntu-latest
  
  steps:
  - name: Pre-deployment contract check
    run: |
      # Ensure no unreviewed breaking changes
      if [[ -f breaking-changes.json && $(cat breaking-changes.json | jq '.unreviewed | length') -gt 0 ]]; then
        echo "❌ Unreviewed breaking changes detected - deployment blocked"
        exit 1
      fi
      
  - name: Deploy to staging
    run: |
      # Deploy application
      
  - name: Post-deployment contract validation
    run: |
      # Validate deployed API matches contract specifications
      node scripts/validate-deployed-contracts.js staging
      
  - name: Production deployment gate
    if: success()
    run: |
      # All gates passed - proceed to production
      echo "✅ All contract validations passed - ready for production"
```

## Acceptance Criteria

### Technical Criteria for Completion

#### API Contract Registry
- [ ] **Central Registry Created**: Master contract registry documenting all API endpoints
- [ ] **Contract Templates**: Standard templates for endpoint and DTO documentation
- [ ] **Ownership Matrix**: Clear responsibility assignment for all contracts
- [ ] **Change Classification**: Breaking change types defined and communicated

#### Agent Workflow Integration  
- [ ] **Pre-Development Checklist**: Mandatory validation gates before API work
- [ ] **Validation Pipeline**: Automated contract validation in CI/CD
- [ ] **Escalation Procedures**: Clear escalation paths for contract conflicts
- [ ] **Documentation Standards**: Required OpenAPI annotation standards

#### NSwag Pipeline Robustness
- [ ] **Health Monitoring**: Automated pipeline health checks
- [ ] **Maintenance Scripts**: Automated maintenance and validation scripts
- [ ] **Error Recovery**: Graceful handling of generation failures
- [ ] **Performance Monitoring**: Pipeline execution time tracking

#### Contract Testing Coverage
- [ ] **Unit Tests**: DTO validation and serialization tests
- [ ] **Integration Tests**: Full API endpoint contract testing
- [ ] **Contract Tests**: Dedicated contract validation test suite
- [ ] **E2E Tests**: End-to-end user workflow validation

#### Change Management Process
- [ ] **Breaking Change Workflow**: 30-90 day notice process implemented
- [ ] **Migration Support**: Automated migration tools where possible
- [ ] **Stakeholder Communication**: Notification templates and procedures
- [ ] **Rollback Procedures**: Documented rollback process for contract changes

#### Monitoring and Validation
- [ ] **Runtime Validation**: Live contract compliance monitoring
- [ ] **Build-Time Validation**: Automated contract validation in CI/CD
- [ ] **Performance Metrics**: API response time and contract compliance tracking
- [ ] **Alert Systems**: Automated alerts for contract violations

## Dependencies

### NuGet Packages Required
- **Swashbuckle.AspNetCore** (>= 6.5.0) - OpenAPI specification generation
- **Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer** (>= 5.0.0) - API versioning
- **NJsonSchema.Annotations** (>= 10.8.0) - Enhanced JSON schema annotations
- **Swashbuckle.AspNetCore.Annotations** (>= 6.5.0) - Swagger annotations

### NPM Packages Required
- **nswag** (>= 18.0.0) - TypeScript generation from OpenAPI
- **swagger-parser** (>= 10.0.0) - OpenAPI specification validation
- **@types/swagger-schema-official** (>= 2.0.0) - TypeScript types for OpenAPI
- **json-schema-to-typescript** (>= 12.0.0) - JSON schema conversion utilities

### External Services
- **PostgreSQL Database**: Contract metadata and change tracking
- **GitHub Actions**: Automated contract validation pipeline
- **Swagger UI**: Interactive API documentation and testing

### Configuration Requirements
- **Environment Variables**: API_BASE_URL, JWT_SECRET, SERVICE_SECRET
- **NSwag Configuration**: packages/shared-types/scripts/nswag.json
- **OpenAPI Configuration**: apps/api/Program.cs Swagger setup
- **CI/CD Secrets**: Database connection strings, API secrets

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
1. **Create API Contract Registry**: Document all existing endpoints
2. **Establish Documentation Standards**: OpenAPI annotation requirements
3. **Implement Basic Validation**: Contract hash validation in CI/CD
4. **Agent Workflow Integration**: Update agent lessons with validation requirements

### Phase 2: Automation (Week 3-4)  
1. **Enhanced NSwag Pipeline**: Health monitoring and maintenance scripts
2. **Contract Testing Suite**: Automated contract validation tests
3. **Runtime Validation**: Live contract compliance monitoring
4. **Change Management Process**: Breaking change workflow implementation

### Phase 3: Advanced Features (Week 5-6)
1. **Performance Monitoring**: Contract compliance and API performance tracking
2. **Advanced Validation**: Schema drift detection and automated fixes
3. **Migration Tools**: Automated migration assistance for breaking changes
4. **Comprehensive Documentation**: Complete contract management documentation

### Phase 4: Monitoring and Optimization (Week 7-8)
1. **Metrics Dashboard**: Contract compliance and API health visualization
2. **Alerting System**: Automated notifications for contract violations
3. **Process Optimization**: Streamline validation workflows based on usage
4. **Training Materials**: Developer onboarding and best practices documentation

---

**Remember**: This specification builds on the comprehensive architecture already in place (NSwag, DTO alignment strategy, architecture discovery process) to create enforcement mechanisms that prevent API contract mismatches. The goal is to ensure the existing excellent technical solutions are properly utilized through process and tooling improvements.

*Created: 2025-08-19 - Response to critical API contract management requirements*
*References: Lines 725-997 of domain-layer-architecture.md, api-authentication-extracted.md patterns, critical process failure analysis*