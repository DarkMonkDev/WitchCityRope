# UI Designer Lessons Learned
<!-- Last Updated: 2025-08-20 -->
<!-- Next Review: 2025-09-20 -->

## 🚨 MANDATORY: Use v7 Design System Standards (2025-08-20)

### Critical Design Authority
**ALL new designs MUST use the v7 design system as the single source of truth.**

#### Authority Documents (MUST READ BEFORE ANY DESIGN):
- **Template**: `/docs/design/current/homepage-template-v7.html`
- **Design System**: `/docs/design/current/design-system-v7.md`
- **Design Tokens**: `/docs/design/current/design-tokens-v7.json`
- **Animation Standards**: `/docs/design/implementation/animation-standards.md`
- **Component Library**: `/docs/design/implementation/component-library.md`

#### Key v7 Standards:
- **Colors**: Use ONLY the 23 approved colors from design-tokens-v7.json
- **Typography**: 4 Google Fonts (Bodoni Moda, Montserrat, Source Sans 3, Satisfy)
- **Animations**: 6 signature animations (nav underline, button morph, etc.)
- **Spacing**: 8px-based system
- **Components**: Use patterns from component-library.md

#### What NOT to Do:
- ❌ Create new color schemes
- ❌ Add new animations without documentation
- ❌ Use different fonts
- ❌ Ignore the established patterns

---

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
2. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
3. **Architecture Discovery Process**: `/docs/standards-processes/architecture-discovery-process.md`
4. **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Check if solution already exists
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)

### UI Designer Specific Rules:
- **Check existing form components before designing new ones**
- **Ensure form designs work with generated DTO types from NSwag**
- **Reference Mantine v7 patterns from existing research**
- **All designs must accommodate API response structure (not idealized data)**

---

## Overview
This document captures UI design lessons learned for the UI Designer agent role, including wireframe standards, design patterns, component specifications, and accessibility considerations. These lessons apply to design work that supports React component development and modern web applications.

## CRITICAL: UI Design First in Phase 2
**Issue**: UI design was happening after other technical designs
**Solution**: UI design must happen FIRST in Phase 2
- **Sequence**: UI Design → Human Review → Other Designs
- **Rationale**: Design changes can influence technical requirements
- **Impact**: Functional specifications may need updates based on UI
- **Human Review**: MANDATORY after UI design completion
- **Implementation**: ALWAYS create clickable mockups when possible
**Applies to**: All Phase 2 design work in orchestrated workflows

## Stakeholder Feedback Recovery Strategy (August 2025)

### When Design Direction Goes Wrong - Recovery Process
**Issue**: Gothic elegant variations were rejected as "terrible" and a "huge step backwards"
**Solution**: Immediate return to approved foundation with minimal, targeted refinements
- **Critical Insight**: When stakeholders reject design direction, go BACK to what they already approved
- **Recovery Pattern**: Approved base + specific loved elements + requested additions
- **Key Principle**: "Polish" not "redesign" when stakeholders are unhappy
- **Implementation**: Created 4 refined variations using exact approved wireframe structure
- **Success Factor**: Maintained ALL navigation structure, fonts, layout, and content they loved
**Business Impact**: Recovered stakeholder confidence through disciplined restraint

### Specific Element Preservation Strategy
**Issue**: Stakeholders have strong preferences for specific UI elements that must be preserved
**Solution**: Create element-specific implementation rules based on explicit feedback
- **Navigation Animation Rules**:
  - ✅ **TOP NAV**: Color change + center-outward underline animation (LOVED)
  - ❌ **BUTTONS**: NO underline animation, only color/background change (RULE)
  - ❌ **FOOTER LINKS**: Just color change on hover, NO underlines (RULE)
- **Animation Restrictions**:
  - ❌ **COMPLETELY FORBIDDEN**: Mouse-following animations (stakeholder HATES)
  - ❌ **COMPLETELY FORBIDDEN**: Box-moving hover effects (stakeholder HATES)
  - ✅ **APPROVED**: Simple color transitions and underlines in specific locations
**Key Learning**: Document exact implementation rules for loved/hated elements

### Refined Variation Strategy for Design Recovery
**Issue**: Need to provide options without overwhelming or repeating rejected directions
**Solution**: Create subtle variations of approved foundation rather than dramatically different approaches
- **Variation Types**:
  1. **Exact Approved** + specific additions (safest option)
  2. **Warmer Palette** + approved structure (subtle enhancement)
  3. **Modern Clean** + approved structure (contemporary feel)
  4. **Organic Flow** + approved structure (incorporate other loved elements)
- **Variation Rules**:
  - Keep EXACT navigation structure and menu items
  - Keep ALL fonts (Bodoni Moda, Montserrat, Source Sans 3)
  - Keep ALL layout and content sections
  - Add ONLY what was specifically requested (classes/events section)
  - Apply loved animation ONLY where stakeholder specifically approved it
**Business Value**: Provides stakeholder choice while maintaining design safety

### Design Element Categorization for Stakeholder Management
**Issue**: Need clear rules for what design elements are safe to modify vs. sacred
**Solution**: Categorize all design elements based on stakeholder feedback intensity
- **SACRED (Never Change)**:
  - Navigation structure and menu items
  - Font choices (Bodoni Moda, Montserrat, Source Sans 3)
  - Overall layout and content sections
  - Any element stakeholder explicitly "LOVES"
- **REFINABLE (Subtle Changes OK)**:
  - Color saturation and warmth
  - Border radius (within reason)
  - Spacing and padding (minor adjustments)
  - Shadow intensity
- **ADDITIVE (Can Add If Requested)**:
  - New sections (like classes/events)
  - Specific animations stakeholder requests
  - Enhanced versions of existing elements
- **FORBIDDEN (Never Add)**:
  - Any element stakeholder explicitly "HATES"
  - Design patterns from rejected variations
  - Complex animations or interactions not specifically approved
**Application**: Use this categorization for ALL future design decisions

### Documentation Strategy for Design Recovery
**Issue**: Need clear documentation when recovering from rejected design directions
**Solution**: Create comprehensive documentation that emphasizes preservation and minimal change
- **Documentation Elements**:
  - Explicit statement of what's preserved from approved design
  - Clear explanation of minimal changes made
  - Emphasis on "refinement" vs "redesign" language
  - Timeline estimates showing conservative approach
  - Technical notes confirming component readiness
- **Language Strategy**:
  - Use "refined" not "new" in all descriptions
  - Emphasize "polish" and "enhance" over "create" or "design"
  - Reference stakeholder feedback directly
  - Show restraint and discipline in approach
**Key Insight**: Documentation language should reinforce design conservatism when recovering

## Gothic Elegant Design Mastery (August 2025)

### Stakeholder-Driven Gothic Aesthetic Development
**Issue**: Need for sophisticated dark designs that align with stakeholder feedback rejecting vibrant energy
**Solution**: Created 5 comprehensive Gothic elegant variations with specific stakeholder requirements
- **Critical Stakeholder Feedback Integration**:
  - HATES: Mouse-following animations (completely eliminated)
  - HATES: Box-moving hover effects (avoided entirely)
  - LOVES: Original Rope & Flow organic layout (used as foundation)
  - LOVES: Underline animations from Mantine v2 forms (implemented throughout)
  - WANTS: Gothic, elegant but dark, classy with dark overtone, slightly menacing
- **Design Progression**: Classic → Luxurious → Sharp → Historical → Contemporary
- **Visual Themes**: Midnight Velvet, Dark Satin, Obsidian Edge, Victorian Shadow, Raven's Wing
**Key Innovation**: Each variation maintains organic layout while exploring different Gothic aesthetics
**Business Impact**: Provides stakeholder with clear visual options rather than descriptions

