# WitchCityRope React Forms Standardization

## Overview

This document outlines the comprehensive forms standardization system for WitchCityRope using Mantine v7 with React Hook Form + Zod validation. This system implements all business requirements from `/docs/standards-processes/forms-validation-requirements.md` with a modern, accessible component library approach.

**Status**: Updated for Mantine v7 migration from Chakra UI
**Last Updated**: 2025-08-17

## Architecture

### Form Validation Stack

```
┌─────────────────────────────────┐
│   React Components (UI Layer)   │
├─────────────────────────────────┤
│   Custom Form Components        │
│   (BaseInput, BaseSelect, etc.) │
├─────────────────────────────────┤
│   React Hook Form + Zod         │
├─────────────────────────────────┤
│   API Error Response Handling   │
├─────────────────────────────────┤
│   Backend Validation (.NET API) │
└─────────────────────────────────┘
```

### Core Principles

1. **Frontend-First Validation**: Immediate user feedback with React Hook Form + Zod
2. **Backend Integration**: Graceful handling of server-side validation responses
3. **Accessibility**: Full ARIA support and screen reader compatibility
4. **Consistency**: Unified styling and behavior across all forms
5. **Type Safety**: Complete TypeScript coverage for forms and validation
6. **Performance**: Optimized rendering with minimal re-renders

## Technology Stack

### Mantine v7 Implementation
- **UI Library**: Mantine v7 (Core, Form, Hooks)
- **Form Library**: Mantine Form with React Hook Form support
- **Validation**: Zod v4.0.17 with `@mantine/form` resolver
- **Icons**: Tabler Icons (included with Mantine)
- **HTTP Client**: Axios v1.11.0 with interceptors
- **Styling**: Mantine's CSS-in-JS theming system

### Previous Implementation (Chakra UI)
The system was previously implemented with:
- **UI Library**: Chakra UI v3.24.2
- **Styling**: Tailwind CSS v4.1.12
- **Form Library**: React Hook Form v7.62.0
- **Migration**: All business logic and validation rules preserved

## Form Component Architecture

### Base Components

All form components follow a consistent pattern:

```typescript
interface BaseFormComponentProps {
  label?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  loading?: boolean;
  helperText?: string;
  'data-testid'?: string;
}
```

### Component Hierarchy

```
FormField (wrapper)
├── Label (with required indicator)
├── Input Component (BaseInput, BaseSelect, etc.)
├── ValidationMessage (error display)
└── HelperText (guidance)
```

## Validation Schema Patterns

### Standard Schemas

```typescript
// User Registration Schema
export const registerSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Please enter a valid email address'),
  
  sceneName: z
    .string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name must not exceed 50 characters')
    .regex(/^[a-zA-Z0-9_-]+$/, 'Scene name can only contain letters, numbers, hyphens, and underscores'),
  
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/, 
           'Password must contain uppercase, lowercase, number, and special character'),
  
  confirmPassword: z.string()
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"],
});

// Event Registration Schema
export const eventRegistrationSchema = z.object({
  eventId: z.string().uuid('Invalid event ID'),
  
  attendeeType: z.enum(['member', 'guest'], {
    errorMap: () => ({ message: 'Please select attendee type' })
  }),
  
  emergencyContact: z.object({
    name: z.string().min(1, 'Emergency contact name is required'),
    phone: z.string().regex(/^\+?1?[0-9]{10,14}$/, 'Please enter a valid phone number'),
    relationship: z.string().min(1, 'Relationship is required')
  }),
  
  medicalInfo: z.string().optional(),
  
  waiverSigned: z.boolean().refine(val => val === true, {
    message: 'You must sign the waiver to register'
  })
});
```

### Async Validation Patterns

```typescript
// Email uniqueness validation
export const createAsyncEmailValidator = (authService: AuthService) => {
  return async (email: string): Promise<boolean> => {
    if (!email || !z.string().email().safeParse(email).success) {
      return true; // Let Zod handle format validation
    }
    
    try {
      const isUnique = await authService.checkEmailUnique(email);
      return isUnique;
    } catch {
      return true; // Assume valid on network error
    }
  };
};

// Scene name uniqueness validation
export const createAsyncSceneNameValidator = (authService: AuthService) => {
  return async (sceneName: string): Promise<boolean> => {
    if (!sceneName || sceneName.length < 2) {
      return true; // Let Zod handle length validation
    }
    
    try {
      const isUnique = await authService.checkSceneNameUnique(sceneName);
      return isUnique;
    } catch {
      return true; // Assume valid on network error
    }
  };
};
```

