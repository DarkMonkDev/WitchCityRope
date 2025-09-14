// Experience & Knowledge step component for vetting application
import React from 'react';
import {
  Stack,
  TextInput,
  Textarea,
  Select,
  NumberInput,
  Text,
  Group,
  Alert,
  Box,
  Paper
} from '@mantine/core';
import { Controller, UseFormReturn } from 'react-hook-form';
import { IconTrophy, IconCalendar, IconBook, IconShield, IconHeart, IconInfoCircle } from '@tabler/icons-react';
import type { ApplicationFormData, ExperienceLevel } from '../../types/vetting.types';
import { EXPERIENCE_LEVEL_CONFIGS, TOUCH_TARGETS } from '../../types/vetting.types';

interface ExperienceStepProps {
  form: UseFormReturn<ApplicationFormData>;
}

export const ExperienceStep: React.FC<ExperienceStepProps> = ({ form }) => {
  const { control, formState: { errors }, watch } = form;
  
  // Watch experience level for dynamic descriptions
  const experienceLevel = watch('experience.level');

  // Transform experience level options for Select component
  const experienceLevelOptions = Object.values(EXPERIENCE_LEVEL_CONFIGS).map(config => ({
    value: config.value.toString(),
    label: config.label,
    description: config.description
  }));

  // Floating label styles
  const floatingLabelStyles = (theme: any) => ({
    input: {
      minHeight: TOUCH_TARGETS.INPUT_HEIGHT,
      fontSize: 16,
      borderColor: theme.colors.wcr[3],
      borderRadius: 12,
      '&:focus': {
        borderColor: theme.colors.wcr[7],
        boxShadow: `0 4px 12px ${theme.colors.wcr[3]}40`
      }
    },
    label: {
      fontFamily: 'Montserrat, sans-serif',
      fontWeight: 500,
      color: theme.colors.wcr[8]
    }
  });

  return (
    <Stack gap="xl">
      {/* Section Header */}
      <Box>
        <Text size="xl" fw={700} c="wcr.7" mb="xs">
          Experience & Knowledge
        </Text>
        <Text c="dimmed">
          Help us understand your experience with rope bondage and your knowledge of safety practices.
        </Text>
      </Box>

      <Stack gap="lg">
        {/* Experience Level - Required */}
        <Controller
          name="experience.level"
          control={control}
          render={({ field }) => (
            <Select
              {...field}
              value={field.value?.toString()}
              onChange={(value) => field.onChange(value ? parseInt(value) : 1)}
              label="Experience Level"
              placeholder="Select your experience level"
              data={experienceLevelOptions}
              leftSection={<IconTrophy size={16} />}
              styles={floatingLabelStyles}
              error={errors.experience?.level?.message}
              required
            />
          )}
        />

        {/* Experience Level Description */}
        {experienceLevel && (
          <Paper p="md" bg="blue.0" withBorder>
            <Group gap="xs">
              <IconInfoCircle size={16} color="#4285F4" />
              <Text size="sm" c="blue.7" fw={500}>
                {EXPERIENCE_LEVEL_CONFIGS[experienceLevel as ExperienceLevel]?.description}
              </Text>
            </Group>
          </Paper>
        )}

        {/* Years of Experience - Required */}
        <Controller
          name="experience.yearsExperience"
          control={control}
          render={({ field }) => (
            <NumberInput
              {...field}
              label="Years of Experience"
              placeholder="Enter number of years"
              min={0}
              max={50}
              leftSection={<IconCalendar size={16} />}
              styles={floatingLabelStyles}
              error={errors.experience?.yearsExperience?.message}
              description="How many years have you been involved in rope bondage?"
              required
            />
          )}
        />

        {/* Experience Description - Required */}
        <Controller
          name="experience.description"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              label="Experience Description"
              placeholder="Describe your experience with rope bondage, including techniques you've practiced, events attended, workshops taken, etc..."
              minRows={4}
              maxRows={8}
              leftSection={<IconBook size={16} />}
              styles={floatingLabelStyles}
              error={errors.experience?.description?.message}
              description={`Minimum 50 characters (${field.value?.length || 0}/500)`}
              maxLength={500}
              required
            />
          )}
        />

        {/* Safety Knowledge Assessment - Required */}
        <Controller
          name="experience.safetyKnowledge"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              label="Safety Knowledge"
              placeholder="Describe your understanding of rope bondage safety, including risk awareness, emergency procedures, anatomy considerations, etc..."
              minRows={3}
              maxRows={6}
              leftSection={<IconShield size={16} />}
              styles={floatingLabelStyles}
              error={errors.experience?.safetyKnowledge?.message}
              description={`Minimum 30 characters (${field.value?.length || 0}/300)`}
              maxLength={300}
              required
            />
          )}
        />

        {/* Consent Understanding - Required */}
        <Controller
          name="experience.consentUnderstanding"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              label="Consent & Communication"
              placeholder="Describe your understanding of consent in rope bondage, including negotiation, boundaries, ongoing consent, and communication practices..."
              minRows={3}
              maxRows={6}
              leftSection={<IconHeart size={16} />}
              styles={floatingLabelStyles}
              error={errors.experience?.consentUnderstanding?.message}
              description={`Minimum 30 characters (${field.value?.length || 0}/300)`}
              maxLength={300}
              required
            />
          )}
        />
      </Stack>

      {/* Safety Notice */}
      <Alert 
        icon={<IconShield />}
        color="red"
        title="Safety is Our Priority"
      >
        <Text size="sm">
          Safety and consent are fundamental to our community. Your responses help us understand 
          your commitment to safe practices and ensure a positive environment for all members.
        </Text>
      </Alert>

      {/* Information Notice */}
      <Alert 
        icon={<IconInfoCircle />}
        color="blue"
        title="Honest Assessment"
      >
        <Text size="sm">
          Please be honest about your experience level. There's no shame in being a beginner - 
          we welcome members at all experience levels and provide mentorship and education opportunities.
        </Text>
      </Alert>

      {/* Required Fields Notice */}
      <Group gap="xs" align="center">
        <Text size="sm" c="dimmed">
          All fields in this section are required to continue.
        </Text>
      </Group>
    </Stack>
  );
};