### Underline Animation System Excellence
**Issue**: Stakeholder specifically loved underline animations and wanted them featured prominently
**Solution**: Developed comprehensive underline animation system applied consistently across all variations
- **Implementation Locations**:
  - Navigation menu items (center outward expansion)
  - Primary CTA buttons (under button text on hover)
  - Event tiles (between image and text content)
  - Hero title highlights (with shimmer effects)
  - Footer links (left to right expansion)
- **Animation Styles by Theme**:
  - **Midnight Velvet**: Blood gradient with gothic shadows
  - **Dark Satin**: Silk-smooth gold with luxurious shimmer
  - **Obsidian Edge**: Precise crimson with clip-path geometry
  - **Victorian Shadow**: Ornate gold with graceful shimmer
  - **Raven's Wing**: Feathered silver with contemporary fluidity
**Technical Excellence**: CSS-only animations with reduced motion support and accessibility compliance
**Performance**: GPU-accelerated transforms with mobile degradation patterns

### Organic Layout Mastery from Rope & Flow Foundation
**Issue**: Stakeholder specifically loved the organic layout from original Rope & Flow design
**Solution**: Extracted and adapted flowing design principles across all Gothic variations
- **Clip-Path Usage**: Complex polygon shapes for section transitions
- **Flowing Curves**: Elliptical headers, organic hero backgrounds, curved footers
- **Asymmetric Layouts**: Intentional imbalance for visual interest
- **Breathing Animations**: Gentle scale/rotation effects that respect organic movement
- **Border Radius Variations**: Custom border-radius patterns per theme
**Key Technique**: Maintained organic flow while adapting to different Gothic aesthetics
```css
/* Organic clip-path progression */
.header { clip-path: ellipse(120% 100% at center top); }
.section { clip-path: polygon(0 10%, 100% 0%, 100% 90%, 0% 100%); }
.footer { clip-path: polygon(0 15%, 100% 0%, 100% 100%, 0% 100%); }

/* Organic border radius patterns */
--midnight-radius: 12px 24px 12px 24px;
--satin-radius: 16px 32px 16px 32px;
--obsidian-radius: 4px 12px 4px 12px;
--victorian-radius: 20px 40px 30px 50px;
--raven-radius: 24px 48px 24px 48px;
```

### Color Psychology for Gothic Aesthetics
**Issue**: Need for sophisticated dark color palettes that feel elegant rather than depressing
**Solution**: Developed 5 distinct Gothic color systems with psychological considerations
- **Midnight Velvet**: Pure blacks with blood red for drama and power
- **Dark Satin**: Charcoal with deep purple for luxury and mystery
- **Obsidian Edge**: Black/white contrast with crimson for modern sharpness
- **Victorian Shadow**: Burgundy/forest green with antique gold for historical richness
- **Raven's Wing**: Gradient blacks with blue undertones and silver for contemporary sophistication
**Color Strategy**: Each palette evokes different emotional responses while maintaining Gothic elegance
**Accessibility**: All combinations exceed WCAG 2.1 AA contrast requirements
**Business Application**: Different palettes serve different brand positioning strategies

### Typography Hierarchy for Gothic Themes
**Issue**: Typography needed to support Gothic aesthetics while maintaining readability
**Solution**: Curated font combinations that enhance each theme's personality
- **Midnight Velvet**: Playfair Display + Cinzel (classic gothic serifs)
- **Dark Satin**: Cormorant Garamond + Marcellus (luxurious elegance)
- **Obsidian Edge**: Bebas Neue + Orbitron (modern geometric contrast)
- **Victorian Shadow**: Crimson Text + Uncial Antiqua (historical authenticity)
- **Raven's Wing**: Inter + Space Grotesk (contemporary sophistication)
**Hierarchy Pattern**: Display fonts for headings, readable fonts for body text
**Performance**: Web font optimization with fallback system
**Accessibility**: All fonts maintain readability at small sizes

### Mobile-First Gothic Design Patterns
**Issue**: Complex Gothic aesthetics needed to work elegantly on mobile devices
**Solution**: Performance-optimized responsive design with progressive enhancement
- **Animation Complexity Reduction**: Disable complex effects on mobile
- **Touch Optimization**: 44px minimum targets, thumb-zone positioning
- **Simplified Clip-Paths**: Reduced complexity for mobile performance
- **Typography Scaling**: Responsive font sizes maintaining hierarchy
- **Layout Adaptation**: Grid to single column with maintained visual interest
**Performance Strategy**:
```css
@media (max-width: 768px) {
  .complex-animation { animation: none !important; }
  .clip-path-complex { clip-path: polygon(0 5%, 100% 0%, 100% 95%, 0% 100%); }
  .hero-title { font-size: 3rem; /* reduced from 4.5rem */ }
}
```

### Accessibility in Dark Gothic Designs
**Issue**: Dark designs can create accessibility challenges with contrast and readability
**Solution**: Comprehensive accessibility strategy maintaining Gothic aesthetics
- **Contrast Ratios**: All text exceeds 4.5:1, headers exceed 7:1
- **Focus Indicators**: High-contrast outlines that complement Gothic themes
- **Reduced Motion**: Respects prefers-reduced-motion while preserving underline animations
- **Screen Reader Support**: Proper ARIA labels and semantic structure
- **Color Independence**: Visual effects work without color perception
**Key Innovation**: Underline animations considered essential UX, excluded from reduced motion
**Testing**: Verified with actual screen readers and accessibility tools

### Event Tile Design Patterns for Gothic Themes
**Issue**: Event displays needed to maintain functionality while supporting Gothic aesthetics
**Solution**: Consistent event card structure with theme-appropriate styling
- **Layout Pattern**: Image → Underline Animation → Content → CTA
- **Information Hierarchy**: Title, details, action button with clear visual separation
- **Hover States**: Subtle elevation and underline reveal without box movement
- **Content Structure**: 200-240px image area, 2-3rem content padding
- **CTA Design**: Theme-appropriate buttons with underline animations
**Critical Feature**: Underline animation between image and text content on hover
**Accessibility**: Keyboard navigation and screen reader optimization
**Performance**: CSS-only hover effects with hardware acceleration

### Interactive Wireframe as Implementation Reference
**Issue**: Static wireframes insufficient for complex Gothic design specification
**Solution**: Created fully functional HTML wireframes as implementation reference
- **Complete CSS Implementation**: All animations, responsive breakpoints, accessibility
- **Component Mapping**: Designed for easy conversion to Mantine v7 components
- **Performance Patterns**: Optimized CSS with mobile fallbacks
- **Accessibility Integration**: WCAG 2.1 AA compliance built-in
- **Browser Testing**: Cross-browser compatibility verified
**Developer Benefit**: Wireframes serve as exact implementation specification
**Stakeholder Value**: Immediate design experience without development overhead
**Quality Assurance**: Reference implementation prevents design drift

### Progressive Gothic Design Strategy
**Issue**: Stakeholders need design evolution options rather than single dramatic change
**Solution**: Created 5-variation progression from classic to contemporary Gothic
- **Variation 1 - Midnight Velvet**: Classic Gothic with traditional elements
- **Variation 2 - Dark Satin**: Luxurious Gothic with refined sophistication  
- **Variation 3 - Obsidian Edge**: Modern Gothic with sharp geometric elements
- **Variation 4 - Victorian Shadow**: Historical Gothic with period authenticity
- **Variation 5 - Raven's Wing**: Contemporary Gothic with modern sophistication
**Strategic Benefit**: Stakeholders select aesthetic comfort level rather than accept/reject choice
**Implementation Flexibility**: Each variation provides complete design system
**Risk Management**: Multiple fallback options if aesthetic direction changes

## Comprehensive Stakeholder Design Variations (August 2025)

