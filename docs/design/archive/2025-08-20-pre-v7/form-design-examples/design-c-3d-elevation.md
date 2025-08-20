# Design C: 3D Elevation Effect
<!-- Last Updated: 2025-08-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## Design Overview

A sophisticated 3D elevation design that makes form fields appear to physically lift off the page when focused. The design combines multiple layered shadows, subtle rotation effects, and smooth vertical translation to create a premium, tactile experience that feels both modern and mystical for the WitchCityRope platform.

## Visual Characteristics

### Core 3D Behavior
- **Resting State**: Fields sit flush with the surface with minimal shadow
- **Hover State**: Subtle lift (2px) with expanding shadow
- **Focus State**: Dramatic elevation (6px) with multi-layered shadows and slight tilt
- **Filled State**: Maintains slight elevation (3px) to show completion
- **Surrounding Fields**: Slightly recede or dim when another field is focused

### Elevation Mechanics
```typescript
const elevationStates = {
  resting: {
    transform: 'translateY(0px) rotateX(0deg)',
    shadows: ['0 1px 3px rgba(0, 0, 0, 0.1)']
  },
  hover: {
    transform: 'translateY(-2px) rotateX(1deg)',
    shadows: [
      '0 2px 6px rgba(0, 0, 0, 0.15)',
      '0 1px 3px rgba(155, 74, 117, 0.1)'
    ]
  },
  focus: {
    transform: 'translateY(-6px) rotateX(2deg) rotateZ(0.5deg)',
    shadows: [
      '0 8px 25px rgba(0, 0, 0, 0.25)',
      '0 4px 15px rgba(155, 74, 117, 0.2)',
      '0 2px 8px rgba(155, 74, 117, 0.3)',
      '0 1px 3px rgba(155, 74, 117, 0.4)'
    ]
  },
  filled: {
    transform: 'translateY(-3px) rotateX(1deg)',
    shadows: [
      '0 4px 12px rgba(0, 0, 0, 0.15)',
      '0 2px 6px rgba(155, 74, 117, 0.15)'
    ]
  }
}
```

### Color Palette
```typescript
const elevationTheme = {
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest) - elevated text
      '#e8ddd4', // light dusty rose - hover backgrounds
      '#d4a5a5', // dusty rose - borders
      '#c48b8b', // medium rose - secondary text
      '#b47171', // deep rose - accent elements
      '#a45757', // dark rose - active borders
      '#9b4a75', // plum - focus states
      '#880124', // burgundy - primary actions
      '#6b0119', // dark burgundy - pressed states
      '#2c2c2c'  // charcoal - backgrounds
    ]
  },
  elevation: {
    surface: '#1e1e1e',
    raised: '#252525',
    floating: '#2a2a2a',
    shadowBase: 'rgba(0, 0, 0, 0.3)',
    shadowAccent: 'rgba(155, 74, 117, 0.2)',
    glowAccent: 'rgba(155, 74, 117, 0.1)'
  }
}
```

## Interaction Specifications

### Animation System
```css
/* Base field styling with perspective */
.elevation-form-container {
  perspective: 1200px;
  perspective-origin: center top;
}

.elevation-field {
  transform-style: preserve-3d;
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  backface-visibility: hidden;
  will-change: transform, box-shadow;
}

/* Resting state */
.elevation-field {
  transform: translateY(0px) rotateX(0deg) rotateZ(0deg);
  box-shadow: 
    0 1px 3px rgba(0, 0, 0, 0.1),
    0 0 0 1px rgba(212, 165, 165, 0.1);
  background: linear-gradient(145deg, #1e1e1e, #252525);
}

/* Hover state - subtle lift */
.elevation-field:hover {
  transform: translateY(-2px) rotateX(1deg);
  box-shadow: 
    0 2px 6px rgba(0, 0, 0, 0.15),
    0 1px 3px rgba(155, 74, 117, 0.1),
    0 0 0 1px rgba(212, 165, 165, 0.2);
  background: linear-gradient(145deg, #252525, #2a2a2a);
}

/* Focus state - dramatic elevation */
.elevation-field:focus,
.elevation-field:focus-within {
  transform: translateY(-6px) rotateX(2deg) rotateZ(0.5deg);
  box-shadow: 
    0 8px 25px rgba(0, 0, 0, 0.25),
    0 4px 15px rgba(155, 74, 117, 0.2),
    0 2px 8px rgba(155, 74, 117, 0.3),
    0 1px 3px rgba(155, 74, 117, 0.4),
    0 0 0 2px rgba(155, 74, 117, 0.5);
  background: linear-gradient(145deg, #2a2a2a, #303030);
}

/* Filled state - maintains elevation */
.elevation-field.has-value {
  transform: translateY(-3px) rotateX(1deg);
  box-shadow: 
    0 4px 12px rgba(0, 0, 0, 0.15),
    0 2px 6px rgba(155, 74, 117, 0.15),
    0 0 0 1px rgba(155, 74, 117, 0.3);
}

/* Surrounding field dimming effect */
.elevation-form-container:has(.elevation-field:focus) .elevation-field:not(:focus) {
  opacity: 0.7;
  transform: translateY(1px) scale(0.98);
  filter: blur(0.5px);
}
```

