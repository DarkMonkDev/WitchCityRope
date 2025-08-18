import React, { useState, useRef, useCallback } from 'react';
import { Container, Box, Text, Stack, Button, Group } from '@mantine/core';
import { Link } from 'react-router-dom';
import { IconArrowLeft } from '@tabler/icons-react';
import { useClickOutside } from '@mantine/hooks';

// Custom Neon Ripple Input Component
interface NeonRippleInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
  type?: string;
  placeholder?: string;
  disabled?: boolean;
  helperText?: string;
  onFocus?: (e: React.FocusEvent) => void;
  onBlur?: () => void;
}

const NeonRippleInput: React.FC<NeonRippleInputProps> = ({ 
  label, 
  value, 
  onChange, 
  error, 
  required,
  type = 'text',
  placeholder,
  disabled,
  helperText,
  onFocus,
  onBlur
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const [isRippleActive, setIsRippleActive] = useState(false);
  const [ripplePosition, setRipplePosition] = useState({ x: '50%', y: '50%' });
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  
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
  
  const handleClick = useCallback((e: React.MouseEvent) => {
    if (disabled) return;
    
    const rect = e.currentTarget.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    
    setRipplePosition({ x: `${x}%`, y: `${y}%` });
    setIsRippleActive(true);
    
    // Reset ripple after animation
    setTimeout(() => setIsRippleActive(false), 800);
    
    inputRef.current?.focus();
  }, [disabled]);
  
  const handleFocus = useCallback((e: React.FocusEvent) => {
    setIsFocused(true);
    onFocus?.(e);
  }, [onFocus]);
  
  const handleBlur = useCallback(() => {
    setIsFocused(false);
    onBlur?.();
  }, [onBlur]);
  
  const getFieldClasses = () => {
    const classes = ['neon-field'];
    if (value.length > 0) classes.push('has-value');
    if (error) classes.push('has-error');
    if (disabled) classes.push('is-disabled');
    if (isRippleActive) classes.push('ripple-active');
    if (isFocused) classes.push('spotlight-target');
    return classes.join(' ');
  };
  
  return (
    <Box className="neon-input-wrapper" pos="relative" ref={containerRef} mb="md">
      <Text component="label" fw={500} size="sm" mb={8} c="#f8f4e6">
        {label}
        {required && <Text component="span" c="red" ml={4}>*</Text>}
      </Text>
      
      <Box 
        style={{ 
          position: 'relative', 
          overflow: 'hidden',
          borderRadius: '8px'
        }}
      >
        <input
          ref={inputRef}
          type={type}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onClick={handleClick}
          placeholder={placeholder}
          disabled={disabled}
          className={getFieldClasses()}
          style={{
            width: '100%',
            backgroundColor: '#252525',
            background: 'linear-gradient(145deg, #252525, #2a2a2a)',
            border: `1px solid ${error ? '#d63031' : 'rgba(155, 74, 117, 0.3)'}`,
            color: '#f8f4e6',
            fontSize: '16px',
            padding: '16px',
            height: '56px',
            borderRadius: '8px',
            position: 'relative',
            overflow: 'hidden',
            transition: 'all 0.3s ease',
            outline: 'none',
            zIndex: 1,
            ...(isFocused && !error && {
              borderColor: '#9b4a75',
              background: 'linear-gradient(145deg, #2a2a2a, #303030)',
              animation: 'neonPulse 2s ease-in-out infinite',
              zIndex: 2
            }),
            ...(value.length > 0 && !isFocused && {
              borderColor: 'rgba(155, 74, 117, 0.8)',
              boxShadow: `
                0 0 8px rgba(155, 74, 117, 0.6),
                0 0 15px rgba(155, 74, 117, 0.3),
                inset 0 0 5px rgba(155, 74, 117, 0.1)
              `
            }),
            ...(error && {
              borderColor: '#d63031',
              boxShadow: '0 0 8px rgba(214, 48, 49, 0.6), 0 0 15px rgba(214, 48, 49, 0.3)',
              animation: 'neonPulse 2s ease-in-out infinite, shake 0.3s ease-in-out'
            })
          }}
        />
        
        {/* Ripple effect */}
        <Box
          style={{
            position: 'absolute',
            top: ripplePosition.y,
            left: ripplePosition.x,
            width: isRippleActive ? '400px' : '0',
            height: isRippleActive ? '400px' : '0',
            background: 'radial-gradient(circle, rgba(155, 74, 117, 0.4) 0%, transparent 70%)',
            borderRadius: '50%',
            transform: 'translate(-50%, -50%)',
            opacity: isRippleActive ? 0.8 : 0,
            pointerEvents: 'none',
            transition: 'all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94)',
            zIndex: 0
          }}
        />
      </Box>
      
      {error && (
        <Text 
          size="xs" 
          c="#d63031" 
          mt="xs" 
          style={{
            marginLeft: '12px',
            textShadow: '0 0 4px rgba(214, 48, 49, 0.5)',
            fontSize: '0.875rem'
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

const FormDesignD: React.FC = () => {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    sceneName: '',
    phone: ''
  });
  
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [activeField, setActiveField] = useState<string | null>(null);
  const [spotlightPosition, setSpotlightPosition] = useState({ x: '50%', y: '50%' });
  
  const validateForm = useCallback(() => {
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
  }, [formData]);
  
  const formRef = useClickOutside(() => {
    setActiveField(null);
  });
  
  const handleFieldFocus = useCallback((fieldName: string, event?: React.FocusEvent) => {
    setActiveField(fieldName);
    
    if (event && formRef.current) {
      const formRect = formRef.current.getBoundingClientRect();
      const targetRect = event.target.getBoundingClientRect();
      
      const x = ((targetRect.left + targetRect.width / 2 - formRect.left) / formRect.width) * 100;
      const y = ((targetRect.top + targetRect.height / 2 - formRect.top) / formRect.height) * 100;
      
      setSpotlightPosition({ x: `${x}%`, y: `${y}%` });
    }
  }, []);
  
  const handleFieldBlur = useCallback(() => {
    setActiveField(null);
  }, []);
  
  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) return;
    
    setIsSubmitting(true);
    
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      alert('Form submitted successfully! (Demo)');
    } catch (error) {
      console.error('Form submission error:', error);
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, validateForm]);
  
  return (
    <Container size="sm" py="xl">
      {/* Custom CSS for Neon Ripple effects */}
      <style>
        {`
          @keyframes neonPulse {
            0%, 100% {
              box-shadow: 
                0 0 5px rgba(155, 74, 117, 0.5),
                0 0 10px rgba(155, 74, 117, 0.3),
                inset 0 0 5px rgba(155, 74, 117, 0.1);
            }
            50% {
              box-shadow: 
                0 0 8px rgba(155, 74, 117, 0.8),
                0 0 15px rgba(155, 74, 117, 0.5),
                0 0 20px rgba(155, 74, 117, 0.3),
                inset 0 0 8px rgba(155, 74, 117, 0.2);
            }
          }
          
          @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-4px); }
            75% { transform: translateX(4px); }
          }
          
          .neon-field:hover:not(:disabled) {
            border-color: rgba(155, 74, 117, 0.6) !important;
            box-shadow: 0 0 5px rgba(155, 74, 117, 0.6), 0 0 10px rgba(155, 74, 117, 0.3) !important;
            background: linear-gradient(145deg, #252525, #2a2a2a) !important;
          }
          
          .neon-form-container::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: radial-gradient(
              circle at var(--spotlight-x, 50%) var(--spotlight-y, 50%),
              transparent 20%,
              rgba(0, 0, 0, 0.2) 50%,
              rgba(0, 0, 0, 0.6) 80%
            );
            opacity: 0;
            pointer-events: none;
            transition: opacity 0.4s ease;
            z-index: 1;
            border-radius: inherit;
          }
          
          .neon-form-container.spotlight-active::before {
            opacity: 1;
          }
          
          .neon-form-container.spotlight-active .neon-field:not(.spotlight-target) {
            opacity: 0.4;
            filter: blur(1px) brightness(0.8);
            transform: scale(0.98);
            transition: all 0.4s ease;
          }
          
          .neon-form-container.spotlight-active .neon-field.spotlight-target {
            opacity: 1;
            filter: brightness(1.2) contrast(1.1) saturate(1.1);
            transform: scale(1.02);
            z-index: 2;
            position: relative;
            transition: all 0.4s ease;
          }
          
          @media (max-width: 768px) {
            .neon-field {
              animation: none !important;
              box-shadow: 0 0 3px rgba(155, 74, 117, 0.3) !important;
            }
            
            .neon-form-container::before {
              display: none;
            }
            
            .neon-form-container.spotlight-active .neon-field {
              opacity: 1 !important;
              filter: none !important;
              transform: none !important;
            }
          }
          
          @media (prefers-reduced-motion: reduce) {
            .neon-field {
              animation: none !important;
              transition: box-shadow 0.1s ease !important;
            }
            
            .neon-form-container::before {
              display: none;
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
          Neon Ripple Spotlight
        </Text>
        <Text size="lg" c="dimmed" mb="xl">
          Cyberpunk-inspired interface with pulsing neon glows, click-position ripple effects, 
          and dynamic spotlighting that creates an immersive sci-fi experience.
        </Text>
      </Box>

      <Box
        ref={formRef}
        className={`neon-form-container ${activeField ? 'spotlight-active' : ''}`}
        style={{
          '--spotlight-x': spotlightPosition.x,
          '--spotlight-y': spotlightPosition.y,
          background: 'linear-gradient(135deg, #1a1a1a 0%, #2c2c2c 100%)',
          padding: '40px',
          borderRadius: '16px',
          border: '1px solid rgba(155, 74, 117, 0.2)',
          backdropFilter: 'blur(20px)',
          position: 'relative',
          transition: 'all 0.4s ease'
        } as React.CSSProperties}
      >
        {/* Ambient neon glow around form */}
        <Box
          style={{
            position: 'absolute',
            top: '-2px',
            left: '-2px',
            right: '-2px',
            bottom: '-2px',
            background: 'linear-gradient(45deg, transparent, rgba(155, 74, 117, 0.1), transparent)',
            borderRadius: '18px',
            opacity: activeField ? 0.6 : 0.2,
            transition: 'opacity 0.4s ease',
            pointerEvents: 'none',
            zIndex: -1
          }}
        />
        
        <form onSubmit={handleSubmit}>
          <Stack gap="xl">
            <NeonRippleInput
              label="Email Address"
              value={formData.email}
              onChange={(value) => setFormData({...formData, email: value})}
              onFocus={(e) => handleFieldFocus('email', e)}
              onBlur={handleFieldBlur}
              type="email"
              required
              placeholder="Enter your email"
              error={errors.email}
            />
            
            <NeonRippleInput
              label="Password"
              value={formData.password}
              onChange={(value) => setFormData({...formData, password: value})}
              onFocus={(e) => handleFieldFocus('password', e)}
              onBlur={handleFieldBlur}
              type="password"
              required
              placeholder="Create a secure password"
              error={errors.password}
            />
            
            <NeonRippleInput
              label="Confirm Password"
              value={formData.confirmPassword}
              onChange={(value) => setFormData({...formData, confirmPassword: value})}
              onFocus={(e) => handleFieldFocus('confirmPassword', e)}
              onBlur={handleFieldBlur}
              type="password"
              required
              placeholder="Confirm your password"
              error={errors.confirmPassword}
            />
            
            <NeonRippleInput
              label="Scene Name"
              value={formData.sceneName}
              onChange={(value) => setFormData({...formData, sceneName: value})}
              onFocus={(e) => handleFieldFocus('sceneName', e)}
              onBlur={handleFieldBlur}
              placeholder="Your community scene name"
              error={errors.sceneName}
            />
            
            <NeonRippleInput
              label="Phone Number"
              value={formData.phone}
              onChange={(value) => setFormData({...formData, phone: value})}
              onFocus={(e) => handleFieldFocus('phone', e)}
              onBlur={handleFieldBlur}
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
                boxShadow: '0 0 15px rgba(155, 74, 117, 0.4)',
                border: '1px solid rgba(155, 74, 117, 0.3)',
                animation: activeField ? 'none' : 'neonPulse 3s ease-in-out infinite'
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
          <strong>Neon Ripple Features:</strong> This design combines cyberpunk aesthetics with dynamic 
          interactions. Click position tracking creates ripples that expand from your exact click point, 
          while the spotlight effect dims surrounding fields and the neon glow pulses continuously.
        </Text>
      </Box>
    </Container>
  );
};

export default FormDesignD;