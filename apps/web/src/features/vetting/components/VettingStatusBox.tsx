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
          <Badge color={config.color} size="lg" variant="light">
            {applicationNumber}
          </Badge>
        </Group>

        {/* Status Description */}
        <Box>
          <Text size="md" c="#4A4A4A">
            {statusDescription}
          </Text>
        </Box>

        {/* Dates */}
        <Group gap="xl">
          <Box>
            <Text size="xs" c="#8B8680" tt="uppercase" fw={600}>
              Submitted:
            </Text>
            <Text size="sm" c="#2B2B2B">
              {submittedAt.toLocaleDateString(undefined, {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
              })}
            </Text>
          </Box>
          <Box>
            <Text size="xs" c="#8B8680" tt="uppercase" fw={600}>
              Last Updated:
            </Text>
            <Text size="sm" c="#2B2B2B">
              {lastUpdated.toLocaleDateString(undefined, {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
              })}
            </Text>
          </Box>
        </Group>

        {/* Next Steps */}
        {nextSteps && (
          <Box
            p="md"
            style={{
              backgroundColor: '#FAF6F2',
              borderRadius: '8px'
            }}
          >
            <Text size="xs" c="#8B8680" tt="uppercase" fw={600} mb="xs">
              Next Steps:
            </Text>
            <Text size="sm" c="#2B2B2B">
              {nextSteps}
            </Text>
          </Box>
        )}

        {/* Estimated Time Remaining */}
        {estimatedDaysRemaining !== undefined && estimatedDaysRemaining !== null && (
          <Group gap="xs">
            <Clock size={16} color="#8B8680" />
            <Text size="sm" c="#8B8680">
              Estimated: {estimatedDaysRemaining} days remaining
            </Text>
          </Group>
        )}
      </Stack>
    </Paper>
  );
};
