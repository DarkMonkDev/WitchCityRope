# React Developer Lessons Learned - Part 3

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 🚨 NAVIGATION: LESSONS LEARNED SPLIT FILES 🚨

**FILES**: 3 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned.md` (STARTUP + CRITICAL PATTERNS)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md` (CONTINUED LESSONS)
**Part 3**: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-3.md` (THIS FILE - MORE LESSONS)
**Read ALL**: All three parts are MANDATORY
**Write to**: THIS FILE (Part 3) for new lessons
**Maximum file size**: 1700 lines (to stay under token limits). All three parts can be up to 1700 lines each

## 🚨 ULTRA CRITICAL: ADD NEW LESSONS TO THIS FILE, NOT PART 1 OR PART 2! 🚨

**PART 1 IS FOR STARTUP** - Keep Part 1 under 1700 lines for startup procedures
**PART 2 IS FOR CORE LESSONS** - Keep Part 2 under 1700 lines
**ALL NEW LESSONS GO HERE** - This is Part 3, the current lessons file
**IF YOU ADD TO PART 1 OR PART 2** - You are doing it wrong!

---

**Skills Usage**: See `/.claude/skills/HOW-TO-USE-SKILLS.md` for complete guide on when/how to use skills

---

## 🚨 CRITICAL: TESTING LIBRARY GETBYTEXT FAILS WITH DUPLICATE TEXT - USE GETBYROLE WITH LEVEL 🚨
**Date**: 2025-11-09
**Category**: React Testing Library / Mantine Modals
**Severity**: CRITICAL - BREAKS TESTS

### What We Learned
**GETBYTEXT FAILS WHEN TEXT APPEARS MULTIPLE TIMES**: Mantine Modal creates nested heading elements (`<h2>` wrapper + `<h3>` actual title), causing `getByText` to find multiple matches.

**ERROR**:
```
TestingLibraryElementError: Found multiple elements with the text: /Remove RSVP?/i

Elements:
  <h2 class="mantine-Modal-title">
    <h3>Remove RSVP?</h3>
  </h2>
```

**ROOT CAUSE**: Mantine Modal wraps custom title in `<h2 class="mantine-Modal-title">`. If title is a heading element (`<h3>`), Testing Library finds BOTH headings.

### ✅ CRITICAL SOLUTION: USE GETBYROLE WITH LEVEL

```typescript
// ❌ WRONG: getByText finds multiple headings
expect(screen.getByText(/Remove RSVP?/i)).toBeInTheDocument();
// Error: Found multiple elements

// ❌ WRONG: getByRole without level still ambiguous
expect(screen.getByRole('heading', { name: /Remove RSVP?/i })).toBeInTheDocument();
// Error: Found multiple elements (h2 and h3)

// ✅ CORRECT: Specify heading level
expect(screen.getByRole('heading', { name: /Remove RSVP?/i, level: 3 })).toBeInTheDocument();
// Success: Targets only the h3
```

### 📋 MANTINE MODAL TITLE STRUCTURE

```typescript
// Component code
<Modal
  title={
    <Title order={3}>Remove RSVP?</Title>  // This is <h3>
  }
>
```

**Renders as**:
```html
<h2 class="mantine-Modal-title" id="...">
  <h3 class="mantine-Title-root" data-order="3">
    Remove RSVP?
  </h3>
</h2>
```

### 🛑 BEST PRACTICES FOR MODAL TITLE TESTING

**Option 1: Use getByRole with level** (Recommended):
```typescript
screen.getByRole('heading', { name: /Modal Title/i, level: 3 })
```

**Option 2: Use data-testid on modal** (Fallback):
```typescript
screen.getByTestId('modal-name')
```

**DON'T use** `getByText` for modal titles - too ambiguous.

### 💥 CONSEQUENCES OF IGNORING

- ❌ Tests fail with "Found multiple elements" error
- ❌ Cannot reliably query modal titles
- ❌ False test failures block CI/CD
- ❌ Developer frustration debugging "working" code

### Tags
#critical #testing-library #mantine-modal #getByRole #test-queries #multiple-elements

---

## ❌ CRITICAL: MANTINE V7 TABS - HOVER STATES DON'T WORK WITH `styles` PROP

**Date**: 2025-11-09
**Context**: Email Templates Admin Page - Attempting to add hover styles to tabs
**Impact**: HIGH - Wasted significant time trying to make hover states work incorrectly

### The Problem

**Attempted approach that FAILED:**
```typescript
<Tabs
  variant="pills"
  styles={{
    tab: {
      '&:hover:not([data-active])': {
        backgroundColor: 'rgba(136, 1, 36, 0.05)',
        borderColor: 'var(--mantine-color-burgundy-6)',
      },
    },
  }}
