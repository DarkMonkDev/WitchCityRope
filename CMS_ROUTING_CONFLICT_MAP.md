# CMS Dynamic Routing - Conflict Map & Route Priority Matrix

**Purpose**: Visual guide to all routes that would be affected by adding dynamic CMS routing

---

## Route Matching Priority (React Router v7)

React Router matches routes in the order they are defined. **More specific routes must come BEFORE less specific ones.**

### Critical Ordering Rules
```
1. Exact path matches (e.g., /login, /dashboard)
2. Parameterized routes with specific prefixes (e.g., /events/:id)
3. Nested routes (/admin/... routes)
4. Parameterized catch-all routes (e.g., /:slug)
5. Wildcard fallback (e.g., /*) - if implemented
```

---

## Current Route Structure (Lines 90-390 of router.tsx)

### Layer 1: Public Non-CMS Routes (MUST come before catch-all)

```
/ (index)                               ✓ High Priority
├── /login                              ✓ High Priority
├── /register                           ✓ High Priority
├── /unauthorized                       ✓ High Priority
├── /test                               ✓ Removable (test route)
├── /vetting-test                       ✓ Removable (test route)
├── /test-notifications                 ✓ Removable (test route)
├── /events                             ✓ High Priority (not /events/:slug)
├── /events/:id                         ✓ High Priority (parameterized)
├── /safety/report                      ✓ Medium Priority
├── /safety/status                      ✓ Medium Priority
├── /join                               ✓ Medium Priority
├── /vetting/apply                      ✓ Medium Priority
├── /checkout/:eventId                  ✓ High Priority
├── /checkout/:eventId/:registrationId  ✓ High Priority
├── /events/:eventId/payment/:registerationId  ✓ High Priority
├── /payment/success                    ✓ Medium Priority
├── /payment/cancel                     ✓ Medium Priority
├── /form-test                          ✓ Removable (test)
├── /mantine-forms                      ✓ Removable (test)
├── /api-validation-v2-simple           ✓ Removable (test)
├── /api-connection-test                ✓ Removable (test)
├── /test-msw                           ✓ Removable (test)
├── /navigation-test                    ✓ Removable (test)
├── /payment-test                       ✓ Removable (test)
├── /admin/event-session-matrix-demo    ✓ Can be protected
├── /admin/events-management-api-demo   ✓ Can be protected
└── /event-form-test                    ✓ Can be protected
```

### Layer 2: Protected Dashboard Routes (MUST come before catch-all)

```
/dashboard                              ✓ High Priority (protected)
├── /dashboard/profile-settings         ✓ High Priority
├── /my-reports                         ✓ High Priority
└── /my-reports/:id                     ✓ High Priority
```

### Layer 3: Admin Protected Routes (MUST come before catch-all)

```
/admin/                                 ✓ CRITICAL (all admin)
├── /admin (index)
├── /admin/events
├── /admin/events/new
├── /admin/events/:id
├── /admin/safety
├── /admin/safety/incidents
├── /admin/safety/incidents/:id
├── /admin/vetting
├── /admin/vetting/applications/:applicationId
├── /admin/members
├── /admin/members/:id
├── /admin/settings
├── /admin/email-templates
├── /admin/cms/revisions
└── /admin/cms/revisions/:pageId
```

### Layer 4: Current CMS Hardcoded Routes (Can be replaced/removed)

```
/resources                      <-- CMS Page: resources
/contact-us                     <-- CMS Page: contact-us
/private-lessons                <-- CMS Page: private-lessons
/about-us                       <-- CMS Page: about-us
/code-of-conduct                <-- CMS Page: code-of-conduct
/privacy-policy                 <-- CMS Page: privacy-policy
/terms-of-service               <-- CMS Page: terms-of-service
/refund-policy                  <-- CMS Page: refund-policy
/faq                            <-- CMS Page: faq
/cms/getting-started            <-- CMS Page: cms/getting-started (nested!)
/event-waiver                   <-- CMS Page: event-waiver
```

---

## Conflict Resolution Strategies

### Strategy A: Root-Level Catch-All (`:slug`)
**Pros**: Cleaner URLs, single route matches all CMS pages  
**Cons**: High collision risk, requires careful route ordering

**Router Configuration**:
```typescript
{
  path: '/',
  element: <RootLayout />,
  children: [
    // ... ALL other routes here (must be before catch-all)
    
    // CMS catch-all LAST
    {
      path: ':slug',
      element: <CmsPageWrapper />,
    },
  ]
}
```

**Collision Risk Matrix**:
| Route | Conflict With `:slug` | Risk Level |
|-------|----------------------|------------|
| `/resources` | No - goes to CMS now | ✓ Safe |
| `/events/:id` | `:slug` doesn't match parameterized route | ✓ Safe |
| `/checkout/:eventId` | `:slug` doesn't match parameterized route | ✓ Safe |
| `/admin/cms/revisions` | No - `/admin/` has priority | ✓ Safe |
| Typo URLs like `/abuot-us` | CAUGHT by `:slug` | ⚠️ Shows 404 from CmsPage |

---

### Strategy B: Nested Catch-All (`/cms/:slug`)
**Pros**: Safer, explicitly marks CMS pages, backward compatible  
**Cons**: Requires URL migration for existing pages

**Router Configuration**:
```typescript
{
  path: '/',
  element: <RootLayout />,
  children: [
    // ... existing routes unchanged
    
    // CMS nested under /cms prefix
    {
      path: 'cms/:slug',
      element: <CmsPageWrapper />,
    },
  ]
}
```

**URL Migration Needed**:
```
OLD                     NEW
/about-us               /cms/about-us
/contact-us             /cms/contact-us
/faq                    /cms/faq
```

