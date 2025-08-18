# Design D: Neon Ripple Spotlight
<!-- Last Updated: 2025-08-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## Design Overview

An innovative form field highlighting system that combines multiple dynamic effects: neon glow pulses, expanding ripple animations from the point of interaction, and a spotlight effect that dims surrounding fields while intensifying the active one. This creates an immersive, sci-fi inspired experience perfect for WitchCityRope's mystical Salem aesthetic.

## Visual Characteristics

### Core Effect System
- **Resting State**: Subtle border with minimal glow
- **Hover State**: Gentle neon pulse begins
- **Click/Focus**: Ripple emanates from click point with spotlight activation
- **Active State**: Continuous neon glow with gentle pulsing
- **Surrounding Fields**: Dimmed with subtle blur while one field is active
- **Fill Complete**: Neon stabilizes to solid glow indicating completion

### Effect Layers
```typescript
const neonRippleEffects = {
  baseGlow: {
    color: '#9b4a75',
    intensity: 0.3,
    blur: '4px'
  },
  neonPulse: {
    animation: 'neonPulse 2s ease-in-out infinite',
    glowLayers: [
      '0 0 5px rgba(155, 74, 117, 0.8)',
      '0 0 10px rgba(155, 74, 117, 0.6)',
      '0 0 15px rgba(155, 74, 117, 0.4)',
      '0 0 20px rgba(155, 74, 117, 0.2)'
    ]
  },
  rippleExpansion: {
    maxRadius: '200px',
    duration: '800ms',
    color: 'rgba(155, 74, 117, 0.4)',
    easing: 'cubic-bezier(0.25, 0.46, 0.45, 0.94)'
  },
  spotlight: {
    activeField: {
      brightness: 1.2,
      contrast: 1.1,
      saturation: 1.1
    },
    inactiveFields: {
      opacity: 0.4,
      blur: '1px',
      brightness: 0.8
    }
  }
}
```

### Color Palette
```typescript
const neonRippleTheme = {
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest) - text
      '#e8ddd4', // light dusty rose - backgrounds
      '#d4a5a5', // dusty rose - borders
      '#c48b8b', // medium rose - secondary elements
      '#b47171', // deep rose - accents
      '#a45757', // dark rose - hover states
      '#9b4a75', // plum - primary neon
      '#880124', // burgundy - secondary neon
      '#6b0119', // dark burgundy - shadows
      '#2c2c2c'  // charcoal - backgrounds
    ]
  },
  neon: {
    primary: '#9b4a75',      // Main neon purple
    secondary: '#880124',     // Burgundy accent
    tertiary: '#d4a5a5',     // Dusty rose highlight
    background: '#1a1a1a',   // Deep dark base
    surface: '#252525',      // Slightly lighter surface
    glow: 'rgba(155, 74, 117, 0.6)',
    ripple: 'rgba(155, 74, 117, 0.4)',
    spotlight: 'rgba(255, 255, 255, 0.05)'
  }
}
```

## Interaction Specifications

### Neon Glow Animation System
```css
/* Base neon glow effect */
.neon-field {
  background: #252525;
  border: 1px solid rgba(155, 74, 117, 0.3);
  color: #f8f4e6;
  position: relative;
  overflow: hidden;
  transition: all 0.3s ease;
}

/* Neon pulse animation */
@keyframes neonPulse {
  0%, 100% {
    box-shadow: 
      0 0 5px rgba(155, 74, 117, 0.5),
      0 0 10px rgba(155, 74, 117, 0.3),
      inset 0 0 5px rgba(155, 74, 117, 0.1);
  }
  50% {
    box-shadow: 
      0 0 8px rgba(155, 74, 117, 0.8),
      0 0 15px rgba(155, 74, 117, 0.5),
      0 0 20px rgba(155, 74, 117, 0.3),
      inset 0 0 8px rgba(155, 74, 117, 0.2);
  }
}

/* Hover state - gentle neon activation */
.neon-field:hover {
  border-color: rgba(155, 74, 117, 0.6);
  box-shadow: 
    0 0 5px rgba(155, 74, 117, 0.6),
    0 0 10px rgba(155, 74, 117, 0.3);
  background: linear-gradient(145deg, #252525, #2a2a2a);
}

/* Focus state - full neon with pulse */
.neon-field:focus,
.neon-field:focus-within {
  border-color: #9b4a75;
  animation: neonPulse 2s ease-in-out infinite;
  background: linear-gradient(145deg, #2a2a2a, #303030);
  outline: none;
}

/* Filled state - stable neon glow */
.neon-field.has-value {
  border-color: rgba(155, 74, 117, 0.8);
  box-shadow: 
    0 0 8px rgba(155, 74, 117, 0.6),
    0 0 15px rgba(155, 74, 117, 0.3),
    inset 0 0 5px rgba(155, 74, 117, 0.1);
}
```

