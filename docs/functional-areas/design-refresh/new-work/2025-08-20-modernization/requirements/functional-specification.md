# Functional Specification: Design System Modernization
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: Functional Specification Agent -->
<!-- Status: Draft -->

## Architecture Discovery Phase (MANDATORY PHASE 0)

### Documents Reviewed:
- [x] `/docs/lessons-learned/functional-spec-lessons-learned.md` - Lines 1-98 - Reviewed mandatory startup procedure and NSwag auto-generation patterns
- [x] `/docs/architecture/react-migration/domain-layer-architecture.md` - Lines 1-999 - Confirmed NSwag type generation and React+API architecture (no manual DTO creation)
- [x] `/docs/functional-areas/design-refresh/new-work/2025-08-20-modernization/requirements/business-requirements.md` - Lines 1-430 - Found comprehensive business requirements with stakeholder approval
- [x] `/docs/functional-areas/design-refresh/new-work/2025-08-20-modernization/requirements/mantine-template-research.md` - Lines 1-441 - Found complete Mantine v7 template analysis and implementation patterns

### Existing Solutions Found:
- **NSwag Type Generation**: Lines 725-997 in domain-layer-architecture.md specify complete TypeScript type auto-generation pipeline
- **React+API Architecture**: Lines 440-515 specify Web service (React) makes HTTP calls to API service pattern
- **Design Foundation**: Lines 55-77 in business-requirements.md document existing color palette, typography, and component patterns
- **Mantine v7 Patterns**: Lines 208-318 in template research provide complete implementation examples for dark theme, navigation, and responsive components

### Verification Statement:
"Confirmed existing solutions provide solid foundation for design modernization. No conflicting architecture patterns found. NSwag handles type generation automatically. Focus specification on UI component modernization using Mantine v7 patterns."

---

## Executive Summary

This functional specification defines the technical implementation for WitchCityRope's design system modernization, transforming the current luxury aesthetic into a more modern, edgy, and interactive experience while preserving excellent UX patterns. The implementation leverages Mantine v7 components to deliver 5 progressive design variations, reorganized documentation structure, and mobile-first responsive patterns.

## Technical Overview

### High-Level Technical Approach
The design modernization follows a component-first approach using Mantine v7's comprehensive UI library to create modern, interactive experiences without custom component development. The implementation focuses on enhancing visual aesthetics, improving interactive elements, and establishing a scalable design system foundation.

**Key Technical Principles:**
- **Mantine v7 Component Library**: Leverage 120+ prebuilt components for rapid development
- **React TypeScript Patterns**: Functional components with hooks and strict prop typing
- **Design System Architecture**: Centralized theme configuration with component variants
- **Progressive Enhancement**: Mobile-first responsive design with performance optimization

## Architecture

### Microservices Architecture Integration
**CRITICAL**: This design modernization respects the established Web+API microservices architecture:
- **Web Service** (React + Vite): UI components at http://localhost:5173
- **API Service** (Minimal API): Business logic at http://localhost:5653  
- **Database** (PostgreSQL): localhost:5433
- **Pattern**: React UI → HTTP → API → Database (design components make API calls)

### Component Structure
```
/apps/web/src/
├── components/
│   ├── layout/
│   │   ├── AppShell.tsx           # Main application shell
│   │   ├── Navigation.tsx         # Role-based navigation
│   │   └── Header.tsx            # Site header with theme toggle
│   ├── common/
│   │   ├── HeroSection.tsx       # Homepage hero component
│   │   ├── EventCard.tsx         # Event listing cards
│   │   ├── UserAvatar.tsx        # User profile display
│   │   └── ThemeToggle.tsx       # Dark/light mode switcher
│   ├── auth/
│   │   ├── LoginForm.tsx         # Authentication form
│   │   └── LoginPage.tsx         # Complete login page
│   └── events/
│       ├── EventGrid.tsx         # Event listing grid
│       ├── EventDetails.tsx      # Event detail view
│       └── EventFilters.tsx      # Event filtering controls
├── hooks/
│   ├── useAuth.ts               # Authentication state management
│   ├── useTheme.ts              # Theme management
│   └── useEvents.ts             # Event data management
├── styles/
│   ├── theme.ts                 # Mantine theme configuration
│   ├── globals.css              # Global styles
│   └── components.module.css    # Component-specific styles
└── types/
    └── ui.ts                    # UI-specific type definitions
```

