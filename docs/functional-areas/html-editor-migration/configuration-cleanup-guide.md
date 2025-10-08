# Configuration Cleanup Guide: TinyMCE to @mantine/tiptap
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: DevOps / Configuration Management Team -->
<!-- Status: Active -->

## Purpose
This guide provides line-by-line instructions for removing all TinyMCE configuration references and package dependencies from the WitchCityRope project.

## Overview

### Files to Update (5 total)

| File | Lines to Remove | Purpose |
|------|-----------------|---------|
| `.env.example` | 14-15 | Remove API key example |
| `.env.staging` | 1 line | Remove staging API key |
| `.env.template` | 1 line | Remove template API key |
| `environment.ts` | 45, 86 | Remove config property |
| `package.json` | 35 | Remove dependency |

---

## File 1: /apps/web/.env.example

### Current Content (Lines 14-15)

```env
# TinyMCE API Key (for rich text editor)
VITE_TINYMCE_API_KEY=your-api-key-here
```

### Action Required

**DELETE** these 2 lines completely.

### Updated Content

```env
# (Previous content above line 14)
# (Next content after line 15)
```

### Verification

```bash
# Check file
cat /home/chad/repos/witchcityrope/apps/web/.env.example | grep -n "TINYMCE"

# Expected output: (nothing)
# If still shows lines, deletion incomplete

# Count lines (should be 2 fewer than before)
wc -l /home/chad/repos/witchcityrope/apps/web/.env.example
```

### Before/After Comparison

**Before** (total ~25 lines):
```env
VITE_API_BASE_URL=http://localhost:5655
VITE_APP_ENVIRONMENT=development
VITE_LOG_LEVEL=debug

# TinyMCE API Key (for rich text editor)
VITE_TINYMCE_API_KEY=your-api-key-here

# Other config variables
VITE_PAYPAL_CLIENT_ID=your-paypal-client-id
```

**After** (total ~23 lines):
```env
VITE_API_BASE_URL=http://localhost:5655
VITE_APP_ENVIRONMENT=development
VITE_LOG_LEVEL=debug

# Other config variables
VITE_PAYPAL_CLIENT_ID=your-paypal-client-id
```

---

## File 2: /apps/web/.env.staging

### Current Content

```env
VITE_TINYMCE_API_KEY=actual-staging-api-key-value-here
```

### Action Required

**DELETE** the entire line containing `VITE_TINYMCE_API_KEY`.

### Verification

```bash
# Check file
grep "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env.staging

# Expected output: (nothing)

# Verify file still valid
cat /home/chad/repos/witchcityrope/apps/web/.env.staging
# Should show other staging config, no TINYMCE
```

### Important Note

**SECURITY**: If this file contains actual API keys, ensure no TINYMCE references remain. The file may not be in git (likely in .gitignore), so changes are local only.

---

## File 3: /apps/web/.env.template

### Current Content

```env
VITE_TINYMCE_API_KEY=
```

### Action Required

**DELETE** the entire line containing `VITE_TINYMCE_API_KEY`.

### Verification

```bash
# Check file
grep "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env.template

# Expected output: (nothing)

# View entire file
cat /home/chad/repos/witchcityrope/apps/web/.env.template
```

---

## File 4: /apps/web/src/config/environment.ts

### Current Content (Line 45)

```typescript
export const environment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  appEnvironment: import.meta.env.VITE_APP_ENVIRONMENT || 'development',
  logLevel: import.meta.env.VITE_LOG_LEVEL || 'info',
  tinyMceApiKey: import.meta.env.VITE_TINYMCE_API_KEY,  // <- LINE 45: DELETE THIS
  paypalClientId: import.meta.env.VITE_PAYPAL_CLIENT_ID,
  // ... other config
};
```

### Current Content (Line 86 - Type Definition)

```typescript
interface EnvironmentConfig {
  apiBaseUrl: string;
  appEnvironment: string;
  logLevel: string;
  tinyMceApiKey?: string;  // <- LINE 86: DELETE THIS
  paypalClientId?: string;
  // ... other types
}
```

### Action Required

1. **DELETE line 45**: Remove `tinyMceApiKey` property from config object
2. **DELETE line 86**: Remove `tinyMceApiKey` from type definition

### Updated Content

**After removing line 45**:
```typescript
export const environment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  appEnvironment: import.meta.env.VITE_APP_ENVIRONMENT || 'development',
  logLevel: import.meta.env.VITE_LOG_LEVEL || 'info',
  paypalClientId: import.meta.env.VITE_PAYPAL_CLIENT_ID,
  // ... other config
};
```

**After removing line 86**:
```typescript
interface EnvironmentConfig {
  apiBaseUrl: string;
  appEnvironment: string;
  logLevel: string;
  paypalClientId?: string;
  // ... other types
}
```

### Verification

```bash
# Search for tinyMce references
grep -n "tinyMce" /home/chad/repos/witchcityrope/apps/web/src/config/environment.ts

# Expected output: (nothing)

# TypeScript compilation check
cd /home/chad/repos/witchcityrope/apps/web
npx tsc --noEmit

# Expected: No TypeScript errors
```