### Ripple Effect Implementation
```css
/* Ripple container */
.neon-field::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 50%;
  width: 0;
  height: 0;
  background: radial-gradient(circle, rgba(155, 74, 117, 0.4) 0%, transparent 70%);
  border-radius: 50%;
  transform: translate(-50%, -50%);
  opacity: 0;
  pointer-events: none;
  transition: all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94);
}

/* Ripple activation on click */
.neon-field.ripple-active::before {
  width: 400px;
  height: 400px;
  opacity: 1;
  animation: rippleExpand 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94) forwards;
}

@keyframes rippleExpand {
  0% {
    width: 0;
    height: 0;
    opacity: 0.8;
  }
  50% {
    opacity: 0.4;
  }
  100% {
    width: 400px;
    height: 400px;
    opacity: 0;
  }
}
```

### Spotlight Effect System
```css
/* Form container for spotlight management */
.neon-form-container {
  position: relative;
  transition: all 0.4s ease;
}

/* Spotlight overlay */
.neon-form-container::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(
    circle at var(--spotlight-x, 50%) var(--spotlight-y, 50%),
    transparent 10%,
    rgba(0, 0, 0, 0.3) 40%,
    rgba(0, 0, 0, 0.6) 80%
  );
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.4s ease;
  z-index: 1;
}

/* Spotlight activation */
.neon-form-container.spotlight-active::before {
  opacity: 1;
}

/* Field highlighting within spotlight */
.neon-form-container.spotlight-active .neon-field:not(.spotlight-target) {
  opacity: 0.4;
  filter: blur(1px) brightness(0.8);
  transform: scale(0.98);
}

.neon-form-container.spotlight-active .neon-field.spotlight-target {
  opacity: 1;
  filter: brightness(1.2) contrast(1.1) saturate(1.1);
  transform: scale(1.02);
  z-index: 2;
  position: relative;
}
```

## Mantine Implementation

