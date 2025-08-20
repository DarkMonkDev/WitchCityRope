# Design Variation 2: Dark Theme Focus (Moderate Change)
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Design Overview

This variation embraces a dark-first design philosophy with high contrast elements, neon accents, and dramatic lighting effects that amplify the edgy, alternative nature of Salem's rope bondage community. The design maintains excellent UX patterns while significantly enhancing the visual impact.

**Edginess Level**: 3/5 (Noticeable alternative feel)
**Animation Level**: Moderate with neon glow effects and dramatic contrasts
**Implementation Complexity**: Medium - Theme system overhaul with enhanced styling

## Color Palette Revolution

### Dark-First Palette
- **Primary Dark**: #0D1117 (GitHub dark inspired)
- **Secondary Dark**: #161B22 (cards and surfaces)
- **Accent Dark**: #21262D (elevated surfaces)
- **Neon Burgundy**: #FF0A54 (electric version of brand burgundy)
- **Neon Rose**: #FF6B9D (vibrant rose gold)
- **Electric Purple**: #C77DFF (enhanced for dark backgrounds)
- **Cyber Green**: #39FF14 (status indicators and accents)
- **Warning Amber**: #FFB000 (bright amber for CTAs)

### Mantine v7 Dark Theme Configuration
```typescript
const darkFocusTheme = createTheme({
  colors: {
    // Dark theme primary colors
    witchcity: [
      '#FFE8F1', // lightest (rarely used in dark theme)
      '#FFB8D6', // light accent
      '#FF6B9D', // neon rose
      '#FF4785', // bright rose
      '#FF0A54', // neon burgundy (primary in dark)
      '#CC0844', // deeper neon
      '#990633', // burgundy base
      '#660422', // darker burgundy
      '#330211', // very dark
      '#1A0108'  // nearly black
    ],
    // Custom neon accents
    neon: [
      '#F0F9FF', // lightest
      '#E0F2FE', 
      '#BAE6FD',
      '#7DD3FC',
      '#38BDF8', // electric blue
      '#0EA5E9',
      '#0284C7', // primary electric
      '#0369A1',
      '#075985',
      '#0C4A6E'  // darkest
    ],
    // Cyber accents
    cyber: [
      '#F0FDF4', // lightest
      '#DCFCE7',
      '#BBF7D0',
      '#86EFAC',
      '#4ADE80',
      '#22C55E',
      '#16A34A', // cyber green primary
      '#15803D',
      '#166534',
      '#14532D'  // darkest
    ]
  },
  primaryColor: 'witchcity',
  defaultColorScheme: 'dark'
});
```

## Component Specifications

### Dramatic Header & Navigation
- **Background**: Pure black with neon accent borders
- **Logo**: Glowing text effect with subtle animation
- **Navigation**: Neon hover states with glow effects
- **Theme Toggle**: Enhanced with electric transition

#### Implementation
```typescript
import { AppShell, Group, NavLink, ActionIcon, Text, Box } from '@mantine/core';
import { IconSun, IconMoon } from '@tabler/icons-react';

const DarkFocusHeader = () => {
  return (
    <AppShell.Header
      p="md"
      style={{
        backgroundColor: '#0D1117',
        borderBottom: '1px solid #FF0A54',
        boxShadow: '0 0 20px rgba(255, 10, 84, 0.1)'
      }}
    >
      <Group justify="space-between" h="100%">
        <Text 
          size="xl" 
          fw="bold"
          style={{
            background: 'linear-gradient(135deg, #FF0A54 0%, #FF6B9D 50%, #C77DFF 100%)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            color: 'transparent',
            textShadow: '0 0 20px rgba(255, 10, 84, 0.5)',
            animation: 'glow 2s ease-in-out infinite alternate'
          }}
        >
          WITCH CITY ROPE
        </Text>
        
        <Group>
          {['Events & Classes', 'How to Join', 'Resources'].map((item) => (
            <NavLink
              key={item}
              label={item}
              style={{
                color: '#E6E6E6',
                padding: '8px 16px',
                borderRadius: '6px',
                transition: 'all 0.3s ease',
                position: 'relative'
              }}
              styles={{
                root: {
                  '&:hover': {
                    backgroundColor: 'rgba(255, 10, 84, 0.1)',
                    color: '#FF6B9D',
                    boxShadow: '0 0 15px rgba(255, 10, 84, 0.3)',
                    transform: 'translateY(-1px)'
                  }
                }
              }}
            />
          ))}
          
          <Button 
            variant="filled"
            style={{
              background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 100%)',
              border: 'none',
              boxShadow: '0 0 20px rgba(255, 10, 84, 0.4)',
              transition: 'all 0.3s ease'
            }}
            styles={{
              root: {
                '&:hover': {
                  transform: 'translateY(-2px)',
                  boxShadow: '0 0 30px rgba(255, 10, 84, 0.6)',
                  background: 'linear-gradient(135deg, #C77DFF 0%, #FF0A54 100%)'
                }
              }
            }}
          >
            LOGIN
          </Button>
        </Group>
      </Group>
    </AppShell.Header>
  );
};
```

