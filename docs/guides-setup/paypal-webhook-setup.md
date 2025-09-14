# PayPal Webhook Setup Guide

## Prerequisites
1. PayPal Developer Account (sandbox or production)
2. ngrok installed (run `./scripts/install-ngrok.sh`)
3. API running locally on port 5653

## Step 1: Start Your Local API
```bash
cd apps/api
dotnet run
# API should be running on http://localhost:5653
```

## Step 2: Start ngrok Tunnel
```bash
# If you installed ngrok with our script:
~/bin/ngrok http 5653

# Or if ngrok is in your PATH:
ngrok http 5653
```

You'll see output like:
```
Session Status                online
Account                       your-email@example.com (Plan: Free)
Version                       3.28.0
Region                        United States (us)
Latency                       34ms
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc123def456.ngrok-free.app -> http://localhost:5653
```

**Copy the HTTPS URL** (e.g., `https://abc123def456.ngrok-free.app`)

## Step 3: Configure PayPal Webhooks

### For Sandbox (Development)
1. Go to https://developer.paypal.com/dashboard/applications/sandbox
2. Click on your app (or create one if needed)
3. Scroll down to "Webhooks"
4. Click "Add Webhook"

### Webhook URL Configuration
Enter your webhook URL:
```
https://YOUR-NGROK-SUBDOMAIN.ngrok-free.app/api/webhooks/paypal
```

Example:
```
https://abc123def456.ngrok-free.app/api/webhooks/paypal
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

## Step 4: Get Webhook ID
After creating the webhook:
1. Copy the **Webhook ID** shown in PayPal dashboard
2. Update your `.env` file:
```env
PAYPAL_WEBHOOK_ID=YOUR_WEBHOOK_ID_HERE
```

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
curl -X POST http://localhost:5653/api/payments/process \
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

### Check ngrok Dashboard
Open http://127.0.0.1:4040 in your browser to see:
- All incoming webhook requests
- Request/response details
- Replay capability for debugging

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
- ngrok URLs change each time you restart (free plan)
- Update PayPal webhook URL when ngrok URL changes
- Consider ngrok paid plan for stable URLs

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
1. Check ngrok is running and URL is correct
2. Verify API is running on correct port (5653)
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
4. Review ngrok dashboard for request/response details

## Testing Checklist

- [ ] ngrok tunnel established
- [ ] Webhook URL configured in PayPal
- [ ] Webhook ID saved in `.env`
- [ ] Test event sent from PayPal Simulator
- [ ] Event received in ngrok dashboard
- [ ] Event processed by API successfully
- [ ] Payment record created/updated in database
- [ ] Response returned to PayPal (200 OK)

## Resources
- [PayPal Webhooks Documentation](https://developer.paypal.com/docs/api/webhooks/v1/)
- [ngrok Documentation](https://ngrok.com/docs)
- [PayPal Webhook Events Reference](https://developer.paypal.com/docs/api-basics/notifications/webhooks/event-names/)