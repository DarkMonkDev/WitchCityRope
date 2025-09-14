#!/bin/bash

# Test PayPal webhook through Cloudflare Tunnel
# This script verifies the entire payment flow is working

echo "🧪 WitchCityRope PayPal Integration Test"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if API is running
echo "1️⃣  Checking if API is running on port 5655..."
if lsof -Pi :5655 -sTCP:LISTEN -t >/dev/null 2>&1; then
    echo -e "${GREEN}✅ API is running on port 5655${NC}"
else
    echo -e "${YELLOW}⚠️  API not detected on port 5655${NC}"
    echo "   Starting API..."
    cd /home/chad/repos/witchcityrope-react/apps/api
    dotnet run --urls http://localhost:5655 &
    sleep 5
fi

# Check if tunnel is running
echo ""
echo "2️⃣  Checking if Cloudflare tunnel is running..."
if pgrep -x "cloudflared" > /dev/null; then
    echo -e "${GREEN}✅ Cloudflare tunnel is running${NC}"
else
    echo -e "${YELLOW}⚠️  Tunnel not running. Starting...${NC}"
    cloudflared tunnel run witchcityrope-dev &
    sleep 3
fi

# Test tunnel connectivity
echo ""
echo "3️⃣  Testing tunnel URL..."
TUNNEL_URL="https://dev-api.chadfbennett.com"
if curl -s -o /dev/null -w "%{http_code}" "$TUNNEL_URL/api/health" | grep -q "200"; then
    echo -e "${GREEN}✅ Tunnel is working: $TUNNEL_URL${NC}"
else
    echo -e "${RED}❌ Tunnel not responding at $TUNNEL_URL${NC}"
    echo "   Please check cloudflared logs: tail -f ~/.cloudflare-tunnel.log"
    exit 1
fi

# Test webhook endpoint
echo ""
echo "4️⃣  Testing webhook endpoint accessibility..."
WEBHOOK_URL="$TUNNEL_URL/api/webhooks/paypal"
RESPONSE=$(curl -s -X POST "$WEBHOOK_URL" \
    -H "Content-Type: application/json" \
    -d '{"test": "data"}' \
    -w "\n%{http_code}")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

if [ "$HTTP_CODE" = "400" ] || [ "$HTTP_CODE" = "401" ]; then
    echo -e "${GREEN}✅ Webhook endpoint is accessible (returned $HTTP_CODE as expected)${NC}"
else
    echo -e "${RED}❌ Unexpected response code: $HTTP_CODE${NC}"
    echo "   Response: $RESPONSE"
fi

# Display PayPal configuration
echo ""
echo "5️⃣  PayPal Configuration:"
echo "   ========================"
if [ -f /home/chad/repos/witchcityrope-react/.env.development ]; then
    echo -e "${GREEN}   Client ID:${NC} $(grep PAYPAL_CLIENT_ID /home/chad/repos/witchcityrope-react/.env.development | cut -d'=' -f2 | head -c20)..."
    WEBHOOK_ID=$(grep PAYPAL_WEBHOOK_ID /home/chad/repos/witchcityrope-react/.env.development | cut -d'=' -f2)
    if [ -n "$WEBHOOK_ID" ] && [ "$WEBHOOK_ID" != "YOUR_SANDBOX_WEBHOOK_ID" ]; then
        echo -e "${GREEN}   Webhook ID:${NC} $WEBHOOK_ID"
    else
        echo -e "${YELLOW}   Webhook ID: NOT SET - Please update .env.development${NC}"
    fi
else
    echo -e "${RED}   .env.development not found${NC}"
fi

# Summary
echo ""
echo "📊 Summary:"
echo "==========="
echo "🌐 Public URL: $TUNNEL_URL"
echo "🎯 Webhook URL for PayPal: $WEBHOOK_URL"
echo "🔗 Local API: http://localhost:5655"
echo ""
echo "📝 Next Steps:"
echo "1. Update .env.development with your new webhook ID (if not done)"
echo "2. Go to PayPal Sandbox Dashboard"
echo "3. Use Webhook Simulator to send a test event"
echo "4. Monitor API logs: tail -f apps/api/logs/*.log"
echo ""
echo "🧪 To simulate a payment locally:"
echo "curl -X POST http://localhost:5655/api/test/simulate-payment \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{\"orderId\": \"TEST-123\", \"amount\": 50.00}'"
echo ""
echo -e "${GREEN}✅ Test complete!${NC}"