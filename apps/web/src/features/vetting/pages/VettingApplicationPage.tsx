// Main vetting application page
//
// Shows the vetting application form for users who haven't applied yet.
// For users with an in-progress application, shows the same VettingAlertBox
// that the dashboard uses (single-source display — see
// apps/web/src/features/vetting/constants/vettingStatusConfig.ts).
//
// NOTE: This page previously rendered a richer `VettingStatusBox` component
// that showed submission date + status description. That was replaced with
// VettingAlertBox in Phase 1 of the vetting status centralization to keep
// /join and /dashboard visually consistent. If the submission-date display
// is needed again, it should be added to VettingAlertBox (or a sibling
// component) rather than reintroducing a separate status panel.
import React, { useState } from 'react';
import { Container, Box, Paper, Text, Button, Group, Stack, Title, ThemeIcon, Alert } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { useMediaQuery } from '@mantine/hooks';
import { VettingApplicationForm } from '../components/VettingApplicationForm';
import { VettingAlertBox } from '../../../pages/dashboard/components/VettingAlertBox';
// VettingStatusDto hook (includes interviewScheduleUrl / reapplyInfoUrl).
// VettingApplicationForm below has its own query for existing-application
// state, so this page no longer needs the detailed MyApplicationStatusResponse.
import { useVettingStatus as useDashboardVettingStatus } from '../../../hooks/useDashboard';
import { useIsAuthenticated } from '../../../stores/authStore';
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
  // Dashboard-shaped status DTO (includes interviewScheduleUrl for the
  // InterviewApproved alert link). Hook is gated on authenticated user, so
  // it returns undefined for public visitors — in which case no alert is shown.
  const isAuthenticated = useIsAuthenticated();
  const { data: dashboardVettingStatus } = useDashboardVettingStatus();
  // Responsive breakpoint matches the existing dashboard convention at
  // apps/web/src/pages/dashboard/MyEventsPage.tsx (991px). Used below to
  // collapse the alert→form gap on mobile while keeping it on desktop.
  const isMobile = useMediaQuery('(max-width: 991px)');

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

  return (
    <Container size="lg" py="xl" className={className}>
      {/*
        Responsive gap between the alert and the form: 16px on desktop for
        visual breathing room, 0 on mobile so the alert sits flush with the
        form (vertical space is scarce on small screens). VettingAlertBox
        no longer owns its own bottom margin — this Stack is the single
        source of spacing below the alert on /join.

        Matches the dashboard's responsive breakpoint convention (991px).
        If the alert isn't rendered, the Stack has only the form as a
        child and the gap value doesn't affect layout.
      */}
      <Stack gap={isMobile ? 0 : 16}>
        {/*
          Vetting status alert — same component used on the dashboard.
          Only renders for authenticated users whose status is not Approved.
          See apps/web/src/features/vetting/constants/vettingStatusConfig.ts
          for the single source of truth on alert copy and styling.
        */}
        {isAuthenticated &&
          dashboardVettingStatus &&
          dashboardVettingStatus.status !== 'Approved' && (
            <VettingAlertBox status={dashboardVettingStatus} />
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