### Core Component with All Effects
```tsx
import { TextInput, Box, Text, Stack } from '@mantine/core';
import { useState, useRef, useEffect, useCallback } from 'react';
import { useId, useClickOutside } from '@mantine/hooks';

interface NeonRippleInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  disabled?: boolean;
  onFocus?: () => void;
  onBlur?: () => void;
}

const NeonRippleInput = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder,
  disabled,
  onFocus,
  onBlur
}: NeonRippleInputProps) => {
  const [isFocused, setIsFocused] = useState(false);
  const [isRippleActive, setIsRippleActive] = useState(false);
  const [ripplePosition, setRipplePosition] = useState({ x: '50%', y: '50%' });
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const id = useId();
  
  const handleClick = useCallback((e: React.MouseEvent) => {
    if (disabled) return;
    
    const rect = e.currentTarget.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    
    setRipplePosition({ x: `${x}%`, y: `${y}%` });
    setIsRippleActive(true);
    
    // Reset ripple after animation
    setTimeout(() => setIsRippleActive(false), 800);
    
    inputRef.current?.focus();
  }, [disabled]);
  
  const handleFocus = useCallback(() => {
    setIsFocused(true);
    onFocus?.();
  }, [onFocus]);
  
  const handleBlur = useCallback(() => {
    setIsFocused(false);
    onBlur?.();
  }, [onBlur]);
  
  const getFieldClasses = () => {
    const classes = ['neon-field'];
    if (value.length > 0) classes.push('has-value');
    if (error) classes.push('has-error');
    if (disabled) classes.push('is-disabled');
    if (isRippleActive) classes.push('ripple-active');
    if (isFocused) classes.push('spotlight-target');
    return classes.join(' ');
  };
  
  return (
    <Box className="neon-input-wrapper" pos="relative" ref={containerRef}>
      <Text component="label" htmlFor={id} fw={500} size="sm" mb={8} c="wcr.0">
        {label}
        {required && <Text component="span" c="red" ml={4}>*</Text>}
      </Text>
      
      <TextInput
        ref={inputRef}
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={handleFocus}
        onBlur={handleBlur}
        onClick={handleClick}
        placeholder={placeholder}
        disabled={disabled}
        error={error}
        classNames={{
          input: getFieldClasses(),
          error: 'neon-error'
        }}
        styles={{
          input: {
            backgroundColor: '#252525',
            background: 'linear-gradient(145deg, #252525, #2a2a2a)',
            border: `1px solid ${error ? '#d63031' : 'rgba(155, 74, 117, 0.3)'}`,
            color: '#f8f4e6',
            fontSize: '16px',
            padding: '16px',
            height: '56px',
            borderRadius: '8px',
            position: 'relative',
            overflow: 'hidden',
            transition: 'all 0.3s ease',
            outline: 'none',
            
            '&::before': {
              content: '""',
              position: 'absolute',
              top: '50%',
              left: '50%',
              width: 0,
              height: 0,
              background: 'radial-gradient(circle, rgba(155, 74, 117, 0.4) 0%, transparent 70%)',
              borderRadius: '50%',
              transform: 'translate(-50%, -50%)',
              opacity: 0,
              pointerEvents: 'none',
              transition: 'all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
              zIndex: 0
            },
            
            '&.ripple-active::before': {
              width: '400px',
              height: '400px',
              opacity: 0.8,
              animation: 'rippleExpand 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94) forwards'
            },
            
            '&:hover:not(:disabled)': {
              borderColor: 'rgba(155, 74, 117, 0.6)',
              boxShadow: '0 0 5px rgba(155, 74, 117, 0.6), 0 0 10px rgba(155, 74, 117, 0.3)',
              background: 'linear-gradient(145deg, #252525, #2a2a2a)'
            },
            
            '&:focus, &:focus-within': {
              borderColor: '#9b4a75',
              background: 'linear-gradient(145deg, #2a2a2a, #303030)',
              animation: 'neonPulse 2s ease-in-out infinite',
              zIndex: 2
            },
            
            '&.has-value': {
              borderColor: 'rgba(155, 74, 117, 0.8)',
              boxShadow: `
                0 0 8px rgba(155, 74, 117, 0.6),
                0 0 15px rgba(155, 74, 117, 0.3),
                inset 0 0 5px rgba(155, 74, 117, 0.1)
              `
            },
            
            '&.has-error': {
              borderColor: '#d63031',
              boxShadow: '0 0 8px rgba(214, 48, 49, 0.6), 0 0 15px rgba(214, 48, 49, 0.3)',
              animation: 'neonPulse 2s ease-in-out infinite, shake 0.3s ease-in-out'
            },
            
            '&:disabled': {
              opacity: 0.4,
              boxShadow: 'none',
              animation: 'none'
            }
          },
          
          error: {
            color: '#d63031',
            fontSize: '0.875rem',
            marginTop: '6px',
            textShadow: '0 0 4px rgba(214, 48, 49, 0.5)'
          }
        }}
      />
    </Box>
  );
};
```

