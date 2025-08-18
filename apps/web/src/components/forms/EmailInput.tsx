import { forwardRef, useState, useEffect, useCallback } from 'react';
import { BaseInput, BaseInputProps } from './BaseInput';
import { useDebouncedValue } from '@mantine/hooks';

export interface EmailInputProps extends BaseInputProps {
  checkUniqueness?: boolean;
  debounceMs?: number;
  excludeCurrentUser?: boolean;
  onUniquenessCheck?: (email: string) => Promise<boolean>;
}

// Email validation regex (matches business requirements)
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const EmailInput = forwardRef<HTMLInputElement, EmailInputProps>(
  ({ 
    checkUniqueness = false,
    debounceMs = 500,
    excludeCurrentUser = false,
    onUniquenessCheck,
    value = '',
    error,
    'data-testid': testId = 'email-input',
    ...props 
  }, ref) => {
    const [isValidatingUniqueness, setIsValidatingUniqueness] = useState(false);
    const [uniquenessError, setUniquenessError] = useState<string>('');
    
    // Debounce email value for async validation
    const [debouncedEmail] = useDebouncedValue(String(value), debounceMs);

    // Check email uniqueness
    const checkEmailUniqueness = useCallback(async (email: string) => {
      if (!checkUniqueness || !onUniquenessCheck || !email || !EMAIL_REGEX.test(email)) {
        return;
      }

      setIsValidatingUniqueness(true);
      setUniquenessError('');

      try {
        const isUnique = await onUniquenessCheck(email);
        if (!isUnique) {
          setUniquenessError('An account with this email already exists');
        }
      } catch (error) {
        // Silently fail on network errors - don't block user
        console.warn('Email uniqueness check failed:', error);
      } finally {
        setIsValidatingUniqueness(false);
      }
    }, [checkUniqueness, onUniquenessCheck]);

    // Trigger uniqueness check when debounced email changes
    useEffect(() => {
      if (debouncedEmail && debouncedEmail !== value) {
        // Only check if the debounced value is different from current value
        // This prevents duplicate calls
        return;
      }
      checkEmailUniqueness(debouncedEmail);
    }, [debouncedEmail, checkEmailUniqueness, value]);

    // Combine validation errors
    const finalError = error || uniquenessError;

    return (
      <BaseInput
        ref={ref}
        type="email"
        label="Email address"
        placeholder="your.email@example.com"
        value={value}
        error={finalError}
        loading={isValidatingUniqueness}
        data-testid={testId}
        autoComplete="email"
        {...props}
      />
    );
  }
);

EmailInput.displayName = 'EmailInput';