### Service Architecture
- **Web Service**: UI components make HTTP calls to API for data
- **API Service**: Provides data endpoints with OpenAPI documentation
- **No Direct Database Access**: Web service NEVER directly accesses database
- **Type Generation**: NSwag automatically generates TypeScript types from API

## Data Models

### UI State Models
```typescript
// Theme configuration
interface WitchCityTheme {
  colors: {
    witchcity: [string, string, string, string, string, string, string, string, string, string];
  };
  primaryColor: 'witchcity';
  defaultColorScheme: 'auto' | 'light' | 'dark';
}

// Navigation state
interface NavigationState {
  activeSection: string;
  userRole: UserRole;
  isMobileOpen: boolean;
  hasNotifications: boolean;
}

// Component variant types
interface ComponentVariant {
  size: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  variant: 'filled' | 'outline' | 'light' | 'subtle' | 'transparent';
  color?: string;
}
```

### Design Variation Models
```typescript
interface DesignVariation {
  id: 'current' | 'enhanced' | 'dark' | 'geometric' | 'modern';
  name: string;
  description: string;
  colorPalette: ColorPalette;
  componentTheme: ComponentThemeConfig;
  animationLevel: 'none' | 'subtle' | 'moderate' | 'dynamic';
  edginessLevel: 1 | 2 | 3 | 4 | 5;
}

interface ColorPalette {
  primary: string[];
  secondary: string[];
  accent: string[];
  neutral: string[];
  semantic: {
    success: string;
    warning: string;
    error: string;
    info: string;
  };
}
```

## API Specifications

### Theme and User Preference Endpoints
| Method | Path | Description | Request | Response |
|--------|------|-------------|---------|----------|
| GET | /api/user/preferences | Get user theme preferences | - | UserPreferencesDto |
| PUT | /api/user/preferences/theme | Update theme preference | ThemePreferenceDto | UserPreferencesDto |
| GET | /api/system/theme | Get system theme configuration | - | SystemThemeDto |

**Note**: Types will be auto-generated via NSwag pipeline from API OpenAPI specification

## Component Specifications

### 1. Homepage & Navigation System

#### Main Navigation Component
- **Path**: `/`
- **Component**: `AppShell` with role-based `Navigation`
- **Authorization**: Public (with role-based menu items)
- **Render Mode**: Client-side with SSR support
- **Key Features**: 
  - Role-based menu sections (Guest, Member, Vetted, Teacher, Admin)
  - Responsive sidebar/hamburger navigation
  - User avatar with dropdown menu
  - Search integration (Ctrl+K shortcut)
  - Theme toggle placement

#### Implementation Pattern
```typescript
import { AppShell, NavLink, Avatar, Menu, Spotlight } from '@mantine/core';
import { useAuth } from '@/hooks/useAuth';

const Navigation = () => {
  const { user, hasRole } = useAuth();
  
  return (
    <AppShell.Navbar p="md">
      <NavLink 
        label="Events" 
        leftSection={<CalendarIcon />}
        href="/events"
      />
      
      {hasRole('member') && (
        <NavLink label="Member Dashboard" leftSection={<DashboardIcon />} />
      )}
      
      {hasRole('admin') && (
        <NavLink label="Admin Panel" leftSection={<SettingsIcon />}>
          <NavLink label="User Management" href="/admin/users" />
          <NavLink label="Event Management" href="/admin/events" />
          <NavLink label="Vetting System" href="/admin/vetting" />
        </NavLink>
      )}
    </AppShell.Navbar>
  );
};
```

#### Hero Section Specifications
- **Layout**: Split content (text left, visual right on desktop)
- **Typography**: 
  - Headline: 3.5rem desktop, 2rem mobile
  - Gradient text effects on key terms
  - Supporting text: large size with dimmed color
- **CTAs**: Dual button approach (primary gradient, secondary outline)
- **Responsive**: Stacked on mobile with adjusted typography

### 2. Login Page

