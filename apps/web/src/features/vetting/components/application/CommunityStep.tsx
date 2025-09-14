// Community Understanding step component for vetting application
import React from 'react';
import {
  Stack,
  Textarea,
  MultiSelect,
  Checkbox,
  Text,
  Group,
  Alert,
  Box,
  Paper,
  List,
  ThemeIcon
} from '@mantine/core';
import { Controller, UseFormReturn } from 'react-hook-form';
import { IconHeart, IconUsers, IconTarget, IconCheck, IconInfoCircle, IconAlertTriangle } from '@tabler/icons-react';
import type { ApplicationFormData } from '../../types/vetting.types';
import { SKILLS_INTERESTS_OPTIONS, TOUCH_TARGETS } from '../../types/vetting.types';

interface CommunityStepProps {
  form: UseFormReturn<ApplicationFormData>;
}

export const CommunityStep: React.FC<CommunityStepProps> = ({ form }) => {
  const { control, formState: { errors }, watch } = form;
  
  // Watch fields for dynamic character counts
  const whyJoin = watch('community.whyJoin');
  const expectations = watch('community.expectations');
  const agreesToGuidelines = watch('community.agreesToGuidelines');

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
          Community Understanding
        </Text>
        <Text c="dimmed">
          Help us understand your motivations and what you hope to gain from joining our community.
        </Text>
      </Box>

      <Stack gap="lg">
        {/* Why Join Community - Required */}
        <Controller
          name="community.whyJoin"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              label="Why do you want to join WitchCityRope?"
              placeholder="Share your motivations for joining our community, what draws you to rope bondage, and what you hope to gain from membership..."
              minRows={4}
              maxRows={8}
              leftSection={<IconHeart size={16} />}
              styles={floatingLabelStyles}
              error={errors.community?.whyJoin?.message}
              description={`Minimum 50 characters (${field.value?.length || 0}/400)`}
              maxLength={400}
              required
            />
          )}
        />

        {/* Skills & Interests - Required */}
        <Controller
          name="community.skillsInterests"
          control={control}
          render={({ field }) => (
            <MultiSelect
              {...field}
              label="Skills & Interests"
              placeholder="Select all that apply"
              data={SKILLS_INTERESTS_OPTIONS}
              leftSection={<IconUsers size={16} />}
              styles={floatingLabelStyles}
              error={errors.community?.skillsInterests?.message}
              description="Select areas you're interested in or have experience with"
              searchable
              clearable
              required
            />
          )}
        />

        {/* Expectations & Goals - Required */}
        <Controller
          name="community.expectations"
          control={control}
          render={({ field }) => (
            <Textarea
              {...field}
              label="Expectations & Goals"
              placeholder="What are your expectations for community involvement? What goals do you have for your rope bondage journey?"
              minRows={3}
              maxRows={6}
              leftSection={<IconTarget size={16} />}
              styles={floatingLabelStyles}
              error={errors.community?.expectations?.message}
              description={`Minimum 30 characters (${field.value?.length || 0}/300)`}
              maxLength={300}
              required
            />
          )}
        />

        {/* Community Guidelines Agreement - Required */}
        <Paper p="lg" withBorder bg="gray.0">
          <Text fw={600} size="md" mb="md" c="wcr.7">
            Community Guidelines
          </Text>
          
          <Text size="sm" c="dimmed" mb="md">
            As a member of WitchCityRope, you agree to abide by our community guidelines:
          </Text>
          
          <List
            spacing="xs"
            size="sm"
            icon={
              <ThemeIcon color="wcr.7" size={18} radius="xl">
                <IconCheck size={12} />
              </ThemeIcon>
            }
            mb="md"
          >
            <List.Item>Treat all community members with respect and dignity</List.Item>
            <List.Item>Practice enthusiastic, informed, and ongoing consent</List.Item>
            <List.Item>Prioritize safety in all rope bondage activities</List.Item>
            <List.Item>Respect others' boundaries, privacy, and personal choices</List.Item>
            <List.Item>Support a welcoming and inclusive environment</List.Item>
            <List.Item>Report safety concerns to community leaders</List.Item>
            <List.Item>Participate constructively in community events and discussions</List.Item>
            <List.Item>Respect the confidentiality of community activities and members</List.Item>
          </List>

          <Controller
            name="community.agreesToGuidelines"
            control={control}
            render={({ field }) => (
              <Checkbox
                {...(field as any)}
                checked={field.value}
                label={
                  <Text size="sm" fw={500}>
                    I agree to abide by the WitchCityRope Community Guidelines
                  </Text>
                }
                error={errors.community?.agreesToGuidelines?.message}
                styles={((theme) => ({
                  input: {
                    '&:checked': {
                      backgroundColor: theme.colors.wcr[7],
                      borderColor: theme.colors.wcr[7]
                    }
                  },
                  label: {
                    fontFamily: 'Source Sans 3, sans-serif'
                  }
                })) as any}
                required
              />
            )}
          />
        </Paper>
      </Stack>

      {/* Community Values Notice */}
      <Alert 
        icon={<IconHeart />}
        color="grape"
        title="Our Community Values"
      >
        <Text size="sm">
          WitchCityRope is built on principles of consent, safety, respect, and inclusivity. 
          We welcome people of all backgrounds, experience levels, and rope interests who share these values.
        </Text>
      </Alert>

      {/* Participation Expectations */}
      <Alert 
        icon={<IconInfoCircle />}
        color="blue"
        title="Participation Expectations"
      >
        <Text size="sm">
          While we don't require specific levels of participation, we do expect members to engage 
          respectfully with the community and contribute positively to our shared learning environment.
        </Text>
      </Alert>

      {/* Warning about Guidelines */}
      {!agreesToGuidelines && (
        <Alert 
          icon={<IconAlertTriangle />}
          color="yellow"
          title="Agreement Required"
        >
          <Text size="sm">
            You must agree to our community guidelines to continue with your application.
          </Text>
        </Alert>
      )}

      {/* Required Fields Notice */}
      <Group gap="xs" align="center">
        <Text size="sm" c="dimmed">
          All fields in this section are required to continue.
        </Text>
      </Group>
    </Stack>
  );
};