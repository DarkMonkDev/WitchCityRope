import React, { useState, useRef } from 'react';
import {
  Container,
  Title,
  Paper,
  Stack,
  Group,
  Text,
  Button,
  Grid,
  Badge,
  Accordion,
  Code,
  Alert,
  Box
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconInfoCircle, IconCheck, IconX } from '@tabler/icons-react';
import {
  EmergencyContactGroup
} from '../components/forms';
import { mockSubmitForm } from '../utils/mockApi';

// Custom Floating Label Input Component with Underline Effect
interface FloatingLabelInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: React.ReactNode;
  required?: boolean;
  type?: string;
  placeholder?: string;
  description?: string;
  disabled?: boolean;
  'data-testid'?: string;
}

// Custom Floating Label Textarea Component with Underline Effect
interface FloatingLabelTextareaProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: React.ReactNode;
  required?: boolean;
  placeholder?: string;
  description?: string;
  disabled?: boolean;
  rows?: number;
  maxLength?: number;
  'data-testid'?: string;
}

// Custom Floating Label Select Component (no underline)
interface FloatingLabelSelectProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: React.ReactNode;
  required?: boolean;
  placeholder?: string;
  description?: string;
  disabled?: boolean;
  data: Array<{ value: string; label: string }>;
  'data-testid'?: string;
}

// Helper text mapping for each field
const getHelperText = (label: string): string => {
  switch (label) {
    case 'Email Address':
    case 'Email':
      return "We'll never share your email";
    case 'Password':
      return 'Minimum 8 characters with special character';
    case 'Scene Name':
      return 'Your unique identifier in the community';
    case 'Phone Number':
    case 'Phone':
      return 'For emergency contact only';
    case 'Basic Input':
      return 'This is a basic text input with validation';
    case 'Floating Textarea':
      return 'This is a floating label textarea with underline effect';
    case 'Floating Select':
      return 'This is a floating label select without underline';
    case 'Emergency Contact Name':
      return 'Full name of your emergency contact';
    case 'Emergency Contact Phone':
      return 'Best phone number to reach your emergency contact';
    case 'Relationship to Contact':
      return 'How this person is related to you';
    default:
      return '';
  }
};