### Before/After Comparison

**Before** (environment object):
```typescript
export const environment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  appEnvironment: import.meta.env.VITE_APP_ENVIRONMENT || 'development',
  logLevel: import.meta.env.VITE_LOG_LEVEL || 'info',
  tinyMceApiKey: import.meta.env.VITE_TINYMCE_API_KEY,
  paypalClientId: import.meta.env.VITE_PAYPAL_CLIENT_ID,
  stripePublishableKey: import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY,
};
```

**After** (environment object):
```typescript
export const environment = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5655',
  appEnvironment: import.meta.env.VITE_APP_ENVIRONMENT || 'development',
  logLevel: import.meta.env.VITE_LOG_LEVEL || 'info',
  paypalClientId: import.meta.env.VITE_PAYPAL_CLIENT_ID,
  stripePublishableKey: import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY,
};
```

---

## File 5: /apps/web/package.json

### Current Content (Line 35)

```json
{
  "dependencies": {
    "@hookform/resolvers": "^5.2.1",
    "@mantine/core": "^7.17.8",
    "@mantine/tiptap": "^7.17.8",
    "@tinymce/tinymce-react": "^6.3.0",  // <- LINE 35: DELETE THIS
    "@tiptap/react": "^3.3.0",
    // ... other dependencies
  }
}
```

### Action Required

**DELETE line 35**: Remove `"@tinymce/tinymce-react": "^6.3.0",`

**IMPORTANT**: Also remove the trailing comma from the previous line if this is not the last dependency.

### Updated Content

```json
{
  "dependencies": {
    "@hookform/resolvers": "^5.2.1",
    "@mantine/core": "^7.17.8",
    "@mantine/tiptap": "^7.17.8",
    "@tiptap/react": "^3.3.0",
    // ... other dependencies
  }
}
```

### Verification

```bash
# Search for tinymce in package.json
grep -n "tinymce" /home/chad/repos/witchcityrope/apps/web/package.json

# Expected output: (nothing)

# Validate JSON syntax
cat /home/chad/repos/witchcityrope/apps/web/package.json | jq '.' > /dev/null
# Expected: No errors (JSON is valid)
```

### Package Dependencies Comparison

**Before**:
```json
{
  "dependencies": {
    "@mantine/tiptap": "^7.17.8",
    "@tinymce/tinymce-react": "^6.3.0",
    "@tiptap/extension-highlight": "^3.3.0",
    "@tiptap/extension-link": "^3.3.0",
    "@tiptap/react": "^3.3.0",
    "@tiptap/starter-kit": "^3.3.0"
  }
}
```

**After**:
```json
{
  "dependencies": {
    "@mantine/tiptap": "^7.17.8",
    "@tiptap/extension-highlight": "^3.3.0",
    "@tiptap/extension-link": "^3.3.0",
    "@tiptap/react": "^3.3.0",
    "@tiptap/starter-kit": "^3.3.0"
  }
}
```

---

## Package Cleanup

### Step 1: Uninstall TinyMCE Package

```bash
cd /home/chad/repos/witchcityrope/apps/web

# Uninstall package
npm uninstall @tinymce/tinymce-react

# Expected output:
# removed 1 package, and audited XXX packages in Xs
```

### Step 2: Verify Removal

```bash
# Check package.json
npm list @tinymce/tinymce-react

# Expected output:
# npm ERR! code ELSPROBLEMS
# npm ERR! extraneous: @tinymce/tinymce-react@6.3.0 (not found)

# Check node_modules
ls -la node_modules/@tinymce/

# Expected output:
# ls: cannot access 'node_modules/@tinymce/': No such file or directory
```

### Step 3: Clean Install (Optional but Recommended)

```bash
# Remove node_modules and lock file
rm -rf node_modules package-lock.json

# Fresh install
npm install

# Verify all Tiptap packages present
npm list @mantine/tiptap
npm list @tiptap/react

# Expected: Both show installed versions
```

### Step 4: Verify Build Still Works

```bash
# TypeScript compilation
npx tsc --noEmit

# Build production bundle
npm run build

# Expected: Build succeeds with no errors
```

---

## Complete Cleanup Verification

### Verification Checklist

Run all verification commands:

```bash
# 1. No TINYMCE in .env files
grep -r "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env* 2>/dev/null
# Expected: (nothing)

# 2. No tinyMce in environment.ts
grep "tinyMce" /home/chad/repos/witchcityrope/apps/web/src/config/environment.ts
# Expected: (nothing)

# 3. No tinymce in package.json
grep "tinymce" /home/chad/repos/witchcityrope/apps/web/package.json
# Expected: (nothing)

# 4. No @tinymce in node_modules
ls /home/chad/repos/witchcityrope/apps/web/node_modules/@tinymce/ 2>&1
# Expected: "No such file or directory"

# 5. TypeScript compiles
cd /home/chad/repos/witchcityrope/apps/web && npx tsc --noEmit
# Expected: No errors

# 6. Build succeeds
npm run build
# Expected: Build completes successfully
```

### Success Criteria

ALL of the following must be true:

- [ ] No TINYMCE environment variables in any .env file
- [ ] No tinyMce references in environment.ts
- [ ] No @tinymce packages in package.json
- [ ] No @tinymce packages in node_modules
- [ ] TypeScript compilation: 0 errors
- [ ] Production build: succeeds
- [ ] No broken imports in codebase

---

## Troubleshooting

### Issue: TypeScript Errors After Cleanup

**Error**: `Cannot find name 'tinyMceApiKey'`

**Cause**: Code still references removed config property

**Solution**: Search and remove references
```bash
grep -r "tinyMceApiKey" apps/web/src/
# Update or remove any files that reference it
```

### Issue: Build Fails After Package Removal

**Error**: `Cannot resolve '@tinymce/tinymce-react'`

**Cause**: Import statements still reference TinyMCE

**Solution**: Find and remove imports
```bash
grep -r "@tinymce" apps/web/src/
# Remove all imports, replace with @mantine/tiptap
```

### Issue: Package Still Shows in node_modules

**Error**: `/node_modules/@tinymce/` directory still exists

**Solution**: Force clean install
```bash
rm -rf node_modules package-lock.json
npm install
```

### Issue: Environment Variable Still Loaded

**Error**: `import.meta.env.VITE_TINYMCE_API_KEY` still has value

**Cause**: Local .env file not updated, or dev server not restarted

**Solution**:
```bash
# Remove from local .env
nano .env
# Delete VITE_TINYMCE_API_KEY line

# Restart dev server
docker-compose down
docker-compose up -d
```

---

## Automated Cleanup Script

**Optional**: Script to automate all cleanup steps

```bash
#!/bin/bash
# cleanup-tinymce-config.sh

set -e  # Exit on error

APPS_WEB_DIR="/home/chad/repos/witchcityrope/apps/web"

echo "🧹 TinyMCE Configuration Cleanup"
echo "================================="
echo ""

# Step 1: Clean .env files
echo "Step 1: Cleaning .env files..."
sed -i '/VITE_TINYMCE_API_KEY/d' "$APPS_WEB_DIR/.env.example" || true
sed -i '/VITE_TINYMCE_API_KEY/d' "$APPS_WEB_DIR/.env.staging" || true
sed -i '/VITE_TINYMCE_API_KEY/d' "$APPS_WEB_DIR/.env.template" || true
sed -i '/# TinyMCE API Key/d' "$APPS_WEB_DIR/.env.example" || true
echo "✓ .env files cleaned"

# Step 2: Clean environment.ts
echo "Step 2: Cleaning environment.ts..."
sed -i '/tinyMceApiKey/d' "$APPS_WEB_DIR/src/config/environment.ts"
echo "✓ environment.ts cleaned"

# Step 3: Uninstall package
echo "Step 3: Uninstalling @tinymce/tinymce-react..."
cd "$APPS_WEB_DIR"
npm uninstall @tinymce/tinymce-react
echo "✓ Package uninstalled"

# Step 4: Verify
echo "Step 4: Verifying cleanup..."
if grep -r "TINYMCE" "$APPS_WEB_DIR/.env"* 2>/dev/null; then
  echo "❌ TINYMCE still found in .env files!"
  exit 1
fi

if grep "tinyMce" "$APPS_WEB_DIR/src/config/environment.ts"; then
  echo "❌ tinyMce still found in environment.ts!"
  exit 1
fi

if npm list @tinymce/tinymce-react 2>/dev/null; then
  echo "❌ @tinymce/tinymce-react still installed!"
  exit 1
fi

echo "✓ All verifications passed"
echo ""
echo "🎉 Cleanup complete!"
echo "Next: Run 'npm run build' to verify build still works"
```

**Usage**:
```bash
chmod +x cleanup-tinymce-config.sh
./cleanup-tinymce-config.sh
```

---

## Configuration Summary

### Before Migration

**Environment Variables**:
- `VITE_TINYMCE_API_KEY` in 3 .env files

**Config Objects**:
- `environment.tinyMceApiKey` property

**Dependencies**:
- `@tinymce/tinymce-react@6.3.0`

**Total Configuration Points**: 5

### After Migration

**Environment Variables**:
- (none)

**Config Objects**:
- (none)

**Dependencies**:
- (none - uses existing @mantine/tiptap)

**Total Configuration Points**: 0

### Benefits

- ✅ **No API key management** - One less secret to manage
- ✅ **No environment-specific config** - Same config everywhere
- ✅ **Simpler deployment** - Fewer environment variables
- ✅ **Smaller bundle** - ~350KB reduction
- ✅ **Faster builds** - One less dependency to resolve

---

## Related Documentation

- **Migration Plan**: [migration-plan.md](./migration-plan.md) - Complete migration phases
- **Component Guide**: [component-implementation-guide.md](./component-implementation-guide.md) - Component code
- **Testing Guide**: [testing-migration-guide.md](./testing-migration-guide.md) - Test updates
- **Rollback Plan**: [rollback-plan.md](./rollback-plan.md) - Emergency procedures

---

## Version History
- **v1.0** (2025-10-08): Initial configuration cleanup guide created

---

**Questions?** See troubleshooting section above or consult [migration-plan.md](./migration-plan.md).
