// Review & Submit step component for vetting application
import React, { useMemo } from 'react';
import {
  Stack,
  Text,
  Group,
  Alert,
  Box,
  Paper,
  Divider,
  Checkbox,
  Switch,
  Badge,
  SimpleGrid,
  List,
  ThemeIcon,
  Accordion
} from '@mantine/core';
import { Controller, UseFormReturn } from 'react-hook-form';
import { 
  IconCheck, 
  IconShieldCheck, 
  IconEye, 
  IconEyeOff, 
  IconInfoCircle, 
  IconAlertTriangle,
  IconUser,
  IconMail,
  IconPhone,
  IconTrophy,
  IconCalendar,
  IconUsers,
  IconTarget
} from '@tabler/icons-react';
import type { ApplicationFormData, ExperienceLevel } from '../../types/vetting.types';
import { EXPERIENCE_LEVEL_CONFIGS, APPLICATION_STATUS_CONFIGS } from '../../types/vetting.types';

interface ReviewStepProps {
  form: UseFormReturn<ApplicationFormData>;
}

export const ReviewStep: React.FC<ReviewStepProps> = ({ form }) => {
  const { control, formState: { errors }, watch } = form;
  
  // Watch all form values for review display
  const formData = watch();
  
  // Calculate application summary
  const applicationSummary = useMemo(() => {
    const totalFields = 15; // Approximate number of key fields
    const completedFields = [
      formData.personalInfo?.fullName,
      formData.personalInfo?.sceneName,
      formData.personalInfo?.email,
      formData.experience?.description,
      formData.experience?.safetyKnowledge,
      formData.experience?.consentUnderstanding,
      formData.community?.whyJoin,
      formData.community?.skillsInterests?.length,
      formData.community?.expectations,
      formData.community?.agreesToGuidelines,
      formData.references?.reference1?.name,
      formData.references?.reference1?.email,
      formData.references?.reference2?.name,
      formData.references?.reference2?.email
    ].filter(Boolean).length;
    
    return {
      completedFields,
      totalFields,
      completionPercentage: Math.round((completedFields / totalFields) * 100)
    };
  }, [formData]);

  const InfoRow = ({ 
    icon, 
    label, 
    value, 
    isPrivate = false 
  }: { 
    icon: React.ReactNode; 
    label: string; 
    value: string | React.ReactNode; 
    isPrivate?: boolean;
  }) => (
    <Group gap="md" align="flex-start">
      <ThemeIcon color="wcr.7" variant="light" size="sm">
        {icon}
      </ThemeIcon>
      <Box style={{ flex: 1 }}>
        <Group gap="xs" mb={2}>
          <Text size="sm" fw={500} c="wcr.8">
            {label}
          </Text>
          {isPrivate && (
            <Badge size="xs" color="blue" variant="light">
              Private
            </Badge>
          )}
        </Group>
        <Text size="sm" c="dimmed">
          {value || 'Not provided'}
        </Text>
      </Box>
    </Group>
  );

  return (
    <Stack gap="xl">
      {/* Section Header */}
      <Box>
        <Text size="xl" fw={700} c="wcr.7" mb="xs">
          Review & Submit
        </Text>
        <Text c="dimmed">
          Please review your application carefully before submitting. You can go back to edit any section if needed.
        </Text>
      </Box>

      {/* Application Summary */}
      <Paper p="lg" withBorder bg="green.0">
        <Group justify="apart" mb="md">
          <Text fw={600} c="green.7">
            Application Summary
          </Text>
          <Badge color="green" variant="filled">
            {applicationSummary.completionPercentage}% Complete
          </Badge>
        </Group>
        <Text size="sm" c="green.7">
          You've completed {applicationSummary.completedFields} of {applicationSummary.totalFields} key fields.
        </Text>
      </Paper>

      {/* Application Review */}
      <Accordion variant="separated" defaultValue="personal">
        {/* Personal Information */}
        <Accordion.Item value="personal">
          <Accordion.Control>
            <Group gap="md">
              <IconUser size={18} color="#880124" />
              <Text fw={500}>Personal Information</Text>
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            <Stack gap="sm">
              <InfoRow 
                icon={<IconUser size={14} />}
                label="Full Name"
                value={formData.personalInfo?.fullName}
                isPrivate
              />
              <InfoRow 
                icon={<IconUser size={14} />}
                label="Scene Name"
                value={formData.personalInfo?.sceneName}
              />
              {formData.personalInfo?.pronouns && (
                <InfoRow 
                  icon={<IconUser size={14} />}
                  label="Pronouns"
                  value={formData.personalInfo.pronouns}
                />
              )}
              <InfoRow 
                icon={<IconMail size={14} />}
                label="Email"
                value={formData.personalInfo?.email}
                isPrivate
              />
              {formData.personalInfo?.phone && (
                <InfoRow 
                  icon={<IconPhone size={14} />}
                  label="Phone"
                  value={formData.personalInfo.phone}
                  isPrivate
                />
              )}
            </Stack>
          </Accordion.Panel>
        </Accordion.Item>

        {/* Experience & Knowledge */}
        <Accordion.Item value="experience">
          <Accordion.Control>
            <Group gap="md">
              <IconTrophy size={18} color="#880124" />
              <Text fw={500}>Experience & Knowledge</Text>
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            <Stack gap="sm">
              <InfoRow 
                icon={<IconTrophy size={14} />}
                label="Experience Level"
                value={formData.experience?.level ? EXPERIENCE_LEVEL_CONFIGS[formData.experience.level as ExperienceLevel]?.label : ''}
              />
              <InfoRow 
                icon={<IconCalendar size={14} />}
                label="Years of Experience"
                value={`${formData.experience?.yearsExperience || 0} years`}
              />
              <InfoRow 
                icon={<IconInfoCircle size={14} />}
                label="Experience Description"
                value={
                  <Text size="sm" c="dimmed" lineClamp={3}>
                    {formData.experience?.description || 'Not provided'}
                  </Text>
                }
              />
            </Stack>
          </Accordion.Panel>
        </Accordion.Item>

        {/* Community Understanding */}
        <Accordion.Item value="community">
          <Accordion.Control>
            <Group gap="md">
              <IconUsers size={18} color="#880124" />
              <Text fw={500}>Community Understanding</Text>
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            <Stack gap="sm">
              <InfoRow 
                icon={<IconTarget size={14} />}
                label="Why Join Community"
                value={
                  <Text size="sm" c="dimmed" lineClamp={3}>
                    {formData.community?.whyJoin || 'Not provided'}
                  </Text>
                }
              />
              <InfoRow 
                icon={<IconUsers size={14} />}
                label="Skills & Interests"
                value={
                  <Group gap={4}>
                    {formData.community?.skillsInterests?.map(skill => (
                      <Badge key={skill} size="sm" color="wcr.7" variant="light">
                        {skill}
                      </Badge>
                    )) || 'None selected'}
                  </Group>
                }
              />
              <InfoRow 
                icon={<IconCheck size={14} />}
                label="Community Guidelines"
                value={formData.community?.agreesToGuidelines ? 'Agreed' : 'Not agreed'}
              />
            </Stack>
          </Accordion.Panel>
        </Accordion.Item>

        {/* References */}
        <Accordion.Item value="references">
          <Accordion.Control>
            <Group gap="md">
              <IconUser size={18} color="#880124" />
              <Text fw={500}>References</Text>
            </Group>
          </Accordion.Control>
          <Accordion.Panel>
            <Stack gap="md">
              {/* Reference 1 */}
              {formData.references?.reference1?.name && (
                <Paper p="md" withBorder bg="blue.0">
                  <Text fw={500} size="sm" mb="xs">Primary Reference</Text>
                  <Stack gap="xs">
                    <Text size="xs" c="dimmed">
                      <strong>Name:</strong> {formData.references.reference1.name}
                    </Text>
                    <Text size="xs" c="dimmed">
                      <strong>Relationship:</strong> {formData.references.reference1.relationship}
                    </Text>
                  </Stack>
                </Paper>
              )}

              {/* Reference 2 */}
              {formData.references?.reference2?.name && (
                <Paper p="md" withBorder bg="blue.0">
                  <Text fw={500} size="sm" mb="xs">Secondary Reference</Text>
                  <Stack gap="xs">
                    <Text size="xs" c="dimmed">
                      <strong>Name:</strong> {formData.references.reference2.name}
                    </Text>
                    <Text size="xs" c="dimmed">
                      <strong>Relationship:</strong> {formData.references.reference2.relationship}
                    </Text>
                  </Stack>
                </Paper>
              )}

              {/* Reference 3 */}
              {formData.references?.reference3?.name && (
                <Paper p="md" withBorder bg="gray.0">
                  <Text fw={500} size="sm" mb="xs">Additional Reference</Text>
                  <Stack gap="xs">
                    <Text size="xs" c="dimmed">
                      <strong>Name:</strong> {formData.references.reference3.name}
                    </Text>
                    <Text size="xs" c="dimmed">
                      <strong>Relationship:</strong> {formData.references.reference3.relationship}
                    </Text>
                  </Stack>
                </Paper>
              )}
            </Stack>
          </Accordion.Panel>
        </Accordion.Item>
      </Accordion>

      <Divider />

      {/* Privacy Options */}
      <Paper p="lg" withBorder>
        <Text fw={600} mb="md" c="wcr.7">
          Privacy Options
        </Text>
        
        <Stack gap="md">
          {/* Anonymous Application Toggle */}
          <Controller
            name="review.isAnonymous"
            control={control}
            render={({ field }) => (
              <Group justify="apart">
                <Box>
                  <Group gap="xs" mb={4}>
                    {field.value ? <IconEyeOff size={16} /> : <IconEye size={16} />}
                    <Text fw={500}>Anonymous Application</Text>
                  </Group>
                  <Text size="sm" c="dimmed">
                    {field.value 
                      ? 'Your identity will be kept private during the review process'
                      : 'Your scene name may be visible to the vetting team'
                    }
                  </Text>
                </Box>
                <Switch
                  {...(field as any)}
                  checked={field.value}
                  color="wcr.7"
                  size="md"
                />
              </Group>
            )}
          />

          {/* Consent to Contact */}
          <Controller
            name="review.consentToContact"
            control={control}
            render={({ field }) => (
              <Checkbox
                {...(field as any)}
                checked={field.value}
                label="I consent to be contacted by the vetting team during the review process"
                description="We may need to contact you for clarification or additional information"
                styles={((theme) => ({
                  input: {
                    '&:checked': {
                      backgroundColor: theme.colors.wcr[7],
                      borderColor: theme.colors.wcr[7]
                    }
                  }
                })) as any}
              />
            )}
          />
        </Stack>
      </Paper>

      {/* Terms Agreement */}
      <Paper p="lg" withBorder bg="yellow.0">
        <Text fw={600} mb="md" c="yellow.8">
          Final Agreement
        </Text>
        
        <Controller
          name="review.agreesToTerms"
          control={control}
          render={({ field }) => (
            <Checkbox
              {...(field as any)}
              checked={field.value}
              label={
                <Text size="sm">
                  I certify that all information provided in this application is true and accurate to the best of my knowledge. 
                  I understand that providing false information may result in denial or revocation of membership.
                </Text>
              }
              error={errors.review?.agreesToTerms?.message}
              styles={((theme) => ({
                input: {
                  '&:checked': {
                    backgroundColor: theme.colors.wcr[7],
                    borderColor: theme.colors.wcr[7]
                  }
                },
                label: {
                  fontFamily: 'Source Sans 3, sans-serif',
                  fontWeight: 500
                }
              })) as any}
              required
            />
          )}
        />
      </Paper>

      {/* Final Notices */}
      <Alert 
        icon={<IconInfoCircle />}
        color="blue"
        title="What Happens Next?"
      >
        <List size="sm" spacing="xs">
          <List.Item>You'll receive an email confirmation with a tracking number</List.Item>
          <List.Item>Your references will be contacted within 24-48 hours</List.Item>
          <List.Item>Our vetting team will review your application within 5-7 business days</List.Item>
          <List.Item>You'll be notified of our decision via email</List.Item>
          <List.Item>If approved, you'll receive welcome information and next steps</List.Item>
        </List>
      </Alert>

      {/* Submission Requirements Check */}
      {(!formData.review?.agreesToTerms) && (
        <Alert 
          icon={<IconAlertTriangle />}
          color="red"
          title="Submission Requirements"
        >
          <Text size="sm">
            You must agree to the final terms and certification to submit your application.
          </Text>
        </Alert>
      )}
    </Stack>
  );
};