import { forwardRef, useMemo } from 'react';
import { PasswordInput as MantinePasswordInput, PasswordInputProps as MantinePasswordInputProps, Progress, Text, Stack, Group, Box } from '@mantine/core';
import { IconCheck, IconX } from '@tabler/icons-react';

export interface PasswordRequirement {
  re: RegExp;
  label: string;
}

export interface PasswordInputProps extends MantinePasswordInputProps {
  showStrengthMeter?: boolean;
  strengthRequirements?: PasswordRequirement[];
  'data-testid'?: string;
}

// Default password requirements from business rules
const DEFAULT_REQUIREMENTS: PasswordRequirement[] = [
  { re: /.{8,}/, label: 'At least 8 characters' },
  { re: /[a-z]/, label: 'Contains lowercase letter' },
  { re: /[A-Z]/, label: 'Contains uppercase letter' },
  { re: /\d/, label: 'Contains number' },
  { re: /[@$!%*?&]/, label: 'Contains special character (@$!%*?&)' }
];

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ 
    showStrengthMeter = true,
    strengthRequirements = DEFAULT_REQUIREMENTS,
    'data-testid': testId,
    value = '',
    ...props 
  }, ref) => {
    // Calculate password strength
    const { strength, color, requirements } = useMemo(() => {
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

    const passwordInput = (
      <MantinePasswordInput
        ref={ref}
        data-testid={testId}
        value={value}
        {...props}
      />
    );

    // If strength meter is disabled, return just the input
    if (!showStrengthMeter || !value) {
      return passwordInput;
    }

    return (
      <Stack gap="xs">
        {passwordInput}
        
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
              >
                {req.label}
              </Text>
            </Group>
          ))}
        </Stack>
      </Stack>
    );
  }
);

PasswordInput.displayName = 'PasswordInput';