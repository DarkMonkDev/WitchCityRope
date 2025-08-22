# Staging Environment Variables

## Overview

This document lists all environment variables required for the staging environment. These variables should be configured in your deployment platform (Docker Compose, Kubernetes, Azure App Service, etc.) or stored securely in a secrets management system.

## Required Environment Variables

### Core Configuration

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Staging` |
| `ASPNETCORE_URLS` | URLs the application listens on | `http://+:8080;https://+:8081` |

### Security & Authentication

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `JWT_SECRET` | JWT signing key (min 32 chars) | `staging-jwt-secret-key-minimum-32-characters-long` |
| `ENCRYPTION_KEY` | Data encryption key (32 chars) | `staging-encryption-key-32-chars` |
| `CORS_ALLOWED_ORIGINS` | Allowed CORS origins | `https://staging.witchcityrope.com` |

### Database

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `CONNECTION_STRING` | SQLite database path | `Data Source=/app/data/witchcityrope_staging.db` |
| `REDIS_CONNECTION_STRING` | Redis connection string | `redis:6379,abortConnect=false` |

### Email Service (SendGrid)

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `SENDGRID_API_KEY` | SendGrid API key | `SG.xxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_WELCOME` | Welcome email template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_EMAIL_VERIFICATION` | Email verification template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_PASSWORD_RESET` | Password reset template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_EVENT_REGISTRATION` | Event registration template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_EVENT_REMINDER` | Event reminder template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_VETTING_APPROVED` | Vetting approved template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |
| `SENDGRID_TEMPLATE_VETTING_REJECTED` | Vetting rejected template ID | `d-xxxxxxxxxxxxxxxxxxxxxx` |

### Payment Processing

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `PAYPAL_CLIENT_ID` | PayPal client ID | `AXxxxxxxxxxxxxxxxxxxxx` |
| `PAYPAL_CLIENT_SECRET` | PayPal client secret | `EKxxxxxxxxxxxxxxxxxxxx` |
| `PAYPAL_WEBHOOK_ID` | PayPal webhook ID | `WH-xxxxxxxxxxxxxxxxxxxx` |
| `STRIPE_PUBLISHABLE_KEY` | Stripe publishable key | `pk_test_xxxxxxxxxxxxxxxxxxxx` |
| `STRIPE_SECRET_KEY` | Stripe secret key | `sk_test_xxxxxxxxxxxxxxxxxxxx` |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook secret | `whsec_xxxxxxxxxxxxxxxxxxxx` |

### Third-Party Services

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `AZURE_STORAGE_CONNECTION_STRING` | Azure Storage connection | `DefaultEndpointsProtocol=https;AccountName=staging;...` |

### Monitoring & Logging

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `SEQ_API_KEY` | Seq logging API key | `xxxxxxxxxxxxxxxxxxxx` |
| `APPLICATION_INSIGHTS_CONNECTION_STRING` | Application Insights connection | `InstrumentationKey=xxxx;IngestionEndpoint=...` |

### SSL/TLS Configuration

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `ASPNETCORE_HTTPS_PORT` | HTTPS port | `443` |
| `ASPNETCORE_Kestrel__Certificates__Default__Path` | SSL certificate path | `/app/certs/staging.pfx` |
| `ASPNETCORE_Kestrel__Certificates__Default__Password` | SSL certificate password | `certificate-password` |

## Docker Compose Environment File

Create a `.env.staging` file with the following content:

```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Staging
BUILD_TARGET=final

# Security
JWT_SECRET=your-staging-jwt-secret-minimum-32-characters
ENCRYPTION_KEY=your-staging-encryption-key-32ch

# Database
CONNECTION_STRING=Data Source=/app/data/witchcityrope_staging.db
REDIS_CONNECTION_STRING=redis:6379,abortConnect=false

# SendGrid
SENDGRID_API_KEY=SG.your-sendgrid-api-key
SENDGRID_TEMPLATE_WELCOME=d-your-welcome-template-id
SENDGRID_TEMPLATE_EMAIL_VERIFICATION=d-your-verification-template-id
SENDGRID_TEMPLATE_PASSWORD_RESET=d-your-password-reset-template-id
SENDGRID_TEMPLATE_EVENT_REGISTRATION=d-your-event-registration-template-id
SENDGRID_TEMPLATE_EVENT_REMINDER=d-your-event-reminder-template-id
SENDGRID_TEMPLATE_VETTING_APPROVED=d-your-vetting-approved-template-id
SENDGRID_TEMPLATE_VETTING_REJECTED=d-your-vetting-rejected-template-id

