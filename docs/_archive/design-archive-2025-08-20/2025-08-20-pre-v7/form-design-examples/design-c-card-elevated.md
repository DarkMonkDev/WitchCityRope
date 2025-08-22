# Design C: Card-Based Elevated
<!-- Last Updated: 2025-08-18 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Designer Agent -->
<!-- Status: Ready for Implementation -->

## Design Overview

A sophisticated card-based design that elevates form sections into distinct, floating containers with subtle shadows and depth. This approach creates visual hierarchy, reduces cognitive load by grouping related fields, and provides an elegant, modern interface that aligns with current UI trends while maintaining WitchCityRope's mystical aesthetic.

## Visual Characteristics

### Card Structure
- **Elevation**: Multiple shadow layers for depth
- **Background**: Semi-transparent dark cards with subtle texture
- **Borders**: Thin accent borders with gradient effects
- **Spacing**: Generous padding and section separation
- **Grouping**: Related fields organized in logical card sections

### Elevation System
```typescript
const cardElevationTheme = {
  shadows: {
    xs: '0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06)',
    sm: '0 4px 6px rgba(0, 0, 0, 0.1), 0 2px 4px rgba(0, 0, 0, 0.06)',
    md: '0 8px 15px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05)',
    lg: '0 12px 25px rgba(0, 0, 0, 0.15), 0 6px 12px rgba(0, 0, 0, 0.08)',
    xl: '0 20px 40px rgba(0, 0, 0, 0.2), 0 10px 20px rgba(0, 0, 0, 0.1)'
  },
  colors: {
    wcr: [
      '#f8f4e6', // ivory - text and accents
      '#e8ddd4', // light dusty - hover states
      '#d4a5a5', // dusty rose - borders
      '#c48b8b', // medium rose - secondary elements
      '#b47171', // deep rose - accent borders
      '#a45757', // dark rose - focus states
      '#9b4a75', // plum - primary accents
      '#880124', // burgundy - primary actions
      '#6b0119', // dark burgundy - pressed states
      '#2c2c2c'  // charcoal - base backgrounds
    ]
  },
  dark: {
    card: 'rgba(44, 44, 44, 0.8)',
    cardHover: 'rgba(44, 44, 44, 0.9)',
    cardFocus: 'rgba(44, 44, 44, 0.95)',
    border: 'rgba(212, 165, 165, 0.2)',
    borderAccent: 'rgba(155, 74, 117, 0.3)',
    glassmorphism: 'rgba(44, 44, 44, 0.6)'
  }
}
```

### Typography Hierarchy
- **Card Titles**: 'Bodoni Moda', 18px, semibold
- **Section Labels**: 'Source Sans 3', 14px, medium weight
- **Input Labels**: 'Source Sans 3', 13px, regular
- **Input Text**: 'Source Sans 3', 16px, regular
- **Helper Text**: 'Source Sans 3', 12px, regular

## Interaction Specifications

### Card Hover Effects
```css
.form-card {
  transition: all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94);
  backdrop-filter: blur(12px);
  border: 1px solid var(--card-border);
}

.form-card:hover {
  transform: translateY(-2px);
  box-shadow: 
    0 12px 25px rgba(0, 0, 0, 0.15),
    0 6px 12px rgba(0, 0, 0, 0.08),
    0 0 0 1px rgba(155, 74, 117, 0.2);
  background-color: var(--card-hover);
  border-color: var(--border-accent);
}

.form-card:focus-within {
  transform: translateY(-3px);
  box-shadow: 
    0 20px 40px rgba(0, 0, 0, 0.2),
    0 10px 20px rgba(0, 0, 0, 0.1),
    0 0 0 2px rgba(155, 74, 117, 0.4);
  background-color: var(--card-focus);
}
```

### Glassmorphism Effects
```css
.glassmorphism-card {
  background: linear-gradient(
    135deg,
    rgba(44, 44, 44, 0.7) 0%,
    rgba(155, 74, 117, 0.1) 100%
  );
  backdrop-filter: blur(20px) saturate(180%);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: 
    0 8px 32px rgba(31, 38, 135, 0.37),
    inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
```