>
```

**Why it failed:**
- Mantine v7's `styles` prop uses inline styles (Emotion/CSS-in-JS at runtime)
- **Pseudo-classes like `:hover`, `:active`, `:focus` CANNOT be used in the `styles` prop**
- This is a hard limitation documented in Mantine's official docs
- No amount of tweaking the syntax will make it work

### The Solution

**✅ Use CSS Modules with `classNames` prop:**

**Step 1: Create CSS Module file:**
```css
/* ComponentName.module.css */
.tab {
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.tab[data-active] {
  background-color: rgba(136, 1, 36, 0.05);
  border-color: var(--mantine-color-burgundy-6);
}

.tab:hover:not([data-active]) {
  background-color: rgba(136, 1, 36, 0.05);
  border-color: var(--mantine-color-burgundy-6);
}
```

**Step 2: Use `classNames` prop in component:**
```typescript
import classes from './ComponentName.module.css';

<Tabs
  variant="pills"
  classNames={{ tab: classes.tab }}
>
```

### Why CSS Modules Work

- CSS Modules generate **real CSS** at build time
- Browser can properly apply pseudo-selectors (`:hover`, `:focus`, etc.)
- Vite processes CSS Modules during build, creating actual CSS classes
- `classNames` prop maps Mantine component parts to CSS Module classes

### Alternative Solutions

**Option 2: Use `sx` prop with `@mantine/emotion` (requires installation):**
```bash
npm install @mantine/emotion
```

```typescript
<Tabs
  sx={{
    '.mantine-Tabs-tab': {
      '&:hover:not([data-active])': {
        backgroundColor: 'rgba(136, 1, 36, 0.05)',
      },
    },
  }}
>
```

**Option 3: Use `@mixin hover` (if postcss-preset-mantine configured):**
```css
.tab {
  @mixin hover {
    background-color: rgba(136, 1, 36, 0.05);
  }
}
```

### Best Practice

**✅ ALWAYS use CSS Modules for:**
- Hover states
- Focus states
- Active states
- Media queries
- Any CSS pseudo-classes or pseudo-elements

**✅ Use `styles` prop ONLY for:**
- Simple inline styles without pseudo-classes
- Dynamic styles that change based on props/state
- One-off styling that doesn't need pseudo-selectors

### Documentation References

- Official Mantine Styles API: https://mantine.dev/styles/styles-api/
- How to Add Hover Styles: https://help.mantine.dev/q/how-to-add-hover-styles
- CSS Modules in Vite: https://vitejs.dev/guide/features.html#css-modules

### Example from WitchCityRope

**File**: `/apps/web/src/pages/admin/EmailTemplatesAdminPage.tsx`
**CSS Module**: `/apps/web/src/pages/admin/EmailTemplatesAdminPage.module.css`

Successfully implemented tab hover states using CSS Modules after multiple failed attempts with `styles` prop.

### Tags
#critical #mantine-v7 #tabs #hover-states #css-modules #styles-prop #pseudo-classes #styling

---

## 🚨 CRITICAL: MOBILE MENU STRUCTURE - CLOSE BUTTON INSIDE PANEL, NOT EXCESSIVE PADDING 🚨

**Date**: 2025-11-09
**Category**: Mobile UX / Navigation / Layout Best Practices
**Severity**: CRITICAL - BREAKS MOBILE NAVIGATION UX

### What We Learned
**EXCESSIVE PADDING TO AVOID HAMBURGER BUTTON IS WRONG PATTERN**: Mobile menu panels with `paddingTop: 120px` to push content below a hamburger button create poor UX and fight against proper structure.

**SYMPTOMS**:
- Login button appears at top of page (outside mobile menu panel)
- Large gap at top of mobile menu when opened
- Content feels disconnected from close action
- Awkward layout that doesn't follow mobile UX best practices

**ROOT CAUSE**: The mobile menu Box has `position: fixed; top: 0; height: 100vh`, and the hamburger/close button is OUTSIDE this panel. Adding excessive padding to push content down fights against this structure.

### ✅ CRITICAL SOLUTION: ADD CLOSE BUTTON INSIDE PANEL HEADER

```typescript
// ❌ WRONG: Hamburger outside panel, excessive padding inside
<Box component="button" onClick={toggleMobileMenu}>
  {/* Hamburger icon - OUTSIDE mobile menu panel */}
</Box>

<Box id="mobile-menu" style={{ position: 'fixed', top: 0, height: '100vh' }}>
  <Stack gap="0" p="var(--space-lg)" style={{ paddingTop: '120px' }}>
    {/* Content starts 120px down - fighting against layout */}
    <Button>Login</Button>
  </Stack>
</Box>

// ✅ CORRECT: Close button INSIDE panel, proper header section
<Box component="button" onClick={toggleMobileMenu}>
  {/* Hamburger icon - can still animate to X visually */}
</Box>

<Box id="mobile-menu" style={{ position: 'fixed', top: 0, height: '100vh' }}>
  {/* Mobile Menu Header with Close Button */}
  <Box
    style={{
      display: 'flex',
      justifyContent: 'flex-end',
      padding: 'var(--space-md)',
      borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
    }}
  >
    <Box
      component="button"
      onClick={closeMobileMenu}
      aria-label="Close mobile menu"
      style={{
        background: 'none',
        border: 'none',
        fontSize: '32px',
        color: 'var(--color-burgundy)',
        cursor: 'pointer',
        padding: '8px',
        lineHeight: 1,
      }}
    >
      ×
    </Box>
  </Box>

  <Stack gap="0" p="var(--space-lg)">
    {/* Content starts immediately after header - natural flow */}
    <Button>Login</Button>
  </Stack>
</Box>
```

### 🛑 MOBILE MENU BEST PRACTICES

**ALWAYS include these elements in mobile menu panels:**
1. **Dedicated header section** inside the panel (at top)
2. **Close button (×)** in top-right of header
3. **Visual separator** below header (border or shadow)
4. **Normal padding** for content (no excessive top padding)

**WHY this is better:**
- Close button is INSIDE the thing you want to close (intuitive UX)
- Clear visual hierarchy (header → content)
- No need for excessive padding hacks
- Follows mobile menu patterns from iOS, Android, and web apps
- Responsive to different screen sizes naturally

### 📋 STRUCTURAL PATTERN

```typescript
// Complete mobile menu structure
<Box className="mobile-menu" style={{ position: 'fixed', top: 0, height: '100vh' }}>
  {/* 1. HEADER SECTION - Inside panel */}
  <Box style={{ display: 'flex', justifyContent: 'flex-end', padding: 'md', borderBottom: '1px solid' }}>
    <button onClick={closeMenu} aria-label="Close mobile menu">×</button>
  </Box>

  {/* 2. CONTENT SECTION - Normal padding */}
  <Stack gap="0" p="lg">
    <Button>Primary CTA (Login/Dashboard)</Button>
    <Link>Navigation Item 1</Link>
    <Link>Navigation Item 2</Link>
  </Stack>
</Box>
```

### 💥 CONSEQUENCES OF EXCESSIVE PADDING APPROACH

- ❌ Login button appears disconnected from menu
- ❌ Wasted vertical space (120px of nothing)
- ❌ Confusing UX (close button not in panel)
- ❌ Doesn't scale well across screen sizes
- ❌ Violates mobile UX conventions

### 🎯 VERIFICATION STEPS

After fix:
1. **Open mobile menu** on mobile viewport (375px)
2. **Verify close button (×)** appears at top-right INSIDE panel
3. **Verify Login button** appears immediately below header
4. **Verify no excessive gap** at top of menu
5. **Click close button** - menu closes smoothly
6. **Compare to iOS/Android patterns** - should feel familiar

### Tags
#critical #mobile-menu #navigation #mobile-ux #layout #best-practices #close-button #panel-structure

---

## 🚨 CRITICAL: MOBILE MENU OVERFLOW BREAKS SCROLL RESTORATION 🚨

**Date**: 2025-11-13
**Category**: React Router / Scroll Restoration / Mobile Navigation
**Severity**: CRITICAL - BREAKS MOBILE NAVIGATION UX

### What We Learned
**SCROLL-TO-TOP NOT WORKING ON MOBILE**: When users navigate using mobile menu links, React Router's `ScrollRestoration` component fails to scroll to top because `document.body.style.overflow = 'hidden'` is still applied.

**ROOT CAUSE**: The Navigation component sets `document.body.style.overflow = 'hidden'` when mobile menu opens to prevent background scrolling. When a navigation link is clicked:
1. `closeMobileMenu()` is called → sets `isMobileMenuOpen = false`
2. React Router navigation begins → `ScrollRestoration` tries to scroll to top
3. **useEffect cleanup runs AFTER navigation** → body overflow is still 'hidden'
4. Scroll-to-top fails silently because body cannot scroll

### 🛑 BROKEN PATTERN:

```typescript
// ❌ WRONG: Body overflow reset happens too late (in useEffect)
const closeMobileMenu = useCallback(() => {
  setIsMobileMenuOpen(false)
  // Body overflow is NOT reset here!
}, [])

useEffect(() => {
  if (isMobileMenuOpen) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''  // This runs AFTER navigation!
  }

  return () => {
    document.body.style.overflow = ''  // Cleanup runs AFTER navigation!
  }
}, [isMobileMenuOpen])
```

**Why This Breaks**:
1. User clicks mobile menu link
2. `closeMobileMenu()` called → `isMobileMenuOpen = false`
3. React Router navigation triggered immediately
4. `ScrollRestoration` tries to scroll to top → **FAILS** (body overflow still 'hidden')
5. `useEffect` cleanup runs after navigation → resets overflow (too late!)
6. User lands on new page without scrolling to top

### ✅ CRITICAL SOLUTION: Synchronous Overflow Reset

```typescript
// ✅ CORRECT: Reset body overflow IMMEDIATELY in closeMobileMenu
const closeMobileMenu = useCallback(() => {
  setIsMobileMenuOpen(false)
  // CRITICAL: Immediately reset body overflow to allow scroll restoration
  // This must happen synchronously, not in useEffect cleanup, to ensure
  // React Router's ScrollRestoration can scroll to top on navigation
  document.body.style.overflow = ''
}, [])

