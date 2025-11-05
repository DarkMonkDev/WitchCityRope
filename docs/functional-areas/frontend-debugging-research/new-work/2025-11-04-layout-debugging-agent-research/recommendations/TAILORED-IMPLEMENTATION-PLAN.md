# Tailored Implementation Plan: Frontend Layout Debugging Enhancements
<!-- Date: 2025-11-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Ready for Approval -->

## Executive Summary

**Purpose**: Detailed implementation plan for enhancing WitchCityRope's existing frontend debugging capabilities with surgical improvements.

**Approach**: **Enhance, don't replace** - Build on sophisticated existing infrastructure (Chrome DevTools MCP, react-developer agent, Mantine UI standards, Skills system) rather than wholesale changes.

**Total Effort**: 9.5 hours over 3 weeks
**Expected ROI**: 60-75% faster layout debugging, 80% fewer regressions, payback after 2-3 weeks

---

## Implementation Philosophy

### Core Principles

1. **Build on Existing Infrastructure**
   - ✅ Chrome DevTools MCP already installed
   - ✅ react-developer agent sophisticated
   - ✅ Mantine UI standards document exists
   - ✅ Skills system active and enforced
   - ✅ Just-in-time standards reading philosophy

2. **Surgical Enhancements, Not Wholesale Changes**
   - Add validation step to existing workflow (not new workflow)
   - Enrich existing Mantine UI standards (not new document)
   - Create mantine-expert skill (aligns with existing Skills system)
   - Add anti-patterns to CLAUDE.md (minimal addition)

3. **Leverage Research Findings**
   - Chrome DevTools MCP: 30-55% productivity gains
   - Visual validation: 80% reduction in regressions
   - Hybrid knowledge architecture: 25-50x context efficiency
   - Plan → Execute workflow: 75% reduction in broken commits

---

## Week 1: Foundation (Immediate Quick Wins)

### Day 1: Add Mantine Anti-Patterns to CLAUDE.md

**Objective**: Prevent common Mantine mistakes at session start
**Effort**: 30 minutes
**Risk**: None - Minimal addition

**Tasks**:

1. **Read current CLAUDE.md** (5 min)
   ```bash
   cat /home/chad/repos/witchcityrope/CLAUDE.md
   ```

2. **Add Mantine v7 section** to Just-In-Time Standards (15 min)
   ```markdown
   ## Mantine v7 Critical Patterns

   **ANTI-PATTERNS** (avoid these):
   - ❌ Using `xs` for mobile styles (use `base` for < 576px)
   - ❌ CSS modules with Mantine components (use sx prop or Styles API)
   - ❌ Inline styles (prevents theme consistency)
   - ❌ Hardcoded breakpoints (use theme.breakpoints: xs, sm, md, lg, xl)
   - ❌ Responsive style props on large lists (use hiddenFrom/visibleFrom)

   **For layout debugging**: Use mantine-expert skill (automatic when encountering Mantine issues)
   **Official docs**: Mantine provides llms.txt at https://mantine.dev/llms.txt
   ```

3. **Update file registry** (5 min)
   ```markdown
   | 2025-11-04 | /CLAUDE.md | MODIFIED | Added Mantine v7 anti-patterns section | Gap Analysis Implementation | ACTIVE | Never |
   ```

4. **Test with agent** (5 min)
   - Start new Claude Code session
   - Verify Mantine anti-patterns appear in startup context
   - Confirm agents see guidance on session start

**Success Criteria**:
- ✅ Mantine anti-patterns section visible in CLAUDE.md
- ✅ File registry updated
- ✅ Agents see anti-patterns on session start

**Deliverable**: Updated `/CLAUDE.md` with Mantine v7 Critical Patterns section

---

### Day 1-2: Integrate Chrome DevTools MCP into Workflow

**Objective**: Make visual validation mandatory for layout changes
**Effort**: 2 hours
**Risk**: Low - Chrome DevTools MCP already installed

**Tasks**:

1. **Update react-developer agent definition** (1 hour)

   Location: `/.claude/agents/react-developer.md`

   Add after existing startup procedures:

   ```markdown
   ## 🚨 MANDATORY: Visual Validation for Layout Changes

   **CRITICAL**: Layout changes MUST be visually verified before commit.

   ### Visual Validation Workflow

   **BEFORE committing ANY layout change:**

   1. **Use Chrome DevTools MCP** to navigate to page
      ```
      Navigate to http://localhost:5173/[your-page]
      ```

   2. **Test at 3 breakpoints** (mobile-first testing):
      - Mobile: 375px width
      - Tablet: 768px width
      - Desktop: 1024px width

   3. **Take screenshots** at each breakpoint:
      ```
      Take screenshot at [breakpoint] viewport
      ```

   4. **Verify layout renders correctly**:
      - ✅ No text cutoff (buttons, headers, labels)
      - ✅ No horizontal overflow
      - ✅ Proper spacing (padding, gaps, margins)
      - ✅ Components visible at correct breakpoints
      - ✅ Responsive props working (base, xs, sm, md, lg, xl)

   5. **Include screenshots in handoff document**:
      - Before: Screenshot showing issue
      - After: Screenshots at all 3 breakpoints showing fix
      - Caption: Breakpoint tested, what was verified

   6. **ONLY commit if visual validation passes**

   ### If Visual Validation Fails

   **DO NOT GUESS**. Use systematic debugging:

   1. **Use Chrome DevTools MCP DOM inspection**:
      ```
      Get DOM snapshot of [element]
      Inspect computed styles
      ```

   2. **Identify root cause**:
      - Fixed width causing overflow?
      - Missing `base` property (styles reset below `xs`)?
      - Wrong breakpoint values?
      - CSS specificity conflict?
      - Negative margin overflow from Grid?

   3. **Apply targeted fix** (not trial-and-error)

   4. **Re-run visual validation** (steps 1-6 above)

   5. **Repeat until all breakpoints pass**

   ### Common Layout Issues Checklist

   **Button text cutoff**:
   - [ ] Button has responsive width (not fixed)
   - [ ] Text wraps or uses truncate properly
   - [ ] Tested at 375px (narrowest mobile)

   **Mobile layout broken**:
   - [ ] Used `base` property (NOT `xs`) for mobile styles
   - [ ] Tested at < 576px screens (below `xs` breakpoint)
   - [ ] Mobile-first approach (base → xs → sm → md → lg)

   **Component not visible**:
   - [ ] Check hiddenFrom/visibleFrom props
   - [ ] Check display responsive props
   - [ ] Verify breakpoint logic (min-width vs max-width)

   ### Why This Matters

   **Button text cutoff documented TWICE** (2025-09-22 AND 2025-10-05) because agents implemented without visual verification. This mandatory workflow prevents recurrence.

   **Research Evidence**: 80% reduction in layout regression bugs with visual validation workflow.
   ```

