import React, { useState } from 'react';
import {
  Container,
  Title,
  Stack,
  Group,
  Switch,
  Button,
  Paper,
  Text,
  Divider,
  Badge,
  Box,
  SimpleGrid,
  ActionIcon
} from '@mantine/core';
import { IconSun, IconMoon } from '@tabler/icons-react';
import { useMantineColorScheme } from '@mantine/core';
import { useForm } from '@mantine/form';
import {
  MantineTextInput,
  MantinePasswordInput,
  MantineTextarea,
  MantineSelect
} from '../components/forms/MantineFormInputs';

interface FormData {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  bio: string;
  country: string;
  experienceLevel: string;
}

const COUNTRY_OPTIONS = [
  { value: 'us', label: 'United States' },
  { value: 'ca', label: 'Canada' },
  { value: 'uk', label: 'United Kingdom' },
  { value: 'au', label: 'Australia' },
  { value: 'de', label: 'Germany' },
  { value: 'fr', label: 'France' },
  { value: 'jp', label: 'Japan' },
  { value: 'other', label: 'Other' }
];

const EXPERIENCE_OPTIONS = [
  { value: 'beginner', label: 'Beginner (0-1 years)' },
  { value: 'intermediate', label: 'Intermediate (1-3 years)' },
  { value: 'advanced', label: 'Advanced (3-5 years)' },
  { value: 'expert', label: 'Expert (5+ years)' }
];