#### Authentication Form Layout
- **Component**: `LoginForm` with validation
- **Container**: Centered card with brand elements
- **Fields**: Email, password with show/hide toggle
- **Validation**: Real-time with error states
- **Accessibility**: ARIA labels, keyboard navigation

#### Implementation Pattern
```typescript
import { Card, TextInput, PasswordInput, Button, Checkbox } from '@mantine/core';
import { useForm } from '@mantine/form';

const LoginForm = () => {
  const form = useForm({
    initialValues: { email: '', password: '', rememberMe: false },
    validate: {
      email: (value) => (/^\S+@\S+$/.test(value) ? null : 'Invalid email'),
      password: (value) => (value.length >= 8 ? null : 'Password too short'),
    },
  });

  return (
    <Card shadow="md" padding="xl" radius="md" withBorder>
      <form onSubmit={form.onSubmit(handleLogin)}>
        <TextInput
          label="Email"
          placeholder="your@email.com"
          {...form.getInputProps('email')}
        />
        <PasswordInput
          label="Password"
          placeholder="Your password"
          {...form.getInputProps('password')}
        />
        <Checkbox
          label="Remember me"
          {...form.getInputProps('rememberMe', { type: 'checkbox' })}
        />
        <Button type="submit" variant="gradient" fullWidth>
          Sign In
        </Button>
      </form>
    </Card>
  );
};
```

#### Features
- **Remember Me**: Checkbox with local storage integration
- **Forgot Password**: Link to password reset flow
- **Error Handling**: Toast notifications for authentication errors
- **Loading States**: Button spinner during authentication
- **Mobile Responsive**: Full-width on mobile, centered card on desktop

### 3. Events Page

#### Event Listing Layout
- **Component**: `EventGrid` with filtering
- **Views**: Grid and list view toggles
- **Cards**: Hover animations, member level badges
- **Filtering**: Search, date range, member level
- **Pagination**: Load more or traditional pagination

#### Event Card Specifications
```typescript
import { Card, Badge, Text, Group, Button } from '@mantine/core';

const EventCard = ({ event }: { event: Event }) => (
  <Card shadow="sm" radius="md" withBorder padding="lg">
    <Group justify="space-between" mb="xs">
      <Text fw="bold" lineClamp={1}>{event.title}</Text>
      <Badge 
        color={event.requiresVetting ? 'red' : 'blue'}
        variant="light"
      >
        {event.requiresVetting ? 'Vetted' : 'Open'}
      </Badge>
    </Group>
    
    <Text size="sm" c="dimmed" mb="md" lineClamp={3}>
      {event.description}
    </Text>
    
    <Group justify="space-between">
      <Text size="xs" c="dimmed">
        {formatDate(event.startDate)}
      </Text>
      <Button variant="light" size="xs">
        Learn More
      </Button>
    </Group>
  </Card>
);
```

#### Grid Layout
- **Responsive Columns**: 1 (mobile), 2 (tablet), 3 (desktop)
- **Card Interactions**: Hover elevation, smooth transitions
- **Content Management**: Consistent card heights, text truncation
- **Loading States**: Skeleton placeholders

### 4. Global Design System Elements

#### Color Palette Specifications
```typescript
const witchCityColors = {
  witchcity: [
    '#f8f4e6', // ivory (lightest - index 0)
    '#faf6f2', // cream
    '#d4a5a5', // dusty rose
    '#b76d75', // rose gold
    '#a45757', // medium rose
    '#9b4a75', // plum
    '#880124', // burgundy (primary - index 6)
    '#660018', // dark burgundy
    '#2c2c2c', // charcoal
    '#1a1a2e'  // midnight (darkest - index 9)
  ]
};
```

#### Typography System
- **Primary Font**: System font stack for performance
- **Heading Hierarchy**: 
  - H1: 3.5rem/2rem (desktop/mobile)
  - H2: 2.5rem/1.75rem
  - H3: 2rem/1.5rem
  - Body: 1rem with 1.6 line height
- **Gradient Text**: Available for brand elements
- **Font Weights**: 400 (normal), 600 (semibold), 700 (bold)

#### Spacing and Sizing Tokens
- **Spacing Scale**: 0, 4, 8, 12, 16, 24, 32, 48, 64, 96px
- **Component Sizes**: xs, sm, md, lg, xl with consistent scaling
- **Touch Targets**: Minimum 44px for mobile interactions
- **Grid System**: 12-column responsive grid with gutters

