// Personal Information step component for vetting application
import React from 'react';
import {
  Stack,
  TextInput,
  Text,
  Group,
  Alert,
  Box
} from '@mantine/core';
import { Controller, UseFormReturn } from 'react-hook-form';
import { IconUser, IconMail, IconPhone, IconInfoCircle, IconLock } from '@tabler/icons-react';
import type { ApplicationFormData } from '../../types/vetting.types';
import { TOUCH_TARGETS } from '../../types/vetting.types';

interface PersonalInfoStepProps {
  form: UseFormReturn<ApplicationFormData>;
}

export const PersonalInfoStep: React.FC<PersonalInfoStepProps> = ({ form }) => {
  const { control, formState: { errors } } = form;

  // Floating label styles (following UI design)
  const floatingLabelStyles = (theme: any) => ({
    input: {
      padding: '16px 12px 6px 12px',
      borderColor: theme.colors.wcr[3],
      borderRadius: 12,
      backgroundColor: theme.colors.wcr[0],
      minHeight: TOUCH_TARGETS.INPUT_HEIGHT,
      fontSize: 16, // Prevents iOS zoom
      '&:focus': {
        borderColor: theme.colors.wcr[7],
        transform: 'translateY(-2px)',
        boxShadow: `0 4px 12px ${theme.colors.wcr[3]}40`
      },
      '&:focus + label, &:not(:placeholder-shown) + label': {
        transform: 'translateY(-24px) scale(0.85)',
        color: theme.colors.wcr[7]
      }
    },
    label: {
      position: 'absolute',
      left: 12,
      top: '50%',
      transform: 'translateY(-50%)',
      transition: 'all 0.3s ease',
      backgroundColor: theme.colors.wcr[0],
      padding: '0 4px',
      color: theme.colors.wcr[8],
      fontFamily: 'Montserrat, sans-serif',
      fontWeight: 500,
      pointerEvents: 'none'
    },
    error: {
      color: theme.colors.red[6],
      fontSize: 12,
      fontFamily: 'Source Sans 3, sans-serif',
      marginTop: 4
    }
  });

  return (
    <Stack gap="xl">
      {/* Section Header */}
      <Box>
        <Text size="xl" fw={700} c="wcr.7" mb="xs">
          Personal Information
        </Text>
        <Text c="dimmed">
          Please provide your basic contact information. This information will be kept confidential 
          and only shared with approved vetting team members.
        </Text>
      </Box>

      {/* Privacy Notice */}
      <Alert 
        icon={<IconLock />}
        color="blue"
        title="Data Privacy"
        styles={{ message: { fontSize: 14 } }}
      >
        All personal information is encrypted and stored securely. You can choose to apply anonymously 
        in the final step if you prefer additional privacy protection.
      </Alert>

      <Stack gap="lg">
        {/* Full Name - Required */}
        <Box pos="relative">
          <Controller
            name="personalInfo.fullName"
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                placeholder=" "
                leftSection={<IconUser size={16} />}
                styles={floatingLabelStyles}
                error={errors.personalInfo?.fullName?.message}
              />
            )}
          />
          <Text
            component="label"
            size="sm"
            style={{
              position: 'absolute',
              left: 40,
              top: '50%',
              transform: 'translateY(-50%)',
              transition: 'all 0.3s ease',
              backgroundColor: '#FAF6F2',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Full Name *
          </Text>
        </Box>

        {/* Scene Name - Required */}
        <Box pos="relative">
          <Controller
            name="personalInfo.sceneName"
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                placeholder=" "
                leftSection={<IconUser size={16} />}
                styles={floatingLabelStyles}
                error={errors.personalInfo?.sceneName?.message}
                description="The name you use in the rope bondage community"
              />
            )}
          />
          <Text
            component="label"
            size="sm"
            style={{
              position: 'absolute',
              left: 40,
              top: '50%',
              transform: 'translateY(-50%)',
              transition: 'all 0.3s ease',
              backgroundColor: '#FAF6F2',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Scene Name *
          </Text>
        </Box>

        {/* Pronouns - Optional */}
        <Box pos="relative">
          <Controller
            name="personalInfo.pronouns"
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                placeholder=" "
                styles={floatingLabelStyles}
                description="Optional - helps us address you respectfully"
              />
            )}
          />
          <Text
            component="label"
            size="sm"
            style={{
              position: 'absolute',
              left: 12,
              top: '50%',
              transform: 'translateY(-50%)',
              transition: 'all 0.3s ease',
              backgroundColor: '#FAF6F2',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Pronouns (optional)
          </Text>
        </Box>

        {/* Email - Required */}
        <Box pos="relative">
          <Controller
            name="personalInfo.email"
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                type="email"
                placeholder=" "
                leftSection={<IconMail size={16} />}
                styles={floatingLabelStyles}
                error={errors.personalInfo?.email?.message}
                description="Used for all vetting communication and status updates"
              />
            )}
          />
          <Text
            component="label"
            size="sm"
            style={{
              position: 'absolute',
              left: 40,
              top: '50%',
              transform: 'translateY(-50%)',
              transition: 'all 0.3s ease',
              backgroundColor: '#FAF6F2',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Email Address *
          </Text>
        </Box>

        {/* Phone - Optional */}
        <Box pos="relative">
          <Controller
            name="personalInfo.phone"
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                type="tel"
                placeholder=" "
                leftSection={<IconPhone size={16} />}
                styles={floatingLabelStyles}
                description="Optional - for urgent communications or interview scheduling"
              />
            )}
          />
          <Text
            component="label"
            size="sm"
            style={{
              position: 'absolute',
              left: 40,
              top: '50%',
              transform: 'translateY(-50%)',
              transition: 'all 0.3s ease',
              backgroundColor: '#FAF6F2',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Phone Number (optional)
          </Text>
        </Box>
      </Stack>

      {/* Information Notice */}
      <Alert 
        icon={<IconInfoCircle />}
        color="grape"
        title="Scene Name Requirements"
      >
        <Text size="sm">
          Your scene name should be unique and appropriate for community use. 
          If your preferred name is already taken, we'll work with you to find an alternative.
        </Text>
      </Alert>

      {/* Required Fields Notice */}
      <Group gap="xs" align="center">
        <Text size="sm" c="dimmed">
          Fields marked with
        </Text>
        <Text size="sm" c="red" fw={600}>
          *
        </Text>
        <Text size="sm" c="dimmed">
          are required to continue.
        </Text>
      </Group>
    </Stack>
  );
};