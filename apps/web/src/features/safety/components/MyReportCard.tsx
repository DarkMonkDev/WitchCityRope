import React from 'react';
import { Paper, Stack, Group, Text, Button, rem } from '@mantine/core';
import { IconArrowRight } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import { IncidentStatusBadge } from './IncidentStatusBadge';

interface MyReportCardProps {
  id: string;
  incidentDate: string;
  location: string;
  status: 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';
  reportedAt: string;
  lastUpdatedAt: string;
}

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
};

const getDaysAgo = (dateString: string): string => {
  const now = new Date();
  const then = new Date(dateString);
  const days = Math.floor((now.getTime() - then.getTime()) / (1000 * 60 * 60 * 24));

  if (days === 0) return 'today';
  if (days === 1) return '1 day ago';
  return `${days} days ago`;
};

const getStatusMessage = (status: string): string => {
  switch (status) {
    case 'ReportSubmitted':
      return 'Your report is awaiting review';
    case 'InformationGathering':
      return 'Your report is being reviewed by our safety team';
    case 'ReviewingFinalReport':
      return 'Your report is being finalized';
    case 'OnHold':
      return 'Your report requires additional review';
    case 'Closed':
      return 'This report has been resolved';
    default:
      return 'Your report is being processed';
  }
};

export const MyReportCard: React.FC<MyReportCardProps> = ({
  id,
  incidentDate,
  location,
  status,
  reportedAt,
  lastUpdatedAt
}) => {
  const navigate = useNavigate();

  const handleViewDetails = () => {
    // Use setTimeout to defer navigation (React Router pattern from lessons learned)
    setTimeout(() => {
      navigate(`/my-reports/${id}`);
    }, 0);
  };

  return (
    <Paper
      p="lg"
      mb="md"
      style={{
        border: '1px solid #E0E0E0',
        borderRadius: '8px',
        transition: 'box-shadow 0.2s ease',
        cursor: 'pointer'
      }}
      onClick={handleViewDetails}
      onMouseEnter={(e) => {
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.boxShadow = 'none';
      }}
      data-testid="my-report-card"
    >
      <Stack gap="md">
        {/* Status Badge */}
        <Group justify="space-between" align="center">
          <IncidentStatusBadge status={status} size="md" />
          <Text size="sm" c="dimmed">
            {getDaysAgo(lastUpdatedAt)}
          </Text>
        </Group>

        {/* Incident Date (prominent) */}
        <div>
          <Text size="xs" c="dimmed" mb={4}>
            Incident Date
          </Text>
          <Text size="lg" fw={700} style={{ color: '#880124' }}>
            {formatDate(incidentDate)}
          </Text>
        </div>

        {/* Location */}
        <div>
          <Text size="xs" c="dimmed" mb={4}>
            Location
          </Text>
          <Text size="sm" fw={600}>
            {location}
          </Text>
        </div>

        {/* Reported Date */}
        <div>
          <Text size="xs" c="dimmed" mb={4}>
            Reported
          </Text>
          <Text size="sm">
            {formatDate(reportedAt)}
          </Text>
        </div>

        {/* Status Message */}
        <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
          {getStatusMessage(status)}
        </Text>

        {/* View Details Button */}
        <Button
          variant="light"
          fullWidth
          rightSection={<IconArrowRight size={16} />}
          onClick={(e) => {
            e.stopPropagation();
            handleViewDetails();
          }}
          styles={{
            root: {
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4
            }
          }}
        >
          View Details
        </Button>
      </Stack>
    </Paper>
  );
};
