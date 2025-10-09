// Common form types and interfaces for WitchCityRope

export interface ValidationError {
  field: string;
  message: string;
  code?: string;
}

export interface FormError {
  message: string;
  validationErrors?: ValidationError[];
  code?: string;
}

export interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
  code?: string;
  timestamp: string;
}

export interface ApiSuccessResponse<T = any> {
  success: true;
  data: T;
  message?: string;
  timestamp: string;
}

export type ApiResponse<T = any> = ApiSuccessResponse<T> | ApiErrorResponse;

// Form submission states
export interface FormSubmissionState {
  isSubmitting: boolean;
  isValidating: boolean;
  isValid: boolean;
  hasErrors: boolean;
  canSubmit: boolean;
}

// Common form data interfaces
export interface LoginFormData {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterFormData {
  email: string;
  sceneName: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  agreeToTerms: boolean;
  agreeToNewsletter?: boolean;
}

export interface UserProfileFormData {
  sceneName: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
  location?: string;
  website?: string;
  experienceLevel?: 'beginner' | 'intermediate' | 'advanced' | 'expert';
  interests?: string[];
}

export interface EventRegistrationFormData {
  eventId: string;
  attendeeType: 'member' | 'guest';
  medicalInfo?: string;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  waiverSigned: boolean;
  additionalNotes?: string;
}

export interface ContactFormData {
  name: string;
  email: string;
  subject: string;
  message: string;
  type: 'general' | 'support' | 'membership' | 'events' | 'feedback';
}

// Form field validation patterns
export interface FieldValidationRule {
  required?: boolean;
  minLength?: number;
  maxLength?: number;
  pattern?: RegExp;
  customValidator?: (value: any) => boolean | string;
  asyncValidator?: (value: any) => Promise<boolean | string>;
}

// Async validation states
export interface AsyncValidationState {
  isValidating: boolean;
  isValid: boolean;
  error?: string;
}

// Form configuration options
export interface FormConfig {
  validateOnChange?: boolean;
  validateOnBlur?: boolean;
  validateOnMount?: boolean;
  reValidateMode?: 'onChange' | 'onBlur' | 'onSubmit';
  shouldFocusError?: boolean;
  criteriaMode?: 'firstError' | 'all';
}

// Common select options
export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
  description?: string;
}

// Pre-defined option sets
export const membershipTypeOptions: SelectOption[] = [
  { value: 'guest', label: 'Guest', description: 'One-time event attendance' },
  { value: 'member', label: 'Member', description: 'Community member' },
  { value: 'vetted', label: 'Vetted Member', description: 'Verified community member' },
  { value: 'instructor', label: 'Instructor', description: 'Qualified teacher' },
  { value: 'admin', label: 'Administrator', description: 'Community administrator' }
];

export const experienceLevelOptions: SelectOption[] = [
  { value: 'beginner', label: 'Beginner', description: 'New to rope bondage' },
  { value: 'intermediate', label: 'Intermediate', description: 'Some experience' },
  { value: 'advanced', label: 'Advanced', description: 'Experienced practitioner' },
  { value: 'expert', label: 'Expert', description: 'Teaching level expertise' }
];

export const contactTypeOptions: SelectOption[] = [
  { value: 'general', label: 'General Inquiry' },
  { value: 'support', label: 'Technical Support' },
  { value: 'membership', label: 'Membership Questions' },
  { value: 'events', label: 'Event Information' },
  { value: 'feedback', label: 'Feedback/Suggestions' }
];

// Form validation messages
export const validationMessages = {
  required: (field: string) => `${field} is required`,
  email: 'Please enter a valid email address',
  password: {
    minLength: 'Password must be at least 8 characters',
    complexity: 'Password must contain uppercase, lowercase, number, and special character',
    match: 'Passwords do not match'
  },
  sceneName: {
    minLength: 'Scene name must be at least 2 characters',
    maxLength: 'Scene name must not exceed 50 characters',
    format: 'Scene name can only contain letters, numbers, hyphens, and underscores',
    unique: 'This scene name is already taken'
  },
  phone: 'Please enter a valid phone number',
  url: 'Please enter a valid URL',
  terms: 'You must agree to the terms and conditions'
};

// Form field configurations
export const fieldConfigs = {
  email: {
    type: 'email' as const,
    autoComplete: 'email',
    placeholder: 'Enter your email address'
  },
  password: {
    type: 'password' as const,
    autoComplete: 'current-password',
    placeholder: 'Enter your password'
  },
  newPassword: {
    type: 'password' as const,
    autoComplete: 'new-password',
    placeholder: 'Create a password'
  },
  sceneName: {
    type: 'text' as const,
    autoComplete: 'username',
    placeholder: 'Choose a scene name'
  },
  firstName: {
    type: 'text' as const,
    autoComplete: 'given-name',
    placeholder: 'First name'
  },
  lastName: {
    type: 'text' as const,
    autoComplete: 'family-name',
    placeholder: 'Last name'
  },
  phone: {
    type: 'tel' as const,
    autoComplete: 'tel',
    placeholder: '(555) 123-4567'
  }
} as const;