useEffect(() => {
  if (isMobileMenuOpen) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''
  }

  // Cleanup: Reset body overflow when component unmounts or menu state changes
  // Note: closeMobileMenu() also resets overflow synchronously for navigation
  return () => {
    document.body.style.overflow = ''
  }
}, [isMobileMenuOpen])
```

### 📋 WHY SYNCHRONOUS RESET IS CRITICAL:

**Synchronous** (happens immediately in same call stack):
- ✅ Body overflow reset BEFORE React Router navigation
- ✅ `ScrollRestoration` can scroll when it runs
- ✅ User scrolls to top on new page

**Asynchronous** (happens in useEffect after state change):
- ❌ Navigation happens first
- ❌ `ScrollRestoration` runs while body overflow still 'hidden'
- ❌ Scroll-to-top fails silently
- ❌ User lands on new page at wrong scroll position

### 🎯 PATTERN FOR MOBILE MENUS WITH BODY SCROLL LOCK:

Whenever you prevent body scrolling with mobile menus:

1. **Set overflow in useEffect** when menu opens/closes
2. **Reset overflow SYNCHRONOUSLY** in close handler before navigation
3. **Keep useEffect cleanup** for unmount edge cases
4. **Test navigation** on mobile viewports to verify scroll restoration

```typescript
// Complete pattern
const closeMobileMenu = useCallback(() => {
  setIsMobileMenuOpen(false)
  document.body.style.overflow = ''  // SYNC reset for navigation
}, [])

useEffect(() => {
  if (isMobileMenuOpen) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''
  }

  return () => {
    document.body.style.overflow = ''  // Cleanup for unmount
  }
}, [isMobileMenuOpen])

// In JSX:
<Link to="/events" onClick={closeMobileMenu}>Events</Link>
```

### 🚨 RELATED ISSUES TO CHECK:

If scroll restoration is broken, check for:
- ✅ Body overflow management in mobile menus
- ✅ Modal/drawer components that lock body scroll
- ✅ Fullscreen overlays that disable scrolling
- ✅ Any code that modifies `document.body.style.overflow`

All these cases need **synchronous cleanup** before navigation.

### 💥 CONSEQUENCES OF IGNORING:

- ❌ Mobile users never scroll to top on navigation
- ❌ Confusing UX (new page appears mid-scroll)
- ❌ Users must manually scroll to top
- ❌ Poor perceived performance
- ❌ Accessibility issues (users may not see page title/header)

### 🔧 DEBUGGING CHECKLIST:

If scroll restoration fails on mobile:
1. **Check browser DevTools** → Inspect `document.body` style attribute
2. **Add console.log** in `closeMobileMenu` to verify overflow reset
3. **Test navigation timing** → Does overflow reset before or after navigation?
4. **Verify ScrollRestoration** is present in RootLayout
5. **Check for other overflow modifiers** in codebase

### Tags
#critical #scroll-restoration #mobile-navigation #react-router #body-overflow #synchronous-cleanup #mobile-ux

---

## 🚨 CRITICAL: GLOBAL CSS SOLUTION FOR MANTINE TEXT COMPONENT MARGIN ISSUES

**Date**: 2025-11-13
**Category**: CSS / Mantine Components / Layout
**Severity**: CRITICAL - PREVENTS INLINE STYLE PROLIFERATION

### What We Learned
**INLINE m={0} PROPS ARE NOT SCALABLE**: Adding `m={0}` (or even `m={0} !important`) to individual `<Text>` components throughout the app to fix alignment issues is a maintenance nightmare. The global CSS solution fixes the issue application-wide without touching individual components.

**ROOT CAUSE**: Mantine's `<Text>` component renders as a `<p>` tag by default, which has browser default margins (typically 1em top and bottom) that cause alignment issues in:
- Breadcrumbs (text doesn't align with links)
- Group components with icons (icon and text misalign vertically)
- Inline contexts (text has unwanted vertical spacing)

### 🛑 VIOLATION PATTERN - DO NOT DO THIS:

```typescript
// ❌ WRONG: Inline margin fixes on every Text component
<Breadcrumbs>
  <Anchor href="/events">Events</Anchor>
  <Text m={0} c="dimmed">Event Details</Text>  {/* Inline fix */}
</Breadcrumbs>

<Group gap="xs">
  <IconCalendar size={20} />
  <Text m={0} size="lg">{formatDate(date)}</Text>  {/* Inline fix */}
</Group>

<Stack>
  <Text m={0}>Some text</Text>  {/* Inline fix */}
</Stack>
```

**Why This Is Wrong**:
- Requires adding `m={0}` to EVERY Text component on EVERY page
- Not scalable (hundreds of components to update)
- Maintenance burden (easy to forget on new components)
- Inconsistent (some developers will forget, some won't)
- Forces developers to add `!important` when default styles are strongly applied

### ✅ CRITICAL SOLUTION: GLOBAL CSS IN index.css

**File**: `/home/chad/repos/witchcityrope/apps/web/src/index.css`

```css
/* Fix Mantine Text component default p tag margins in inline contexts */
/* Breadcrumbs, Group, Stack, and inline containers should not have p tag margins */
.mantine-Breadcrumbs-root p,
.mantine-Group-root p,
.mantine-Stack-root > p,
.mantine-Text-root {
  margin: 0;
  padding: 0;
}

/* Preserve paragraph spacing in content areas where it's needed */
.mantine-Paper-root > p,
.mantine-Card-root > p,
article p,
.content p {
  margin-bottom: 1rem;
}
```

**Why This Works**:
1. **Targets Mantine components globally** - No need to touch individual components
2. **Scoped to inline contexts** - Only removes margins in breadcrumbs, groups, stacks
3. **Preserves content spacing** - Keeps paragraph spacing in Paper, Card, and article contexts
4. **Zero maintenance** - One fix applies everywhere automatically
5. **Future-proof** - New Text components automatically work correctly

### 📋 IMPLEMENTATION PATTERN:

**Step 1: Add global CSS rules to index.css**
```css
/* Add to /apps/web/src/index.css */
.mantine-Breadcrumbs-root p,
.mantine-Group-root p,
.mantine-Stack-root > p,
.mantine-Text-root {
  margin: 0;
  padding: 0;
}
```

**Step 2: Remove ALL inline m={0} props**
```bash
# Search for inline margin fixes
grep -r "m={0}" apps/web/src/ --include="*.tsx"

