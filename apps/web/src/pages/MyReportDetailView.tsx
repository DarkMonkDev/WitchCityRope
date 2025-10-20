import React, { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Container,
  Stack,
  Alert,
  Text,
  Skeleton,
  rem,
  Card,
  Group,
  Title
} from '@mantine/core';
import {
  IconAlertCircle,
  IconArrowLeft,
  IconInfoCircle,
  IconMail
} from '@tabler/icons-react';
import { IncidentDetailHeader } from '../features/safety/components/IncidentDetailHeader';
import { IncidentDetailsCard } from '../features/safety/components/IncidentDetailsCard';
import { PeopleInvolvedCard } from '../features/safety/components/PeopleInvolvedCard';

// Mock data interface - will be replaced with actual API types
interface MyReportDetail {
  id: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';
  incidentDate: string;
  reportedAt: string;
  lastUpdatedAt: string;
  location: string;
  description: string;
  involvedParties?: string;
  witnesses?: string;
  isAnonymous: boolean;
}

// Mock data - will be replaced with API call
const MOCK_REPORT: MyReportDetail = {
  id: '1',
  severity: 'High',
  status: 'InformationGathering',
  incidentDate: '2025-10-15T19:30:00Z',
  reportedAt: '2025-10-16T08:00:00Z',
  lastUpdatedAt: '2025-10-17T14:30:00Z',
  location: 'Monthly Rope Jam - Main Studio',
  description: 'During a suspension demonstration, the bottom indicated discomfort but the top continued for several seconds before responding. The bottom felt their safeword was not immediately honored. No physical injury occurred, but the bottom felt unsafe and left the event early.',
  involvedParties: 'Top: "RopeExpert123"\nBottom: "FirstTimer" (reporter)',
  witnesses: 'DM Assistant who was spotting the scene\nAnother attendee who was nearby',
  isAnonymous: false
};

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  });
};

const getStatusExplanation = (status: string): string => {
  switch (status) {
    case 'ReportSubmitted':
      return 'Your report has been received and is awaiting initial review by our safety team. You may be contacted if additional information is needed.';
    case 'InformationGathering':
      return 'Your report is currently being reviewed by our safety team. Additional information may be gathered from relevant parties.';
    case 'ReviewingFinalReport':
      return 'The investigation is complete and the final report is being prepared. You will be notified when the incident is closed.';
    case 'OnHold':
      return 'Your report is temporarily on hold pending additional information or external processes. It will resume when possible.';
    case 'Closed':
      return 'This incident has been resolved and the case is closed. Thank you for bringing this to our attention.';
    default:
      return 'Your report is being processed by our safety team.';
  }
};

export const MyReportDetailView: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [isLoading] = useState(false);
  const [error] = useState<string | null>(null);

  // TODO: Replace with actual API call
  const report = MOCK_REPORT;

  // TODO: Add authentication check
  // Verify that the current user is the reporter (reporterId matches)
  // If not, redirect to unauthorized or my-reports page

  if (!id) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Invalid Report">
          <Text>No report ID provided in URL.</Text>
        </Alert>
      </Container>
    );
  }

  if (isLoading) {
    return (
      <Container size="lg" py="xl">
        <Stack gap="lg">
          <Skeleton height={100} />
          <Skeleton height={300} />
          <Skeleton height={200} />
        </Stack>
      </Container>
    );
  }

  if (error) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Report">
          <Text>{error}</Text>
          <Link to="/my-reports" style={{ color: '#880124', textDecoration: 'underline' }}>
            Back to My Reports
          </Link>
        </Alert>
      </Container>
    );
  }

  if (!report) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle />} color="yellow" title="Report Not Found">
          <Text>This report could not be found or you do not have permission to view it.</Text>
          <Link to="/my-reports" style={{ color: '#880124', textDecoration: 'underline' }}>
            Back to My Reports
          </Link>
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="lg" py="xl">
      <Stack gap="lg">
        {/* Back Link */}
        <Link
          to="/my-reports"
          style={{
            textDecoration: 'none',
            color: '#880124',
            display: 'flex',
            alignItems: 'center',
            gap: rem(8),
            fontSize: '14px',
            fontWeight: 600
          }}
        >
          <IconArrowLeft size={16} />
          Back to My Reports
        </Link>

        {/* Header - NO reference number, NO coordinator */}
        <Stack gap="md">
          <Title order={1} style={{ color: '#880124' }}>
            Your Safety Report
          </Title>

          <Group gap="xl" wrap="wrap">
            <div>
              <Text size="xs" c="dimmed" mb={4}>Reported</Text>
              <Text size="sm" fw={600}>{formatDate(report.reportedAt)}</Text>
            </div>
            <div>
              <Text size="xs" c="dimmed" mb={4}>Last Updated</Text>
              <Text size="sm" fw={600}>{formatDate(report.lastUpdatedAt)}</Text>
            </div>
          </Group>
        </Stack>

        {/* Status Explanation */}
        <Alert color="blue" icon={<IconInfoCircle />}>
          <Text fw={600} mb="xs">Current Status: {report.status.replace(/([A-Z])/g, ' $1').trim()}</Text>
          <Text size="sm">{getStatusExplanation(report.status)}</Text>
        </Alert>

        {/* Incident Details - User's submitted information only */}
        <IncidentDetailsCard
          description={report.description}
          isAnonymous={report.isAnonymous}
          reporterName={report.isAnonymous ? undefined : 'You (identified report)'}
          requestedFollowUp={!report.isAnonymous}
          createdAt={report.reportedAt}
          updatedAt={report.lastUpdatedAt}
        />

        {/* People Involved - Only what user submitted */}
        {(report.involvedParties || report.witnesses) && (
          <PeopleInvolvedCard
            involvedParties={report.involvedParties}
            witnesses={report.witnesses}
          />
        )}

        {/* Contact Information Card */}
        <Card p="xl">
          <Group gap="md" mb="md">
            <IconMail size={24} style={{ color: '#880124' }} />
            <Title order={3} style={{ color: '#880124' }}>
              Need to Provide Additional Information?
            </Title>
          </Group>

          <Text size="sm" mb="md">
            If you need to provide additional details or have questions about this report, please contact our safety team:
          </Text>

          <Alert color="purple" icon={<IconMail />}>
            <Text size="sm" fw={600}>
              Email: <a href="mailto:safety@witchcityrope.com" style={{ color: '#880124' }}>safety@witchcityrope.com</a>
            </Text>
            <Text size="xs" c="dimmed" mt="xs">
              Please reference the date of your report ({formatDate(report.reportedAt)}) when contacting us.
            </Text>
          </Alert>
        </Card>

        {/* Important Notice */}
        <Alert color="gray" icon={<IconInfoCircle />}>
          <Text size="sm">
            This is a limited view of your report. For privacy and safety reasons, coordinator information and internal notes are not displayed.
          </Text>
        </Alert>
      </Stack>
    </Container>
  );
};