### Advanced Shadow Layering
```css
/* Multi-layer shadow system for depth perception */
.elevation-shadows-complex {
  /* Base shadow - ground contact */
  box-shadow: 
    /* Ambient shadow - soft, large */
    0 8px 25px rgba(0, 0, 0, 0.25),
    /* Direct shadow - sharp, medium */
    0 4px 15px rgba(0, 0, 0, 0.15),
    /* Accent glow - colored, soft */
    0 2px 8px rgba(155, 74, 117, 0.3),
    /* Inner glow - colored, sharp */
    0 1px 3px rgba(155, 74, 117, 0.4),
    /* Border highlight */
    inset 0 1px 0 rgba(255, 255, 255, 0.1),
    /* Top edge highlight */
    0 -1px 0 rgba(155, 74, 117, 0.2);
}

/* Animated shadow expansion */
@keyframes shadowExpand {
  0% {
    box-shadow: 
      0 1px 3px rgba(0, 0, 0, 0.1);
  }
  100% {
    box-shadow: 
      0 8px 25px rgba(0, 0, 0, 0.25),
      0 4px 15px rgba(155, 74, 117, 0.2),
      0 2px 8px rgba(155, 74, 117, 0.3),
      0 1px 3px rgba(155, 74, 117, 0.4);
  }
}
```

## Mantine Implementation

### Core Component Structure
```tsx
import { TextInput, Box, Text, Stack } from '@mantine/core';
import { useState, useRef, useEffect } from 'react';
import { useId } from '@mantine/hooks';

interface ElevationInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  disabled?: boolean;
}

const ElevationInput = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder,
  disabled
}: ElevationInputProps) => {
  const [isFocused, setIsFocused] = useState(false);
  const [hasValue, setHasValue] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const id = useId();
  
  useEffect(() => {
    setHasValue(value.length > 0);
  }, [value]);
  
  const getFieldClasses = () => {
    const classes = ['elevation-field'];
    if (hasValue) classes.push('has-value');
    if (error) classes.push('has-error');
    if (disabled) classes.push('is-disabled');
    return classes.join(' ');
  };
  
  return (
    <Box className="elevation-input-wrapper" pos="relative">
      <TextInput
        ref={inputRef}
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        placeholder={placeholder}
        disabled={disabled}
        error={error}
        label={
          <Text component="label" htmlFor={id} fw={500} size="sm" mb={8}>
            {label}
            {required && <Text component="span" c="red" ml={4}>*</Text>}
          </Text>
        }
        classNames={{
          input: getFieldClasses(),
          label: 'elevation-label',
          error: 'elevation-error'
        }}
        styles={{
          input: {
            backgroundColor: '#1e1e1e',
            background: 'linear-gradient(145deg, #1e1e1e, #252525)',
            borderColor: error ? '#d63031' : 'rgba(212, 165, 165, 0.2)',
            color: '#f8f4e6',
            fontSize: '16px',
            padding: '16px',
            height: '56px',
            borderRadius: '8px',
            transformStyle: 'preserve-3d',
            backfaceVisibility: 'hidden',
            transition: 'all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275)',
            willChange: 'transform, box-shadow',
            boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1), 0 0 0 1px rgba(212, 165, 165, 0.1)',
            
            '&:hover:not(:disabled)': {
              transform: 'translateY(-2px) rotateX(1deg)',
              background: 'linear-gradient(145deg, #252525, #2a2a2a)',
              boxShadow: `
                0 2px 6px rgba(0, 0, 0, 0.15),
                0 1px 3px rgba(155, 74, 117, 0.1),
                0 0 0 1px rgba(212, 165, 165, 0.2)
              `
            },
            
            '&:focus, &:focus-within': {
              transform: 'translateY(-6px) rotateX(2deg) rotateZ(0.5deg)',
              background: 'linear-gradient(145deg, #2a2a2a, #303030)',
              borderColor: '#9b4a75',
              boxShadow: `
                0 8px 25px rgba(0, 0, 0, 0.25),
                0 4px 15px rgba(155, 74, 117, 0.2),
                0 2px 8px rgba(155, 74, 117, 0.3),
                0 1px 3px rgba(155, 74, 117, 0.4),
                0 0 0 2px rgba(155, 74, 117, 0.5)
              `
            },
            
            '&.has-value': {
              transform: 'translateY(-3px) rotateX(1deg)',
              boxShadow: `
                0 4px 12px rgba(0, 0, 0, 0.15),
                0 2px 6px rgba(155, 74, 117, 0.15),
                0 0 0 1px rgba(155, 74, 117, 0.3)
              `
            },
            
            '&.has-error': {
              borderColor: '#d63031',
              boxShadow: `
                0 4px 12px rgba(214, 48, 49, 0.2),
                0 2px 6px rgba(214, 48, 49, 0.3),
                0 0 0 1px #d63031
              `
            },
            
            '&:disabled': {
              opacity: 0.6,
              transform: 'none',
              boxShadow: 'none'
            }
          },
          
          label: {
            color: '#f8f4e6',
            fontWeight: 500,
            marginBottom: '8px'
          },
          
          error: {
            color: '#d63031',
            fontSize: '0.875rem',
            marginTop: '6px',
            animation: 'shake 0.3s ease-in-out'
          }
        }}
      />
    </Box>
  );
};
```

