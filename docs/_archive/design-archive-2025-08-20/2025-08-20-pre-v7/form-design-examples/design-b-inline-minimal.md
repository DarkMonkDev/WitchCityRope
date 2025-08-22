# Design B: Inline Minimal
<!-- Last Updated: 2025-08-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## Design Overview

A clean, minimalist design with labels positioned inline to the left of input fields. This approach maximizes visual clarity, reduces cognitive load, and creates a sophisticated, professional appearance perfect for WitchCityRope's community platform. The design emphasizes readability and efficient space usage.

## Visual Characteristics

### Layout Structure
- **Label Position**: Fixed position to the left of input fields
- **Label Width**: Consistent 140px width for alignment
- **Input Width**: Flexible, responsive to container
- **Alignment**: Labels right-aligned to create clean edge against inputs
- **Spacing**: 16px gap between label and input field

### Color Palette
```typescript
const inlineMinimalTheme = {
  colors: {
    wcr: [
      '#f8f4e6', // ivory - labels and text
      '#e8ddd4', // light dusty - hover backgrounds
      '#d4a5a5', // dusty rose - borders
      '#c48b8b', // medium rose - secondary elements
      '#b47171', // deep rose - accent elements
      '#a45757', // dark rose - active states
      '#9b4a75', // plum - focus states
      '#880124', // burgundy - primary actions
      '#6b0119', // dark burgundy - pressed states
      '#2c2c2c'  // charcoal - backgrounds
    ]
  },
  dark: {
    background: '#1a1a1a',
    surface: '#252525',
    border: 'rgba(212, 165, 165, 0.15)',
    text: '#f8f4e6',
    secondaryText: 'rgba(248, 244, 230, 0.7)',
    accent: '#9b4a75'
  }
}
```

### Typography
- **Labels**: 'Source Sans 3', 14px, medium weight, right-aligned
- **Input Text**: 'Source Sans 3', 16px, regular
- **Helper Text**: 'Source Sans 3', 13px, regular
- **Required Indicators**: 'Source Sans 3', 14px, medium

## Interaction Specifications

### Hover Effects
```css
/* Label Hover */
.inline-label:hover {
  color: var(--mantine-color-wcr-5);
  transition: color 0.2s ease;
}

/* Input Hover */
.inline-input:hover {
  border-color: var(--mantine-color-wcr-4);
  background-color: rgba(37, 37, 37, 0.8);
  transition: all 0.2s ease;
}

/* Row Hover Effect */
.input-row:hover .inline-label {
  color: var(--mantine-color-wcr-4);
}
```

### Focus States
```css
.inline-input:focus {
  border-color: var(--mantine-color-wcr-6);
  box-shadow: 0 0 0 1px var(--mantine-color-wcr-6);
  background-color: rgba(37, 37, 37, 1);
  outline: none;
}

.inline-input:focus + .focus-indicator {
  opacity: 1;
  transform: scaleX(1);
}
```