### Field Focus Within Cards
```css
.card-input:focus {
  border-color: var(--mantine-color-wcr-6);
  box-shadow: 
    0 0 0 1px var(--mantine-color-wcr-6),
    0 4px 12px rgba(155, 74, 117, 0.2);
  background: linear-gradient(
    135deg,
    rgba(155, 74, 117, 0.05) 0%,
    rgba(136, 1, 36, 0.03) 100%
  );
}
```

## Mantine Implementation

### Core Card Component
```tsx
import { Paper, Stack, Text, TextInput, Group, Box, Button } from '@mantine/core';
import { ReactNode } from 'react';

interface FormCardProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
  className?: string;
  glassmorphism?: boolean;
}

const FormCard = ({ 
  title, 
  subtitle, 
  children, 
  className,
  glassmorphism = false 
}: FormCardProps) => {
  return (
    <Paper
      className={`form-card ${className || ''}`}
      p="xl"
      radius="lg"
      style={{
        background: glassmorphism 
          ? 'linear-gradient(135deg, rgba(44, 44, 44, 0.7) 0%, rgba(155, 74, 117, 0.1) 100%)'
          : 'rgba(44, 44, 44, 0.8)',
        backdropFilter: glassmorphism ? 'blur(20px) saturate(180%)' : 'blur(12px)',
        border: glassmorphism 
          ? '1px solid rgba(255, 255, 255, 0.1)'
          : '1px solid rgba(212, 165, 165, 0.2)',
        boxShadow: glassmorphism
          ? '0 8px 32px rgba(31, 38, 135, 0.37), inset 0 1px 0 rgba(255, 255, 255, 0.1)'
          : '0 8px 15px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05)',
        transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
        position: 'relative',
        overflow: 'hidden',
        '&:hover': {
          transform: 'translateY(-2px)',
          boxShadow: '0 12px 25px rgba(0, 0, 0, 0.15), 0 6px 12px rgba(0, 0, 0, 0.08)',
          borderColor: 'rgba(155, 74, 117, 0.3)'
        },
        '&:focus-within': {
          transform: 'translateY(-3px)',
          boxShadow: '0 20px 40px rgba(0, 0, 0, 0.2), 0 10px 20px rgba(0, 0, 0, 0.1)',
          borderColor: 'rgba(155, 74, 117, 0.4)'
        },
        '&::before': glassmorphism ? {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: '1px',
          background: 'linear-gradient(90deg, transparent, rgba(155, 74, 117, 0.6), transparent)'
        } : undefined
      }}
    >
      <Stack spacing="lg">
        {/* Card Header */}
        <Box>
          <Text
            size="lg"
            weight={600}
            c="wcr.0"
            style={{
              fontFamily: 'Bodoni Moda, serif',
              letterSpacing: '0.5px'
            }}
          >
            {title}
          </Text>
          {subtitle && (
            <Text size="sm" c="dimmed" mt="xs">
              {subtitle}
            </Text>
          )}
        </Box>
        
        {/* Card Content */}
        <Box>
          {children}
        </Box>
      </Stack>
    </Paper>
  );
};
```

### Enhanced Input Component for Cards
```tsx
interface CardInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  helperText?: string;
  description?: string;
}

const CardInput = ({
  label,
  value,
  onChange,
  error,
  required,
  type = 'text',
  placeholder,
  helperText,
  description
}: CardInputProps) => {
  return (
    <Box>
      <Text
        component="label"
        size="sm"
        weight={500}
        c="wcr.1"
        mb="xs"
        style={{ display: 'block' }}
      >
        {label}
        {required && <span style={{ color: '#ff6b6b', marginLeft: '4px' }}>*</span>}
      </Text>
      
      {description && (
        <Text size="xs" c="dimmed" mb="xs">
          {description}
        </Text>
      )}
      
      <TextInput
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        error={error}
        className="card-input"
        styles={{
          input: {
            backgroundColor: 'rgba(37, 37, 37, 0.6)',
            borderColor: error ? '#ff6b6b' : 'rgba(212, 165, 165, 0.3)',
            color: 'var(--mantine-color-wcr-0)',
            fontSize: '16px',
            height: '48px',
            borderRadius: '8px',
            border: '1px solid',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            backdropFilter: 'blur(8px)',
            '&:hover': {
              borderColor: error ? '#ff6b6b' : 'var(--mantine-color-wcr-4)',
              backgroundColor: 'rgba(37, 37, 37, 0.8)'
            },
            '&:focus': {
              borderColor: error ? '#ff6b6b' : 'var(--mantine-color-wcr-6)',
              boxShadow: error 
                ? '0 0 0 1px #ff6b6b, 0 4px 12px rgba(255, 107, 107, 0.2)'
                : '0 0 0 1px var(--mantine-color-wcr-6), 0 4px 12px rgba(155, 74, 117, 0.2)',
              background: error
                ? 'rgba(37, 37, 37, 0.8)'
                : 'linear-gradient(135deg, rgba(155, 74, 117, 0.05) 0%, rgba(136, 1, 36, 0.03) 100%)'
            }
          }
        }}
      />
      
      {(error || helperText) && (
        <Text size="xs" c={error ? 'red' : 'dimmed'} mt="xs">
          {error || helperText}
        </Text>
      )}
    </Box>
  );
};
```