### Form Container with Perspective
```tsx
const ElevationForm = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });
  
  return (
    <Box
      className="elevation-form-container"
      style={{
        perspective: '1200px',
        perspectiveOrigin: 'center top',
        background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
        padding: '40px',
        borderRadius: '16px',
        border: '1px solid rgba(212, 165, 165, 0.1)',
        backdropFilter: 'blur(20px)',
        position: 'relative'
      }}
    >
      {/* Ambient lighting effect */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: '40%',
          background: 'linear-gradient(180deg, rgba(155, 74, 117, 0.02) 0%, transparent 100%)',
          borderRadius: '16px 16px 0 0',
          pointerEvents: 'none'
        }}
      />
      
      <Stack spacing="xl">
        <ElevationInput
          label="Email Address"
          value={formData.email}
          onChange={(value) => setFormData({...formData, email: value})}
          type="email"
          required
          placeholder="Enter your email"
        />
        
        <ElevationInput
          label="Password"
          value={formData.password}
          onChange={(value) => setFormData({...formData, password: value})}
          type="password"
          required
          placeholder="Create a secure password"
        />
        
        <ElevationInput
          label="Confirm Password"
          value={formData.confirmPassword}
          onChange={(value) => setFormData({...formData, confirmPassword: value})}
          type="password"
          required
          placeholder="Confirm your password"
        />
        
        <ElevationInput
          label="Scene Name"
          value={formData.sceneName}
          onChange={(value) => setFormData({...formData, sceneName: value})}
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
            transform: 'translateY(0px)',
            transition: 'all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275)',
            boxShadow: '0 4px 15px rgba(155, 74, 117, 0.3)'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.transform = 'translateY(-2px)';
            e.currentTarget.style.boxShadow = '0 6px 20px rgba(155, 74, 117, 0.4)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.transform = 'translateY(0px)';
            e.currentTarget.style.boxShadow = '0 4px 15px rgba(155, 74, 117, 0.3)';
          }}
        >
          Create Account
        </Button>
      </Stack>
    </Box>
  );
};
```

### Custom CSS Animations
```css
/* Additional CSS to be added to global styles */
@keyframes shake {
  0%, 100% { transform: translateX(0); }
  25% { transform: translateX(-4px); }
  75% { transform: translateX(4px); }
}

/* Form container focus management */
.elevation-form-container:has(.elevation-field:focus) .elevation-field:not(:focus) {
  opacity: 0.7;
  transform: translateY(1px) scale(0.98);
  filter: blur(0.5px);
  transition: all 0.3s ease;
}

/* Performance optimization for mobile */
@media (max-width: 768px) {
  .elevation-field {
    transform: none !important;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
    transition: box-shadow 0.2s ease !important;
  }
  
  .elevation-field:focus {
    transform: none !important;
    box-shadow: 0 0 0 2px rgba(155, 74, 117, 0.5) !important;
  }
}

/* Reduced motion support */
@media (prefers-reduced-motion: reduce) {
  .elevation-field {
    transform: none !important;
    transition: box-shadow 0.1s ease !important;
  }
}
```

## Responsive Behavior

