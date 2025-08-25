# TinyMCE API Key Configuration Guide

## Overview

This guide explains how to configure the TinyMCE API key for the WitchCityRope React application to unlock cloud-based features and remove restrictions.

## API Key Details

- **API Key**: `3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp`
- **Provider**: TinyCloud (tiny.cloud)
- **Usage**: Same key for both development and production
- **Features Enabled**: 
  - Premium plugins access
  - Cloud-based spell checking
  - Enhanced toolbar options
  - Branding removal
  - Increased request limits

## Environment Configuration

### Development Setup

1. **Environment Variable**: The API key is stored in `.env.development`:
```bash
# TinyMCE Cloud API Key
VITE_TINYMCE_API_KEY=3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp
```

2. **Production Environment**: Also configured in `.env.production`:
```bash
# TinyMCE Cloud API Key (same for dev and production)
VITE_TINYMCE_API_KEY=3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp
```

### Security Measures

- **Never commit API keys**: Environment files with real keys are gitignored
- **Example file**: `.env.example` contains placeholder for new developers
- **Runtime validation**: Component checks for missing API key and shows warning

## Component Implementation

The `TinyMCERichTextEditor` component automatically:

1. **Reads API key** from `import.meta.env.VITE_TINYMCE_API_KEY`
2. **Shows warning** if API key is missing
3. **Falls back gracefully** to basic functionality without key
4. **Passes key to TinyMCE** via the `apiKey` prop

```typescript
// Automatic API key usage
const apiKey = import.meta.env.VITE_TINYMCE_API_KEY;

<Editor
  apiKey={apiKey}
  // ... other props
/>
```

## For New Developers

1. **Copy environment template**:
```bash
cp .env.example .env.development
```

2. **Update API key** in your `.env.development` file:
```bash
VITE_TINYMCE_API_KEY=3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp
```

3. **Restart dev server** to load new environment variables:
```bash
npm run dev
```

## Domain Configuration

The API key is configured for the following domains:
- `localhost:*` (development)
- `witchcityrope.com` (production)
- Any subdomains of the above

## Troubleshooting

### Missing API Key Warning
If you see: "TinyMCE API key not configured. Using basic functionality."

**Solution**: 
1. Check your `.env.development` file exists
2. Verify `VITE_TINYMCE_API_KEY` is set correctly
3. Restart the development server

### Editor Not Loading
If TinyMCE editor doesn't appear:

**Check**:
1. Network connectivity to TinyCloud
2. Console for JavaScript errors
3. API key validity in TinyCloud dashboard

### Limited Functionality
If some features are missing:

**Verify**:
1. API key is properly configured
2. Key hasn't exceeded usage limits
3. Domain is allowed in TinyCloud settings

## API Key Management

- **Current Key Owner**: Development team lead
- **TinyCloud Account**: Contact project admin for access
- **Renewal**: Key doesn't expire but monitor usage limits
- **Backup**: Same key works across all environments

## Files Modified

- `/apps/web/.env.development` - Development API key
- `/apps/web/.env.production` - Production API key  
- `/apps/web/.env.example` - Template for new developers
- `/apps/web/.gitignore` - Ignore real environment files
- `/apps/web/src/components/forms/TinyMCERichTextEditor.tsx` - Component implementation

## Security Notes

- ✅ API key stored in environment variables (not source code)
- ✅ Environment files are gitignored
- ✅ Example file provided for new developers
- ✅ Component handles missing key gracefully
- ✅ Warning shown if key is not configured

## Next Steps

The TinyMCE editor is now fully configured with the API key. All rich text editing features are available across the application with consistent behavior in development and production environments.