**Impact**: Requires 301 redirects and SEO consideration

---

### Strategy C: Hybrid (Explicit + Catch-All)
**Pros**: Maximum compatibility, can phase in gradually  
**Cons**: Most code, hardest to maintain

**Router Configuration**:
```typescript
{
  path: '/',
  element: <RootLayout />,
  children: [
    // ... all protected routes
    
    // Keep POPULAR hardcoded routes
    { path: 'about-us', element: <AboutUsPage /> },
    { path: 'contact-us', element: <ContactUsPage /> },
    
    // NEW pages go through catch-all
    { path: ':slug', element: <CmsPageWrapper /> },
  ]
}
```

---

## Parameterized Route Analysis

### Routes That Use Parameters (Already Match-Specific)
These don't conflict with a `:slug` catch-all because React Router matches exact structure:

```
/events/:id              - React Router knows this is specific
/checkout/:eventId       - React Router knows this is specific
/my-reports/:id          - React Router knows this is specific
/admin/events/:id        - React Router knows this is specific
/admin/safety/incidents/:id  - React Router knows this is specific
```

**Why No Conflict?**
- `/events/123` matches `/events/:id` before it could match `/:slug`
- `/checkout/456` matches `/checkout/:eventId` first
- React Router tests routes in order they're defined

---

## CMS-Specific Route Conflicts

### Problem: `/cms/getting-started`

Current route: `/cms/getting-started`  
Expected CMS slug: `cms/getting-started`

**Issue**: If you add `/:slug` catch-all at root level:
- `/cms/getting-started` won't match because `/cms` is the first segment
- Would need a separate route or nested catch-all

**Solutions**:
1. **Keep hardcoded**: Keep `{ path: 'cms/getting-started', element: <GettingStartedPage /> }`
2. **Nested catch-all**: Add `{ path: 'cms/:slug', element: <CmsPageWrapper /> }` 
3. **Rename slug**: Change backend slug from `cms/getting-started` to just `getting-started`

---

## Recommended Safe Implementation

### Step 1: Register Route (Won't Break Anything)
```typescript
export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // ... all existing routes (no changes needed) ...
      
      // ADD THIS LAST
      {
        path: ':slug',
        element: <CmsDynamicPage />,
      },
    ],
  },
  // ... existing CheckIn routes ...
])
```

**Why Safe?**:
- Existing routes don't change
- `:slug` only matches unmatched URLs
- Existing CMS pages still work via old hardcoded routes
- New pages work via catch-all

### Step 2: Create CmsDynamicPage Component
```typescript
export const CmsDynamicPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  
  if (!slug) {
    return <Navigate to="/" replace />;
  }
  
  return <CmsPage slug={slug} />;
}
```

### Step 3: Test
- Visit `/about-us` - still works (matched first route)
- Visit `/cms/getting-started` - still works (matched first route)
- Visit `/new-page` - works (matched catch-all, fetches from API)
- Visit `/nonexistent` - shows CmsPage 404 message

### Step 4: Migrate (When Ready)
Once catch-all is stable:
1. Update navigation/menus to use new pages
2. Remove hardcoded route imports one by one
3. Add database migration to ensure slug consistency
4. Add 301 redirects for SEO

---

## Potential Issues & Mitigations

### Issue 1: Catch-All Matches Everything
**Problem**: `/random-typo` hits catch-all, calls API, gets 404 from CMS  
**Mitigation**: CmsPage component handles 404 gracefully (already does)

### Issue 2: URL Migration Breaks SEO
**Problem**: Changing `/about-us` to `/cms/about-us` breaks old links  
**Mitigation**: Keep hardcoded route and add redirect OR use 301 redirects at nginx level

### Issue 3: No Create Page UI
**Problem**: Can create pages via API but frontend has no UI  
**Mitigation**: Add admin page to create pages (future work)

### Issue 4: Cache Busting
**Problem**: Slug changes = different cache key  
**Mitigation**: React Query uses slug in cache key (intentional, correct behavior)

### Issue 5: Deep Nesting (admin/cms/revisions/123)
**Problem**: Admin revision routes might conflict with catch-all  
**Mitigation**: Admin routes already come before catch-all in order, safe

---

## Route Ordering Checklist for Safe Implementation

- [ ] All `/admin/*` routes are defined before catch-all
- [ ] All protected routes (`/dashboard`, `/my-reports`) before catch-all
- [ ] All payment routes (`/checkout/*`) before catch-all
- [ ] All event routes (`/events/:id`) before catch-all
- [ ] `:slug` catch-all is LAST in children array
- [ ] CmsDynamicPage component handles missing slug
- [ ] CmsDynamicPage uses useParams correctly
- [ ] API 404 errors show graceful message
- [ ] Test with typo URL shows graceful 404 (not server error)
- [ ] Verify existing pages still work at old URLs

---

## File Changes Required for Safe Implementation

| File | Change | Complexity |
|------|--------|------------|
| `router.tsx` | Add `:slug` route at end | Trivial |
| `CmsDynamicPage.tsx` | Create new wrapper component | Low |
| `CmsPage.tsx` | No change needed (already works) | N/A |
| `useCmsPage.ts` | No change needed | N/A |
| API endpoints | No change needed | N/A |
| Tests | Add E2E tests for catch-all | Medium |

---

## Summary

**For safe, non-breaking dynamic routing implementation:**

1. Add one `:slug` route at the END of all other routes
2. Create a simple wrapper component that extracts slug from params
3. Component passes slug to existing CmsPage (no changes needed)
4. All existing routes continue to work unchanged
5. New CMS pages are accessible via catch-all route
6. No URL migration required (backward compatible)
7. Can be deployed immediately with zero risk
8. Later migration of hardcoded routes is optional cleanup