### Mobile Optimization (xs - sm)
- **Transforms Disabled**: 3D effects disabled for performance
- **Simple Focus**: 2px border highlight only
- **Touch Targets**: Minimum 44px height
- **Reduced Shadows**: Single shadow layer
- **Fast Transitions**: 200ms maximum

### Tablet (md)
- **Subtle 3D**: Reduced elevation (max 3px lift)
- **Simplified Shadows**: 2-layer shadow system
- **Touch-Friendly**: Larger touch areas
- **Medium Transitions**: 300ms timing

### Desktop (lg+)
- **Full 3D Effects**: Complete elevation system
- **Complex Shadows**: 4-layer shadow system
- **Micro-Interactions**: Hover states enabled
- **Smooth Transitions**: 400ms cubic-bezier timing

## Accessibility Features

### WCAG 2.1 AA Compliance
- **Focus Indicators**: High-contrast focus states
- **Color Independence**: Elevation works without color
- **Keyboard Navigation**: Full keyboard support
- **Screen Readers**: Proper ARIA implementation
- **Motion Control**: Respects `prefers-reduced-motion`

### Implementation
```tsx
// Accessibility enhancements
<TextInput
  aria-label={label}
  aria-required={required}
  aria-invalid={!!error}
  aria-describedby={error ? `${id}-error` : `${id}-help`}
  role="textbox"
/>

<Text
  id={error ? `${id}-error` : `${id}-help`}
  role={error ? 'alert' : 'note'}
  aria-live={error ? 'polite' : 'off'}
>
  {error || helpText}
</Text>
```

## Performance Considerations

### Optimization Strategies
- **Hardware Acceleration**: Uses `transform3d()` for GPU rendering
- **Will-Change**: Applied selectively during interactions
- **Layer Management**: Minimizes paint operations
- **Mobile Fallbacks**: Simplified effects on mobile devices

### Performance Monitoring
```typescript
const usePerformanceOptimizedElevation = () => {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
  const isSlowDevice = navigator.hardwareConcurrency < 4;
  
  return {
    enableElevation: !isMobile && !prefersReducedMotion && !isSlowDevice,
    maxShadowLayers: isMobile ? 1 : isSlowDevice ? 2 : 4,
    transitionDuration: isMobile ? '200ms' : '400ms'
  };
};
```

## Brand Integration

### WitchCityRope Aesthetic
- **Mystical Elevation**: Fields float like magical objects
- **Salem Influence**: Dark, mysterious depth effects
- **Professional Touch**: Subtle, sophisticated interactions
- **Community Focus**: Welcoming, tactile experience

### Dark Theme Optimization
- **Depth Perception**: Multiple shadow layers create true depth
- **Warm Lighting**: Subtle burgundy accent glows
- **Surface Texture**: Gradient backgrounds suggest material
- **Ambient Light**: Soft overhead lighting effect

## Usage Guidelines

### When to Use
- Registration and onboarding forms
- Profile creation and editing
- Event creation forms
- Important data entry forms
- Premium user experiences

### When Not to Use
- Simple search fields
- Quick filter forms
- Mobile-only applications
- High-frequency input forms
- Users with motion sensitivity

### Best Practices
1. Test on various devices for performance
2. Provide fallbacks for reduced motion
3. Ensure focus states are clearly visible
4. Use consistent elevation hierarchy
5. Avoid overwhelming users with too many floating elements

## Testing Checklist

### Functionality
- [ ] Smooth elevation transitions on focus
- [ ] Proper shadow layering at all states
- [ ] Surrounding field dimming works correctly
- [ ] Keyboard navigation maintains elevation
- [ ] Touch interactions work on mobile
- [ ] Performance acceptable on older devices

### Visual Quality
- [ ] 3D effect appears realistic
- [ ] Shadows align with light source direction
- [ ] Colors match brand guidelines
- [ ] Animation timing feels natural
- [ ] No visual glitches during transitions

### Accessibility
- [ ] Focus states visible without 3D effects
- [ ] Works with screen readers
- [ ] Respects motion preferences
- [ ] Keyboard-only navigation functional
- [ ] Color contrast maintained

### Performance
- [ ] 60fps animations on target devices
- [ ] No jank during field transitions
- [ ] Memory usage within acceptable limits
- [ ] Battery impact minimized on mobile
- [ ] Graceful degradation on slow devices

---

*This 3D elevation design creates a premium, tactile form experience that makes users feel like they're interacting with physical objects floating above the surface, perfectly aligned with WitchCityRope's mystical and sophisticated brand identity.*