#### Animation Specifications
- **Duration**: 150ms (micro), 300ms (standard), 500ms (emphasis)
- **Easing**: ease-out for entrances, ease-in for exits
- **Transform**: translateY, scale, opacity for common animations
- **Hover States**: 150ms transitions for interactive elements
- **Loading**: Skeleton animations, progress indicators

#### Component Variants
```typescript
// Button variants
const buttonVariants = {
  primary: { variant: 'gradient', gradient: { from: 'witchcity.6', to: 'witchcity.4' } },
  secondary: { variant: 'outline', color: 'witchcity' },
  subtle: { variant: 'light', color: 'witchcity' },
  minimal: { variant: 'transparent', color: 'witchcity' }
};

// Card variants
const cardVariants = {
  default: { shadow: 'sm', radius: 'md', withBorder: true },
  elevated: { shadow: 'md', radius: 'lg', withBorder: false },
  interactive: { shadow: 'sm', radius: 'md', withBorder: true, className: 'hover-elevate' }
};
```

#### Responsive Breakpoints
- **xs**: 0-575px (mobile)
- **sm**: 576-767px (large mobile)
- **md**: 768-991px (tablet)
- **lg**: 992-1199px (desktop)
- **xl**: 1200px+ (large desktop)

## Technical Specifications

### Mantine v7 Component Mapping

#### Core Layout Components
| Feature | Mantine Component | Implementation Notes |
|---------|------------------|---------------------|
| App Layout | AppShell | With navbar, header, main content areas |
| Navigation | NavLink, Menu | Role-based rendering with icons |
| Grid System | Grid, SimpleGrid | Responsive column management |
| Cards | Card | Event listings, user profiles, content blocks |
| Modals | Modal, Drawer | Form overlays, mobile navigation |

#### Interactive Components
| Feature | Mantine Component | Implementation Notes |
|---------|------------------|---------------------|
| Buttons | Button | Gradient, outline, light variants |
| Forms | TextInput, Select, DateInput | With validation states |
| Tables | Table | Sortable columns, pagination |
| Notifications | Notifications | Toast messages for feedback |
| Theme Toggle | ActionIcon, Switch | Dark/light mode switcher |

#### Advanced Components
| Feature | Mantine Component | Implementation Notes |
|---------|------------------|---------------------|
| Search | Spotlight | Global search with Ctrl+K |
| Date Picker | DatePicker, Calendar | Event scheduling |
| File Upload | Dropzone | Profile images, documents |
| Charts | Recharts integration | Analytics dashboards |
| Carousel | Carousel | Image galleries, testimonials |

### State Management for Theme Toggle

#### Theme Context Implementation
```typescript
import { createContext, useContext, useState, useEffect } from 'react';
import { MantineColorScheme } from '@mantine/core';

interface ThemeContextType {
  colorScheme: MantineColorScheme;
  toggleColorScheme: () => void;
  setColorScheme: (scheme: MantineColorScheme) => void;
}

const ThemeContext = createContext<ThemeContextType | null>(null);

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const [colorScheme, setColorScheme] = useState<MantineColorScheme>('auto');

  useEffect(() => {
    // Load saved preference
    const saved = localStorage.getItem('mantine-color-scheme');
    if (saved) setColorScheme(saved as MantineColorScheme);
  }, []);

  const toggleColorScheme = () => {
    const next = colorScheme === 'dark' ? 'light' : 'dark';
    setColorScheme(next);
    localStorage.setItem('mantine-color-scheme', next);
  };

  return (
    <ThemeContext.Provider value={{ colorScheme, toggleColorScheme, setColorScheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) throw new Error('useTheme must be used within ThemeProvider');
  return context;
};
```

## Performance Requirements

### Loading Performance
- **Initial Page Load**: <3 seconds on 3G connection
- **Interaction Response**: <200ms for UI feedback
- **Theme Switching**: <100ms transition duration
- **Component Rendering**: <16ms for 60fps animations
- **Bundle Size**: <500KB initial JavaScript bundle

