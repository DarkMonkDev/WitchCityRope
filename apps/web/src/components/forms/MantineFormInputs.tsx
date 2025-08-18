import React, { forwardRef, useState, useCallback } from 'react';
import {
  TextInput,
  PasswordInput as MantinePasswordInput,
  Textarea,
  Select,
  TextInputProps,
  PasswordInputProps,
  TextareaProps,
  SelectProps,
  Box,
  Text,
  Stack,
  Group,
  Progress,
  Loader
} from '@mantine/core';
import { IconAlertCircle, IconCheck, IconX } from '@tabler/icons-react';
import clsx from 'clsx';
import styles from '../../styles/FormComponents.module.css';

// Extended interfaces to support our custom features
export interface EnhancedTextInputProps extends Omit<TextInputProps, 'classNames'> {
  loading?: boolean;
  asyncValidation?: boolean;
  taperedUnderline?: boolean;
  'data-testid'?: string;
}

export interface EnhancedPasswordInputProps extends Omit<PasswordInputProps, 'classNames'> {
  loading?: boolean;
  showStrengthMeter?: boolean;
  strengthRequirements?: PasswordRequirement[];
  taperedUnderline?: boolean;
  'data-testid'?: string;
}

export interface EnhancedTextareaProps extends Omit<TextareaProps, 'classNames'> {
  loading?: boolean;
  taperedUnderline?: boolean;
  'data-testid'?: string;
}

export interface EnhancedSelectProps extends Omit<SelectProps, 'classNames'> {
  loading?: boolean;
  taperedUnderline?: boolean;
  'data-testid'?: string;
}

export interface PasswordRequirement {
  re: RegExp;
  label: string;
}

// Default password requirements
const DEFAULT_PASSWORD_REQUIREMENTS: PasswordRequirement[] = [
  { re: /.{8,}/, label: 'At least 8 characters' },
  { re: /[a-z]/, label: 'Contains lowercase letter' },
  { re: /[A-Z]/, label: 'Contains uppercase letter' },
  { re: /\d/, label: 'Contains number' },
  { re: /[@$!%*?&]/, label: 'Contains special character (@$!%*?&)' }
];

// Enhanced TextInput with floating label and tapered underline
export const EnhancedTextInput = forwardRef<HTMLInputElement, EnhancedTextInputProps>(
  ({
    loading = false,
    asyncValidation = false,
    taperedUnderline = false,
    'data-testid': testId,
    rightSection,
    label,
    value = '',
    onFocus,
    onBlur,
    error,
    className,
    placeholder,
    ...props
  }, ref) => {
    const [focused, setFocused] = useState(false);
    const hasValue = Boolean(value);

    const handleFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(true);
      onFocus?.(event);
    }, [onFocus]);

    const handleBlur = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(false);
      onBlur?.(event);
    }, [onBlur]);

    // Determine right section content
    const getRightSection = () => {
      if (loading || asyncValidation) {
        return <Loader size="xs" />;
      }
      if (error) {
        return <IconAlertCircle size={16} color="var(--mantine-color-red-6)" />;
      }
      return rightSection;
    };

    // Build container classes
    const containerClasses = clsx(
      styles.formGroup,
      {
        [styles.taperedUnderline]: taperedUnderline,
        [styles.focused]: focused && taperedUnderline,
        [styles.error]: error && taperedUnderline,
        [styles.loadingOverlay]: loading,
        [styles.loading]: loading
      }
    );

    // Build input classes
    const inputClasses = clsx(
      styles.enhancedInput,
      {
        [styles.errorInput]: error,
        [styles.wcrFocus]: !error
      },
      className
    );

    if (label) {
      return (
        <Box className={containerClasses}>
          {/* Input container isolates input + label from helper text */}
          <Box className={styles.inputContainer}>
            <TextInput
              ref={ref}
              data-testid={testId}
              value={value}
              onFocus={handleFocus}
              onBlur={handleBlur}
              rightSection={getRightSection()}
              rightSectionProps={{ style: { pointerEvents: 'none' } }}
              error={false} // Error styling handled by outer container
              className={inputClasses}
              placeholder={placeholder}
              styles={{
                root: {
                  position: 'relative'
                },
                wrapper: {
                  position: 'relative'
                },
                input: {
                  '&::placeholder': {
                    opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                    visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                    transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
                  },
                  // Remove any conflicting focus styles
                  '&:focus': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  },
                  '&:focus-visible': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  }
                }
              }}
              {...props}
              // Remove description and error from here - they go outside
              description={undefined}
              error={undefined}
            />
            <Text
              className={clsx(styles.floatingLabel, {
                [styles.hasValue]: hasValue,
                [styles.isFocused]: focused,
                [styles.isEmpty]: !hasValue && !focused,
                [styles.hasError]: error
              })}
              component="label"
            >
              {label}
            </Text>
          </Box>
          {/* Description and error outside the input container */}
          {props.description && (
            <Text
              size="xs"
              c="dimmed"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {props.description}
            </Text>
          )}
          {error && (
            <Text
              size="xs"
              c="red"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {error}
            </Text>
          )}
        </Box>
      );
    }

    return (
      <Box className={containerClasses}>
        <TextInput
          ref={ref}
          data-testid={testId}
          value={value}
          onFocus={handleFocus}
          onBlur={handleBlur}
          rightSection={getRightSection()}
          rightSectionProps={{ style: { pointerEvents: 'none' } }}
          error={error}
          className={inputClasses}
          placeholder={placeholder}
          styles={{
            root: {
              display: 'flex',
              flexDirection: 'column'
            },
            label: {
              order: 1,
              marginBottom: 'var(--mantine-spacing-xs)'
            },
            wrapper: {
              order: 2
            },
            input: {
              '&::placeholder': {
                opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
              },
              // Remove any conflicting focus styles
              '&:focus': {
                outline: 'none !important',
                boxShadow: 'none !important'
              },
              '&:focus-visible': {
                outline: 'none !important',
                boxShadow: 'none !important'
              }
            },
            description: {
              order: 3,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)',
              color: 'var(--mantine-color-gray-6)'
            },
            error: {
              order: 4,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)'
            }
          }}
          {...props}
        />
      </Box>
    );
  }
);

