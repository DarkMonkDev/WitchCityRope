# Environment-Specific Testing Configuration

## Overview
This document defines how PayPal integration testing works across different environments, ensuring CI/CD uses mocks while development and staging can use real PayPal sandbox.

## Environment Testing Matrix

| Environment | PayPal Mode | Webhook Verification | Testing Method | URL |
|-------------|-------------|---------------------|----------------|-----|
| **Local Dev** | Sandbox | Disabled | Real PayPal Sandbox | https://dev-api.chadfbennett.com |
| **CI/CD (GitHub)** | Mocked | Disabled | Mock Services | N/A (internal) |
| **Staging** | Sandbox | Enabled | Real PayPal Sandbox | https://staging-api.witchcityrope.com |
| **Production** | Live | Enabled | Real PayPal Live | https://api.witchcityrope.com |

## 1. Local Development Configuration

### Environment File (.env.development)
```env
# PayPal Configuration
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=AaTUvkwNVutLN6ujfPHX7wk1lh0vndE3wAxZwM5-pTgS38-AJNheP2bYH_DmEr22wy5lVubJEL3dEXZI
PAYPAL_CLIENT_SECRET=EB2JmD74p6d9PEQ6EOAKzPeclO9tWnaVspuniZMSZmU78TD6tYdJS4yHKH-2Tos0ur7KD2nbuSNuLtLx
PAYPAL_WEBHOOK_ID=1PH29187W48812152
WEBHOOK_BASE_URL=https://dev-api.chadfbennett.com
SKIP_WEBHOOK_VERIFICATION=true  # Simplify local testing

# API Configuration
API_PORT=5655
```

### Testing Approach
- Uses real PayPal Sandbox API
- Cloudflare tunnel provides stable webhook URL
- Webhook verification disabled for easier debugging
- Can test actual payment flows end-to-end

## 2. CI/CD Configuration (GitHub Actions)

### Environment File (.env.test)
```env
# Mock all external services
USE_MOCK_PAYMENT_SERVICE=true
SKIP_WEBHOOK_VERIFICATION=true
PAYPAL_MODE=mock
```

### Mock Service Implementation
Create `/apps/api/Services/MockPayPalService.cs`:
```csharp
public class MockPayPalService : IPayPalService
{
    public Task<PayPalOrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        return Task.FromResult(new PayPalOrderResponse
        {
            Id = $"MOCK-{Guid.NewGuid()}",
            Status = "CREATED",
            Links = new[]
            {
                new PayPalLink { Rel = "approve", Href = "/mock/paypal/approve" }
            }
        });
    }

    public Task<bool> VerifyWebhookSignatureAsync(string webhookId, WebhookEvent evt)
    {
        // Always return true in mocked mode
        return Task.FromResult(true);
    }

    public Task<CaptureOrderResponse> CaptureOrderAsync(string orderId)
    {
        return Task.FromResult(new CaptureOrderResponse
        {
            Id = orderId,
            Status = "COMPLETED",
            PurchaseUnits = new[]
            {
                new PurchaseUnit
                {
                    Payments = new PaymentCollection
                    {
                        Captures = new[]
                        {
                            new Capture
                            {
                                Id = $"CAPTURE-{Guid.NewGuid()}",
                                Status = "COMPLETED",
                                Amount = new Money { Value = "50.00", CurrencyCode = "USD" }
                            }
                        }
                    }
                }
            }
        });
    }
}
```

### Service Registration (Program.cs)
```csharp
// In Program.cs or Startup.cs
if (builder.Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE"))
{
    builder.Services.AddSingleton<IPayPalService, MockPayPalService>();
}
else
{
    builder.Services.AddScoped<IPayPalService, PayPalService>();
}
```

### GitHub Actions Workflow
`.github/workflows/test.yml`:
```yaml
name: Run Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '20'
        
    - name: Create test environment file
      run: |
        cat > .env.test << EOF
        USE_MOCK_PAYMENT_SERVICE=true
        SKIP_WEBHOOK_VERIFICATION=true
        DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=test;Username=postgres;Password=postgres
        JWT_SECRET=test-secret-key
        EOF
        
    - name: Install dependencies
      run: |
        dotnet restore
        cd apps/web && npm ci
        
    - name: Run API tests with mocks
      run: |
        dotnet test tests/WitchCityRope.Core.Tests/ --environment Test
        dotnet test tests/WitchCityRope.Api.Tests/ --environment Test
        
    - name: Run E2E tests with mocks
      run: |
        # Start API with mock services
        cd apps/api
        dotnet run --environment Test &
        API_PID=$!
        
        # Wait for API to be ready
        sleep 10
        
        # Run Playwright tests
        cd ../../apps/web
        npm run test:e2e:ci
        
        # Cleanup
        kill $API_PID
```

## 3. Staging Configuration