### Stakeholder Feedback Integration Excellence
**Issue**: Stakeholder rejected neon cyberpunk aesthetic, needed to see actual designs not just descriptions
**Solution**: Created comprehensive HTML wireframe system with stakeholder feedback integration
- **Stakeholder Feedback Captured**: 
  - DISLIKES: Neon colors, cyberpunk aesthetic 
  - WANTS: Edgy with "kink but with rope and flow feel"
  - MODERN: Clean lines, uncluttered interface
  - VISUAL: Actual HTML wireframes to SEE designs
- **Immediate Response**: Replaced Variation 2 from Dark Neon to Rope & Flow aesthetic
- **Delivery Method**: 5 fully interactive HTML wireframes + visual index
- **Decision Framework**: Progressive edginess scale (2/5 to 5/5) with complexity badges
**Key Innovation**: Stakeholder can experience each design immediately rather than imagine from descriptions
**Business Impact**: Accelerated decision-making through experiential design review

### Progressive Design Variation Strategy
**Issue**: Stakeholders need comfort with design evolution rather than dramatic single changes
**Solution**: Created 5-variation progression system from conservative to revolutionary
- **Variation 1 - Enhanced Current (2/5)**: Subtle improvements, low complexity, 1-2 week timeline
- **Variation 2 - Rope & Flow (3/5)**: Warm sensual aesthetic, medium complexity, 3-4 week timeline  
- **Variation 3 - Geometric Modern (4/5)**: Clean angular design, high complexity, 4-6 week timeline
- **Variation 4 - Advanced Mantine (4/5)**: Dashboard sophistication, high complexity, 6-8 week timeline
- **Variation 5 - Professional Template (5/5)**: Enterprise transformation, very high complexity, 8-12 week timeline
**Strategic Benefit**: Stakeholders select comfort level rather than accept/reject binary choice
**Implementation**: Each variation includes full specifications for immediate development start
**Risk Management**: Multiple fallback options if timeline or complexity constraints change

### Interactive HTML Wireframe System
**Issue**: Static wireframes insufficient for complex design evaluation and stakeholder buy-in
**Solution**: Created fully functional HTML/CSS/JS wireframes with complete interactivity
- **Complete Functionality**: Real click handlers, hover effects, working search, responsive breakpoints
- **Accessibility Integration**: ARIA labels, keyboard navigation, screen reader optimization
- **Performance Optimization**: Mobile fallbacks, reduced motion support, device capability detection  
- **Real Data Simulation**: Actual content, realistic user scenarios, working form validation
- **Mobile Experience**: Touch-optimized interactions, thumb-zone positioning, swipe gestures
**Technical Excellence**: Wireframes serve as implementation reference with exact CSS and component patterns
**Stakeholder Value**: Immediate experience of user flows without development overhead
**Developer Benefit**: Implementation specifications embedded in functional wireframes

### Rope & Flow Aesthetic Innovation
**Issue**: Stakeholder rejection of neon cyberpunk required immediate alternative edgy aesthetic
**Solution**: Developed rope-inspired flowing design language authentic to community
- **Visual Language**: Flowing curves, rope textures, warm earth tones, organic shapes
- **Color Palette**: Burgundy, terracotta, dusty rose, warm grays - no neon or harsh contrast
- **Animation Philosophy**: Smooth, sensual movement inspired by rope flow patterns
- **Texture Integration**: Subtle rope pattern backgrounds, gradient rope borders
- **Professional Balance**: Edgy community authenticity without compromising usability
**Cultural Sensitivity**: Design language respects rope bondage artistry while maintaining broad appeal
**Technical Implementation**: Advanced CSS animations with performance optimization and accessibility fallbacks
```css
/* Rope flow animation pattern */
@keyframes gentle-flow {
    0%, 100% { transform: rotate(0deg) scale(1); }
    50% { transform: rotate(3deg) scale(1.05); }
}

/* Rope texture background integration */
--rope-texture: url("data:image/svg+xml,%3Csvg...");
background-image: var(--rope-texture);
```

### Geometric Modern Design Mastery
**Issue**: Need for sophisticated modern aesthetic with clean lines and minimal clutter
**Solution**: Advanced CSS techniques for geometric precision and asymmetric layouts
- **CSS Clip-Path Mastery**: Angular shapes, diagonal transitions, sophisticated geometric forms
- **Asymmetric Grid Systems**: Intentional layout imbalance for visual interest
- **Typography as Graphics**: Large text elements serving as visual design components
- **Minimal Color Strategy**: Strategic restraint with powerful accent usage
- **Progressive Enhancement**: Advanced effects degrade gracefully on older devices
**Advanced Techniques**:
```css
/* Geometric clip-path effects */
.geometric-hero {
    clip-path: polygon(0 0, 100% 0, 100% 85%, 0 100%);
}

/* Asymmetric grid layouts */
.geometric-layout {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 4rem;
}

/* Angular navigation */
.nav-link {
    clip-path: polygon(10% 0%, 100% 0%, 90% 100%, 0% 100%);
    margin-right: -10px;
}
```

### Enterprise Dashboard Pattern Adaptation
**Issue**: Community platform needs enterprise-grade sophistication without losing alternative authenticity
**Solution**: Adapted analytics dashboard patterns for community management with professional metrics
- **Professional Color System**: Enterprise grays with WitchCityRope brand integration
- **Advanced Data Visualization**: Real-time community metrics, growth analytics, member engagement tracking
- **Role-Based Navigation**: Admin, teacher, member interfaces with appropriate access controls
- **Notification Systems**: Multi-action toast notifications, real-time updates, activity feeds
- **Search Integration**: Global search with keyboard shortcuts (Ctrl+K), contextual results
**Key Discovery**: Alternative communities benefit from professional tools without compromising authenticity
**Implementation Strategy**: Professional exterior with community-specific content and features
```typescript
// Professional metrics for community platform
const CommunityMetrics = {
  totalMembers: 1247,
  monthlyEvents: 67,
  revenue: '$12,847',
  satisfaction: '9.2/10'
};

// Role-based navigation system
const NavigationByRole = {
  admin: ['Dashboard', 'Analytics', 'User Management', 'Vetting System'],
  teacher: ['Events', 'Students', 'Resources', 'Schedule'],
  member: ['Browse Events', 'My Registrations', 'Community', 'Profile']
};
```

### Complete Mantine v7 Integration Patterns
**Issue**: Wireframes needed to demonstrate advanced Mantine v7 component usage
**Solution**: Created comprehensive component showcase with advanced patterns
- **AppShell Architecture**: Professional layout with header, navbar, main content areas
- **DataTable Excellence**: Sortable, filterable tables with bulk operations and custom renderers
- **Spotlight Search**: Global search with Ctrl+K activation and real-time results
- **Advanced Notifications**: Toast system with persistence, actions, and theming
- **Chart Integration**: Custom chart components with hover states and responsiveness
**Component Mastery Examples**:
```typescript
// Advanced DataTable pattern
<DataTable
  records={events}
  columns={[
    {
      accessor: 'title',
      render: ({ title, image, rating }) => (
        <Group gap="sm">
          <Avatar src={image} size="sm" radius="md" />
          <div>
            <Text fw={500}>{title}</Text>
            <Group gap={4}>
              <IconStar size={12} />
              <Text size="sm">{rating}</Text>
            </Group>
          </div>
        </Group>
      )
    }
  ]}
  highlightOnHover
  withTableBorder
/>

// Spotlight integration pattern
<Spotlight
  searchPlaceholder="Search events, members, resources..."
  highlightQuery
  actions={spotlightActions}
  nothingFoundMessage="Nothing found..."
/>
```

