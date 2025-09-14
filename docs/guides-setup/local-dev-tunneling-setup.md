# Local Development Tunneling Setup

## Quick Answer: Tunneling Options

You have **THREE options** for local webhook testing:

### Option 1: ngrok (Easiest, URL changes each time)
### Option 2: Cloudflare Tunnel (Free, permanent URL)
### Option 3: No tunnel - Mock webhooks locally

---

## Option 1: ngrok Setup (Recommended for Quick Testing)

### Install ngrok (One Time)
```bash
# Run our install script
cd /home/chad/repos/witchcityrope-react
./scripts/install-ngrok.sh

# Restart terminal or run:
source ~/.bashrc
```

### Start ngrok Each Session
```bash
# Start API first
cd apps/api
dotnet run --urls http://localhost:5655

# In another terminal, start ngrok
~/bin/ngrok http 5655
```

You'll see:
```
Forwarding: https://abc123def456.ngrok-free.app -> http://localhost:5655
```

### Configure PayPal (Each Time URL Changes)
1. Copy the HTTPS URL (e.g., `https://abc123def456.ngrok-free.app`)
2. Go to PayPal Sandbox Dashboard
3. Update webhook URL to: `https://abc123def456.ngrok-free.app/api/webhooks/paypal`

**Downside**: URL changes every time you restart ngrok

---

## Option 2: Cloudflare Tunnel (Permanent FREE URL)

### Install Cloudflare (One Time)
```bash
# Download and install
wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb

# Login to your Cloudflare account
cloudflared tunnel login
```

### Create Tunnel (One Time)
```bash
# Create named tunnel
cloudflared tunnel create witchcityrope-dev

# Get your tunnel ID (shown after creation)
# It looks like: 6ff42ae2-765d-4adf-8112-31c55c1551ef
```

### Configure Tunnel (One Time)
Create `~/.cloudflared/config.yml`:
```yaml
url: http://localhost:5655
tunnel: YOUR-TUNNEL-ID
credentials-file: /home/chad/.cloudflared/YOUR-TUNNEL-ID.json

ingress:
  - hostname: dev-api.witchcityrope.com
    service: http://localhost:5655
  - service: http_status:404
```

### Add DNS Record (One Time)
```bash
# Point your subdomain to the tunnel
cloudflared tunnel route dns witchcityrope-dev dev-api.witchcityrope.com
```

### Run Tunnel Each Session
```bash
# Start API
cd apps/api
dotnet run --urls http://localhost:5655

# In another terminal, start tunnel
cloudflared tunnel run witchcityrope-dev
```

### Configure PayPal (One Time Only!)
1. Your URL is always: `https://dev-api.witchcityrope.com`
2. In PayPal Sandbox, set webhook to: `https://dev-api.witchcityrope.com/api/webhooks/paypal`
3. Never needs updating!

**Benefit**: Permanent URL, never changes

---

## Option 3: Mock Webhooks (No Tunnel Needed)

### Create Test Endpoint
Add to your API for local testing:

```csharp
// apps/api/Features/Testing/TestWebhookController.cs
[ApiController]
[Route("api/test")]
public class TestWebhookController : ControllerBase
{
    [HttpPost("simulate-payment-complete")]
    public async Task<IActionResult> SimulatePaymentComplete(
        [FromBody] SimulatePaymentRequest request)
    {
        // Simulate PayPal webhook
        var webhookEvent = new
        {
            event_type = "PAYMENT.CAPTURE.COMPLETED",
            resource = new
            {
                id = request.OrderId,
                status = "COMPLETED",
                amount = new { value = request.Amount }
            }
        };
        
        // Process as if it came from PayPal
        await _paymentService.ProcessWebhookEvent(webhookEvent);
        return Ok();
    }
}
```

### Test with curl
```bash
curl -X POST http://localhost:5655/api/test/simulate-payment-complete \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEST-123",
    "amount": "50.00"
  }'
```

**Benefit**: No external dependencies, instant testing

---

## Which Option to Choose?

### Use ngrok when:
- Quick testing needed
- Don't mind updating PayPal URL each session
- Testing actual PayPal integration

### Use Cloudflare Tunnel when:
- Want permanent URL
- Own a domain
- Testing frequently with webhooks

### Use Mock Webhooks when:
- Testing payment flow logic
- Don't need actual PayPal integration
- Want fastest development cycle

---

## Your Current Setup

Based on your webhook IDs:
- **Staging Webhook ID**: `1GN678263B992340W` ✅
- **Production Webhook ID**: `93J292600X332032T` ✅

For local development, you need to:
1. Choose one of the three options above
2. If using ngrok or Cloudflare, register that URL in PayPal Sandbox
3. The same Sandbox app (`1GN678263B992340W`) is used for both local and staging

---

## Auto-Start Script

We created `/scripts/start-dev-tunnel.sh` that:
1. Auto-detects API port
2. Starts ngrok if installed
3. Shows the URL to use

Run it with:
```bash
./scripts/start-dev-tunnel.sh
```

---

## Environment Variables

Your `.env.development` is already configured:
```env
PAYPAL_WEBHOOK_ID=1GN678263B992340W  # ✅ Set
WEBHOOK_BASE_URL=https://YOUR-NGROK-URL.ngrok-free.app  # Update with your tunnel URL
```

Just update `WEBHOOK_BASE_URL` with whichever tunnel URL you're using.

---

## Testing Webhooks

Once your tunnel is running:

1. **Check tunnel works**:
```bash
curl https://your-tunnel-url.ngrok-free.app/api/health
```

2. **Test from PayPal**:
- Go to PayPal Dashboard → Webhooks → Webhook Simulator
- Select your webhook
- Choose event: `PAYMENT.CAPTURE.COMPLETED`
- Send test

3. **Monitor logs**:
```bash
# Watch API logs
tail -f apps/api/logs/*.log

# Or check ngrok dashboard
open http://localhost:4040
```