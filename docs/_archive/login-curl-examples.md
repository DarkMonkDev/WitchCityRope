# WitchCityRope Login API - Curl Examples

## Basic Login Request

```bash
# Simple login request
curl -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# With pretty-printed JSON response
curl -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' | jq .

# Save response to file
curl -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' \
  -o login-response.json
```

## With Cookie Management

```bash
# Login and save cookies
curl -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -c cookies.txt \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Use saved cookies for subsequent requests
curl 'http://localhost:8180/api/user/profile' \
  -b cookies.txt
```

## With Token Extraction

```bash
# Login and extract access token
ACCESS_TOKEN=$(curl -s -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}' | jq -r '.accessToken')

# Use the token for authenticated requests
curl 'http://localhost:8180/api/user/profile' \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

## Full Login Flow with Error Handling

```bash
# Login with full error handling
RESPONSE=$(curl -s -w '\n%{http_code}' -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}')

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

if [ "$HTTP_CODE" = "200" ]; then
    echo "Login successful!"
    echo "$BODY" | jq .
else
    echo "Login failed with status: $HTTP_CODE"
    echo "$BODY"
fi
```

## HTTPS with Self-Signed Certificate

```bash
# If using HTTPS with self-signed certificate
curl -k -X POST 'https://localhost:8181/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'
```

## Debugging Options

```bash
# Verbose output to see request/response headers
curl -v -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'

# Include response headers in output
curl -i -X POST 'http://localhost:8180/api/identity/account/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@witchcityrope.com","password":"Test123!"}'
```

## Notes

- The API endpoint is `/api/identity/account/login`
- The request must be POST with JSON content type
- Expected JSON payload: `{"email": "string", "password": "string"}`
- Successful response (200 OK) includes:
  - `accessToken`: JWT access token for API authentication
  - `refreshToken`: Token for refreshing expired access tokens
  - `expiresIn`: Token expiration time in seconds
- The application uses JWT Bearer token authentication
- No CSRF token is required for the API endpoints