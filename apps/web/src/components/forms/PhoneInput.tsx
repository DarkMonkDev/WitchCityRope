import { forwardRef, useCallback } from 'react';
import { BaseInput, BaseInputProps } from './BaseInput';

export interface PhoneInputProps extends BaseInputProps {
  format?: 'US' | 'international';
}

// Phone number formatting utilities
const formatUSPhone = (value: string): string => {
  // Remove all non-numeric characters
  const numbers = value.replace(/\D/g, '');
  
  // Limit to 10 digits for US numbers
  const limited = numbers.substring(0, 10);
  
  // Format as (XXX) XXX-XXXX
  if (limited.length >= 6) {
    return `(${limited.substring(0, 3)}) ${limited.substring(3, 6)}-${limited.substring(6)}`;
  } else if (limited.length >= 3) {
    return `(${limited.substring(0, 3)}) ${limited.substring(3)}`;
  } else if (limited.length > 0) {
    return `(${limited}`;
  }
  
  return '';
};

const formatInternationalPhone = (value: string): string => {
  // Remove all non-numeric and non-plus characters
  const cleaned = value.replace(/[^\d+]/g, '');
  
  // Ensure it starts with + if it has content
  if (cleaned.length > 0 && !cleaned.startsWith('+')) {
    return '+' + cleaned;
  }
  
  return cleaned;
};

// Validation regex for phone numbers (from business requirements)
const US_PHONE_REGEX = /^\([0-9]{3}\) [0-9]{3}-[0-9]{4}$/;
const INTERNATIONAL_PHONE_REGEX = /^\+?1?[0-9]{10,14}$/;

export const PhoneInput = forwardRef<HTMLInputElement, PhoneInputProps>(
  ({ 
    format = 'US',
    value = '',
    onChange,
    'data-testid': testId = 'phone-input',
    ...props 
  }, ref) => {
    
    // Handle input formatting
    const handleChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
      const inputValue = event.target.value;
      let formattedValue: string;
      
      if (format === 'US') {
        formattedValue = formatUSPhone(inputValue);
      } else {
        formattedValue = formatInternationalPhone(inputValue);
      }
      
      // Create new event with formatted value
      const formattedEvent = {
        ...event,
        target: {
          ...event.target,
          value: formattedValue
        }
      };
      
      onChange?.(formattedEvent);
    }, [format, onChange]);

    // Determine placeholder based on format
    const placeholder = format === 'US' ? '(555) 123-4567' : '+1 555 123 4567';
    
    return (
      <BaseInput
        ref={ref}
        type="tel"
        label="Phone number"
        placeholder={placeholder}
        value={value}
        onChange={handleChange}
        data-testid={testId}
        autoComplete="tel"
        {...props}
      />
    );
  }
);

PhoneInput.displayName = 'PhoneInput';

// Export validation utilities for use in Zod schemas
export const validateUSPhone = (phone: string): boolean => {
  return US_PHONE_REGEX.test(phone);
};

export const validateInternationalPhone = (phone: string): boolean => {
  return INTERNATIONAL_PHONE_REGEX.test(phone.replace(/\D/g, ''));
};