# Blazor Server Troubleshooting Guide

> **⚠️ ARCHIVED**: This Blazor-specific troubleshooting guide has been consolidated into `/docs/lessons-learned/frontend-lessons-learned.md` with React equivalents.
> 
> **Archive Date**: 2025-08-16  
> **Reason**: Migrated to React - Blazor Server troubleshooting no longer applicable
> 
> This file is kept for historical reference during the migration period.

## Overview

This document contains hard-won lessons from debugging Blazor Server issues in the WitchCityRope application. These issues took hours to resolve and understanding them will save future developers significant time.

## Common Issues and Solutions

### 1. Interactive Elements Not Working (Tabs, Buttons, Forms)

#### Symptoms
- Tab clicks do nothing
- Buttons don't respond to clicks
- Forms submit with full page refresh instead of AJAX
- No onclick handlers in rendered HTML
- Browser shows static HTML with no interactivity

#### Root Cause
Missing `@rendermode` directive on interactive pages. This is MANDATORY in .NET 9 Blazor Server.

#### Solution
Add the render mode directive to ALL interactive pages:

```razor
@page "/admin/events/edit/{EventId:guid}"
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
@* Rest of your page content *@
```

#### Verification
1. Check Network tab for WebSocket connection to `_blazor` endpoint
2. Inspect button elements - they should have onclick handlers
3. No full page refresh when clicking interactive elements

### 2. "Blazor has already started" Error

#### Symptoms
- Console error: "Blazor has already started"
- But Blazor doesn't actually work
- No WebSocket connections
- Blazor object exists but no interactivity

#### Root Cause
Deprecated browser events (`beforeunload`, `unload`) in blazor.server.js preventing proper initialization.

#### Solution
Create a JavaScript file that intercepts deprecated events BEFORE blazor.server.js loads:

```javascript
// blazor-fix.js
(function() {
    const originalAddEventListener = window.addEventListener;
    
    window.addEventListener = function(type, listener, options) {
        if (type === 'beforeunload' || type === 'unload') {
            // Redirect to modern 'pagehide' event
            return originalAddEventListener.call(this, 'pagehide', listener, options);
        }
        return originalAddEventListener.call(this, type, listener, options);
    };
})();
```

Load it BEFORE blazor.server.js in App.razor:
```html
<script src="js/blazor-fix.js"></script>
<script src="_framework/blazor.server.js"></script>
```

### 3. Blazor Error UI Shows on Every Page

#### Symptoms
- Yellow error bar at bottom of EVERY page
- Shows even when no errors occur
- CSS issue, not JavaScript

#### Solution
Add CSS to hide the error UI by default:

```css
/* app.css */
#blazor-error-ui {
    background: lightyellow;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
    display: none; /* Hidden by default */
    left: 0;
    padding: 0.6rem 1.25rem 0.7rem 1.25rem;
    position: fixed;
    width: 100%;
    z-index: 1000;
}
```

### 4. Syncfusion Component Initialization Errors

#### Symptoms
- "Object reference not set to an instance of an object"
- Errors when switching tabs containing Syncfusion components
- RichTextEditor fails to initialize

#### Solution
Always use nullable references and null checks:

```csharp
// Component reference
private SfRichTextEditor? emailTemplateEditor;  // Note the ?

// Before using
if (emailTemplateEditor != null)
{
    var content = await emailTemplateEditor.GetTextAsync();
}
```

For persistent issues, replace with simpler alternatives:
```razor
@* Instead of complex RichTextEditor *@
<textarea @bind="model.Content" class="form-control" rows="10"></textarea>
```

## Debugging Checklist

### 1. Check Blazor Initialization

```javascript
// Run in browser console
const blazorState = {
    hasBlazor: typeof window.Blazor !== 'undefined',
    hasNavigationManager: !!window.Blazor?._internal?.navigationManager,
    hasReconnectionHandler: !!window.Blazor?.defaultReconnectionHandler,
    webSockets: performance.getEntriesByType('resource')
        .filter(e => e.name.includes('_blazor') && e.name.includes('ws://'))
};
console.log(blazorState);
```

### 2. Common Mistakes to Avoid

❌ **DON'T** check for `Blazor._started` - this property doesn't exist
❌ **DON'T** forget to add `@rendermode` to interactive pages
❌ **DON'T** assume hot reload works - always restart containers
❌ **DON'T** load blazor-fix.js AFTER blazor.server.js

✅ **DO** check WebSocket connections in Network tab
✅ **DO** restart containers after Blazor configuration changes
✅ **DO** use nullable references for component @ref
✅ **DO** check browser console for deprecation warnings

### 3. Container Restart Command

After ANY of these changes, restart the container:
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
```

Wait 10-15 seconds for full startup before testing.

## Quick Diagnostic Test

Create this test file to quickly diagnose Blazor issues:

```javascript
// test-blazor-health.js
const puppeteer = require('puppeteer');

async function testBlazorHealth() {
    const browser = await puppeteer.launch({ headless: false });
    const page = await browser.newPage();
    
    await page.goto('http://localhost:5651/admin/events/edit/some-guid');
    
    const health = await page.evaluate(() => ({
        blazor: typeof window.Blazor !== 'undefined',
        websockets: performance.getEntriesByType('resource')
            .filter(e => e.name.includes('_blazor')).length,
        hasOnclick: document.querySelector('button')?.onclick !== null
    }));
    
    console.log('Blazor Health:', health);
    // Expected: { blazor: true, websockets: > 0, hasOnclick: true }
}
```

## Prevention

1. **Always add @rendermode to new interactive pages**
2. **Test interactivity immediately after creating new pages**
3. **Use the project's validation script**: `./scripts/validate-blazor-architecture.sh`
4. **Check CLAUDE.md for architecture requirements**
5. **Run E2E tests after major changes**

## Known Issues in WitchCityRope

As of January 19, 2025, the following pages are missing `@rendermode` and may not work properly:

### Critical Pages (Forms/User Input)
- ❌ `/Features/Auth/Pages/DeletePersonalData.razor`
- ❌ `/Features/Auth/Pages/ChangePassword.razor`
- ❌ `/Features/Auth/Pages/LoginWith2fa.razor`
- ❌ `/Features/Auth/Pages/ManageEmail.razor`
- ❌ `/Features/Auth/Pages/ResetPassword.razor`
- ❌ `/Features/Auth/Pages/Register.razor`
- ❌ `/Features/Auth/Pages/Login.razor`
- ❌ `/Features/Auth/Pages/ForgotPassword.razor`
- ❌ `/Features/Auth/Pages/ManageProfile.razor`
- ❌ `/Features/Members/Pages/Profile.razor`
- ❌ `/Features/Members/Pages/MyTickets.razor`

### How to Check
Run the provided script to find all pages missing @rendermode:
```bash
./scripts/check-rendermode.sh
```

## References

- [.NET 9 Blazor Render Modes](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)
- [Browser Deprecation of unload Events](https://developer.chrome.com/blog/page-lifecycle-api/)
- [Blazor Server SignalR Connection](https://docs.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr)