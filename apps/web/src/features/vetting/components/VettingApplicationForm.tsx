// Simplified Vetting Application Form Component
// Based on approved UI mockups with floating labels and streamlined process

import React, { useState, useEffect } from 'react';
import {
  Box,
  Button,
  Checkbox,
  Text,
  Title,
  Alert,
  LoadingOverlay,
  Stack,
  Group,
  Paper,
  List,
  ThemeIcon,
  Anchor,
  SimpleGrid
} from '@mantine/core';
import {
  EnhancedTextInput,
  EnhancedTextarea
} from '../../../components/forms/MantineFormInputs';
import { useForm } from '@mantine/form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconAlertCircle, IconShieldCheck, IconLogin, IconUserPlus } from '@tabler/icons-react';
import { useAuthStore, useUserSceneName } from '../../../stores/authStore';
import { simplifiedVettingApi, getSimplifiedVettingErrorMessage } from '../api/simplifiedVettingApi';
import { simplifiedApplicationSchema, defaultFormValues, fieldValidationMessages } from '../schemas/simplifiedApplicationSchema';
import type {
  SimplifiedApplicationFormData,
  SimplifiedCreateApplicationRequest,
  SimplifiedApplicationStatus
} from '../types/simplified-vetting.types';
import { TOUCH_TARGETS } from '../types/vetting.types';

interface VettingApplicationFormProps {
  onSubmitSuccess?: (applicationId: string, statusUrl: string) => void;
  onSubmitError?: (error: Error) => void;
  className?: string;
}