### Cyberpunk Hero Section
- **Background**: Deep black with animated neon grid pattern
- **Typography**: Glowing text effects with electric gradients
- **CTAs**: Neon buttons with pulsing glow animations
- **Visual Elements**: Animated circuit-like patterns

#### Implementation
```typescript
import { Container, Title, Text, Button, Group, Box } from '@mantine/core';

const CyberpunkHero = () => (
  <Box
    style={{
      backgroundColor: '#0D1117',
      minHeight: '90vh',
      display: 'flex',
      alignItems: 'center',
      position: 'relative',
      overflow: 'hidden'
    }}
  >
    {/* Animated grid background */}
    <Box
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        background: `
          linear-gradient(rgba(255, 10, 84, 0.03) 1px, transparent 1px),
          linear-gradient(90deg, rgba(255, 10, 84, 0.03) 1px, transparent 1px)
        `,
        backgroundSize: '50px 50px',
        animation: 'grid-move 20s linear infinite'
      }}
    />
    
    {/* Neon accent circles */}
    <Box
      style={{
        position: 'absolute',
        top: '20%',
        right: '10%',
        width: '300px',
        height: '300px',
        borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(199, 125, 255, 0.1) 0%, transparent 70%)',
        filter: 'blur(40px)'
      }}
    />
    
    <Box
      style={{
        position: 'absolute',
        bottom: '20%',
        left: '5%',
        width: '250px',
        height: '250px',
        borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(255, 107, 157, 0.1) 0%, transparent 70%)',
        filter: 'blur(30px)'
      }}
    />
    
    <Container size="xl" ta="center" style={{ position: 'relative', zIndex: 1 }}>
      <Text 
        size="xl" 
        mb="md"
        style={{ 
          color: '#FF6B9D',
          fontFamily: 'monospace',
          fontWeight: 500,
          textTransform: 'uppercase',
          letterSpacing: '2px',
          textShadow: '0 0 10px rgba(255, 107, 157, 0.5)'
        }}
      >
        &gt; WHERE_CURIOSITY_MEETS_CONNECTION
      </Text>
      
      <Title
        size="4.5rem"
        fw="bold"
        mb="lg"
        style={{
          background: 'linear-gradient(135deg, #FFFFFF 0%, #FF6B9D 50%, #C77DFF 100%)',
          backgroundClip: 'text',
          WebkitBackgroundClip: 'text',
          color: 'transparent',
          textShadow: '0 0 30px rgba(255, 255, 255, 0.2)',
          lineHeight: 1.1,
          letterSpacing: '-2px'
        }}
      >
        SALEM'S<br />
        <Text 
          component="span" 
          inherit
          style={{
            background: 'linear-gradient(135deg, #FF0A54 0%, #FFB000 100%)',
            backgroundClip: 'text',
            WebkitBackgroundClip: 'text',
            color: 'transparent',
            textShadow: '0 0 30px rgba(255, 10, 84, 0.4)'
          }}
        >
          ROPE BONDAGE
        </Text><br />
        COMMUNITY
      </Title>
      
      <Text 
        size="lg" 
        mb="xl" 
        maw={900} 
        mx="auto"
        style={{
          color: '#B3B3B3',
          lineHeight: 1.7,
          fontSize: '1.2rem'
        }}
      >
        Join 600+ members in Salem's most authentic alternative community—
        where consent, creativity, and connection create transformative experiences
      </Text>
      
      <Group justify="center" gap="lg">
        <Button
          size="xl"
          style={{
            background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 100%)',
            border: 'none',
            boxShadow: '0 0 30px rgba(255, 10, 84, 0.4)',
            fontSize: '1.1rem',
            fontWeight: 'bold',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            animation: 'pulse-glow 2s ease-in-out infinite alternate'
          }}
          styles={{
            root: {
              '&:hover': {
                transform: 'translateY(-3px) scale(1.02)',
                boxShadow: '0 0 40px rgba(255, 10, 84, 0.6)',
                background: 'linear-gradient(135deg, #C77DFF 0%, #FF0A54 100%)'
              }
            }
          }}
        >
          ENTER THE COMMUNITY
        </Button>
        
        <Button
          size="xl"
          variant="outline"
          style={{
            borderColor: '#39FF14',
            color: '#39FF14',
            backgroundColor: 'rgba(57, 255, 20, 0.05)',
            fontSize: '1.1rem',
            fontWeight: 'bold',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            boxShadow: '0 0 20px rgba(57, 255, 20, 0.2)'
          }}
          styles={{
            root: {
              '&:hover': {
                backgroundColor: 'rgba(57, 255, 20, 0.1)',
                transform: 'translateY(-2px)',
                boxShadow: '0 0 30px rgba(57, 255, 20, 0.4)'
              }
            }
          }}
        >
          LEARN MORE
        </Button>
      </Group>
    </Container>
  </Box>
);
```