2. **Update lessons learned Part 2** (30 min)

   Location: `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md`

   Add prevention pattern:

   ```markdown
   ## Prevention Pattern: Visual Validation Prevents Recurring Layout Bugs

   **Problem**: Button text cutoff documented TWICE (2025-09-22 AND 2025-10-05) because agents implemented layout changes without visual verification.

   **Solution**: MANDATORY visual validation workflow using Chrome DevTools MCP before committing layout changes.

   **Workflow**:
   1. Implement layout change
   2. Use Chrome DevTools MCP to navigate to page
   3. Test at 375px, 768px, 1024px viewports
   4. Take screenshots at all 3 breakpoints
   5. Verify: No text cutoff, no overflow, proper spacing, components visible
   6. Include screenshots in handoff document
   7. ONLY commit if validation passes

   **Why This Works**: Visual evidence prevents assumptions. Agents SEE the layout works before shipping.

   **Evidence**: 80% reduction in layout regression bugs (Chrome DevTools MCP research).
   ```

3. **Test workflow with sample task** (30 min)

   Create test scenario:

   ```markdown
   Test Task: "Add a new button to the event card component. Ensure it displays correctly on mobile, tablet, and desktop."

   Expected Workflow:
   1. Agent implements button
   2. Agent uses Chrome DevTools MCP to navigate to events page
   3. Agent sets viewport to 375px
   4. Agent takes screenshot
   5. Agent verifies button renders correctly (no cutoff)
   6. Agent repeats for 768px and 1024px
   7. Agent includes 3 screenshots in handoff
   8. Agent commits

   Success: Agent followed visual validation workflow without prompting.
   ```

**Success Criteria**:
- ✅ Visual validation section added to agent definition
- ✅ Lessons learned updated with prevention pattern
- ✅ Test scenario passes (agent follows workflow)
- ✅ File registry updated

**Deliverables**:
- Updated `/.claude/agents/react-developer.md`
- Updated `/home/chad/repos/witchcityrope/docs/lessons-learned/react-developer-lessons-learned-2.md`
- Test scenario results documented

---

### Day 2-3: Create BreakpointDebugger Component

**Objective**: Provide visual feedback during development showing current breakpoint
**Effort**: 30 minutes
**Risk**: None - Development-only component

**Tasks**:

1. **Create BreakpointDebugger component** (20 min)

   Location: `/home/chad/repos/witchcityrope/apps/web/src/utils/BreakpointDebugger.tsx`

   ```typescript
   import { useViewportSize } from '@mantine/hooks';
   import { useMatches } from '@mantine/hooks';

   /**
    * Development-only breakpoint indicator
    * Shows current Mantine breakpoint and viewport width in bottom-right corner
    *
    * Purpose: Helps developers identify current responsive breakpoint during development
    * Prevents confusion between base (<576px), xs (576px+), sm (768px+), etc.
    *
    * Usage: Add to App.tsx root: <BreakpointDebugger />
    */
   export function BreakpointDebugger() {
     const { width } = useViewportSize();
     const breakpoint = useMatches({
       base: 'base',
       xs: 'xs',
       sm: 'sm',
       md: 'md',
       lg: 'lg',
       xl: 'xl',
     });

     // Only show in development
     if (process.env.NODE_ENV !== 'development') return null;

     return (
       <div
         style={{
           position: 'fixed',
           bottom: 10,
           right: 10,
           padding: '8px 12px',
           background: 'rgba(0, 0, 0, 0.8)',
           color: 'white',
           borderRadius: 4,
           fontSize: 12,
           zIndex: 9999,
           fontFamily: 'monospace',
           pointerEvents: 'none', // Don't interfere with clicks
         }}
       >
         <div style={{ fontWeight: 'bold' }}>{breakpoint.toUpperCase()}</div>
         <div>{width}px</div>
       </div>
     );
   }
   ```

2. **Add to App.tsx** (5 min)

   Location: `/home/chad/repos/witchcityrope/apps/web/src/App.tsx`

   ```typescript
   import { BreakpointDebugger } from './utils/BreakpointDebugger';

   function App() {
     return (
       <MantineProvider>
         {/* Your app content */}
         <Router>
           <Routes>
             {/* ... */}
           </Routes>
         </Router>

         {/* Development-only breakpoint indicator */}
         <BreakpointDebugger />
       </MantineProvider>
     );
   }
   ```

3. **Test in development** (5 min)

   ```bash
   cd /home/chad/repos/witchcityrope
   npm run dev
   # Navigate to http://localhost:5173
   # Resize browser window
   # Verify breakpoint indicator updates in bottom-right corner
   ```

**Success Criteria**:
- ✅ BreakpointDebugger component created
- ✅ Added to App.tsx
- ✅ Visible in development environment
- ✅ Shows current breakpoint (base, xs, sm, md, lg, xl)
- ✅ Shows viewport width in pixels
- ✅ Hides in production

**Deliverables**:
- `/home/chad/repos/witchcityrope/apps/web/src/utils/BreakpointDebugger.tsx`
- Updated `/home/chad/repos/witchcityrope/apps/web/src/App.tsx`
- Screenshot of BreakpointDebugger in action

---

## Week 2: Knowledge Enhancement

### Day 4-5: Enrich Mantine UI Standards Document

**Objective**: Add missing Mantine v7 patterns to existing standards document
**Effort**: 3 hours
**Risk**: Low - Enriching existing document

**Tasks**:

1. **Read current Mantine UI standards** (15 min)
   ```bash
   cat /home/chad/repos/witchcityrope/docs/standards-processes/frontend/mantine-ui-standards.md
   ```