export const VettingApplicationForm: React.FC<VettingApplicationFormProps> = ({
  onSubmitSuccess,
  onSubmitError,
  className
}) => {
  const queryClient = useQueryClient();
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const userSceneName = useUserSceneName();

  // Check if user already has an application
  const { data: existingApplication, isLoading: isCheckingApplication, error: checkError } = useQuery({
    queryKey: ['vetting', 'my-application'],
    queryFn: simplifiedVettingApi.checkExistingApplication,
    enabled: !!user && isAuthenticated,
    retry: false,
    // Catch 401 errors gracefully - they're expected for non-authenticated users
    throwOnError: (error: any) => {
      // Only throw non-auth errors
      return error?.response?.status !== 401;
    }
  });

  // Check if we have an existing application and pre-populate form
  const hasExistingApp = !!existingApplication;
  const appData = existingApplication as SimplifiedApplicationStatus | null;

  // Form setup with Mantine form validation
  const form = useForm<SimplifiedApplicationFormData>({
    initialValues: hasExistingApp && appData ? {
      firstName: appData.firstName,
      lastName: appData.lastName,
      pronouns: appData.pronouns || '',
      fetLifeHandle: appData.fetLifeHandle || '',
      otherNames: appData.otherNames || '',
      whyJoin: appData.whyJoin,
      experienceWithRope: appData.experienceWithRope,
      agreeToCommunityStandards: appData.agreeToCommunityStandards,
    } : {
      ...defaultFormValues,
      // Remove email and sceneName from form - they're shown at top but not editable
    },
    validate: {
      firstName: (value) => {
        if (!value || value.trim().length < 1) {
          return fieldValidationMessages.firstName.required;
        }
        if (value.length > 50) {
          return fieldValidationMessages.firstName.maxLength;
        }
        return null;
      },
      lastName: (value) => {
        if (!value || value.trim().length < 1) {
          return fieldValidationMessages.lastName.required;
        }
        if (value.length > 50) {
          return fieldValidationMessages.lastName.maxLength;
        }
        return null;
      },
      pronouns: (value) => {
        if (value && value.length > 50) {
          return fieldValidationMessages.pronouns.maxLength;
        }
        return null;
      },
      fetLifeHandle: (value) => {
        if (value && value.length > 50) {
          return fieldValidationMessages.fetLifeHandle.maxLength;
        }
        return null;
      },
      otherNames: (value) => {
        if (value && value.length > 500) {
          return fieldValidationMessages.otherNames.maxLength;
        }
        return null;
      },
      whyJoin: (value) => {
        if (!value || value.trim().length === 0) {
          return fieldValidationMessages.whyJoin.required;
        }
        if (value.length > 2000) {
          return fieldValidationMessages.whyJoin.maxLength;
        }
        return null;
      },
      experienceWithRope: (value) => {
        if (!value || value.trim().length === 0) {
          return fieldValidationMessages.experienceWithRope.required;
        }
        if (value.length > 2000) {
          return fieldValidationMessages.experienceWithRope.maxLength;
        }
        return null;
      },
      agreeToCommunityStandards: (value) => {
        if (value !== true) {
          return fieldValidationMessages.agreeToCommunityStandards.required;
        }
        return null;
      },
    },
    // Enable validation on value change for better UX
    validateInputOnChange: true,
    validateInputOnBlur: true,
  });

  // Update form values when existing application data becomes available
  useEffect(() => {
    if (hasExistingApp && appData) {
      form.setValues({
        firstName: appData.firstName,
        lastName: appData.lastName,
        pronouns: appData.pronouns || '',
        fetLifeHandle: appData.fetLifeHandle || '',
        otherNames: appData.otherNames || '',
        whyJoin: appData.whyJoin,
        experienceWithRope: appData.experienceWithRope,
        agreeToCommunityStandards: appData.agreeToCommunityStandards,
      });
    }
  }, [hasExistingApp, appData]); // eslint-disable-line react-hooks/exhaustive-deps

  // Submit application mutation
  const submitMutation = useMutation({
    mutationFn: async (formData: SimplifiedApplicationFormData): Promise<any> => {
      // Check authentication before submitting
      if (!isAuthenticated || !user) {
        throw new Error('You must be logged in to submit an application. Please login or create an account first.');
      }

      const request: SimplifiedCreateApplicationRequest = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        pronouns: formData.pronouns || undefined,
        preferredSceneName: user?.sceneName || userSceneName, // Get from auth context
        fetLifeHandle: formData.fetLifeHandle || undefined,
        otherNames: formData.otherNames || undefined,
        email: user?.email || '', // Get from auth context
        whyJoin: formData.whyJoin,
        experienceWithRope: formData.experienceWithRope,
        agreeToCommunityStandards: formData.agreeToCommunityStandards, // Match backend model
      };

      return simplifiedVettingApi.submitApplication(request);
    },
    onSuccess: (response) => {
      // Invalidate dashboard and vetting queries to refresh status
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['vetting', 'my-application'] });

      // Show success notification
      notifications.show({
        title: 'Application Submitted Successfully!',
        message: 'Check your email for confirmation.',
        color: 'green',
        autoClose: 10000,
        icon: <IconCheck />,
      });

      // Trigger parent callback to show success screen
      onSubmitSuccess?.(response.applicationId, response.statusPageUrl);
    },
    onError: (error) => {
      const errorMessage = getSimplifiedVettingErrorMessage(error);

      notifications.show({
        title: 'Submission Failed',
        message: errorMessage,
        color: 'red',
        autoClose: 8000,
        icon: <IconAlertCircle />,
      });

      onSubmitError?.(error as Error);
    },
  });

  // Handle form submission
  const handleFormSubmit = (values: SimplifiedApplicationFormData) => {
    console.log('VettingApplicationForm: Form submitted with values:', {
      ...values,
      hasUser: !!user,
      userEmail: user?.email,
      userSceneName: user?.sceneName || userSceneName,
      isAuthenticated
    });
    submitMutation.mutate(values);
  };

  // Loading state while checking for existing application
  if (isCheckingApplication) {
    return (
      <Box className={className} pos="relative">
        <LoadingOverlay visible={true} />
        <Paper p="xl" shadow="sm">
          <Text>Checking application status...</Text>
        </Paper>
      </Box>
    );
  }

  // Show authentication requirement prominently if user is not logged in
  if (!isAuthenticated || !user) {
    return (
      <Box className={className}>
        <Paper p="xl" shadow="sm">
          <Stack gap="lg">
            <Title order={2} size="h2" mb="sm" c="wcr.7">
              Apply to Join Witch City Rope
            </Title>

            <Alert
              color="blue"
              icon={<IconLogin />}
              title="Login Required"
              data-testid="login-required-alert"
            >
              <Stack gap="md">
                <Text>
                  You must have an account and be logged in to submit a vetting application.
                  This helps us process applications efficiently and securely.
                </Text>

                <Stack gap="md" align="flex-start">
                  <Group gap="md" align="center">
                    <Button
                      component="a"
                      href={`/login?returnUrl=${encodeURIComponent(window.location.pathname)}`}
                      color="wcr"
                      leftSection={<IconLogin />}
                      data-testid="login-to-account-button"
                      style={{
                        minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                        paddingTop: 12,
                        paddingBottom: 12,
                        paddingLeft: 24,
                        paddingRight: 24,
                        fontSize: 16,
                        fontWeight: 600,
                        lineHeight: 1.4,
                      }}
                    >
                      Login to Your Account
                    </Button>

                    <Title order={2} size="h2" c="wcr.7">
                      OR
                    </Title>

                    <Button
                      component="a"
                      href="/register"
                      variant="outline"
                      color="wcr"
                      leftSection={<IconUserPlus />}
                      data-testid="create-account-button"
                      style={{
                        minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                        paddingTop: 12,
                        paddingBottom: 12,
                        paddingLeft: 24,
                        paddingRight: 24,
                        fontSize: 16,
                        fontWeight: 600,
                        lineHeight: 1.4,
                      }}
                    >
                      Create an Account
                    </Button>
                  </Group>
                </Stack>
              </Stack>
            </Alert>

            {/* Privacy Notice */}
            <Alert
              icon={<IconShieldCheck />}
              color="gray"
              title="Privacy & Data Protection"
            >
              All personal information is encrypted and only accessible to approved vetting team members.
              Your data will never be shared outside the review process.
            </Alert>

            {/* Preview of what the form covers */}
            <Box>
              <Title order={4} mb="sm">What we'll ask you:</Title>
              <List size="sm" spacing="xs">
                <List.Item>Your first and last name</List.Item>
                <List.Item>Your pronouns (optional)</List.Item>
                <List.Item>Your FetLife handle (optional)</List.Item>
                <List.Item>Any other names or handles (optional)</List.Item>
                <List.Item>Why you'd like to join our community</List.Item>
                <List.Item>Your experience with rope bondage or BDSM</List.Item>
                <List.Item>Agreement to our community standards</List.Item>
              </List>
              <Text size="xs" c="dimmed" mt="sm">
                Your email and scene name will be pulled from your account profile.
              </Text>
            </Box>
          </Stack>
        </Paper>
      </Box>
    );
  }

  return (
    <Box className={className} pos="relative">
      <LoadingOverlay visible={submitMutation.isPending} />

      <Paper p="xl" shadow="sm">
        <Stack gap="lg">
          {/* Header */}
          <div>
            <Title order={2} size="h2" mb="sm" c="wcr.7">
              Apply to Join Witch City Rope
            </Title>
            <Text c="dimmed" size="lg">
              Please fill out this application thoughtfully. We review all applications within 1-2 weeks.
            </Text>
          </div>

          {/* Privacy Notice */}
          <Alert
            icon={<IconShieldCheck />}
            color="blue"
            title="Privacy & Data Protection"
            style={{ marginBottom: '12px' }}
          >
            All personal information is encrypted and only accessible to approved vetting team members.
            Your data will never be shared outside the review process.
          </Alert>

          <form onSubmit={form.onSubmit(handleFormSubmit)} data-testid="vetting-application-form">
            <Stack gap="lg">
              {/* Scene Name and Email in 2-column grid (read-only) */}
              <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg">
                <EnhancedTextInput
                  label="Scene Name"
                  value={(hasExistingApp && appData) ? appData.preferredSceneName : (userSceneName || user?.sceneName || 'Not set')}
                  readOnly
                  disabled
                  data-testid="scene-name-input"
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: 'var(--mantine-color-gray-0)',
                      cursor: 'not-allowed',
                    },
                  }}
                />

                <EnhancedTextInput
                  label="Email"
                  value={(hasExistingApp && appData) ? appData.email : (user?.email || '')}
                  readOnly
                  disabled
                  data-testid="email-input"
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: 'var(--mantine-color-gray-0)',
                      cursor: 'not-allowed',
                    },
                  }}
                />
              </SimpleGrid>

              {/* First four fields in 2-column grid (responsive: 1 column on mobile/tablet, 2 columns on desktop) */}
              <SimpleGrid cols={{ base: 1, md: 2 }} spacing="lg" style={{ '--sg-spacing-y': 'var(--mantine-spacing-sm)' } as React.CSSProperties}>
                {/* First Name */}
                <EnhancedTextInput
                  label="First Name"
                  placeholder="Enter your first name"
                  required
                  description={fieldValidationMessages.firstName.required}
                  data-testid="first-name-input"
                  readOnly={hasExistingApp}
                  disabled={hasExistingApp}
                  {...form.getInputProps('firstName')}
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                      cursor: hasExistingApp ? 'not-allowed' : undefined,
                    },
                  }}
                />

                {/* Last Name */}
                <EnhancedTextInput
                  label="Last Name"
                  placeholder="Enter your last name"
                  required
                  description={fieldValidationMessages.lastName.required}
                  data-testid="last-name-input"
                  readOnly={hasExistingApp}
                  disabled={hasExistingApp}
                  {...form.getInputProps('lastName')}
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                      cursor: hasExistingApp ? 'not-allowed' : undefined,
                    },
                  }}
                />

                {/* Pronouns */}
                <EnhancedTextInput
                  label="Pronouns"
                  placeholder="Enter your pronouns (optional)"
                  description={fieldValidationMessages.pronouns.optional}
                  data-testid="pronouns-input"
                  readOnly={hasExistingApp}
                  disabled={hasExistingApp}
                  {...form.getInputProps('pronouns')}
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                      cursor: hasExistingApp ? 'not-allowed' : undefined,
                    },
                  }}
                />

                {/* FetLife Handle */}
                <EnhancedTextInput
                  label="FetLife Handle"
                  placeholder="Enter your FetLife handle (optional)"
                  description={fieldValidationMessages.fetLifeHandle.optional}
                  data-testid="fetlife-handle-input"
                  readOnly={hasExistingApp}
                  disabled={hasExistingApp}
                  {...form.getInputProps('fetLifeHandle')}
                  styles={{
                    input: {
                      height: 56,
                      fontSize: 16,
                      backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                      cursor: hasExistingApp ? 'not-allowed' : undefined,
                    },
                  }}
                />
              </SimpleGrid>

              {/* Other Names */}
              <EnhancedTextarea
                label="Other Names or Handles"
                placeholder="List any other names, nicknames, or social media handles (optional)"
                description={fieldValidationMessages.otherNames.optional}
                minRows={2}
                maxRows={4}
                autosize
                data-testid="other-names-textarea"
                readOnly={hasExistingApp}
                disabled={hasExistingApp}
                {...form.getInputProps('otherNames')}
                styles={{
                  input: {
                    fontSize: 16,
                    backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                    cursor: hasExistingApp ? 'not-allowed' : undefined,
                  },
                }}
              />


              {/* Why Join - New required field */}
              <EnhancedTextarea
                label="Why would you like to join Witch City Rope"
                placeholder={fieldValidationMessages.whyJoin.placeholder}
                required
                minRows={4}
                maxRows={8}
                autosize
                description="Tell us why you would like to join our community"
                data-testid="why-join-textarea"
                readOnly={hasExistingApp}
                disabled={hasExistingApp}
                {...form.getInputProps('whyJoin')}
                styles={{
                  input: {
                    fontSize: 16,
                    backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                    cursor: hasExistingApp ? 'not-allowed' : undefined,
                  },
                }}
              />

              {/* Experience with Rope */}
              <EnhancedTextarea
                label="Experience with Rope"
                placeholder={fieldValidationMessages.experienceWithRope.placeholder}
                required
                minRows={4}
                maxRows={8}
                autosize
                description="Share your experience in rope bondage, BDSM, or kink communities"
                data-testid="experience-with-rope-textarea"
                readOnly={hasExistingApp}
                disabled={hasExistingApp}
                {...form.getInputProps('experienceWithRope')}
                styles={{
                  input: {
                    fontSize: 16,
                    backgroundColor: hasExistingApp ? 'var(--mantine-color-gray-0)' : undefined,
                    cursor: hasExistingApp ? 'not-allowed' : undefined,
                  },
                }}
              />

              {/* Community Standards Agreement */}
              <Paper
                p="md"
                style={{
                  backgroundColor: 'var(--mantine-color-gray-0)',
                  border: '2px solid var(--mantine-color-gray-3)',
                  borderRadius: 12,
                }}
              >
                <Stack gap="xs" style={{ '--stack-gap': '0px' } as React.CSSProperties}>
                  {/* Title and intro text stacked vertically */}
                  <Title order={4} c="wcr.7">Community Standards Agreement</Title>
                  <Text size="sm" mb="xs">
                    By submitting this application, you acknowledge that:
                  </Text>

                  {/* Bulleted list in 2-column layout */}
                  <SimpleGrid cols={{ base: 1, md: 2 }} spacing="xs">
                    <List size="sm" c="dimmed">
                      <List.Item>You are at least 21 years old</List.Item>
                      <List.Item>You are comfortable with nudity, sex, and BDSM being discussed</List.Item>
                      <List.Item>You understand the importance of consent and boundaries</List.Item>
                    </List>
                    <List size="sm" c="dimmed">
                      <List.Item>You will conduct yourself respectfully</List.Item>
                      <List.Item>
                        You agree to follow our{' '}
                        <Anchor href="/code-of-conduct" target="_blank" rel="noopener noreferrer">
                          Code of Conduct
                        </Anchor>
                      </List.Item>
                    </List>
                  </SimpleGrid>

                  <div style={{ '--sg-spacing-y': 'var(--mantine-spacing-sm)', marginTop: '10px' } as React.CSSProperties}>
                    <Checkbox
                      label={<Text fw={600}>I agree to all of the above items</Text>}
                      required
                      data-testid="community-standards-checkbox"
                      disabled={hasExistingApp}
                      {...form.getInputProps('agreeToCommunityStandards', { type: 'checkbox' })}
                      styles={{
                        label: {
                          fontFamily: 'var(--font-heading)',
                          color: 'var(--color-wcr-7)',
                          cursor: hasExistingApp ? 'not-allowed' : undefined,
                        },
                        input: {
                          accentColor: 'var(--color-wcr-7)',
                          cursor: hasExistingApp ? 'not-allowed' : undefined,
                        },
                      }}
                    />
                  </div>
                </Stack>
              </Paper>

              {/* Submit Button */}
              <Group justify="flex-end" mt="xl">
                <Button
                  type="submit"
                  size="lg"
                  loading={submitMutation.isPending}
                  disabled={
                    hasExistingApp ||
                    Object.keys(form.errors).length > 0 ||
                    !isAuthenticated ||
                    !form.values.firstName?.trim() ||
                    !form.values.lastName?.trim() ||
                    !form.values.whyJoin?.trim() ||
                    !form.values.experienceWithRope?.trim() ||
                    form.values.agreeToCommunityStandards !== true
                  }
                  onClick={() => {
                    console.log('VettingApplicationForm: Submit button clicked', {
                      hasErrors: Object.keys(form.errors).length > 0,
                      errors: form.errors,
                      isAuthenticated,
                      formValues: form.values,
                      checkboxValue: form.values.agreeToCommunityStandards,
                      isPending: submitMutation.isPending
                    });
                  }}
                  leftSection={<IconCheck />}
                  color="wcr"
                  data-testid="submit-application-button"
                  style={{
                    minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                    paddingTop: 12,
                    paddingBottom: 12,
                    paddingLeft: 32,
                    paddingRight: 32,
                    fontSize: 16,
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    lineHeight: 1.4,
                  }}
                  title={!isAuthenticated ? 'You must be logged in to submit an application' : undefined}
                >
                  {submitMutation.isPending ? 'Submitting Application...' : 'Submit Application'}
                </Button>
              </Group>
            </Stack>
          </form>
        </Stack>
      </Paper>
    </Box>
  );
};