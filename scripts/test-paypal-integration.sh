#!/bin/bash

# Incremental PayPal Integration Test Script
# Tests each component separately to identify issues

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

API_URL="http://localhost:5655"

echo "🧪 PayPal Integration Test Suite"
echo "================================="
echo ""

# Function to check if API is running
check_api() {
    echo "1️⃣  Checking if API is running..."
    if curl -s -f "$API_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ API is running on $API_URL${NC}"
        return 0
    else
        echo -e "${RED}❌ API is not running on $API_URL${NC}"
        echo "   Please start the API with: cd apps/api && dotnet run --urls $API_URL"
        return 1
    fi
}

# Function to check environment configuration
check_environment() {
    echo ""
    echo "2️⃣  Checking environment configuration..."
    
    # Check if .env.development exists
    if [ -f ".env.development" ]; then
        echo -e "${GREEN}✅ .env.development found${NC}"
        
        # Check for PayPal configuration
        if grep -q "PAYPAL_CLIENT_ID" .env.development; then
            echo -e "${GREEN}✅ PayPal Client ID configured${NC}"
        else
            echo -e "${RED}❌ PayPal Client ID missing${NC}"
        fi
        
        if grep -q "PAYPAL_CLIENT_SECRET" .env.development; then
            echo -e "${GREEN}✅ PayPal Client Secret configured${NC}"
        else
            echo -e "${RED}❌ PayPal Client Secret missing${NC}"
        fi
        
        if grep -q "PAYPAL_WEBHOOK_ID" .env.development; then
            WEBHOOK_ID=$(grep PAYPAL_WEBHOOK_ID .env.development | cut -d'=' -f2)
            echo -e "${GREEN}✅ PayPal Webhook ID configured: $WEBHOOK_ID${NC}"
        else
            echo -e "${YELLOW}⚠️  PayPal Webhook ID not configured${NC}"
        fi
        
        # Check mock service flag
        if grep -q "USE_MOCK_PAYMENT_SERVICE=true" .env.development; then
            echo -e "${YELLOW}⚠️  Mock PayPal service is ENABLED - Using mock responses${NC}"
            USING_MOCK=true
        else
            echo -e "${GREEN}✅ Using real PayPal sandbox${NC}"
            USING_MOCK=false
        fi
    else
        echo -e "${RED}❌ .env.development not found${NC}"
        return 1
    fi
}

# Function to test mock service
test_mock_service() {
    echo ""
    echo "3️⃣  Testing Mock PayPal Service..."
    
    # Create a test order with mock service
    RESPONSE=$(curl -s -X POST "$API_URL/api/payments/create-order" \
        -H "Content-Type: application/json" \
        -d '{
            "amount": 50.00,
            "currency": "USD",
            "customerId": "test-customer-123",
            "slidingScalePercentage": 25,
            "metadata": {
                "eventId": "test-event-123"
            }
        }' 2>/dev/null || echo "ERROR")
    
    if echo "$RESPONSE" | grep -q "MOCK-ORDER"; then
        ORDER_ID=$(echo "$RESPONSE" | grep -o '"orderId":"[^"]*"' | cut -d'"' -f4)
        echo -e "${GREEN}✅ Mock order created: $ORDER_ID${NC}"
        
        # Test capture
        echo "   Testing mock capture..."
        CAPTURE_RESPONSE=$(curl -s -X POST "$API_URL/api/payments/capture/$ORDER_ID" 2>/dev/null || echo "ERROR")
        
        if echo "$CAPTURE_RESPONSE" | grep -q "COMPLETED"; then
            echo -e "${GREEN}✅ Mock capture successful${NC}"
        else
            echo -e "${RED}❌ Mock capture failed${NC}"
            echo "   Response: $CAPTURE_RESPONSE"
        fi
    else
        echo -e "${RED}❌ Failed to create mock order${NC}"
        echo "   Response: $RESPONSE"
    fi
}

