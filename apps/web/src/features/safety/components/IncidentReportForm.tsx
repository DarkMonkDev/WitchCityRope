// Incident Report Form Component
// Main form for submitting safety incidents (anonymous or identified)

import React, { useState, useCallback } from 'react';
import {
  Box,
  Paper,
  Title,
  Text,
  Group,
  Stack,
  Button,
  Radio,
  Alert,
  Grid,
  Checkbox
} from '@mantine/core';
import { DatePickerInput, TimeInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { IconInfoCircle, IconShieldCheck, IconClock } from '@tabler/icons-react';
import { 
  MantineTextInput, 
  MantineTextarea, 
  MantineSelect 
} from '../../../components/forms/MantineFormInputs';
import { useSubmitIncidentForm } from '../hooks/useSubmitIncident';
import { useAuthStore } from '../../../stores/authStore';
import { 
  IncidentFormData, 
  IncidentSeverity, 
  SEVERITY_CONFIGS 
} from '../types/safety.types';
import { SubmissionConfirmation } from './SubmissionConfirmation';

interface IncidentReportFormProps {
  onSubmissionComplete?: (referenceNumber: string) => void;
}

export function IncidentReportForm({ onSubmissionComplete }: IncidentReportFormProps) {
  const [agreementChecked, setAgreementChecked] = useState(false);
  const user = useAuthStore(state => state.user);
  const isAuthenticated = useAuthStore(state => state.isAuthenticated);
  
  const {
    submitIncident,
    submissionResult,
    isSubmitting,
    error,
    isSuccess,
    resetSubmission
  } = useSubmitIncidentForm();
  
  // Initialize form with default values
  const form = useForm<IncidentFormData>({
    initialValues: {
      severity: IncidentSeverity.Medium,
      incidentDate: new Date().toISOString().split('T')[0], // Today
      incidentTime: new Date().toLocaleTimeString('en-US', { 
        hour12: false, 
        hour: '2-digit', 
        minute: '2-digit' 
      }),
      location: '',
      description: '',
      involvedParties: '',
      witnesses: '',
      isAnonymous: !isAuthenticated, // Default to anonymous if not logged in
      requestFollowUp: false,
      contactEmail: user?.email || '',
      contactPhone: ''
    },
    validate: {
      location: (value) => {
        if (!value || value.length < 5) {
          return 'Location must be at least 5 characters';
        }
        if (value.length > 200) {
          return 'Location must be less than 200 characters';
        }
        return null;
      },
      description: (value) => {
        if (!value || value.length < 50) {
          return 'Description must be at least 50 characters';
        }
        if (value.length > 5000) {
          return 'Description must be less than 5000 characters';
        }
        return null;
      },
      contactEmail: (value, values) => {
        if (!values.isAnonymous && (!value || !value.includes('@'))) {
          return 'Valid email address is required for identified reports';
        }
        return null;
      },
      requestFollowUp: (value, values) => {
        if (values.isAnonymous && value) {
          return 'Anonymous reports cannot request follow-up contact';
        }
        return null;
      }
    }
  });
  
  // Handle form submission
  const handleSubmit = useCallback(async (values: IncidentFormData) => {
    if (!agreementChecked) {
      return;
    }
    
    try {
      const result = await submitIncident(values, user?.id);
      onSubmissionComplete?.(result.referenceNumber);
    } catch (error) {
      console.error('Submission failed:', error);
    }
  }, [submitIncident, user?.id, agreementChecked, onSubmissionComplete]);
  
  // Handle new report
  const handleNewReport = useCallback(() => {
    resetSubmission();
    form.reset();
    setAgreementChecked(false);
  }, [resetSubmission, form]);
  
  // Show confirmation screen if successfully submitted
  if (isSuccess && submissionResult) {
    return (
      <SubmissionConfirmation 
        submissionResult={submissionResult}
        onNewReport={handleNewReport}
      />
    );
  }
  
  // Severity options for radio group
  const severityOptions = Object.values(SEVERITY_CONFIGS).map(config => ({
    value: config.value,
    label: `${config.icon} ${config.label}`,
    description: config.description
  }));
  
  return (
    <Box maw={800} mx="auto" p="md">
      <Paper shadow="sm" p="xl" radius="md">
        <Stack gap="lg">
          {/* Header */}
          <Box>
            <Title order={1} size="h2" mb="xs">
              Report a Safety Incident
            </Title>
            <Text c="dimmed" size="sm">
              Complete confidentiality available - Anonymous reporting protected
            </Text>
          </Box>
          
          {/* Privacy Notice */}
          <Alert 
            variant="light" 
            color="blue" 
            icon={<IconShieldCheck size={16} />}
            title="Privacy & Security"
          >
            <Text size="sm">
              Your report will be encrypted immediately and accessible only to the safety team. 
              Anonymous reports do not log IP addresses or personal identifying information.
            </Text>
          </Alert>
          
          {/* Form */}
          <form onSubmit={form.onSubmit(handleSubmit)}>
            <Grid gutter="lg">
              {/* Left Column - Incident Details */}
              <Grid.Col span={{ base: 12, md: 8 }}>
                <Stack gap="md">
                  <Title order={3} size="h4">Incident Details</Title>
                  
                  {/* Severity Level */}
                  <Box>
                    <Text fw={500} size="sm" mb="xs">
                      Severity Level *
                    </Text>
                    <Radio.Group
                      value={form.values.severity}
                      onChange={(value) => form.setFieldValue('severity', value as IncidentSeverity)}
                    >
                      <Stack gap="xs">
                        {severityOptions.map((option) => (
                          <Radio
                            key={option.value}
                            value={option.value}
                            label={
                              <Box>
                                <Text size="sm" fw={500}>{option.label}</Text>
                                <Text size="xs" c="dimmed">{option.description}</Text>
                              </Box>
                            }
                          />
                        ))}
                      </Stack>
                    </Radio.Group>
                  </Box>
                  
                  {/* Date and Time */}
                  <Group grow align="flex-start">
                    <Box>
                      <Text fw={500} size="sm" mb="xs">
                        When did this happen? *
                      </Text>
                      <DatePickerInput
                        value={new Date(form.values.incidentDate)}
                        onChange={(date) => {
                          if (date) {
                            form.setFieldValue('incidentDate', date.toISOString().split('T')[0]);
                          }
                        }}
                        placeholder="Select date"
                        maxDate={new Date()}
                        clearable={false}
                      />
                    </Box>
                    <TimeInput
                      label="Time"
                      value={form.values.incidentTime}
                      onChange={(event) => form.setFieldValue('incidentTime', event.currentTarget.value)}
                      leftSection={<IconClock size={16} />}
                    />
                  </Group>
                  
                  {/* Location */}
                  <MantineTextInput
                    label="Where did this happen? *"
                    placeholder="Enter location details (building, room, specific area)"
                    taperedUnderline
                    {...form.getInputProps('location')}
                  />
                  
                  {/* Description */}
                  <MantineTextarea
                    label="Incident Description *"
                    placeholder="Describe what happened in detail..."
                    description={`${form.values.description.length}/5000 characters (minimum 50)`}
                    minRows={4}
                    autosize
                    taperedUnderline
                    {...form.getInputProps('description')}
                  />
                  
                  {/* Involved Parties */}
                  <MantineTextarea
                    label="Involved Parties (Optional)"
                    placeholder="Names or descriptions of people involved"
                    description="This information will be encrypted"
                    minRows={2}
                    autosize
                    taperedUnderline
                    {...form.getInputProps('involvedParties')}
                  />
                  
                  {/* Witnesses */}
                  <MantineTextarea
                    label="Witnesses (Optional)"
                    placeholder="Names and contact information of witnesses"
                    description="This information will be encrypted"
                    minRows={2}
                    autosize
                    taperedUnderline
                    {...form.getInputProps('witnesses')}
                  />
                </Stack>
              </Grid.Col>
              
              {/* Right Column - Privacy & Contact */}
              <Grid.Col span={{ base: 12, md: 4 }}>
                <Stack gap="md">
                  <Title order={3} size="h4">Privacy & Contact</Title>
                  
                  {/* Anonymous/Identified Toggle */}
                  <Box>
                    <Text fw={500} size="sm" mb="xs">
                      Reporter Information
                    </Text>
                    <Radio.Group
                      value={form.values.isAnonymous ? 'anonymous' : 'identified'}
                      onChange={(value) => {
                        const isAnonymous = value === 'anonymous';
                        form.setFieldValue('isAnonymous', isAnonymous);
                        if (isAnonymous) {
                          form.setFieldValue('requestFollowUp', false);
                          form.setFieldValue('contactEmail', '');
                          form.setFieldValue('contactPhone', '');
                        } else if (user?.email) {
                          form.setFieldValue('contactEmail', user.email);
                        }
                      }}
                    >
                      <Stack gap="xs">
                        <Radio
                          value="anonymous"
                          label="Anonymous Report"
                          description="No contact information stored"
                        />
                        <Radio
                          value="identified"
                          label="Include My Contact"
                          description="Allow follow-up communication"
                          disabled={!isAuthenticated}
                        />
                      </Stack>
                    </Radio.Group>
                    {!isAuthenticated && (
                      <Text size="xs" c="dimmed" mt="xs">
                        <IconInfoCircle size={12} style={{ display: 'inline', marginRight: '4px' }} />
                        Login required for identified reports
                      </Text>
                    )}
                  </Box>
                  
                  {/* Contact Information - only show for identified reports */}
                  {!form.values.isAnonymous && (
                    <>
                      <MantineTextInput
                        label="Contact Email *"
                        placeholder="your.email@example.com"
                        type="email"
                        taperedUnderline
                        {...form.getInputProps('contactEmail')}
                      />
                      
                      <MantineTextInput
                        label="Phone (Optional)"
                        placeholder="Phone number for urgent contact"
                        type="tel"
                        taperedUnderline
                        {...form.getInputProps('contactPhone')}
                      />
                      
                      <Checkbox
                        label="Request follow-up contact"
                        checked={form.values.requestFollowUp}
                        onChange={(event) => form.setFieldValue('requestFollowUp', event.currentTarget.checked)}
                      />
                    </>
                  )}
                  
                  {/* Privacy Reminder */}
                  <Alert variant="light" color="green" icon={<IconShieldCheck size={16} />}>
                    <Stack gap="xs">
                      <Text size="sm" fw={500}>Your report will be:</Text>
                      <Text size="xs">• Encrypted immediately</Text>
                      <Text size="xs">• Accessible only to safety team</Text>
                      <Text size="xs">• Assigned tracking number</Text>
                      <Text size="xs">• Anonymous if selected</Text>
                    </Stack>
                  </Alert>
                </Stack>
              </Grid.Col>
            </Grid>
            
            {/* Agreement and Submit */}
            <Box mt="xl">
              <Checkbox
                checked={agreementChecked}
                onChange={(event) => setAgreementChecked(event.currentTarget.checked)}
                label="I understand this report may trigger safety team investigation"
                mb="md"
              />
              
              {error && (
                <Alert variant="light" color="red" mb="md">
                  <Text size="sm">
                    Failed to submit report: {error instanceof Error ? error.message : 'Unknown error occurred'}
                  </Text>
                </Alert>
              )}
              
              <Group justify="center">
                <Button
                  type="submit"
                  size="lg"
                  loading={isSubmitting}
                  disabled={!agreementChecked}
                  style={{
                    background: 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)',
                    border: 'none'
                  }}
                >
                  Submit Safety Report
                </Button>
              </Group>
            </Box>
          </form>
        </Stack>
      </Paper>
    </Box>
  );
}