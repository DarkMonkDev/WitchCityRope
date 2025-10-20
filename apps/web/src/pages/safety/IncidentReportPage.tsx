import React, { useState } from 'react';
import {
  Container,
  Title,
  Text,
  Paper,
  Stack,
  Group,
  Switch,
  TextInput,
  Textarea,
  Select,
  Button,
  Alert,
  Box
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconInfoCircle, IconCheck, IconAlertCircle } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';

type SeverityLevel = 'Low' | 'Medium' | 'High' | 'Critical';

interface IncidentFormValues {
  isAnonymous: boolean;
  contactEmail: string;
  contactPhone: string;
  severity: SeverityLevel | '';
  incidentDate: string;
  incidentTime: string;
  location: string;
  description: string;
  involvedParties: string;
  witnesses: string;
  requestFollowUp: boolean;
}

const severityOptions: { value: SeverityLevel; description: string }[] = [
  {
    value: 'Low',
    description: 'Minor concern, documentation only'
  },
  {
    value: 'Medium',
    description: 'Safety concern requiring follow-up'
  },
  {
    value: 'High',
    description: 'Boundary violation, harassment'
  },
  {
    value: 'Critical',
    description: 'Immediate safety risk, consent violation'
  }
];

export const IncidentReportPage: React.FC = () => {
  const navigate = useNavigate();
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const form = useForm<IncidentFormValues>({
    initialValues: {
      isAnonymous: true,
      contactEmail: '',
      contactPhone: '',
      severity: '',
      incidentDate: '',
      incidentTime: '',
      location: '',
      description: '',
      involvedParties: '',
      witnesses: '',
      requestFollowUp: false
    },
    validate: {
      contactEmail: (value, values) =>
        !values.isAnonymous && !value ? 'Email is required for identified reports' : null,
      severity: (value) => !value ? 'Please select a severity level' : null,
      incidentDate: (value) => !value ? 'Incident date is required' : null,
      location: (value) => !value || value.length < 3 ? 'Location is required' : null,
      description: (value) =>
        !value || value.length < 20 ? 'Description must be at least 20 characters' : null
    }
  });

  const handleSubmit = async (values: IncidentFormValues) => {
    setIsSubmitting(true);

    try {
      // TODO: API call to submit incident
      // const response = await incidentService.submitIncident({
      //   ...values,
      //   incidentDate: `${values.incidentDate}T${values.incidentTime || '00:00'}:00`
      // });

      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));

      setIsSubmitted(true);

      notifications.show({
        title: 'Report Submitted',
        message: 'Your incident report has been received',
        color: 'green',
        icon: <IconCheck />
      });
    } catch (error) {
      notifications.show({
        title: 'Submission Failed',
        message: 'Failed to submit report. Please try again.',
        color: 'red',
        icon: <IconAlertCircle />
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isSubmitted) {
    return (
      <Container size="md" py="xl">
        <Paper p="xl" radius="md" style={{ border: '2px solid #4A5C3A' }}>
          <Stack gap="lg" align="center">
            <Box
              style={{
                width: 80,
                height: 80,
                borderRadius: '50%',
                backgroundColor: '#4A5C3A',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                fontSize: 48
              }}
            >
              ✓
            </Box>

            <Title order={2} ta="center" style={{ color: '#880124' }}>
              Report Submitted
            </Title>

            {form.values.isAnonymous ? (
              <>
                <Text ta="center" size="lg">
                  Your report has been submitted and will be reviewed by our safety team.
                </Text>
                <Alert color="blue" icon={<IconInfoCircle />}>
                  <Text size="sm">
                    This report was submitted anonymously. We cannot provide status updates or contact you for follow-up information.
                  </Text>
                </Alert>
              </>
            ) : (
              <>
                <Text ta="center" size="lg">
                  Your report has been received and you will be contacted if additional information is needed.
                </Text>
                <Text ta="center" c="dimmed">
                  You can view the status of your reports in the <strong>My Reports</strong> section.
                </Text>
                <Group gap="md">
                  <Button
                    variant="filled"
                    color="blue"
                    onClick={() => navigate('/my-reports')}
                  >
                    View My Reports
                  </Button>
                  <Button variant="light" onClick={() => navigate('/')}>
                    Return Home
                  </Button>
                </Group>
              </>
            )}

            {form.values.isAnonymous && (
              <Button variant="light" onClick={() => navigate('/')} mt="md">
                Return Home
              </Button>
            )}

            <Box mt="lg" p="md" style={{ backgroundColor: '#F5F5F5', borderRadius: 8, width: '100%' }}>
              <Title order={4} mb="sm" style={{ color: '#880124' }}>
                Support Resources
              </Title>
              <Stack gap="xs">
                <Text size="sm">
                  <strong>National Sexual Assault Hotline:</strong> 1-800-656-HOPE (4673)
                </Text>
                <Text size="sm">
                  <strong>LGBTQ National Hotline:</strong> 1-888-843-4564
                </Text>
                <Text size="sm">
                  <strong>Community Support:</strong> safety@witchcityrope.com
                </Text>
              </Stack>
            </Box>
          </Stack>
        </Paper>
      </Container>
    );
  }

  return (
    <Box style={{ backgroundColor: '#FAF6F2', minHeight: '100vh' }}>
      {/* Header */}
      <Box
        style={{
          background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
          color: 'white',
          padding: '48px 0 32px',
          textAlign: 'center'
        }}
      >
        <Container size="md">
          <Title order={1} mb="md">
            Report a Safety Incident
          </Title>
          <Text size="lg" style={{ opacity: 0.9 }}>
            Your safety and well-being are our top priority
          </Text>
        </Container>
      </Box>

      {/* Form */}
      <Container size="md" py="xl">
        {/* Confidentiality Notice */}
        <Alert
          color="yellow"
          icon={<IconInfoCircle />}
          mb="xl"
          styles={{
            root: {
              borderLeft: '6px solid #FFBF00'
            }
          }}
        >
          <Title order={4} mb="xs" style={{ color: '#880124' }}>
            Confidentiality & Privacy
          </Title>
          <Text size="sm" mb="xs">
            All incident reports are treated with the utmost confidentiality and are only shared with our safety team on a need-to-know basis.
          </Text>
          <Text size="sm" mb="xs">
            You may submit this report anonymously or include your contact information for follow-up.
          </Text>
          <Text size="sm" fw={600} style={{ color: '#880124' }}>
            Sensitive information is encrypted and stored securely.
          </Text>
        </Alert>

        <Paper p="xl" radius="md" component="form" onSubmit={form.onSubmit(handleSubmit)}>
          <Stack gap="xl">
            {/* Anonymous Toggle */}
            <Box>
              <Switch
                data-testid="anonymous-checkbox"
                label="Submit Anonymously"
                description="Your identity will not be recorded with this report"
                size="lg"
                checked={form.values.isAnonymous}
                onChange={(event) => form.setFieldValue('isAnonymous', event.currentTarget.checked)}
                styles={{
                  label: {
                    fontWeight: 600,
                    fontSize: 18
                  }
                }}
              />
            </Box>

            {/* Contact Information (conditional) */}
            {!form.values.isAnonymous && (
              <Box>
                <Title order={3} mb="md" style={{ color: '#880124' }}>
                  Contact Information
                </Title>
                <Stack gap="md">
                  <TextInput
                    data-testid="contact-email-input"
                    label="Email Address"
                    placeholder="your.email@example.com"
                    type="email"
                    required
                    {...form.getInputProps('contactEmail')}
                  />
                  <TextInput
                    data-testid="contact-phone-input"
                    label="Phone Number (Optional)"
                    placeholder="(555) 123-4567"
                    type="tel"
                    {...form.getInputProps('contactPhone')}
                  />
                  <Switch
                    data-testid="request-followup-checkbox"
                    label="I would like to be contacted for follow-up"
                    checked={form.values.requestFollowUp}
                    onChange={(event) =>
                      form.setFieldValue('requestFollowUp', event.currentTarget.checked)
                    }
                  />
                </Stack>
              </Box>
            )}

            {/* Severity Level */}
            <Box>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Severity Level
              </Title>
              <Text size="sm" c="dimmed" mb="md">
                Select the severity level that best describes this incident
              </Text>
              <Stack gap="sm" data-testid="severity-select">
                {severityOptions.map((option) => (
                  <Paper
                    key={option.value}
                    data-testid={`severity-${option.value.toLowerCase()}`}
                    p="md"
                    style={{
                      border: `2px solid ${
                        form.values.severity === option.value ? '#880124' : '#E0E0E0'
                      }`,
                      borderRadius: 8,
                      cursor: 'pointer',
                      transition: 'all 0.2s ease',
                      backgroundColor:
                        form.values.severity === option.value ? '#FFF8F0' : 'white'
                    }}
                    onClick={() => form.setFieldValue('severity', option.value)}
                  >
                    <Group justify="space-between" align="center">
                      <Box>
                        <Text size="md" fw={600}>{option.value}</Text>
                        <Text size="sm" mt="xs" c="dimmed">
                          {option.description}
                        </Text>
                      </Box>
                    </Group>
                  </Paper>
                ))}
              </Stack>
              {form.errors.severity && (
                <Text c="red" size="sm" mt="xs">
                  {form.errors.severity}
                </Text>
              )}
            </Box>

            {/* Incident Details */}
            <Box>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Incident Details
              </Title>
              <Stack gap="md">
                <Group grow align="flex-start">
                  <TextInput
                    data-testid="incident-date-input"
                    label="Date of Incident"
                    type="date"
                    required
                    {...form.getInputProps('incidentDate')}
                  />
                  <TextInput
                    data-testid="incident-time-input"
                    label="Time of Incident (Optional)"
                    type="time"
                    {...form.getInputProps('incidentTime')}
                  />
                </Group>

                <TextInput
                  data-testid="incident-location-input"
                  label="Location"
                  placeholder="Where did this incident occur?"
                  required
                  {...form.getInputProps('location')}
                />

                <Textarea
                  data-testid="incident-description-textarea"
                  label="Description of Incident"
                  placeholder="Please provide a detailed description of what happened..."
                  required
                  minRows={6}
                  {...form.getInputProps('description')}
                />
              </Stack>
            </Box>

            {/* People Involved */}
            <Box>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                People Involved
              </Title>
              <Stack gap="md">
                <Textarea
                  data-testid="involved-parties-textarea"
                  label="Involved Parties (Optional)"
                  placeholder="Names or descriptions of people involved in the incident..."
                  minRows={3}
                  {...form.getInputProps('involvedParties')}
                />

                <Textarea
                  data-testid="witnesses-textarea"
                  label="Witnesses (Optional)"
                  placeholder="Names or descriptions of witnesses, if any..."
                  minRows={3}
                  {...form.getInputProps('witnesses')}
                />
              </Stack>
            </Box>

            {/* Submit Button */}
            <Button
              data-testid="submit-report-button"
              type="submit"
              size="lg"
              fullWidth
              loading={isSubmitting}
              disabled={Object.keys(form.errors).length > 0}
              styles={{
                root: {
                  backgroundColor: '#880124',
                  height: 56,
                  fontSize: 18,
                  fontWeight: 700
                }
              }}
            >
              {isSubmitting ? 'Submitting Report...' : 'Submit Report'}
            </Button>

            {/* Privacy Note */}
            <Alert color="blue" icon={<IconInfoCircle />}>
              <Text size="xs">
                By submitting this report, you acknowledge that the information provided will be reviewed by our safety team and may be used to address safety concerns within our community.
              </Text>
            </Alert>
          </Stack>
        </Paper>
      </Container>
    </Box>
  );
};