### Environment File (.env.staging)
```env
# PayPal Sandbox with real verification
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=${STAGING_PAYPAL_CLIENT_ID}
PAYPAL_CLIENT_SECRET=${STAGING_PAYPAL_SECRET}
PAYPAL_WEBHOOK_ID=1GN678263B992340W
WEBHOOK_BASE_URL=https://staging-api.witchcityrope.com
SKIP_WEBHOOK_VERIFICATION=false  # Enable verification

# API Configuration  
API_PORT=5655
```

### Testing Approach
- Uses real PayPal Sandbox API
- Full webhook verification enabled
- Tests actual payment flows with real external service
- Validates production-like scenarios

## 4. Production Configuration

### Environment File (.env.production)
```env
# PayPal Live Mode
PAYPAL_MODE=live
PAYPAL_CLIENT_ID=${PROD_PAYPAL_CLIENT_ID}
PAYPAL_CLIENT_SECRET=${PROD_PAYPAL_SECRET}
PAYPAL_WEBHOOK_ID=93J292600X332032T
WEBHOOK_BASE_URL=https://api.witchcityrope.com
SKIP_WEBHOOK_VERIFICATION=false  # Always verify in production

# API Configuration
API_PORT=5655
```

## 5. E2E Test Configuration

### Playwright Tests with Environment Detection
`tests/e2e/payment.spec.ts`:
```typescript
import { test, expect } from '@playwright/test';

const IS_CI = process.env.CI === 'true';
const API_URL = process.env.API_URL || 'http://localhost:5655';

test.describe('Payment Flow', () => {
  if (IS_CI) {
    // CI Mode: Use mocked responses
    test.beforeEach(async ({ page }) => {
      await page.route('**/api/payments/create-order', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            orderId: 'MOCK-ORDER-123',
            approveUrl: `${API_URL}/mock/paypal/approve`
          })
        });
      });

      await page.route('**/api/payments/capture/*', route => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            status: 'COMPLETED',
            paymentId: 'MOCK-PAYMENT-123'
          })
        });
      });
    });
  }

  test('complete payment flow', async ({ page }) => {
    await page.goto('/events/123/payment');
    
    // Fill payment form
    await page.fill('[data-testid="cardholder-name"]', 'Test User');
    
    if (!IS_CI) {
      // Real PayPal flow
      await page.click('[data-testid="paypal-button"]');
      // Handle PayPal redirect...
    } else {
      // Mocked flow
      await page.click('[data-testid="pay-button"]');
    }
    
    await expect(page.locator('[data-testid="payment-success"]')).toBeVisible();
  });
});
```

## 6. Mock Webhook Testing

### Webhook Test Fixtures
Store in `/tests/fixtures/paypal-webhooks/`:

`payment-completed.json`:
```json
{
  "id": "WH-TEST-001",
  "event_type": "PAYMENT.CAPTURE.COMPLETED",
  "resource_type": "capture",
  "resource": {
    "id": "CAPTURE-TEST-123",
    "amount": {
      "currency_code": "USD",
      "value": "50.00"
    },
    "status": "COMPLETED",
    "custom_id": "EVENT-123-USER-456"
  }
}
```

### Integration Test Using Fixtures
```csharp
[Fact]
public async Task ProcessWebhook_PaymentCompleted_UpdatesDatabase()
{
    // Arrange
    var webhook = LoadFixture("payment-completed.json");
    
    // Act
    var result = await _webhookProcessor.ProcessAsync(webhook);
    
    // Assert
    Assert.True(result.IsSuccess);
    var payment = await _dbContext.Payments
        .FirstOrDefaultAsync(p => p.ExternalId == "CAPTURE-TEST-123");
    Assert.NotNull(payment);
    Assert.Equal(PaymentStatus.Completed, payment.Status);
}
```

## 7. Running Tests by Environment

### Local Development
```bash
# Real PayPal Sandbox
dotnet test --environment Development
npm run test:e2e
```

### CI/CD Pipeline
```bash
# Mocked services
dotnet test --environment Test
npm run test:e2e:ci
```

### Staging
```bash
# Real PayPal Sandbox with verification
dotnet test --environment Staging
npm run test:e2e:staging
```

## 8. Configuration Validation

Add startup validation in `Program.cs`:
```csharp
var environment = builder.Environment.EnvironmentName;
var useMocks = builder.Configuration.GetValue<bool>("USE_MOCK_PAYMENT_SERVICE");

if (environment == "Production" && useMocks)
{
    throw new InvalidOperationException("Cannot use mock services in production!");
}

if (environment == "Test" && !useMocks)
{
    throw new InvalidOperationException("CI/CD must use mock services!");
}

logger.LogInformation($"Environment: {environment}, Using Mocks: {useMocks}");
```

## Summary

This configuration ensures:
1. **CI/CD tests are fast and reliable** - No external dependencies
2. **Local development uses real sandbox** - Test actual PayPal integration
3. **Staging validates production scenarios** - Full verification enabled
4. **Production is properly secured** - Live PayPal with all verifications
5. **Tests are environment-aware** - Automatically adapt to their context

The key is using the `USE_MOCK_PAYMENT_SERVICE` flag to switch between real and mocked PayPal services, allowing CI/CD to run without external dependencies while still testing the full flow in development and staging.