### Form Container with Spotlight Management
```tsx
const NeonRippleForm = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });
  
  const [activeField, setActiveField] = useState<string | null>(null);
  const [spotlightPosition, setSpotlightPosition] = useState({ x: '50%', y: '50%' });
  const formRef = useRef<HTMLDivElement>(null);
  
  const handleFieldFocus = useCallback((fieldName: string, event?: React.FocusEvent) => {
    setActiveField(fieldName);
    
    if (event && formRef.current) {
      const formRect = formRef.current.getBoundingClientRect();
      const targetRect = event.target.getBoundingClientRect();
      
      const x = ((targetRect.left + targetRect.width / 2 - formRect.left) / formRect.width) * 100;
      const y = ((targetRect.top + targetRect.height / 2 - formRect.top) / formRect.height) * 100;
      
      setSpotlightPosition({ x: `${x}%`, y: `${y}%` });
    }
  }, []);
  
  const handleFieldBlur = useCallback(() => {
    setActiveField(null);
  }, []);
  
  useClickOutside(() => {
    setActiveField(null);
  }, formRef);
  
  return (
    <Box
      ref={formRef}
      className={`neon-form-container ${activeField ? 'spotlight-active' : ''}`}
      style={{
        '--spotlight-x': spotlightPosition.x,
        '--spotlight-y': spotlightPosition.y,
        background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
        padding: '40px',
        borderRadius: '16px',
        border: '1px solid rgba(155, 74, 117, 0.2)',
        backdropFilter: 'blur(20px)',
        position: 'relative',
        transition: 'all 0.4s ease'
      }}
    >
      {/* Ambient neon glow around form */}
      <Box
        style={{
          position: 'absolute',
          top: '-2px',
          left: '-2px',
          right: '-2px',
          bottom: '-2px',
          background: 'linear-gradient(45deg, transparent, rgba(155, 74, 117, 0.1), transparent)',
          borderRadius: '18px',
          opacity: activeField ? 0.6 : 0.2,
          transition: 'opacity 0.4s ease',
          pointerEvents: 'none',
          zIndex: -1
        }}
      />
      
      <Stack spacing="xl">
        <NeonRippleInput
          label="Email Address"
          value={formData.email}
          onChange={(value) => setFormData({...formData, email: value})}
          onFocus={(e) => handleFieldFocus('email', e)}
          onBlur={handleFieldBlur}
          type="email"
          required
          placeholder="Enter your email"
        />
        
        <NeonRippleInput
          label="Password"
          value={formData.password}
          onChange={(value) => setFormData({...formData, password: value})}
          onFocus={(e) => handleFieldFocus('password', e)}
          onBlur={handleFieldBlur}
          type="password"
          required
          placeholder="Create a secure password"
        />
        
        <NeonRippleInput
          label="Confirm Password"
          value={formData.confirmPassword}
          onChange={(value) => setFormData({...formData, confirmPassword: value})}
          onFocus={(e) => handleFieldFocus('confirmPassword', e)}
          onBlur={handleFieldBlur}
          type="password"
          required
          placeholder="Confirm your password"
        />
        
        <NeonRippleInput
          label="Scene Name"
          value={formData.sceneName}
          onChange={(value) => setFormData({...formData, sceneName: value})}
          onFocus={(e) => handleFieldFocus('sceneName', e)}
          onBlur={handleFieldBlur}
          placeholder="Your community scene name"
        />
        
        <Button
          size="lg"
          variant="gradient"
          gradient={{ from: 'wcr.6', to: 'wcr.7', deg: 45 }}
          style={{
            height: '56px',
            fontWeight: 600,
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
            marginTop: '16px',
            boxShadow: '0 0 15px rgba(155, 74, 117, 0.4)',
            border: '1px solid rgba(155, 74, 117, 0.3)',
            animation: activeField ? 'none' : 'neonPulse 3s ease-in-out infinite'
          }}
        >
          Create Account
        </Button>
      </Stack>
    </Box>
  );
};
```

