import React, { useState, useRef } from 'react';
import { Container, Box, Text, Stack, Button, Group } from '@mantine/core';
import { Link } from 'react-router-dom';
import { IconArrowLeft } from '@tabler/icons-react';

// Custom Floating Label Input Component
interface FloatingLabelInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
}

// Helper text mapping for each field
const getHelperText = (label: string): string => {
  switch (label) {
    case 'Email Address':
      return "We'll never share your email";
    case 'Password':
      return 'Minimum 8 characters with special character';
    case 'Confirm Password':
      return 'Must match password above';
    case 'Scene Name':
      return 'Your unique identifier in the community';
    case 'Phone Number':
      return 'For emergency contact only';
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
  placeholder = ' ' // Space for floating effect
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  
  const isActive = isFocused || value.length > 0;
  
  return (
    <Box style={{ position: 'relative', marginBottom: '24px' }}>
      <input
        ref={inputRef}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        placeholder={placeholder}
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
          // Force dark background for all states and autocomplete
          WebkitBoxShadow: 'inset 0 0 0 1000px var(--mantine-color-dark-7)',
          WebkitTextFillColor: '#f8f4e6',
          ...(isFocused && !error && {
            borderColor: '#9b4a75',
            boxShadow: '0 0 0 2px rgba(155, 74, 117, 0.2), 0 4px 12px rgba(155, 74, 117, 0.1), inset 0 0 0 1000px var(--mantine-color-dark-7)',
            background: 'linear-gradient(145deg, rgba(155, 74, 117, 0.03), rgba(136, 1, 36, 0.02))',
            transform: 'translateY(-2px)'
          })
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
      
      {error && (
        <Text size="sm" c="red" mt="xs" style={{
          animation: 'shake 0.5s ease-in-out'
        }}>
          {error}
        </Text>
      )}
      
      {/* Helper text */}
      {!error && getHelperText(label) && (
        <Text size="sm" c="dimmed" mt="xs" style={{
          marginLeft: '12px',
          opacity: 0.85,
          fontSize: '1.1rem',
          lineHeight: '1.6'
        }}>
          {getHelperText(label)}
        </Text>
      )}

      <style>{`
        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          25% { transform: translateX(-4px); }
          75% { transform: translateX(4px); }
        }
        
        /* Force dark backgrounds on all input states */
        input[type="text"], input[type="email"], input[type="password"], input[type="tel"] {
          background-color: var(--mantine-color-dark-7) !important;
          color: #f8f4e6 !important;
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
      `}</style>
    </Box>
  );
};

const FormDesignA: React.FC = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });

  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    setIsSubmitting(true);
    // Simulate form validation
    const newErrors: Record<string, string> = {};
    
    if (!formData.email) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    
    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    }
    
    if (formData.confirmPassword !== formData.password) {
      newErrors.confirmPassword = 'Passwords do not match';
    }

    setErrors(newErrors);
    
    // Simulate async operation
    setTimeout(() => {
      setIsSubmitting(false);
      if (Object.keys(newErrors).length === 0) {
        alert('Form submitted successfully! (Demo)');
      }
    }, 1500);
  };

  return (
    <Container size="sm" py="xl">
      <Group mb="xl">
        <Button
          component={Link}
          to="/form-designs"
          variant="subtle"
          leftSection={<IconArrowLeft size={16} />}
          c="dimmed"
        >
          Back to Gallery
        </Button>
      </Group>

      <Box mb="xl" style={{ textAlign: 'center' }}>
        <Text
          size="xl"
          fw={700}
          mb="md"
          style={{
            background: 'linear-gradient(135deg, #9b4a75 0%, #b47171 50%, #d4a5a5 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            fontSize: '36px',
            fontFamily: 'Bodoni Moda, serif',
            letterSpacing: '1px'
          }}
        >
          Floating Label Design
        </Text>
        <Text size="lg" c="dimmed" mb="xs">
          Modern & Elegant
        </Text>
        <Text c="dimmed" maw={500} mx="auto">
          Sophisticated floating label design with smooth transitions and subtle lighting effects that create an immersive "Lightning Dark" experience.
        </Text>
      </Box>

      <Box
        style={{
          background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
          padding: '40px',
          borderRadius: '12px',
          border: '1px solid rgba(212, 165, 165, 0.1)',
          backdropFilter: 'blur(10px)',
          boxShadow: '0 20px 40px rgba(0, 0, 0, 0.3)'
        }}
      >
        <Stack gap="xl">
          <FloatingLabelInput
            label="Email Address"
            value={formData.email}
            onChange={(value) => setFormData({...formData, email: value})}
            type="email"
            required
            error={errors.email}
          />
          
          <FloatingLabelInput
            label="Password"
            value={formData.password}
            onChange={(value) => setFormData({...formData, password: value})}
            type="password"
            required
            error={errors.password}
          />
          
          <FloatingLabelInput
            label="Confirm Password"
            value={formData.confirmPassword}
            onChange={(value) => setFormData({...formData, confirmPassword: value})}
            type="password"
            required
            error={errors.confirmPassword}
          />
          
          <FloatingLabelInput
            label="Scene Name"
            value={formData.sceneName}
            onChange={(value) => setFormData({...formData, sceneName: value})}
          />
          
          <FloatingLabelInput
            label="Phone Number"
            value={formData.phone}
            onChange={(value) => setFormData({...formData, phone: value})}
            type="tel"
          />
          
          <Button
            size="lg"
            variant="gradient"
            gradient={{ from: 'grape.6', to: 'grape.7', deg: 45 }}
            loading={isSubmitting}
            onClick={handleSubmit}
            style={{
              height: '56px',
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              marginTop: '16px'
            }}
          >
            Create Account
          </Button>
        </Stack>
      </Box>

      <Box mt="xl" p="md" style={{
        background: 'rgba(44, 44, 44, 0.3)',
        borderRadius: '8px',
        border: '1px solid rgba(212, 165, 165, 0.1)'
      }}>
        <Text size="sm" fw={500} c="white" mb="xs">Design Features:</Text>
        <Text size="sm" c="dimmed" style={{ lineHeight: 1.6 }}>
          • Labels smoothly animate up and outside the field border when focused or filled<br/>
          • Subtle glow effects and gradient backgrounds on focus<br/>
          • Elegant transitions using cubic-bezier easing<br/>
          • Error states with shake animation and color changes<br/>
          • Mystical "Lightning Dark" aesthetic with plum accent colors
        </Text>
      </Box>
    </Container>
  );
};

export default FormDesignA;