## Error Response Format

### Frontend Error Types

```typescript
interface ValidationError {
  field: string;
  message: string;
  code?: string;
}

interface FormError {
  message: string;
  validationErrors?: ValidationError[];
  code?: string;
}

interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
  code?: string;
  timestamp: string;
}
```

### Backend Error Mapping

```typescript
export const mapApiErrorsToForm = (
  apiError: ApiErrorResponse,
  setError: UseFormSetError<any>
) => {
  if (apiError.errors) {
    // Map field-specific errors
    Object.entries(apiError.errors).forEach(([field, messages]) => {
      const fieldName = field.toLowerCase();
      const message = Array.isArray(messages) ? messages[0] : messages;
      setError(fieldName as any, {
        type: 'server',
        message
      });
    });
  } else {
    // Set general form error
    setError('root', {
      type: 'server',
      message: apiError.message || 'An unexpected error occurred'
    });
  }
};
```

## Accessibility Implementation

### ARIA Attributes

```typescript
export const getFieldAriaProps = (
  id: string,
  error?: string,
  required?: boolean,
  describedBy?: string
) => ({
  'aria-invalid': !!error,
  'aria-required': required,
  'aria-describedby': [
    error ? `${id}-error` : '',
    describedBy ? `${id}-help` : ''
  ].filter(Boolean).join(' ') || undefined
});
```

### Screen Reader Support

```typescript
// Validation message with proper announcement
const ValidationMessage: React.FC<ValidationMessageProps> = ({ 
  message, 
  fieldId 
}) => {
  if (!message) return null;
  
  return (
    <div
      id={`${fieldId}-error`}
      role="alert"
      aria-live="polite"
      className="text-red-500 text-sm mt-1"
    >
      {message}
    </div>
  );
};
```

## Loading and Disabled States

### Form Submission States

```typescript
interface FormSubmissionState {
  isSubmitting: boolean;
  isValidating: boolean;
  isValid: boolean;
  hasErrors: boolean;
}

export const useFormState = (formState: FormState<any>) => {
  return {
    isSubmitting: formState.isSubmitting,
    isValidating: formState.isValidating,
    isValid: formState.isValid && !formState.isSubmitting,
    hasErrors: Object.keys(formState.errors).length > 0,
    canSubmit: formState.isValid && !formState.isSubmitting && !formState.isValidating
  };
};
```

### Loading Indicators

```typescript
// Async validation loading state
const BaseInput: React.FC<BaseInputProps> = ({ 
  loading, 
  ...props 
}) => {
  return (
    <div className="relative">
      <input {...props} />
      {loading && (
        <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
          <Loader2 className="h-4 w-4 animate-spin text-gray-400" />
        </div>
      )}
    </div>
  );
};
```

## Success Feedback Patterns

### Toast Notifications

```typescript
export interface ToastNotification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title?: string;
  message: string;
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export const useToast = () => {
  const [toasts, setToasts] = useState<ToastNotification[]>([]);
  
  const showToast = useCallback((toast: Omit<ToastNotification, 'id'>) => {
    const id = nanoid();
    setToasts(prev => [...prev, { ...toast, id }]);
    
    if (toast.duration !== 0) {
      setTimeout(() => {
        setToasts(prev => prev.filter(t => t.id !== id));
      }, toast.duration || 5000);
    }
  }, []);
  
  return { toasts, showToast };
};
```

### Form Success States

```typescript
// Successful form submission feedback
export const FormSuccessMessage: React.FC<{
  message: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}> = ({ message, action }) => {
  return (
    <div className="bg-green-500/20 border border-green-500 text-green-200 px-4 py-3 rounded mb-4">
      <div className="flex items-center">
        <CheckCircle className="h-5 w-5 mr-2" />
        <span>{message}</span>
      </div>
      {action && (
        <button
          onClick={action.onClick}
          className="mt-2 text-green-300 hover:text-green-100 underline text-sm"
        >
          {action.label}
        </button>
      )}
    </div>
  );
};
```

## Common Form Patterns

### Login Form Pattern

```typescript
export const useLoginForm = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const { showToast } = useToast();
  
  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false
    }
  });
  
  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data);
      showToast({
        type: 'success',
        message: 'Welcome back! You have been logged in successfully.'
      });
      navigate('/dashboard');
    } catch (error) {
      if (error instanceof ApiError) {
        mapApiErrorsToForm(error.response, form.setError);
      } else {
        form.setError('root', {
          message: 'An unexpected error occurred. Please try again.'
        });
      }
    }
  };
  
  return {
    ...form,
    onSubmit: form.handleSubmit(onSubmit)
  };
};
```