const FloatingLabelInput: React.FC<FloatingLabelInputProps> = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder = ' ', // Space for floating effect
  description,
  disabled,
  'data-testid': dataTestId
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  
  const isActive = isFocused || value.length > 0;
  
  return (
    <Box style={{ position: 'relative', marginBottom: '24px' }}>
      {/* Input field container with underline */}
      <Box style={{ position: 'relative' }}>
        <input
          ref={inputRef}
          type={type}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          placeholder={placeholder}
          disabled={disabled}
          data-testid={dataTestId}
          style={{
            width: '100%',
            backgroundColor: 'var(--mantine-color-dark-7) !important',
            borderColor: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(212, 165, 165, 0.3)',
            color: '#f8f4e6 !important',
            fontSize: '16px',
            padding: '16px 12px 8px 12px',
            height: '56px',
            borderRadius: '8px',
            border: '1px solid',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            outline: 'none',
            opacity: disabled ? 0.6 : 1,
            cursor: disabled ? 'not-allowed' : 'text',
            // Force dark background for all states
            WebkitBoxShadow: 'inset 0 0 0 1000px var(--mantine-color-dark-7)',
            // Ensure consistent underline positioning across all input types
            margin: '0',
            boxSizing: 'border-box',
            display: 'block'
          }}
        />
        
        {/* Underline animation on focus - positioned just below the bottom border */}
        <Box
          style={{
            position: 'absolute',
            bottom: '-2px',
            left: '16px',
            right: '16px',
            height: '2px',
            background: error ? '#d63031' : '#9b4a75',
            opacity: isFocused ? 1 : 0,
            transform: isFocused ? 'scaleX(1)' : 'scaleX(0)',
            transformOrigin: 'center',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            borderRadius: '1px',
            // Create tapering effect - last 15% on each side tapers from 2px to 1px
            clipPath: 'polygon(0% 25%, 15% 0%, 85% 0%, 100% 25%, 100% 75%, 85% 100%, 15% 100%, 0% 75%)',
            WebkitClipPath: 'polygon(0% 25%, 15% 0%, 85% 0%, 100% 25%, 100% 75%, 85% 100%, 15% 100%, 0% 75%)',
            // Ensure consistent positioning across all input types
            marginTop: '0px',
            marginBottom: '0px'
          }}
        />
        
        <Text
          component="label"
          onClick={() => inputRef.current?.focus()}
          style={{
            position: 'absolute',
            left: '12px',
            top: isActive ? '4px' : '18px',
            fontSize: isActive ? '12px' : '16px',
            color: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(248, 244, 230, 0.7)',
            pointerEvents: 'none',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            transformOrigin: 'left center',
            background: isActive ? 'var(--mantine-color-dark-7)' : 'transparent',
            padding: isActive ? '0 4px' : '0',
            zIndex: 1,
            fontWeight: isActive ? 500 : 400
          }}
        >
          {label}
          {required && <span style={{ color: '#d63031', marginLeft: '2px' }}>*</span>}
        </Text>
      </Box>
      
      {error && (
        <Text size="sm" c="red" mt="xs" style={{
          animation: 'shake 0.5s ease-in-out'
        }}>
          {error}
        </Text>
      )}
      
      {/* Helper text */}
      {!error && (description || getHelperText(label)) && (
        <Text size="sm" c="dimmed" mt="xs" style={{
          marginLeft: '12px',
          opacity: 0.85,
          fontSize: '1.1rem',
          lineHeight: '1.6'
        }}>
          {description || getHelperText(label)}
        </Text>
      )}

      <style>{`
        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          25% { transform: translateX(-4px); }
          75% { transform: translateX(4px); }
        }
        
        /* Force dark backgrounds on all input states and ensure consistent margins */
        input[type="text"], input[type="email"], input[type="password"], input[type="tel"],
        textarea, select {
          background-color: var(--mantine-color-dark-7) !important;
          color: #f8f4e6 !important;
          margin: 0 !important;
          box-sizing: border-box !important;
        }
        
        /* Override autofill styles */
        input:-webkit-autofill,
        input:-webkit-autofill:hover,
        input:-webkit-autofill:focus,
        input:-webkit-autofill:active {
          -webkit-box-shadow: inset 0 0 0 1000px var(--mantine-color-dark-7) !important;
          -webkit-text-fill-color: #f8f4e6 !important;
          background-color: var(--mantine-color-dark-7) !important;
          color: #f8f4e6 !important;
        }
        
        /* Enhanced dropdown styling for dark theme - comprehensive visual styling */
        select {
          border-radius: 8px !important;
        }
        
        select:focus {
          background-color: var(--mantine-color-dark-7) !important;
          border-color: #9b4a75 !important;
          box-shadow: 0 0 0 2px rgba(155, 74, 117, 0.2);
        }
        
        select:hover {
          border-color: rgba(155, 74, 117, 0.5) !important;
        }
        
        /* Comprehensive dropdown menu styling */
        select option {
          background-color: var(--mantine-color-dark-7) !important;
          color: #f8f4e6 !important;
          padding: 8px 12px !important;
          border-radius: 4px !important;
          margin: 2px !important;
        }
        
        /* Option hover states for browsers that support it */
        select option:hover {
          background-color: rgba(155, 74, 117, 0.3) !important;
          color: #f8f4e6 !important;
        }
        
        select option:focus,
        select option:checked,
        select option:selected {
          background-color: #9b4a75 !important;
          color: #f8f4e6 !important;
        }
        
        /* Custom scrollbar for dropdown */
        select::-webkit-scrollbar {
          width: 8px;
        }
        
        select::-webkit-scrollbar-track {
          background: var(--mantine-color-dark-8);
          border-radius: 4px;
        }
        
        select::-webkit-scrollbar-thumb {
          background: #9b4a75;
          border-radius: 4px;
        }
        
        select::-webkit-scrollbar-thumb:hover {
          background: #b47171;
        }
        
        /* Dark theme focus states */
        select:focus-visible {
          outline: 2px solid rgba(155, 74, 117, 0.4);
          outline-offset: 2px;
        }
        
        /* Firefox-specific dropdown styling */
        @-moz-document url-prefix() {
          select option {
            background-color: var(--mantine-color-dark-7) !important;
            color: #f8f4e6 !important;
          }
          
          select option:hover {
            background-color: rgba(155, 74, 117, 0.3) !important;
          }
          
          select option:checked {
            background-color: #9b4a75 !important;
            color: #f8f4e6 !important;
          }
        }
        
        /* Safari-specific improvements */
        @media screen and (-webkit-min-device-pixel-ratio:0) {
          select {
            background-color: var(--mantine-color-dark-7) !important;
          }
          
          select option {
            background: var(--mantine-color-dark-7) !important;
            color: #f8f4e6 !important;
          }
        }
      `}</style>
    </Box>
  );
};

