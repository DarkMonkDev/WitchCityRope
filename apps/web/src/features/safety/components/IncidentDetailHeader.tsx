import React from 'react';
import { Group, Stack, Text, Badge, rem } from '@mantine/core';
import { IconArrowLeft, IconLock, IconUser } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { IncidentStatusBadge } from './IncidentStatusBadge';

interface IncidentDetailHeaderProps {
  referenceNumber: string;
  status: 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';
  reportedDate: string;
  incidentDate: string;
  location: string;
  coordinatorName?: string;
  isAnonymous: boolean;
  viewMode?: 'admin' | 'user'; // Default: 'admin'
}

const formatDate = (date: string): string => {
  const d = new Date(date);
  return d.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
};

const getDaysAgo = (date: string): string => {
  const now = new Date();
  const then = new Date(date);
  const days = Math.floor((now.getTime() - then.getTime()) / (1000 * 60 * 60 * 24));

  if (days === 0) return 'today';
  if (days === 1) return '1 day ago';
  return `${days} days ago`;
};

export const IncidentDetailHeader: React.FC<IncidentDetailHeaderProps> = ({
  referenceNumber,
  status,
  reportedDate,
  incidentDate,
  location,
  coordinatorName,
  isAnonymous,
  viewMode = 'admin'
}) => {
  const isUserView = viewMode === 'user';

  return (
    <Stack gap="md" mb="lg">
      {/* Back Link */}
      <Link
        to={isUserView ? "/my-reports" : "/admin/incident-management"}
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
        {isUserView ? 'Back to My Reports' : 'Back to Dashboard'}
      </Link>

      {/* Main Header */}
      <Group justify="space-between" align="flex-start" wrap="wrap">
        <Stack gap="xs">
          <IncidentStatusBadge status={status} size="lg" />

          {/* Reference number only shown in admin view */}
          {!isUserView && (
            <Text size="xl" fw={700} style={{ color: '#880124' }}>
              {referenceNumber}
            </Text>
          )}
        </Stack>

        <Stack gap="xs" align="flex-end">
          <Text size="sm" c="dimmed">
            Reported: {formatDate(reportedDate)} ({getDaysAgo(reportedDate)})
          </Text>
          {/* Coordinator info only shown in admin view */}
          {!isUserView && (
            <>
              {coordinatorName ? (
                <Group gap="xs">
                  <IconUser size={16} style={{ color: '#880124' }} />
                  <Text size="sm" fw={600}>
                    Coordinator: {coordinatorName}
                  </Text>
                </Group>
              ) : (
                <Badge color="gray" variant="light" leftSection={<IconUser size={12} />}>
                  Unassigned
                </Badge>
              )}
            </>
          )}
        </Stack>
      </Group>

      {/* Incident Details Row */}
      <Group gap="xl" wrap="wrap">
        <div>
          <Text size="xs" c="dimmed" mb={4}>Incident Date</Text>
          <Text size="sm" fw={600}>{formatDate(incidentDate)}</Text>
        </div>

        <div>
          <Text size="xs" c="dimmed" mb={4}>Location</Text>
          <Text size="sm" fw={600}>{location}</Text>
        </div>

        <div>
          <Text size="xs" c="dimmed" mb={4}>Report Type</Text>
          {isAnonymous ? (
            <Badge color="gray" leftSection={<IconLock size={12} />}>
              Anonymous
            </Badge>
          ) : (
            <Badge color="blue" leftSection={<IconUser size={12} />}>
              Identified
            </Badge>
          )}
        </div>
      </Group>
    </Stack>
  );
};