### Optimization Strategies
- **Code Splitting**: Route-based component loading
- **Tree Shaking**: Import only used Mantine components
- **Image Optimization**: WebP format with fallbacks
- **CSS Optimization**: Critical CSS inlined, non-critical deferred
- **Caching**: Aggressive caching for static design assets

### Performance Monitoring
```typescript
// Performance measurement hooks
const usePerformanceMetrics = () => {
  useEffect(() => {
    // Measure component render time
    const observer = new PerformanceObserver((list) => {
      list.getEntries().forEach((entry) => {
        console.log(`${entry.name}: ${entry.duration}ms`);
      });
    });
    observer.observe({ entryTypes: ['measure'] });
    
    return () => observer.disconnect();
  }, []);
};
```

## Accessibility Requirements

### WCAG 2.1 AA Compliance
- **Color Contrast**: 4.5:1 minimum for normal text, 3:1 for large text
- **Keyboard Navigation**: Full functionality without mouse
- **Screen Reader Support**: ARIA labels, roles, and descriptions
- **Focus Management**: Visible focus indicators, logical tab order
- **Motion Sensitivity**: Respect prefers-reduced-motion setting

### Implementation Patterns
```typescript
// Accessible button with proper ARIA
const AccessibleButton = ({ children, onClick, ...props }) => (
  <Button
    onClick={onClick}
    aria-label={props['aria-label']}
    role="button"
    tabIndex={0}
    onKeyDown={(e) => {
      if (e.key === 'Enter' || e.key === ' ') onClick(e);
    }}
    {...props}
  >
    {children}
  </Button>
);

// Skip link for keyboard users
const SkipLink = () => (
  <a href="#main-content" className="skip-link">
    Skip to main content
  </a>
);
```

### Accessibility Testing
- **Automated**: axe-core integration in test suite
- **Manual**: Keyboard navigation testing
- **Screen Reader**: NVDA, JAWS, VoiceOver testing
- **Color Vision**: Colorblind simulation testing

## Mobile Specifications

### Mobile-First Design Approach
- **Breakpoint Strategy**: Design for 320px width minimum
- **Touch Optimization**: 44px minimum touch targets
- **Gesture Support**: Swipe navigation for event browsing
- **Performance**: Aggressive optimization for slower connections
- **Offline Support**: Basic offline functionality for viewed content

### Mobile-Specific Components
```typescript
// Mobile navigation drawer
const MobileNavigation = () => {
  const [opened, { toggle }] = useDisclosure();
  
  return (
    <>
      <Burger opened={opened} onClick={toggle} hiddenFrom="sm" />
      <Drawer
        opened={opened}
        onClose={toggle}
        size="xs"
        padding="md"
        title="Navigation"
        hiddenFrom="sm"
      >
        <Stack>
          <NavLink label="Events" onClick={toggle} />
          <NavLink label="Members" onClick={toggle} />
          <NavLink label="About" onClick={toggle} />
        </Stack>
      </Drawer>
    </>
  );
};
```

### Progressive Web App Features
- **Service Worker**: Cache critical resources
- **App Manifest**: Installable web app
- **Push Notifications**: Event reminders
- **Offline Indicators**: Network status awareness

## Design Variation Approach

### Five Design Variations Strategy

#### Variation 1: Enhanced Current (Subtle Evolution)
- **Description**: Current design with improved interactions
- **Changes**: 
  - Enhanced hover states and micro-animations
  - Improved button gradients and shadows
  - Refined typography spacing
- **Edginess Level**: 2/5 (minimal increase)
- **Implementation**: CSS enhancements, no component changes

#### Variation 2: Dark Theme Focus (Moderate Change)
- **Description**: Dark-first design with high contrast
- **Changes**:
  - Dark theme as default with light option
  - Dramatic color contrasts
  - Neon accent colors for CTAs
- **Edginess Level**: 3/5 (noticeable alternative feel)
- **Implementation**: Theme configuration changes

#### Variation 3: Geometric Modern (Significant Shift)
- **Description**: Clean geometric patterns with modern typography
- **Changes**:
  - Sharp angles and geometric shapes
  - Sans-serif typography throughout
  - Minimal color palette with bold accents
