// References step component for vetting application
import React from 'react';
import {
  Stack,
  TextInput,
  Text,
  Group,
  Alert,
  Box,
  Paper,
  Divider,
  SimpleGrid
} from '@mantine/core';
import { Controller, UseFormReturn } from 'react-hook-form';
import { IconUser, IconMail, IconHeart, IconShieldCheck, IconInfoCircle, IconAlertTriangle } from '@tabler/icons-react';
import type { ApplicationFormData } from '../../types/vetting.types';
import { TOUCH_TARGETS } from '../../types/vetting.types';

interface ReferencesStepProps {
  form: UseFormReturn<ApplicationFormData>;
}

export const ReferencesStep: React.FC<ReferencesStepProps> = ({ form }) => {
  const { control, formState: { errors }, watch } = form;
  
  // Watch reference fields to validate they're not the same as applicant email
  const applicantEmail = watch('personalInfo.email');
  const reference1Email = watch('references.reference1.email');
  const reference2Email = watch('references.reference2.email');
  const reference3Email = watch('references.reference3.email');

  // Check for duplicate emails
  const hasDuplicateEmails = () => {
    const emails = [reference1Email, reference2Email, reference3Email, applicantEmail].filter(Boolean);
    return new Set(emails).size !== emails.length;
  };

  // Floating label styles
  const floatingLabelStyles = (theme: any, hasError?: boolean) => ({
    input: {
      padding: '16px 12px 6px 12px',
      borderColor: hasError ? theme.colors.red[6] : theme.colors.wcr[3],
      borderRadius: 12,
      backgroundColor: theme.colors.wcr[0],
      minHeight: TOUCH_TARGETS.INPUT_HEIGHT,
      fontSize: 16,
      '&:focus': {
        borderColor: hasError ? theme.colors.red[6] : theme.colors.wcr[7],
        boxShadow: hasError 
          ? `0 4px 12px ${theme.colors.red[6]}40`
          : `0 4px 12px ${theme.colors.wcr[3]}40`
      },
      '&:focus + label, &:not(:placeholder-shown) + label': {
        transform: 'translateY(-24px) scale(0.85)',
        color: hasError ? theme.colors.red[6] : theme.colors.wcr[7]
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
    }
  });

  const ReferenceCard = ({ 
    referenceKey, 
    title, 
    isRequired = false 
  }: { 
    referenceKey: 'reference1' | 'reference2' | 'reference3';
    title: string;
    isRequired?: boolean;
  }) => (
    <Paper p="lg" withBorder bg={isRequired ? 'blue.0' : 'gray.0'}>
      <Text fw={600} size="md" mb="md" c={isRequired ? 'blue.7' : 'gray.7'}>
        {title} {isRequired && <Text component="span" c="red">*</Text>}
      </Text>
      
      <Stack gap="md">
        {/* Reference Name */}
        <Box pos="relative">
          <Controller
            name={`references.${referenceKey}.name`}
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                placeholder=" "
                leftSection={<IconUser size={16} />}
                styles={(theme) => floatingLabelStyles(theme, !!errors.references?.[referenceKey]?.name)}
                error={errors.references?.[referenceKey]?.name?.message}
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
              backgroundColor: isRequired ? '#E7F5FF' : '#F8F9FA',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Full Name {isRequired && '*'}
          </Text>
        </Box>

        {/* Reference Email */}
        <Box pos="relative">
          <Controller
            name={`references.${referenceKey}.email`}
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                type="email"
                placeholder=" "
                leftSection={<IconMail size={16} />}
                styles={(theme) => floatingLabelStyles(theme, !!errors.references?.[referenceKey]?.email)}
                error={errors.references?.[referenceKey]?.email?.message}
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
              backgroundColor: isRequired ? '#E7F5FF' : '#F8F9FA',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Email Address {isRequired && '*'}
          </Text>
        </Box>

        {/* Relationship Description */}
        <Box pos="relative">
          <Controller
            name={`references.${referenceKey}.relationship`}
            control={control}
            render={({ field }) => (
              <TextInput
                {...field}
                placeholder=" "
                leftSection={<IconHeart size={16} />}
                styles={(theme) => floatingLabelStyles(theme, !!errors.references?.[referenceKey]?.relationship)}
                error={errors.references?.[referenceKey]?.relationship?.message}
                description="e.g., 'Rope partner for 2 years', 'Workshop instructor', 'Community friend'"
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
              backgroundColor: isRequired ? '#E7F5FF' : '#F8F9FA',
              padding: '0 4px',
              color: '#880124',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 500,
              pointerEvents: 'none'
            }}
          >
            Relationship {isRequired && '*'}
          </Text>
        </Box>
      </Stack>
    </Paper>
  );

  return (
    <Stack gap="xl">
      {/* Section Header */}
      <Box>
        <Text size="xl" fw={700} c="wcr.7" mb="xs">
          References
        </Text>
        <Text c="dimmed">
          Please provide contacts who can speak to your character and involvement in the rope bondage community.
        </Text>
      </Box>

      {/* Privacy Notice */}
      <Alert 
        icon={<IconShieldCheck />}
        color="blue"
        title="Reference Privacy Protection"
      >
        <Text size="sm">
          Your references will be contacted professionally via email with information about your application. 
          Their responses remain confidential and are only shared with the vetting team.
        </Text>
      </Alert>

      {/* Reference Guidelines */}
      <Alert 
        icon={<IconInfoCircle />}
        color="grape"
        title="Good Reference Qualities"
      >
        <Text size="sm" mb="xs">
          Ideal references are people who:
        </Text>
        <Text size="sm" component="ul" pl="md">
          <li>Have known you in a rope bondage context for several months or more</li>
          <li>Can speak to your character, safety practices, and community involvement</li>
          <li>Are active in the rope bondage community themselves</li>
          <li>Will respond promptly to our verification email (typically within 1 week)</li>
        </Text>
      </Alert>

      <Stack gap="lg">
        {/* Reference 1 - Required */}
        <ReferenceCard
          referenceKey="reference1"
          title="Primary Reference"
          isRequired
        />

        <Divider />

        {/* Reference 2 - Required */}
        <ReferenceCard
          referenceKey="reference2"
          title="Secondary Reference"
          isRequired
        />

        <Divider />

        {/* Reference 3 - Optional */}
        <ReferenceCard
          referenceKey="reference3"
          title="Additional Reference (Optional)"
        />
      </Stack>

      {/* Validation Warnings */}
      {hasDuplicateEmails() && (
        <Alert 
          icon={<IconAlertTriangle />}
          color="red"
          title="Duplicate Email Addresses"
        >
          <Text size="sm">
            Each reference must have a unique email address, and reference emails cannot match your application email.
          </Text>
        </Alert>
      )}

      {/* Important Notes */}
      <Alert 
        icon={<IconAlertTriangle />}
        color="yellow"
        title="Important Notes"
      >
        <Text size="sm" component="div">
          <Text mb="xs">Before listing someone as a reference, please:</Text>
          <Text component="ul" pl="md">
            <li>Ask for their permission to use them as a reference</li>
            <li>Ensure they will be available to respond within 7-14 days</li>
            <li>Provide them with accurate contact information</li>
            <li>Let them know they'll receive an email from WitchCityRope</li>
          </Text>
        </Text>
      </Alert>

      {/* Required Fields Notice */}
      <Group gap="xs" align="center">
        <Text size="sm" c="dimmed">
          Two references are required to continue. The third reference is optional but recommended.
        </Text>
      </Group>
    </Stack>
  );
};