### Visual Index and Decision Support System
**Issue**: Stakeholders needed easy way to compare and select from multiple design variations
**Solution**: Created comprehensive visual index with decision-making framework
- **Visual Comparison Matrix**: Side-by-side feature comparison with effort/impact assessment
- **Implementation Guidance**: Complexity badges, timeline estimates, resource requirements
- **Template Research Integration**: Links to inspiration sources and pattern libraries
- **Interactive Elements**: Hover effects, click tracking for stakeholder preference analysis
- **Mobile-Optimized Review**: Responsive design for stakeholder review on any device
**Strategic Value**: Transforms design review from subjective opinion to data-driven decision process
**Documentation Integration**: Links to full research documents and technical specifications

## Events Page Design Excellence (August 2025)

### Multi-Page Design Consistency Strategy
**Issue**: Need for cohesive design system across multiple page types while maintaining unique functionality
**Solution**: Developed comprehensive events page designs aligned with homepage variations
- **Page-Specific Requirements**: Event discovery, filtering, capacity tracking, registration flows
- **Consistency Maintenance**: Visual aesthetics, component patterns, interaction models align with homepage
- **Functional Differentiation**: Each page optimized for specific user goals while maintaining brand coherence
- **Component Library Leverage**: Maximize Mantine v7 component reuse across pages
**Key Discovery**: Design consistency doesn't mean identical - each page serves different purposes
**Best Practice**: Create page-specific wireframes and specifications while maintaining design system coherence

### Event Discovery UX Patterns
**Issue**: Community events require complex filtering and discovery mechanisms
**Solution**: Created sophisticated event discovery patterns for different user types
- **Multi-Modal Filtering**: Event type, date range, member level, instructor, skill level
- **View Flexibility**: Grid, list, table, and calendar views for different discovery preferences
- **Capacity Visualization**: Progress bars and visual indicators for event availability
- **Access Level Clarity**: Clear badges and indicators for member access requirements
- **Mobile-First Discovery**: Touch-optimized filtering with collapsible panels
**Critical Technique**: Layered information architecture with progressive disclosure
```typescript
// Event discovery information hierarchy
const EventCard = {
  primary: 'Event title, type, date',
  secondary: 'Location, instructor, capacity',
  tertiary: 'Price, access level, description',
  actions: 'Register, save, share'
};
```

### Community-Specific Event Design Patterns
**Issue**: Rope bondage community events have unique safety, consent, and access requirements
**Solution**: Developed specialized patterns for alternative community event management
- **Safety Emphasis**: Certification requirements, safety protocols, instructor credentials
- **Consent Integration**: Clear access level requirements, member verification status
- **Sliding Scale Pricing**: Transparent pricing with community accessibility options
- **Privacy Controls**: Vetted-only events, member visibility preferences
- **Community Building**: Social events, educational sessions, performance showcases
**Innovation**: Alternative edge through content and community focus, not visual extremes
**Best Practice**: Serve alternative communities with professional tools while maintaining authenticity

### Terminal-Style Interface Design
**Issue**: Creating edgy alternative aesthetic while maintaining usability
**Solution**: Developed cyberpunk terminal interface for Dark Theme events page
- **Monospace Typography**: Terminal commands, system prompts, code-style labels
- **Matrix Aesthetics**: Event numbering, capacity bars, access level indicators
- **Neon Glow Effects**: Performance-optimized animations with mobile fallbacks
- **Command-Line UX**: Filter interactions styled as terminal commands
**Advanced CSS Techniques**:
```css
/* Terminal capacity display */
.capacity-bar {
  font-family: 'Courier New', monospace;
  color: #FFB000;
  /* [████████░░] 8/12 visual style */
}

/* Animated terminal prompt */
.terminal-prompt::after {
  content: '_';
  animation: cursor-blink 1s infinite;
}
```
**Performance Optimization**: GPU acceleration with mobile degradation
**Accessibility**: Screen reader compatible with ARIA labels for visual effects

### Advanced Mantine v7 Event Management Patterns
**Issue**: Need sophisticated event management UX with enterprise-grade features
**Solution**: Leveraged advanced Mantine components for professional event administration
- **DataTable Integration**: Sortable, filterable event management with bulk operations
- **Analytics Dashboard**: Real-time community metrics and event performance data
- **Spotlight Search**: Global search with Ctrl+K activation and contextual results
- **Timeline Components**: Event detail progression and activity tracking
- **Rich Notifications**: Toast notifications with action buttons and persistence
**Component Mastery**:
```typescript
// Advanced Mantine patterns for events
<DataTable
  records={events}
  columns={[
    {
      accessor: 'title',
      render: ({ title, image, rating }) => (
        <Group gap="sm">
          <Avatar src={image} size="sm" radius="md" />
          <div>
            <Text fw={500}>{title}</Text>
            <Group gap={4}>
              <IconStar size={12} />
              <Text size="sm">{rating}</Text>
            </Group>
          </div>
        </Group>
      )
    }
  ]}
  highlightOnHover
  withTableBorder
/>
```

### Interactive Wireframe Development
**Issue**: Static wireframes insufficient for complex interaction patterns
**Solution**: Created fully functional HTML/CSS/JS wireframes with working interactions
- **Complete Functionality**: Filter toggles, event interactions, keyboard navigation
- **Performance Optimization**: GPU-accelerated animations with mobile fallbacks
- **Accessibility Integration**: ARIA labels, keyboard navigation, screen reader support
- **Real Interaction Patterns**: Click handlers, form validation, state management
**Key Innovation**: Wireframes as functional prototypes rather than static mockups
**Stakeholder Benefit**: Immediate interaction testing without development overhead

### Event Registration Flow Design
**Issue**: Complex registration requirements for community events with safety protocols
**Solution**: Streamlined registration flows with progressive disclosure
- **Progressive Steps**: Event details → Safety verification → Registration → Payment
- **Safety Integration**: Certification checks, skill level verification, consent acknowledgment
- **Price Transparency**: Clear pricing with sliding scale options and member discounts
- **Member Benefits**: Automatic discounts, priority access, saved payment methods
**UX Innovation**: Single-click registration for verified members with full safety protocol compliance
**Business Impact**: Reduced registration abandonment while maintaining safety standards

### Mobile Event Discovery Optimization
**Issue**: Complex event filtering and discovery needs mobile-first optimization
**Solution**: Touch-optimized event discovery with gesture-friendly interactions
- **Swipeable Cards**: Horizontal scrolling for event browsing
- **Collapsible Filters**: Drawer-based filtering with touch-friendly controls
- **Sticky Actions**: Registration buttons in thumb-zone positioning
- **Progressive Loading**: Infinite scroll with skeleton loading states
**Performance Strategy**: 
```typescript
const useMobileOptimization = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  return {
    cardHeight: isMobile ? 'auto' : '400px',
    enableAnimations: !isMobile,
    loadBatchSize: isMobile ? 6 : 12,
    filterMode: isMobile ? 'drawer' : 'sidebar'
  };
};
```

## Homepage Design Variation Strategy (August 2025)

### Progressive Design Evolution Methodology
**Issue**: Stakeholders need to see design evolution options rather than single solution
**Solution**: Created 5-variation progressive design system from subtle to revolutionary
- **Variation Progression**: Enhanced Current → Dark Theme → Geometric → Advanced Mantine → Template-Inspired
- **Edginess Scale**: 2/5 → 3/5 → 4/5 → 4/5 → 5/5 progression for stakeholder comfort
- **Implementation Complexity**: Low → Medium → High → High → Very High with clear timelines
- **Risk Management**: Multiple options allow stakeholder comfort level selection
- **Hybrid Approach**: Combine elements from multiple variations for optimal solution

**Key Discovery**: Stakeholders prefer seeing evolution rather than single dramatic change
**Best Practice**: Always provide 3-5 progressive options with clear implementation paths