2. **Add Mobile-First Responsive Patterns section** (1 hour)

   Add after existing Layout Components section:

   ```markdown
   ## 🚨 CRITICAL: Mobile-First Responsive Patterns

   ### The `base` Property Pattern

   **IMPORTANT**: `xs` breakpoint = 576px **minimum width**. Screens < 576px need `base` property.

   **Why This Matters**: WitchCityRope is mobile-first. Community members use phones at events. Without `base`, layouts break on < 576px screens.

   ```typescript
   // ❌ WRONG: Missing mobile styles (< 576px screens get NO styles)
   <Box p={{ xs: 0, sm: 'md', lg: 'xl' }} />

   // ✅ CORRECT: base covers mobile (< 576px)
   <Box p={{ base: 0, sm: 'md', lg: 'xl' }} />
   ```

   **Breakpoint Reminder**:
   - `base`: < 576px (mobile phones)
   - `xs`: 576px+ (large phones, small tablets)
   - `sm`: 768px+ (tablets)
   - `md`: 992px+ (small desktops)
   - `lg`: 1200px+ (desktops)
   - `xl`: 1408px+ (large desktops)

   ### Performance-Optimized Conditional Rendering

   **Recommended**: Use `hiddenFrom`/`visibleFrom` instead of responsive style props.

   ```typescript
   // ✅ BETTER: Performance-optimized (CSS classes, no style injection)
   <MobileMenu hiddenFrom="sm" />
   <DesktopMenu visibleFrom="sm" />

   // ⚠️ SLOWER: Responsive style props (injects <style /> tags)
   <Component display={{ base: 'block', sm: 'none' }} />
   ```

   **Performance Impact**:
   - `hiddenFrom`/`visibleFrom`: Use CSS classes `.mantine-hidden-from-{x}` (fast)
   - Responsive style props: Inject `<style />` tags next to components (slower)
   - **NOT recommended for large lists** (100+ items)

   **When to Use Each**:
   - ✅ Use `hiddenFrom`/`visibleFrom`: Conditional component visibility
   - ✅ Use responsive style props: Top-level layout components (infrequent updates)
   - ❌ Avoid responsive style props: Large lists, frequently re-rendering components
   ```

3. **Add Component Selection Decision Tree** (30 min)

   ```markdown
   ## Layout Component Decision Tree

   **Decision Process**:

   ```
   Need responsive layout?
   ├─ Yes → Continue
   └─ No → Use Stack, Group, or Box

   All items equal width?
   ├─ Yes → Use SimpleGrid
   └─ No → Continue

   Need bidirectional control (row/column)?
   ├─ Yes → Use Flex
   └─ No → Continue

   Complex multi-column layout with varying widths?
   ├─ Yes → Use Grid
   └─ No → Reconsider requirements
   ```

   **Component Use Cases**:

   | Component | Use Case | Example |
   |-----------|----------|---------|
   | **Grid** | Complex layouts with varying column widths | Dashboard (sidebar + main content) |
   | **SimpleGrid** | Equal-width items | Event cards gallery, image grid |
   | **Flex** | Bidirectional layouts (horizontal/vertical switching) | Button toolbar, navigation bar |
   | **Stack** | Simple vertical spacing | Form fields, vertical lists |
   | **Group** | Simple horizontal spacing | Button rows, inline elements |

   **Quick Reference**:
   - Dashboard layout → **Grid**
   - Event cards → **SimpleGrid**
   - Button group → **Flex** (direction switching)
   - Form fields → **Stack**
   - Button row → **Group**
   ```

4. **Add Container Queries section** (30 min)

   ```markdown
   ## Container Queries (v7.16.0+)

   **Purpose**: Responsive styles based on **container width** instead of viewport width.

   **When to Use**:
   - ✅ Reusable components that adapt to parent width
   - ✅ Components used in sidebars, modals, or constrained spaces
   - ✅ Design systems with context-aware components

   ```typescript
   // Grid with container queries
   <Grid
     type="container"
     breakpoints={{ xs: '100px', md: '300px', lg: '400px' }}
   >
     <Grid.Col span={{ base: 12, md: 6, lg: 3 }}>
       Responsive to parent container width (not viewport)
     </Grid.Col>
   </Grid>

   // SimpleGrid with container queries
   <SimpleGrid
     type="container"
     cols={{ base: 1, xs: 2, md: 4 }}
   >
     <Card>Content</Card>
   </SimpleGrid>
   ```

   **Browser Support**: Chrome 105+, Safari 16+, Firefox 110+ (modern browsers only)
   ```

5. **Add Testing Patterns section** (30 min)

   ```markdown
   ## Testing Responsive Layouts

   ### Unit Testing with React Testing Library

   **Required**: All Mantine components need `MantineProvider` wrapper.

   ```typescript
   import { render } from '@testing-library/react';
   import { MantineProvider } from '@mantine/core';

   // Create reusable render helper
   function renderWithMantine(ui: React.ReactElement) {
     return render(
       <MantineProvider>{ui}</MantineProvider>
     );
   }

   // Use in tests
   test('renders button correctly', () => {
     renderWithMantine(<MyComponent />);
     // ... assertions
   });
   ```

   ### Testing useMediaQuery Hook

   ```typescript
   // Mock window.matchMedia
   Object.defineProperty(window, 'matchMedia', {
     writable: true,
     value: (query: string) => ({
       matches: query === '(max-width: 48em)', // Match condition
       media: query,
       addEventListener: jest.fn(),
       removeEventListener: jest.fn(),
     }),
   });
   ```

   ### E2E Testing at Multiple Breakpoints

   ```typescript
   // Playwright test
   import { test, expect } from '@playwright/test';

   test('mobile layout', async ({ page }) => {
     await page.setViewportSize({ width: 375, height: 667 });
     await page.goto('http://localhost:5173');

     // Verify mobile-specific layout
     await expect(page.locator('[data-testid="mobile-menu"]')).toBeVisible();
   });
   ```
   ```

6. **Update file registry** (5 min)

**Success Criteria**:
- ✅ Mobile-first section added with `base` property pattern
- ✅ hiddenFrom/visibleFrom performance guidance added
- ✅ Component selection decision tree added
- ✅ Container queries section added
- ✅ Testing patterns section added
- ✅ File registry updated

