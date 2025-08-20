# Login Variation 2: Dark Theme Focus (Moderate Change)
<!-- Last Updated: 2025-08-20 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Complete -->

## Design Overview

This login variation embraces a dark-first design philosophy with high contrast elements, neon accents, and dramatic lighting effects that amplify the edgy, alternative nature of Salem's rope bondage community. The design maintains excellent UX patterns while significantly enhancing the visual impact through cyberpunk-inspired aesthetics.

**Edginess Level**: 3/5 (Noticeable alternative feel)
**Homepage Alignment**: Variation 2 - Dark Theme Focus
**Implementation Complexity**: Medium - Theme system overhaul with enhanced styling

## Visual Design Specifications

### Dark-First Color Palette
- **Primary Dark**: #0D1117 (GitHub dark inspired - main background)
- **Secondary Dark**: #161B22 (cards and elevated surfaces)
- **Accent Dark**: #21262D (hover states and borders)
- **Neon Burgundy**: #FF0A54 (electric version of brand burgundy)
- **Neon Rose**: #FF6B9D (vibrant rose gold)
- **Electric Purple**: #C77DFF (enhanced for dark backgrounds)
- **Cyber Green**: #39FF14 (status indicators and accents)
- **Warning Amber**: #FFB000 (bright amber for secondary CTAs)

### Typography System
- **Brand Title**: Neon gradient with glow effects and monospace fonts
- **Form Labels**: Uppercase monospace with cyberpunk terminology
- **Input Text**: Monospace font for terminal aesthetic
- **Helper Text**: Dimmed neon colors with uppercase formatting