const FloatingLabelTextarea: React.FC<FloatingLabelTextareaProps> = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  placeholder = ' ', // Space for floating effect
  description,
  disabled,
  rows = 4,
  maxLength,
  'data-testid': dataTestId
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  
  const isActive = isFocused || value.length > 0;
  
  return (
    <Box style={{ position: 'relative', marginBottom: '24px' }}>
      {/* Textarea field container with underline */}
      <Box style={{ position: 'relative' }}>
        <textarea
          ref={textareaRef}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          placeholder={placeholder}
          disabled={disabled}
          rows={rows}
          maxLength={maxLength}
          data-testid={dataTestId}
          style={{
            width: '100%',
            backgroundColor: 'var(--mantine-color-dark-7) !important',
            borderColor: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(212, 165, 165, 0.3)',
            color: '#f8f4e6 !important',
            fontSize: '16px',
            padding: '16px 12px 8px 12px',
            minHeight: `${rows * 24 + 32}px`,
            borderRadius: '8px',
            border: '1px solid',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            outline: 'none',
            opacity: disabled ? 0.6 : 1,
            cursor: disabled ? 'not-allowed' : 'text',
            resize: 'vertical',
            fontFamily: 'inherit',
            // Fix textarea browser default margins that cause underline gap
            margin: '0',
            boxSizing: 'border-box',
            display: 'block'
          }}
        />
        
        {/* Underline animation on focus - positioned to overlap the bottom border of textarea */}
        <Box
          style={{
            position: 'absolute',
            bottom: '0px',
            left: '16px',
            right: '16px',
            height: '2px',
            background: error ? '#d63031' : '#9b4a75',
            opacity: isFocused ? 1 : 0,
            transform: isFocused ? 'scaleX(1)' : 'scaleX(0)',
            transformOrigin: 'center',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            borderRadius: '1px',
            // Create tapering effect - last 15% on each side tapers from 2px to 1px
            clipPath: 'polygon(0% 25%, 15% 0%, 85% 0%, 100% 25%, 100% 75%, 85% 100%, 15% 100%, 0% 75%)',
            WebkitClipPath: 'polygon(0% 25%, 15% 0%, 85% 0%, 100% 25%, 100% 75%, 85% 100%, 15% 100%, 0% 75%)',
            // Ensure tight positioning at bottom of textarea
            marginTop: '0px',
            marginBottom: '0px'
          }}
        />
        
        <Text
          component="label"
          onClick={() => textareaRef.current?.focus()}
          style={{
            position: 'absolute',
            left: '12px',
            top: isActive ? '4px' : '18px',
            fontSize: isActive ? '12px' : '16px',
            color: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(248, 244, 230, 0.7)',
            pointerEvents: 'none',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            transformOrigin: 'left center',
            background: isActive ? 'var(--mantine-color-dark-7)' : 'transparent',
            padding: isActive ? '0 4px' : '0',
            zIndex: 1,
            fontWeight: isActive ? 500 : 400
          }}
        >
          {label}
          {required && <span style={{ color: '#d63031', marginLeft: '2px' }}>*</span>}
        </Text>
      </Box>
      
      {error && (
        <Text size="sm" c="red" mt="xs" style={{
          animation: 'shake 0.5s ease-in-out'
        }}>
          {error}
        </Text>
      )}
      
      {/* Helper text */}
      {!error && (description || getHelperText(label)) && (
        <Text size="sm" c="dimmed" mt="xs" style={{
          marginLeft: '12px',
          opacity: 0.85,
          fontSize: '1.1rem',
          lineHeight: '1.6'
        }}>
          {description || getHelperText(label)}
        </Text>
      )}
    </Box>
  );
};