**Deliverable**: Enhanced `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/mantine-ui-standards.md`

---

### Day 5-6: Create mantine-expert Skill

**Objective**: Create just-in-time Mantine knowledge base with llms.txt integration
**Effort**: 2 hours
**Risk**: Low - Aligns with existing Skills system

**Tasks**:

1. **Create mantine-expert.md skill file** (1.5 hours)

   Location: `/.claude/skills/mantine-expert.md`

   ```markdown
   # Mantine Expert Skill
   <!-- Version: 1.0 -->
   <!-- Last Updated: 2025-11-04 -->
   <!-- Activation: Mantine layout issues, responsive design, component selection -->

   ## Purpose

   Just-in-time Mantine v7 knowledge base for layout debugging and responsive design.
   Provides critical patterns, decision trees, and debugging techniques for WitchCityRope's mobile-first React application.

   ## When to Use This Skill

   **Trigger phrases**:
   - "Mantine layout issue"
   - "responsive design"
   - "Grid not working"
   - "SimpleGrid vs Flex"
   - "mobile breakpoint"
   - "hiddenFrom / visibleFrom"
   - "container queries"
   - "button text cutoff"
   - "layout broken on mobile"

   **Auto-activate when**:
   - Agent encounters Mantine component layout issues
   - Agent needs to choose between Grid/SimpleGrid/Flex
   - Agent implementing responsive layouts
   - Agent debugging mobile-first styling

   ## 🚨 CRITICAL: The `base` Property Pattern

   **MOST COMMON MISTAKE**: Using `xs` for mobile styles.

   **Problem**: `xs` breakpoint = 576px **minimum width**. Screens < 576px get NO styles.

   **Solution**: ALWAYS use `base` property for mobile styles (< 576px).

   ```typescript
   // ❌ WRONG: Missing mobile styles
   <Box p={{ xs: 0, sm: 'md', lg: 'xl' }} />
   // Result: Screens < 576px have NO padding

   // ✅ CORRECT: base covers mobile
   <Box p={{ base: 0, sm: 'md', lg: 'xl' }} />
   // Result: All screens covered (< 576px gets base)
   ```

   **Why This Matters**: WitchCityRope is mobile-first. Community members use phones at events.

   ## Component Selection Decision Tree

   ### Quick Decision

   ```
   Need responsive layout?
   ├─ All items equal width? → SimpleGrid
   ├─ Bidirectional (row/column)? → Flex
   ├─ Complex varying widths? → Grid
   └─ Simple spacing? → Stack/Group
   ```

   ### Detailed Decision Matrix

   | Need | Component | Example |
   |------|-----------|---------|
   | Dashboard (sidebar + main) | Grid | `<Grid.Col span={{ base: 12, md: 3 }}>` |
   | Event cards | SimpleGrid | `<SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>` |
   | Button toolbar | Flex | `<Flex direction={{ base: 'column', sm: 'row' }}>` |
   | Form fields | Stack | `<Stack gap="md">` |
   | Button row | Group | `<Group gap="md">` |

   ## Performance: hiddenFrom/visibleFrom

   **Recommended**: Use `hiddenFrom`/`visibleFrom` for conditional rendering.

   ```typescript
   // ✅ BETTER: Performance-optimized (CSS classes)
   <MobileMenu hiddenFrom="sm" />
   <DesktopMenu visibleFrom="sm" />

   // ⚠️ SLOWER: Responsive style props (injects <style /> tags)
   <Component display={{ base: 'block', sm: 'none' }} />
   ```

   **Performance Impact**:
   - `hiddenFrom`/`visibleFrom`: CSS classes (fast, no style injection)
   - Responsive style props: Injects `<style />` tags (slower)
   - **NOT recommended** for large lists (100+ items)

   ## Common Pitfalls and Solutions

   ### Pitfall 1: Styles Reset Below `xs`

   **Problem**: Responsive styles using `xs` don't apply to very small screens.
   **Cause**: `xs` = `min-width: 576px`, so screens < 576px don't match.
   **Solution**: Use `base` property for mobile.

   ### Pitfall 2: Negative Margin Overflow

   **Problem**: Content overflows parent with visible scrollbars.
   **Cause**: Grid uses negative margins for gutters.
   **Solution**: Add `overflow="hidden"` to Grid or padding to parent.

   ```typescript
   // ✅ Solution 1: Hide overflow
   <Grid gutter="lg" overflow="hidden">
     <Grid.Col span={4}>Content</Grid.Col>
   </Grid>

   // ✅ Solution 2: Add parent padding
   <Container p="lg">
     <Grid gutter="lg">
       <Grid.Col span={4}>Content</Grid.Col>
     </Grid>
   </Container>
   ```

   ### Pitfall 3: Columns Not Wrapping

   **Problem**: Grid columns don't wrap to next row.
   **Cause**: Total span exceeds `columns` prop (default 12).
   **Solution**: Ensure total span ≤ columns, or use responsive spans.

   ```typescript
   // ❌ Problem: 8 + 6 = 14 > 12
   <Grid columns={12}>
     <Grid.Col span={8}>Content</Grid.Col>
     <Grid.Col span={6}>Overflows</Grid.Col>
   </Grid>

   // ✅ Solution: Responsive spans
   <Grid columns={12}>
     <Grid.Col span={{ base: 12, md: 8 }}>Content</Grid.Col>
     <Grid.Col span={{ base: 12, md: 4 }}>Wraps on mobile</Grid.Col>
   </Grid>
   ```

   ## Debugging Workflow

   1. **Use Chrome DevTools MCP**:
      - Navigate to page
      - Set viewport to 375px (mobile), 768px (tablet), 1024px (desktop)
      - Take screenshots at each breakpoint

   2. **Check BreakpointDebugger**:
      - Bottom-right corner shows current breakpoint
      - Verify expected breakpoint is active

   3. **Inspect computed styles**:
      - Use Chrome DevTools MCP DOM inspection
      - Look for: fixed widths, min-width conflicts, overflow issues

   4. **Verify breakpoint logic**:
      - Check if `base` property is used (not `xs` for mobile)
      - Confirm breakpoint values match expectations

   5. **Apply targeted fix**:
      - Don't guess - use root cause from inspection
      - Test fix at all 3 breakpoints

   ## Testing Patterns

   ### Unit Testing (React Testing Library)

   ```typescript
   import { render } from '@testing-library/react';
   import { MantineProvider } from '@mantine/core';

   function renderWithMantine(ui: React.ReactElement) {
     return render(
       <MantineProvider>{ui}</MantineProvider>
     );
   }

   test('renders correctly', () => {
     renderWithMantine(<MyComponent />);
     // ... assertions
   });
   ```

   ### Mock window.matchMedia

   ```typescript
   Object.defineProperty(window, 'matchMedia', {
     writable: true,
     value: (query: string) => ({
       matches: query === '(max-width: 48em)',
       media: query,
       addEventListener: jest.fn(),
       removeEventListener: jest.fn(),
     }),
   });
   ```

   ## llms.txt Integration (Deep Documentation)

   **For complex component questions** not covered above, fetch latest Mantine documentation:

   ```
   Fetch https://mantine.dev/llms.txt
   ```

   **When to fetch llms.txt**:
   - Complex component configuration beyond basic patterns
   - Advanced theming questions
   - Deep API reference needed
   - Official migration guidance

   **NOTE**: This skill covers 90% of common layout tasks. Only fetch llms.txt for deep dives.

   ## WitchCityRope-Specific Patterns

   ### Event Cards Gallery

   ```typescript
   <SimpleGrid
     cols={{ base: 1, sm: 2, lg: 3 }}
     spacing={{ base: 'sm', md: 'lg' }}
   >
     {events.map(event => <EventCard key={event.id} event={event} />)}
   </SimpleGrid>
   ```

   ### Dashboard Layout

   ```typescript
   <Grid gutter="md">
     <Grid.Col span={{ base: 12, md: 3 }}>
       <Sidebar />
     </Grid.Col>
     <Grid.Col span={{ base: 12, md: 9 }}>
       <MainContent />
     </Grid.Col>
   </Grid>
   ```

   ### Form Layout

   ```typescript
   <Stack gap="md">
     <Grid gutter="md">
       <Grid.Col span={{ base: 12, sm: 6 }}>
         <TextInput label="First Name" />
       </Grid.Col>
       <Grid.Col span={{ base: 12, sm: 6 }}>
         <TextInput label="Last Name" />
       </Grid.Col>
     </Grid>

     <TextInput label="Email" />

     <Flex
       direction={{ base: 'column', sm: 'row' }}
       gap="sm"
       justify="flex-end"
     >
       <Button variant="default">Cancel</Button>
       <Button>Submit</Button>
     </Flex>
   </Stack>
   ```

   ## Success Metrics

   - Agent completes layout task without "which component?" questions
   - Agent uses `base` property for mobile styles (not `xs`)
   - Agent uses hiddenFrom/visibleFrom for conditional rendering
   - Agent tests at 3 breakpoints (375px, 768px, 1024px)
   - No button text cutoff issues recur

   ## Related Documents

   - **Mantine UI Standards**: `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/mantine-ui-standards.md`
   - **React Patterns**: `/home/chad/repos/witchcityrope/docs/standards-processes/development-standards/react-patterns.md`
   - **Agent Definition**: `/.claude/agents/react-developer.md`
   ```