### Complete Form Example
```tsx
const RegistrationFormElevated = () => {
  const [formData, setFormData] = useState({
    // Account Information
    email: '',
    password: '',
    confirmPassword: '',
    
    // Personal Information
    preferredName: '',
    sceneName: '',
    phone: '',
    
    // Community Preferences
    interests: '',
    experience: '',
    bio: ''
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  return (
    <Stack spacing="xl" style={{ maxWidth: '700px', margin: '0 auto' }}>
      {/* Account Information Card */}
      <FormCard
        title="Account Information"
        subtitle="Create your secure login credentials"
      >
        <Stack spacing="md">
          <CardInput
            label="Email Address"
            value={formData.email}
            onChange={(value) => setFormData({...formData, email: value})}
            type="email"
            placeholder="your.email@example.com"
            required
            error={errors.email}
            description="This will be your login username"
          />
          
          <Group grow>
            <CardInput
              label="Password"
              value={formData.password}
              onChange={(value) => setFormData({...formData, password: value})}
              type="password"
              placeholder="Choose a strong password"
              required
              error={errors.password}
              helperText="At least 8 characters with numbers and symbols"
            />
            
            <CardInput
              label="Confirm Password"
              value={formData.confirmPassword}
              onChange={(value) => setFormData({...formData, confirmPassword: value})}
              type="password"
              placeholder="Confirm your password"
              required
              error={errors.confirmPassword}
            />
          </Group>
        </Stack>
      </FormCard>
      
      {/* Personal Information Card */}
      <FormCard
        title="Personal Information"
        subtitle="Help us create your profile"
        glassmorphism
      >
        <Stack spacing="md">
          <Group grow>
            <CardInput
              label="Preferred Name"
              value={formData.preferredName}
              onChange={(value) => setFormData({...formData, preferredName: value})}
              placeholder="How should we address you?"
              required
              description="This name will be displayed in your profile"
            />
            
            <CardInput
              label="Scene Name"
              value={formData.sceneName}
              onChange={(value) => setFormData({...formData, sceneName: value})}
              placeholder="Your rope scene identity"
              description="Optional - used for community interactions"
            />
          </Group>
          
          <CardInput
            label="Phone Number"
            value={formData.phone}
            onChange={(value) => setFormData({...formData, phone: value})}
            type="tel"
            placeholder="(555) 123-4567"
            helperText="For event notifications and emergency contact"
          />
        </Stack>
      </FormCard>
      
      {/* Community Preferences Card */}
      <FormCard
        title="Community Preferences"
        subtitle="Tell us about your interests and experience"
      >
        <Stack spacing="md">
          <CardInput
            label="Rope Interests"
            value={formData.interests}
            onChange={(value) => setFormData({...formData, interests: value})}
            placeholder="Shibari, suspension, floor work, etc."
            description="What aspects of rope work interest you most?"
          />
          
          <CardInput
            label="Experience Level"
            value={formData.experience}
            onChange={(value) => setFormData({...formData, experience: value})}
            placeholder="Beginner, intermediate, advanced"
            description="Help us recommend appropriate events"
          />
          
          <Box>
            <Text
              component="label"
              size="sm"
              weight={500}
              c="wcr.1"
              mb="xs"
              style={{ display: 'block' }}
            >
              Brief Bio
            </Text>
            <Text size="xs" c="dimmed" mb="xs">
              Share a bit about yourself and what brings you to our community
            </Text>
            <Textarea
              value={formData.bio}
              onChange={(e) => setFormData({...formData, bio: e.target.value})}
              placeholder="Tell us about your journey with rope..."
              minRows={4}
              styles={{
                input: {
                  backgroundColor: 'rgba(37, 37, 37, 0.6)',
                  borderColor: 'rgba(212, 165, 165, 0.3)',
                  color: 'var(--mantine-color-wcr-0)',
                  borderRadius: '8px',
                  transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
                  '&:focus': {
                    borderColor: 'var(--mantine-color-wcr-6)',
                    boxShadow: '0 0 0 1px var(--mantine-color-wcr-6), 0 4px 12px rgba(155, 74, 117, 0.2)'
                  }
                }
              }}
            />
          </Box>
        </Stack>
      </FormCard>
      
      {/* Action Card */}
      <FormCard title="Ready to Join?">
        <Stack spacing="md">
          <Text size="sm" c="dimmed">
            By creating an account, you agree to our community guidelines and privacy policy.
            All members must be 18+ and commit to maintaining a safe, consensual environment.
          </Text>
          
          <Button
            size="lg"
            variant="gradient"
            gradient={{ from: 'wcr.6', to: 'wcr.7', deg: 45 }}
            fullWidth
            style={{
              height: '56px',
              fontWeight: 600,
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              borderRadius: '8px'
            }}
          >
            Create Account
          </Button>
          
          <Group position="center" spacing="xs">
            <Text size="sm" c="dimmed">Already have an account?</Text>
            <Button variant="subtle" size="sm" c="wcr.4">
              Sign In
            </Button>
          </Group>
        </Stack>
      </FormCard>
    </Stack>
  );
};
```

## Progressive Enhancement

### Card Animation Sequence
```tsx
const useCardAnimation = () => {
  const [isVisible, setIsVisible] = useState(false);
  
  useEffect(() => {
    const timer = setTimeout(() => setIsVisible(true), 100);
    return () => clearTimeout(timer);
  }, []);
  
  return {
    style: {
      opacity: isVisible ? 1 : 0,
      transform: isVisible ? 'translateY(0)' : 'translateY(20px)',
      transition: 'all 0.6s cubic-bezier(0.25, 0.46, 0.45, 0.94)'
    }
  };
};

// Usage in cards
const AnimatedFormCard = ({ children, delay = 0, ...props }) => {
  const animation = useCardAnimation();
  
  return (
    <FormCard
      {...props}
      style={{
        ...animation.style,
        transitionDelay: `${delay}ms`
      }}
    >
      {children}
    </FormCard>
  );
};
```

### Success State Card
```tsx
const SuccessCard = ({ message }: { message: string }) => {
  return (
    <FormCard
      title="Success!"
      glassmorphism
      style={{
        background: 'linear-gradient(135deg, rgba(76, 175, 80, 0.1) 0%, rgba(44, 44, 44, 0.8) 100%)',
        borderColor: 'rgba(76, 175, 80, 0.3)'
      }}
    >
      <Group>
        <CheckIcon size={24} color="green" />
        <Text c="green">{message}</Text>
      </Group>
    </FormCard>
  );
};
```