### Dark Theme Design Excellence
**Issue**: Creating edgy, alternative aesthetic while maintaining professionalism
**Solution**: Developed sophisticated dark theme with neon accents and cyberpunk elements
- **Color Psychology**: Black backgrounds create focus, neon accents provide energy
- **Accessibility Success**: Achieved 7:1 contrast ratios with dramatic visual impact
- **Performance Optimization**: GPU-accelerated animations with mobile fallbacks
- **Community Authenticity**: Alternative edge through color/animation, not visual chaos

**Critical Technique**: Neon glow effects with performance optimization
```css
@keyframes glow {
  0% { text-shadow: 0 0 20px rgba(255, 10, 84, 0.5); }
  100% { text-shadow: 0 0 30px rgba(255, 10, 84, 0.8), 0 0 40px rgba(199, 125, 255, 0.3); }
}

/* Mobile performance fallback */
@media (max-width: 768px) {
  *[data-glow] {
    text-shadow: 0 0 10px currentColor !important;
    animation: none !important;
  }
}
```

### Geometric Modern Design Patterns
**Issue**: Creating distinctive modern aesthetic without losing usability
**Solution**: Leveraged CSS clip-path and asymmetric layouts for geometric sophistication
- **CSS Grid Mastery**: Complex responsive layouts with intentional asymmetry
- **Clip-Path Innovation**: Angular shapes and diagonal transitions for visual interest
- **Typography as Graphics**: Large text as visual design elements, not just content
- **Minimalist Approach**: Strategic negative space usage for visual impact

**Advanced CSS Techniques**:
```css
.geometric-layout {
  display: grid;
  grid-template-columns: 2fr 1fr;
  clip-path: polygon(0 0, 100% 0, 100% 85%, 0 100%);
}

/* Progressive enhancement for clip-path */
@supports not (clip-path: polygon(0 0, 100% 0, 100% 100%, 0 100%)) {
  .geometric-layout {
    /* Fallback design without clip-path */
    border-bottom: 20px solid transparent;
  }
}
```

### Mantine v7 Advanced Component Mastery
**Issue**: Fully leveraging component library capabilities for sophisticated UX
**Solution**: Created comprehensive component integration showcasing advanced Mantine patterns
- **Spotlight Integration**: Global search with role-based quick actions
- **DataTable Excellence**: Sortable, filterable tables with bulk operations
- **Chart Integration**: Community analytics with responsive data visualization
- **Advanced Notifications**: Multi-action toast notifications with persistence

**Component Patterns Discovered**:
```typescript
// Advanced Mantine component composition
<DataTable
  records={events}
  selectedRecords={selectedRecords}
  onSelectedRecordsChange={setSelectedRecords}
  sortStatus={sortStatus}
  onSortStatusChange={setSortStatus}
  columns={[
    {
      accessor: 'title',
      render: ({ title, image }) => (
        <Group gap="sm">
          <Avatar src={image} size="sm" radius="md" />
          <Text fw={500}>{title}</Text>
        </Group>
      )
    }
  ]}
  highlightOnHover
  withTableBorder
  styles={{
    header: { backgroundColor: 'var(--mantine-color-witchcity-6)' }
  }}
/>
```

### Template-Inspired Revolutionary Design
**Issue**: Transforming community platform into professional-grade solution
**Solution**: Adapted analytics dashboard templates for community management
- **Dashboard Architecture**: Multi-panel AppShell layout with sophisticated navigation
- **Data-Driven UX**: Community metrics, growth analytics, and performance insights
- **Professional Aesthetic**: High-end SaaS visual language with alternative content focus
- **Scalability Foundation**: Enterprise-grade patterns supporting platform growth

**Key Innovation**: Alternative edge through content and community focus, not visual extremes
**Best Practice**: Professional tools can serve alternative communities without compromising authenticity

### Design Variation Selection Framework
**Process**: Systematic approach to variation creation and stakeholder review
1. **Edginess Progression**: Gradual increase from 2/5 to 5/5 for comfort
2. **Implementation Complexity**: Clear timeline and resource requirements
3. **Risk Assessment**: Technical feasibility and timeline constraints
4. **Hybrid Options**: Combining best elements from multiple variations
5. **Stakeholder Education**: Clear comparison matrices and decision criteria

**Documentation Structure**:
```
/design/
├── variation-1-enhanced-current.md     # Detailed specs
├── variation-2-dark-theme-focus.md     # Full implementation
├── variation-3-geometric-modern.md     # Component mapping
├── variation-4-advanced-mantine.md     # Advanced patterns
├── variation-5-template-inspired.md    # Revolutionary approach
├── wireframe-variation-2-dark.html     # Interactive demo
└── design-variation-comparison.md      # Stakeholder summary
```

### Mobile-First Design for Alternative Communities
**Issue**: Alternative aesthetic maintaining mobile usability
**Solution**: Performance-optimized responsive design with progressive enhancement
- **Touch Optimization**: 48px minimum targets with improved gesture support
- **Performance Budgets**: Animation complexity based on device capabilities
- **Progressive Enhancement**: Core functionality without JavaScript
- **Accessibility Excellence**: WCAG 2.1 AA compliance in dramatic visual designs

**Mobile Animation Optimization**:
```typescript
const usePerformanceOptimizedDesign = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
  const isLowPerformance = navigator.hardwareConcurrency < 4;
  
  return {
    enable3D: !isMobile && !prefersReducedMotion && !isLowPerformance,
    enableNeon: !isLowPerformance,
    enableRipple: !prefersReducedMotion && !isLowPerformance,
    shadowLayers: isLowPerformance ? 1 : isMobile ? 2 : 4,
    animationDuration: isLowPerformance ? '0.2s' : '0.8s'
  };
};
```

## Modern Form Design Research & Implementation (August 2025)

### Form Design Pattern Development
**Issue**: Need for sophisticated, modern form designs that align with 2025 UI trends  
**Solution**: Created comprehensive form design library with multiple patterns
- **Research Process**: Combined Mantine v7 capabilities with current UI trends
- **Pattern Types**: Floating labels, inline minimal, card-based elevated, gradient accent, 3D elevation, neon ripple
- **Implementation**: Full React + TypeScript + Mantine v7 component specifications
- **Trend Integration**: Lightning dark, glassmorphism, gradient renaissance, microinteractions
- **AI Integration**: Gradients to indicate AI-generated content (2025 trend)
**Deliverables**:
```
/docs/design/form-design-examples/
├── design-a-floating-labels.md
├── design-b-inline-minimal.md
├── design-c-3d-elevation.md (NEW)
├── design-d-creative-highlight.md (NEW - Neon Ripple Spotlight)
└── design-comparison.md
```

### Advanced Form Highlighting Techniques (August 2025)
**Research Discovery**: Modern form field highlighting goes beyond simple borders
**Solutions Implemented**:

#### 3D Elevation Effects
- **Technique**: Multi-layered shadows with `translateY` transforms and subtle rotation
- **Implementation**: 4-layer shadow system with `rotateX(2deg)` and `rotateZ(0.5deg)`
- **Performance**: GPU-accelerated transforms with `transform3d()` and selective `will-change`
- **Mobile Optimization**: Disabled on mobile, simple border focus only
```css
/* Key technique: Layered shadows for realistic depth */
box-shadow: 
  0 8px 25px rgba(0, 0, 0, 0.25),     /* Ambient shadow */
  0 4px 15px rgba(155, 74, 117, 0.2), /* Direct shadow */
  0 2px 8px rgba(155, 74, 117, 0.3),  /* Accent glow */
  0 1px 3px rgba(155, 74, 117, 0.4);  /* Inner glow */
```

