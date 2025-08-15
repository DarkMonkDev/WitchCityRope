# Form Validation Research

*Generated on August 13, 2025*

## Overview
This document researches React form validation strategies, libraries, and security patterns for the WitchCityRope migration from Blazor Server to React.

## Current WitchCityRope Validation System

### Existing Blazor Implementation
- **Validation Framework**: Blazor EditForm with DataAnnotationsValidator
- **Validation Attributes**: C# Data Annotations on models
- **Client-Side**: Real-time validation with Blazor components
- **Server-Side**: ASP.NET Core model validation
- **Custom Components**: WCR validation components with consistent styling

### Current Validation Features
```csharp
// Example from current implementation
public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string SceneName { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords don't match")]
    public string ConfirmPassword { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Age confirmation is required")]
    public bool AgeConfirmed { get; set; }
}
```

### Current Component Structure
```
Shared/Validation/Components/
├── WcrInputText.razor
├── WcrInputEmail.razor
├── WcrInputPassword.razor
├── WcrInputDate.razor
├── WcrInputNumber.razor
├── WcrInputSelect.razor
├── WcrInputTextArea.razor
├── WcrInputCheckbox.razor
├── WcrInputRadio.razor
├── WcrValidationMessage.razor
└── WcrValidationSummary.razor
```

## React Form Library Analysis (2025)

### **Option 1: React Hook Form + Zod (Recommended)**