- **Edginess Level**: 4/5 (distinctly modern)
- **Implementation**: Component style overrides

#### Variation 4: Advanced Mantine (Dramatic Change)
- **Description**: Leverages advanced Mantine components
- **Changes**:
  - Spotlight search integration
  - Advanced data visualization
  - Rich interactive elements
- **Edginess Level**: 4/5 (feature-rich modern)
- **Implementation**: Component library expansion

#### Variation 5: Template-Inspired Ultra-Modern (Revolutionary)
- **Description**: Inspired by analytics dashboard templates
- **Changes**:
  - Dashboard-style layouts throughout
  - Advanced animations and transitions
  - Professional data-driven aesthetic
- **Edginess Level**: 5/5 (completely transformed)
- **Implementation**: Comprehensive redesign

### Variation Selection Process
1. **Stakeholder Review**: Present all 5 variations with interactive prototypes
2. **User Testing**: Community member feedback on usability
3. **Technical Assessment**: Implementation complexity and timeline
4. **Performance Validation**: Ensure all variations meet performance requirements
5. **Final Selection**: Stakeholder decision with refinement round

## Testing Requirements

### Unit Test Coverage
- **Target**: 80% test coverage for UI components
- **Tools**: Jest, React Testing Library
- **Focus Areas**: Component rendering, user interactions, accessibility

### Integration Tests
```typescript
// Example component integration test
import { render, screen, fireEvent } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { ThemeToggle } from '@/components/common/ThemeToggle';

test('theme toggle switches between light and dark', async () => {
  render(
    <MantineProvider>
      <ThemeToggle />
    </MantineProvider>
  );
  
  const toggle = screen.getByRole('button', { name: /toggle theme/i });
  fireEvent.click(toggle);
  
  expect(document.documentElement).toHaveAttribute('data-mantine-color-scheme', 'dark');
});
```

### End-to-End Testing
- **Tool**: Playwright for cross-browser testing
- **Scenarios**: 
  - Navigation flows for different user roles
  - Theme switching across pages
  - Mobile responsive behavior
  - Event browsing and filtering

### Performance Testing
- **Lighthouse**: Automated performance audits
- **Core Web Vitals**: LCP, FID, CLS monitoring
- **Bundle Analysis**: Bundle size impact assessment
- **Memory Usage**: Component memory leak detection

### Accessibility Testing
- **Automated**: axe-core in CI/CD pipeline
- **Manual**: Keyboard navigation verification
- **Screen Reader**: NVDA/JAWS compatibility testing
- **Color Contrast**: Automated contrast ratio validation

## Implementation Phases

### Phase 1: Foundation Setup (Week 1)
- **Mantine v7 Integration**: Install and configure Mantine with custom theme
- **Theme System**: Implement WitchCity color palette and dark/light switching
- **Component Structure**: Set up base component organization
- **Build Pipeline**: Configure Vite for optimized Mantine builds

### Phase 2: Core Components (Week 2)
- **AppShell Layout**: Implement main application shell
- **Navigation System**: Role-based navigation with responsive behavior
- **Theme Toggle**: User preference management
- **Button System**: Standardized button variants and interactions

### Phase 3: Page-Specific Components (Week 3)
- **Homepage**: Hero section and main landing content
- **Login Page**: Authentication form with validation
- **Events Page**: Event listing grid with filtering
- **Common Components**: Cards, forms, loading states

### Phase 4: Design Variations (Week 4)
- **Variation Development**: Create 5 design variations
- **Interactive Prototypes**: Functional prototypes for stakeholder review
- **Performance Optimization**: Ensure all variations meet performance goals
- **Documentation**: Component usage guides and implementation notes

### Phase 5: Refinement & Polish (Week 5)
- **Stakeholder Feedback**: Incorporate selected variation feedback
- **Accessibility Audit**: Complete WCAG 2.1 AA compliance verification
- **Performance Tuning**: Final optimization and bundle size reduction
- **Documentation Completion**: Developer handoff materials

## Migration Requirements

### Current Design Asset Migration
- **Archive Legacy**: Move existing designs to `/docs/_archive/design-legacy-2025-08-20/`
- **Update References**: Fix all documentation links to point to new structure
- **Component Mapping**: Map existing components to new Mantine equivalents
- **Style Migration**: Convert custom CSS to Mantine theme configurations