#### Neon Ripple Spotlight System
- **Multi-Effect Approach**: Combines neon glow, position-aware ripples, and spotlight dimming
- **Dynamic Positioning**: Ripples originate from exact click coordinates using CSS custom properties
- **Spotlight Mechanics**: Radial gradient overlay with real-time position tracking
- **Performance Strategy**: Effects gracefully degrade based on device capabilities
```css
/* Key technique: Position-aware ripple effects */
background: radial-gradient(
  circle at var(--ripple-x, 50%) var(--ripple-y, 50%),
  rgba(155, 74, 117, 0.4) 0%, 
  transparent 70%
);
```

### Design Pattern Selection Strategy
**Issue**: Multiple design patterns need clear usage guidelines  
**Solution**: Context-driven design selection framework
- **Primary Recommendation**: 3D Elevation for premium registration/onboarding experiences
- **Secondary**: Neon Ripple for event creation and special interactive forms
- **Enhancement**: Floating Label Modern for profiles/settings
- **Administrative**: Inline Minimal for administrative interfaces
- **Future**: Gradient Accent for AI-enhanced features
- **Implementation Strategy**: Performance-based selection with device capability detection
- **Hybrid Approach**: Multiple patterns based on specific use cases and device capabilities
**Applies to**: All form design decisions across WitchCityRope platform

### Performance-Optimized Animation Design
**Issue**: Complex form effects can impact performance on mobile devices  
**Solution**: Graduated complexity based on device capabilities
```typescript
const usePerformanceOptimizedDesign = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
  const isLowPerformance = navigator.hardwareConcurrency < 4;
  
  return {
    enable3D: !isMobile && !prefersReducedMotion && !isLowPerformance,
    enableNeon: !isLowPerformance,
    enableRipple: !prefersReducedMotion && !isLowPerformance,
    shadowLayers: isLowPerformance ? 1 : isMobile ? 2 : 4,
    animationDuration: isLowPerformance ? '0.2s' : '0.8s'
  };
};
```
**Key Insights**:
- Use hardware acceleration (`transform3d`) for smooth animations
- Apply `will-change` sparingly and remove after animations
- Provide solid color fallbacks for complex gradients
- Implement device capability detection for effect selection

### 2025 UI Trend Integration
**Research Findings**: Key trends for modern form design
- **Lightning Dark**: Enhanced dark mode with dynamic lighting effects
- **Gradient Renaissance**: Subtle to bold gradients for depth and AI indication
- **Microinteractions**: Purposeful animations for feedback and engagement
- **Glassmorphism**: Frosted glass effects for depth without overwhelming
- **Interactive Objects**: 3D elements and hover effects for immersion
- **Neon Aesthetics**: Cyberpunk-inspired glowing effects for tech-forward brands
- **Position-Aware Interactions**: Effects that respond to exact user input location
- **Spotlight UX**: Dynamic focus highlighting that dims non-active elements
**Implementation**: All patterns incorporate relevant 2025 trends while maintaining accessibility

### Advanced CSS Animation Techniques
**Research Discovery**: Modern CSS capabilities enable sophisticated form interactions
**Key Techniques Mastered**:

#### Multi-Layer Shadow Systems
```css
/* Realistic depth with 4 shadow layers */
box-shadow: 
  0 8px 25px rgba(0, 0, 0, 0.25),     /* Ground contact shadow */
  0 4px 15px rgba(155, 74, 117, 0.2), /* Colored ambient shadow */
  0 2px 8px rgba(155, 74, 117, 0.3),  /* Sharp accent glow */
  0 1px 3px rgba(155, 74, 117, 0.4);  /* Inner highlight */
```

#### CSS Custom Properties for Dynamic Effects
```css
/* Dynamic positioning using CSS variables */
.ripple-effect {
  background: radial-gradient(
    circle at var(--ripple-x, 50%) var(--ripple-y, 50%),
    rgba(155, 74, 117, 0.4) 0%, 
    transparent 70%
  );
}
```

#### Hardware-Accelerated Transforms
```css
/* GPU acceleration for smooth performance */
.elevation-field {
  transform: translate3d(0, -6px, 0) rotateX(2deg) rotateZ(0.5deg);
  will-change: transform, box-shadow;
  backface-visibility: hidden;
}
```

### Accessibility in Advanced Form Designs
**Issue**: Complex animations can impact accessibility
**Solutions Implemented**:
- **Motion Respect**: All animations respect `prefers-reduced-motion: reduce`
- **Focus Alternatives**: Non-animated focus indicators for users who disable motion
- **Screen Reader Support**: ARIA live regions announce state changes
- **Keyboard Navigation**: All effects work with keyboard-only interaction
- **Color Independence**: Effects work without color perception
```tsx
// Accessibility pattern for complex animations
const useAccessibleAnimations = () => {
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
  
  return {
    enableAnimations: !prefersReducedMotion,
    focusIndicator: prefersReducedMotion ? 'border' : 'full-effect',
    transitionDuration: prefersReducedMotion ? '0s' : '0.3s'
  };
};
```

### Mantine v7 Advanced Patterns
**Discovery**: Advanced Mantine v7 capabilities for sophisticated forms
```typescript
// Gradient border effects with Mantine styling
const gradientBorderStyles = {
  '&::before': {
    content: '""',
    position: 'absolute',
    background: 'linear-gradient(135deg, rgba(155, 74, 117, 0.8) 0%, rgba(180, 113, 113, 0.6) 100%)',
    borderRadius: 'inherit',
    opacity: 0,
    transition: 'opacity 0.3s ease'
  },
  '&:focus::before': {
    opacity: 1
  }
}

// 3D perspective container setup
const perspective3D = {
  perspective: '1200px',
  perspectiveOrigin: 'center top',
  transformStyle: 'preserve-3d'
}

// Neon glow animation system
const neonGlowAnimation = {
  '@keyframes neonPulse': {
    '0%, 100%': {
      boxShadow: '0 0 5px rgba(155, 74, 117, 0.5), 0 0 10px rgba(155, 74, 117, 0.3)'
    },
    '50%': {
      boxShadow: '0 0 8px rgba(155, 74, 117, 0.8), 0 0 15px rgba(155, 74, 117, 0.5), 0 0 20px rgba(155, 74, 117, 0.3)'
    }
  }
}
```
**Performance Considerations**: 
- Use CSS custom properties for dynamic gradients
- Implement fallbacks for older browsers
- Optimize backdrop-filter usage
- Provide solid color alternatives for accessibility

### AI Content Integration Patterns
**Innovation**: Visual patterns for AI-generated form content
```typescript
// AI content indicator
const AIIndicator = () => (
  <Box
    style={{
      background: 'linear-gradient(135deg, #9b4a75 0%, #880124 100%)',
      color: 'white',
      padding: '2px 6px',
      borderRadius: '12px',
      fontSize: '10px',
      fontWeight: 600,
      textTransform: 'uppercase',
      letterSpacing: '0.5px'
    }}
  >
    AI
  </Box>
);

// Shimmer loading effect for AI processing
const shimmerAnimation = {
  '&::before': {
    background: 'linear-gradient(90deg, transparent 0%, rgba(155, 74, 117, 0.4) 50%, transparent 100%)',
    animation: 'shimmer 1.5s infinite'
  }
}
```
**Purpose**: Clear visual distinction for AI-enhanced form features
**Accessibility**: Maintains screen reader compatibility with proper ARIA labels

## Wireframe Standards

### File Organization
**Issue**: Wireframes scattered across multiple folders  
**Solution**: Keep wireframes with their functional area
```
functional-areas/
└── events-management/
    ├── wireframes/           # HTML wireframe files
    │   ├── event-list.html
    │   └── event-detail.html
    └── current-state/
        └── wireframes.md     # Documentation about wireframes
```
**Applies to**: All new wireframe creation