const FloatingLabelSelect: React.FC<FloatingLabelSelectProps> = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  placeholder = ' ', // Space for floating effect
  description,
  disabled,
  data,
  'data-testid': dataTestId
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const selectRef = useRef<HTMLSelectElement>(null);
  
  const isActive = isFocused || value.length > 0 || isOpen;
  
  return (
    <Box style={{ position: 'relative', marginBottom: '24px' }}>
      {/* Select field container - no underline */}
      <Box style={{ position: 'relative' }}>
        <select
          ref={selectRef}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => { setIsFocused(true); setIsOpen(true); }}
          onBlur={() => { setIsFocused(false); setIsOpen(false); }}
          disabled={disabled}
          data-testid={dataTestId}
          style={{
            width: '100%',
            backgroundColor: 'var(--mantine-color-dark-7) !important',
            borderColor: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(212, 165, 165, 0.3)',
            color: '#f8f4e6 !important',
            fontSize: '16px',
            padding: '16px 12px 8px 12px',
            height: '56px',
            borderRadius: '8px',
            border: '1px solid',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            outline: 'none',
            opacity: disabled ? 0.6 : 1,
            cursor: disabled ? 'not-allowed' : 'pointer',
            appearance: 'none',
            WebkitAppearance: 'none',
            MozAppearance: 'none',
            // Custom dropdown arrow
            backgroundImage: `url("data:image/svg+xml;charset=UTF-8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23f8f4e6' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><polyline points='6,9 12,15 18,9'></polyline></svg>")`,
            backgroundRepeat: 'no-repeat',
            backgroundPosition: 'right 12px center',
            backgroundSize: '16px',
            paddingRight: '40px',
            // Ensure consistent positioning with inputs and textarea
            margin: '0',
            boxSizing: 'border-box',
            display: 'block'
          }}
        >
          {!value && <option value="" disabled hidden>{placeholder}</option>}
          {data.map((option) => (
            <option
              key={option.value}
              value={option.value}
              style={{
                backgroundColor: 'var(--mantine-color-dark-7)',
                color: '#f8f4e6'
              }}
            >
              {option.label}
            </option>
          ))}
        </select>
        
        <Text
          component="label"
          onClick={() => selectRef.current?.focus()}
          style={{
            position: 'absolute',
            left: '12px',
            top: isActive ? '4px' : '18px',
            fontSize: isActive ? '12px' : '16px',
            color: error ? '#d63031' : isActive ? '#9b4a75' : 'rgba(248, 244, 230, 0.7)',
            pointerEvents: 'none',
            transition: 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            transformOrigin: 'left center',
            background: isActive ? 'var(--mantine-color-dark-7)' : 'transparent',
            padding: isActive ? '0 4px' : '0',
            zIndex: 1,
            fontWeight: isActive ? 500 : 400
          }}
        >
          {label}
          {required && <span style={{ color: '#d63031', marginLeft: '2px' }}>*</span>}
        </Text>
      </Box>
      
      {error && (
        <Text size="sm" c="red" mt="xs" style={{
          animation: 'shake 0.5s ease-in-out'
        }}>
          {error}
        </Text>
      )}
      
      {/* Helper text */}
      {!error && (description || getHelperText(label)) && (
        <Text size="sm" c="dimmed" mt="xs" style={{
          marginLeft: '12px',
          opacity: 0.85,
          fontSize: '1.1rem',
          lineHeight: '1.6'
        }}>
          {description || getHelperText(label)}
        </Text>
      )}
    </Box>
  );
};

interface FormData {
  basicInput: string;
  email: string;
  sceneName: string;
  password: string;
  phone: string;
  textarea: string;
  select: string;
  emergencyContact: {
    name: string;
    phone: string;
    relationship: string;
  };
}