#### **React Hook Form Advantages**
- **Performance**: Uncontrolled components reduce re-renders
- **Bundle Size**: Minimal overhead (~25KB)
- **API**: Simple and intuitive hook-based API
- **Integration**: Works with any validation library
- **Popularity**: 4.9M weekly downloads (double Formik's 2.5M)
- **TypeScript**: Excellent TypeScript support

#### **Zod Validation Benefits**
- **Type Safety**: Runtime validation with TypeScript inference
- **Modern**: Built for TypeScript-first development
- **Zero Dependencies**: Lightweight with no external dependencies
- **Composable**: Easy schema composition and reuse
- **Error Messages**: Clear, customizable error messages

#### **Implementation Example**
```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

// Schema definition
const registerSchema = z.object({
  email: z.string()
    .min(1, 'Email is required')
    .email('Invalid email format'),
  sceneName: z.string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name cannot exceed 50 characters'),
  password: z.string()
    .min(8, 'Password must be at least 8 characters')
    .max(100, 'Password cannot exceed 100 characters'),
  confirmPassword: z.string(),
  ageConfirmed: z.boolean()
    .refine(val => val === true, 'Age confirmation is required')
}).refine(data => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"]
});

type RegisterFormData = z.infer<typeof registerSchema>;

// Component implementation
const RegisterForm = () => {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    mode: 'onChange' // Real-time validation
  });

  const onSubmit = async (data: RegisterFormData) => {
    try {
      await authAPI.register(data);
    } catch (error) {
      // Handle server errors
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <WcrInput
        label="Email Address"
        {...register('email')}
        error={errors.email?.message}
        type="email"
      />
      
      <WcrInput
        label="Scene Name"
        {...register('sceneName')}
        error={errors.sceneName?.message}
      />
      
      <WcrPasswordInput
        label="Password"
        {...register('password')}
        error={errors.password?.message}
        showStrengthIndicator
      />
      
      <WcrInput
        label="Confirm Password"
        {...register('confirmPassword')}
        error={errors.confirmPassword?.message}
        type="password"
      />
      
      <WcrCheckbox
        label="I confirm that I am 21 years of age or older"
        {...register('ageConfirmed')}
        error={errors.ageConfirmed?.message}
      />
      
      <Button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Creating Account...' : 'Create Account'}
      </Button>
    </form>
  );
};
```

### **Option 2: Formik + Yup (Alternative for JavaScript)**

#### **When to Consider Formik + Yup**
- **Existing Codebase**: Already using Formik
- **JavaScript Projects**: Not using TypeScript extensively
- **Team Familiarity**: Team experienced with Formik patterns
- **Complex Forms**: Need Formik's form state management features

#### **Implementation Example**
```typescript
import { Formik, Form } from 'formik';
import * as Yup from 'yup';

const registerValidationSchema = Yup.object({
  email: Yup.string()
    .email('Invalid email format')
    .required('Email is required'),
  sceneName: Yup.string()
    .min(2, 'Scene name must be at least 2 characters')
    .max(50, 'Scene name cannot exceed 50 characters')
    .required('Scene name is required'),
  password: Yup.string()
    .min(8, 'Password must be at least 8 characters')
    .max(100, 'Password cannot exceed 100 characters')
    .required('Password is required'),
  confirmPassword: Yup.string()
    .oneOf([Yup.ref('password')], "Passwords don't match")
    .required('Confirm password is required'),
  ageConfirmed: Yup.boolean()
    .oneOf([true], 'Age confirmation is required')
});

const RegisterForm = () => (
  <Formik
    initialValues={{
      email: '',
      sceneName: '',
      password: '',
      confirmPassword: '',
      ageConfirmed: false
    }}
    validationSchema={registerValidationSchema}
    onSubmit={async (values, { setSubmitting }) => {
      try {
        await authAPI.register(values);
      } catch (error) {
        // Handle errors
      } finally {
        setSubmitting(false);
      }
    }}
  >
    {({ values, errors, touched, isSubmitting }) => (
      <Form>
        <WcrFormikInput
          name="email"
          label="Email Address"
          type="email"
        />
        <WcrFormikInput
          name="sceneName"
          label="Scene Name"
        />
        {/* More fields */}
      </Form>
    )}
  </Formik>
);
```

## Validation Security Patterns (2025)

### **Dual-Layer Validation Architecture**

#### **Client-Side Validation (UX)**
- **Purpose**: Immediate feedback and improved user experience
- **Security**: Not trusted - easily bypassed
- **Implementation**: Real-time validation as users type
- **Benefits**: Reduces server load, improves perceived performance

#### **Server-Side Validation (Security)**
- **Purpose**: Security enforcement and data integrity
- **Security**: Trusted source of truth for validation
- **Implementation**: API endpoint validation before processing
- **Benefits**: Prevents malicious data, ensures business rules

### **Implementation Pattern**
```typescript
// Client-side schema (for UX)
const clientRegistrationSchema = z.object({
  email: z.string().email('Invalid email format'),
  sceneName: z.string().min(2, 'Too short').max(50, 'Too long'),
  password: z.string().min(8, 'Password too short'),
  confirmPassword: z.string(),
  ageConfirmed: z.boolean().refine(val => val, 'Must confirm age')
}).refine(data => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ["confirmPassword"]
});

// Server-side validation (API endpoint)
const serverRegistrationSchema = z.object({
  email: z.string()
    .email('Invalid email format')
    .refine(async (email) => {
      const exists = await checkEmailExists(email);
      return !exists;
    }, 'Email already registered'),
  sceneName: z.string()
    .min(2, 'Scene name too short')
    .max(50, 'Scene name too long')
    .refine(async (name) => {
      const exists = await checkSceneNameExists(name);
      return !exists;
    }, 'Scene name already taken'),
  password: z.string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain uppercase letter')
    .regex(/[a-z]/, 'Password must contain lowercase letter')
    .regex(/[0-9]/, 'Password must contain number')
    .regex(/[^A-Za-z0-9]/, 'Password must contain special character'),
  ageConfirmed: z.boolean().refine(val => val === true, 'Age confirmation required')
});
```

### **Asynchronous Validation**
```typescript
// Real-time username availability check
const useAsyncValidation = () => {
  const [isChecking, setIsChecking] = useState(false);
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  const checkAvailability = useDebouncedCallback(async (sceneName: string) => {
    if (sceneName.length < 2) return;
    
    setIsChecking(true);
    try {
      const available = await authAPI.checkSceneNameAvailability(sceneName);
      setIsAvailable(available);
    } catch (error) {
      setIsAvailable(null);
    } finally {
      setIsChecking(false);
    }
  }, 500);

  return { checkAvailability, isChecking, isAvailable };
};

// Usage in component
const WcrSceneNameInput = ({ value, onChange, ...props }) => {
  const { checkAvailability, isChecking, isAvailable } = useAsyncValidation();

  useEffect(() => {
    if (value) {
      checkAvailability(value);
    }
  }, [value, checkAvailability]);

  return (
    <div className="wcr-form-group">
      <label>Scene Name</label>
      <div className="input-with-status">
        <input 
          value={value}
          onChange={(e) => {
            onChange(e.target.value);
          }}
          {...props}
        />
        {isChecking && <Spinner size="sm" />}
        {isAvailable === true && <CheckIcon color="green" />}
        {isAvailable === false && <XIcon color="red" />}
      </div>
      {isAvailable === false && (
        <span className="error">Scene name is already taken</span>
      )}
    </div>
  );
};
```

## Custom WCR Components Migration

### **Base Input Component**
```typescript
interface WcrInputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  helpText?: string;
  required?: boolean;
}

const WcrInput = forwardRef<HTMLInputElement, WcrInputProps>(
  ({ label, error, helpText, required, className, ...props }, ref) => {
    const id = useId();
    
    return (
      <div className="wcr-form-group">
        <label htmlFor={id} className="wcr-label">
          {label}
          {required && <span className="required">*</span>}
        </label>
        
        <input
          ref={ref}
          id={id}
          className={cn(
            "wcr-input",
            error && "wcr-input-error",
            className
          )}
          aria-invalid={!!error}
          aria-describedby={error ? `${id}-error` : helpText ? `${id}-help` : undefined}
          {...props}
        />
        
        {helpText && !error && (
          <span id={`${id}-help`} className="wcr-help-text">
            {helpText}
          </span>
        )}
        
        {error && (
          <span id={`${id}-error`} className="wcr-error" role="alert">
            {error}
          </span>
        )}
      </div>
    );
  }
);
```

### **Password Strength Component**
```typescript
const WcrPasswordInput = ({ value, onChange, showStrengthIndicator, ...props }) => {
  const strength = useMemo(() => calculatePasswordStrength(value), [value]);
  
  return (
    <div className="wcr-form-group">
      <WcrInput
        type="password"
        value={value}
        onChange={onChange}
        {...props}
      />
      
      {showStrengthIndicator && value && (
        <div className="password-strength">
          <div className="strength-bar">
            <div 
              className={`strength-fill strength-${strength.level}`}
              style={{ width: `${strength.percentage}%` }}
            />
          </div>
          <span className="strength-text">{strength.text}</span>
          
          <ul className="strength-requirements">
            {strength.requirements.map((req, index) => (
              <li key={index} className={req.met ? 'met' : 'unmet'}>
                {req.met ? <CheckIcon /> : <XIcon />}
                {req.text}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

const calculatePasswordStrength = (password: string) => {
  const requirements = [
    { text: 'At least 8 characters', met: password.length >= 8 },
    { text: 'Contains uppercase letter', met: /[A-Z]/.test(password) },
    { text: 'Contains lowercase letter', met: /[a-z]/.test(password) },
    { text: 'Contains number', met: /[0-9]/.test(password) },
    { text: 'Contains special character', met: /[^A-Za-z0-9]/.test(password) }
  ];
  
  const metCount = requirements.filter(req => req.met).length;
  const percentage = (metCount / requirements.length) * 100;
  
  let level = 'weak';
  let text = 'Weak';
  
  if (percentage >= 80) {
    level = 'strong';
    text = 'Strong';
  } else if (percentage >= 60) {
    level = 'medium';
    text = 'Medium';
  }
  
  return { requirements, percentage, level, text };
};
```

### **Age Verification Component**
```typescript
const WcrAgeVerification = ({ value, onChange, error, ...props }) => {
  const [showWarning, setShowWarning] = useState(false);
  
  const handleChange = (checked: boolean) => {
    if (!checked && value) {
      setShowWarning(true);
      setTimeout(() => setShowWarning(false), 3000);
    }
    onChange(checked);
  };
  
  return (
    <div className="wcr-age-verification">
      <div className="age-notice">
        <Icon name="warning" />
        <span>21+ COMMUNITY • AGE VERIFICATION REQUIRED</span>
      </div>
      
      <WcrCheckbox
        checked={value}
        onChange={handleChange}
        error={error}
        {...props}
      >
        I confirm that I am 21 years of age or older and understand that 
        this community involves adult content and activities.
      </WcrCheckbox>
      
      {showWarning && (
        <div className="age-warning" role="alert">
          <Icon name="alert" />
          <span>
            You must be 21 or older to join the WitchCityRope community.
          </span>
        </div>
      )}
    </div>
  );
};
```

## Complex Form Patterns

### **Multi-Step Forms (Vetting Application)**
```typescript
const useMultiStepForm = (steps: FormStep[]) => {
  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState({});
  
  const nextStep = (stepData: any) => {
    setFormData(prev => ({ ...prev, ...stepData }));
    setCurrentStep(prev => Math.min(prev + 1, steps.length - 1));
  };
  
  const prevStep = () => {
    setCurrentStep(prev => Math.max(prev - 1, 0));
  };
  
  const goToStep = (stepIndex: number) => {
    setCurrentStep(stepIndex);
  };
  
  return {
    currentStep,
    formData,
    nextStep,
    prevStep,
    goToStep,
    isFirstStep: currentStep === 0,
    isLastStep: currentStep === steps.length - 1,
    progress: ((currentStep + 1) / steps.length) * 100
  };
};

// Vetting application implementation
const VettingApplicationForm = () => {
  const steps = [
    { title: 'Personal Information', component: PersonalInfoStep },
    { title: 'Experience & Interests', component: ExperienceStep },
    { title: 'References', component: ReferencesStep },
    { title: 'Agreement & Submission', component: AgreementStep }
  ];
  
  const {
    currentStep,
    formData,
    nextStep,
    prevStep,
    isFirstStep,
    isLastStep,
    progress
  } = useMultiStepForm(steps);
  
  const CurrentStepComponent = steps[currentStep].component;
  
  return (
    <div className="multi-step-form">
      <div className="progress-header">
        <div className="progress-bar">
          <div 
            className="progress-fill" 
            style={{ width: `${progress}%` }}
          />
        </div>
        <span className="progress-text">
          Step {currentStep + 1} of {steps.length}: {steps[currentStep].title}
        </span>
      </div>
      
      <CurrentStepComponent
        initialData={formData}
        onNext={nextStep}
        onPrev={!isFirstStep ? prevStep : undefined}
        isLastStep={isLastStep}
      />
    </div>
  );
};
```

### **Dynamic Form Fields**
```typescript
// Event registration with dynamic pricing/options
const EventRegistrationForm = ({ event }) => {
  const schema = useMemo(() => {
    const baseSchema = z.object({
      attendeeType: z.enum(['member', 'guest']),
      emergencyContact: z.string().min(1, 'Emergency contact required'),
      medicalConditions: z.string().optional(),
      experience: z.enum(['beginner', 'intermediate', 'advanced'])
    });
    
    // Add dynamic fields based on event requirements
    if (event.requiresWaiver) {
      baseSchema.waiverSigned = z.boolean().refine(val => val, 'Waiver must be signed');
    }
    
    if (event.hasWorkshopOptions) {
      baseSchema.workshopSelection = z.array(z.string()).min(1, 'Select at least one workshop');
    }
    
    return baseSchema;
  }, [event]);
  
  // Form implementation with dynamic fields
};
```

## Performance Optimization

### **Debounced Validation**
```typescript
const useDebouncedValidation = (schema: z.ZodSchema, delay = 300) => {
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  const validate = useDebouncedCallback((data: any) => {
    const result = schema.safeParse(data);
    if (!result.success) {
      const fieldErrors = result.error.flatten().fieldErrors;
      setErrors(fieldErrors);
    } else {
      setErrors({});
    }
  }, delay);
  
  return { errors, validate };
};
```

### **Memoized Validation Rules**
```typescript
const useValidationSchemas = () => {
  const schemas = useMemo(() => ({
    login: z.object({
      email: z.string().email(),
      password: z.string().min(1)
    }),
    register: z.object({
      email: z.string().email(),
      sceneName: z.string().min(2).max(50),
      password: z.string().min(8),
      confirmPassword: z.string(),
      ageConfirmed: z.boolean().refine(val => val)
    }).refine(data => data.password === data.confirmPassword, {
      message: "Passwords don't match",
      path: ["confirmPassword"]
    }),
    profile: z.object({
      sceneName: z.string().min(2).max(50),
      bio: z.string().max(500),
      emergencyContact: z.string().min(1)
    })
  }), []);
  
  return schemas;
};
```

## Accessibility Considerations

### **Screen Reader Support**
```typescript
const WcrValidationMessage = ({ children, type = 'error' }) => (
  <div
    role="alert"
    aria-live="polite"
    className={`wcr-validation-message wcr-validation-${type}`}
  >
    <Icon 
      name={type === 'error' ? 'alert-circle' : 'info'} 
      aria-hidden="true"
    />
    <span>{children}</span>
  </div>
);
```

### **Keyboard Navigation**
```typescript
const WcrFormGroup = ({ children, error }) => {
  const errorId = useId();
  
  return (
    <fieldset className="wcr-form-group">
      {children}
      {error && (
        <div
          id={errorId}
          role="alert"
          aria-live="polite"
          className="wcr-error"
        >
          {error}
        </div>
      )}
    </fieldset>
  );
};
```

## Testing Strategy

### **Validation Testing**
```typescript
describe('Registration Schema', () => {
  it('validates correct data', () => {
    const validData = {
      email: 'test@example.com',
      sceneName: 'TestUser',
      password: 'SecurePass123!',
      confirmPassword: 'SecurePass123!',
      ageConfirmed: true
    };
    
    const result = registerSchema.safeParse(validData);
    expect(result.success).toBe(true);
  });
  
  it('rejects invalid email', () => {
    const invalidData = {
      email: 'invalid-email',
      // ... other fields
    };
    
    const result = registerSchema.safeParse(invalidData);
    expect(result.success).toBe(false);
    expect(result.error?.issues[0].message).toBe('Invalid email format');
  });
});
```

### **Component Testing**
```typescript
describe('WcrPasswordInput', () => {
  it('shows strength indicator when enabled', () => {
    render(
      <WcrPasswordInput 
        value="weak"
        showStrengthIndicator
        onChange={() => {}}
      />
    );
    
    expect(screen.getByText('Weak')).toBeInTheDocument();
  });
  
  it('updates strength as password changes', async () => {
    const { rerender } = render(
      <WcrPasswordInput 
        value=""
        showStrengthIndicator
        onChange={() => {}}
      />
    );
    
    rerender(
      <WcrPasswordInput 
        value="StrongPass123!"
        showStrengthIndicator
        onChange={() => {}}
      />
    );
    
    expect(screen.getByText('Strong')).toBeInTheDocument();
  });
});
```

## Migration Recommendations

### **Phase 1: Core Validation Setup**
1. **Install Dependencies**: React Hook Form + Zod
2. **Create Base Components**: WcrInput, WcrCheckbox, WcrSelect
3. **Define Core Schemas**: Login, registration, profile forms
4. **Implement Error Handling**: Consistent error display patterns

### **Phase 2: Complex Forms**
1. **Multi-step Forms**: Vetting application, event registration
2. **Async Validation**: Username/email availability checks
3. **Dynamic Forms**: Event-specific registration options
4. **File Upload**: Profile images, vetting documents

### **Phase 3: Security & Performance**
1. **Server-Side Integration**: API validation layer
2. **Rate Limiting**: Prevent abuse of validation endpoints
3. **Debounced Validation**: Optimize performance
4. **Error Boundaries**: Graceful error handling

### **Phase 4: Advanced Features**
1. **Real-time Collaboration**: Multi-user form editing
2. **Form Analytics**: Track completion rates, common errors
3. **A/B Testing**: Form optimization experiments
4. **Accessibility Audit**: WCAG 2.1 AA compliance

## Conclusion

For the WitchCityRope migration, **React Hook Form + Zod** is the recommended approach, providing:

1. **Performance**: Minimal re-renders and excellent runtime performance
2. **Type Safety**: End-to-end type safety from validation to TypeScript
3. **Developer Experience**: Intuitive API with excellent debugging
4. **Security**: Proper client/server validation separation
5. **Flexibility**: Works with any UI component library
6. **Future-Proof**: Modern architecture following 2025 best practices

**Key Success Factors**:
- Maintain dual-layer validation (client UX + server security)
- Preserve WCR's custom validation component aesthetic
- Implement proper accessibility patterns
- Focus on performance optimization for complex forms
- Ensure seamless migration of existing validation logic

This approach ensures a robust, secure, and user-friendly form validation system that enhances the WitchCityRope user experience while maintaining the highest security standards for community safety.