export const MantineFormTest: React.FC = () => {
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const [taperedUnderlines, setTaperedUnderlines] = useState(false);
  const [showPasswordStrength, setShowPasswordStrength] = useState(true);
  const [simulateLoading, setSimulateLoading] = useState(false);
  const [simulateErrors, setSimulateErrors] = useState(false);
  const [showPlaceholders, setShowPlaceholders] = useState(true);

  const form = useForm<FormData>({
    initialValues: {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      confirmPassword: '',
      bio: '',
      country: '',
      experienceLevel: ''
    },
    validate: {
      firstName: (value) => (!value ? 'First name is required' : null),
      lastName: (value) => (!value ? 'Last name is required' : null),
      email: (value) => {
        if (!value) return 'Email is required';
        if (!/^\S+@\S+$/.test(value)) return 'Invalid email format';
        return null;
      },
      password: (value) => {
        if (!value) return 'Password is required';
        if (value.length < 8) return 'Password must be at least 8 characters';
        return null;
      },
      confirmPassword: (value, values) => {
        if (!value) return 'Please confirm your password';
        if (value !== values.password) return 'Passwords do not match';
        return null;
      },
      bio: (value) => {
        if (value && value.length > 500) return 'Bio must be less than 500 characters';
        return null;
      },
      country: (value) => (!value ? 'Please select your country' : null),
      experienceLevel: (value) => (!value ? 'Please select your experience level' : null)
    }
  });

  const handleSubmit = (values: FormData) => {
    console.log('Form submitted:', values);
    // In a real app, you would send this to your API
  };

  const getError = (field: keyof FormData) => {
    if (!simulateErrors) return form.errors[field];
    // Simulate some errors for testing
    const errorMap: Partial<Record<keyof FormData, string>> = {
      firstName: 'This field has an error',
      email: 'This email is already taken',
      password: 'Password is too weak'
    };
    return form.errors[field] || errorMap[field];
  };

  return (
    <Container size="lg" py="xl">
      <Stack gap="xl">
        <Group justify="space-between" align="center">
          <Box>
            <Title order={1} mb="md">
              Mantine v7 Form Components Demo
            </Title>
            <Text c="dimmed" size="lg">
              Enhanced Mantine form components with floating labels as standard and consistent styling
            </Text>
          </Box>
          <ActionIcon
            onClick={() => toggleColorScheme()}
            variant="default"
            size="xl"
            aria-label="Toggle color scheme"
            data-testid="color-scheme-toggle"
          >
            {colorScheme === 'dark' ? (
              <IconSun size={24} />
            ) : (
              <IconMoon size={24} />
            )}
          </ActionIcon>
        </Group>

        {/* Test Controls */}
        <Paper p="md" radius="md" withBorder>
          <Title order={3} mb="md">
            Test Controls
          </Title>
          <SimpleGrid cols={{ base: 1, sm: 2, md: 3, lg: 4 }} spacing="md">
            <Switch
              label="Tapered Underlines"
              checked={taperedUnderlines}
              onChange={(event) => setTaperedUnderlines(event.currentTarget.checked)}
              data-testid="tapered-underlines-switch"
            />
            <Switch
              label="Password Strength"
              checked={showPasswordStrength}
              onChange={(event) => setShowPasswordStrength(event.currentTarget.checked)}
              data-testid="password-strength-switch"
            />
            <Switch
              label="Simulate Loading"
              checked={simulateLoading}
              onChange={(event) => setSimulateLoading(event.currentTarget.checked)}
              data-testid="simulate-loading-switch"
            />
            <Stack gap="xs">
              <Switch
                label="Dark Mode"
                checked={colorScheme === 'dark'}
                onChange={() => toggleColorScheme()}
                data-testid="dark-mode-switch"
              />
              <Text size="xs" c="dimmed">
                Currently: {colorScheme === 'dark' ? 'Dark' : 'Light'}
              </Text>
            </Stack>
          </SimpleGrid>
          <Group mt="md" gap="md">
            <Switch
              label="Show Placeholders (per-field on focus)"
              checked={showPlaceholders}
              onChange={(event) => setShowPlaceholders(event.currentTarget.checked)}
              data-testid="show-placeholders-switch"
            />
            <Switch
              label="Simulate Errors"
              checked={simulateErrors}
              onChange={(event) => setSimulateErrors(event.currentTarget.checked)}
              data-testid="simulate-errors-switch"
            />
            <Button
              variant="outline"
              onClick={() => form.reset()}
              data-testid="reset-form-button"
            >
              Reset Form
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                form.setValues({
                  firstName: 'Jane',
                  lastName: 'Doe',
                  email: 'jane.doe@example.com',
                  password: 'SecurePass123!',
                  confirmPassword: 'SecurePass123!',
                  bio: 'I am passionate about rope bondage and looking to learn more techniques.',
                  country: 'us',
                  experienceLevel: 'intermediate'
                });
              }}
              data-testid="populate-form-button"
            >
              Populate Form
            </Button>
          </Group>
        </Paper>

        {/* Form Demo */}
        <Paper p="xl" radius="md" withBorder>
          <form onSubmit={form.onSubmit(handleSubmit)}>
            <Stack gap="lg">
              <Title order={2} mb="md">
                Registration Form Demo
              </Title>

              {/* Basic Text Inputs */}
              <Group grow align="flex-start">
                <MantineTextInput
                  label="First Name"
                  placeholder={showPlaceholders ? "Enter your first name" : undefined}
                  taperedUnderline={taperedUnderlines}
                  loading={simulateLoading}
                  error={getError('firstName')}
                  data-testid="first-name-input"
                  {...form.getInputProps('firstName')}
                />
                <MantineTextInput
                  label="Last Name"
                  placeholder={showPlaceholders ? "Enter your last name" : undefined}
                  taperedUnderline={taperedUnderlines}
                  loading={simulateLoading}
                  error={getError('lastName')}
                  data-testid="last-name-input"
                  {...form.getInputProps('lastName')}
                />
              </Group>

              {/* Email Input */}
              <MantineTextInput
                label="Email Address"
                placeholder={showPlaceholders ? "Enter your email" : undefined}
                type="email"
                taperedUnderline={taperedUnderlines}
                loading={simulateLoading}
                asyncValidation={simulateLoading}
                error={getError('email')}
                data-testid="email-input"
                description="We'll use this to send you important updates"
                {...form.getInputProps('email')}
              />

              {/* Password Inputs */}
              <Group grow align="flex-start">
                <MantinePasswordInput
                  label="Password"
                  placeholder={showPlaceholders ? "Create a secure password" : undefined}
                  taperedUnderline={taperedUnderlines}
                  showStrengthMeter={showPasswordStrength}
                  error={getError('password')}
                  data-testid="password-input"
                  description="Must be at least 8 characters with mixed case, numbers, and symbols"
                  {...form.getInputProps('password')}
                />
                <MantinePasswordInput
                  label="Confirm Password"
                  placeholder={showPlaceholders ? "Confirm your password" : undefined}
                  taperedUnderline={taperedUnderlines}
                  showStrengthMeter={false}
                  error={getError('confirmPassword')}
                  data-testid="confirm-password-input"
                  {...form.getInputProps('confirmPassword')}
                />
              </Group>

              {/* Textarea */}
              <MantineTextarea
                label="Bio"
                placeholder={showPlaceholders ? "Tell us about yourself and your interests" : undefined}
                taperedUnderline={taperedUnderlines}
                loading={simulateLoading}
                error={getError('bio')}
                rows={4}
                maxLength={500}
                data-testid="bio-textarea"
                description="Optional: Share your experience and interests (max 500 characters)"
                {...form.getInputProps('bio')}
              />

              {/* Select Inputs */}
              <Group grow align="flex-start">
                <MantineSelect
                  label="Country"
                  placeholder={showPlaceholders ? "Select your country" : undefined}
                  data={COUNTRY_OPTIONS}
                  taperedUnderline={taperedUnderlines}
                  loading={simulateLoading}
                  error={getError('country')}
                  searchable
                  data-testid="country-select"
                  {...form.getInputProps('country')}
                />
                <MantineSelect
                  label="Experience Level"
                  placeholder={showPlaceholders ? "Select your experience" : undefined}
                  data={EXPERIENCE_OPTIONS}
                  taperedUnderline={taperedUnderlines}
                  loading={simulateLoading}
                  error={getError('experienceLevel')}
                  data-testid="experience-select"
                  {...form.getInputProps('experienceLevel')}
                />
              </Group>

              <Divider my="md" />

              {/* Feature Showcase */}
              <Box>
                <Title order={4} mb="md">
                  Feature Showcase
                </Title>
                <Group gap="md" mb="md">
                  <Badge color="blue" variant="light">
                    Native Mantine Components
                  </Badge>
                  <Badge color="green" variant="light">
                    CSS Modules Integration
                  </Badge>
                  <Badge color="purple" variant="light">
                    Dark Theme Support
                  </Badge>
                  <Badge color="teal" variant="light">
                    Enhanced Error States
                  </Badge>
                  <Badge color="orange" variant="light">
                    Floating Labels Standard
                  </Badge>
                  <Badge color="red" variant="light">
                    Enhanced Validation States
                  </Badge>
                </Group>
                <Text size="sm" c="dimmed">
                  • Uses Mantine's built-in props like label, description, error<br />
                  • Centralized CSS module classes for consistent styling<br />
                  • Floating labels as the default and only behavior for all inputs<br />
                  • Tapered underline effect as optional enhancement<br />
                  • Underline colors match input border colors for consistency<br />
                  • Mantine's CSS variables for theming<br />
                  • Built-in dark theme support with proper contrast<br />
                  • Password strength meter with customizable requirements<br />
                  • Loading states with visual feedback<br />
                  • Enhanced validation states with improved error styling<br />
                  • Properly positioned helper text and error messages<br />
                  • Professional error state design with background colors<br />
                  • Per-field placeholder visibility on focus (not global)
                </Text>
              </Box>

              {/* Submit Button */}
              <Group justify="flex-end" mt="xl">
                <Button
                  type="submit"
                  size="md"
                  loading={simulateLoading}
                  data-testid="submit-button"
                >
                  Register Account
                </Button>
              </Group>
            </Stack>
          </form>
        </Paper>

        {/* Form State Display */}
        <Paper p="md" radius="md" withBorder>
          <Title order={4} mb="md">
            Form State (for debugging)
          </Title>
          <Text component="pre" size="xs" style={{ overflow: 'auto' }}>
            {JSON.stringify({ values: form.values, errors: form.errors }, null, 2)}
          </Text>
        </Paper>
      </Stack>
    </Container>
  );
};

export default MantineFormTest;