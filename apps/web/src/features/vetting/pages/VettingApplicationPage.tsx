// Main vetting application page
import React, { useState } from 'react';
import {
  Container,
  Box,
  Paper,
  Text,
  Button,
  Group,
  Stack,
  Title,
  Timeline,
  ThemeIcon,
  Alert
} from '@mantine/core';
import {
  IconCheck,
  IconInfoCircle,
  IconCalendar,
  IconMail
} from '@tabler/icons-react';
import { VettingApplicationForm } from '../components/VettingApplicationForm';
import { VettingStatusBox } from '../components/VettingStatusBox';
import { useVettingStatus } from '../hooks/useVettingStatus';
import { Link, useNavigate } from 'react-router-dom';

interface VettingApplicationPageProps {
  className?: string;
}

export const VettingApplicationPage: React.FC<VettingApplicationPageProps> = ({
  className
}) => {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [submissionResult, setSubmissionResult] = useState<{
    applicationNumber: string;
    statusUrl: string;
  } | null>(null);
  const navigate = useNavigate();
  const { data: statusData, isLoading: statusLoading } = useVettingStatus();

  const handleSubmissionComplete = (applicationNumber: string, statusUrl: string) => {
    setSubmissionResult({ applicationNumber, statusUrl });
    setIsSubmitted(true);

    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  if (isSubmitted && submissionResult) {
    return (
      <Container size="md" py="xl" className={className}>
        <Paper p="xl" shadow="lg" ta="center">
          <Stack gap="xl">
            <Box>
              <Title order={1} c="green.7" mb="md">
                Application Submitted Successfully!
              </Title>
              <Text size="lg" c="dimmed">
                Your vetting application has been received and you should receive a confirmation email shortly.
              </Text>
            </Box>

            <Alert
              icon={<IconInfoCircle />}
              color="blue"
              title="What happens next?"
            >
              <Stack gap="sm" align="flex-start" ta="left" maw={600} mx="auto">
                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>1</Text>
                  </ThemeIcon>
                  <Text size="sm">Confirmation email sent - You'll receive an email confirming your submission</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>2</Text>
                  </ThemeIcon>
                  <Text size="sm">Application review - Our team reviews your application (typically 1-2 weeks)</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>3</Text>
                  </ThemeIcon>
                  <Text size="sm">Interview invitation - If approved to proceed, you'll receive an email to schedule your interview</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>4</Text>
                  </ThemeIcon>
                  <Text size="sm">Interview scheduled - Schedule a time that works for you and our vetting team</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>5</Text>
                  </ThemeIcon>
                  <Text size="sm">Interview completed - Meet with our vetting team to discuss your application</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>6</Text>
                  </ThemeIcon>
                  <Text size="sm">Final decision - You'll receive an email with the outcome of your application</Text>
                </Group>

                <Group gap="sm" align="flex-start">
                  <ThemeIcon size={24} color="blue" variant="light" mt={2}>
                    <Text size="xs" fw={700}>7</Text>
                  </ThemeIcon>
                  <Text size="sm">Welcome to the community! - If approved, you'll gain full access to all events and resources</Text>
                </Group>
              </Stack>
            </Alert>

            <Group justify="center" gap="md">
              <Button
                component={Link}
                to="/dashboard"
                color="blue"
                size="lg"
              >
                Go to Dashboard
              </Button>

              <Button
                variant="outline"
                color="wcr.7"
                size="lg"
                onClick={() => navigate('/')}
              >
                Return to Home
              </Button>
            </Group>
          </Stack>
        </Paper>
      </Container>
    );
  }

  // Show status box if user has an existing application
  const hasExistingApplication =
    statusData?.hasApplication && statusData.application && !statusLoading;

  return (
    <Container size="lg" py="xl" className={className}>
      <Stack gap="xl">
        {/* Show status box at top if application exists */}
        {hasExistingApplication && statusData.application.status && (
          <VettingStatusBox
            status={statusData.application.status as import('../types/vettingStatus').VettingStatus}
            applicationNumber={statusData.application.applicationNumber || 'N/A'}
            submittedAt={new Date(statusData.application.submittedAt!)}
            lastUpdated={new Date(statusData.application.lastUpdated!)}
            statusDescription={statusData.application.statusDescription || ''}
            nextSteps={statusData.application.nextSteps || undefined}
            estimatedDaysRemaining={statusData.application.estimatedDaysRemaining || undefined}
          />
        )}

        {/* Show form for new applications or Draft status */}
        <VettingApplicationForm
          onSubmitSuccess={(applicationId, statusUrl) => {
            const applicationNumber = applicationId.slice(-8).toUpperCase(); // Generate display number from ID
            handleSubmissionComplete(applicationNumber, statusUrl);
          }}
        />
      </Stack>
    </Container>
  );
};