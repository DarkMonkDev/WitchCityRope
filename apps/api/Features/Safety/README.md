# Safety System - Encryption Configuration

## Overview
The Safety System uses AES-256 encryption to protect sensitive incident data including descriptions, contact information, and involved parties.

## Encryption Key Configuration

### Development Key
The development environment uses a randomly generated 256-bit key stored in `appsettings.Development.json`:

```json
{
  "Safety": {
    "EncryptionKey": "QEtBHh15GCVx77qQ6vvmN6aoPbK9iL+fVxlc+iI6Olo="
  }
}
```

### Generating New Keys

#### For Development/Testing
```bash
# Generate a new 256-bit (32 bytes) random key and Base64 encode it
openssl rand -base64 32
```

#### For Production
**CRITICAL**: Never use the development key in production!

1. Generate a secure random key:
```bash
openssl rand -base64 32
```

2. Store the key securely using:
   - Azure Key Vault
   - AWS Secrets Manager
   - Kubernetes secrets
   - Environment variables (for containerized deployments)

3. Configure in production appsettings:
```json
{
  "Safety": {
    "EncryptionKey": "${SAFETY_ENCRYPTION_KEY}"
  }
}
```

### Key Requirements
- **Length**: Exactly 32 bytes (256 bits) when Base64 decoded
- **Format**: Base64 encoded string
- **Security**: Cryptographically secure random generation
- **Rotation**: Plan for periodic key rotation with data migration

### Validation
To verify a key is the correct size:
```bash
echo "YOUR_BASE64_KEY" | base64 -d | wc -c
# Should output: 32
```

## Encryption Process
1. **Encryption**: Data is encrypted using AES-256-CBC with a random IV per operation
2. **Storage**: Encrypted data is stored as Base64 strings in the database
3. **Decryption**: Data is decrypted when accessed by authorized safety team members

## Error Resolution
If you encounter "Specified key is not a valid size for this algorithm":
1. Verify the key decodes to exactly 32 bytes
2. Ensure the key is properly Base64 encoded
3. Generate a new key using the methods above
4. Restart the API service after configuration changes

## Security Notes
- Never log or expose encryption keys
- Use different keys for different environments
- Implement key rotation procedures
- Monitor for decryption failures
- Keys should be stored in secure key management systems in production