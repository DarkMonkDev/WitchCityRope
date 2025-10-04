// Simplified Vetting Application Form Component
// Based on approved UI mockups with floating labels and streamlined process

import React, { useState } from 'react';
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
  Anchor
} from '@mantine/core';
import {
  EnhancedTextInput,
  EnhancedTextarea
} from '../../../components/forms/MantineFormInputs';
import { useForm, zodResolver } from '@mantine/form';
import { useMutation, useQuery } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconAlertCircle, IconShieldCheck, IconLogin } from '@tabler/icons-react';
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
  const user = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const userSceneName = useUserSceneName();
  const [showSuccess, setShowSuccess] = useState(false);

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

  // Form setup with Mantine form + Zod validation
  const form = useForm<SimplifiedApplicationFormData>({
    validate: zodResolver(simplifiedApplicationSchema as any),
    initialValues: {
      ...defaultFormValues,
      // Remove email and sceneName from form - they're shown at top but not editable
    },
    // Enable validation on value change for better UX
    validateInputOnChange: true,
    validateInputOnBlur: true,
  });

  // No need to update form values since email and sceneName are displayed separately

  // Submit application mutation
  const submitMutation = useMutation({
    mutationFn: async (formData: SimplifiedApplicationFormData): Promise<any> => {
      // Check authentication before submitting
      if (!isAuthenticated || !user) {
        throw new Error('You must be logged in to submit an application. Please login or create an account first.');
      }

      const request: SimplifiedCreateApplicationRequest = {
        realName: formData.realName,
        pronouns: formData.pronouns || undefined,
        sceneName: user?.sceneName || userSceneName, // Get from auth context
        fetLifeHandle: formData.fetLifeHandle || undefined,
        otherNames: formData.otherNames || undefined,
        email: user?.email || '', // Get from auth context
        whyJoin: formData.whyJoin,
        experienceWithRope: formData.experienceWithRope,
        agreesToCommunityStandards: formData.agreesToCommunityStandards,
      };

      return simplifiedVettingApi.submitApplication(request);
    },
    onSuccess: (response) => {
      setShowSuccess(true);

      notifications.show({
        title: 'Application Submitted Successfully!',
        message: `Your application #${response.applicationNumber} has been submitted. Check your email for confirmation.`,
        color: 'green',
        autoClose: 10000,
        icon: <IconCheck />,
      });

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
  const handleSubmit = form.onSubmit((values) => {
    submitMutation.mutate(values);
  });

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

  // Show existing application status if user already applied
  if (existingApplication) {
    const app = existingApplication as SimplifiedApplicationStatus;
    return (
      <Box className={className}>
        <Paper p="xl" shadow="sm">
          <Stack gap="md">
            <Title order={2} size="h3" c="wcr.7">
              Application Already Submitted
            </Title>

            <Alert color="blue" icon={<IconCheck />}>
              You have already submitted a vetting application. Only one application is allowed per person.
            </Alert>

            <Box>
              <Text size="sm" c="dimmed" mb="xs">Application Status:</Text>
              <Text size="lg" fw={600} tt="capitalize">
                {app.status.replace('-', ' ')}
              </Text>
            </Box>

            <Box>
              <Text size="sm" c="dimmed" mb="xs">Submitted:</Text>
              <Text>
                {new Date(app.submittedAt).toLocaleDateString()}
              </Text>
            </Box>

            <Text c="dimmed">
              {app.statusMessage}
            </Text>
          </Stack>
        </Paper>
      </Box>
    );
  }

  // Show success message after submission
  if (showSuccess) {
    return (
      <Box className={className}>
        <Paper p="xl" shadow="sm" ta="center">
          <Stack gap="lg">
            <ThemeIcon size={64} color="green" variant="light" mx="auto">
              <IconCheck size={32} />
            </ThemeIcon>

            <Title order={2} c="green">
              Application Submitted Successfully!
            </Title>

            <Text size="lg">
              Your vetting application has been received and you should receive a confirmation email shortly.
            </Text>

            <Text c="dimmed">
              Our team will review your application and contact you within 1-2 weeks with our decision.
            </Text>
          </Stack>
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

                <Group gap="md">
                  <Button
                    component="a"
                    href="/login"
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

                  <Text size="sm" c="dimmed">
                    Don't have an account?{' '}
                    <Anchor href="/register" fw={600}>
                      Create one here
                    </Anchor>
                  </Text>
                </Group>
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
                <List.Item>Your real name</List.Item>
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
          >
            All personal information is encrypted and only accessible to approved vetting team members.
            Your data will never be shared outside the review process.
          </Alert>

          {/* Display user info at top */}
          <Paper p="md" bg="gray.0" style={{ borderRadius: 8, border: '1px solid var(--mantine-color-gray-3)' }}>
            <Stack gap="xs">
              <Text size="sm" fw={600} c="wcr.7">Your Account Information</Text>
              <Group>
                <Box>
                  <Text size="xs" c="dimmed">Email:</Text>
                  <Text size="sm" fw={500}>{user?.email}</Text>
                </Box>
                <Box>
                  <Text size="xs" c="dimmed">Scene Name:</Text>
                  <Text size="sm" fw={500}>{userSceneName || user?.sceneName || 'Not set'}</Text>
                </Box>
              </Group>
              <Text size="xs" c="dimmed">This information will be used for your application. To update it, please edit your profile.</Text>
            </Stack>
          </Paper>

          <form onSubmit={handleSubmit} data-testid="vetting-application-form">
            <Stack gap="lg">
              {/* Real Name */}
              <EnhancedTextInput
                label="Real Name"
                placeholder="Enter your real name"
                required
                description={fieldValidationMessages.realName.required}
                data-testid="real-name-input"
                {...form.getInputProps('realName')}
                styles={{
                  input: {
                    height: 56,
                    fontSize: 16,
                  },
                }}
              />

              {/* Pronouns */}
              <EnhancedTextInput
                label="Pronouns"
                placeholder="Enter your pronouns (optional)"
                description={fieldValidationMessages.pronouns.optional}
                data-testid="pronouns-input"
                {...form.getInputProps('pronouns')}
                styles={{
                  input: {
                    height: 56,
                    fontSize: 16,
                  },
                }}
              />


              {/* FetLife Handle */}
              <EnhancedTextInput
                label="FetLife Handle"
                placeholder="Enter your FetLife handle (optional)"
                description={fieldValidationMessages.fetLifeHandle.optional}
                data-testid="fetlife-handle-input"
                {...form.getInputProps('fetLifeHandle')}
                styles={{
                  input: {
                    height: 56,
                    fontSize: 16,
                  },
                }}
              />

              {/* Other Names */}
              <EnhancedTextarea
                label="Other Names or Handles"
                placeholder="List any other names, nicknames, or social media handles (optional)"
                description={fieldValidationMessages.otherNames.optional}
                minRows={2}
                maxRows={4}
                autosize
                data-testid="other-names-textarea"
                {...form.getInputProps('otherNames')}
                styles={{
                  input: {
                    fontSize: 16,
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
                {...form.getInputProps('whyJoin')}
                styles={{
                  input: {
                    fontSize: 16,
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
                {...form.getInputProps('experienceWithRope')}
                styles={{
                  input: {
                    fontSize: 16,
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
                <Stack gap="md">
                  <Title order={4} c="wcr.7">Community Standards Agreement</Title>

                  <Text size="sm" mb="xs">
                    By submitting this application, you acknowledge that:
                  </Text>

                  <List size="sm" spacing="xs" c="dimmed">
                    <List.Item>You are at least 21 years old</List.Item>
                    <List.Item>You are comfortable with nudity, sex, and BDSM being discussed</List.Item>
                    <List.Item>You understand the importance of consent and boundaries</List.Item>
                    <List.Item>You will conduct yourself respectfully</List.Item>
                    <List.Item>You agree to follow our Code of Conduct</List.Item>
                  </List>

                  <Checkbox
                    label={<Text fw={600}>I agree to all of the above items</Text>}
                    required
                    data-testid="community-standards-checkbox"
                    {...form.getInputProps('agreesToCommunityStandards', { type: 'checkbox' })}
                    styles={{
                      label: {
                        fontFamily: 'var(--font-heading)',
                        color: 'var(--color-wcr-7)',
                      },
                      input: {
                        accentColor: 'var(--color-wcr-7)',
                      },
                    }}
                  />
                </Stack>
              </Paper>

              {/* Submit Button */}
              <Group justify="flex-end" mt="xl">
                <Button
                  type="submit"
                  size="lg"
                  loading={submitMutation.isPending}
                  disabled={Object.keys(form.errors).length > 0 || !isAuthenticated || !form.values.realName || !form.values.whyJoin || !form.values.experienceWithRope || !form.values.agreesToCommunityStandards}
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