### Neon Event Cards
- **Background**: Dark cards with glowing borders
- **Headers**: Electric gradient backgrounds
- **Badges**: Neon-colored member level indicators
- **Hover Effects**: Dramatic glow and elevation

#### Implementation
```typescript
import { Card, Badge, Text, Group, Box } from '@mantine/core';

const NeonEventCard = ({ event }) => (
  <Card
    radius="lg"
    p="lg"
    style={{
      backgroundColor: '#161B22',
      border: '1px solid #30363D',
      cursor: 'pointer',
      transition: 'all 0.4s ease',
      position: 'relative',
      overflow: 'hidden'
    }}
    styles={{
      root: {
        '&:hover': {
          transform: 'translateY(-8px) rotateX(5deg)',
          boxShadow: '0 20px 40px rgba(255, 10, 84, 0.2)',
          borderColor: '#FF0A54',
          '&::before': {
            opacity: 1
          }
        },
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: 'linear-gradient(135deg, rgba(255, 10, 84, 0.1) 0%, rgba(199, 125, 255, 0.1) 100%)',
          opacity: 0,
          transition: 'opacity 0.4s ease',
          zIndex: 0
        }
      }
    }}
  >
    {/* Neon header with electric effect */}
    <Box
      h={120}
      mb="md"
      style={{
        background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 50%, #39FF14 100%)',
        borderRadius: '8px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        overflow: 'hidden',
        boxShadow: '0 0 20px rgba(255, 10, 84, 0.3)'
      }}
    >
      {/* Electric circuit pattern */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: `url("data:image/svg+xml,${encodeURIComponent(`
            <svg width='100' height='100' viewBox='0 0 100 100' xmlns='http://www.w3.org/2000/svg'>
              <path d='M10,50 L30,50 L35,30 L40,70 L45,50 L90,50' stroke='rgba(255,255,255,0.3)' stroke-width='1' fill='none'/>
              <circle cx='35' cy='30' r='2' fill='rgba(255,255,255,0.5)'/>
              <circle cx='40' cy='70' r='2' fill='rgba(255,255,255,0.5)'/>
            </svg>
          `)}")`,
          backgroundSize: '80px 80px',
          animation: 'circuit-pulse 3s ease-in-out infinite'
        }}
      />
      
      <Text 
        size="xl" 
        fw="bold" 
        c="white" 
        ta="center"
        style={{ 
          position: 'relative', 
          zIndex: 1,
          textShadow: '0 0 10px rgba(0, 0, 0, 0.5)',
          textTransform: 'uppercase',
          letterSpacing: '1px'
        }}
      >
        {event.title}
      </Text>
    </Box>
    
    <Group justify="space-between" mb="xs" style={{ position: 'relative', zIndex: 1 }}>
      <Text 
        fw={600} 
        size="sm"
        style={{
          color: '#FF6B9D',
          textTransform: 'uppercase',
          letterSpacing: '0.5px',
          fontFamily: 'monospace'
        }}
      >
        {event.date}
      </Text>
      <Badge
        variant="filled"
        color={event.requiresVetting ? 'red' : 'cyan'}
        size="sm"
        style={{
          backgroundColor: event.requiresVetting ? '#FF0A54' : '#39FF14',
          color: event.requiresVetting ? 'white' : '#000',
          boxShadow: `0 0 10px ${event.requiresVetting ? 'rgba(255, 10, 84, 0.5)' : 'rgba(57, 255, 20, 0.5)'}`,
          textTransform: 'uppercase',
          fontWeight: 'bold',
          letterSpacing: '0.5px'
        }}
      >
        {event.requiresVetting ? 'VETTED' : 'OPEN'}
      </Badge>
    </Group>
    
    <Text 
      size="sm" 
      mb="lg" 
      lineClamp={3}
      style={{ 
        color: '#C9D1D9',
        lineHeight: 1.6,
        position: 'relative',
        zIndex: 1
      }}
    >
      {event.description}
    </Text>
    
    <Group justify="space-between" align="center" style={{ position: 'relative', zIndex: 1 }}>
      <Text 
        fw="bold"
        style={{
          color: '#FFB000',
          fontSize: '1.1rem',
          textShadow: '0 0 8px rgba(255, 176, 0, 0.5)'
        }}
      >
        {event.price}
      </Text>
      <Text 
        size="sm" 
        fw={600}
        style={{
          color: event.spotsLeft > 5 ? '#39FF14' : event.spotsLeft > 0 ? '#FFB000' : '#FF0A54',
          textShadow: `0 0 8px ${
            event.spotsLeft > 5 ? 'rgba(57, 255, 20, 0.5)' : 
            event.spotsLeft > 0 ? 'rgba(255, 176, 0, 0.5)' : 
            'rgba(255, 10, 84, 0.5)'
          }`,
          textTransform: 'uppercase',
          letterSpacing: '0.5px'
        }}
      >
        {event.spotsLeft > 0 ? `${event.spotsLeft} SPOTS LEFT` : 'FULL'}
      </Text>
    </Group>
  </Card>
);
```

## Advanced Animation System

### Neon Glow Animations
```css
@keyframes glow {
  0% { text-shadow: 0 0 20px rgba(255, 10, 84, 0.5); }
  100% { text-shadow: 0 0 30px rgba(255, 10, 84, 0.8), 0 0 40px rgba(199, 125, 255, 0.3); }
}

