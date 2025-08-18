import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Container, Box, Text, Stack, Button, Group } from '@mantine/core';
import { Link } from 'react-router-dom';
import { IconArrowLeft } from '@tabler/icons-react';
// Custom 3D elevation form components defined inline

// Custom 3D Elevation Input Component
interface ElevationInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  disabled?: boolean;
  helperText?: string;
}

const ElevationInput: React.FC<ElevationInputProps> = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder,
  disabled,
  helperText
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const [hasValue, setHasValue] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  
  useEffect(() => {
    setHasValue(value.length > 0);
  }, [value]);
  
  const getHelperText = (label: string): string => {
    if (helperText) return helperText;
    switch (label) {
      case 'Email Address': return "We'll never share your email";
      case 'Password': return 'Minimum 8 characters with special character';
      case 'Confirm Password': return 'Must match password above';
      case 'Scene Name': return 'Your unique identifier in the community';
      case 'Phone Number': return 'For emergency contact only';
      default: return '';
    }
  };
  
  const getFieldClasses = () => {
    const classes = ['elevation-field'];
    if (hasValue) classes.push('has-value');
    if (error) classes.push('has-error');
    if (disabled) classes.push('is-disabled');
    return classes.join(' ');
  };
  
  return (
    <Box className="elevation-input-wrapper" pos="relative" mb="md">
      <Text component="label" fw={500} size="sm" mb={8} c="#f8f4e6">
        {label}
        {required && <Text component="span" c="red" ml={4}>*</Text>}
      </Text>
      
      <input
        ref={inputRef}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        placeholder={placeholder}
        disabled={disabled}
        className={getFieldClasses()}
        style={{
          width: '100%',
          backgroundColor: 'var(--mantine-color-dark-7)',
          background: 'linear-gradient(145deg, #1e1e1e, #252525)',
          borderColor: error ? '#d63031' : 'rgba(212, 165, 165, 0.2)',
          border: `1px solid ${error ? '#d63031' : 'rgba(212, 165, 165, 0.2)'}`,
          color: '#f8f4e6',
          fontSize: '16px',
          padding: '16px',
          height: '56px',
          borderRadius: '8px',
          transformStyle: 'preserve-3d' as const,
          backfaceVisibility: 'hidden',
          transition: 'all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275)',
          willChange: 'transform, box-shadow',
          boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1), 0 0 0 1px rgba(212, 165, 165, 0.1)',
          outline: 'none',
          ...(isFocused && !error && {
            borderColor: '#9b4a75',
            transform: 'translateY(-6px) rotateX(2deg) rotateZ(0.5deg)',
            background: 'linear-gradient(145deg, rgba(155, 74, 117, 0.03), rgba(136, 1, 36, 0.02))',
            boxShadow: `
              0 8px 25px rgba(0, 0, 0, 0.25),
              0 4px 15px rgba(155, 74, 117, 0.2),
              0 2px 8px rgba(155, 74, 117, 0.3),
              0 1px 3px rgba(155, 74, 117, 0.4),
              0 0 0 2px rgba(155, 74, 117, 0.5)
            `
          }),
          ...(hasValue && !isFocused && {
            transform: 'translateY(-3px) rotateX(1deg)',
            boxShadow: `
              0 4px 12px rgba(0, 0, 0, 0.15),
              0 2px 6px rgba(155, 74, 117, 0.15),
              0 0 0 1px rgba(155, 74, 117, 0.3)
            `
          }),
          ...(error && {
            borderColor: '#d63031',
            boxShadow: `
              0 4px 12px rgba(214, 48, 49, 0.2),
              0 2px 6px rgba(214, 48, 49, 0.3),
              0 0 0 1px #d63031
            `
          })
        }}
      />
      
      {error && (
        <Text 
          size="xs" 
          c="#d63031" 
          mt="xs" 
          style={{
            marginLeft: '12px',
            animation: 'shake 0.3s ease-in-out'
          }}
        >
          {error}
        </Text>
      )}
      
      {!error && getHelperText(label) && (
        <Text size="xs" c="dimmed" mt="xs" style={{
          marginLeft: '12px',
          opacity: 0.8,
          fontSize: '0.875rem'
        }}>
          {getHelperText(label)}
        </Text>
      )}
    </Box>
  );
};

