// Incident Report Form Component
// Main form for submitting safety incidents (anonymous or identified)
// UPDATED: Matches wireframe exactly per user requirements

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
  IncidentType,
  WhereOccurred,
  SpokenToPersonStatus,
  INCIDENT_TYPE_LABELS,
  WHERE_OCCURRED_LABELS,
  SPOKEN_TO_PERSON_LABELS
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

  // Initialize form with default values (matching wireframe)
  const form = useForm<IncidentFormData>({
    initialValues: {
      incidentType: IncidentType.SafetyConcern,
      incidentDate: new Date().toISOString().split('T')[0], // Today
      incidentTime: new Date().toLocaleTimeString('en-US', {
        hour12: false,
        hour: '2-digit',
        minute: '2-digit'
      }),
      whereOccurred: WhereOccurred.AtEvent,
      eventName: '',
      location: '',
      description: '',
      witnesses: '',
      involvedParties: '',
      hasSpokenToPerson: '',
      desiredOutcomes: '',
      futureInteractionPreference: '',
      anonymousDuringInvestigation: false,
      anonymousInFinalReport: false,
      isAnonymous: !isAuthenticated, // Default to anonymous if not logged in
      requestFollowUp: false,
      contactEmail: user?.email || '',
      contactName: ''
    },
    validate: {
      incidentDate: (value) => {
        if (!value) return 'Incident date is required';
        return null;
      },
      location: (value) => {
        if (!value || value.length < 3) {
          return 'Location details are required (minimum 3 characters)';
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
      onSubmissionComplete?.((result as any).referenceNumber);
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
        submissionResult={submissionResult as any}
        onNewReport={handleNewReport}
      />
    );
  }

  return (
    <Box maw={900} mx="auto" p="md">
      <Paper shadow="sm" p="xl" radius="md">
        <Stack gap="xl">
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
            icon={<IconShieldCheck />}
            title="Privacy & Security"
          >
            <Text size="sm">
              Your report will be encrypted immediately and accessible only to the safety team.
              Anonymous reports do not log IP addresses or personal identifying information.
            </Text>
          </Alert>

          {/* Form */}
          <form onSubmit={form.onSubmit(handleSubmit)}>
            <Stack gap="xl">
              {/* Anonymous/Identified Toggle - FIRST per wireframe */}
              <Box>
                <Text fw={600} size="md" mb="md">
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
                      form.setFieldValue('contactName', '');
                    } else if (user?.email) {
                      form.setFieldValue('contactEmail', user.email);
                    }
                  }}
                >
                  <Group>
                    <Radio
                      value="anonymous"
                      label="Anonymous Report"
                      description="No contact information stored"
                    />
                    <Radio
                      value="identified"
                      label="Include My Contact"
                      description="Allow follow-up communication"
                    />
                  </Group>
                </Radio.Group>
                {!isAuthenticated && !form.values.isAnonymous && (
                  <Alert variant="light" color="blue" mt="md">
                    <Text size="sm">
                      You can submit a report with contact information without logging in.
                      However, logging in provides additional features like tracking your report and receiving updates.
                    </Text>
                  </Alert>
                )}
              </Box>

              {/* Contact Information - if identified */}
              {!form.values.isAnonymous && (
                <Paper p="md" withBorder>
                  <Stack gap="md">
                    <Text fw={600} size="sm">Contact Information</Text>
                    <MantineTextInput
                      label="Your Name *"
                      placeholder="Enter your name"
                      taperedUnderline
                      {...form.getInputProps('contactName')}
                    />
                    <MantineTextInput
                      label="Contact Email *"
                      placeholder="your.email@example.com"
                      type="email"
                      taperedUnderline
                      {...form.getInputProps('contactEmail')}
                    />
                    <Checkbox
                      label="Request follow-up contact"
                      checked={form.values.requestFollowUp}
                      onChange={(event) => form.setFieldValue('requestFollowUp', event.currentTarget.checked)}
                    />
                  </Stack>
                </Paper>
              )}

              {/* Type of Incident - Dropdown per user request */}
              <MantineSelect
                label="Type of Incident *"
                placeholder="Select incident type"
                data={Object.entries(INCIDENT_TYPE_LABELS).map(([value, label]) => ({
                  value,
                  label
                }))}
                taperedUnderline
                {...form.getInputProps('incidentType')}
              />

              {/* When did this occur? */}
              <Group grow align="flex-start">
                <Box>
                  <Text fw={600} size="md" mb="xs">
                    When did this occur? *
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

              {/* Where did this occur? - Dropdown per wireframe */}
              <MantineSelect
                label="Where did this occur? *"
                placeholder="Select location type"
                data={Object.entries(WHERE_OCCURRED_LABELS).map(([value, label]) => ({
                  value,
                  label
                }))}
                taperedUnderline
                {...form.getInputProps('whereOccurred')}
              />

              {/* Location Details - ONLY for "Other community space" */}
              {form.values.whereOccurred === WhereOccurred.OtherSpace && (
                <MantineTextInput
                  label="Location Details *"
                  placeholder="Specific location (building, room, area, etc.)"
                  description="Provide specific details about where this occurred"
                  taperedUnderline
                  {...form.getInputProps('location')}
                />
              )}

              {/* Event Name - Always visible per user request */}
              <MantineTextInput
                label="Event Name (if applicable)"
                placeholder="Name of the event (if this occurred at an event)"
                taperedUnderline
                {...form.getInputProps('eventName')}
              />

              {/* Description - Bigger with better prompt */}
              <MantineTextarea
                label="What happened? *"
                placeholder="Please describe the incident in as much detail as you're comfortable sharing. Include what happened, when it occurred, who was involved, and any other relevant information that will help us understand the situation."
                description={`${form.values.description.length}/5000 characters (minimum 50 required)`}
                minRows={8}
                maxRows={15}
                autosize
                taperedUnderline
                {...form.getInputProps('description')}
              />

              {/* Were there any witnesses? */}
              <MantineTextarea
                label="Were there any witnesses?"
                placeholder="Names and contact information of witnesses"
                description="This information will be encrypted"
                minRows={2}
                autosize
                taperedUnderline
                {...form.getInputProps('witnesses')}
              />

              {/* Person(s) involved */}
              <MantineTextarea
                label="Person(s) involved"
                placeholder="Names or descriptions of people involved"
                description="This information will be encrypted"
                minRows={2}
                autosize
                taperedUnderline
                {...form.getInputProps('involvedParties')}
              />

              {/* Have you spoken to this person? */}
              <Box>
                <Text fw={600} size="md" mb="md">
                  Have you spoken to this person about the incident?
                </Text>
                <Radio.Group
                  value={form.values.hasSpokenToPerson}
                  onChange={(value) => form.setFieldValue('hasSpokenToPerson', value as SpokenToPersonStatus)}
                >
                  <Group>
                    {Object.entries(SPOKEN_TO_PERSON_LABELS).map(([value, label]) => (
                      <Radio key={value} value={value} label={label} />
                    ))}
                  </Group>
                </Radio.Group>
              </Box>

              {/* Desired Outcomes - Free-text */}
              <MantineTextarea
                label="What outcome are you hoping for?"
                placeholder="Please describe what resolution or outcome you're hoping for from this report..."
                description="Examples: Documentation only, mediation, speaking with the person involved, removing someone from the community, discussing options with the safety team, etc."
                minRows={3}
                autosize
                taperedUnderline
                {...form.getInputProps('desiredOutcomes')}
              />

              {/* Future Interaction Preference */}
              <MantineTextarea
                label="What interaction do you want with this person going forward?"
                placeholder="Please describe how you'd like interactions with the involved person to be handled going forward..."
                description="Examples: No contact, supervised contact only, willing to communicate through mediation, open to discussing the situation, etc."
                minRows={3}
                autosize
                taperedUnderline
                {...form.getInputProps('futureInteractionPreference')}
              />

              {/* Anonymity Preference - Two Independent Checkboxes */}
              <Box>
                <Text fw={600} size="md" mb="md">
                  How would you like to be identified?
                </Text>
                <Stack gap="md">
                  <Checkbox
                    label="Keep me anonymous during the investigation"
                    description="Your identity will not be shared with anyone during the investigation process"
                    checked={form.values.anonymousDuringInvestigation}
                    onChange={(event) => form.setFieldValue('anonymousDuringInvestigation', event.currentTarget.checked)}
                  />
                  <Checkbox
                    label="Keep me anonymous in the final report"
                    description="Your identity will not be included in the final report documentation"
                    checked={form.values.anonymousInFinalReport}
                    onChange={(event) => form.setFieldValue('anonymousInFinalReport', event.currentTarget.checked)}
                  />

                  {/* Dynamic explanatory text based on selections */}
                  <Alert variant="light" color="blue">
                    <Text size="sm" fw={500} mb="xs">What this means:</Text>
                    <Text size="sm">
                      {!form.values.anonymousDuringInvestigation && !form.values.anonymousInFinalReport && (
                        "Your identity will be shared during the investigation and included in the final report."
                      )}
                      {form.values.anonymousDuringInvestigation && !form.values.anonymousInFinalReport && (
                        "Your identity will be kept confidential during the investigation, but you will be identified in the final report."
                      )}
                      {!form.values.anonymousDuringInvestigation && form.values.anonymousInFinalReport && (
                        "Your identity will be shared during the investigation, but will be kept anonymous in the final report."
                      )}
                      {form.values.anonymousDuringInvestigation && form.values.anonymousInFinalReport && (
                        "Your identity will be kept confidential both during the investigation and in the final report (as much as possible)."
                      )}
                    </Text>
                  </Alert>
                </Stack>
              </Box>

              {/* Agreement and Submit */}
              <Box>
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
            </Stack>
          </form>
        </Stack>
      </Paper>
    </Box>
  );
}
