# WitchCityRope Form Components - Mantine v7

## Overview

This directory contains standardized form components for the WitchCityRope React application built with Mantine v7. All components implement the business requirements from `/docs/standards-processes/forms-validation-requirements.md` with React Hook Form + Zod validation, TypeScript, and accessibility.

## Mantine v7 Architecture

### Technology Stack

- **UI Framework**: Mantine v7 (Core, Form, Hooks, Dates)
- **Form Management**: Mantine Form with React Hook Form support
- **Validation**: Zod schemas with Mantine form resolver
- **Icons**: Tabler Icons (included with Mantine)
- **Styling**: Mantine's CSS-in-JS theming system
- **Accessibility**: WCAG 2.1 AA compliance built-in

### Component Hierarchy

```
FormField (Mantine Stack wrapper)
├── Label (with required indicator)
├── Input Component (TextInput, Select, etc.)
├── ValidationMessage (error display)
└── HelperText (guidance)
```

## Available Components

### Core Mantine Components

- **BaseInput** - TextInput with validation and loading states
- **BaseSelect** - Select dropdown with option management  
- **BaseTextarea** - Textarea with auto-resize
- **ValidationMessage** - Reusable error message display
- **FormField** - Stack wrapper with consistent spacing

### Specialized WitchCityRope Components

- **EmailInput** - Email validation with async uniqueness check
- **PasswordInput** - Password input with strength indicator and visibility toggle
- **SceneNameInput** - Scene name validation with async uniqueness check
- **PhoneInput** - Phone number input with formatting
- **EmergencyContactGroup** - Complete emergency contact fieldset

## Business Rule Implementation

### Password Requirements (8+ characters, mixed case, digit, special)

```tsx
import { PasswordInput } from '@/components/forms';

// Automatic validation and strength indicator
<PasswordInput
  label="Password"
  description="Must contain uppercase, lowercase, number, and special character"
  required
  {...form.getInputProps('password')}
  data-testid="password-input"
/>
```

### Email Uniqueness Validation

```tsx
import { EmailInput } from '@/components/forms';

// Includes async uniqueness checking with 500ms debounce
<EmailInput
  label="Email address"
  required
  {...form.getInputProps('email')}
  data-testid="email-input"
/>
```

### Scene Name Validation (2-50 chars, alphanumeric + underscore/hyphen)

```tsx
import { SceneNameInput } from '@/components/forms';

// Validates regex pattern and async uniqueness
<SceneNameInput
  label="Scene name" 
  description="This is how you'll be known in the community"
  required
  {...form.getInputProps('sceneName')}
  data-testid="scene-name-input"
/>
```

## Usage Examples

### Basic Registration Form with Mantine

```tsx
import { useForm, zodResolver } from '@mantine/form';
import { TextInput, PasswordInput, Button, Stack } from '@mantine/core';
import { z } from 'zod';
import { EmailInput, PasswordInput, SceneNameInput } from '@/components/forms';

const registrationSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  sceneName: z
    .string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name must not exceed 50 characters')
    .regex(/^[a-zA-Z0-9_-]+$/, 'Scene name can only contain letters, numbers, hyphens, and underscores'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]/, 
           'Password must contain uppercase, lowercase, number, and special character'),
  confirmPassword: z.string()
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
});

type RegistrationFormValues = z.infer<typeof registrationSchema>;

export const RegistrationForm = () => {
  const form = useForm<RegistrationFormValues>({
    validate: zodResolver(registrationSchema),
    initialValues: {
      email: '',
      sceneName: '',
      password: '',
      confirmPassword: ''
    }
  });

  const handleSubmit = async (values: RegistrationFormValues) => {
    try {
      await authService.register(values);
    } catch (error) {
      // Handle API errors with Mantine notifications
    }
  };

  return (
    <form onSubmit={form.onSubmit(handleSubmit)} data-testid="registration-form">
      <Stack gap="md">
        <EmailInput
          label="Email address"
          placeholder="your.email@example.com"
          required
          {...form.getInputProps('email')}
          data-testid="email-input"
        />

        <SceneNameInput
          label="Scene name"
          placeholder="YourSceneName"
          description="This is how you'll be known in the community"
          required
          {...form.getInputProps('sceneName')}
          data-testid="scene-name-input"
        />

        <PasswordInput
          label="Password"
          description="Must contain uppercase, lowercase, number, and special character"
          required
          {...form.getInputProps('password')}
          data-testid="password-input"
        />

        <PasswordInput
          label="Confirm password"
          required
          {...form.getInputProps('confirmPassword')}
          data-testid="confirm-password-input"
        />

        <Button 
          type="submit" 
          loading={form.isSubmitting}
          data-testid="submit-button"
        >
          Create Account
        </Button>
      </Stack>
    </form>
  );
};
```

### Emergency Contact Form

