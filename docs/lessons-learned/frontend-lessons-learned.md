# Frontend Lessons Learned - WitchCityRope React

## Critical Button Sizing Issue - NEVER REPEAT

**Problem**: Button text gets cut off when not properly sized
**Solution**: ALWAYS set explicit button dimensions
**Example**:
```tsx
// ❌ WRONG - Text gets cut off
<Button>Add Email Template</Button>

// ✅ CORRECT - Properly sized
<Button style={{
  minWidth: '160px',
  height: '48px', 
  fontWeight: 600,
}}>Add Email Template</Button>
```

## TinyMCE Editor Implementation

**Problem**: Removing working TinyMCE implementation and replacing with basic textarea
**Solution**: ALWAYS verify TinyMCE is working before making changes
**Implementation**:
```tsx
import { Editor } from '@tinymce/tinymce-react';

<Editor
  value={value}
  onEditorChange={(content) => setValue(content)}
  init={{
    height: 300,
    menubar: false,
    plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker',
    toolbar: 'undo redo | blocks | bold italic underline strikethrough | link | bullist numlist | indent outdent | removeformat',
    branding: false,
  }}
/>
```

## TinyMCE CDN Loading and Debugging

**Problem**: Misinterpreting 307 redirects as TinyMCE loading failures
**Solution**: 307 redirects from TinyMCE CDN are NORMAL - they route to optimal edge servers
**Key Points**:
- TinyMCE takes 3-5 seconds to initialize from CDN
- Network 307 redirects are expected behavior, not errors  
- Use Playwright to verify actual functionality vs network logs
- API key `3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp` works correctly
**Debug Command**: `npx playwright test tests/playwright/tinymce-debug.spec.ts --headed`

## TinyMCE v8 Plugin Compatibility - CRITICAL FIX

**Problem**: TinyMCE v8 deprecated `textcolor` and `colorpicker` plugins causing console errors
**Solution**: Remove deprecated plugins from all Editor configurations
**Before**:
```typescript
plugins: 'advlist autolink lists link charmap preview anchor textcolor colorpicker'
```
**After**:
```typescript
apiKey="3f628sek98zponk2rt5ncrkc2n5lj9ghobeppfskrjvkpmqp"
plugins: 'advlist autolink lists link charmap preview anchor'
```
**Action**: Always check TinyMCE migration docs when upgrading versions

## Missing CSS Classes Console Debugging

**Problem**: Using undefined CSS classes causes silent failures and missing styles
**Solution**: Replace all custom CSS classes with proper Mantine components
**Examples**:
```typescript
// ❌ WRONG - Undefined CSS classes
<button className="btn btn-primary">Add Session</button>
<button className="table-action-btn">Edit</button>

// ✅ CORRECT - Mantine components
<Button variant="filled" color="burgundy">Add Session</Button>
<Button size="compact-xs" variant="light" leftSection={<IconEdit />}>Edit</Button>
```
**Debug Method**: Use Playwright console error capture to find all undefined classes

## Wireframe Adherence

**Problem**: Adding sections not in wireframe (Staff Assignments) 
**Solution**: ALWAYS check wireframe before adding new sections
**Action**: Only implement what's explicitly shown in wireframes

## Tab Naming Precision

**Problem**: Using "Volunteers/Staff" when wireframe shows "Volunteers"
**Solution**: Use EXACT text from wireframes, don't add extra words

## Testing Requirements

**Problem**: Not testing visual changes before claiming completion
**Solution**: ALWAYS use Playwright to take screenshots and verify against wireframes
**Command**: `npm run test:e2e -- --grep="Screenshots"`

## Form Structure Following Wireframes

**Problem**: Missing volunteer position edit form at bottom of tab
**Solution**: Follow wireframe layout exactly, including all sections and forms

## Constant Page Reloading Issue - CRITICAL FIX APPLIED ✅

**Problem**: Pages with React components constantly reload and remount in infinite loops
**Root Cause**: Multiple conflicting Vite dev servers running on the same port causing resource conflicts and hot reload failures

**Solution Steps Applied**:

1. **CRITICAL**: Kill all conflicting dev servers first
```bash
# Check for multiple dev servers
ps aux | grep "vite\|npm" | grep -v grep

# Kill conflicting processes (may require sudo for system processes)
pkill -f vite
# OR restart on different port
npm run dev -- --port 5174
```

2. **API Client 401 Interceptor Fix**: 
```typescript
if (response?.status === 401) {
  const isOnDemoPage = window.location.pathname.includes('/demo') || 
                      window.location.pathname.includes('/admin/events-management-api-demo')
  
  if (!window.location.pathname.includes('/login') && !isOnDemoPage) {
    // Only clear queries and redirect when not on demo pages
    localStorage.removeItem('auth_token')
    queryClient.clear()
    window.location.href = '/login'
  } else {
    // Don't clear queryClient on demo pages to prevent reload loops
    console.log('Skipping redirect and query clearing (on demo page)');
  }
}
```

3. **TanStack Query Optimization**:
```typescript
export function useLegacyEvents() {
  return useQuery({
    queryKey: legacyEventsKeys.eventsList(),
    queryFn: () => legacyEventsApiService.getEvents(),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    refetchInterval: false, // Disable automatic refetching
    refetchOnReconnect: false,
    retry: (failureCount, error: any) => {
      // Don't retry on client errors (4xx)
      if (error?.response?.status >= 400 && error?.response?.status < 500) {
        return false;
      }
      return failureCount < 3;
    },
  });
}
```

4. **Mantine v7 Tabs Fix**:
```typescript
// ❌ WRONG (Mantine v6)
<Tabs value={activeTab} onTabChange={setActiveTab}>

// ✅ CORRECT (Mantine v7)  
<Tabs value={activeTab} onChange={setActiveTab}>
```

**Verification Results**:
- ✅ Component mounts: 2 (normal for React.StrictMode)
- ✅ Component unmounts: 1 (normal)
- ✅ Page navigations: 2 (normal)
- ✅ Events loading: 3 fallback events displaying correctly
- ✅ API calls: Working properly to http://localhost:5655/api/events

**Key Insight**: The primary cause was dev server conflicts, NOT TanStack Query configuration. Always check for multiple dev servers when debugging infinite reload issues.