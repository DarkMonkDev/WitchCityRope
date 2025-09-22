// Simplified Vetting Application Form Component
// Based on approved UI mockups with floating labels and streamlined process

import React, { useEffect, useState } from 'react';
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
  Textarea,
  TextInput,
  Paper,
  List,
  ThemeIcon,
} from '@mantine/core';
import { useForm, zodResolver } from '@mantine/form';
import { useMutation, useQuery } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconAlertCircle, IconShieldCheck } from '@tabler/icons-react';
import { useAuthStore } from '../../../stores/authStore';
import { simplifiedVettingApi, getSimplifiedVettingErrorMessage } from '../api/simplifiedVettingApi';
import { simplifiedApplicationSchema, defaultFormValues, fieldValidationMessages } from '../schemas/simplifiedApplicationSchema';
import type {
  SimplifiedApplicationFormData,
  SimplifiedCreateApplicationRequest,
  SimplifiedApplicationStatus
} from '../types/simplified-vetting.types';

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
  const [showSuccess, setShowSuccess] = useState(false);

  // Check if user already has an application
  const { data: existingApplication, isLoading: isCheckingApplication, error: checkError } = useQuery({
    queryKey: ['vetting', 'my-application'],
    queryFn: simplifiedVettingApi.checkExistingApplication,
    enabled: !!user && isAuthenticated,
    retry: false,
  });

  // Form setup with Mantine form + Zod validation
  const form = useForm<SimplifiedApplicationFormData>({
    validate: (values) => {
      const result = simplifiedApplicationSchema.safeParse(values);
      if (result.success) {
        return {};
      }

      const errors: Record<string, string> = {};
      result.error.issues.forEach((issue) => {
        if (issue.path.length > 0) {
          errors[issue.path.join('.')] = issue.message;
        }
      });
      return errors;
    },
    initialValues: {
      ...defaultFormValues,
      email: user?.email || '', // Pre-fill email from auth
    },
  });

  // Update email when user changes
  useEffect(() => {
    if (user?.email && user.email !== form.values.email) {
      form.setFieldValue('email', user.email);
    }
  }, [user?.email, form]);

  // Submit application mutation
  const submitMutation = useMutation({
    mutationFn: async (formData: SimplifiedApplicationFormData): Promise<any> => {
      const request: SimplifiedCreateApplicationRequest = {
        realName: formData.realName,
        sceneName: formData.sceneName,
        fetLifeHandle: formData.fetLifeHandle || undefined,
        email: formData.email,
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

  // Check for authentication error
  if (checkError && (!user || !isAuthenticated)) {
    return (
      <Box className={className}>
        <Alert color="red" icon={<IconAlertCircle />}>
          You must be logged in to submit a vetting application.
        </Alert>
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

          <form onSubmit={handleSubmit}>
            <Stack gap="lg">
              {/* Real Name */}
              <TextInput
                label="Real Name"
                placeholder=" "
                required
                withAsterisk
                description={fieldValidationMessages.realName.required}
                {...form.getInputProps('realName')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    height: 56,
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.wcr[4],
                    backgroundColor: theme.colors.gray[0],
                    '&:focus': {
                      borderColor: theme.colors.wcr[7],
                      backgroundColor: 'white',
                      transform: 'translateY(-2px)',
                      boxShadow: `0 4px 12px ${theme.colors.wcr[4]}30`,
                    },
                  },
                })}
              />

              {/* Scene Name */}
              <TextInput
                label="Preferred Scene Name"
                placeholder=" "
                required
                withAsterisk
                description="This is how you'll be known at events. You can use any name you're comfortable with."
                {...form.getInputProps('sceneName')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    height: 56,
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.wcr[4],
                    backgroundColor: theme.colors.gray[0],
                    '&:focus': {
                      borderColor: theme.colors.wcr[7],
                      backgroundColor: 'white',
                      transform: 'translateY(-2px)',
                      boxShadow: `0 4px 12px ${theme.colors.wcr[4]}30`,
                    },
                  },
                })}
              />

              {/* FetLife Handle */}
              <TextInput
                label="FetLife Handle"
                placeholder=" "
                description={fieldValidationMessages.fetLifeHandle.optional}
                {...form.getInputProps('fetLifeHandle')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    height: 56,
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.wcr[4],
                    backgroundColor: theme.colors.gray[0],
                    '&:focus': {
                      borderColor: theme.colors.wcr[7],
                      backgroundColor: 'white',
                      transform: 'translateY(-2px)',
                      boxShadow: `0 4px 12px ${theme.colors.wcr[4]}30`,
                    },
                  },
                })}
              />

              {/* Email - Pre-filled and readonly */}
              <TextInput
                label="Email Address"
                placeholder=" "
                required
                withAsterisk
                readOnly
                description={fieldValidationMessages.email.readonly}
                {...form.getInputProps('email')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    height: 56,
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.gray[4],
                    backgroundColor: theme.colors.gray[1],
                    color: theme.colors.gray[7],
                  },
                })}
              />

              {/* Why Join - New required field */}
              <Textarea
                label="Why would you like to join Witch City Rope"
                placeholder={fieldValidationMessages.whyJoin.placeholder}
                required
                withAsterisk
                minRows={4}
                maxRows={8}
                autosize
                description="Tell us why you would like to join our community"
                {...form.getInputProps('whyJoin')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.wcr[4],
                    backgroundColor: theme.colors.gray[0],
                    '&:focus': {
                      borderColor: theme.colors.wcr[7],
                      backgroundColor: 'white',
                      transform: 'translateY(-2px)',
                      boxShadow: `0 4px 12px ${theme.colors.wcr[4]}30`,
                    },
                  },
                })}
              />

              {/* Experience with Rope */}
              <Textarea
                label="Experience with Rope"
                placeholder={fieldValidationMessages.experienceWithRope.placeholder}
                required
                withAsterisk
                minRows={4}
                maxRows={8}
                autosize
                description="Share your experience in rope bondage, BDSM, or kink communities"
                {...form.getInputProps('experienceWithRope')}
                styles={(theme) => ({
                  label: {
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 500,
                    color: theme.colors.wcr[7],
                  },
                  input: {
                    fontSize: 16,
                    borderWidth: 2,
                    borderColor: theme.colors.wcr[4],
                    backgroundColor: theme.colors.gray[0],
                    '&:focus': {
                      borderColor: theme.colors.wcr[7],
                      backgroundColor: 'white',
                      transform: 'translateY(-2px)',
                      boxShadow: `0 4px 12px ${theme.colors.wcr[4]}30`,
                    },
                  },
                })}
              />

              {/* Community Standards Agreement */}
              <Paper
                p="md"
                style={(theme) => ({
                  backgroundColor: theme.colors.gray[0],
                  border: `2px solid ${theme.colors.gray[3]}`,
                  borderRadius: 12,
                })}
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
                    {...form.getInputProps('agreesToCommunityStandards', { type: 'checkbox' })}
                    styles={(theme) => ({
                      label: {
                        fontFamily: 'Montserrat, sans-serif',
                        color: theme.colors.wcr[7],
                      },
                      input: {
                        accentColor: theme.colors.wcr[7],
                      },
                    })}
                  />
                </Stack>
              </Paper>

              {/* Submit Button */}
              <Group justify="flex-end" mt="xl">
                <Button
                  type="submit"
                  size="lg"
                  loading={submitMutation.isPending}
                  disabled={!form.isValid() || !form.isDirty()}
                  leftSection={<IconCheck />}
                  style={{
                    height: 56,
                    fontSize: 16,
                    fontFamily: 'Montserrat, sans-serif',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    paddingLeft: 32,
                    paddingRight: 32,
                  }}
                  styles={(theme) => ({
                    root: {
                      background: `linear-gradient(135deg, ${theme.colors.yellow[4]} 0%, ${theme.colors.yellow[6]} 100%)`,
                      color: theme.colors.dark[9],
                      border: 'none',
                      borderRadius: '12px 6px 12px 6px',
                      boxShadow: `0 4px 15px ${theme.colors.yellow[4]}40`,
                      transition: 'all 0.3s ease',
                      '&:hover': {
                        borderRadius: '6px 12px 6px 12px',
                        transform: 'translateY(-2px)',
                        boxShadow: `0 6px 20px ${theme.colors.yellow[4]}60`,
                      },
                    },
                  })}
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