# Function to test real PayPal sandbox
test_paypal_sandbox() {
    echo ""
    echo "4️⃣  Testing Real PayPal Sandbox Connection..."
    
    # First, get an access token
    echo "   Getting PayPal access token..."
    
    # Read credentials from .env.development
    CLIENT_ID=$(grep PAYPAL_CLIENT_ID .env.development | cut -d'=' -f2)
    CLIENT_SECRET=$(grep PAYPAL_CLIENT_SECRET .env.development | cut -d'=' -f2)
    
    TOKEN_RESPONSE=$(curl -s -X POST "https://api-m.sandbox.paypal.com/v1/oauth2/token" \
        -u "$CLIENT_ID:$CLIENT_SECRET" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=client_credentials" 2>/dev/null || echo "ERROR")
    
    if echo "$TOKEN_RESPONSE" | grep -q "access_token"; then
        echo -e "${GREEN}✅ Successfully authenticated with PayPal Sandbox${NC}"
        ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
        
        # Test creating an order directly with PayPal
        echo "   Creating test order with PayPal..."
        ORDER_RESPONSE=$(curl -s -X POST "https://api-m.sandbox.paypal.com/v2/checkout/orders" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $ACCESS_TOKEN" \
            -d '{
                "intent": "CAPTURE",
                "purchase_units": [{
                    "amount": {
                        "currency_code": "USD",
                        "value": "10.00"
                    }
                }]
            }' 2>/dev/null || echo "ERROR")
        
        if echo "$ORDER_RESPONSE" | grep -q '"status":"CREATED"'; then
            ORDER_ID=$(echo "$ORDER_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
            echo -e "${GREEN}✅ PayPal order created: $ORDER_ID${NC}"
            
            # Get approval URL
            APPROVE_URL=$(echo "$ORDER_RESPONSE" | grep -o '"href":"[^"]*","rel":"approve"' | cut -d'"' -f4)
            echo -e "${BLUE}   Approval URL: $APPROVE_URL${NC}"
        else
            echo -e "${RED}❌ Failed to create PayPal order${NC}"
            echo "   Response: $ORDER_RESPONSE"
        fi
    else
        echo -e "${RED}❌ Failed to authenticate with PayPal${NC}"
        echo "   Response: $TOKEN_RESPONSE"
        echo ""
        echo "   Please check:"
        echo "   - PAYPAL_CLIENT_ID and PAYPAL_CLIENT_SECRET in .env.development"
        echo "   - Credentials are from a Sandbox app (not Live)"
        echo "   - App is active in PayPal Developer Dashboard"
    fi
}

# Function to test webhook endpoint
test_webhook_endpoint() {
    echo ""
    echo "5️⃣  Testing Webhook Endpoint..."
    
    # Send a mock webhook to our endpoint
    WEBHOOK_RESPONSE=$(curl -s -X POST "$API_URL/api/webhooks/paypal" \
        -H "Content-Type: application/json" \
        -H "PAYPAL-TRANSMISSION-ID: TEST-$(date +%s)" \
        -H "PAYPAL-TRANSMISSION-TIME: $(date -u +%Y-%m-%dT%H:%M:%S.000Z)" \
        -H "PAYPAL-TRANSMISSION-SIG: mock-signature" \
        -H "PAYPAL-CERT-URL: mock-cert-url" \
        -H "PAYPAL-AUTH-ALGO: SHA256withRSA" \
        -d '{
            "id": "WH-TEST-'$(date +%s)'",
            "event_type": "PAYMENT.CAPTURE.COMPLETED",
            "resource": {
                "id": "TEST-CAPTURE-123",
                "status": "COMPLETED",
                "amount": {
                    "currency_code": "USD",
                    "value": "50.00"
                }
            }
        }' -w "\nHTTP_CODE:%{http_code}" 2>/dev/null || echo "ERROR")
    
    HTTP_CODE=$(echo "$WEBHOOK_RESPONSE" | grep "HTTP_CODE:" | cut -d':' -f2)
    
    if [ "$HTTP_CODE" = "200" ]; then
        echo -e "${GREEN}✅ Webhook endpoint is accessible${NC}"
    else
        echo -e "${RED}❌ Webhook endpoint returned: $HTTP_CODE${NC}"
        echo "   Response: $WEBHOOK_RESPONSE"
    fi
}

# Function to test Cloudflare tunnel
test_cloudflare_tunnel() {
    echo ""
    echo "6️⃣  Testing Cloudflare Tunnel (if configured)..."
    
    TUNNEL_URL="https://dev-api.chadfbennett.com"
    
    if curl -s -f "$TUNNEL_URL/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Cloudflare tunnel is working: $TUNNEL_URL${NC}"
        
        # Test webhook through tunnel
        echo "   Testing webhook through tunnel..."
        TUNNEL_WEBHOOK_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "$TUNNEL_URL/api/webhooks/paypal" \
            -H "Content-Type: application/json" \
            -d '{"test": "data"}' 2>/dev/null || echo "ERROR")
        
        if [ "$TUNNEL_WEBHOOK_RESPONSE" = "200" ] || [ "$TUNNEL_WEBHOOK_RESPONSE" = "400" ]; then
            echo -e "${GREEN}✅ Webhook endpoint accessible through tunnel${NC}"
        else
            echo -e "${YELLOW}⚠️  Webhook through tunnel returned: $TUNNEL_WEBHOOK_RESPONSE${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  Cloudflare tunnel not accessible${NC}"
        echo "   This is OK for local testing"
    fi
}

# Function to run integration test
run_integration_test() {
    echo ""
    echo "7️⃣  Running Full Integration Test..."
    
    if [ "$USING_MOCK" = true ]; then
        echo -e "${BLUE}   Using mock service for integration test${NC}"
    else
        echo -e "${BLUE}   Using real PayPal sandbox for integration test${NC}"
    fi
    
    # Create order through our API
    CREATE_RESPONSE=$(curl -s -X POST "$API_URL/api/payments/process" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer test-token" \
        -d '{
            "eventId": "'$(uuidgen)'",
            "userId": "'$(uuidgen)'",
            "amount": 25.00,
            "slidingScalePercentage": 0,
            "paymentMethodType": "PayPal"
        }' 2>/dev/null || echo "ERROR")
    
    if echo "$CREATE_RESPONSE" | grep -q "orderId"; then
        ORDER_ID=$(echo "$CREATE_RESPONSE" | grep -o '"orderId":"[^"]*"' | cut -d'"' -f4)
        echo -e "${GREEN}✅ Integration test order created: $ORDER_ID${NC}"
        
        if [ "$USING_MOCK" = false ]; then
            APPROVE_URL=$(echo "$CREATE_RESPONSE" | grep -o '"approveUrl":"[^"]*"' | cut -d'"' -f4)
            if [ -n "$APPROVE_URL" ]; then
                echo -e "${BLUE}   Approval URL: $APPROVE_URL${NC}"
                echo "   (User would complete payment at this URL)"
            fi
        fi
    else
        echo -e "${RED}❌ Integration test failed${NC}"
        echo "   Response: $CREATE_RESPONSE"
    fi
}

# Main execution
main() {
    # Check API
    if ! check_api; then
        exit 1
    fi
    
    # Check environment
    check_environment
    
    # Run appropriate tests based on configuration
    if [ "$USING_MOCK" = true ]; then
        test_mock_service
    else
        test_paypal_sandbox
    fi
    
    # Test webhook endpoint
    test_webhook_endpoint
    
    # Test Cloudflare tunnel
    test_cloudflare_tunnel
    
    # Run integration test
    run_integration_test
    
    echo ""
    echo "================================="
    echo "🏁 Test Suite Complete!"
    echo ""
    
    if [ "$USING_MOCK" = true ]; then
        echo "📝 Note: You're using mock PayPal service"
        echo "   To test real PayPal, set USE_MOCK_PAYMENT_SERVICE=false in .env.development"
    else
        echo "📝 Note: You're using real PayPal sandbox"
        echo "   To use mocks (for CI/CD), set USE_MOCK_PAYMENT_SERVICE=true"
    fi
    
    echo ""
    echo "🔍 Next Steps:"
    echo "1. Check API logs for detailed information"
    echo "2. Use PayPal Webhook Simulator for end-to-end testing"
    echo "3. Monitor Cloudflare tunnel logs if using webhooks"
}

# Run main function
main