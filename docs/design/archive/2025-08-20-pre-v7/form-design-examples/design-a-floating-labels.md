# Design A: Floating Label Modern
<!-- Last Updated: 2025-08-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## Design Overview

A sophisticated floating label design that combines modern UI trends with the WitchCityRope mystical aesthetic. Labels elegantly float above fields when focused or filled, with smooth transitions and subtle lighting effects that create an immersive "Lightning Dark" experience.

## Visual Characteristics

### Label Behavior
- **Resting State**: Label sits inside the input field as placeholder text
- **Active State**: Label smoothly animates up and outside the field border
- **Focused State**: Label remains floating with accent color
- **Filled State**: Label stays floating even when field loses focus

### Color Palette
```typescript
const floatingLabelTheme = {
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest) - floating labels
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
  primaryColor: 'wcr',
  dark: {
    background: '#1a1a1a',
    surface: 'rgba(255, 255, 255, 0.02)',
    border: 'rgba(212, 165, 165, 0.2)',
    text: '#f8f4e6',
    placeholder: 'rgba(248, 244, 230, 0.5)'
  }
}
```

### Typography
- **Floating Labels**: 'Source Sans 3', 12px, medium weight
- **Input Text**: 'Source Sans 3', 16px, regular
- **Helper Text**: 'Source Sans 3', 14px, regular
- **Error Text**: 'Source Sans 3', 14px, medium

## Interaction Specifications

### Animations & Transitions
```css
/* Label Animation */
.floating-label {
  transition: all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94);
  transform-origin: left center;
}

.floating-label.active {
  transform: translateY(-24px) scale(0.85);
  color: var(--mantine-color-wcr-6);
}

/* Field Focus Effect */
.floating-input:focus {
  border-color: var(--mantine-color-wcr-6);
  box-shadow: 0 0 0 2px rgba(155, 74, 117, 0.2),
              0 4px 12px rgba(155, 74, 117, 0.1);
}

/* Subtle Glow Effect */
.floating-input:focus-within {
  background: linear-gradient(145deg, 
    rgba(155, 74, 117, 0.03), 
    rgba(136, 1, 36, 0.02)
  );
}
```

### Hover States
- **Background**: Subtle gradient overlay (2% opacity)
- **Border**: Lightens by 20%
- **Cursor**: Changes to text cursor over entire field area
- **Duration**: 200ms ease-out transition

