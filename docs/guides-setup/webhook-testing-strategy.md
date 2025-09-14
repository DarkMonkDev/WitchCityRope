# Webhook Testing Strategy

## The Port Problem - SOLVED

We standardize on **port 5655** for the API going forward:
- Update all scripts to use 5655
- Configure Docker to use 5655
- Set API_PORT=5655 in .env.development

## Stable Webhook URLs - Three Solutions

### Option 1: ngrok Paid Plan ($10/month)
- Get a static subdomain like `witchcityrope.ngrok.io`
- Never changes, even across restarts
- Best for solo development

### Option 2: Cloudflare Tunnel (FREE)
- Permanent URL like `api-dev.witchcityrope.com`
- Requires domain you own
- Free and reliable

### Option 3: Mock Webhooks in Development
- Use webhook mocking for local dev
- Only use real webhooks in staging/production

## CI/CD Testing Strategy

For GitHub Actions E2E tests, you have several options:

### 1. Mock PayPal Service (RECOMMENDED)
```csharp
// In test configuration
services.AddSingleton<IPayPalService, MockPayPalService>();

public class MockPayPalService : IPayPalService
{
    public Task<Result<PayPalOrderResponse>> CreateOrderAsync(...)
    {
        // Return predictable test data
        return Result<PayPalOrderResponse>.Success(new PayPalOrderResponse
        {
            OrderId = "TEST-ORDER-123",
            Status = "CREATED"
        });
    }
    
    public Task<Result<WebhookVerificationResult>> VerifyWebhookAsync(...)
    {
        // Always return success in tests
        return Result<WebhookVerificationResult>.Success(...);
    }
}
```

### 2. PayPal Sandbox in CI
```yaml
# .github/workflows/e2e-tests.yml
env:
  PAYPAL_MODE: sandbox
  PAYPAL_CLIENT_ID: ${{ secrets.PAYPAL_SANDBOX_CLIENT_ID }}
  PAYPAL_CLIENT_SECRET: ${{ secrets.PAYPAL_SANDBOX_SECRET }}
  SKIP_WEBHOOK_VERIFICATION: true  # Skip signature verification in CI
```

### 3. Webhook Recording/Replay
- Record real webhook payloads during development
- Replay them in tests for consistency
- Store in `/tests/fixtures/paypal-webhooks/`

## Environment Configuration

### Development (.env.development)
```env
# Use sandbox credentials
PAYPAL_MODE=sandbox
SKIP_WEBHOOK_VERIFICATION=true  # Simplify local testing
WEBHOOK_BASE_URL=https://your-tunnel.ngrok-free.app
```

### CI/CD (.env.test)
```env
# Use mocked services
USE_MOCK_PAYMENT_SERVICE=true
SKIP_WEBHOOK_VERIFICATION=true
```

### Staging (.env.staging)
```env
# Use sandbox with real webhooks
PAYPAL_MODE=sandbox
SKIP_WEBHOOK_VERIFICATION=false
WEBHOOK_BASE_URL=https://staging.witchcityrope.com
```

### Production (.env.production)
```env
# Real PayPal with verification
PAYPAL_MODE=production
SKIP_WEBHOOK_VERIFICATION=false
WEBHOOK_BASE_URL=https://api.witchcityrope.com
```

## Auto-Start Script for Development

Add to your `.bashrc` or `.zshrc`:
```bash
# Auto-start WitchCityRope dev environment
alias wcr-dev="cd ~/repos/witchcityrope-react && ./scripts/start-dev-tunnel.sh"
```

Or create a systemd service (Linux):
```ini
# /etc/systemd/user/witchcityrope-tunnel.service
[Unit]
Description=WitchCityRope Development Tunnel
After=network.target

[Service]
Type=simple
ExecStart=/home/chad/repos/witchcityrope-react/scripts/start-dev-tunnel.sh
Restart=always
RestartSec=10

[Install]
WantedBy=default.target
```

Enable with:
```bash
systemctl --user enable witchcityrope-tunnel
systemctl --user start witchcityrope-tunnel
```

## Setting Up Cloudflare Tunnel (FREE Alternative)

1. Install cloudflared:
```bash
wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb
```

2. Login to Cloudflare:
```bash
cloudflared tunnel login
```

3. Create tunnel:
```bash
cloudflared tunnel create witchcityrope-dev
```

4. Configure tunnel:
```yaml
# ~/.cloudflared/config.yml
url: http://localhost:5655
tunnel: YOUR-TUNNEL-ID
credentials-file: /home/chad/.cloudflared/YOUR-TUNNEL-ID.json
```

5. Create DNS record:
```bash
cloudflared tunnel route dns witchcityrope-dev api-dev.witchcityrope.com
```

6. Run tunnel:
```bash
cloudflared tunnel run witchcityrope-dev
```

Now you have a permanent URL: `https://api-dev.witchcityrope.com`

## Testing PayPal in E2E Tests

### Playwright Test Example
```typescript
// tests/e2e/payment.spec.ts
import { test, expect } from '@playwright/test';

test.describe('PayPal Integration', () => {
  test.beforeEach(async ({ page }) => {
    // Set up mock if in CI
    if (process.env.CI) {
      await page.route('**/api/payments/**', route => {
        route.fulfill({
          status: 200,
          body: JSON.stringify({
            orderId: 'TEST-ORDER-123',
            approveUrl: '/mock-paypal-approve'
          })
        });
      });
    }
  });

  test('should process PayPal payment', async ({ page }) => {
    // Your test here
  });
});
```

### Mock PayPal Approval Page
```typescript
// For CI testing, create a mock approval flow
app.get('/mock-paypal-approve', (req, res) => {
  // Simulate PayPal approval
  res.redirect(`/api/payments/capture?token=TEST-TOKEN&PayerID=TEST-PAYER`);
});
```

## Summary of Recommendations

1. **Standardize on port 5655** - Update all configs to use this port
2. **Use Cloudflare Tunnel for free stable URLs** - Better than ngrok free plan
3. **Mock PayPal in CI/CD tests** - Faster and more reliable
4. **Use real sandbox only in staging** - Where you need end-to-end validation
5. **Separate environment configs** - Different settings for dev/test/staging/prod

This approach means:
- You never have to update PayPal webhook URLs
- CI/CD tests run without external dependencies
- Development is simplified with consistent ports
- Production uses real verification