@keyframes pulse-glow {
  0% { 
    box-shadow: 0 0 30px rgba(255, 10, 84, 0.4);
    transform: scale(1);
  }
  100% { 
    box-shadow: 0 0 40px rgba(255, 10, 84, 0.6), 0 0 60px rgba(199, 125, 255, 0.2);
    transform: scale(1.02);
  }
}

@keyframes grid-move {
  0% { transform: translate(0, 0); }
  100% { transform: translate(50px, 50px); }
}

@keyframes circuit-pulse {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 0.7; }
}
```

### Interactive Micro-Animations
- **Button Hover**: Dramatic glow with color inversions
- **Card Hover**: 3D perspective with neon border glow
- **Text Effects**: Subtle typing animations for code-style text
- **Loading States**: Electric progress bars with neon trails

## Dark Theme Specifications

### Color Psychology
- **Black Backgrounds**: Creates focus and drama
- **Neon Accents**: High energy and alternative culture
- **Electric Colors**: Cyberpunk aesthetic matching rope community edge
- **High Contrast**: Ensures accessibility while maintaining drama

### Theme Implementation
```typescript
const darkFocusConfig = {
  colorScheme: 'dark',
  colors: {
    dark: [
      '#C9D1D9', // text primary
      '#B3B3B3', // text secondary  
      '#8B949E', // text dimmed
      '#6E7681', // borders
      '#484F58', // hover backgrounds
      '#30363D', // surface backgrounds
      '#21262D', // elevated surfaces
      '#161B22', // card backgrounds
      '#0D1117', // page background
      '#010409'  // deepest black
    ]
  },
  primaryShade: { light: 6, dark: 4 },
  defaultRadius: 'md'
};
```

## Mobile Responsive Enhancements

### Touch-Optimized Interactions
- **Larger Touch Targets**: 48px minimum for all interactive elements
- **Gesture Support**: Swipe gestures for event browsing
- **Haptic Feedback**: Visual feedback for touch interactions
- **Performance**: Optimized animations for mobile GPUs

### Mobile-Specific Adaptations
```typescript
const mobileEnhancements = {
  '@media (max-width: 768px)': {
    // Reduced glow effects for performance
    '*[data-glow]': {
      textShadow: '0 0 10px currentColor',
      boxShadow: '0 0 15px rgba(255, 10, 84, 0.3) !important'
    },
    
    // Simpler animations
    '@keyframes pulse-glow': {
      '0%': { transform: 'scale(1)' },
      '100%': { transform: 'scale(1.05)' }
    },
    
    // Touch-friendly spacing
    '.mantine-Button-root': {
      minHeight: '48px',
      padding: '12px 24px'
    }
  }
};
```

## Performance Considerations

### GPU Acceleration
- **Transform-Only Animations**: Using transform3d for hardware acceleration
- **Layer Management**: Strategic use of will-change property
- **Composite Layers**: Optimized for 60fps animations
- **Reduced Motion**: Respects user preferences

### Bundle Optimization
```typescript
// Selective Mantine imports
import { Button } from '@mantine/core';
import { useColorScheme } from '@mantine/hooks';
// Avoid importing entire library