export const FormComponentsTest: React.FC = () => {
  const [formStates, setFormStates] = useState({
    showErrors: false,
    isSubmitting: false,
    disableAll: false
  });

  const form = useForm<FormData>({
    initialValues: {
      basicInput: '',
      email: '',
      sceneName: '',
      password: '',
      phone: '',
      textarea: '',
      select: '',
      emergencyContact: {
        name: '',
        phone: '',
        relationship: ''
      }
    },
    validate: {
      basicInput: (value) => value.length < 2 ? 'Basic input must be at least 2 characters' : null,
      email: (value) => {
        if (!value) return 'Email is required';
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return !emailRegex.test(value) ? 'Please enter a valid email address' : null;
      },
      sceneName: (value) => {
        if (!value) return 'Scene name is required';
        if (value.length < 2) return 'Scene name must be at least 2 characters';
        if (value.length > 50) return 'Scene name must not exceed 50 characters';
        const sceneNameRegex = /^[a-zA-Z0-9_-]+$/;
        return !sceneNameRegex.test(value) ? 'Scene name can only contain letters, numbers, hyphens, and underscores' : null;
      },
      password: (value) => {
        if (!value) return 'Password is required';
        if (value.length < 8) return 'Password must be at least 8 characters';
        const hasUpper = /[A-Z]/.test(value);
        const hasLower = /[a-z]/.test(value);
        const hasNumber = /\d/.test(value);
        const hasSpecial = /[@$!%*?&]/.test(value);
        if (!hasUpper) return 'Password must contain uppercase letter';
        if (!hasLower) return 'Password must contain lowercase letter';
        if (!hasNumber) return 'Password must contain number';
        if (!hasSpecial) return 'Password must contain special character';
        return null;
      },
      select: (value) => !value ? 'Please select an option' : null,
      textarea: (value) => value.length > 500 ? 'Textarea must not exceed 500 characters' : null,
      'emergencyContact.name': (value) => !value ? 'Emergency contact name is required' : null,
      'emergencyContact.phone': (value) => {
        if (!value) return 'Emergency contact phone is required';
        const phoneRegex = /^\(\d{3}\)\s\d{3}-\d{4}$/;
        return !phoneRegex.test(value) ? 'Please enter a valid phone number' : null;
      },
      'emergencyContact.relationship': (value) => !value ? 'Relationship is required' : null
    }
  });

  const handleSubmit = async (values: FormData) => {
    setFormStates(prev => ({ ...prev, isSubmitting: true }));
    
    try {
      await mockSubmitForm(values);
      notifications.show({
        title: 'Success!',
        message: 'Form submitted successfully',
        color: 'green',
        icon: <IconCheck size={16} />
      });
      form.reset();
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: 'Form submission failed',
        color: 'red',
        icon: <IconX size={16} />
      });
    } finally {
      setFormStates(prev => ({ ...prev, isSubmitting: false }));
    }
  };

  const toggleErrors = () => {
    setFormStates(prev => ({ ...prev, showErrors: !prev.showErrors }));
    if (!formStates.showErrors) {
      form.validate();
    } else {
      form.clearErrors();
    }
  };

  const toggleDisabled = () => {
    setFormStates(prev => ({ ...prev, disableAll: !prev.disableAll }));
  };

  const fillTestData = () => {
    form.setValues({
      basicInput: 'Test Input Value',
      email: 'test@example.com',
      sceneName: 'TestUser123',
      password: 'SecurePass123!',
      phone: '(555) 123-4567',
      textarea: 'This is a test textarea with some sample content.',
      select: 'option2',
      emergencyContact: {
        name: 'Jane Doe',
        phone: '(555) 987-6543',
        relationship: 'Friend'
      }
    });
  };

  const fillConflictData = () => {
    form.setValues({
      basicInput: 'Conflict Test',
      email: 'taken@example.com', // This will trigger uniqueness error
      sceneName: 'admin', // This will trigger uniqueness error
      password: 'weak', // This will trigger strength errors
      phone: '555123',
      textarea: '',
      select: '',
      emergencyContact: {
        name: '',
        phone: '',
        relationship: ''
      }
    });
  };

  const selectOptions = [
    { value: 'option1', label: 'Option 1' },
    { value: 'option2', label: 'Option 2' },
    { value: 'option3', label: 'Option 3' }
  ];

  const relationshipOptions = [
    { value: 'spouse', label: 'Spouse/Partner' },
    { value: 'parent', label: 'Parent' },
    { value: 'sibling', label: 'Sibling' },
    { value: 'friend', label: 'Friend' },
    { value: 'other', label: 'Other' }
  ];

  return (
    <Box style={{
      minHeight: '100vh',
      background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
      paddingTop: '40px',
      paddingBottom: '40px'
    }}>
      <Container size="lg">
        <Stack gap="xl">
          {/* Header */}
          <Box style={{ textAlign: 'center' }}>
            <Text
              size="xl"
              fw={700}
              mb="md"
              style={{
                background: 'linear-gradient(135deg, #d4a5a5 0%, #c48b8b 50%, #b47171 100%)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                fontSize: '42px',
                fontFamily: 'Bodoni Moda, serif',
                letterSpacing: '1px'
              }}
            >
              Form Components Test
            </Text>
            <Text size="lg" c="dimmed" mb="xs">
              Floating Label Design with Dark Theme
            </Text>
            <Text c="dimmed" maw={800} mx="auto">
              Comprehensive testing page for all WitchCityRope form components with floating labels, underline effects, and dark theme styling.
            </Text>
          </Box>

          {/* Test Controls */}
          <Paper p="md" style={{
            background: 'rgba(44, 44, 44, 0.8)',
            border: '1px solid rgba(212, 165, 165, 0.1)',
            borderRadius: '12px',
            backdropFilter: 'blur(10px)'
          }}>
            <Title order={2} size="h3" mb="md" c="white">Test Controls</Title>
            <Group>
              <Button 
                onClick={fillTestData} 
                variant="light"
                data-testid="fill-test-data"
              >
                Fill Test Data
              </Button>
              <Button 
                onClick={fillConflictData} 
                variant="light" 
                color="orange"
                data-testid="fill-conflict-data"
              >
                Fill Conflict Data
              </Button>
              <Button 
                onClick={toggleErrors} 
                variant="light" 
                color={formStates.showErrors ? 'red' : 'blue'}
                data-testid="toggle-errors"
              >
                {formStates.showErrors ? 'Hide' : 'Show'} Validation Errors
              </Button>
              <Button 
                onClick={toggleDisabled} 
                variant="light" 
                color={formStates.disableAll ? 'red' : 'gray'}
                data-testid="toggle-disabled"
              >
                {formStates.disableAll ? 'Enable' : 'Disable'} All Fields
              </Button>
            </Group>
          </Paper>

          {/* Instructions */}
          <Alert icon={<IconInfoCircle size={16} />} title="Testing Instructions" color="blue" style={{
            background: 'rgba(59, 130, 246, 0.1)',
            border: '1px solid rgba(59, 130, 246, 0.3)'
          }}>
            <Stack gap="xs">
              <Text size="sm">• Use "Fill Test Data" to populate all fields with valid data</Text>
              <Text size="sm">• Use "Fill Conflict Data" to test validation errors and async uniqueness checks</Text>
              <Text size="sm">• Click in and out of fields to see floating label animations</Text>
              <Text size="sm">• Focus on fields to see elegant underline animation</Text>
              <Text size="sm">• All fields use floating labels with clean focus indicators</Text>
              <Text size="sm">• Dark theme with sophisticated color transitions</Text>
            </Stack>
          </Alert>

          {/* Main Form */}
          <Box style={{
            background: 'linear-gradient(135deg, rgba(44, 44, 44, 0.8) 0%, rgba(26, 26, 26, 0.9) 100%)',
            padding: '40px',
            borderRadius: '12px',
            border: '1px solid rgba(212, 165, 165, 0.1)',
            backdropFilter: 'blur(10px)',
            boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)'
          }}>
            <form onSubmit={form.onSubmit(handleSubmit)}>
              <Stack gap="xl">
                
                {/* Basic Components */}
                <Box>
                  <Title order={2} size="h3" mb="md" c="white">Basic Form Components</Title>
                  <Grid>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelInput
                        label="Basic Input"
                        value={form.values.basicInput}
                        onChange={(value) => form.setFieldValue('basicInput', value)}
                        error={form.errors.basicInput}
                        required
                        disabled={formStates.disableAll}
                        data-testid="basic-input"
                      />
                    </Grid.Col>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelSelect
                        label="Floating Select"
                        value={form.values.select}
                        onChange={(value) => form.setFieldValue('select', value)}
                        error={form.errors.select}
                        data={selectOptions}
                        required
                        disabled={formStates.disableAll}
                        data-testid="floating-select"
                      />
                    </Grid.Col>
                    <Grid.Col span={12}>
                      <FloatingLabelTextarea
                        label="Floating Textarea"
                        value={form.values.textarea}
                        onChange={(value) => form.setFieldValue('textarea', value)}
                        error={form.errors.textarea}
                        rows={4}
                        maxLength={500}
                        disabled={formStates.disableAll}
                        data-testid="floating-textarea"
                      />
                    </Grid.Col>
                  </Grid>
                </Box>

                {/* Specialized Components */}
                <Box>
                  <Title order={2} size="h3" mb="md" c="white">Floating Label Inputs</Title>
                  <Grid>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelInput
                        label="Email Address"
                        type="email"
                        value={form.values.email}
                        onChange={(value) => form.setFieldValue('email', value)}
                        error={form.errors.email}
                        required
                        description="Checks email uniqueness (try 'taken@example.com')"
                        disabled={formStates.disableAll}
                        data-testid="email-input"
                      />
                    </Grid.Col>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelInput
                        label="Scene Name"
                        value={form.values.sceneName}
                        onChange={(value) => form.setFieldValue('sceneName', value)}
                        error={form.errors.sceneName}
                        required
                        description="Checks scene name uniqueness (try 'admin')"
                        disabled={formStates.disableAll}
                        data-testid="scene-name-input"
                      />
                    </Grid.Col>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelInput
                        label="Password"
                        type="password"
                        value={form.values.password}
                        onChange={(value) => form.setFieldValue('password', value)}
                        error={form.errors.password}
                        required
                        description="Shows real-time strength meter and requirements"
                        disabled={formStates.disableAll}
                        data-testid="password-input"
                      />
                    </Grid.Col>
                    <Grid.Col span={{ base: 12, md: 6 }}>
                      <FloatingLabelInput
                        label="Phone Number"
                        type="tel"
                        value={form.values.phone}
                        onChange={(value) => form.setFieldValue('phone', value)}
                        error={form.errors.phone}
                        description="Auto-formats US phone numbers as you type"
                        disabled={formStates.disableAll}
                        data-testid="phone-input"
                      />
                    </Grid.Col>
                  </Grid>
                </Box>

                {/* Emergency Contact Group */}
                <Box>
                  <Title order={2} size="h3" mb="md" c="white">Emergency Contact Group</Title>
                  
                  {/* Emergency Contact with Floating Label Components */}
                  <Box style={{
                    background: 'rgba(44, 44, 44, 0.6)',
                    padding: '24px',
                    borderRadius: '12px',
                    border: '1px solid rgba(212, 165, 165, 0.2)'
                  }}>
                    <Text fw={600} size="lg" mb="lg" c="white">Emergency Contact Information</Text>
                    
                    <Stack gap="lg">
                      <FloatingLabelInput
                        label="Emergency Contact Name"
                        value={form.values.emergencyContact.name}
                        onChange={(value) => form.setFieldValue('emergencyContact.name', value)}
                        error={form.errors['emergencyContact.name']}
                        required
                        description="Full name of your emergency contact"
                        disabled={formStates.disableAll}
                        data-testid="emergency-contact-name"
                      />
                      
                      <FloatingLabelInput
                        label="Emergency Contact Phone"
                        type="tel"
                        value={form.values.emergencyContact.phone}
                        onChange={(value) => form.setFieldValue('emergencyContact.phone', value)}
                        error={form.errors['emergencyContact.phone']}
                        required
                        description="Best phone number to reach your emergency contact"
                        disabled={formStates.disableAll}
                        data-testid="emergency-contact-phone"
                      />
                      
                      <FloatingLabelSelect
                        label="Relationship to Contact"
                        value={form.values.emergencyContact.relationship}
                        onChange={(value) => form.setFieldValue('emergencyContact.relationship', value)}
                        error={form.errors['emergencyContact.relationship']}
                        data={relationshipOptions}
                        required
                        description="How this person is related to you"
                        disabled={formStates.disableAll}
                        data-testid="emergency-contact-relationship"
                      />
                    </Stack>
                  </Box>
                </Box>

                {/* Submit Button */}
                <Group justify="center" mt="xl">
                  <Button
                    type="submit"
                    size="lg"
                    variant="gradient"
                    gradient={{ from: 'grape.6', to: 'grape.7', deg: 45 }}
                    loading={formStates.isSubmitting}
                    disabled={formStates.disableAll}
                    data-testid="submit-button"
                    style={{
                      height: '56px',
                      fontWeight: 600,
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px'
                    }}
                  >
                    {formStates.isSubmitting ? 'Submitting...' : 'Submit Form'}
                  </Button>
                </Group>

              </Stack>
            </form>
          </Box>

          {/* Form State Display */}
          <Paper p="md" style={{
            background: 'rgba(44, 44, 44, 0.8)',
            border: '1px solid rgba(212, 165, 165, 0.1)',
            borderRadius: '12px',
            backdropFilter: 'blur(10px)'
          }}>
            <Title order={2} size="h3" mb="md" c="white">Form State</Title>
            <Grid>
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Stack gap="xs">
                  <Group>
                    <Badge color={form.isValid() ? 'green' : 'red'}>
                      {form.isValid() ? 'Valid' : 'Invalid'}
                    </Badge>
                    <Badge color={form.isDirty() ? 'blue' : 'gray'}>
                      {form.isDirty() ? 'Dirty' : 'Pristine'}
                    </Badge>
                    <Badge color={formStates.isSubmitting ? 'yellow' : 'gray'}>
                      {formStates.isSubmitting ? 'Submitting' : 'Ready'}
                    </Badge>
                  </Group>
                </Stack>
              </Grid.Col>
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Accordion variant="contained">
                  <Accordion.Item value="form-values">
                    <Accordion.Control>View Form Values</Accordion.Control>
                    <Accordion.Panel>
                      <Code block>
                        {JSON.stringify(form.values, null, 2)}
                      </Code>
                    </Accordion.Panel>
                  </Accordion.Item>
                  <Accordion.Item value="form-errors">
                    <Accordion.Control>View Form Errors</Accordion.Control>
                    <Accordion.Panel>
                      <Code block>
                        {JSON.stringify(form.errors, null, 2)}
                      </Code>
                    </Accordion.Panel>
                  </Accordion.Item>
                </Accordion>
              </Grid.Col>
            </Grid>
          </Paper>

          {/* Component Documentation */}
          <Paper p="md" style={{
            background: 'rgba(44, 44, 44, 0.8)',
            border: '1px solid rgba(212, 165, 165, 0.1)',
            borderRadius: '12px',
            backdropFilter: 'blur(10px)'
          }}>
            <Title order={2} size="h3" mb="md" c="white">Design Features Demonstrated</Title>
            <Grid>
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Stack gap="xs">
                  <Text fw={700} c="white">Floating Label Features:</Text>
                  <Text size="sm" c="dimmed">• Labels smoothly animate up when focused or filled</Text>
                  <Text size="sm" c="dimmed">• Clean underline animation on focus</Text>
                  <Text size="sm" c="dimmed">• Elegant transitions with cubic-bezier easing</Text>
                  <Text size="sm" c="dimmed">• Dark theme with sophisticated colors</Text>
                  <Text size="sm" c="dimmed">• Helper text with 1.1rem for readability</Text>
                  <Text size="sm" c="dimmed">• Error states with shake animation</Text>
                </Stack>
              </Grid.Col>
              <Grid.Col span={{ base: 12, md: 6 }}>
                <Stack gap="xs">
                  <Text fw={700} c="white">Interaction States:</Text>
                  <Text size="sm" c="dimmed">• Default/empty with subtle borders</Text>
                  <Text size="sm" c="dimmed">• Focus with brand color underline</Text>
                  <Text size="sm" c="dimmed">• Filled with elevated label position</Text>
                  <Text size="sm" c="dimmed">• Error with red color and animation</Text>
                  <Text size="sm" c="dimmed">• Disabled with reduced opacity</Text>
                  <Text size="sm" c="dimmed">• All test controls still functional</Text>
                </Stack>
              </Grid.Col>
            </Grid>
          </Paper>

        </Stack>
      </Container>
    </Box>
  );
};

export default FormComponentsTest;