EnhancedTextInput.displayName = 'EnhancedTextInput';

// Enhanced PasswordInput with strength meter
export const EnhancedPasswordInput = forwardRef<HTMLInputElement, EnhancedPasswordInputProps>(
  ({
    loading = false,
    showStrengthMeter = true,
    strengthRequirements = DEFAULT_PASSWORD_REQUIREMENTS,
    taperedUnderline = false,
    'data-testid': testId,
    label,
    value = '',
    onFocus,
    onBlur,
    error,
    className,
    placeholder,
    ...props
  }, ref) => {
    const [focused, setFocused] = useState(false);
    const hasValue = Boolean(value);

    const handleFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(true);
      onFocus?.(event);
    }, [onFocus]);

    const handleBlur = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(false);
      onBlur?.(event);
    }, [onBlur]);

    // Calculate password strength
    const { strength, color, requirements } = React.useMemo(() => {
      const stringValue = String(value);
      const requirementChecks = strengthRequirements.map((requirement) => ({
        ...requirement,
        met: requirement.re.test(stringValue)
      }));

      const metRequirements = requirementChecks.filter(req => req.met).length;
      const strengthPercentage = metRequirements / strengthRequirements.length * 100;
      
      let strengthColor = 'red';
      if (strengthPercentage >= 100) strengthColor = 'green';
      else if (strengthPercentage >= 80) strengthColor = 'yellow';
      else if (strengthPercentage >= 50) strengthColor = 'orange';

      return {
        strength: strengthPercentage,
        color: strengthColor,
        requirements: requirementChecks
      };
    }, [value, strengthRequirements]);

    // Build container classes
    const containerClasses = clsx(
      styles.formGroup,
      {
        [styles.taperedUnderline]: taperedUnderline,
        [styles.focused]: focused && taperedUnderline,
        [styles.error]: error && taperedUnderline,
        [styles.loadingOverlay]: loading,
        [styles.loading]: loading
      }
    );

    // Build input classes
    const inputClasses = clsx(
      styles.enhancedInput,
      {
        [styles.errorInput]: error,
        [styles.wcrFocus]: !error
      },
      className
    );

    const passwordInput = label ? (
      <Box className={styles.inputContainer}>
        <MantinePasswordInput
          ref={ref}
          data-testid={testId}
          value={value}
          onFocus={handleFocus}
          onBlur={handleBlur}
          error={false} // Error styling handled by outer container
          className={inputClasses}
          placeholder={placeholder}
          styles={{
            root: {
              position: 'relative'
            },
            wrapper: {
              position: 'relative'
            },
            input: {
              '&::placeholder': {
                opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
              },
              // Remove any conflicting focus styles
              '&:focus': {
                outline: 'none !important',
                boxShadow: 'none !important'
              },
              '&:focus-visible': {
                outline: 'none !important',
                boxShadow: 'none !important'
              }
            }
          }}
          {...props}
          // Remove description and error from here - they go outside
          description={undefined}
          error={undefined}
        />
        <Text
          className={clsx(styles.floatingLabel, {
            [styles.hasValue]: hasValue,
            [styles.isFocused]: focused,
            [styles.isEmpty]: !hasValue && !focused,
            [styles.hasError]: error
          })}
          component="label"
        >
          {label}
        </Text>
      </Box>
    ) : (
      <MantinePasswordInput
        ref={ref}
        data-testid={testId}
        value={value}
        onFocus={handleFocus}
        onBlur={handleBlur}
        error={error}
        className={inputClasses}
        placeholder={placeholder}
        styles={{
          root: {
            display: 'flex',
            flexDirection: 'column'
          },
          label: {
            order: 1,
            marginBottom: 'var(--mantine-spacing-xs)'
          },
          wrapper: {
            order: 2
          },
          input: {
            '&::placeholder': {
              opacity: (focused && !hasValue) ? 1 : 0,
              transition: 'opacity 0.2s ease-in-out'
            },
            // Remove any conflicting focus styles
            '&:focus': {
              outline: 'none !important',
              boxShadow: 'none !important'
            },
            '&:focus-visible': {
              outline: 'none !important',
              boxShadow: 'none !important'
            }
          },
          description: {
            order: 3,
            textAlign: 'left',
            marginTop: 'var(--mantine-spacing-xs)',
            fontSize: 'var(--mantine-font-size-xs)',
            color: 'var(--mantine-color-gray-6)'
          },
          error: {
            order: 4,
            textAlign: 'left',
            marginTop: 'var(--mantine-spacing-xs)',
            fontSize: 'var(--mantine-font-size-xs)'
          }
        }}
        {...props}
      />
    );

    // If strength meter is disabled or no value, return just the input
    if (!showStrengthMeter || !value) {
      return (
        <Box className={containerClasses}>
          {passwordInput}
          {/* Description and error outside the input container */}
          {props.description && (
            <Text
              size="xs"
              c="dimmed"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {props.description}
            </Text>
          )}
          {error && (
            <Text
              size="xs"
              c="red"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {error}
            </Text>
          )}
        </Box>
      );
    }

    return (
      <Box className={containerClasses}>
        <Stack gap="xs">
          {passwordInput}
          
          {/* Description and error outside the input container */}
          {props.description && (
            <Text
              size="xs"
              c="dimmed"
              style={{ textAlign: 'left' }}
            >
              {props.description}
            </Text>
          )}
          {error && (
            <Text
              size="xs"
              c="red"
              style={{ textAlign: 'left' }}
            >
              {error}
            </Text>
          )}
          
          {/* Password Strength Progress Bar */}
          <Box>
            <Group justify="space-between" mb={3}>
              <Text size="sm" fw={500}>
                Password strength
              </Text>
              <Text size="sm" c={color} fw={500}>
                {strength < 50 ? 'Weak' : strength < 100 ? 'Fair' : 'Strong'}
              </Text>
            </Group>
            <Progress 
              value={strength} 
              color={color} 
              size="sm" 
              data-testid={`${testId}-strength-meter`}
            />
          </Box>

          {/* Password Requirements Checklist */}
          <Stack gap={2}>
            {requirements.map((req, index) => (
              <Group key={index} gap="xs" align="center">
                {req.met ? (
                  <IconCheck size={14} color="var(--mantine-color-green-6)" />
                ) : (
                  <IconX size={14} color="var(--mantine-color-red-6)" />
                )}
                <Text 
                  size="sm" 
                  c={req.met ? 'green.6' : 'red.6'}
                  data-testid={`${testId}-requirement-${index}`}
                  className={clsx(styles.validationMessage, {
                    [styles.success]: req.met,
                    [styles.error]: !req.met
                  })}
                >
                  {req.label}
                </Text>
              </Group>
            ))}
          </Stack>
        </Stack>
      </Box>
    );
  }
);