### Data Migration Considerations
- **User Preferences**: Migrate existing theme preferences to new system
- **Asset URLs**: Update any hardcoded asset references
- **Configuration**: Update environment variables for new theme system
- **Caching**: Clear existing design-related caches

### Backward Compatibility
- **API Contracts**: No changes to existing API contracts required
- **URL Structure**: Maintain existing URL patterns
- **Functionality**: All existing features remain functional
- **User Experience**: Familiar navigation patterns preserved

## Dependencies

### Required Packages
```json
{
  "dependencies": {
    "@mantine/core": "^7.0.0",
    "@mantine/hooks": "^7.0.0",
    "@mantine/form": "^7.0.0",
    "@mantine/notifications": "^7.0.0",
    "@mantine/spotlight": "^7.0.0",
    "@mantine/dates": "^7.0.0",
    "@mantine/carousel": "^7.0.0",
    "@mantine/dropzone": "^7.0.0",
    "@tabler/icons-react": "^2.0.0"
  },
  "devDependencies": {
    "@testing-library/jest-dom": "^6.0.0",
    "@testing-library/react": "^14.0.0",
    "@testing-library/user-event": "^14.0.0",
    "jest-axe": "^8.0.0"
  }
}
```

### External Services
- **CDN**: Static asset delivery optimization
- **Analytics**: Performance monitoring integration
- **Error Tracking**: Component error boundary reporting
- **A11y Testing**: Automated accessibility scanning

### Configuration Needs
```typescript
// Environment variables for theme system
interface ThemeConfig {
  DEFAULT_COLOR_SCHEME: 'auto' | 'light' | 'dark';
  THEME_STORAGE_KEY: string;
  ENABLE_THEME_PERSISTENCE: boolean;
  ANIMATION_DISABLED: boolean; // For reduced motion users
}

// Vite configuration for Mantine optimization
export default defineConfig({
  plugins: [react()],
  define: {
    __MANTINE_VERSION__: JSON.stringify(pkg.dependencies['@mantine/core'])
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'mantine-core': ['@mantine/core'],
          'mantine-extra': ['@mantine/hooks', '@mantine/form', '@mantine/notifications']
        }
      }
    }
  }
});
```

## Acceptance Criteria

### Technical Completion Criteria
- [ ] All Mantine v7 components properly integrated and themed
- [ ] Theme switching functional with user preference persistence
- [ ] Role-based navigation working for all user types
- [ ] All components responsive across mobile, tablet, desktop
- [ ] Performance targets met: <3s load, <200ms interactions
- [ ] Accessibility compliance: WCAG 2.1 AA verified
- [ ] 5 design variations completed with interactive prototypes
- [ ] Testing suite: 80% coverage, E2E tests passing

### Design Quality Criteria
- [ ] Visual consistency across all components and pages
- [ ] Smooth animations and micro-interactions
- [ ] Professional edgy aesthetic appropriate for community
- [ ] Clear information hierarchy and intuitive navigation
- [ ] Cohesive color palette and typography system
- [ ] Mobile-optimized touch interactions

### Documentation Criteria
- [ ] Complete component library documentation
- [ ] Implementation guides for developers
- [ ] Design system guidelines and usage examples
- [ ] Migration guide for legacy design elements
- [ ] Performance optimization recommendations
- [ ] Accessibility implementation patterns

### Stakeholder Approval Criteria
- [ ] Design variations meet "modern, edgy, spicy" direction
- [ ] UX patterns and navigation preserved from current system
- [ ] Community aesthetic authentic to alternative culture
- [ ] Admin interfaces elevated to match public page quality
- [ ] Mobile experience feels native and contemporary
- [ ] Performance maintains or improves current benchmarks

---

*This functional specification provides comprehensive technical guidance for implementing the WitchCityRope design system modernization using Mantine v7 components while respecting the established React+API microservices architecture and leveraging NSwag type generation patterns.*

*Implementation should follow the documented phases with stakeholder review points at design variation completion and final refinement approval.*

*All component development must maintain accessibility standards, performance requirements, and the authentic community culture that makes WitchCityRope unique in Salem's alternative community.*