### Registration Form Pattern

```typescript
export const useRegistrationForm = () => {
  const { register } = useAuth();
  const navigate = useNavigate();
  const { showToast } = useToast();
  
  const form = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    mode: 'onChange'
  });
  
  // Async validation for email uniqueness
  const emailValidation = useAsyncValidation(
    form.watch('email'),
    createAsyncEmailValidator(authService),
    500 // debounce delay
  );
  
  // Async validation for scene name uniqueness
  const sceneNameValidation = useAsyncValidation(
    form.watch('sceneName'),
    createAsyncSceneNameValidator(authService),
    500
  );
  
  const onSubmit = async (data: RegisterFormData) => {
    try {
      await register(data);
      showToast({
        type: 'success',
        message: 'Registration successful! Please check your email to verify your account.',
        duration: 10000
      });
      navigate('/login');
    } catch (error) {
      if (error instanceof ApiError) {
        mapApiErrorsToForm(error.response, form.setError);
      } else {
        form.setError('root', {
          message: 'Registration failed. Please try again.'
        });
      }
    }
  };
  
  return {
    ...form,
    onSubmit: form.handleSubmit(onSubmit),
    isEmailValidating: emailValidation.isValidating,
    isSceneNameValidating: sceneNameValidation.isValidating
  };
};
```

### Event Registration Form Pattern

```typescript
export const useEventRegistrationForm = (eventId: string) => {
  const { registerForEvent } = useEvents();
  const { user } = useAuth();
  const { showToast } = useToast();
  
  const form = useForm<EventRegistrationFormData>({
    resolver: zodResolver(eventRegistrationSchema),
    defaultValues: {
      eventId,
      attendeeType: user?.membershipType || 'guest',
      emergencyContact: {
        name: '',
        phone: '',
        relationship: ''
      },
      medicalInfo: '',
      waiverSigned: false
    }
  });
  
  const onSubmit = async (data: EventRegistrationFormData) => {
    try {
      await registerForEvent(data);
      showToast({
        type: 'success',
        message: 'Registration successful! You will receive a confirmation email shortly.',
        action: {
          label: 'View Event Details',
          onClick: () => navigate(`/events/${eventId}`)
        }
      });
    } catch (error) {
      if (error instanceof ApiError) {
        mapApiErrorsToForm(error.response, form.setError);
      } else {
        form.setError('root', {
          message: 'Registration failed. Please try again.'
        });
      }
    }
  };
  
  return {
    ...form,
    onSubmit: form.handleSubmit(onSubmit)
  };
};
```

## Testing Patterns

### Form Testing Utilities

```typescript
// Testing utility for form interactions
export const createFormTestUtils = (container: HTMLElement) => ({
  fillInput: (testId: string, value: string) => {
    const input = screen.getByTestId(testId) as HTMLInputElement;
    fireEvent.change(input, { target: { value } });
    return input;
  },
  
  selectOption: (testId: string, value: string) => {
    const select = screen.getByTestId(testId) as HTMLSelectElement;
    fireEvent.change(select, { target: { value } });
    return select;
  },
  
  submitForm: (testId: string = 'form') => {
    const form = screen.getByTestId(testId);
    fireEvent.submit(form);
    return form;
  },
  
  expectError: (fieldName: string, message?: string) => {
    const error = screen.getByTestId(`${fieldName}-error`);
    expect(error).toBeInTheDocument();
    if (message) {
      expect(error).toHaveTextContent(message);
    }
    return error;
  },
  
  expectNoError: (fieldName: string) => {
    const error = screen.queryByTestId(`${fieldName}-error`);
    expect(error).not.toBeInTheDocument();
  }
});
```

### Integration Testing