### Card Design
- **Background**: Deep dark (#161B22) with glowing border
- **Border**: Animated neon border with gradient colors
- **Shadow**: Dramatic glow effects with color casting
- **Border Radius**: Medium radius (12px) with sharp corners
- **Hover Effect**: Intensified glow and subtle elevation

## Layout Structure

### Desktop Layout (1024px+)
```
                    Dark Background with Grid Pattern
    ┌─────────────────────────────────────────────────────────┐
    │ ████ ████ ████ ████ ████ ████ ████ ████ ████ ████ ████ │
    │ ████     ┌─────────────────┐ Floating    ████ ████ ████ │
    │ ████     │ ╔═══════════╗   │ Neon Orbs   ████ ████ ████ │
    │ ████     │ ║ WITCH     ║   │             ████ ████ ████ │
    │ ████     │ ║ CITY ROPE ║   │    ◯        ████ ████ ████ │
    │ ████     │ ╚═══════════╝   │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     │ > ACCESS_TERM   │             ████ ████ ████ │
    │ ████     │                 │   ◯         ████ ████ ████ │
    │ ████     │ [NEON EMAIL]    │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     │ [NEON PASS]     │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     │ ◉ REMEMBER      │             ████ ████ ████ │
    │ ████     │   > RESET_PASS  │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     │ [AUTHENTICATE]  │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     │ > CREATE_ACCT   │             ████ ████ ████ │
    │ ████     │                 │             ████ ████ ████ │
    │ ████     └─────────────────┘             ████ ████ ████ │
    │ ████ ████ ████ ████ ████ ████ ████ ████ ████ ████ ████ │
    └─────────────────────────────────────────────────────────┘
```

### Mobile Layout (320px-768px)
```
    Dark Background - Full Screen
    ┌─────────────────────────────────────┐
    │ ████ ████ ████ ████ ████ ████ ████ │
    │ ┌─────────────────────────────────┐ │
    │ │ ╔═════════════════════════════╗ │ │
    │ │ ║      WITCH CITY ROPE        ║ │ │
    │ │ ╚═════════════════════════════╝ │ │
    │ │                                 │ │
    │ │      > ACCESS_TERMINAL          │ │
    │ │                                 │ │
    │ │  [EMAIL_ADDRESS - Full Width]   │ │
    │ │                                 │ │
    │ │  [PASSWORD_HASH - Full Width]   │ │
    │ │                                 │ │
    │ │  ◉ REMEMBER_SESSION             │ │
    │ │                > RESET_PASSWORD │ │
    │ │                                 │ │
    │ │  [AUTHENTICATE - Full Width]    │ │
    │ │                                 │ │
    │ │       > CREATE_ACCOUNT          │ │
    │ │                                 │ │
    │ └─────────────────────────────────┘ │
    │ ████ ████ ████ ████ ████ ████ ████ │
    └─────────────────────────────────────┘
```

## Component Specifications

### Animated Background System
```typescript
const DarkBackground = () => (
  <Box
    style={{
      backgroundColor: '#0D1117',
      minHeight: '100vh',
      position: 'relative',
      overflow: 'hidden'
    }}
  >
    {/* Animated grid pattern */}
    <Box
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        background: `
          linear-gradient(rgba(255, 10, 84, 0.05) 1px, transparent 1px),
          linear-gradient(90deg, rgba(255, 10, 84, 0.05) 1px, transparent 1px)
        `,
        backgroundSize: '40px 40px',
        animation: 'grid-move 20s linear infinite'
      }}
    />

    {/* Floating neon orbs */}
    <Box
      style={{
        position: 'absolute',
        top: '20%',
        right: '15%',
        width: '200px',
        height: '200px',
        borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(199, 125, 255, 0.1) 0%, transparent 70%)',
        filter: 'blur(40px)',
        animation: 'float 6s ease-in-out infinite'
      }}
    />
    
    <Box
      style={{
        position: 'absolute',
        bottom: '25%',
        left: '10%',
        width: '150px',
        height: '150px',
        borderRadius: '50%',
        background: 'radial-gradient(circle, rgba(255, 107, 157, 0.08) 0%, transparent 70%)',
        filter: 'blur(30px)',
        animation: 'float 8s ease-in-out infinite reverse'
      }}
    />
  </Box>
);
```

### Neon Card with Animated Border
```typescript
const NeonLoginCard = ({ children }) => (
  <Card
    radius="lg"
    padding="xl"
    style={{
      backgroundColor: '#161B22',
      border: '1px solid #FF0A54',
      boxShadow: '0 0 30px rgba(255, 10, 84, 0.2)',
      maxWidth: 420,
      margin: '0 auto',
      position: 'relative',
      overflow: 'hidden'
    }}
  >
    {/* Animated border glow */}
    <Box
      style={{
        position: 'absolute',
        top: -2,
        left: -2,
        right: -2,
        bottom: -2,
        background: 'linear-gradient(45deg, #FF0A54, #C77DFF, #39FF14, #FF0A54)',
        borderRadius: 'inherit',
        zIndex: -1,
        animation: 'border-glow 3s ease-in-out infinite',
        backgroundSize: '300% 300%'
      }}
    />
    
    {/* Content overlay */}
    <Box
      style={{
        position: 'relative',
        zIndex: 1,
        backgroundColor: '#161B22',
        borderRadius: 'calc(var(--mantine-radius-lg) - 2px)',
        padding: 'inherit'
      }}
    >
      {children}
    </Box>
  </Card>
);
```

### Cyberpunk Header
```typescript
<Box ta="center" mb="xl">
  <Title
    order={2}
    size="2rem"
    fw="bold"
    mb="xs"
    style={{
      background: 'linear-gradient(135deg, #FF0A54 0%, #FF6B9D 50%, #C77DFF 100%)',
      backgroundClip: 'text',
      WebkitBackgroundClip: 'text',
      color: 'transparent',
      textShadow: '0 0 20px rgba(255, 10, 84, 0.5)',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      letterSpacing: '3px',
      textTransform: 'uppercase',
      animation: 'text-glow 2s ease-in-out infinite alternate'
    }}
  >
    WITCH CITY ROPE
  </Title>
  <Text 
    size="lg" 
    fw={600}
    style={{
      color: '#FF6B9D',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      textTransform: 'uppercase',
      letterSpacing: '2px',
      fontSize: '0.9rem'
    }}
  >
    &gt; ACCESS_TERMINAL
  </Text>
</Box>
```

### Terminal-Style Form Inputs
```typescript
// Email Input with Neon Styling
<Box mb="md">
  <Text 
    size="xs" 
    fw="bold" 
    mb="xs"
    style={{
      color: '#C77DFF',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      fontSize: '0.8rem',
      fontWeight: 'bold',
      textTransform: 'uppercase',
      letterSpacing: '1px'
    }}
  >
    EMAIL_ADDRESS
  </Text>
  <TextInput
    placeholder="user@domain.com"
    size="md"
    {...form.getInputProps('email')}
    styles={{
      input: {
        backgroundColor: '#0D1117',
        borderColor: '#30363D',
        color: '#C9D1D9',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        fontSize: '0.9rem',
        padding: '12px 16px',
        transition: 'all 0.3s ease',
        '&:focus': {
          borderColor: '#FF0A54',
          boxShadow: '0 0 10px rgba(255, 10, 84, 0.3), inset 0 0 5px rgba(255, 10, 84, 0.1)',
          backgroundColor: '#161B22'
        },
        '&::placeholder': {
          color: '#8B949E',
          fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace'
        },
        '&:hover': {
          borderColor: '#484F58',
          boxShadow: '0 0 5px rgba(255, 10, 84, 0.1)'
        }
      },
      error: {
        color: '#FF6B9D',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        fontSize: '0.7rem',
        textTransform: 'uppercase'
      }
    }}
  />
</Box>

// Password Input with Terminal Styling
<Box mb="lg">
  <Text 
    size="xs" 
    fw="bold" 
    mb="xs"
    style={{
      color: '#C77DFF',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      fontSize: '0.8rem',
      fontWeight: 'bold',
      textTransform: 'uppercase',
      letterSpacing: '1px'
    }}
  >
    PASSWORD_HASH
  </Text>
  <PasswordInput
    placeholder="••••••••••••"
    size="md"
    {...form.getInputProps('password')}
    styles={{
      input: {
        backgroundColor: '#0D1117',
        borderColor: '#30363D',
        color: '#C9D1D9',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        fontSize: '0.9rem',
        padding: '12px 16px',
        transition: 'all 0.3s ease',
        '&:focus': {
          borderColor: '#FF0A54',
          boxShadow: '0 0 10px rgba(255, 10, 84, 0.3), inset 0 0 5px rgba(255, 10, 84, 0.1)',
          backgroundColor: '#161B22'
        }
      },
      visibilityToggle: {
        color: '#8B949E',
        '&:hover': {
          color: '#FF6B9D'
        }
      }
    }}
  />
</Box>
```

### Cyberpunk Secondary Actions
```typescript
<Group justify="space-between" mb="xl">
  <Checkbox
    label="REMEMBER_SESSION"
    color="red"
    {...form.getInputProps('rememberMe', { type: 'checkbox' })}
    styles={{
      label: {
        color: '#39FF14',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        fontSize: '0.8rem',
        textTransform: 'uppercase',
        letterSpacing: '0.5px'
      },
      input: {
        backgroundColor: '#0D1117',
        borderColor: '#30363D',
        '&:checked': {
          backgroundColor: '#39FF14',
          borderColor: '#39FF14',
          boxShadow: '0 0 8px rgba(57, 255, 20, 0.4)'
        }
      }
    }}
  />
  <Anchor 
    size="xs" 
    href="/forgot-password"
    style={{
      color: '#FFB000',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      textDecoration: 'none',
      textTransform: 'uppercase',
      letterSpacing: '0.5px',
      fontSize: '0.8rem',
      transition: 'all 0.2s ease',
      '&:hover': {
        color: '#FFC547',
        textShadow: '0 0 8px rgba(255, 176, 0, 0.5)'
      }
    }}
  >
    &gt; RESET_PASSWORD
  </Anchor>
</Group>
```

### Electric Authentication Button
```typescript
<Button
  type="submit"
  size="lg"
  fullWidth
  mb="md"
  style={{
    background: 'linear-gradient(135deg, #FF0A54 0%, #C77DFF 100%)',
    border: 'none',
    boxShadow: '0 0 20px rgba(255, 10, 84, 0.4)',
    fontSize: '1rem',
    fontWeight: 'bold',
    textTransform: 'uppercase',
    letterSpacing: '3px',
    fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
    height: '52px',
    position: 'relative',
    overflow: 'hidden'
  }}
  styles={{
    root: {
      transition: 'all 0.3s ease',
      '&:hover': {
        transform: 'translateY(-3px)',
        boxShadow: '0 0 30px rgba(255, 10, 84, 0.6), 0 0 60px rgba(199, 125, 255, 0.3)',
        background: 'linear-gradient(135deg, #C77DFF 0%, #FF0A54 100%)'
      },
      '&::before': {
        content: '""',
        position: 'absolute',
        top: 0,
        left: '-100%',
        width: '100%',
        height: '100%',
        background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent)',
        transition: 'left 0.5s ease'
      },
      '&:hover::before': {
        left: '100%'
      }
    }
  }}
  loading={loading}
  loaderProps={{ color: 'white' }}
>
  {loading ? 'AUTHENTICATING...' : '&gt; AUTHENTICATE'}
</Button>
```

### Terminal Registration Link
```typescript
<Text 
  ta="center" 
  size="sm"
  style={{
    color: '#8B949E',
    fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    fontSize: '0.8rem'
  }}
>
  &gt; <Anchor 
    style={{
      color: '#39FF14',
      textDecoration: 'none',
      transition: 'all 0.2s ease',
      '&:hover': {
        color: '#4AFF20',
        textShadow: '0 0 8px rgba(57, 255, 20, 0.5)'
      }
    }} 
    href="/register"
  >
    CREATE_ACCOUNT
  </Anchor>
</Text>
```

## Advanced Animation System

### CSS Keyframe Animations
```css
@keyframes grid-move {
  0% { transform: translate(0, 0); }
  100% { transform: translate(40px, 40px); }
}

@keyframes float {
  0%, 100% { 
    transform: translateY(0px) rotate(0deg); 
    opacity: 0.7;
  }
  50% { 
    transform: translateY(-20px) rotate(180deg); 
    opacity: 1;
  }
}

@keyframes border-glow {
  0% { 
    background-position: 0% 50%;
    box-shadow: 0 0 20px rgba(255, 10, 84, 0.3);
  }
  50% { 
    background-position: 100% 50%;
    box-shadow: 0 0 30px rgba(199, 125, 255, 0.4);
  }
  100% { 
    background-position: 0% 50%;
    box-shadow: 0 0 20px rgba(255, 10, 84, 0.3);
  }
}

@keyframes text-glow {
  0% { 
    text-shadow: 0 0 20px rgba(255, 10, 84, 0.5);
    filter: brightness(1);
  }
  100% { 
    text-shadow: 0 0 30px rgba(255, 10, 84, 0.8), 0 0 40px rgba(199, 125, 255, 0.3);
    filter: brightness(1.1);
  }
}

@keyframes pulse-glow {
  0% { 
    box-shadow: 0 0 20px rgba(255, 10, 84, 0.4);
    transform: scale(1);
  }
  100% { 
    box-shadow: 0 0 30px rgba(255, 10, 84, 0.6), 0 0 60px rgba(199, 125, 255, 0.2);
    transform: scale(1.02);
  }
}

/* Circuit animation for loading states */
@keyframes circuit-pulse {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 0.8; }
}

/* Shimmer effect for buttons */
@keyframes shimmer {
  0% { left: -100%; }
  100% { left: 100%; }
}
```

### Interactive Micro-Animations
```typescript
const useNeonEffects = () => {
  const [isHovered, setIsHovered] = useState(false);
  const [isFocused, setIsFocused] = useState(false);

  const neonGlowStyle = {
    transition: 'all 0.3s ease',
    boxShadow: isHovered || isFocused 
      ? '0 0 15px rgba(255, 10, 84, 0.4), 0 0 30px rgba(199, 125, 255, 0.2)'
      : '0 0 5px rgba(255, 10, 84, 0.1)',
  };

  return {
    neonGlowStyle,
    setIsHovered,
    setIsFocused
  };
};
```

## Responsive Design Adaptations

### Mobile Performance Optimization
```css
/* Reduce glow effects on mobile for performance */
@media (max-width: 768px) {
  *[data-glow] {
    text-shadow: 0 0 10px currentColor !important;
    box-shadow: 0 0 15px rgba(255, 10, 84, 0.3) !important;
    animation: none !important;
  }
  
  /* Simpler animations */
  @keyframes pulse-glow {
    0% { transform: scale(1); }
    100% { transform: scale(1.05); }
  }
  
  /* Disable floating orbs on mobile */
  .floating-orb {
    display: none;
  }
  
  /* Reduce grid opacity */
  .grid-background {
    opacity: 0.5;
  }
}

/* Touch-friendly sizing */
@media (max-width: 768px) {
  .mantine-Button-root {
    min-height: 52px;
    padding: 16px 24px;
    font-size: 0.9rem;
  }
  
  .mantine-TextInput-input,
  .mantine-PasswordInput-input {
    min-height: 48px;
    padding: 14px 16px;
    font-size: 1rem;
  }
}
```

### Breakpoint-Specific Layouts
```typescript
const mobileStyles = {
  '@media (max-width: 768px)': {
    '.login-container': {
      padding: '1rem',
      height: '100vh'
    },
    '.login-card': {
      margin: '2rem 0',
      width: '100%',
      maxWidth: 'none'
    },
    '.neon-title': {
      fontSize: '1.5rem',
      letterSpacing: '1px'
    }
  }
};
```

## Accessibility in Dark Theme

### High Contrast Mode Support
```css
@media (prefers-contrast: high) {
  .dark-theme {
    --neon-primary: #FFFFFF;
    --neon-secondary: #FFFF00;
    --background-primary: #000000;
    --text-primary: #FFFFFF;
  }
  
  .glow-effect {
    text-shadow: none !important;
    box-shadow: 2px 2px 0 #FFFFFF !important;
  }
}
```

### Screen Reader Optimization
```typescript
const accessibilityProps = {
  // Live region for status updates
  'aria-live': 'polite',
  'aria-atomic': true,
  
  // Descriptive labels for terminal styling
  'aria-label': 'Login form with cyberpunk terminal interface',
  'aria-describedby': 'login-description',
  
  // Skip decorative elements
  'aria-hidden': true, // For animated background elements
  
  // Enhanced focus management
  role: 'main',
  tabIndex: -1
};

// Screen reader announcements for animations
const announceStateChange = (state) => {
  const announcement = document.getElementById('sr-announcements');
  announcement.textContent = `Login ${state}`;
};
```

### Color Independence
```typescript
// Ensure no information is conveyed by color alone
const statusIndicators = {
  success: {
    color: '#39FF14',
    icon: '✓',
    text: 'Success'
  },
  error: {
    color: '#FF0A54',
    icon: '✗',
    text: 'Error'
  },
  warning: {
    color: '#FFB000',
    icon: '⚠',
    text: 'Warning'
  }
};
```

## Performance Considerations

### GPU Acceleration Optimization
```css
.gpu-accelerated {
  transform: translate3d(0, 0, 0);
  will-change: transform, box-shadow;
  backface-visibility: hidden;
}

/* Remove will-change after animations */
.animation-complete {
  will-change: auto;
}
```

### Bundle Size Management
```typescript
// Lazy load complex animations
const NeonEffects = lazy(() => import('./components/NeonEffects'));

// Conditional loading based on device capabilities
const shouldLoadAdvancedEffects = () => {
  return (
    !window.matchMedia('(prefers-reduced-motion: reduce)').matches &&
    navigator.hardwareConcurrency >= 4 &&
    !window.matchMedia('(max-width: 768px)').matches
  );
};
```

## Form Validation with Terminal Aesthetics

### Custom Error Display
```typescript
const TerminalError = ({ error }) => (
  <Text
    size="xs"
    style={{
      color: '#FF6B9D',
      fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
      backgroundColor: 'rgba(255, 107, 157, 0.1)',
      padding: '4px 8px',
      borderRadius: '4px',
      border: '1px solid rgba(255, 107, 157, 0.3)',
      marginTop: '4px',
      textTransform: 'uppercase',
      letterSpacing: '0.5px'
    }}
  >
    ERROR: {error}
  </Text>
);
```

### Success States with Neon Effects
```typescript
const TerminalSuccess = () => {
  notifications.show({
    title: '> ACCESS_GRANTED',
    message: '> REDIRECTING_TO_DASHBOARD...',
    color: 'green',
    styles: {
      root: {
        backgroundColor: '#161B22',
        borderColor: '#39FF14',
        color: '#39FF14'
      },
      title: {
        color: '#39FF14',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        textTransform: 'uppercase'
      },
      description: {
        color: '#8B949E',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace'
      }
    }
  });
};
```

## Security Features Enhanced

### Rate Limiting Visualization
```typescript
const SecurityIndicator = ({ attempts }) => (
  <Box
    style={{
      marginTop: '1rem',
      padding: '0.5rem',
      backgroundColor: 'rgba(255, 176, 0, 0.1)',
      border: '1px solid rgba(255, 176, 0, 0.3)',
      borderRadius: '4px'
    }}
  >
    <Text
      size="xs"
      style={{
        color: '#FFB000',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        textTransform: 'uppercase'
      }}
    >
      SECURITY_LEVEL: {attempts > 2 ? 'HIGH' : 'NORMAL'}
    </Text>
  </Box>
);
```

### CAPTCHA Integration with Dark Theme
```typescript
const DarkCaptcha = () => (
  <Box
    style={{
      backgroundColor: '#0D1117',
      border: '1px solid #30363D',
      borderRadius: '8px',
      padding: '1rem',
      marginTop: '1rem'
    }}
  >
    <Text
      size="sm"
      mb="md"
      style={{
        color: '#C77DFF',
        fontFamily: 'Monaco, "Cascadia Code", "Roboto Mono", monospace',
        textTransform: 'uppercase'
      }}
    >
      SECURITY_VERIFICATION
    </Text>
    {/* CAPTCHA component would go here */}
  </Box>
);
```

This Dark Theme Focus login variation significantly enhances the edgy, alternative aesthetic while maintaining excellent UX patterns. The cyberpunk-inspired design with neon accents and dramatic contrasts creates a distinctive visual identity that authentically represents Salem's rope bondage community's alternative culture while leveraging Mantine v7's theming capabilities for consistent, professional implementation.