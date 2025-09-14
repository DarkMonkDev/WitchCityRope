// Application status tracking component for public use
import React from 'react';
import {
  Box,
  Paper,
  Timeline,
  Text,
  Group,
  Alert,
  Button,
  Progress,
  Stack,
  ThemeIcon,
  Badge,
  Divider,
  LoadingOverlay
} from '@mantine/core';
import {
  IconCircleCheck,
  IconEye,
  IconUsers,
  IconFlag,
  IconPhone,
  IconInfoCircle,
  IconRefresh,
  IconAlertTriangle,
  IconClock,
  IconMail
} from '@tabler/icons-react';
import { useApplicationStatus } from '../../hooks/useApplicationStatus';
import type { ApplicationStatus } from '../../types/vetting.types';
import { APPLICATION_STATUS_CONFIGS } from '../../types/vetting.types';

interface ApplicationStatusProps {
  trackingToken?: string;
  onTokenNotFound?: () => void;
  className?: string;
}

export const ApplicationStatusComponent: React.FC<ApplicationStatusProps> = ({
  trackingToken,
  onTokenNotFound,
  className
}) => {
  const {
    statusData,
    isLoading,
    error,
    isComplete,
    needsAdditionalInfo,
    getStatusMessage,
    getTimeEstimate,
    getProgressPercentage,
    getCurrentPhase,
    refreshStatus
  } = useApplicationStatus(trackingToken);

  // Handle token not found
  React.useEffect(() => {
    if (error && onTokenNotFound) {
      onTokenNotFound();
    }
  }, [error, onTokenNotFound]);

  if (!trackingToken) {
    return (
      <Alert 
        icon={<IconInfoCircle />}
        color="blue"
        title="No Tracking Token"
      >
        <Text size="sm">
          Please provide a valid tracking token to check your application status.
        </Text>
      </Alert>
    );
  }

  if (isLoading) {
    return (
      <Paper p="xl" shadow="sm" pos="relative" className={className}>
        <LoadingOverlay visible />
        <Text ta="center" c="dimmed">
          Loading application status...
        </Text>
      </Paper>
    );
  }

  if (error || !statusData) {
    return (
      <Alert 
        icon={<IconAlertTriangle />}
        color="red"
        title="Unable to Load Status"
        className={className}
      >
        <Text size="sm" mb="md">
          We couldn't find an application with that tracking token, or there was an error loading your status.
        </Text>
        <Button 
          variant="outline" 
          size="sm"
          onClick={() => window.location.reload()}
        >
          Try Again
        </Button>
      </Alert>
    );
  }

  // Get status configuration
  const statusConfig = APPLICATION_STATUS_CONFIGS[(statusData as any)?.status as ApplicationStatus];

  // Timeline step mapping
  const getTimelineSteps = () => {
    const progress = (statusData as any)?.progress;
    
    return [
      {
        title: 'Application Submitted',
        description: `Submitted on ${new Date((statusData as any)?.submittedAt).toLocaleDateString()}`,
        completed: progress.applicationSubmitted,
        icon: IconCircleCheck,
        color: 'green'
      },
      {
        title: 'References Contacted',
        description: 'Reference verification in progress',
        completed: progress.referencesContacted,
        icon: IconUsers,
        color: 'blue'
      },
      {
        title: 'References Received',
        description: 'Reference responses collected',
        completed: progress.referencesReceived,
        icon: IconMail,
        color: 'cyan'
      },
      {
        title: 'Under Review',
        description: 'Being reviewed by vetting team',
        completed: progress.underReview,
        icon: IconEye,
        color: 'yellow'
      },
      {
        title: 'Interview Scheduled',
        description: 'Interview appointment confirmed (if required)',
        completed: progress.interviewScheduled,
        icon: IconPhone,
        color: 'orange',
        optional: true
      },
      {
        title: 'Decision Made',
        description: 'Final approval status',
        completed: progress.decisionMade,
        icon: IconFlag,
        color: isComplete ? ((statusData as any)?.status === 'approved' ? 'green' : 'red') : 'gray'
      }
    ].filter(step => !step.optional || step.completed);
  };

  const timelineSteps = getTimelineSteps();
  const activeStep = timelineSteps.findIndex(step => !step.completed);

  return (
    <Box className={className}>
      {/* Header */}
      <Paper p="xl" shadow="sm" mb="lg">
        <Stack gap="md">
          <Group justify="apart" align="flex-start">
            <Box>
              <Text size="xl" fw={700} c="wcr.7" mb="xs">
                Application Status
              </Text>
              <Text size="lg" c="dimmed">
                Application #{(statusData as any)?.applicationNumber}
              </Text>
            </Box>
            
            <Group>
              <Badge
                color={statusConfig?.color || 'gray'}
                size="lg"
                variant="filled"
                leftSection={statusConfig?.icon}
              >
                {statusConfig?.label || (statusData as any)?.status}
              </Badge>
              <Button
                variant="light"
                size="sm"
                leftSection={<IconRefresh size={14} />}
                onClick={refreshStatus}
              >
                Refresh
              </Button>
            </Group>
          </Group>

          {/* Progress Bar */}
          <Box>
            <Group justify="apart" mb="xs">
              <Text size="sm" fw={500}>
                Progress: {getCurrentPhase()}
              </Text>
              <Text size="sm" c="dimmed">
                {getProgressPercentage()}% Complete
              </Text>
            </Group>
            <Progress
              value={getProgressPercentage()}
              color={isComplete ? ((statusData as any)?.status === 'approved' ? 'green' : 'red') : 'blue'}
              size="lg"
              radius="md"
            />
          </Box>

          {/* Time Estimate */}
          {getTimeEstimate() && !isComplete && (
            <Group gap="xs">
              <IconClock size={16} color="gray" />
              <Text size="sm" c="dimmed">
                {getTimeEstimate()}
              </Text>
            </Group>
          )}
        </Stack>
      </Paper>

      {/* Current Status Message */}
      <Alert
        icon={statusConfig?.icon ? <statusConfig.icon /> : <IconInfoCircle />}
        color={statusConfig?.color || 'blue'}
        title={`Status: ${statusConfig?.label || (statusData as any)?.status}`}
        mb="lg"
      >
        <Text size="sm">
          {getStatusMessage()}
        </Text>
      </Alert>

      {/* Additional Info Needed Alert */}
      {needsAdditionalInfo && (
        <Alert
          icon={<IconAlertTriangle />}
          color="orange"
          title="Action Required"
          mb="lg"
        >
          <Text size="sm" mb="md">
            We need some additional information to continue processing your application. 
            Please check your email for specific details about what is needed.
          </Text>
          <Button size="sm" color="orange">
            Provide Additional Information
          </Button>
        </Alert>
      )}

      {/* Timeline */}
      <Paper p="xl" shadow="sm" mb="lg">
        <Text size="lg" fw={600} c="wcr.7" mb="xl">
          Application Timeline
        </Text>
        
        <Timeline
          active={activeStep >= 0 ? activeStep : timelineSteps.length - 1}
          color="wcr.7"
          bulletSize={32}
          lineWidth={3}
        >
          {timelineSteps.map((step, index) => (
            <Timeline.Item
              key={index}
              title={step.title}
              bullet={
                <ThemeIcon
                  color={step.completed ? step.color : 'gray'}
                  size={24}
                  radius="xl"
                >
                  <step.icon size={14} />
                </ThemeIcon>
              }
            >
              <Text size="sm" c="dimmed" mt={4}>
                {step.description}
              </Text>
            </Timeline.Item>
          ))}
        </Timeline>
      </Paper>

      {/* Recent Updates */}
      {(statusData as any)?.recentUpdates && (statusData as any)?.recentUpdates.length > 0 && (
        <Paper p="xl" shadow="sm" mb="lg">
          <Text size="lg" fw={600} c="wcr.7" mb="md">
            Recent Updates
          </Text>
          
          <Stack gap="md">
            {(statusData as any)?.recentUpdates.map((update, index) => (
              <Box key={index}>
                <Group gap="xs" mb="xs">
                  <Text size="sm" fw={500}>
                    {update.type.replace(/([A-Z])/g, ' $1').trim()}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {new Date(update.updatedAt).toLocaleDateString()}
                  </Text>
                </Group>
                <Text size="sm" c="dimmed">
                  {update.message}
                </Text>
                {index < (statusData as any)?.recentUpdates.length - 1 && <Divider mt="md" />}
              </Box>
            ))}
          </Stack>
        </Paper>
      )}

      {/* Final Status Messages */}
      {(statusData as any)?.status === 'approved' && (
        <Alert
          icon={<IconCircleCheck />}
          color="green"
          title="Welcome to WitchCityRope!"
          mb="lg"
        >
          <Text size="sm" mb="md">
            Congratulations! Your application has been approved. You should receive a welcome email 
            with information about accessing member-only resources and upcoming events.
          </Text>
          <Button color="green">
            View Member Resources
          </Button>
        </Alert>
      )}

      {(statusData as any)?.status === 'denied' && (
        <Alert
          icon={<IconAlertTriangle />}
          color="red"
          title="Application Decision"
          mb="lg"
        >
          <Text size="sm" mb="md">
            Your application was not approved at this time. If you have questions about this decision 
            or would like feedback, please contact our vetting team.
          </Text>
          <Group>
            <Button variant="outline" color="red" size="sm">
              Contact Vetting Team
            </Button>
            <Button variant="light" color="blue" size="sm">
              Learn About Reapplying
            </Button>
          </Group>
        </Alert>
      )}

      {/* Help Section */}
      <Paper p="lg" shadow="sm" bg="gray.0">
        <Text size="md" fw={600} mb="md">
          Need Help?
        </Text>
        <Text size="sm" c="dimmed" mb="md">
          If you have questions about your application status or the vetting process, 
          we're here to help.
        </Text>
        <Group>
          <Button variant="outline" size="sm">
            Contact Support
          </Button>
          <Button variant="light" size="sm">
            Vetting FAQ
          </Button>
        </Group>
      </Paper>

      {/* Last Updated */}
      <Group justify="center" mt="md">
        <Text size="xs" c="dimmed">
          Last updated: {(statusData as any)?.lastUpdateAt 
            ? new Date((statusData as any)?.lastUpdateAt).toLocaleString()
            : new Date((statusData as any)?.submittedAt).toLocaleString()
          }
        </Text>
      </Group>
    </Box>
  );
};