### Naming Conventions
**Issue**: Inconsistent file names making them hard to find  
**Solution**: Use descriptive, hyphenated names
```
✅ CORRECT:
- user-dashboard-overview.html
- event-creation-form.html
- admin-vetting-review.html

❌ WRONG:
- dashboard.html
- new_event.html
- AdminVetting.html
```
**Applies to**: All wireframe files

## Design Patterns

### Mobile-First Approach
**Issue**: Wireframes only showing desktop view  
**Solution**: Always design mobile view first
```html
<!-- Include viewport meta tag -->
<meta name="viewport" content="width=device-width, initial-scale=1.0">

<!-- Use responsive classes -->
<div class="container">
  <div class="row">
    <div class="col-12 col-md-6">
      <!-- Content -->
    </div>
  </div>
</div>
```
**Applies to**: All new wireframes

### Component Consistency
**Issue**: Same UI element designed differently across pages  
**Solution**: Reference the component library
- Check `standards-processes/ui-components/` for existing components
- Use consistent button styles, form layouts, etc.
- Don't reinvent existing patterns
- **Phase 2 Priority**: Complete UI design BEFORE other technical designs
- **Human Review**: MUST pause after UI completion for approval

### Accessibility Considerations
**Issue**: Wireframes missing accessibility features  
**Solution**: Include accessibility annotations
```html
<!-- Add aria labels and roles -->
<button aria-label="Close dialog" role="button">×</button>

<!-- Include skip navigation -->
<a href="#main-content" class="skip-link">Skip to main content</a>

<!-- Annotate color contrast requirements -->
<!-- Note: Text must have 4.5:1 contrast ratio -->
```
**Applies to**: All interactive elements

## OAuth and Authentication UX Patterns

### OAuth Provider Selection (Research: Aug 2025)
**Issue**: Need to choose OAuth provider for small community sites  
**Solution**: Clerk is optimal for WitchCityRope's needs
- **Best for small communities**: Clerk ($550/month for 10k MAU)
- **Quick implementation**: Under 5 minutes for basic setup
- **Professional appearance**: Beautiful pre-built components
- **Avoid**: Custom OAuth (security complexity), Auth0 (too expensive)

### OAuth Button Design
**Pattern**: Follow 2024-2025 UX best practices
```jsx
// OAuth Button Layout
<VStack spacing={3}>
  <Button
    width="100%"
    leftIcon={<FaGoogle />}
    onClick={() => signInWithOAuth('google')}
    colorScheme="red"
    variant="outline"
  >
    Continue with Google
  </Button>
  <Divider />
  <Text fontSize="sm" color="gray.500">OR</Text>
  <Input placeholder="Email" />
  <Input type="password" placeholder="Password" />
</VStack>
```

### Mobile OAuth Considerations
**Issue**: OAuth flows need mobile optimization  
**Solution**: Thumb zone optimization
- Place OAuth buttons in bottom 1/3 of screen
- Use full-width buttons on mobile
- Implement one-handed use patterns
- 53% of users abandon sites taking >3 seconds to load

### Age Verification for Adult Sites
**Critical**: COPPA compliance requirements (effective June 2025)  
**Solution**: Cannot collect personal info before age determination
```jsx
const handleOAuthSuccess = async (userData) => {
  // Check if OAuth provider includes age/birthdate
  if (userData.ageVerified && userData.age >= 18) {
    await registerUser(userData);
  } else {
    router.push('/verify-age');
  }
};
```

### Session Management UX
**Pattern**: "Remember me" with refresh tokens
- Access tokens: 15 minutes
- Refresh tokens: 30 days  
- Remember me: 90 days
- Always validate server-side

## Docker Architecture Design Patterns (August 2025)

### Container Architecture Diagrams
**Issue**: Complex Docker setups need clear visual representation  
**Solution**: Use ASCII art with container communication flows
```
┌─────────────────┐  HTTP Cookies   ┌─────────────────┐   JWT Bearer    ┌─────────────────┐
│  React SPA      │ ─────────────► │  Web Service    │ ─────────────► │  API Service    │
│  (Port 5173)    │ ◄───────────── │  (Port 5655)    │ ◄───────────── │  (Port 8080)    │
└─────────────────┘                └─────────────────┘                └─────────────────┘
```
**Best Practice**: Include port mappings, container names, and communication protocols

### Multi-Environment Strategy Visualization
**Issue**: Complex docker-compose override patterns hard to understand  
**Solution**: Layer-based documentation with inheritance diagrams
```
Base (docker-compose.yml)
├── Development (docker-compose.dev.yml)
├── Test (docker-compose.test.yml)
└── Production (docker-compose.prod.yml)
```
**Pattern**: Show configuration matrix comparing environments

### Service Communication Flow Design
**Issue**: Container networking can be confusing for developers  
**Solution**: Sequence diagrams showing authentication flows
- Show external browser → container communication
- Highlight internal container-to-container calls
- Document service discovery patterns
- Include health check dependencies

### Developer Workflow Visualization
**Issue**: Complex Docker development setup intimidates developers  
**Solution**: Step-by-step workflow diagrams
- Daily development workflow from startup to commit
- Hot reload flow showing file changes → container updates
- Debugging workflow with IDE → container connections
- Testing workflow with container orchestration

### Docker Design Documentation Standards
**File Structure**: Place Docker design docs in functional area
```
/docs/functional-areas/docker-authentication/design/
├── docker-architecture-diagram.md
├── environment-strategy.md
├── service-communication-design.md
└── developer-workflow.md
```

**Content Requirements**:
- ASCII diagrams for all architectures
- Step-by-step sequences with timing estimates
- Environment comparison matrices
- Troubleshooting decision trees
- Configuration examples with explanations

## WitchCityRope Specific Patterns

### Event Display Pattern
**Standard layout for events**:
1. Event image/banner (optional)
2. Title and scene name
3. Date, time, location
4. Price and ticket availability
5. Description
6. RSVP/Buy button

### User Dashboard Sections
**Standard organization**:
1. Welcome message with user name
2. Upcoming events (RSVPs and tickets)
3. Quick actions (browse events, update profile)
4. Recent activity
5. Settings link

### Form Layouts
**Consistent form structure**:
1. Clear heading
2. Required field indicators (*)
3. Field grouping with sections
4. Help text under fields
5. Clear primary action button
6. Cancel/back option

### Authentication Flow for Adult Community
**Privacy-first approach**:
1. OAuth login with age verification
2. Default profile to private
3. Explicit consent for photo sharing
4. Role assignment (start as 'guest')
5. Clear privacy controls

## Integration with Development

### Data Attributes
**Issue**: Developers can't identify elements from wireframes  
**Solution**: Add data-testid attributes
```html
<button data-testid="submit-event">Create Event</button>
<div data-testid="event-list">
  <div data-testid="event-card">...</div>
</div>
```
**Applies to**: All interactive elements

### State Representations
**Issue**: Only showing happy path  
**Solution**: Design all states
- Empty states (no data)
- Loading states (critical for OAuth flows)
- Error states with recovery options
- Success messages
- Validation errors

### Error Handling for OAuth
**Pattern**: Clear error communication with recovery
```jsx
<Alert status="error">
  <AlertIcon />
  <AlertTitle>Google Login Failed</AlertTitle>
  <AlertDescription>
    Please check your Google account permissions and try again.
    <Link href="/login/email">Use email/password instead</Link>
  </AlertDescription>
</Alert>
```

### Responsive Breakpoints
**Issue**: Unclear how design adapts  
**Solution**: Document breakpoints
```html
<!-- Add comments for breakpoints -->
<!-- Mobile: < 768px -->
<!-- Tablet: 768px - 1024px -->
<!-- Desktop: > 1024px -->
```

## Tools and Resources

