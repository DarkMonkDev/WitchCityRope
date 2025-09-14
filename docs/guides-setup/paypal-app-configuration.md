# PayPal App Configuration

## PayPal Apps Overview

WitchCityRope uses TWO separate PayPal app definitions:

### 1. **WitchCityRopeSandbox** (Testing Only)
- **Purpose**: All testing environments (Local Dev, CI/CD, Staging)
- **Mode**: Always Sandbox - NO real money
- **Dashboard**: https://developer.paypal.com/dashboard/applications/sandbox
- **Client ID**: `AaTUvkwNVutLN6ujfPHX7wk1lh0vndE3wAxZwM5-pTgS38-AJNheP2bYH_DmEr22wy5lVubJEL3dEXZI`
- **Secret**: `EB2JmD74p6d9PEQ6EOAKzPeclO9tWnaVspuniZMSZmU78TD6tYdJS4yHKH-2Tos0ur7KD2nbuSNuLtLx`

### 2. **WitchCityRopeLive** (Production Only)
- **Purpose**: Production environment ONLY
- **Mode**: Live - REAL money transactions
- **Dashboard**: https://developer.paypal.com/dashboard/applications/live
- **Client ID**: `AUDFPb1c8YzskQ9gpMaFJN2MWvtiErUaBXPMFMadPE8Hn78PJziXrQt70C-bn0X5PUF_g_GfhArsivuU`
- **Secret**: `[NEVER COMMIT - Store in secure secrets manager]`

## Environment Configuration

### Local Development (.env.development)
```env
# ALWAYS use Sandbox for development
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=AaTUvkwNVutLN6ujfPHX7wk1lh0vndE3wAxZwM5-pTgS38-AJNheP2bYH_DmEr22wy5lVubJEL3dEXZI
PAYPAL_CLIENT_SECRET=EB2JmD74p6d9PEQ6EOAKzPeclO9tWnaVspuniZMSZmU78TD6tYdJS4yHKH-2Tos0ur7KD2nbuSNuLtLx
PAYPAL_WEBHOOK_ID=[Get from PayPal Sandbox Dashboard after webhook setup]
SKIP_WEBHOOK_VERIFICATION=true
```

### CI/CD Testing (.env.test)
```env
# Use Sandbox for automated tests
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=AaTUvkwNVutLN6ujfPHX7wk1lh0vndE3wAxZwM5-pTgS38-AJNheP2bYH_DmEr22wy5lVubJEL3dEXZI
PAYPAL_CLIENT_SECRET=EB2JmD74p6d9PEQ6EOAKzPeclO9tWnaVspuniZMSZmU78TD6tYdJS4yHKH-2Tos0ur7KD2nbuSNuLtLx
USE_MOCK_PAYMENT_SERVICE=true  # Override with mocks for speed
```

### Staging Environment (.env.staging)
```env
# Still use Sandbox for staging tests
PAYPAL_MODE=sandbox
PAYPAL_CLIENT_ID=AaTUvkwNVutLN6ujfPHX7wk1lh0vndE3wAxZwM5-pTgS38-AJNheP2bYH_DmEr22wy5lVubJEL3dEXZI
PAYPAL_CLIENT_SECRET=EB2JmD74p6d9PEQ6EOAKzPeclO9tWnaVspuniZMSZmU78TD6tYdJS4yHKH-2Tos0ur7KD2nbuSNuLtLx
PAYPAL_WEBHOOK_ID=[Get from PayPal Sandbox Dashboard]
WEBHOOK_BASE_URL=https://staging.witchcityrope.com
SKIP_WEBHOOK_VERIFICATION=false  # Enable verification in staging
```

### Production Environment (.env.production)
```env
# ONLY production uses Live app
PAYPAL_MODE=live
PAYPAL_CLIENT_ID=AUDFPb1c8YzskQ9gpMaFJN2MWvtiErUaBXPMFMadPE8Hn78PJziXrQt70C-bn0X5PUF_g_GfhArsivuU
PAYPAL_CLIENT_SECRET=${PAYPAL_LIVE_SECRET}  # From secure secrets manager
PAYPAL_WEBHOOK_ID=[Get from PayPal Live Dashboard]
WEBHOOK_BASE_URL=https://api.witchcityrope.com
SKIP_WEBHOOK_VERIFICATION=false  # MUST verify in production
```

## Webhook Configuration

### For WitchCityRopeSandbox App

Register these webhook URLs in the Sandbox app:
1. `https://[your-ngrok-url].ngrok.io/api/webhooks/paypal` (Local dev - dynamic)
2. `https://staging.witchcityrope.com/api/webhooks/paypal` (Staging - fixed)

### For WitchCityRopeLive App

Register ONLY this webhook URL in the Live app:
1. `https://api.witchcityrope.com/api/webhooks/paypal` (Production - fixed)

## Security Rules

### NEVER:
- ❌ Use WitchCityRopeLive credentials in development
- ❌ Use WitchCityRopeSandbox in production
- ❌ Commit the Live app secret to git
- ❌ Skip webhook verification in production
- ❌ Process real payments in development/staging

### ALWAYS:
- ✅ Use WitchCityRopeSandbox for ALL testing
- ✅ Store Live secret in secure secrets manager (Azure Key Vault, AWS Secrets Manager, etc.)
- ✅ Verify webhook signatures in production
- ✅ Use environment variables for configuration
- ✅ Test payment flows in sandbox before deploying to production

## Testing Accounts

### Sandbox Test Accounts (auto-created by PayPal)
- **Buyer**: sb-buyer@witchcityrope.com
- **Seller**: sb-seller@witchcityrope.com
- Login at: https://www.sandbox.paypal.com/

### Production Accounts
- Real PayPal accounts with real money
- Your actual business PayPal account receives payments

## Quick Reference

| Environment | PayPal App | Mode | Real Money | Webhook Verification |
|------------|------------|------|------------|---------------------|
| Local Dev | WitchCityRopeSandbox | sandbox | No | Skip |
| CI/CD | WitchCityRopeSandbox | sandbox | No | Skip |
| Staging | WitchCityRopeSandbox | sandbox | No | Enable |
| Production | WitchCityRopeLive | live | Yes | Required |

## Switching Between Environments

The API automatically selects the correct PayPal app based on the `ASPNETCORE_ENVIRONMENT`:

```csharp
public class PayPalService
{
    public PayPalService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var mode = configuration["PAYPAL_MODE"]; // "sandbox" or "live"
        var clientId = configuration["PAYPAL_CLIENT_ID"];
        var clientSecret = configuration["PAYPAL_CLIENT_SECRET"];
        
        // Validate we're not using live credentials in non-production
        if (mode == "live" && !environment.IsProduction())
        {
            throw new InvalidOperationException("Cannot use live PayPal in non-production!");
        }
    }
}
```

## Testing Checklist

### Before Going Live
- [ ] Test all payment flows in WitchCityRopeSandbox
- [ ] Verify webhook processing in staging
- [ ] Confirm refund flows work correctly
- [ ] Test edge cases (failed payments, disputes)
- [ ] Verify sliding scale pricing calculations
- [ ] Switch to WitchCityRopeLive credentials
- [ ] Update webhook URL in Live app dashboard
- [ ] Enable webhook signature verification
- [ ] Test with small real transaction
- [ ] Monitor first 24 hours of transactions