EnhancedPasswordInput.displayName = 'EnhancedPasswordInput';

// Enhanced Textarea
export const EnhancedTextarea = forwardRef<HTMLTextAreaElement, EnhancedTextareaProps>(
  ({
    loading = false,
    taperedUnderline = false,
    'data-testid': testId,
    label,
    value = '',
    onFocus,
    onBlur,
    error,
    className,
    placeholder,
    ...props
  }, ref) => {
    const [focused, setFocused] = useState(false);
    const hasValue = Boolean(value);

    const handleFocus = useCallback((event: React.FocusEvent<HTMLTextAreaElement>) => {
      setFocused(true);
      onFocus?.(event);
    }, [onFocus]);

    const handleBlur = useCallback((event: React.FocusEvent<HTMLTextAreaElement>) => {
      setFocused(false);
      onBlur?.(event);
    }, [onBlur]);

    // Build container classes
    const containerClasses = clsx(
      styles.formGroup,
      {
        [styles.taperedUnderline]: taperedUnderline,
        [styles.focused]: focused && taperedUnderline,
        [styles.error]: error && taperedUnderline,
        [styles.loadingOverlay]: loading,
        [styles.loading]: loading
      }
    );

    // Build input classes
    const inputClasses = clsx(
      styles.enhancedInput,
      {
        [styles.errorInput]: error,
        [styles.wcrFocus]: !error
      },
      className
    );

    if (label) {
      return (
        <Box className={containerClasses}>
          {/* Input container isolates input + label from helper text */}
          <Box className={styles.inputContainer}>
            <Textarea
              ref={ref}
              data-testid={testId}
              value={value}
              onFocus={handleFocus}
              onBlur={handleBlur}
              error={false} // Error styling handled by outer container
              className={inputClasses}
              placeholder={placeholder}
              styles={{
                root: {
                  position: 'relative'
                },
                wrapper: {
                  position: 'relative'
                },
                input: {
                  '&::placeholder': {
                    opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                    visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                    transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
                  },
                  // Remove any conflicting focus styles
                  '&:focus': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  },
                  '&:focus-visible': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  }
                }
              }}
              {...props}
              // Remove description and error from here - they go outside
              description={undefined}
              error={undefined}
            />
            <Text
              className={clsx(styles.floatingLabel, {
                [styles.hasValue]: hasValue,
                [styles.isFocused]: focused,
                [styles.isEmpty]: !hasValue && !focused,
                [styles.hasError]: error
              })}
              component="label"
            >
              {label}
            </Text>
          </Box>
          {/* Description and error outside the input container */}
          {props.description && (
            <Text
              size="xs"
              c="dimmed"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {props.description}
            </Text>
          )}
          {error && (
            <Text
              size="xs"
              c="red"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {error}
            </Text>
          )}
        </Box>
      );
    }

    return (
      <Box className={containerClasses}>
        <Textarea
          ref={ref}
          data-testid={testId}
          value={value}
          onFocus={handleFocus}
          onBlur={handleBlur}
          error={error}
          className={inputClasses}
          placeholder={placeholder}
          styles={{
            root: {
              display: 'flex',
              flexDirection: 'column'
            },
            label: {
              order: 1,
              marginBottom: 'var(--mantine-spacing-xs)'
            },
            wrapper: {
              order: 2
            },
            input: {
              '&::placeholder': {
                opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
              },
              // Remove any conflicting focus styles
              '&:focus': {
                outline: 'none !important',
                boxShadow: 'none !important'
              },
              '&:focus-visible': {
                outline: 'none !important',
                boxShadow: 'none !important'
              }
            },
            description: {
              order: 3,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)',
              color: 'var(--mantine-color-gray-6)'
            },
            error: {
              order: 4,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)'
            }
          }}
          {...props}
        />
      </Box>
    );
  }
);

