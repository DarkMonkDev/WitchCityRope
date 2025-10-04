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
            <ThemeIcon size={80} color="green" variant="light" mx="auto">
              <IconCheck size={40} />
            </ThemeIcon>
            
            <Box>
              <Title order={1} c="green.7" mb="md">
                Application Submitted Successfully!
              </Title>
              <Text size="lg" c="dimmed">
                Thank you for applying to join WitchCityRope
              </Text>
            </Box>

            <Alert
              icon={<IconInfoCircle />}
              color="blue"
              title="What happens next?"
            >
              <Text size="sm" mb="md">
                Your application <strong>#{submissionResult.applicationNumber}</strong> has been received 
                and is now in our review queue.
              </Text>
              
              <Timeline active={1} color="blue" bulletSize={24} lineWidth={2}>
                <Timeline.Item bullet={<IconMail size={12} />} title="Email Confirmation">
                  <Text size="xs" c="dimmed">
                    You'll receive a confirmation email with your application details
                  </Text>
                </Timeline.Item>

                <Timeline.Item bullet={<IconCalendar size={12} />} title="Review Process">
                  <Text size="xs" c="dimmed">
                    Our vetting team will review your application within 1-2 weeks
                  </Text>
                </Timeline.Item>

                <Timeline.Item bullet={<IconCheck size={12} />} title="Decision Notification">
                  <Text size="xs" c="dimmed">
                    You'll receive an email with our decision and next steps
                  </Text>
                </Timeline.Item>
              </Timeline>
            </Alert>

            <Group justify="center" gap="md">
              <Button
                component={Link}
                to="/vetting/status"
                color="blue"
                size="lg"
              >
                Check Application Status
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

            <Paper p="md" bg="gray.0" withBorder>
              <Text size="sm" c="dimmed" ta="left">
                <strong>Important:</strong> Save your application number <strong>#{submissionResult.applicationNumber}</strong> 
                and check your email for the status tracking link. You can use either to check your application progress at any time.
              </Text>
            </Paper>
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
        {hasExistingApplication && (
          <VettingStatusBox
            status={statusData.application.status}
            applicationNumber={statusData.application.applicationNumber || 'N/A'}
            submittedAt={new Date(statusData.application.submittedAt)}
            lastUpdated={new Date(statusData.application.lastUpdated)}
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