// CSS-in-JS optimization
const styles = createStyles((theme) => ({
  glowButton: {
    // Styles only applied when needed
    '&:hover': {
      boxShadow: `0 0 30px ${theme.colors.witchcity[4]}`
    }
  }
}));
```

## Accessibility in Dark Theme

### High Contrast Compliance
- **Text Contrast**: 7:1 ratio for enhanced readability
- **Focus Indicators**: Bright neon outlines for keyboard navigation
- **Motion Control**: All animations respect prefers-reduced-motion
- **Color Independence**: No information conveyed by color alone

### Screen Reader Optimization
```typescript
const accessibilityProps = {
  'aria-label': 'Neon-styled navigation button',
  'aria-describedby': 'button-description',
  role: 'button',
  tabIndex: 0
};

// Reduced motion alternative
const useReducedMotion = () => {
  return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
};
```

## Implementation Strategy

### Phase 1: Theme Foundation
1. **Dark Color System**: Implement enhanced dark theme palette
2. **Neon Accents**: Add electric color variations
3. **Animation Base**: Set up glow and pulse keyframes
4. **Component Themes**: Apply dark styling to all Mantine components

### Phase 2: Interactive Enhancement
1. **Hover Effects**: Implement dramatic glow animations
2. **Card Transforms**: Add 3D perspective effects
3. **Button Gradients**: Create electric gradient buttons
4. **Text Effects**: Add glowing text treatments

### Phase 3: Performance Optimization
1. **Animation Performance**: Optimize for 60fps on all devices
2. **Bundle Size**: Tree-shake unused effects
3. **Mobile Adaptation**: Reduce effects for mobile performance
4. **Accessibility**: Ensure all effects have reduced-motion alternatives

This dark theme variation significantly enhances the edgy, alternative aesthetic while maintaining the excellent UX patterns. The neon accents and dramatic contrasts create a distinctive visual identity that authentically represents Salem's rope bondage community while leveraging Mantine v7's theming capabilities.