```tsx
import { Stack, Group, TextInput } from '@mantine/core';
import { PhoneInput } from '@/components/forms';

<Stack gap="sm">
  <TextInput
    label="Emergency contact name"
    placeholder="Jane Smith"
    required
    {...form.getInputProps('emergencyContact.name')}
    data-testid="emergency-contact-name"
  />
  
  <PhoneInput
    label="Emergency contact phone"
    placeholder="(555) 123-4567"
    required
    {...form.getInputProps('emergencyContact.phone')}
    data-testid="emergency-contact-phone"
  />
  
  <TextInput
    label="Relationship to emergency contact"
    placeholder="Spouse, Partner, Friend, etc."
    required
    {...form.getInputProps('emergencyContact.relationship')}
    data-testid="emergency-contact-relationship"
  />
</Stack>
```

## Component Props Interface

### BaseInput (Mantine TextInput wrapper)

```tsx
interface BaseInputProps extends TextInputProps {
  loading?: boolean;
  asyncValidation?: boolean;
  'data-testid'?: string;
}
```

### EmailInput

```tsx
interface EmailInputProps extends BaseInputProps {
  checkUniqueness?: boolean;
  debounceMs?: number;
  excludeCurrentUser?: boolean;
}
```

### PasswordInput

```tsx
interface PasswordInputProps extends PasswordInputProps {
  showStrengthMeter?: boolean;
  strengthRequirements?: PasswordRequirement[];
}

interface PasswordRequirement {
  re: RegExp;
  label: string;
}
```

### SceneNameInput

```tsx
interface SceneNameInputProps extends BaseInputProps {
  checkUniqueness?: boolean;
  debounceMs?: number;
  excludeCurrentUser?: boolean;
}
```

## Mantine Theming

### WitchCityRope Theme Configuration

```tsx
import { MantineProvider, createTheme } from '@mantine/core';

const theme = createTheme({
  primaryColor: 'purple',
  defaultRadius: 'md',
  fontFamily: 'Inter, sans-serif',
  components: {
    TextInput: {
      defaultProps: {
        size: 'md',
        variant: 'filled'
      },
      styles: {
        input: {
          backgroundColor: 'var(--mantine-color-dark-6)',
          borderColor: 'var(--mantine-color-dark-4)',
          color: 'var(--mantine-color-white)',
          '&:focus': {
            borderColor: 'var(--mantine-color-purple-5)',
            boxShadow: '0 0 0 2px var(--mantine-color-purple-5)'
          }
        },
        label: {
          color: 'var(--mantine-color-gray-3)',
          fontWeight: 500
        },
        error: {
          color: 'var(--mantine-color-red-4)'
        }
      }
    },
    PasswordInput: {
      defaultProps: {
        size: 'md',
        variant: 'filled'
      }
    },
    Select: {
      defaultProps: {
        size: 'md',
        variant: 'filled'
      }
    }
  }
});

export const App = () => (
  <MantineProvider theme={theme}>
    {/* App content */}
  </MantineProvider>
);
```

## Error Message Standards

### Required Field Indicators

All required fields show a red asterisk (*) automatically with Mantine's `withAsterisk` prop:

```tsx
<TextInput
  label="Email address"
  withAsterisk
  required
  {...form.getInputProps('email')}
/>
```

### Error Display

Mantine automatically handles error display from form validation:

```tsx
// Error messages from business requirements are preserved
const errors = {
  email: {
    required: "Email is required",
    invalid: "Please enter a valid email address", 
    exists: "An account with this email already exists"
  },
  sceneName: {
    required: "Scene name is required",
    tooShort: "Scene name must be at least 2 characters",
    tooLong: "Scene name must not exceed 50 characters", 
    invalidChars: "Scene name can only contain letters, numbers, hyphens, and underscores",
    exists: "This scene name is already taken"
  },
  password: {
    required: "Password is required",
    tooShort: "Password must be at least 8 characters",
    missingUpper: "Password must contain at least one uppercase letter",
    missingLower: "Password must contain at least one lowercase letter", 
    missingDigit: "Password must contain at least one number",
    missingSpecial: "Password must contain at least one special character (@$!%*?&)"
  }
};
```

## Accessibility Features

### ARIA Support (Built into Mantine)

Mantine components include proper ARIA attributes automatically:

- `aria-invalid` when field has errors
- `aria-required` for required fields  
- `aria-describedby` linking error messages and descriptions
- `role="alert"` for error announcements

### Screen Reader Support

- **Labels**: Properly associated with inputs
- **Error Messages**: Announced with `aria-live` regions
- **Helper Text**: Associated with inputs via `aria-describedby`
- **Required Fields**: Indicated with `aria-required` and visual asterisk

### Keyboard Navigation

- **Tab Order**: Natural tab sequence through form elements
- **Focus Management**: Clear focus indicators with Mantine's focus ring
- **Enter Key**: Submits forms when appropriate
- **Escape Key**: Clears modals and overlays

## Loading and Async Validation

### Async Validation States