# PayPal
PAYPAL_CLIENT_ID=your-paypal-client-id
PAYPAL_CLIENT_SECRET=your-paypal-client-secret
PAYPAL_WEBHOOK_ID=your-paypal-webhook-id

# Stripe
STRIPE_PUBLISHABLE_KEY=pk_test_your-stripe-publishable-key
STRIPE_SECRET_KEY=sk_test_your-stripe-secret-key
STRIPE_WEBHOOK_SECRET=whsec_your-stripe-webhook-secret

AZURE_STORAGE_CONNECTION_STRING=your-azure-storage-connection-string

# Monitoring
SEQ_API_KEY=your-seq-api-key
APPLICATION_INSIGHTS_CONNECTION_STRING=your-app-insights-connection-string
```

## Kubernetes Secrets

For Kubernetes deployments, create secrets:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: witchcityrope-staging-secrets
  namespace: staging
type: Opaque
stringData:
  JWT_SECRET: "your-staging-jwt-secret-minimum-32-characters"
  ENCRYPTION_KEY: "your-staging-encryption-key-32ch"
  SENDGRID_API_KEY: "SG.your-sendgrid-api-key"
  PAYPAL_CLIENT_SECRET: "your-paypal-client-secret"
  STRIPE_SECRET_KEY: "sk_test_your-stripe-secret-key"
  STRIPE_WEBHOOK_SECRET: "whsec_your-stripe-webhook-secret"
```

## Azure Key Vault Configuration

For Azure deployments, store secrets in Key Vault:

```bash
# Create Key Vault
az keyvault create --name wcr-staging-kv --resource-group wcr-staging-rg --location eastus

# Add secrets
az keyvault secret set --vault-name wcr-staging-kv --name "JwtSecret" --value "your-staging-jwt-secret"
az keyvault secret set --vault-name wcr-staging-kv --name "EncryptionKey" --value "your-encryption-key"
az keyvault secret set --vault-name wcr-staging-kv --name "SendGridApiKey" --value "SG.your-api-key"
az keyvault secret set --vault-name wcr-staging-kv --name "PayPalClientSecret" --value "your-paypal-secret"
az keyvault secret set --vault-name wcr-staging-kv --name "StripeSecretKey" --value "sk_test_your-key"
```

## Environment Variable Validation

Add this script to validate all required environment variables are set:

```bash
#!/bin/bash
# validate-staging-env.sh

REQUIRED_VARS=(
    "ASPNETCORE_ENVIRONMENT"
    "JWT_SECRET"
    "ENCRYPTION_KEY"
    "SENDGRID_API_KEY"
    "PAYPAL_CLIENT_ID"
    "PAYPAL_CLIENT_SECRET"
)

MISSING_VARS=()

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ]; then
        MISSING_VARS+=($var)
    fi
done

if [ ${#MISSING_VARS[@]} -ne 0 ]; then
    echo "Error: Missing required environment variables:"
    printf '%s\n' "${MISSING_VARS[@]}"
    exit 1
else
    echo "All required environment variables are set!"
fi
```

## Security Best Practices

1. **Never commit sensitive values** to version control
2. **Use different values** for staging vs production
3. **Rotate secrets regularly** (every 90 days)
4. **Use secrets management** tools (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault)
5. **Limit access** to staging environment variables
6. **Monitor secret usage** and access patterns
7. **Use least privilege** principle for service accounts

## Loading Environment Variables

### Docker Compose
```bash
docker-compose --env-file .env.staging up -d
```

### Systemd Service
```ini
[Service]
EnvironmentFile=/etc/witchcityrope/staging.env
```

### PowerShell
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Staging"
$env:JWT_SECRET = "your-staging-jwt-secret"
# ... other variables
```

### Bash
```bash
export ASPNETCORE_ENVIRONMENT=Staging
export JWT_SECRET="your-staging-jwt-secret"
# ... other variables
```