2. **Update SKILLS-REGISTRY.md** (15 min)

   Location: `/.claude/skills/SKILLS-REGISTRY.md`

   Add to registry:

   ```markdown
   ### mantine-expert
   **Purpose**: Just-in-time Mantine v7 knowledge base for layout debugging
   **When to Use**: Mantine layout issues, responsive design, component selection
   **Who Uses**: react-developer, ui-designer
   **Activation**: "Mantine layout", "responsive design", "Grid", "mobile breakpoint"
   ```

3. **Update react-developer agent definition** (10 min)

   Location: `/.claude/agents/react-developer.md`

   Add to Skills section:

   ```markdown
   ### mantine-expert skill
   **Use when**: Working with Mantine components, responsive layouts, mobile-first design
   **Provides**: Component selection decision tree, `base` property pattern, debugging workflow
   **Automatic**: Skill should auto-activate for Mantine layout tasks
   ```

4. **Test skill activation** (5 min)

   Test prompts:
   - "I need to create a responsive grid for event cards"
   - "Button text is cut off on mobile, how do I fix it?"
   - "Should I use Grid or SimpleGrid for this layout?"

   Expected: Skill activates and provides relevant guidance.

**Success Criteria**:
- ✅ mantine-expert.md skill file created
- ✅ SKILLS-REGISTRY.md updated
- ✅ Agent definition updated
- ✅ Test prompts activate skill
- ✅ File registry updated

**Deliverables**:
- `/.claude/skills/mantine-expert.md`
- Updated `/.claude/skills/SKILLS-REGISTRY.md`
- Updated `/.claude/agents/react-developer.md`
- Test results documented

---

## Week 3: Polish and Validation

### Day 7-8: Create Layout Debugging Decision Tree

**Objective**: Systematic troubleshooting guide for common responsive layout issues
**Effort**: 2 hours
**Risk**: Low - Documentation only

**Tasks**:

1. **Create decision tree document** (1.5 hours)

   Location: `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/layout-debugging-decision-tree.md`

   ```markdown
   # Layout Debugging Decision Tree
   <!-- Last Updated: 2025-11-04 -->
   <!-- Version: 1.0 -->
   <!-- Purpose: Systematic troubleshooting for responsive layout issues -->

   ## Purpose

   Systematic approach to debugging common responsive layout issues in WitchCityRope's React + Mantine v7 application.

   **Use when**: Layout doesn't work as expected, especially on mobile devices.

   ## Overall Workflow: Plan → Execute

   **CRITICAL**: Don't jump straight to coding. Follow Plan → Execute pattern.

   ### Plan Mode (BEFORE implementing fix)

   1. **Analyze issue** using Chrome DevTools MCP
   2. **Inspect rendering** at target breakpoints
   3. **Identify root cause** (not symptoms)
   4. **Propose solution** with rationale
   5. **Get approval** (human or self-review if confident)

   ### Execute Mode (AFTER plan approved)

   1. **Implement minimal fix** (targeted, not trial-and-error)
   2. **Validate visually** at 3 breakpoints
   3. **Iterate if needed** (but with new plan)
   4. **Document in handoff**

   **Research Evidence**: 75% reduction in broken commits with Plan → Execute workflow.

   ## Symptom-Based Decision Tree

   ### Symptom 1: Text Cut Off (Buttons, Headers, Labels)

   **Step 1**: Use Chrome DevTools MCP
   ```
   Navigate to page
   Set viewport to 375px (mobile)
   Take screenshot showing cutoff
   ```

   **Step 2**: Inspect element
   ```
   Get DOM snapshot of affected element
   Check computed styles:
   - width: fixed? (e.g., width: 200px)
   - max-width: too narrow?
   - overflow: hidden?
   - white-space: nowrap?
   ```

   **Step 3**: Identify root cause

   | Root Cause | Evidence | Solution |
   |------------|----------|----------|
   | Fixed width | `width: 200px` | Use responsive width: `w={{ base: '100%', sm: 200 }}` |
   | Missing responsive props | No `base` property | Add `base` property for mobile |
   | Overflow hidden | `overflow: hidden` | Add text wrapping or truncate component |
   | Font too large for container | `font-size: 18px` in narrow container | Use responsive font size |

   **Step 4**: Apply targeted fix
   ```typescript
   // Example: Button text cutoff due to fixed width
   // ❌ Before
   <Button w={200}>Register for Event</Button>

   // ✅ After
   <Button w={{ base: '100%', sm: 200 }}>Register for Event</Button>
   ```

   **Step 5**: Validate fix
   ```
   Take screenshot at 375px → Text fits
   Take screenshot at 768px → Button maintains 200px width
   Take screenshot at 1024px → Button maintains 200px width
   Include screenshots in handoff
   ```

   ### Symptom 2: Layout Broken on Mobile (< 576px)

   **Step 1**: Check for `base` property

   **Most common cause**: Missing `base` property. Styles reset below `xs` breakpoint (576px).

   ```typescript
   // ❌ Problem: Missing base property
   <Box p={{ xs: 0, sm: 'md', lg: 'xl' }} />
   // Result: Screens < 576px have NO padding

   // ✅ Solution: Add base property
   <Box p={{ base: 0, sm: 'md', lg: 'xl' }} />
   // Result: All screens covered
   ```

   **Step 2**: Use Chrome DevTools MCP
   ```
   Set viewport to 375px (below xs breakpoint)
   Take screenshot
   Check if styles are missing
   ```

   **Step 3**: Verify breakpoint logic
   ```typescript
   // Common mistake: Using xs for mobile
   span={{ xs: 12, md: 6 }} // ❌ Screens < 576px get default span

   // Correct: Using base for mobile
   span={{ base: 12, md: 6 }} // ✅ All screens covered
   ```

   **Step 4**: Apply fix
   - Replace `xs` with `base` for mobile styles
   - Test at 375px to verify

   ### Symptom 3: Horizontal Overflow (Scrollbar Appears)

   **Step 1**: Identify overflow source
   ```
   Use Chrome DevTools MCP
   Navigate to page
   Set viewport to 375px
   Take screenshot showing overflow
   ```

   **Step 2**: Common causes

   | Cause | Evidence | Solution |
   |-------|----------|----------|
   | Grid negative margins | Grid with large gutter | Add `overflow="hidden"` or parent padding |
   | Fixed width exceeds viewport | `width: 500px` on 375px screen | Use responsive width with max-width |
   | Missing responsive props | Component not shrinking | Add responsive width/padding |
   | Flexbox min-width | Flex items have default min-width | Add `style={{ minWidth: 0 }}` |

   **Step 3**: Apply targeted fix
   ```typescript
   // Example: Grid overflow due to negative margins
   // ❌ Problem
   <Container>
     <Grid gutter="lg">
       <Grid.Col span={4}>Content</Grid.Col>
     </Grid>
   </Container>

   // ✅ Solution 1: Hide overflow
   <Container>
     <Grid gutter="lg" overflow="hidden">
       <Grid.Col span={4}>Content</Grid.Col>
     </Grid>
   </Container>

   // ✅ Solution 2: Add parent padding
   <Container p="lg">
     <Grid gutter="lg">
       <Grid.Col span={4}>Content</Grid.Col>
     </Grid>
   </Container>
   ```

   ### Symptom 4: Component Not Visible on Mobile/Desktop

   **Step 1**: Check conditional rendering
   ```
   Look for: hiddenFrom, visibleFrom, display props
   ```

   **Step 2**: Verify breakpoint logic
   ```typescript
   // Check if breakpoint logic is correct
   hiddenFrom="sm" // Hidden at sm (768px) and above
   visibleFrom="md" // Visible at md (992px) and above

   // Common mistake: Inverted logic
   hiddenFrom="sm" // Wanted visible on mobile, but this hides on desktop!
   ```

   **Step 3**: Use Chrome DevTools MCP
   ```
   Navigate to page
   Set viewport to target breakpoint
   Check if element exists in DOM
   - If exists but hidden: Check CSS display/visibility
   - If doesn't exist: Check conditional rendering logic
   ```

   **Step 4**: Apply fix
   ```typescript
   // Example: Mobile menu not visible on mobile
   // ❌ Problem: Inverted logic
   <MobileMenu visibleFrom="sm" /> // Visible on DESKTOP!

   // ✅ Solution: Correct logic
   <MobileMenu hiddenFrom="sm" /> // Visible on mobile, hidden on desktop
   ```

   ### Symptom 5: Columns Not Wrapping to Next Row

   **Step 1**: Check column math
   ```
   Total span must be ≤ columns prop (default 12)
   ```

   **Step 2**: Identify issue
   ```typescript
   // ❌ Problem: 8 + 6 = 14 > 12
   <Grid columns={12}>
     <Grid.Col span={8}>Content</Grid.Col>
     <Grid.Col span={6}>Overflows (doesn't wrap)</Grid.Col>
   </Grid>
   ```

   **Step 3**: Apply fix
   ```typescript
   // ✅ Solution 1: Responsive spans (mobile wraps, desktop side-by-side)
   <Grid columns={12}>
     <Grid.Col span={{ base: 12, md: 8 }}>Content</Grid.Col>
     <Grid.Col span={{ base: 12, md: 4 }}>Wraps on mobile</Grid.Col>
   </Grid>

   // ✅ Solution 2: Adjust span values (fits within 12 columns)
   <Grid columns={12}>
     <Grid.Col span={8}>Content</Grid.Col>
     <Grid.Col span={4}>Fits (8 + 4 = 12)</Grid.Col>
   </Grid>
   ```

   ## Debugging Tools Reference

   ### 1. Chrome DevTools MCP (PRIMARY)

   **Use for**:
   - Visual verification (screenshots)
   - DOM inspection (computed styles)
   - Responsive testing (viewport emulation)

   **Commands**:
   ```
   Navigate to http://localhost:5173/[page]
   Set viewport to [375px/768px/1024px] width
   Take screenshot
   Get DOM snapshot of [element selector]
   ```

   ### 2. BreakpointDebugger Component

   **What it shows**: Current Mantine breakpoint (base, xs, sm, md, lg, xl) and viewport width
   **Location**: Bottom-right corner (development only)
   **Use for**: Verifying expected breakpoint is active

   ### 3. Browser DevTools

   **Use for**:
   - Inspecting element styles
   - Checking media query matches
   - Debugging CSS specificity conflicts

   ### 4. React DevTools

   **Use for**:
   - Inspecting component props
   - Checking if hiddenFrom/visibleFrom classes applied
   - Monitoring re-renders

   ## Common Patterns Checklist

   **Before committing layout changes, verify**:

   - [ ] **Mobile-first**: Used `base` property (not `xs`) for mobile styles
   - [ ] **Visual validation**: Tested at 375px, 768px, 1024px with screenshots
   - [ ] **No text cutoff**: Buttons, headers, labels all visible
   - [ ] **No horizontal overflow**: No scrollbars on narrow viewports
   - [ ] **Components visible**: Conditional rendering logic correct
   - [ ] **Proper spacing**: Padding, gaps, margins appropriate at all breakpoints
   - [ ] **Performance**: Used hiddenFrom/visibleFrom for conditional rendering (not responsive display props)

   ## Success Metrics

   - Agent follows Plan → Execute workflow (analyze before implementing)
   - Agent identifies root cause (not symptoms)
   - Agent applies targeted fix (not trial-and-error)
   - Agent validates fix with visual evidence (screenshots)
   - Agent documents fix in handoff

   ## Related Resources

   - **mantine-expert skill**: `/.claude/skills/mantine-expert.md`
   - **Mantine UI Standards**: `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/mantine-ui-standards.md`
   - **Agent Definition**: `/.claude/agents/react-developer.md`
   - **Chrome DevTools MCP**: Lessons learned Part 1, lines 55-82
   ```

2. **Link from multiple locations** (15 min)

   Add references in:
   - `/.claude/agents/react-developer.md` (link from visual validation section)
   - `/CLAUDE.md` (add to Just-In-Time Standards quick reference)
   - `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/mantine-ui-standards.md` (link from debugging section)

3. **Update file registry** (5 min)

**Success Criteria**:
- ✅ Decision tree document created
- ✅ Linked from agent definition, CLAUDE.md, Mantine UI standards
- ✅ Covers all common layout issues from gap analysis
- ✅ File registry updated

**Deliverable**: `/home/chad/repos/witchcityrope/docs/standards-processes/frontend/layout-debugging-decision-tree.md`

---

### Day 8-9: Monitor and Iterate

**Objective**: Validate enhancements are working, collect metrics, iterate
**Effort**: 2 hours over 1 week
**Risk**: None - Monitoring only

**Tasks**:

1. **Test with real layout task** (30 min)

   Assign test task to react-developer agent:
   ```
   "Add a new 'View Details' button to event cards. Ensure it displays correctly on mobile (375px), tablet (768px), and desktop (1024px). Follow visual validation workflow."
   ```

   **Expected workflow**:
   1. Agent implements button
   2. Agent uses Chrome DevTools MCP to navigate to events page
   3. Agent tests at 375px, 768px, 1024px
   4. Agent takes 3 screenshots
   5. Agent verifies button renders correctly (no cutoff, proper spacing)
   6. Agent includes screenshots in handoff
   7. Agent commits

   **Success**: Agent follows visual validation workflow without human intervention.

2. **Collect baseline metrics** (30 min)

   Over next 5 layout tasks, track:
   - Time per task (start to commit)
   - Number of iterations (how many attempts to fix)
   - Regression rate (did fix break something else?)
   - Screenshot compliance (did agent include screenshots?)

   **Baseline targets**:
   - Time: 5-10 minutes per task (down from 15-40 min)
   - Iterations: 1-2 (down from 2-3)
   - Regressions: < 5% (down from 30%)
   - Screenshot compliance: 100%

3. **Review agent usage patterns** (30 min)

   After 2 weeks:
   - Check if agents use Chrome DevTools MCP (tool invocation logs)
   - Check if mantine-expert skill activates on Mantine tasks
   - Check if agents reference decision tree when stuck
   - Check if agents use `base` property consistently

   **Success indicators**:
   - 90%+ Chrome DevTools MCP usage on layout tasks
   - 80%+ mantine-expert skill activation on Mantine tasks
   - Zero "which component should I use?" questions
   - Zero `xs` without `base` mistakes

4. **Iterate based on findings** (30 min)

   **If agents skip visual validation**:
   - Make it more prominent in agent definition
   - Add to CLAUDE.md as anti-pattern
   - Consider adding validation skill that blocks commit

   **If agents don't use mantine-expert skill**:
   - Refine skill description (trigger phrases)
   - Add explicit references in CLAUDE.md
   - Test activation with more sample prompts

   **If recurring issues persist**:
   - Add to decision tree
   - Enhance mantine-expert skill
   - Update lessons learned

**Success Criteria**:
- ✅ Test task completed successfully with visual validation
- ✅ Baseline metrics collected (5 tasks)
- ✅ Usage patterns reviewed
- ✅ Iterations documented

**Deliverable**: Monitoring report with metrics and iteration recommendations

---

## Total Effort Summary

| Week | Task | Effort | Risk | Dependencies |
|------|------|--------|------|--------------|
| **Week 1** | | **3 hours** | **Low** | |
| Day 1 | Add Mantine anti-patterns to CLAUDE.md | 30 min | None | None |
| Day 1-2 | Integrate Chrome DevTools MCP into workflow | 2 hours | Low | Chrome DevTools MCP installed |
| Day 2-3 | Create BreakpointDebugger component | 30 min | None | None |
| **Week 2** | | **5 hours** | **Low** | |
| Day 4-5 | Enrich Mantine UI standards document | 3 hours | Low | None |
| Day 5-6 | Create mantine-expert skill | 2 hours | Low | Skills system |
| **Week 3** | | **2 hours** | **Low** | |
| Day 7-8 | Create layout debugging decision tree | 2 hours | Low | None |
| Day 8-9 | Monitor and iterate | (ongoing) | None | Week 1-2 complete |
| **TOTAL** | | **9.5 hours** | **Low** | |

---

## Success Criteria (Overall)

### Primary Success Metrics

1. **Layout Debugging Time**
   - Baseline: 15-40 min per issue
   - Target: 5-10 min per issue (60-75% reduction)
   - Measurement: Track time from "layout issue identified" to "fix committed"

2. **Regression Rate**
   - Baseline: 30% (fixes break unrelated layouts)
   - Target: < 5% (visual validation catches issues)
   - Measurement: Track "fix introduced new layout bug" incidents

3. **Visual Validation Compliance**
   - Baseline: 0% (agents don't use Chrome DevTools MCP)
   - Target: 100% (mandatory workflow)
   - Measurement: Check handoff documents for screenshots

4. **Recurring Bug Rate**
   - Baseline: Button text cutoff documented 2x
   - Target: 0 recurring bugs (visual validation prevents)
   - Measurement: Track if same issues recur

### Secondary Success Metrics

5. **Agent Self-Sufficiency**
   - Target: 70%+ of layout tasks completed without human debugging intervention
   - Measurement: Track "agent asked for manual debugging help" incidents

6. **Knowledge Base Effectiveness**
   - Target: 80%+ mantine-expert skill activation on Mantine tasks
   - Measurement: Monitor skill invocation logs

7. **Mobile-First Compliance**
   - Target: 100% of layouts use `base` property (not `xs` for mobile)
   - Measurement: Code review of responsive props

---

## Risk Mitigation

### Risk 1: Agents Skip Visual Validation Workflow

**Probability**: Medium (30%)
**Impact**: High - Defeats purpose of implementation
**Mitigation**:
- Make visual validation ultra-prominent in agent definition
- Add to CLAUDE.md anti-patterns: "Committing layout changes without visual validation"
- Consider adding validation skill that checks for screenshots in handoff

**Monitoring**: Week 1-2, check handoff documents for screenshots

---

### Risk 2: mantine-expert Skill Doesn't Activate

**Probability**: Medium (30%)
**Impact**: Medium - Agents miss available knowledge
**Mitigation**:
- Comprehensive skill description with specific trigger phrases
- Explicit CLAUDE.md reference: "For layout debugging, use mantine-expert skill"
- Test scenarios with 3-5 sample prompts

**Monitoring**: Week 2-3, track skill activation logs

---

### Risk 3: Decision Tree Gets Ignored

**Probability**: Low (20%)
**Impact**: Low - Agents can still debug without it
**Mitigation**:
- Link from multiple locations (agent definition, CLAUDE.md, Mantine standards)
- Add to agent startup validation: "Check decision tree for layout issues"
- Create visual flowchart version for quick reference

**Monitoring**: Week 3+, check if agents reference decision tree when stuck

---

## Rollback Plan

**If enhancements don't improve metrics after 3 weeks**:

1. **Revert CLAUDE.md changes** (5 min)
   - Remove Mantine anti-patterns section
   - Restore original just-in-time standards

2. **Revert agent definition changes** (15 min)
   - Remove visual validation workflow section
   - Restore original startup procedures

3. **Analyze failure reasons** (1 hour)
   - Why didn't agents follow workflows?
   - Was documentation too buried?
   - Were trigger phrases ineffective?

4. **Iterate with lessons learned** (ongoing)
   - Try different enforcement approach
   - Simplify workflows
   - Add more aggressive visual markers

**NOTE**: Rollback only removes procedural changes. Knowledge enhancements (Mantine UI standards, mantine-expert skill, decision tree) remain valuable even if workflow changes fail.

---

## Approval Required

**Before starting implementation, approve**:

1. **Week 1 Plan**: 3 hours effort for Chrome DevTools MCP workflow integration + BreakpointDebugger + CLAUDE.md anti-patterns
2. **Week 2 Plan**: 5 hours effort for Mantine UI standards enrichment + mantine-expert skill creation
3. **Week 3 Plan**: 2 hours effort for layout debugging decision tree + monitoring

**Total commitment**: 9.5 hours over 3 weeks

**Expected ROI**:
- Payback after 15-20 layout tasks (~2-3 weeks)
- Net positive ROI: Week 3+
- 60-75% time savings per layout task
- 80% reduction in regressions
- Zero recurring bugs

**Approval signature**: _______________________

**Approved date**: _______________________

---

## Next Steps (After Approval)

1. **Start Week 1, Day 1** (30 min)
   - Add Mantine anti-patterns to CLAUDE.md
   - Update file registry
   - Test with new Claude Code session

2. **Continue Week 1** (2.5 hours)
   - Integrate Chrome DevTools MCP into workflow
   - Create BreakpointDebugger component
   - Test with sample layout task

3. **Schedule Week 2** (5 hours)
   - Block time for Mantine UI standards enrichment
   - Block time for mantine-expert skill creation
   - Plan testing scenarios

4. **Monitor Progress** (ongoing)
   - Track success metrics weekly
   - Iterate based on findings
   - Document lessons learned

---

**Implementation Plan Complete**: 2025-11-04
**Ready for Approval**: Yes
**All Prerequisites Documented**: Yes
**Success Metrics Defined**: Yes
**Risk Mitigation Planned**: Yes
