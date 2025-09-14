# PayPal Webhook Setup Guide

## Prerequisites
1. PayPal Developer Account (sandbox or production)
2. Cloudflare Tunnel configured (automatic on terminal open)
3. API running locally on port 5655

## Step 1: Start Your Local API
```bash
cd apps/api
dotnet run --urls http://localhost:5655
# API should be running on http://localhost:5655
```

## Step 2: Verify Cloudflare Tunnel (Auto-Started)

The tunnel starts automatically when you open a terminal. To verify:

```bash
# Check if tunnel is running
ps aux | grep cloudflared

# View tunnel logs
tail -f ~/.cloudflare-tunnel.log
```

**Your permanent URL:** `https://dev-api.chadfbennett.com`

This URL never changes and always points to `localhost:5655`.

## Step 3: Configure PayPal Webhooks

### For Sandbox (Development)
1. Go to https://developer.paypal.com/dashboard/applications/sandbox
2. Click on your app (or create one if needed)
3. Scroll down to "Webhooks"
4. Click "Add Webhook"

### Webhook URL Configuration

**For Local Development:**
```
https://dev-api.chadfbennett.com/api/webhooks/paypal
```

**For Staging:**
```
https://staging-api.witchcityrope.com/api/webhooks/paypal
```

**For Production:**
```
https://api.witchcityrope.com/api/webhooks/paypal
```

### Select Events to Listen For
Check the following events (minimum required):

**Payment Events:**
- ✅ `PAYMENT.CAPTURE.COMPLETED` - Payment successfully captured
- ✅ `PAYMENT.CAPTURE.DENIED` - Payment capture denied
- ✅ `PAYMENT.CAPTURE.REFUNDED` - Payment refunded
- ✅ `PAYMENT.CAPTURE.PENDING` - Payment pending

**Checkout Events:**
- ✅ `CHECKOUT.ORDER.APPROVED` - Order approved by buyer
- ✅ `CHECKOUT.ORDER.COMPLETED` - Checkout completed
- ✅ `CHECKOUT.PAYMENT-APPROVAL.REVERSED` - Payment reversed

**Optional Events (for comprehensive tracking):**
- `PAYMENT.AUTHORIZATION.CREATED`
- `PAYMENT.AUTHORIZATION.VOIDED`
- `CUSTOMER.DISPUTE.CREATED`
- `CUSTOMER.DISPUTE.RESOLVED`

## Step 4: Webhook IDs (Already Configured)

### Local Development
- **Webhook ID**: `1PH29187W48812152`
- **PayPal App**: WitchCityRopeSandbox
- **URL**: `https://dev-api.chadfbennett.com/api/webhooks/paypal`
- **File**: `.env.development`

### Staging
- **Webhook ID**: `1GN678263B992340W`
- **PayPal App**: WitchCityRopeSandbox
- **URL**: `https://staging-api.witchcityrope.com/api/webhooks/paypal`
- **File**: `.env.staging`

### Production
- **Webhook ID**: `93J292600X332032T`
- **PayPal App**: WitchCityRopeLive
- **URL**: `https://api.witchcityrope.com/api/webhooks/paypal`
- **File**: `.env.production`

## Step 5: Test the Webhook

### Using PayPal Webhook Simulator
1. In PayPal Developer Dashboard, go to "Webhooks"
2. Click "Webhook Simulator"
3. Select your webhook URL
4. Choose an event type (e.g., `PAYMENT.CAPTURE.COMPLETED`)
5. Click "Send Test"

### Manual Testing with API
```bash
# Create a test payment
curl -X POST http://localhost:5655/api/payments/process \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "eventId": "guid-here",
    "userId": "guid-here",
    "amount": 50.00,
    "slidingScalePercentage": 0,
    "paymentMethodType": "PayPal"
  }'
```

## Step 6: Monitor Webhook Activity

### Check Cloudflare Tunnel Logs
```bash
# View tunnel logs
tail -f ~/.cloudflare-tunnel.log

# Check tunnel status
ps aux | grep cloudflared
```

### Check API Logs
```bash
# View API logs
tail -f apps/api/logs/*.log

# Or if using console output
# Look for log entries like:
# [Information] Processing PayPal webhook event: PAYMENT.CAPTURE.COMPLETED
```

## Webhook Endpoint Details

**Endpoint:** `POST /api/webhooks/paypal`

**Headers PayPal Sends:**
- `PAYPAL-TRANSMISSION-ID` - Unique message ID
- `PAYPAL-TRANSMISSION-TIME` - Timestamp
- `PAYPAL-TRANSMISSION-SIG` - Signature for verification
- `PAYPAL-CERT-URL` - Certificate URL
- `PAYPAL-AUTH-ALGO` - Algorithm used

**Response Codes:**
- `200 OK` - Webhook processed successfully
- `400 Bad Request` - Invalid webhook data or signature
- `500 Internal Server Error` - Processing error

## Important Notes

### For Development
- Cloudflare tunnel provides permanent URL: `https://dev-api.chadfbennett.com`
- Tunnel auto-starts when you open a terminal
- No need to update PayPal webhook URLs

### For Production
- Use your actual domain (e.g., `https://api.witchcityrope.com/api/webhooks/paypal`)
- Ensure SSL certificate is valid
- Set up webhook signature verification
- Monitor webhook failures and implement retry logic

### Security
- Always verify webhook signatures in production
- Never expose webhook endpoints without validation
- Log all webhook events for audit trail
- Implement idempotency to handle duplicate events

## Troubleshooting

### Webhook Not Receiving Events
1. Check Cloudflare tunnel is running: `ps aux | grep cloudflared`
2. Verify API is running on port 5655
3. Check PayPal webhook is Active (not Disabled)
4. Ensure events are selected in PayPal dashboard

### Signature Verification Failing
1. Check `PAYPAL_WEBHOOK_ID` in environment
2. Verify using correct environment (sandbox vs production)
3. Ensure headers are being passed correctly

### Events Not Processing
1. Check API logs for errors
2. Verify database connection is working
3. Check Payment entity configuration
4. Review Cloudflare tunnel logs: `tail -f ~/.cloudflare-tunnel.log`

## Testing Checklist

- [ ] Cloudflare tunnel running (auto-started)
- [ ] Webhook URLs configured in PayPal
- [ ] Webhook IDs in correct `.env` files
- [ ] Test event sent from PayPal Simulator
- [ ] Event received at `https://dev-api.chadfbennett.com`
- [ ] Event processed by API successfully
- [ ] Payment record created/updated in database
- [ ] Response returned to PayPal (200 OK)

## Resources
- [PayPal Webhooks Documentation](https://developer.paypal.com/docs/api/webhooks/v1/)
- [Cloudflare Tunnel Documentation](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/)
- [PayPal Webhook Events Reference](https://developer.paypal.com/docs/api-basics/notifications/webhooks/event-names/)