import React, { useState } from 'react';
import {
  Container,
  Stack,
  Title,
  Text,
  Alert,
  SimpleGrid,
  Skeleton,
  Button,
  Box
} from '@mantine/core';
import { IconInfoCircle, IconFileOff } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { MyReportCard } from '../features/safety/components/MyReportCard';

// Mock data interface - will be replaced with actual API types
interface MyReportSummary {
  id: string;
  incidentDate: string;
  location: string;
  status: 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';
  reportedAt: string;
  lastUpdatedAt: string;
}

// Mock data - will be replaced with API call
const MOCK_REPORTS: MyReportSummary[] = [
  {
    id: '1',
    incidentDate: '2025-10-15T19:30:00Z',
    location: 'Monthly Rope Jam - Main Studio',
    status: 'InformationGathering',
    reportedAt: '2025-10-16T08:00:00Z',
    lastUpdatedAt: '2025-10-17T14:30:00Z'
  },
  {
    id: '2',
    incidentDate: '2025-09-28T18:00:00Z',
    location: 'Workshop Series - Advanced Techniques',
    status: 'Closed',
    reportedAt: '2025-09-29T09:15:00Z',
    lastUpdatedAt: '2025-10-12T16:45:00Z'
  },
  {
    id: '3',
    incidentDate: '2025-08-10T20:00:00Z',
    location: 'Social Event - Summer Mixer',
    status: 'Closed',
    reportedAt: '2025-08-11T10:00:00Z',
    lastUpdatedAt: '2025-08-25T14:00:00Z'
  }
];

export const MyReportsPage: React.FC = () => {
  // TODO: Replace with actual API call and auth check
  const [reports] = useState<MyReportSummary[]>(MOCK_REPORTS);
  const [isLoading] = useState(false);

  // TODO: Add authentication check here
  // If user is not authenticated or is anonymous/guest, redirect to login

  return (
    <Container size="lg" py="xl">
      <Stack gap="lg">
        {/* Page Header */}
        <div>
          <Title order={1} style={{ color: '#880124', marginBottom: '8px' }}>
            My Safety Reports
          </Title>
          <Text c="dimmed">
            View the status of incidents you've reported
          </Text>
        </div>

        {/* Info Alert */}
        <Alert color="blue" icon={<IconInfoCircle />}>
          You can view reports you submitted while logged in. Anonymous reports cannot be tracked.
        </Alert>

        {/* Loading State */}
        {isLoading && (
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} height={300} radius="md" />
            ))}
          </SimpleGrid>
        )}

        {/* Empty State */}
        {!isLoading && reports.length === 0 && (
          <Box p="xl" style={{ textAlign: 'center' }}>
            <IconFileOff
              size={48}
              style={{ color: '#868e96', marginBottom: '16px' }}
            />
            <Text size="lg" c="dimmed" mb="xs">
              You haven't submitted any reports yet
            </Text>
            <Text size="sm" c="dimmed" mb="md">
              If you need to report a safety incident, you can do so using the form below.
            </Text>
            <Button
              variant="light"
              component={Link}
              to="/safety/report"
              styles={{
                root: {
                  minHeight: 40,
                  height: 'auto',
                  padding: '10px 20px',
                  lineHeight: 1.4
                }
              }}
            >
              Report an Incident
            </Button>
          </Box>
        )}

        {/* Reports Grid */}
        {!isLoading && reports.length > 0 && (
          <SimpleGrid
            cols={{ base: 1, sm: 2, lg: 3 }}
            spacing="md"
            data-testid="reports-grid"
          >
            {reports.map((report) => (
              <MyReportCard
                key={report.id}
                id={report.id}
                incidentDate={report.incidentDate}
                location={report.location}
                status={report.status}
                reportedAt={report.reportedAt}
                lastUpdatedAt={report.lastUpdatedAt}
              />
            ))}
          </SimpleGrid>
        )}

        {/* TODO: Add pagination if more than 12 reports */}
      </Stack>
    </Container>
  );
};