# Remove them from components
# Before: <Text m={0} c="dimmed">...</Text>
# After:  <Text c="dimmed">...</Text>
```

**Step 3: Test alignment in all contexts**
- Breadcrumbs at mobile width (375px)
- Event header icon-text alignment
- Admin page breadcrumbs
- Any Group components with icons + text

### 🎯 SCOPING STRATEGY:

**Remove margins in inline contexts:**
- `.mantine-Breadcrumbs-root p` - Breadcrumb text items
- `.mantine-Group-root p` - Icon + text groups
- `.mantine-Stack-root > p` - Stack direct children
- `.mantine-Text-root` - All Text components globally

**Preserve margins in content contexts:**
- `.mantine-Paper-root > p` - Content within Paper components
- `.mantine-Card-root > p` - Content within Card components
- `article p` - Article content (semantic HTML)
- `.content p` - Custom content class (if used)

### 🚨 WHEN TO USE THIS APPROACH:

**Use global CSS when:**
- ✅ Issue affects multiple components across the app
- ✅ Same fix needed in many places (breadcrumbs, groups, etc.)
- ✅ Default styling causes systemic alignment issues
- ✅ Inline fixes would require hundreds of changes

**Use inline styles when:**
- ❌ Issue is specific to ONE component
- ❌ Different spacing needed in different contexts
- ❌ Override is truly exceptional, not the norm

### 💥 CONSEQUENCES OF NOT USING GLOBAL SOLUTION:

- ❌ Adding `m={0}` to hundreds of components manually
- ❌ Forgetting `m={0}` on new components (inconsistent UX)
- ❌ Maintenance burden (every new Text component needs it)
- ❌ Developer frustration (why do I need this everywhere?)
- ❌ Code bloat (inline props repeated everywhere)
- ❌ Need for `!important` hacks when defaults are strongly applied

### 🔧 VERIFICATION CHECKLIST:

After implementing global CSS fix:
1. **Check breadcrumbs** - Text aligns with links at all breakpoints
2. **Check icon groups** - Icons and text align vertically
3. **Check event headers** - Calendar/clock/location icons align with text
4. **Check content areas** - Paragraph spacing preserved in descriptions
5. **Search codebase** - Zero `m={0}` props remain in any component
6. **Test mobile** - Alignment works at 375px width
7. **Test desktop** - Alignment works at 1440px width

### 📁 FILES AFFECTED IN THIS FIX:

**Global CSS added:**
- `/home/chad/repos/witchcityrope/apps/web/src/index.css` (lines 203-219)

**Inline m={0} props removed from:**
- `/home/chad/repos/witchcityrope/apps/web/src/pages/events/EventDetailPage.tsx` (4 instances)
- `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventDetailsPage.tsx` (1 instance)
- `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminMemberDetailsPage.tsx` (1 instance)

### Tags
#critical #css #mantine #global-styles #text-component #margin-reset #scalability #maintenance

---

## ✅ REACT ROUTER V7 SCROLLRESTORATION - REPLACE CUSTOM SCROLLTOTOP COMPONENT

**Date**: 2025-11-13
**Category**: React Router v7 / Navigation / Scroll Behavior
**Severity**: IMPROVEMENT - BETTER UX WITH BUILT-IN FEATURE

### What We Learned
**REACT ROUTER V7 HAS BUILT-IN SCROLLRESTORATION**: The custom `ScrollToTop` component that always scrolls to top on navigation should be replaced with React Router v7's `ScrollRestoration` component for better browser-native behavior.

**CUSTOM SCROLLTOTOP LIMITATIONS**:
- Always scrolls to top on navigation (no distinction between new page vs back button)
- Breaks browser back/forward scroll position restoration
- Doesn't handle anchor links properly (`#section` URLs)
- Not standards-compliant with History API

**REACT ROUTER V7 SCROLLRESTORATION BENEFITS**:
- Scrolls to top on NEW navigation (clicking links, form submissions)
- Restores scroll position on browser back/forward buttons
- Handles anchor links (`#section`) automatically
- Uses browser History API properly (standards-compliant)
- Zero configuration needed - just drop it in

### ✅ MIGRATION PATTERN

**Step 1: Update RootLayout component**
```typescript
// ❌ BEFORE: Custom ScrollToTop component
import { Outlet, useLocation } from 'react-router-dom';
import { ScrollToTop } from '../ScrollToTop';

export const RootLayout: React.FC = () => {
  return (
    <Box>
      <ScrollToTop />
      <Outlet />
    </Box>
  );
};

// ✅ AFTER: React Router v7 ScrollRestoration
import { Outlet, useLocation, ScrollRestoration } from 'react-router-dom';

export const RootLayout: React.FC = () => {
  return (
    <Box>
      {/* React Router v7 scroll restoration - handles scroll to top on navigation
          and restores scroll position on browser back/forward */}
      <ScrollRestoration />
      <Outlet />
    </Box>
  );
};
```

**Step 2: Delete custom ScrollToTop component**
```bash
rm /home/chad/repos/witchcityrope/apps/web/src/components/ScrollToTop.tsx
```

**Step 3: Verify no other imports**
```bash
# Should return nothing
grep -r "ScrollToTop" apps/web/src/
```

### 🛑 BEHAVIOR COMPARISON

| Scenario | Custom ScrollToTop | ScrollRestoration |
|----------|-------------------|------------------|
| Click link to new page | Scrolls to top ✅ | Scrolls to top ✅ |
| Browser back button | Scrolls to top ❌ | Restores position ✅ |
| Browser forward button | Scrolls to top ❌ | Restores position ✅ |
| Anchor link (`#section`) | Scrolls to top ❌ | Scrolls to anchor ✅ |
| Hash change on same page | Scrolls to top ❌ | Scrolls to anchor ✅ |
| Standards compliant | Partial ⚠️ | Full ✅ |

### 📋 TESTING CHECKLIST

After migration, verify:
- [ ] Click "View Full Catalog" on homepage → Should scroll to top of events page
- [ ] Scroll down events page, click an event → Should scroll to top of event detail
- [ ] Click browser back button → Should restore scroll position on events page
- [ ] Navigate: Home → Events → Event Detail → Back → Back → Should restore positions
- [ ] Test with any anchor links (if they exist) → Should scroll to anchors

### 🎯 WHEN TO USE EACH PATTERN

**Use ScrollRestoration (RECOMMENDED)**:
- ✅ All new React Router v7 applications
- ✅ When migrating from older routing solutions
- ✅ When you want browser-native scroll behavior
- ✅ When you have anchor links in your application

**Use custom ScrollToTop (ONLY IF)**:
- ⚠️ You're on older React Router (< v6.4)
- ⚠️ You explicitly want to NEVER restore scroll position
- ⚠️ You have custom scroll behavior requirements