```tsx
// Built-in loading states for async validation
<EmailInput
  label="Email address"
  loading={isValidatingEmail}
  {...form.getInputProps('email')}
/>

// Custom hook for async validation with debouncing
const useAsyncValidation = (value: string, validator: Function, delay = 500) => {
  const [isValidating, setIsValidating] = useState(false);
  // Implementation details...
};
```

### Form Submission States

```tsx
// Mantine Button with loading state
<Button
  type="submit"
  loading={form.isSubmitting}
  disabled={!form.isValid()}
>
  {form.isSubmitting ? 'Creating Account...' : 'Create Account'}
</Button>
```

## Testing with Mantine Components

### Test Data Attributes

All components include `data-testid` attributes:

```tsx
// Component usage
<EmailInput data-testid="email-input" />

// Test selection  
const emailInput = screen.getByTestId('email-input');
```

### Testing Utilities

```tsx
export const mantineFormTestUtils = {
  fillInput: async (testId: string, value: string) => {
    const input = screen.getByTestId(testId);
    await user.clear(input);
    await user.type(input, value);
  },
  
  selectOption: async (testId: string, option: string) => {
    const select = screen.getByTestId(testId);
    await user.click(select);
    await user.click(screen.getByText(option));
  },
  
  submitForm: async (testId: string = 'form') => {
    const form = screen.getByTestId(testId);
    await user.click(within(form).getByRole('button', { name: /submit/i }));
  },
  
  expectError: (fieldTestId: string, message?: string) => {
    const field = screen.getByTestId(fieldTestId);
    const error = within(field).getByText(/error/i);
    expect(error).toBeInTheDocument();
    if (message) {
      expect(error).toHaveTextContent(message);
    }
  }
};
```

## Migration from Chakra UI

### Component Mapping

```tsx
// Before (Chakra UI)
import { Input, FormControl, FormLabel, FormErrorMessage } from '@chakra-ui/react';

<FormControl isInvalid={!!error}>
  <FormLabel>Email</FormLabel>
  <Input {...register('email')} />
  <FormErrorMessage>{error}</FormErrorMessage>
</FormControl>

// After (Mantine)
import { TextInput } from '@mantine/core';

<TextInput
  label="Email"
  {...form.getInputProps('email')}
/>
```

### Styling Migration

```tsx
// Chakra UI styling
<Input
  bg="gray.700"
  borderColor="gray.600"
  color="white"
  _focus={{
    borderColor: 'purple.500',
    boxShadow: '0 0 0 1px purple.500'
  }}
/>

// Mantine theming (configured globally)
<TextInput /> // Styles applied via theme
```

## Performance Optimization

### Component Memoization

```tsx
import { memo } from 'react';

const EmailInput = memo<EmailInputProps>(({ ...props }) => {
  // Component implementation
});
```

### Bundle Optimization

```tsx
// Tree-shaking friendly imports
import { TextInput, PasswordInput, Button } from '@mantine/core';
import { IconEye, IconEyeOff } from '@tabler/icons-react';
```

## Installation and Setup

### Required Packages

```bash
npm install @mantine/core @mantine/hooks @mantine/form @mantine/dates
npm install @tabler/icons-react
npm install @hookform/resolvers zod
```

### PostCSS Configuration

```js
// postcss.config.js
module.exports = {
  plugins: {
    '@mantine/postcss-preset': {}
  }
};
```

### App Setup

```tsx
import '@mantine/core/styles.css';
import '@mantine/dates/styles.css';

import { MantineProvider } from '@mantine/core';
import { theme } from './theme';

function App() {
  return (
    <MantineProvider theme={theme}>
      {/* Your app */}
    </MantineProvider>
  );
}
```

## Related Documentation

- [Forms Validation Requirements](../../../../docs/standards-processes/forms-validation-requirements.md) - Business rules and requirements
- [Forms Standardization](../../../../docs/standards-processes/forms-standardization.md) - Complete architecture documentation
- [Frontend Lessons Learned](../../../../docs/lessons-learned/frontend-lessons-learned.md) - React best practices
- [Mantine Documentation](https://mantine.dev/) - Official Mantine v7 documentation

## Implementation Status

- [x] Core Mantine form components (TextInput, PasswordInput, Select)
- [x] Business rule validation schemas (Email, Scene Name, Password)
- [x] Accessibility implementation (ARIA, screen reader support)
- [x] Error handling and display patterns
- [x] Loading states and async validation
- [x] Testing utilities and data-testid attributes
- [ ] Specialized components (EmailInput, SceneNameInput, PasswordInput)
- [ ] Emergency contact component group
- [ ] Phone number input with formatting
- [ ] Advanced form patterns (multi-step, conditional fields)
- [ ] Performance optimization implementation

## Support

For questions about Mantine form components:
1. Check Mantine v7 documentation at https://mantine.dev/
2. Review business requirements in forms-validation-requirements.md
3. Consult existing form implementations in `/apps/web/src/pages/`
4. Contact the development team for architectural guidance