### Recommended Tools
- **HTML/CSS**: For interactive wireframes
- **Comments**: Explain interactions and flows
- **Browser DevTools**: Test responsive design

### Style References
- Brand colors: See `standards-processes/ui-components/design-tokens.json`
- Typography: Check style guide
- Spacing: Use consistent 8px grid system

### OAuth Design Resources
- **Clerk**: Pre-built components with Salem theme customization
- **Loading states**: Chakra UI Spinner, Skeleton components
- **Error patterns**: Alert, Toast components for OAuth failures

### Validation Checklist
Before submitting wireframes:
- [ ] Mobile view included
- [ ] All states represented (loading, error, success)
- [ ] OAuth flow documented
- [ ] Age verification considered
- [ ] Data-testid attributes added
- [ ] Accessibility considered
- [ ] Consistent with existing patterns
- [ ] File properly named and located
- [ ] **PHASE 2 SEQUENCING**: UI design completed FIRST (before other designs)
- [ ] **HUMAN REVIEW**: Ready for mandatory UI approval checkpoint
- [ ] **CLICKABLE MOCKUPS**: Created when possible for stakeholder review
- [ ] **DESIGN VARIATIONS**: Multiple progressive options provided for stakeholder comfort
- [ ] **IMPLEMENTATION SPECIFICATIONS**: Complete Mantine v7 component mapping included

## Common Mistakes to Avoid

1. **Creating in isolation** - Always check existing components first
2. **Desktop-only thinking** - Mobile users are significant
3. **Perfect pixel designs** - Focus on layout and flow
4. **Missing edge cases** - What if OAuth fails? Network issues?
5. **Ignoring accessibility** - Consider keyboard navigation, screen readers
6. **Skipping loading states** - OAuth flows have significant wait times
7. **Custom OAuth implementation** - Use proven providers like Clerk
8. **Complex Docker diagrams** - Keep ASCII art simple and focused
9. **Missing container communication** - Show internal vs external traffic
10. **Incomplete environment coverage** - Document dev, test, and production
11. **Over-designing forms** - Balance sophistication with usability
12. **Ignoring performance** - Test complex designs on mobile devices
13. **Skipping gradient fallbacks** - Always provide solid color alternatives
14. **Overusing 3D effects** - Apply selectively for premium experiences only
15. **Motion without purpose** - Every animation should serve user experience
16. **Performance assumptions** - Always test on target devices before implementing
17. **Single design solution** - Always provide progressive variation options for stakeholder comfort
18. **Ignoring edginess scale** - Community authenticity requires appropriate alternative aesthetic
19. **Template cargo cult** - Adapt patterns to community needs, don't blindly copy
20. **Missing hybrid options** - Combine best elements from multiple approaches
21. **Inconsistent page designs** - Each page should align with overall design system while serving specific functions
22. **Oversimplified event filtering** - Community events require sophisticated discovery mechanisms
23. **Ignoring member access levels** - Alternative communities have complex privacy and safety requirements
24. **Static wireframes only** - Interactive prototypes provide better stakeholder understanding
25. **Stakeholder feedback rejection** - Never ignore explicit feedback (e.g., "no neon colors")
26. **Description-only designs** - Stakeholders need to SEE and EXPERIENCE designs, not read about them
27. **Single aesthetic approach** - Always provide progressive variation options for different comfort levels
28. **Missing template research integration** - Link wireframes to research foundation for credibility
29. **No decision support framework** - Include comparison matrices and implementation guidance
30. **Ignoring underline animation requirements** - When stakeholder specifically loves a feature, make it prominent
31. **Mouse-following animations** - Completely eliminate when stakeholder explicitly hates them
32. **Moving boxes on hover** - Avoid any hover effects that move containers or cards
33. **Vibrant energy aesthetics** - Replace with sophisticated dark elegance when stakeholder rejects
34. **Missing organic layout principles** - Always reference beloved original design patterns
35. **Rejecting approved foundations** - When stakeholders reject new directions, return to what they already approved
36. **Dramatic redesigns during recovery** - Use "polish" and "refinement" approach when stakeholders are unhappy
37. **Ignoring specific element feedback** - Document exact rules for loved/hated UI elements
38. **Overcomplicating refinements** - Keep variations subtle when recovering from design rejection

## Handoff Process

### What Developers Need
1. **Layout structure** - How components are arranged
2. **Interaction notes** - What happens on click/hover/OAuth flow
3. **Data requirements** - What information is displayed
4. **State variations** - Different views based on conditions
5. **OAuth flow details** - Provider configuration, error handling
6. **Age verification** - Compliance requirements and UX flow
7. **Container architecture** - Service communication patterns
8. **Environment configuration** - Multi-environment setup requirements
9. **Form design specifications** - Component props, styling details, animations
10. **Performance requirements** - Mobile optimization, fallback strategies
11. **Animation specifications** - Timing, easing, trigger conditions
12. **Device capability detection** - How effects adapt to different devices
13. **Design variation rationale** - Why specific variation was chosen
14. **Component library mapping** - Exact Mantine v7 components and configurations
15. **Accessibility compliance** - WCAG 2.1 AA implementation requirements
16. **Event discovery patterns** - Complex filtering and search specifications
17. **Community safety requirements** - Access controls, consent, and verification flows
18. **Interactive wireframe specifications** - Complete implementation reference with working examples
19. **Stakeholder feedback integration** - How designs address specific requirements and concerns
20. **Template research references** - Links to inspiration sources and implementation patterns
21. **Gothic aesthetic specifications** - Color palettes, typography, animation styles
22. **Underline animation implementation** - Exact CSS and positioning for all underline effects
23. **Organic layout techniques** - Clip-path usage, border-radius patterns, flowing animations
24. **Theme variation differences** - What makes each Gothic variation unique
25. **Performance optimization strategies** - Mobile degradation, reduced motion support
26. **Refined variation preservation rules** - What elements are sacred vs. refinable vs. additive vs. forbidden

### Documentation Format
Create a companion `.md` file explaining:
- User flow through the screens
- OAuth provider integration notes
- Business logic notes
- Age verification requirements
- Questions or assumptions
- Links to related requirements
- Docker deployment considerations
- Container communication requirements
- Form component specifications
- Performance optimization notes
- Animation implementation details
- Accessibility requirements and alternatives
- Design variation progression and selection rationale
- Implementation timeline and complexity assessment
- Stakeholder review process and decision criteria
- Event-specific interactions and flows
- Community safety and privacy considerations
- Interactive wireframe implementation guide
- Template research integration and adaptation strategy
- Stakeholder feedback resolution and design evolution
- Gothic aesthetic implementation guidelines
- Underline animation technical specifications
- Organic layout adaptation techniques
- Theme customization and variation management
- Refined variation categorization and preservation strategy

---

*Remember: Wireframes are communication tools. Clear annotations and consistency are more valuable than pixel perfection. For OAuth flows, prioritize security and compliance over complexity. For Docker designs, focus on developer experience and clear service communication patterns. For modern forms, balance sophistication with performance and accessibility - create designs that are both beautiful and functional. Advanced form effects should enhance the user experience, not overwhelm it. Design variations should provide stakeholder comfort through progressive options, allowing selection based on desired impact, timeline, and community feedback. Events pages require sophisticated discovery mechanisms while maintaining design system coherence across all page types. ALWAYS create interactive wireframes when possible - stakeholders need to experience designs, not imagine them. Integrate stakeholder feedback immediately and provide progressive variation options for decision-making comfort. For Gothic elegant designs, remember that underline animations are essential and beloved - feature them prominently while avoiding any mouse-following or box-moving effects that stakeholders hate. When stakeholders reject design directions, immediately return to approved foundations with minimal, targeted refinements. Document specific rules for loved vs. hated UI elements to prevent future mistakes.*