### 💥 CONSEQUENCES OF NOT MIGRATING

- ❌ Poor UX - back button doesn't restore scroll position
- ❌ Users have to scroll to find where they were
- ❌ Anchor links don't work properly
- ❌ Not using built-in framework features
- ❌ More custom code to maintain

### 🚨 FILES AFFECTED

**MODIFIED**:
- `/apps/web/src/components/layout/RootLayout.tsx` - Replaced ScrollToTop with ScrollRestoration

**DELETED**:
- `/apps/web/src/components/ScrollToTop.tsx` - No longer needed

### Tags
#react-router-v7 #scroll-restoration #navigation #browser-history #ux-improvement #migration #built-in-features

---

## 🚨 CRITICAL: MULTIPLE AUTHENTICATION PATTERNS CAUSE SYSTEMATIC BUGS 🚨

### ⚠️ PROBLEM: Logout failing after CSRF implementation, architecture confusion causing wasted effort
**DISCOVERED**: 2025-11-23 - User clicks logout, appears logged out briefly, page refreshes, user still logged in
**ROOT CAUSE**: Three different authentication patterns active simultaneously causing confusion and bugs

### 🛑 ROOT CAUSE ANALYSIS:

**THE DISASTER**: Found 3 completely different authentication patterns in use:

1. **Pattern A - TanStack Query Mutations** (MODERN, RECOMMENDED):
   - **Location**: `/apps/web/src/features/auth/api/mutations.ts`
   - **Used For**: Login and Register forms
   - **Why**: Industry standard, automatic loading/error states, good developer experience
   - **Status**: ✅ CORRECT - Should be used everywhere

2. **Pattern B - React Context + authService** (LEGACY, PROBLEMATIC):
   - **Location**: `/apps/web/src/contexts/AuthContext.tsx` + `/apps/web/src/services/authService.ts`
   - **Used For**: Logout operation only
   - **Why**: Historical - left over from earlier implementation
   - **Status**: ❌ DELETED - Replaced with Pattern A

3. **Pattern C - Duplicate Hooks** (DEAD CODE):
   - **Location**: `/apps/web/src/lib/api/hooks/useAuth.ts`
   - **Content**: 6 duplicate auth hooks (useLogin, useLogout, useRegister, etc.)
   - **Used For**: NOTHING - dead code
   - **Status**: ❌ DELETED - Unused duplication

### 💥 HOW THIS CAUSED THE BUG:

**The Logout Bug Timeline**:

1. **November 2025**: CSRF protection rolled out to ~38 backend endpoints
2. **Logout endpoint requires CSRF token** for security
3. **Frontend logout didn't send CSRF token** - but which file do we update?
4. **Architecture confusion**:
   - Login uses Pattern A (TanStack Query mutations)
   - Logout uses Pattern B (AuthContext + authService)
   - Pattern C has duplicate useLogout hook (never used)
5. **Wrong file updated first**: Updated `/features/auth/api/mutations.ts` useLogout() - not actually used!
6. **Bug persisted**: Logout still failing because real logout uses Pattern B
7. **Correct file found**: Updated `/services/authService.ts` logout() - actually works!
8. **Bug fixed BUT** only because we accidentally updated BOTH files

**WASTED EFFORT**: Should have been 1 file update, turned into 2 files + debugging + investigation

### 🔥 CONSEQUENCES OF ARCHITECTURE CONFUSION:

1. ❌ **Bugs during infrastructure changes**: CSRF rollout missed logout
2. ❌ **Wasted developer time**: Updated wrong file, had to debug and fix again
3. ❌ **Maintenance nightmare**: Which pattern for new auth features?
4. ❌ **Onboarding confusion**: New developers don't know which pattern to use
5. ❌ **Code duplication**: Same functionality implemented 3 different ways
6. ❌ **Testing complexity**: Need to test 3 different code paths for same feature

### ✅ SOLUTION - STANDARD AUTHENTICATION PATTERN:

**DECISION**: After comprehensive research (November 2025), adopted single standard pattern
**RESEARCH**: `/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md`
**GUIDE**: `/docs/standards-processes/frontend/authentication-pattern-guide.md`

**OFFICIAL PATTERN**: TanStack Query Mutations + Zustand Store

#### Reading Auth State:
```typescript
import { useUser, useIsAuthenticated } from '@/stores/authStore'

const user = useUser()                    // Get current user data
const isAuthenticated = useIsAuthenticated() // Check if logged in
```

#### Auth Operations (Login, Logout, Register):
```typescript
import { useLogin, useLogout, useRegister } from '@/features/auth/api/mutations'

// In component
const loginMutation = useLogin()
const logoutMutation = useLogout()
const registerMutation = useRegister()

// Usage
loginMutation.mutate({ email, password })
logoutMutation.mutate()
registerMutation.mutate({ email, password, sceneName })

// Automatic loading states
{loginMutation.isPending && <Spinner />}

// Automatic error handling
{loginMutation.error && <Alert>{loginMutation.error.message}</Alert>}
```

#### Getting Server-Verified User Data:
```typescript
import { useCurrentUser } from '@/lib/api/hooks/useAuth'

const { data: currentUser } = useCurrentUser()
```

### 🗑️ WHAT WAS DELETED:

**Obsolete Files Removed** (November 23, 2025):
1. ❌ `/apps/web/src/contexts/AuthContext.tsx` - React Context pattern
2. ❌ `/apps/web/src/services/authService.ts` - Direct fetch calls
3. ❌ `/apps/web/src/hooks/useAuth.ts` - Context wrapper hook
4. ❌ `/apps/web/src/examples/LoginFormExample.tsx` - Old example code

**Components Updated**:
1. ✅ `/apps/web/src/components/layout/UtilityBar.tsx` - Now uses `useLogout()` mutation
2. ✅ `/apps/web/src/components/layout/Navigation.tsx` - Now uses `useLogout()` mutation
3. ✅ `/apps/web/src/main.tsx` - Removed `<AuthProvider>` wrapper
4. ✅ `/apps/web/src/test/integration/msw-verification.test.ts` - Uses api client directly

### 📋 CORRECT LOGOUT IMPLEMENTATION:

**File**: `/apps/web/src/features/auth/api/mutations.ts`

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '@/lib/api/client'  // SINGLE canonical API client
import { useAuthActions } from '@/stores/authStore'
import { getCSRFToken, initializeCSRFProtection } from '@/hooks/useCSRFToken'