const FormDesignC: React.FC = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    
    if (!formData.email) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    }
    
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your password';
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords must match';
    }
    
    if (!formData.sceneName) {
      newErrors.sceneName = 'Scene name is required';
    }
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };
  
  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) return;
    
    setIsSubmitting(true);
    
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    alert('Form submitted successfully! (Demo)');
    setIsSubmitting(false);
  }, [formData]);
  
  return (
    <Container size="sm" py="xl">
      {/* Custom CSS for 3D effects */}
      <style>
        {`
          @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-4px); }
            75% { transform: translateX(4px); }
          }
          
          .elevation-form-container {
            perspective: 1200px;
            perspective-origin: center top;
          }
          
          .elevation-field:hover:not(:disabled) {
            transform: translateY(-2px) rotateX(1deg) !important;
            background: linear-gradient(145deg, #252525, #2a2a2a) !important;
            box-shadow: 
              0 2px 6px rgba(0, 0, 0, 0.15),
              0 1px 3px rgba(155, 74, 117, 0.1),
              0 0 0 1px rgba(212, 165, 165, 0.2) !important;
          }
          
          .elevation-form-container:has(.elevation-field:focus) .elevation-field:not(:focus) {
            opacity: 0.7;
            transform: translateY(1px) scale(0.98);
            filter: blur(0.5px);
            transition: all 0.3s ease;
          }
          
          @media (max-width: 768px) {
            .elevation-field {
              transform: none !important;
              box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
              transition: box-shadow 0.2s ease !important;
            }
            
            .elevation-field:focus {
              transform: none !important;
              box-shadow: 0 0 0 2px rgba(155, 74, 117, 0.5) !important;
            }
          }
          
          @media (prefers-reduced-motion: reduce) {
            .elevation-field {
              transform: none !important;
              transition: box-shadow 0.1s ease !important;
            }
          }
        `}
      </style>
      
      <Box mb="xl">
        <Group mb="md">
          <Button
            component={Link}
            to="/form-designs"
            variant="subtle"
            leftSection={<IconArrowLeft size={16} />}
            c="dimmed"
            size="sm"
          >
            Back to Gallery
          </Button>
        </Group>
        
        <Text
          size="xl"
          fw={700}
          mb="md"
          style={{
            background: 'linear-gradient(135deg, #9b4a75 0%, #b47171 50%, #d4a5a5 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            fontSize: '42px',
            fontFamily: 'Bodoni Moda, serif',
            letterSpacing: '1px'
          }}
        >
          3D Elevation Effect
        </Text>
        <Text size="lg" c="dimmed" mb="xl">
          Fields physically lift off the page when focused, creating a premium tactile experience 
          with multi-layered shadows and subtle 3D rotation effects.
        </Text>
      </Box>

      <Box
        className="elevation-form-container"
        style={{
          perspective: '1200px',
          perspectiveOrigin: 'center top',
          background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
          padding: '40px',
          borderRadius: '16px',
          border: '1px solid rgba(212, 165, 165, 0.1)',
          backdropFilter: 'blur(20px)',
          position: 'relative'
        }}
      >
        {/* Ambient lighting effect */}
        <Box
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: '40%',
            background: 'linear-gradient(180deg, rgba(155, 74, 117, 0.02) 0%, transparent 100%)',
            borderRadius: '16px 16px 0 0',
            pointerEvents: 'none'
          }}
        />
        
        <form onSubmit={handleSubmit}>
          <Stack gap="xl">
            <ElevationInput
              label="Email Address"
              value={formData.email}
              onChange={(value) => setFormData({...formData, email: value})}
              type="email"
              required
              placeholder="Enter your email"
              error={errors.email}
            />
            
            <ElevationInput
              label="Password"
              value={formData.password}
              onChange={(value) => setFormData({...formData, password: value})}
              type="password"
              required
              placeholder="Create a secure password"
              error={errors.password}
            />
            
            <ElevationInput
              label="Confirm Password"
              value={formData.confirmPassword}
              onChange={(value) => setFormData({...formData, confirmPassword: value})}
              type="password"
              required
              placeholder="Confirm your password"
              error={errors.confirmPassword}
            />
            
            <ElevationInput
              label="Scene Name"
              value={formData.sceneName}
              onChange={(value) => setFormData({...formData, sceneName: value})}
              placeholder="Your community scene name"
              error={errors.sceneName}
            />
            
            <ElevationInput
              label="Phone Number"
              value={formData.phone}
              onChange={(value) => setFormData({...formData, phone: value})}
              type="tel"
              placeholder="(555) 123-4567"
              error={errors.phone}
            />
            
            <Button
              type="submit"
              size="lg"
              variant="gradient"
              gradient={{ from: '#9b4a75', to: '#880124', deg: 45 }}
              loading={isSubmitting}
              style={{
                height: '56px',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                marginTop: '16px',
                transform: 'translateY(0px)',
                transition: 'all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275)',
                boxShadow: '0 4px 15px rgba(155, 74, 117, 0.3)'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateY(-2px)';
                e.currentTarget.style.boxShadow = '0 6px 20px rgba(155, 74, 117, 0.4)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateY(0px)';
                e.currentTarget.style.boxShadow = '0 4px 15px rgba(155, 74, 117, 0.3)';
              }}
            >
              {isSubmitting ? 'Creating Account...' : 'Create Account'}
            </Button>
          </Stack>
        </form>
      </Box>
      
      <Box mt="xl" p="md" style={{
        background: 'rgba(44, 44, 44, 0.6)',
        borderRadius: '8px',
        border: '1px solid rgba(212, 165, 165, 0.1)'
      }}>
        <Text size="sm" c="dimmed" style={{ lineHeight: 1.6 }}>
          <strong>3D Elevation Features:</strong> This design uses CSS transforms and multi-layered shadows 
          to create the illusion of fields physically lifting off the page. The perspective container and 
          rotateX/rotateZ transforms provide realistic 3D depth, while surrounding fields dim when one is active.
        </Text>
      </Box>
    </Container>
  );
};

export default FormDesignC;