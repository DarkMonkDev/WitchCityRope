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