```typescript
// Example integration test for login form
describe('LoginForm Integration', () => {
  const mockAuthService = {
    login: jest.fn(),
    checkEmailUnique: jest.fn()
  };
  
  beforeEach(() => {
    jest.clearAllMocks();
  });
  
  it('should handle successful login', async () => {
    mockAuthService.login.mockResolvedValue({
      user: { id: '1', email: 'test@example.com', sceneName: 'TestUser' },
      token: 'mock-token'
    });
    
    render(<LoginPage />, {
      wrapper: ({ children }) => (
        <AuthProvider authService={mockAuthService}>
          <BrowserRouter>{children}</BrowserRouter>
        </AuthProvider>
      )
    });
    
    const utils = createFormTestUtils(document.body);
    
    utils.fillInput('email-input', 'test@example.com');
    utils.fillInput('password-input', 'password123');
    utils.submitForm('login-form');
    
    await waitFor(() => {
      expect(mockAuthService.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123'
      });
    });
  });
  
  it('should handle validation errors', async () => {
    render(<LoginPage />);
    
    const utils = createFormTestUtils(document.body);
    
    utils.fillInput('email-input', 'invalid-email');
    utils.submitForm('login-form');
    
    await waitFor(() => {
      utils.expectError('email', 'Please enter a valid email address');
    });
  });
  
  it('should handle server errors', async () => {
    mockAuthService.login.mockRejectedValue(
      new ApiError({
        success: false,
        message: 'Invalid credentials',
        errors: {
          email: ['Email not found'],
          password: ['Incorrect password']
        }
      })
    );
    
    render(<LoginPage />);
    
    const utils = createFormTestUtils(document.body);
    
    utils.fillInput('email-input', 'test@example.com');
    utils.fillInput('password-input', 'wrongpassword');
    utils.submitForm('login-form');
    
    await waitFor(() => {
      utils.expectError('email', 'Email not found');
      utils.expectError('password', 'Incorrect password');
    });
  });
});
```

## Migration to Mantine

### Mantine Integration Plan

When migrating to Mantine v7, the following changes will be needed:

1. **Replace Form Hook**:
```typescript
// Current: React Hook Form
const form = useForm<FormData>({
  resolver: zodResolver(schema)
});

// Mantine: @mantine/form
const form = useForm<FormData>({
  validate: zodResolver(schema),
  initialValues: defaultValues
});
```

2. **Update Component Imports**:
```typescript
// Current: Custom components
import { BaseInput, BaseSelect } from '@/components/forms';

// Mantine: Core components
import { TextInput, Select } from '@mantine/core';
```

3. **Styling Migration**:
```typescript
// Current: Tailwind classes
className="w-full px-3 py-2 border border-slate-600 rounded-md"

// Mantine: Component props and theme
<TextInput
  size="md"
  variant="filled"
  styles={(theme) => ({
    input: {
      backgroundColor: theme.colors.dark[6],
      borderColor: theme.colors.dark[4]
    }
  })}
/>
```

## Performance Optimization

### Form Performance Best Practices

1. **Debounced Validation**:
```typescript
const useAsyncValidation = (
  value: string,
  validator: (value: string) => Promise<boolean>,
  delay: number = 500
) => {
  const [isValidating, setIsValidating] = useState(false);
  const [isValid, setIsValid] = useState(true);
  
  const debouncedValidator = useMemo(
    () => debounce(async (val: string) => {
      if (!val) return;
      
      setIsValidating(true);
      try {
        const valid = await validator(val);
        setIsValid(valid);
      } finally {
        setIsValidating(false);
      }
    }, delay),
    [validator, delay]
  );
  
  useEffect(() => {
    debouncedValidator(value);
    return () => debouncedValidator.cancel();
  }, [value, debouncedValidator]);
  
  return { isValidating, isValid };
};
```

2. **Memoized Components**:
```typescript
const BaseInput = memo<BaseInputProps>(({ 
  label, 
  error, 
  ...inputProps 
}) => {
  return (
    <FormField label={label} error={error}>
      <input {...inputProps} />
    </FormField>
  );
});
```

3. **Optimized Re-renders**:
```typescript
// Use React Hook Form's controller for complex components
const ControlledSelect = ({ name, control, options, ...props }) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => (
        <BaseSelect
          {...field}
          {...props}
          error={fieldState.error?.message}
          options={options}
        />
      )}
    />
  );
};
```

## Related Documentation

- [Form Component Library](./README.md) - Complete component API reference
- [Validation Patterns](../validation-standardization/VALIDATION_STANDARDS.md) - Validation implementation details
- [Frontend Lessons Learned](../../lessons-learned/frontend-lessons-learned.md) - React form patterns and best practices
- [Authentication Patterns](../../functional-areas/authentication/) - Auth-specific form implementations

## Implementation Status

- [x] Core form validation patterns with React Hook Form + Zod
- [x] Error handling and API integration
- [x] Accessibility implementation
- [ ] Mantine v7 component migration
- [ ] Advanced form patterns (multi-step, conditional fields)
- [ ] Form analytics and error tracking
- [ ] Advanced testing utilities
- [ ] Performance optimization implementation

## Support

For questions about the forms standardization system:
1. Check existing form implementations in `/apps/web/src/pages/`
2. Review the form components in `/apps/web/src/components/forms/`
3. Consult React Hook Form and Zod documentation
4. Contact the development team for architectural guidance