export function useLogout() {
  const queryClient = useQueryClient()
  const { logout } = useAuthActions()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: async () => {
      // CSRF token handling with automatic retry
      let csrfToken = getCSRFToken()
      if (!csrfToken) {
        await initializeCSRFProtection()
        csrfToken = getCSRFToken()
      }

      // Call logout endpoint (CSRF token sent via interceptor)
      try {
        await apiClient.post('/api/auth/logout')
      } catch (error: any) {
        // If CSRF validation failed, retry with fresh token
        if (error.response?.status === 400 &&
            error.response?.data?.title === 'CSRF Validation Failed') {
          await initializeCSRFProtection()
          await apiClient.post('/api/auth/logout') // Retry once
        } else {
          throw error
        }
      }
    },
    onSuccess: () => {
      // 1. Clear Zustand auth store
      logout()

      // 2. Clear sessionStorage (Zustand persistence)
      sessionStorage.removeItem('auth-store')

      // 3. CRITICAL: Use clear() NOT invalidateQueries()
      // clear() prevents refetch after logout (invalidateQueries triggers refetch = bug)
      queryClient.clear()

      // 4. Navigate to home page
      navigate('/', { replace: true })

      console.log('✅ Logout successful')
    },
    onError: (error) => {
      console.error('❌ Logout failed:', error)
      // CRITICAL: Still clear local state even on error
      // Backend logout may have succeeded despite error response
      logout()
      sessionStorage.removeItem('auth-store')
      queryClient.clear()
      navigate('/', { replace: true })
    },
    retry: false,
  })
}
```

**Usage in Component**:
```typescript
import { useLogout } from '@/features/auth/api/mutations'

export const UserMenu = () => {
  const logoutMutation = useLogout()

  return (
    <button
      onClick={() => logoutMutation.mutate()}
      disabled={logoutMutation.isPending}
    >
      {logoutMutation.isPending ? 'Logging out...' : 'Logout'}
    </button>
  )
}
```

### ⚠️ CRITICAL: queryClient.clear() vs invalidateQueries()

**MUST use `clear()` on logout, NOT `invalidateQueries()`**:

```typescript
// ✅ CORRECT - Clears cache without triggering refetch
onSuccess: () => {
  logout()
  sessionStorage.removeItem('auth-store')
  queryClient.clear() // ← CRITICAL
  navigate('/')
}

// ❌ WRONG - Triggers refetch while user is logged out (causes errors)
onSuccess: () => {
  logout()
  sessionStorage.removeItem('auth-store')
  queryClient.invalidateQueries({ queryKey: ['user'] }) // ← BUG
  navigate('/')
}
```

**Why**: `invalidateQueries()` marks queries as stale and triggers refetch. After logout, user is not authenticated, so refetch fails with 401 errors. Use `clear()` to remove queries from cache without triggering refetch.

### 🚫 NEVER CREATE NEW AUTH PATTERNS:

**❌ DON'T DO THIS** (creating new auth patterns):
```typescript
// ❌ WRONG - New React Context for auth
export const MyAuthContext = createContext()

// ❌ WRONG - New service file for auth
export const myAuthService = {
  login: async () => { /* ... */ }
}

// ❌ WRONG - New custom hook wrapping fetch
export const useMyAuth = () => {
  const [user, setUser] = useState(null)
  // Custom auth logic...
}
```

**✅ DO THIS** (use standard pattern):
```typescript
// ✅ CORRECT - Use existing mutations
import { useLogin, useLogout } from '@/features/auth/api/mutations'
import { useUser, useIsAuthenticated } from '@/stores/authStore'

// ✅ CORRECT - Use existing Zustand store
const user = useUser()
const isAuthenticated = useIsAuthenticated()

// ✅ CORRECT - Use existing mutations
const loginMutation = useLogin()
const logoutMutation = useLogout()
```

### 📋 PREVENTION CHECKLIST:

**Before implementing ANY authentication feature:**
- [ ] **Read authentication pattern guide FIRST**: `/docs/standards-processes/frontend/authentication-pattern-guide.md`
- [ ] **Check if mutation already exists**: Look in `/features/auth/api/mutations.ts`
- [ ] **Use Zustand for state**: Import from `@/stores/authStore`
- [ ] **Never create new auth patterns**: Use existing standard pattern
- [ ] **CSRF tokens handled automatically**: Axios interceptor adds them
- [ ] **Use `queryClient.clear()` on logout**: NOT `invalidateQueries()`
- [ ] **Test with Playwright**: Verify complete login/logout flow works

**For Auth State Management:**
- [ ] **Read state**: Use `useUser()`, `useIsAuthenticated()` from Zustand
- [ ] **Change state**: Use mutations from `/features/auth/api/mutations.ts`
- [ ] **Server-verified data**: Use `useCurrentUser()` from `/lib/api/hooks/useAuth.ts`
- [ ] **NO custom hooks**: Don't create new auth hooks
- [ ] **NO React Context**: Don't create new auth contexts
- [ ] **NO service files**: Don't create new auth services

### 💥 CONSEQUENCES OF IGNORING:

1. ❌ **Infrastructure changes miss your code**: CSRF rollout missed Pattern B
2. ❌ **Bugs in production**: Logout appeared to work but didn't
3. ❌ **Wasted developer time**: Multiple fixes for same issue
4. ❌ **Maintenance nightmare**: Multiple patterns to maintain
5. ❌ **Security risks**: Inconsistent CSRF protection
6. ❌ **Failed code reviews**: Non-standard patterns rejected

### 🎯 WHY THIS PATTERN WAS CHOSEN:

**Research conducted November 2025** - evaluated 3 options:

**Option 1: TanStack Query + Zustand** (SELECTED - Score: 9.0/10):
- ✅ Industry standard for React apps in 2025
- ✅ Recommended by Microsoft for .NET + React
- ✅ Automatic loading/error states
- ✅ Optimized caching and performance
- ✅ Best developer experience
- ✅ Strong TypeScript support
- ✅ Active community support

**Option 2: React Query + Context** (Rejected - Score: 7.5/10):
- ⚠️ Mixing patterns (Query for data, Context for state)
- ⚠️ Manual state synchronization required
- ⚠️ Less performant than Zustand

**Option 3: Redux Toolkit + RTK Query** (Rejected - Score: 7.0/10):
- ⚠️ Heavyweight for auth-only use case
- ⚠️ Steep learning curve
- ⚠️ More boilerplate code

**Full Research**: See `/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md`

### 🚨 FILES AFFECTED:

**DELETED** (Obsolete patterns):
- `/apps/web/src/contexts/AuthContext.tsx`
- `/apps/web/src/services/authService.ts`
- `/apps/web/src/hooks/useAuth.ts`
- `/apps/web/src/examples/LoginFormExample.tsx`

**UPDATED** (Migrated to standard):
- `/apps/web/src/features/auth/api/mutations.ts` - Added complete useLogout() (lines 182-246)
- `/apps/web/src/components/layout/UtilityBar.tsx` - Uses useLogout() mutation
- `/apps/web/src/components/layout/Navigation.tsx` - Uses useLogout() mutation
- `/apps/web/src/main.tsx` - Removed AuthProvider
- `/apps/web/src/lib/api/index.ts` - Removed hooks/useAuth export
- `/apps/web/src/test/integration/msw-verification.test.ts` - Uses api client

**DOCUMENTATION CREATED**:
- `/docs/standards-processes/frontend/authentication-pattern-guide.md` - Complete developer guide
- `/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md` - Research document

### 🔗 RELATED LESSONS:

- **Backend CSRF Protection** - See backend-developer-lessons-learned-4.md for server-side implementation
- **React Router Navigation** - See lesson above about scroll restoration
- **State Management** - Zustand best practices throughout this file

### 🎓 KEY TAKEAWAYS:

1. **ONE authentication pattern** - Never create alternatives
2. **TanStack Query + Zustand** - Industry standard for 2025
3. **CSRF tokens automatic** - Axios interceptor handles them
4. **queryClient.clear() on logout** - NOT invalidateQueries()
5. **Delete obsolete code** - Don't leave old patterns around
6. **Read the guide first** - `/docs/standards-processes/frontend/authentication-pattern-guide.md`
7. **Architecture matters** - Multiple patterns = systematic bugs

**Migration completed**: November 23, 2025
**Pattern frozen**: No new auth patterns allowed
**Mandatory reading**: Authentication Pattern Guide for all auth work

### Tags
#critical #authentication #csrf #tanstack-query #zustand #architecture #technical-debt #security #logout #pattern-standardization #infrastructure #httponly-cookies #owasp

---

## 🚨 CRITICAL: NAIVE UTC TIME STORAGE - NEVER USE TIMEZONE CONVERSION FOR USER-ENTERED TIMES 🚨

**Date**: 2025-11-28
**Category**: DateTime / Timezone Handling / Event Display
**Severity**: CRITICAL - CAUSES 5-HOUR TIME DISPLAY ERRORS

### What We Learned

**USER-ENTERED TIMES ARE STORED AS "NAIVE UTC"**: When a user enters event/session times (e.g., 9:00 PM), the system stores them using `Date.UTC()` - so 9:00 PM becomes `21:00:00.000Z`. The UTC value IS the local time the user entered.

**DISPLAYING WITH TIMEZONE CONVERSION = BUG**: If you use `toLocaleTimeString()` with a timezone parameter, it will convert 21:00 UTC → 4:00 PM EST (5 hours behind). This is WRONG - user entered 9:00 PM and should see 9:00 PM.

### 🛑 TWO TYPES OF TIMES - DIFFERENT HANDLING:

| Time Type | Examples | Storage | Display Method |
|-----------|----------|---------|----------------|
| **User-entered times** | Event start/end, session times | Naive UTC (21:00Z = 9pm) | `getUTCHours()` - NO conversion |
| **System timestamps** | Check-in times, payment times, notes | True UTC (actual moment) | `toLocaleTimeString(timeZone)` - WITH conversion |

### ❌ WRONG PATTERN - DO NOT DO THIS:

```typescript
// ❌ WRONG: Using timezone conversion for event times
const formatEventTime = (dateString: string, timeZone: string): string => {
  const date = new Date(dateString);
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
    timeZone  // ← THIS CAUSES THE BUG
  });
}

