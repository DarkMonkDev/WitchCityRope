import { useState, useEffect, useCallback, useMemo } from 'react';
import { UseFormSetError, FormState } from 'react-hook-form';
import { debounce } from '../utils/debounce';
import type { ApiErrorResponse, AsyncValidationState, FormSubmissionState } from '../types/forms';

// Hook for async validation (email uniqueness, scene name uniqueness, etc.)
export const useAsyncValidation = (
  value: string,
  validator: (value: string) => Promise<boolean | string>,
  delay: number = 500
): AsyncValidationState => {
  const [state, setState] = useState<AsyncValidationState>({
    isValidating: false,
    isValid: true,
    error: undefined
  });

  const debouncedValidator = useMemo(
    () => debounce(async (val: string) => {
      if (!val || val.length < 2) {
        setState({ isValidating: false, isValid: true });
        return;
      }

      setState(prev => ({ ...prev, isValidating: true }));
      
      try {
        const result = await validator(val);
        if (typeof result === 'boolean') {
          setState({
            isValidating: false,
            isValid: result,
            error: result ? undefined : 'This value is already taken'
          });
        } else {
          setState({
            isValidating: false,
            isValid: false,
            error: result
          });
        }
      } catch (error) {
        setState({
          isValidating: false,
          isValid: true, // Assume valid on error
          error: undefined
        });
      }
    }, delay),
    [validator, delay]
  );

  useEffect(() => {
    debouncedValidator(value);
    return () => debouncedValidator.cancel();
  }, [value, debouncedValidator]);

  return state;
};

// Hook for form submission state management
export const useFormSubmissionState = (formState: FormState<any>): FormSubmissionState => {
  return useMemo(() => ({
    isSubmitting: formState.isSubmitting,
    isValidating: formState.isValidating,
    isValid: formState.isValid && !formState.isSubmitting,
    hasErrors: Object.keys(formState.errors).length > 0,
    canSubmit: formState.isValid && !formState.isSubmitting && !formState.isValidating
  }), [formState]);
};

// Hook for mapping API errors to form fields
export const useApiErrorHandler = () => {
  const mapApiErrorsToForm = useCallback((
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
  }, []);

  return { mapApiErrorsToForm };
};

// Hook for form field accessibility
export const useFieldAccessibility = (
  id: string,
  error?: string,
  required?: boolean,
  helperText?: string
) => {
  return useMemo(() => ({
    'aria-invalid': !!error,
    'aria-required': required,
    'aria-describedby': [
      error ? `${id}-error` : '',
      helperText ? `${id}-help` : ''
    ].filter(Boolean).join(' ') || undefined
  }), [id, error, required, helperText]);
};

// Hook for form validation with custom rules
export const useFormValidationRules = () => {
  const createEmailValidation = useCallback((message?: string) => ({
    required: 'Email is required',
    pattern: {
      value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
      message: message || 'Please enter a valid email address'
    }
  }), []);

  const createPasswordValidation = useCallback((minLength: number = 8) => ({
    required: 'Password is required',
    minLength: {
      value: minLength,
      message: `Password must be at least ${minLength} characters`
    },
    pattern: {
      value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
      message: 'Password must contain uppercase, lowercase, number, and special character'
    }
  }), []);

  const createSceneNameValidation = useCallback(() => ({
    required: 'Scene name is required',
    minLength: {
      value: 2,
      message: 'Scene name must be at least 2 characters'
    },
    maxLength: {
      value: 50,
      message: 'Scene name must not exceed 50 characters'
    },
    pattern: {
      value: /^[a-zA-Z0-9_-]+$/,
      message: 'Scene name can only contain letters, numbers, hyphens, and underscores'
    }
  }), []);

  const createPhoneValidation = useCallback(() => ({
    pattern: {
      value: /^\+?1?[0-9]{10,14}$/,
      message: 'Please enter a valid phone number'
    }
  }), []);

  const createUrlValidation = useCallback(() => ({
    pattern: {
      value: /^https?:\/\/.+/,
      message: 'Please enter a valid URL starting with http:// or https://'
    }
  }), []);

  return {
    createEmailValidation,
    createPasswordValidation,
    createSceneNameValidation,
    createPhoneValidation,
    createUrlValidation
  };
};

// Hook for managing form dirty state and unsaved changes warning
export const useUnsavedChangesWarning = (isDirty: boolean, shouldWarn: boolean = true) => {
  useEffect(() => {
    if (!shouldWarn) return;

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (isDirty) {
        e.preventDefault();
        e.returnValue = '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [isDirty, shouldWarn]);

  return {
    hasUnsavedChanges: isDirty
  };
};

// Hook for form auto-save functionality
export const useFormAutoSave = <T>(
  data: T,
  onSave: (data: T) => Promise<void>,
  delay: number = 2000,
  enabled: boolean = false
) => {
  const [isSaving, setIsSaving] = useState(false);
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  const debouncedSave = useMemo(
    () => debounce(async (formData: T) => {
      if (!enabled) return;
      
      setIsSaving(true);
      try {
        await onSave(formData);
        setLastSaved(new Date());
      } catch (error) {
        console.error('Auto-save failed:', error);
      } finally {
        setIsSaving(false);
      }
    }, delay),
    [onSave, delay, enabled]
  );

  useEffect(() => {
    if (enabled && data) {
      debouncedSave(data);
    }
    return () => debouncedSave.cancel();
  }, [data, debouncedSave, enabled]);

  return {
    isSaving,
    lastSaved,
    saveNow: () => debouncedSave.flush()
  };
};