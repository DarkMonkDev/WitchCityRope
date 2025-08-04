# Syncfusion License Setup

## Overview
WitchCityRope uses Syncfusion Blazor components for UI. A valid license is required to remove the trial watermark.

## Getting a License

### Option 1: Community License (Free)
If you qualify for the community license:
1. Go to https://www.syncfusion.com/products/communitylicense
2. Sign up for a free account
3. Generate your license key
4. The community license is free for companies and individuals with less than $1 million USD in annual gross revenue

### Option 2: Trial License (30 days)
1. Go to https://www.syncfusion.com/account/downloads
2. Sign up for a trial account
3. Generate a trial license key

### Option 3: Commercial License
1. Purchase a license from https://www.syncfusion.com/sales/products
2. Access your license key from your account dashboard

## Setting Up the License

### Method 1: Environment Variable (Recommended for Production)
Set the environment variable:
```bash
# Windows PowerShell
$env:SYNCFUSION_LICENSE_KEY="your-license-key-here"

# Linux/Mac/WSL
export SYNCFUSION_LICENSE_KEY="your-license-key-here"
```

### Method 2: appsettings.json
Update the license key in `appsettings.json`:
```json
{
  "Syncfusion": {
    "LicenseKey": "your-license-key-here"
  }
}
```

### Method 3: User Secrets (Recommended for Development)
```bash
# Navigate to the Web project
cd src/WitchCityRope.Web

# Initialize user secrets
dotnet user-secrets init

# Set the license key
dotnet user-secrets set "Syncfusion:LicenseKey" "your-license-key-here"
```

## Current Version
The project uses Syncfusion.Blazor version 30.1.37. Make sure your license key is valid for this version.

## Troubleshooting

### License Prompt Still Appears
1. Ensure the license key is being loaded correctly
2. Check the console output for the warning message
3. Verify the license key is valid for version 30.1.37
4. Clear browser cache and restart the application

### Invalid License Key
- Make sure you're using the correct license key format
- Ensure the key hasn't expired
- Verify the key supports the Blazor platform

### Version Mismatch
If you upgraded from an older version, you may need a new license key that supports version 30.x.

## Running Without a License
The application will run in trial mode without a license, showing a trial watermark. This is acceptable for development but not for production use.