// User enters 9pm → stored as 21:00Z → displays as 4:00 PM (5 hours wrong!)
```

### ✅ CORRECT PATTERN - USE THIS:

```typescript
// ✅ CORRECT: Extract UTC values directly for user-entered times
const formatStoredTime = (dateString: string): string => {
  const date = new Date(dateString);
  const hours = date.getUTCHours();    // ← Get the UTC hour (21)
  const minutes = date.getUTCMinutes(); // ← Get the UTC minute (0)

  // Format in 12-hour format
  const period = hours >= 12 ? 'PM' : 'AM';
  const hour12 = hours % 12 || 12;
  const minuteStr = minutes.toString().padStart(2, '0');

  return `${hour12}:${minuteStr} ${period}`;  // Returns "9:00 PM"
}

// User enters 9pm → stored as 21:00Z → displays as 9:00 PM (correct!)
```

### 📁 SHARED UTILITY FUNCTION LOCATION:

**Use the utility from eventUtils.ts**:
```typescript
import { formatStoredTime, formatEventTime } from '@/utils/eventUtils';

// For single time
const time = formatStoredTime(session.startTime);  // "9:00 PM"

// For time range
const range = formatEventTime(event.startDate, event.endDate);  // "6:00 PM - 9:00 PM"
```

**Source file**: `/apps/web/src/utils/eventUtils.ts`
- `formatStoredTime(dateString)` - Single time
- `formatEventTime(startDate, endDate)` - Time range

### 🚨 COMMON MISTAKES WHEN SEARCHING FOR TIME DISPLAY BUGS:

**Mistake 1: Searching only event-related folders**
- ❌ Only searching `/components/events/` and `/pages/events/`
- ✅ Search the ENTIRE `/apps/web/src/` directory for `toLocaleTimeString`

**Mistake 2: Missing duplicate component names**
- ❌ Assuming one file per component name
- ✅ Check for same component name in different locations (e.g., `/components/dashboard/` vs `/components/events/`)

**Mistake 3: Not analyzing each instance individually**
- ❌ Assuming all times in a file are the same type
- ✅ A single file can have BOTH user-entered times AND system timestamps - analyze each usage

### 📋 HOW TO IDENTIFY WHICH PATTERN TO USE:

**Use getUTCHours() (no timezone conversion) for**:
- Event start/end times
- Session start/end times
- Volunteer shift times
- Any time the USER ENTERED manually

**Use toLocaleTimeString(timeZone) for**:
- Check-in timestamps (when attendee checked in)
- Payment timestamps (when payment was made)
- Note timestamps (when note was created)
- System audit times (when records were modified)

**The key question**: "Did a human enter this time, or was it generated by the system?"
- Human entered → Naive UTC → Use `getUTCHours()`
- System generated → True UTC → Use `toLocaleTimeString(timeZone)`

### 💥 CONSEQUENCES OF IGNORING:

- ❌ Times display 5 hours wrong in US Eastern timezone
- ❌ Different times shown in different UI locations (table vs modal)
- ❌ User confusion (entered 9pm, sees 4pm)
- ❌ Time varies based on user's timezone instead of being consistent

### 🎯 VERIFICATION STEPS:

When implementing or reviewing time display code:
1. **Identify the time source** - Is it user-entered or system-generated?
2. **Check for timezone parameter** - If user-entered time has timezone param = BUG
3. **Look for `toLocaleTimeString` with timezone** - Flag for review
4. **Compare modal vs table display** - Should show SAME time
5. **Test with event that has session starting at 9pm** - Should display "9:00 PM" everywhere

### 🔗 RELATED CODE:

**Backend stores times as naive UTC in SessionFormModal.tsx**:
```typescript
// When user enters 21:00 (9pm), we store it as UTC
const startDateTime = new Date(Date.UTC(year, month, day, startHour, startMinute, 0, 0));
const sessionData = {
  startTime: startDateTime.toISOString(),  // "2025-01-15T21:00:00.000Z"
};
```

**Modal correctly displays using getUTCHours**:
```typescript
// In SessionFormModal.tsx - correctly extracts UTC time for display
const startTimeString = `${startDate.getUTCHours().toString().padStart(2, '0')}:${startDate.getUTCMinutes().toString().padStart(2, '0')}`;
```

### Tags
#critical #datetime #timezone #naive-utc #event-times #session-times #tolocalestring #getutchours #time-display #bug-fix

---

## 🚨 CRITICAL: API ERROR HANDLING STANDARDIZATION - SINGLE API CLIENT 🚨

**Date**: 2025-12-09
**Category**: API / Error Handling / Architecture Standardization
**Severity**: CRITICAL - PREVENTS DUPLICATE CODE AND INCONSISTENT ERROR HANDLING

### What We Learned

**THERE IS ONE CANONICAL API CLIENT**: `/apps/web/src/lib/api/client.ts` exports `apiClient`

The project previously had TWO API clients causing confusion:
- `/apps/web/src/api/client.ts` (DELETED - old location)
- `/apps/web/src/lib/api/client.ts` (KEPT - single source of truth)

**THE SINGLE CLIENT HAS RFC 9457 ERROR EXTRACTION BUILT-IN**: The `apiClient` response interceptor automatically extracts user-friendly error messages from RFC 9457 Problem Details responses.

### 🛑 ROOT CAUSE OF PROBLEMS:

**Before standardization**:
1. Two API clients existed with different behaviors
2. Some code imported `api` from `/api/client.ts`
3. Other code imported `apiClient` from `/lib/api/client.ts`
4. Error handling was inconsistent - some places extracted messages manually, others didn't
5. Users saw generic "Request failed with status code 400" instead of helpful messages

### ✅ STANDARD API CLIENT PATTERN:

**📖 MANDATORY READING**: `/docs/standards-processes/frontend/api-error-handling-standard.md`

**Import the single canonical client**:
```typescript
// ✅ CORRECT - Single source of truth
import { apiClient } from '@/lib/api/client'