EnhancedTextarea.displayName = 'EnhancedTextarea';

// Enhanced Select
export const EnhancedSelect = forwardRef<HTMLInputElement, EnhancedSelectProps>(
  ({
    loading = false,
    taperedUnderline = false,
    'data-testid': testId,
    rightSection,
    label,
    value = '',
    onFocus,
    onBlur,
    error,
    className,
    placeholder,
    ...props
  }, ref) => {
    const [focused, setFocused] = useState(false);
    const hasValue = Boolean(value);

    const handleFocus = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(true);
      onFocus?.(event);
    }, [onFocus]);

    const handleBlur = useCallback((event: React.FocusEvent<HTMLInputElement>) => {
      setFocused(false);
      onBlur?.(event);
    }, [onBlur]);

    // Determine right section content
    const getRightSection = () => {
      if (loading) {
        return <Loader size="xs" />;
      }
      if (error) {
        return <IconAlertCircle size={16} color="var(--mantine-color-red-6)" />;
      }
      return rightSection;
    };

    // Build container classes
    const containerClasses = clsx(
      styles.formGroup,
      {
        [styles.taperedUnderline]: taperedUnderline,
        [styles.focused]: focused && taperedUnderline,
        [styles.error]: error && taperedUnderline,
        [styles.loadingOverlay]: loading,
        [styles.loading]: loading
      }
    );

    // Build input classes
    const inputClasses = clsx(
      styles.enhancedInput,
      {
        [styles.errorInput]: error,
        [styles.wcrFocus]: !error
      },
      className
    );

    if (label) {
      return (
        <Box className={containerClasses}>
          {/* Input container isolates input + label from helper text */}
          <Box className={styles.inputContainer}>
            <Select
              ref={ref}
              data-testid={testId}
              value={value}
              onFocus={handleFocus}
              onBlur={handleBlur}
              rightSection={getRightSection()}
              error={false} // Error styling handled by outer container
              className={inputClasses}
              placeholder={placeholder}
              styles={{
                root: {
                  position: 'relative'
                },
                wrapper: {
                  position: 'relative'
                },
                input: {
                  '&::placeholder': {
                    opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                    visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                    transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
                  },
                  // Remove any conflicting focus styles
                  '&:focus': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  },
                  '&:focus-visible': {
                    outline: 'none !important',
                    boxShadow: 'none !important'
                  }
                }
              }}
              {...props}
              // Remove description and error from here - they go outside
              description={undefined}
              error={undefined}
            />
            <Text
              className={clsx(styles.floatingLabel, {
                [styles.hasValue]: hasValue,
                [styles.isFocused]: focused,
                [styles.isEmpty]: !hasValue && !focused,
                [styles.hasError]: error
              })}
              component="label"
            >
              {label}
            </Text>
          </Box>
          {/* Description and error outside the input container */}
          {props.description && (
            <Text
              size="xs"
              c="dimmed"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {props.description}
            </Text>
          )}
          {error && (
            <Text
              size="xs"
              c="red"
              mt="xs"
              style={{ textAlign: 'left' }}
            >
              {error}
            </Text>
          )}
        </Box>
      );
    }

    return (
      <Box className={containerClasses}>
        <Select
          ref={ref}
          data-testid={testId}
          value={value}
          onFocus={handleFocus}
          onBlur={handleBlur}
          rightSection={getRightSection()}
          error={error}
          className={inputClasses}
          placeholder={placeholder}
          styles={{
            root: {
              display: 'flex',
              flexDirection: 'column'
            },
            label: {
              order: 1,
              marginBottom: 'var(--mantine-spacing-xs)'
            },
            wrapper: {
              order: 2
            },
            input: {
              '&::placeholder': {
                opacity: `${(focused && !hasValue) ? 1 : 0} !important`,
                visibility: (focused && !hasValue) ? 'visible' : 'hidden',
                transition: 'opacity 0.2s ease-in-out, visibility 0.2s ease-in-out'
              },
              // Remove any conflicting focus styles
              '&:focus': {
                outline: 'none !important',
                boxShadow: 'none !important'
              },
              '&:focus-visible': {
                outline: 'none !important',
                boxShadow: 'none !important'
              }
            },
            description: {
              order: 3,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)',
              color: 'var(--mantine-color-gray-6)'
            },
            error: {
              order: 4,
              textAlign: 'left',
              marginTop: 'var(--mantine-spacing-xs)',
              fontSize: 'var(--mantine-font-size-xs)'
            }
          }}
          {...props}
        />
      </Box>
    );
  }
);

EnhancedSelect.displayName = 'EnhancedSelect';

// Export all components
export {
  EnhancedTextInput as MantineTextInput,
  EnhancedPasswordInput as MantinePasswordInput,
  EnhancedTextarea as MantineTextarea,
  EnhancedSelect as MantineSelect
};