### Required CSS Animations
```css
/* Global CSS to be added */
@keyframes neonPulse {
  0%, 100% {
    box-shadow: 
      0 0 5px rgba(155, 74, 117, 0.5),
      0 0 10px rgba(155, 74, 117, 0.3),
      inset 0 0 5px rgba(155, 74, 117, 0.1);
  }
  50% {
    box-shadow: 
      0 0 8px rgba(155, 74, 117, 0.8),
      0 0 15px rgba(155, 74, 117, 0.5),
      0 0 20px rgba(155, 74, 117, 0.3),
      inset 0 0 8px rgba(155, 74, 117, 0.2);
  }
}

@keyframes rippleExpand {
  0% {
    width: 0;
    height: 0;
    opacity: 0.8;
  }
  50% {
    opacity: 0.4;
  }
  100% {
    width: 400px;
    height: 400px;
    opacity: 0;
  }
}

@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25% { transform: translateX(-4px); }
  75% { transform: translateX(4px); }
}

/* Spotlight effect */
.neon-form-container::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(
    circle at var(--spotlight-x, 50%) var(--spotlight-y, 50%),
    transparent 20%,
    rgba(0, 0, 0, 0.2) 50%,
    rgba(0, 0, 0, 0.6) 80%
  );
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.4s ease;
  z-index: 1;
  border-radius: inherit;
}

.neon-form-container.spotlight-active::before {
  opacity: 1;
}

.neon-form-container.spotlight-active .neon-field:not(.spotlight-target) {
  opacity: 0.4;
  filter: blur(1px) brightness(0.8);
  transform: scale(0.98);
  transition: all 0.4s ease;
}

.neon-form-container.spotlight-active .neon-field.spotlight-target {
  opacity: 1;
  filter: brightness(1.2) contrast(1.1) saturate(1.1);
  transform: scale(1.02);
  z-index: 2;
  position: relative;
  transition: all 0.4s ease;
}
```

## Advanced Features

### Dynamic Ripple Origin
```tsx
const useDynamicRipple = () => {
  const [rippleOrigin, setRippleOrigin] = useState({ x: 50, y: 50 });
  
  const triggerRipple = useCallback((event: React.MouseEvent) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const x = ((event.clientX - rect.left) / rect.width) * 100;
    const y = ((event.clientY - rect.top) / rect.height) * 100;
    
    setRippleOrigin({ x, y });
    
    // Apply CSS custom properties for ripple position
    event.currentTarget.style.setProperty('--ripple-x', `${x}%`);
    event.currentTarget.style.setProperty('--ripple-y', `${y}%`);
  }, []);
  
  return { rippleOrigin, triggerRipple };
};
```

### Performance-Optimized Neon
```tsx
const usePerformanceNeon = () => {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
  const isLowPerformance = navigator.hardwareConcurrency < 4;
  
  return {
    enableNeon: !isMobile || !isLowPerformance,
    enableRipple: !prefersReducedMotion && !isLowPerformance,
    enableSpotlight: !isMobile && !prefersReducedMotion,
    animationDuration: isLowPerformance ? '0.2s' : '0.8s',
    glowLayers: isLowPerformance ? 2 : 4
  };
};
```

## Responsive Behavior

### Mobile (xs - sm)
- **Simplified Neon**: Single glow layer only
- **No Ripple**: Touch ripple disabled for performance
- **No Spotlight**: Dimming effect disabled
- **Quick Transitions**: 200ms maximum
- **Touch Optimization**: 44px minimum touch targets

### Tablet (md)
- **Medium Neon**: 2-layer glow effect
- **Simple Ripple**: Basic expand animation
- **Subtle Spotlight**: Reduced opacity dimming
- **Medium Timing**: 400ms transitions

### Desktop (lg+)
- **Full Neon**: Complete 4-layer glow system
- **Complex Ripple**: Full position-aware expansion
- **Dynamic Spotlight**: Real-time position tracking
- **Smooth Timing**: 800ms ripple expansion

## Accessibility Features