// ❌ WRONG - Old import path (file deleted)
import { api } from '@/api/client'

// ❌ WRONG - Never use raw axios
import axios from 'axios'
```

### 🔄 ERROR MESSAGE EXTRACTION (AUTOMATIC):

The `apiClient` interceptor automatically extracts RFC 9457 error messages:

```typescript
// Backend returns RFC 9457 Problem Details:
// {
//   "type": "https://tools.ietf.org/html/rfc9457",
//   "title": "Bad Request",
//   "status": 400,
//   "detail": "Event cannot be changed to Draft status because it started more than 2 hours ago"
// }

// Without extraction (raw axios):
error.message === "Request failed with status code 400"  // ❌ Generic, unhelpful

// With apiClient interceptor:
error.message === "Event cannot be changed to Draft status because it started more than 2 hours ago"  // ✅ User-friendly
```

### 📋 CORRECT USAGE PATTERNS:

**Pattern 1: Using useApiMutation Hook (Recommended)**
```typescript
import { useApiMutation } from '@/lib/api'
import { apiClient } from '@/lib/api/client'

const createEvent = useApiMutation(
  async (data: CreateEventRequest) => {
    const response = await apiClient.post('/api/events', data)
    return response.data
  },
  {
    successMessage: 'Event created successfully',
    onSuccess: () => navigate('/events'),
  }
)
// Error notifications shown automatically with extracted message
```

**Pattern 2: Direct useMutation with Error Display**
```typescript
import { useMutation } from '@tanstack/react-query'
import { apiClient } from '@/lib/api/client'
import { notifications } from '@mantine/notifications'

const updateEvent = useMutation({
  mutationFn: async (data) => {
    const response = await apiClient.put(`/api/events/${data.id}`, data)
    return response.data
  },
  onError: (error) => {
    // error.message is ALREADY extracted - just use it directly
    notifications.show({
      title: 'Error',
      message: error.message,  // "Event cannot be changed..." from interceptor
      color: 'red',
    })
  },
})
```

### ❌ ANTI-PATTERNS - DO NOT DO THIS:

**Anti-Pattern 1: Generic Error Messages**
```typescript
// ❌ WRONG - Ignores API's helpful error message
onError: (error) => {
  notifications.show({
    message: 'An error occurred',  // Generic, unhelpful
  })
}
```

**Anti-Pattern 2: Manual Response Parsing**
```typescript
// ❌ WRONG - Duplicates interceptor logic
onError: (error) => {
  const message = error.response?.data?.detail || error.response?.data?.title || error.message
  // This is already done by the interceptor!
}
```

**Anti-Pattern 3: Using Wrong Import**
```typescript
// ❌ WRONG - Don't import from these paths
import { api } from '@/api/client'  // DELETED
import axios from 'axios'           // Never use raw axios
```

### 📁 FILES AFFECTED BY STANDARDIZATION:

**Deleted** (duplicate API client):
- `/apps/web/src/api/client.ts` - Old location, removed

**Updated to use `apiClient`** (13 files migrated):
- `/apps/web/src/features/events/api/queries.ts`
- `/apps/web/src/features/events/api/mutations.ts`
- `/apps/web/src/features/tickets/api/ticketApi.ts`
- `/apps/web/src/features/tickets/api/mutations.ts`
- `/apps/web/src/pages/admin/AdminSettingsPage.tsx`
- `/apps/web/src/pages/ApiConnectionTest.tsx`
- And more...

**New files created**:
- `/apps/web/src/lib/api/hooks/useApiMutation.ts` - Standardized mutation wrapper
- `/docs/standards-processes/frontend/api-error-handling-standard.md` - Complete guide

### 🎯 PREVENTION CHECKLIST:

**Before creating ANY API code:**
- [ ] Read API Error Handling Standard FIRST
- [ ] Import `apiClient` from `@/lib/api/client`
- [ ] NEVER import from `@/api/client` (deleted)
- [ ] NEVER use raw axios directly
- [ ] Use `error.message` directly - it's already extracted
- [ ] Consider using `useApiMutation` for automatic notifications
- [ ] Don't duplicate RFC 9457 parsing logic

**For error handling in mutations:**
- [ ] Use `error.message` directly - interceptor extracts it
- [ ] Show error in notification or UI element
- [ ] Don't use generic messages like "An error occurred"

### 💥 CONSEQUENCES OF IGNORING:

1. ❌ **Inconsistent error messages**: Some components show "Request failed with status code 400", others show actual message
2. ❌ **Duplicate code**: Multiple places parsing RFC 9457 responses
3. ❌ **Maintenance nightmare**: Bug fixes need to be applied in multiple locations
4. ❌ **Poor user experience**: Generic error messages don't help users understand what went wrong
5. ❌ **TypeScript errors**: Wrong imports cause build failures

### 🔗 RELATED DOCUMENTATION:

- **API Error Handling Standard**: `/docs/standards-processes/frontend/api-error-handling-standard.md`
- **Authentication Pattern Guide**: `/docs/standards-processes/frontend/authentication-pattern-guide.md`
- **useApiMutation Hook**: `/apps/web/src/lib/api/hooks/useApiMutation.ts`
- **API Client Source**: `/apps/web/src/lib/api/client.ts`

### Tags
#critical #api-client #error-handling #rfc9457 #standardization #single-source-of-truth #apiClient #useMutation #architecture

---
