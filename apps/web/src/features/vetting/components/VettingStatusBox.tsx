/**
 * VettingStatusBox Component
 * Displays current vetting application status with visual indicators
 *
 * Uses Mantine v7 components for consistent styling
 */
import React from 'react';
import { Paper, Stack, Badge, Text, Group, Box } from '@mantine/core';
import {
  Clock,
  CheckCircle,
  XCircle,
  AlertCircle,
  Calendar,
  FileText,
  Pause
} from 'lucide-react';
import type { VettingStatus } from '../types/vettingStatus';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

export interface VettingStatusBoxProps {
  status: VettingStatus;
  applicationNumber: string;
  submittedAt: Date;
  lastUpdated: Date;
  statusDescription: string;
  nextSteps?: string;
  estimatedDaysRemaining?: number;
}

/**
 * Status configuration for visual styling
 */
const statusConfig: Record<
  VettingStatus,
  {
    displayName: string;
    color: string;
    icon: React.ElementType;
  }
> = {
  UnderReview: {
    displayName: 'Under Review',
    color: 'indigo',
    icon: Clock
  },
  InterviewApproved: {
    displayName: 'Awaiting Interview',
    color: 'teal',
    icon: CheckCircle
  },
  FinalReview: {
    displayName: 'Final Review',
    color: 'blue',
    icon: FileText
  },
  Approved: {
    displayName: 'Approved',
    color: 'green',
    icon: CheckCircle
  },
  Denied: {
    displayName: 'Denied',
    color: 'red',
    icon: XCircle
  },
  OnHold: {
    displayName: 'On Hold',
    color: 'yellow',
    icon: Pause
  },
  Withdrawn: {
    displayName: 'Withdrawn',
    color: 'gray',
    icon: AlertCircle
  }
};

/**
 * VettingStatusBox Component
 *
 * Displays comprehensive status information for a vetting application
 * with visual indicators, dates, and next steps.
 *
 * @example
 * ```typescript
 * <VettingStatusBox
 *   status="UnderReview"
 *   applicationNumber="V-2025-001"
 *   submittedAt={new Date()}
 *   lastUpdated={new Date()}
 *   statusDescription="Application received and under review"
 *   nextSteps="Waiting for initial review"
 *   estimatedDaysRemaining={7}
 * />
 * ```
 */
export const VettingStatusBox: React.FC<VettingStatusBoxProps> = ({
  status,
  applicationNumber,
  submittedAt,
  lastUpdated,
  statusDescription,
  nextSteps,
  estimatedDaysRemaining
}) => {
  const config = statusConfig[status];
  const Icon = config.icon;
  const eventTimeZone = useEventTimeZone();

  // Format submitted date as "Submitted - Nov 4, 2025"
  const formattedSubmittedDate = `Submitted - ${submittedAt.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    timeZone: eventTimeZone
  })}`;

  // Use simplified layout for all statuses
  return (
    <Paper
      p="lg"
      radius="md"
      withBorder
      style={{
        borderColor: '#880124',
        borderWidth: '2px'
      }}
    >
      <Stack gap="md">
        {/* Status Header */}
        <Group justify="space-between" align="center">
          <Group gap="sm">
            <Icon size={24} color="#880124" />
            <Text size="xl" fw={700} c="#2B2B2B">
              {config.displayName}
            </Text>
          </Group>
          <Text size="sm" c="#8B8680" fw={500}>
            {formattedSubmittedDate}
          </Text>
        </Group>

        {/* Status Description with Next Steps */}
        <Box>
          <Text size="md" c="#4A4A4A">
            {statusDescription} {nextSteps}
          </Text>
        </Box>
      </Stack>
    </Paper>
  );
};