### Error States
- **Border**: Solid red border (#d63031)
- **Background**: Subtle red tint
- **Label**: Red color for associated label
- **Animation**: Subtle shake on validation failure

## Mantine Implementation

### Core Component
```tsx
import { TextInput, Box, Text, Group, Stack } from '@mantine/core';
import { forwardRef } from 'react';

interface InlineLabelInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  helperText?: string;
  disabled?: boolean;
}

const InlineLabelInput = forwardRef<HTMLInputElement, InlineLabelInputProps>(({
  label,
  value,
  onChange,
  error,
  required,
  type = 'text',
  placeholder,
  helperText,
  disabled
}, ref) => {
  return (
    <Stack spacing="xs">
      <Group spacing="md" align="flex-start" className="input-row">
        <Text
          className="inline-label"
          component="label"
          size="sm"
          weight={500}
          style={{
            width: '140px',
            textAlign: 'right',
            color: error ? '#d63031' : 'var(--mantine-color-wcr-0)',
            lineHeight: '40px', // Align with input height
            flexShrink: 0,
            paddingTop: '2px'
          }}
        >
          {label}
          {required && <span style={{ color: '#d63031', marginLeft: '2px' }}>*</span>}
        </Text>
        
        <Box style={{ flex: 1, position: 'relative' }}>
          <TextInput
            ref={ref}
            type={type}
            value={value}
            onChange={(e) => onChange(e.target.value)}
            placeholder={placeholder}
            error={!!error}
            disabled={disabled}
            className="inline-input"
            styles={{
              input: {
                backgroundColor: 'var(--mantine-color-dark-6)',
                borderColor: error ? '#d63031' : 'var(--mantine-color-wcr-2)',
                color: 'var(--mantine-color-wcr-0)',
                fontSize: '16px',
                height: '40px',
                padding: '8px 12px',
                borderRadius: '4px',
                border: '1px solid',
                transition: 'all 0.2s ease',
                '&:hover:not(:disabled)': {
                  borderColor: error ? '#d63031' : 'var(--mantine-color-wcr-4)',
                  backgroundColor: 'rgba(37, 37, 37, 0.8)'
                },
                '&:focus': {
                  borderColor: error ? '#d63031' : 'var(--mantine-color-wcr-6)',
                  boxShadow: error 
                    ? '0 0 0 1px #d63031' 
                    : '0 0 0 1px var(--mantine-color-wcr-6)',
                  backgroundColor: 'var(--mantine-color-dark-6)'
                },
                '&:disabled': {
                  backgroundColor: 'var(--mantine-color-dark-7)',
                  color: 'var(--mantine-color-dark-2)',
                  cursor: 'not-allowed'
                }
              }
            }}
          />
          
          {/* Subtle underline animation on focus */}
          <Box
            className="focus-indicator"
            style={{
              position: 'absolute',
              bottom: '0',
              left: '0',
              right: '0',
              height: '2px',
              background: error ? '#d63031' : 'var(--mantine-color-wcr-6)',
              opacity: 0,
              transform: 'scaleX(0)',
              transformOrigin: 'center',
              transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)'
            }}
          />
        </Box>
      </Group>
      
      {(error || helperText) && (
        <Group spacing="md">
          <Box style={{ width: '140px', flexShrink: 0 }} /> {/* Spacer for alignment */}
          <Box style={{ flex: 1 }}>
            {error && (
              <Text size="xs" c="red" style={{ marginBottom: '2px' }}>
                {error}
              </Text>
            )}
            {helperText && !error && (
              <Text size="xs" c="dimmed">
                {helperText}
              </Text>
            )}
          </Box>
        </Group>
      )}
    </Stack>
  );
});

InlineLabelInput.displayName = 'InlineLabelInput';
```

### Form Layout Example
```tsx
const RegistrationFormInline = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: '',
    preferredName: ''
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  return (
    <Box
      style={{
        backgroundColor: 'var(--mantine-color-dark-7)',
        padding: '40px',
        borderRadius: '8px',
        border: '1px solid var(--mantine-color-wcr-2)',
        maxWidth: '600px',
        margin: '0 auto'
      }}
    >
      <Stack spacing="lg">
        <Box mb="lg">
          <Text size="xl" weight={600} c="wcr.0" mb="xs">
            Join Our Community
          </Text>
          <Text size="sm" c="dimmed">
            Create your account to access events and connect with the rope community
          </Text>
        </Box>
        
        <InlineLabelInput
          label="Email Address"
          value={formData.email}
          onChange={(value) => setFormData({...formData, email: value})}
          type="email"
          placeholder="your.email@example.com"
          required
          error={errors.email}
          helperText="We'll never share your email with anyone"
        />
        
        <InlineLabelInput
          label="Password"
          value={formData.password}
          onChange={(value) => setFormData({...formData, password: value})}
          type="password"
          placeholder="Choose a strong password"
          required
          error={errors.password}
          helperText="At least 8 characters with numbers and symbols"
        />
        
        <InlineLabelInput
          label="Confirm Password"
          value={formData.confirmPassword}
          onChange={(value) => setFormData({...formData, confirmPassword: value})}
          type="password"
          placeholder="Confirm your password"
          required
          error={errors.confirmPassword}
        />
        
        <InlineLabelInput
          label="Preferred Name"
          value={formData.preferredName}
          onChange={(value) => setFormData({...formData, preferredName: value})}
          placeholder="How should we address you?"
          helperText="This will be displayed in your profile"
        />
        
        <InlineLabelInput
          label="Scene Name"
          value={formData.sceneName}
          onChange={(value) => setFormData({...formData, sceneName: value})}
          placeholder="Your rope scene identity"
          helperText="Optional - used for community interactions"
        />
        
        <InlineLabelInput
          label="Phone Number"
          value={formData.phone}
          onChange={(value) => setFormData({...formData, phone: value})}
          type="tel"
          placeholder="(555) 123-4567"
          helperText="For event notifications and emergencies"
        />
        
        <Group spacing="md" mt="xl">
          <Box style={{ width: '140px', flexShrink: 0 }} /> {/* Spacer */}
          <Box style={{ flex: 1 }}>
            <Button
              size="md"
              variant="filled"
              color="wcr.7"
              fullWidth
              style={{
                height: '44px',
                fontWeight: 600
              }}
            >
              Create Account
            </Button>
          </Box>
        </Group>
      </Stack>
    </Box>
  );
};
```

### Compact Variant
```tsx
const CompactInlineForm = () => {
  return (
    <Stack spacing="md">
      <InlineLabelInput
        label="Search Events"
        value={searchTerm}
        onChange={setSearchTerm}
        placeholder="Enter keywords..."
      />
      
      <Group spacing="md">
        <Text
          size="sm"
          weight={500}
          style={{
            width: '140px',
            textAlign: 'right',
            color: 'var(--mantine-color-wcr-0)',
            lineHeight: '36px'
          }}
        >
          Date Range
        </Text>
        
        <Group spacing="xs" style={{ flex: 1 }}>
          <DateInput
            value={startDate}
            onChange={setStartDate}
            placeholder="Start date"
            style={{ flex: 1 }}
          />
          <Text size="sm" c="dimmed">to</Text>
          <DateInput
            value={endDate}
            onChange={setEndDate}
            placeholder="End date"
            style={{ flex: 1 }}
          />
        </Group>
      </Group>
    </Stack>
  );
};
```

## Responsive Behavior

### Mobile (xs - sm: < 768px)
```tsx
// Stack labels above inputs on mobile
const MobileInlineInput = ({ label, ...props }) => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  
  if (isMobile) {
    return (
      <Stack spacing="xs">
        <Text size="sm" weight={500} c="wcr.0">
          {label}
          {props.required && <span style={{ color: '#d63031' }}>*</span>}
        </Text>
        <TextInput {...props} />
      </Stack>
    );
  }
  
  return <InlineLabelInput label={label} {...props} />;
};
```

### Tablet (md: 768px - 991px)
- Label width: 120px (reduced)
- Font size: 13px for labels
- Maintain inline layout

### Desktop (lg+: 992px+)
- Full 140px label width
- Standard font sizes
- Optimal spacing and proportions

## Accessibility Features

### WCAG 2.1 AA Compliance
```tsx
// Enhanced accessibility
<TextInput
  aria-label={label}
  aria-required={required}
  aria-invalid={!!error}
  aria-describedby={
    [
      helperText && `${id}-helper`,
      error && `${id}-error`
    ].filter(Boolean).join(' ') || undefined
  }
/>

{helperText && (
  <Text id={`${id}-helper`} size="xs" c="dimmed">
    {helperText}
  </Text>
)}

{error && (
  <Text id={`${id}-error`} size="xs" c="red" role="alert">
    {error}
  </Text>
)}
```

### Keyboard Navigation
- Tab order: Label → Input → Next field
- Labels are clickable and focus input
- Clear focus indicators
- Logical reading order maintained

## Performance Considerations

### Optimizations
```tsx
// Memoize static styles
const useInlineStyles = createStyles((theme) => ({
  inputRow: {
    '&:hover': {
      '& .inline-label': {
        color: theme.colors.wcr[4]
      }
    }
  },
  
  label: {
    width: '140px',
    textAlign: 'right',
    flexShrink: 0,
    transition: 'color 0.2s ease'
  },
  
  input: {
    transition: 'all 0.2s ease',
    '&:hover': {
      borderColor: theme.colors.wcr[4]
    }
  }
}));
```

### Bundle Size
- Minimal CSS overhead
- No complex animations
- Reusable component patterns

## Brand Integration

### WitchCityRope Aesthetic
- **Clean Professional**: Emphasizes content over decoration
- **Sophisticated**: Consistent alignment creates polish
- **Accessible**: High contrast and clear hierarchy
- **Community-Focused**: Reduces barriers to participation

### Dark Theme Optimization
- Subtle backgrounds that don't compete with text
- High contrast ratios for all text elements
- Warm accent colors from brand palette
- Consistent visual rhythm

## Usage Guidelines

### When to Use
- **Administrative Forms**: User management, settings
- **Data Entry**: Structured information collection
- **Professional Contexts**: Member profiles, event management
- **Dense Forms**: Multiple fields need efficient layout

### When Not to Use
- **Simple Forms**: 1-3 fields (consider stacked layout)
- **Mobile-First**: Primarily mobile users
- **Creative Forms**: When personality is more important than efficiency
- **Long Labels**: Labels that don't fit in 140px width

### Best Practices
1. **Consistent Label Width**: Maintain 140px across all forms
2. **Right-Align Labels**: Creates clean edge against inputs
3. **Logical Grouping**: Group related fields with spacing
4. **Required Indicators**: Use consistent * placement
5. **Helper Text**: Align with input field, not label

## Form Validation Patterns

### Real-time Validation
```tsx
const useFieldValidation = (value: string, rules: ValidationRule[]) => {
  const [error, setError] = useState<string>('');
  const [touched, setTouched] = useState(false);
  
  useEffect(() => {
    if (touched && value) {
      const validation = validateField(value, rules);
      setError(validation.error || '');
    }
  }, [value, touched, rules]);
  
  return { error, setTouched };
};
```

### Error Display
- Errors appear below input field
- Maintain alignment with helper text
- Clear, actionable error messages
- Immediate feedback on correction

## Testing Checklist

### Visual Testing
- [ ] Labels align consistently across all fields
- [ ] Responsive behavior works correctly
- [ ] Error states display properly
- [ ] Focus indicators are visible
- [ ] Color contrast meets accessibility standards

### Functional Testing
- [ ] Keyboard navigation works smoothly
- [ ] Labels are clickable and focus inputs
- [ ] Form submission handles validation correctly
- [ ] Helper text provides useful guidance
- [ ] Error messages are clear and actionable

### Cross-browser Testing
- [ ] Chrome, Firefox, Safari, Edge compatibility
- [ ] Mobile browser testing
- [ ] Screen reader compatibility
- [ ] High contrast mode support

---

*This inline minimal design provides a clean, professional form experience that maximizes readability and efficiency while maintaining WitchCityRope's sophisticated brand aesthetic.*