## Responsive Behavior

### Mobile (xs - sm)
```tsx
const responsiveCardStyles = {
  '@media (max-width: 767px)': {
    padding: 'md',
    margin: '0 xs',
    
    // Stack group inputs vertically
    '& .mantine-Group-root': {
      flexDirection: 'column',
      '& > *': {
        width: '100%'
      }
    }
  }
}
```

### Tablet (md)
- Reduce card padding slightly
- Maintain card structure
- Optimize group layouts

### Desktop (lg+)
- Full elevation effects
- Maximum card widths
- Optimal spacing and proportions

## Accessibility Features

### WCAG 2.1 AA Compliance
```tsx
// Enhanced card accessibility
<Paper
  role="region"
  aria-labelledby={`card-title-${id}`}
  tabIndex={0}
  onKeyDown={(e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      const firstInput = e.currentTarget.querySelector('input');
      firstInput?.focus();
    }
  }}
>
  <Text id={`card-title-${id}`}>
    {title}
  </Text>
</Paper>
```

### Focus Management
- Cards are keyboard navigable
- Clear focus indicators
- Logical tab order within cards
- Escape key handling for card navigation

## Performance Considerations

### Optimization Strategies
```tsx
// Virtualize cards for long forms
import { Virtuoso } from 'react-virtuoso';

const VirtualizedFormCards = ({ cards }) => (
  <Virtuoso
    totalCount={cards.length}
    itemContent={(index) => (
      <Box mb="xl">
        {cards[index]}
      </Box>
    )}
  />
);
```

### Backdrop Filter Performance
- Use `will-change: transform` sparingly
- Limit backdrop-filter to focused/hovered states
- Provide fallbacks for older browsers

## Brand Integration

### WitchCityRope Aesthetic
- **Mystical Elevation**: Cards feel like floating magical interfaces
- **Professional Depth**: Sophisticated visual hierarchy
- **Salem Influence**: Dark materials with warm accent lighting
- **Community Cards**: Each section feels like a welcoming space

### Glassmorphism Implementation
- Subtle transparency maintains readability
- Warm color gradients reflect brand palette
- Backdrop blur creates depth without distraction
- Border highlights add magical accent

## Usage Guidelines

### When to Use
- **Complex Forms**: Multiple sections of related information
- **Onboarding Flows**: Step-by-step user registration
- **Settings Pages**: Grouped configuration options
- **Profile Creation**: Distinct information categories

### When Not to Use
- **Simple Forms**: Single-section forms (1-5 fields)
- **Mobile-First Apps**: Performance concerns on older devices
- **High-Frequency Actions**: Quick data entry scenarios
- **Minimal Interfaces**: When simplicity is paramount

### Card Organization Principles
1. **Logical Grouping**: Related fields in same card
2. **Progressive Disclosure**: Most important information first
3. **Visual Hierarchy**: Primary cards more prominent
4. **Consistent Sizing**: Similar card heights when possible

## Testing Checklist

### Visual Testing
- [ ] Cards display proper elevation and shadows
- [ ] Glassmorphism effects render correctly
- [ ] Hover animations are smooth
- [ ] Focus states are clearly visible
- [ ] Responsive behavior maintains card integrity

### Performance Testing
- [ ] Smooth animations on lower-end devices
- [ ] Backdrop filter performance acceptable
- [ ] No layout shift during card interactions
- [ ] Loading states handle gracefully

### Accessibility Testing
- [ ] Screen reader navigation through cards
- [ ] Keyboard navigation between and within cards
- [ ] Focus management works correctly
- [ ] Color contrast meets standards in all states

---

*This card-based elevated design creates an immersive, sophisticated form experience that organizes complex information into digestible, elegant sections while maintaining excellent usability and performance.*