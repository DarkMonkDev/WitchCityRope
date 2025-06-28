# Witch City Rope Style Guide Development Plan

## Overview
This document outlines the comprehensive plan for creating a brand voice guide, visual style guide, and applying consistent styling to all existing HTML wireframes.

## Phase 1: Brand Voice & Tone Guide (Priority 1)
**Timeline: 1-2 days**

### Why Brand Voice Comes First
The brand voice establishes the personality and communication style that will inform all visual design decisions. Visual elements should authentically represent the established brand personality.

### Tasks:
1. **Define Core Values** (2 hours)
   - [ ] Identify 3-5 core community values
   - [ ] Write value statements with examples
   - [ ] Align with existing safety and education focus

2. **Create Brand Voice Attributes** (3 hours)
   - [ ] Select 5 key personality traits (suggested: Knowledgeable, Inclusive, Respectful, Supportive, Responsible)
   - [ ] Write descriptions for each trait
   - [ ] Create do's and don'ts for each attribute

3. **Develop Tone Matrix** (2 hours)
   - [ ] Map different tones for various scenarios:
     - Educational content (classes/workshops)
     - Safety communications
     - Community celebrations
     - Incident handling
     - Member communications

4. **Write Vocabulary Guide** (3 hours)
   - [ ] Preferred terminology for rope bondage education
   - [ ] Consent and safety language
   - [ ] Inclusive language guidelines
   - [ ] Scene name vs. legal name usage

5. **Create Content Examples** (4 hours)
   - [ ] Event descriptions
   - [ ] Safety notices
   - [ ] Welcome messages
   - [ ] Error messages
   - [ ] Form instructions

## Phase 2: Visual Style Guide Foundation (Priority 2)
**Timeline: 2-3 days**

### MCP Tools to Use:
1. **mcp-design-system-extractor** - For analyzing existing styles
2. **Design System MCP** - For managing design tokens
3. **Shadcn-vue MCP Server** - For component generation with Tailwind

### Tasks:
1. **Audit Current Styles** (4 hours)
   - [ ] Extract all colors used across wireframes
   - [ ] Document typography variations
   - [ ] Catalog spacing patterns
   - [ ] List all component variations

2. **Define Design Tokens** (6 hours)
   - [ ] **Colors:**
     - Primary: Brown (`#8B4513`)
     - Secondary: Dark Brown (`#6B3410`)
     - Status colors (success, warning, error, info)
     - Neutral grays
   - [ ] **Typography:**
     - Font scale (12px to 32px)
     - Font weights (400, 500, 600, 700)
     - Line heights
   - [ ] **Spacing:**
     - Base unit: 4px
     - Scale: 4, 8, 12, 16, 20, 24, 32, 40, 48, 60
   - [ ] **Border Radius:**
     - Small: 4px
     - Medium: 6px
     - Large: 8px
     - XL: 12px
     - Pill: 20px

3. **Create Component Library** (8 hours)
   - [ ] **Buttons:**
     - Primary, Secondary, Warning, Danger
     - Sizes: Small, Regular, Large
     - States: Default, Hover, Active, Disabled
   - [ ] **Forms:**
     - Input fields
     - Textareas
     - Select dropdowns
     - Checkboxes/Radio buttons
     - Error states
   - [ ] **Cards:**
     - Event cards
     - Member cards
     - Info cards
   - [ ] **Navigation:**
     - Main nav (guest vs. member)
     - Admin sidebar
     - Mobile menu
   - [ ] **Badges/Pills:**
     - Status badges
     - Event type pills
     - Member status
   - [ ] **Alerts:**
     - Success, Warning, Error, Info

## Phase 3: Apply Styles to Wireframes (Priority 3)
**Timeline: 3-4 days**

### Standardization Tasks:
1. **Navigation Consistency** (4 hours)
   - [ ] Standardize main navigation across all pages
   - [ ] Implement consistent user menu
   - [ ] Add mobile navigation to all pages
   - [ ] Maintain admin sidebar pattern

2. **Button Standardization** (3 hours)
   - [ ] Replace all button variations with component library buttons
   - [ ] Ensure consistent hover states
   - [ ] Apply proper sizing scale

3. **Color Consolidation** (2 hours)
   - [ ] Replace hardcoded colors with design tokens
   - [ ] Remove duplicate color values
   - [ ] Apply semantic color naming

4. **Typography Updates** (3 hours)
   - [ ] Apply consistent font scale
   - [ ] Standardize heading sizes
   - [ ] Fix line height inconsistencies

5. **Spacing Alignment** (3 hours)
   - [ ] Apply spacing scale throughout
   - [ ] Standardize padding on sections
   - [ ] Align card and container padding

6. **Component Replacement** (6 hours)
   - [ ] Replace custom components with library versions
   - [ ] Ensure consistent border radius
   - [ ] Apply standard shadow styles

## Phase 4: Create Living Style Guide (Priority 4)
**Timeline: 2 days**

### Deliverables:
1. **Style Guide Website** (8 hours)
   - [ ] Create interactive component showcase
   - [ ] Include copy/paste code examples
   - [ ] Document usage guidelines
   - [ ] Show do's and don'ts

2. **CSS Framework** (4 hours)
   - [ ] Create master CSS file with all tokens
   - [ ] Build component CSS classes
   - [ ] Create utility classes
   - [ ] Ensure mobile responsiveness

3. **Documentation** (4 hours)
   - [ ] Write implementation guide
   - [ ] Create maintenance instructions
   - [ ] Document decision rationale
   - [ ] Include accessibility notes

## Tools & Resources Needed

### MCP Servers to Install:
```bash
# Design token management
npx @modelcontextprotocol/create-server install design-system-mcp

# For component development (if using Tailwind)
npx @shadcn/vue init

# For extracting existing patterns
npm install -g mcp-design-system-extractor
```

### Additional Tools:
- VS Code with MCP support
- CSS validator
- Accessibility checker
- Color contrast analyzer

## Success Criteria
1. All wireframes use consistent navigation patterns
2. Button styles are unified across all pages
3. Color palette is reduced to defined tokens
4. Typography follows established scale
5. Components are reusable and documented
6. Brand voice is evident in all copy
7. Style guide is maintainable and extensible

## Next Immediate Steps
1. Start with Phase 1 - Brand Voice Guide
2. Install recommended MCP tools
3. Create initial design token file
4. Begin wireframe audit

## Notes
- The brand voice must be established first to inform visual decisions
- Focus on creating a sustainable system, not just fixing current issues
- Consider accessibility throughout the process
- Keep the educational and safety-focused nature of the community central to all decisions