### Focus States
- **Border**: Plum accent color (#9b4a75)
- **Shadow**: Soft outer glow with plum tint
- **Background**: Very subtle gradient
- **Label**: Animates to floating position with accent color

### Error States
- **Border**: Deep red (#d63031)
- **Background**: Subtle red tint (1% opacity)
- **Label**: Red color with shake animation
- **Helper Text**: Error message appears below with slide-in effect

## Mantine Implementation

### Component Structure
```tsx
import { TextInput, Box, Text } from '@mantine/core';
import { useState, useRef } from 'react';

interface FloatingLabelInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
}

const FloatingLabelInput = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder = ' ' // Space for floating effect
}: FloatingLabelInputProps) => {
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  
  const isActive = isFocused || value.length > 0;
  
  return (
    <Box className="floating-input-wrapper" pos="relative">
      <TextInput
        ref={inputRef}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        error={error}
        placeholder={placeholder}
        classNames={{
          input: 'floating-input',
          error: 'floating-error'
        }}
        styles={{
          input: {
            backgroundColor: 'var(--mantine-color-dark-7)',
            borderColor: error ? '#d63031' : 'var(--mantine-color-wcr-2)',
            color: 'var(--mantine-color-wcr-0)',
            fontSize: '16px',
            padding: '16px 12px 8px 12px',
            height: '56px',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            '&:hover': {
              borderColor: 'var(--mantine-color-wcr-3)',
              background: 'linear-gradient(145deg, rgba(155, 74, 117, 0.02), rgba(136, 1, 36, 0.01))'
            },
            '&:focus': {
              borderColor: 'var(--mantine-color-wcr-6)',
              boxShadow: '0 0 0 2px rgba(155, 74, 117, 0.2), 0 4px 12px rgba(155, 74, 117, 0.1)',
              background: 'linear-gradient(145deg, rgba(155, 74, 117, 0.03), rgba(136, 1, 36, 0.02))'
            }
          }
        }}
      />
      
      <Text
        className={`floating-label ${isActive ? 'active' : ''}`}
        component="label"
        onClick={() => inputRef.current?.focus()}
        style={{
          position: 'absolute',
          left: '12px',
          top: isActive ? '4px' : '18px',
          fontSize: isActive ? '12px' : '16px',
          color: isActive ? 'var(--mantine-color-wcr-6)' : 'var(--mantine-color-wcr-4)',
          pointerEvents: 'none',
          transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
          transformOrigin: 'left center',
          background: isActive ? 'var(--mantine-color-dark-7)' : 'transparent',
          padding: isActive ? '0 4px' : '0',
          zIndex: 1
        }}
      >
        {label}
        {required && <span style={{ color: '#d63031' }}>*</span>}
      </Text>
      
      {error && (
        <Text size="sm" c="red" mt="xs" className="floating-error-text">
          {error}
        </Text>
      )}
    </Box>
  );
};
```

### Form Layout Example
```tsx
const RegistrationFormFloating = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });
  
  return (
    <Box
      style={{
        background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
        padding: '32px',
        borderRadius: '12px',
        border: '1px solid rgba(212, 165, 165, 0.1)',
        backdropFilter: 'blur(10px)'
      }}
    >
      <Stack spacing="xl">
        <FloatingLabelInput
          label="Email Address"
          value={formData.email}
          onChange={(value) => setFormData({...formData, email: value})}
          type="email"
          required
        />
        
        <FloatingLabelInput
          label="Password"
          value={formData.password}
          onChange={(value) => setFormData({...formData, password: value})}
          type="password"
          required
        />
        
        <FloatingLabelInput
          label="Scene Name"
          value={formData.sceneName}
          onChange={(value) => setFormData({...formData, sceneName: value})}
        />
        
        <FloatingLabelInput
          label="Phone Number"
          value={formData.phone}
          onChange={(value) => setFormData({...formData, phone: value})}
          type="tel"
        />
        
        <Button
          size="lg"
          variant="gradient"
          gradient={{ from: 'wcr.6', to: 'wcr.7', deg: 45 }}
          style={{
            height: '56px',
            fontWeight: 600,
            textTransform: 'uppercase',
            letterSpacing: '0.5px'
          }}
        >
          Create Account
        </Button>
      </Stack>
    </Box>
  );
};
```

## Responsive Behavior

### Mobile (xs - sm)
- Field height: 52px (reduced from 56px)
- Font size: 16px (prevents zoom on iOS)
- Floating label: 11px when active
- Touch targets: Minimum 44px
- Spacing: 16px between fields

### Tablet (md)
- Field height: 56px
- Standard spacing: 20px between fields
- Larger touch targets for better usability

### Desktop (lg - xl)
- Field height: 56px
- Spacing: 24px between fields
- Hover effects fully enabled
- Focus rings more prominent

## Accessibility Features

### WCAG 2.1 AA Compliance
- **Color Contrast**: 4.5:1 minimum for all text
- **Focus Indicators**: Clear visual focus states
- **Keyboard Navigation**: Full keyboard accessibility
- **Screen Readers**: Proper ARIA labels and descriptions

### Implementation
```tsx
// ARIA attributes for floating labels
<TextInput
  aria-label={label}
  aria-required={required}
  aria-invalid={!!error}
  aria-describedby={error ? `${id}-error` : undefined}
/>

<Text
  id={error ? `${id}-error` : undefined}
  role={error ? 'alert' : undefined}
>
  {error}
</Text>
```

## Performance Considerations

### Animation Optimization
- Use `transform` and `opacity` for animations (GPU accelerated)
- Avoid animating `top`, `left`, or other layout properties
- Use `will-change` sparingly and remove after animation

### CSS-in-JS Optimization
```tsx
// Optimized styles using Mantine's style system
const useFloatingStyles = createStyles((theme) => ({
  input: {
    backgroundColor: theme.colors.dark[7],
    transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
    '&:hover': {
      borderColor: theme.colors.wcr[3]
    }
  },
  label: {
    transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
    transformOrigin: 'left center'
  }
}));
```

## Brand Integration

### WitchCityRope Aesthetic
- **Mystical Feel**: Subtle glowing effects on focus
- **Professional**: Clean typography and spacing
- **Salem Influence**: Deep purples and burgundy accents
- **Community-Focused**: Warm, welcoming color transitions

### Dark Mode Optimization
- High contrast for readability
- Reduced eye strain with warm tints
- Subtle lighting effects that feel magical
- Consistent with "Lightning Dark" trend

## Usage Guidelines

### When to Use
- Registration forms
- Profile editing
- Event creation forms
- Contact forms
- Any form where elegance is prioritized

### When Not to Use
- Simple search inputs
- Filters or quick actions
- Forms with very short labels
- Mobile-only applications (consider simpler alternatives)

### Best Practices
1. Ensure labels are descriptive and concise
2. Use consistent animation timing across all fields
3. Test with actual content, not lorem ipsum
4. Verify accessibility with screen readers
5. Test on various devices and orientations

## Testing Checklist

### Functionality
- [ ] Labels animate smoothly on focus
- [ ] Labels stay floating when field has content
- [ ] Error states display correctly
- [ ] Keyboard navigation works properly
- [ ] Touch interactions work on mobile

### Visual
- [ ] Animations are smooth (60fps)
- [ ] Colors match brand guidelines
- [ ] Focus states are clearly visible
- [ ] Error states are prominent but not jarring
- [ ] Responsive behavior works across all breakpoints

### Accessibility
- [ ] Screen reader compatibility
- [ ] Keyboard-only navigation
- [ ] Color contrast ratios meet WCAG standards
- [ ] Focus indicators are visible
- [ ] Error messages are announced properly

---

*This design creates an elegant, modern form experience that aligns with WitchCityRope's sophisticated community platform while maintaining excellent usability and accessibility standards.*