### WCAG 2.1 AA Compliance
- **Motion Control**: Respects `prefers-reduced-motion`
- **Focus States**: Clear non-animated focus indicators
- **Color Independence**: Works without neon effects
- **Screen Reader**: ARIA labels for state changes
- **Keyboard Navigation**: Full keyboard support

### Implementation
```tsx
// Accessibility enhancements
const useAccessibleNeon = (id: string) => {
  const announceStateChange = useCallback((state: string) => {
    const announcement = `Field ${state}`;
    // Screen reader announcement
    const ariaLive = document.getElementById(`${id}-live`);
    if (ariaLive) {
      ariaLive.textContent = announcement;
    }
  }, [id]);
  
  return { announceStateChange };
};

// Usage in component
<div
  id={`${id}-live`}
  aria-live="polite"
  aria-atomic="true"
  style={{ position: 'absolute', left: '-10000px' }}
/>
```

## Performance Considerations

### GPU Acceleration
- Uses `transform3d()` for hardware acceleration
- `will-change` applied during animations only
- Composite layers optimized for smooth rendering

### Memory Management
```typescript
const useAnimationCleanup = () => {
  const animationRefs = useRef<number[]>([]);
  
  const addAnimation = useCallback((id: number) => {
    animationRefs.current.push(id);
  }, []);
  
  const cleanupAnimations = useCallback(() => {
    animationRefs.current.forEach(id => cancelAnimationFrame(id));
    animationRefs.current = [];
  }, []);
  
  useEffect(() => {
    return cleanupAnimations;
  }, [cleanupAnimations]);
  
  return { addAnimation, cleanupAnimations };
};
```

## Brand Integration

### WitchCityRope Aesthetic
- **Mystical Neon**: Purple and burgundy glows evoke magical energy
- **Salem Influence**: Dark, mysterious lighting effects
- **Professional Touch**: Subtle, controlled animations
- **Community Focus**: Warm, inviting glow colors

### Sci-Fi Interface Feel
- **Futuristic**: Ripple effects suggest advanced technology
- **Interactive**: Spotlight creates immersive experience
- **Responsive**: Dynamic effects respond to user interaction
- **Memorable**: Unique visual signature for the platform

## Usage Guidelines

### When to Use
- Registration and signup forms
- Event creation (special experiences)
- Profile setup and editing
- Premium feature interfaces
- Interactive dashboards

### When Not to Use
- Simple search forms
- Quick filter interfaces
- Mobile-only applications
- High-frequency input forms
- Users with motion sensitivity

### Best Practices
1. Test performance on target devices
2. Provide reduced-motion alternatives
3. Ensure effects enhance rather than distract
4. Use consistent timing across all effects
5. Maintain readability during all animations

## Testing Checklist

### Visual Effects
- [ ] Neon glow appears smooth and consistent
- [ ] Ripple expands from correct click position
- [ ] Spotlight follows active field accurately
- [ ] Surrounding fields dim appropriately
- [ ] Color transitions match brand guidelines

### Performance
- [ ] 60fps animations on target devices
- [ ] No memory leaks from repeated animations
- [ ] Graceful degradation on slower devices
- [ ] Battery usage within acceptable limits
- [ ] No visual artifacts during state changes

### Accessibility
- [ ] Effects respect motion preferences
- [ ] Focus states visible without effects
- [ ] Screen reader compatibility maintained
- [ ] Keyboard navigation fully functional
- [ ] Color contrast preserved during all states

### Interaction
- [ ] Click ripples respond to exact position
- [ ] Spotlight tracks field focus accurately
- [ ] Multiple rapid clicks handled gracefully
- [ ] Touch interactions work on mobile
- [ ] Error states clearly communicated

---

*This Neon Ripple Spotlight design creates an immersive, sci-fi inspired form experience that makes users feel like they're interacting with advanced technology. The combination of neon glows, position-aware ripples, and dynamic spotlighting creates a memorable and engaging interface perfect for WitchCityRope's mystical and innovative brand identity.*