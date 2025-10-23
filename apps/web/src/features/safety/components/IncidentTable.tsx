import React, { useCallback } from 'react';
import {
  Table,
  Text,
  Group,
  ActionIcon,
  Menu,
  Skeleton,
  Box,
  Button
} from '@mantine/core';
import {
  IconDots,
  IconUserPlus,
  IconEye,
  IconClock,
  IconCheck,
  IconX
} from '@tabler/icons-react';
import { IncidentStatusBadge } from './IncidentStatusBadge';
import type { IncidentSummaryDto } from '../types/safety.types';

interface IncidentTableProps {
  incidents: IncidentSummaryDto[];
  isLoading?: boolean;
  onRowClick?: (incidentId: string) => void;
  onAssign?: (incidentId: string) => void;
  onPutOnHold?: (incidentId: string) => void;
  onClose?: (incidentId: string) => void;
  onClearFilters?: () => void;
}

/**
 * Incident Table Component
 *
 * Pattern Source: VettingReviewGrid.tsx table section
 *
 * Displays incident data in tabular format with:
 * - Reference Number (clickable)
 * - Status Badge (color-coded)
 * - Assigned To (coordinator name or "Unassigned")
 * - Last Updated (relative time with aging indicator)
 * - Actions Menu (quick actions dropdown)
 *
 * Aging indicators:
 * - 0-3 days: Normal (transparent background)
 * - 4-7 days: Warning (yellow tint, amber text)
 * - 8+ days: Alert (red tint, red text)
 */
export const IncidentTable: React.FC<IncidentTableProps> = ({
  incidents,
  isLoading = false,
  onRowClick,
  onAssign,
  onPutOnHold,
  onClose,
  onClearFilters
}) => {
  // Calculate days since update for aging indicator
  const getDaysOld = useCallback((updatedAt?: string) => {
    if (!updatedAt) return 0;
    const days = Math.floor(
      (new Date().getTime() - new Date(updatedAt).getTime()) / (1000 * 60 * 60 * 24)
    );
    return days;
  }, []);

  // Get row background based on age
  const getRowBackground = useCallback((daysOld: number) => {
    if (daysOld > 7) return 'rgba(220, 53, 69, 0.05)';  // Red tint
    if (daysOld > 3) return 'rgba(255, 193, 7, 0.05)';  // Yellow tint
    return 'transparent';
  }, []);

  // Format relative time
  const formatRelativeTime = useCallback((dateString?: string) => {
    if (!dateString) return 'Unknown';
    const days = getDaysOld(dateString);
    if (days === 0) return 'Today';
    if (days === 1) return '1 day ago';
    if (days < 7) return `${days} days ago`;
    if (days < 14) return '1 week ago';
    if (days < 30) return `${Math.floor(days / 7)} weeks ago`;
    if (days < 60) return '1 month ago';
    return `${Math.floor(days / 30)} months ago`;
  }, [getDaysOld]);

  // Loading state
  if (isLoading) {
    return (
      <>
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} height={60} mb="sm" />
        ))}
      </>
    );
  }

  // Empty state
  if (incidents.length === 0) {
    return (
      <Box p="xl" ta="center">
        <Text c="dimmed" size="lg" mb="xs">
          🔍 No incidents match your filters
        </Text>
        <Text c="dimmed" size="sm" mb="md">
          Try adjusting your filters or search terms.
        </Text>
        {onClearFilters && (
          <Button
            variant="light"
            onClick={onClearFilters}
            leftSection={<IconX size={16} />}
          >
            Clear Filters
          </Button>
        )}
      </Box>
    );
  }

  return (
    <Table striped highlightOnHover>
      {/* Header - Mirroring VettingReviewGrid pattern */}
      <Table.Thead style={{ backgroundColor: '#880124' }}>
        <Table.Tr>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Reference
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Status
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Coordinator
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Last Updated
            </Text>
          </Table.Th>
          <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none', width: 100 }}>
            <Text fw={600} size="sm" c="white" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Actions
            </Text>
          </Table.Th>
        </Table.Tr>
      </Table.Thead>

      {/* Body */}
      <Table.Tbody>
        {incidents.map((incident) => {
          const daysOld = getDaysOld(incident.lastUpdatedAt);
          const rowBg = getRowBackground(daysOld);

          return (
            <Table.Tr
              key={incident.id}
              onClick={() => onRowClick?.(incident.id!)}
              style={{
                cursor: 'pointer',
                backgroundColor: rowBg
              }}
            >
              {/* Reference Number */}
              <Table.Td>
                <Text size="sm" fw={600} style={{ color: '#880124' }}>
                  {incident.referenceNumber || 'N/A'}
                </Text>
              </Table.Td>

              {/* Status Badge */}
              <Table.Td>
                <IncidentStatusBadge status={incident.status!} size="sm" />
              </Table.Td>

              {/* Assigned To */}
              <Table.Td>
                {incident.coordinatorName ? (
                  <Text size="sm" style={{ color: '#2B2B2B' }}>
                    {incident.coordinatorName}
                  </Text>
                ) : (
                  <Text c="dimmed" fw={600} size="sm">
                    Unassigned
                  </Text>
                )}
              </Table.Td>

              {/* Last Updated with aging indicator */}
              <Table.Td>
                <Text
                  size="sm"
                  style={{
                    color: daysOld > 7 ? '#AA0130' : daysOld > 3 ? '#E6AC00' : '#2B2B2B',
                    fontWeight: daysOld > 3 ? 600 : 400
                  }}
                >
                  {formatRelativeTime(incident.lastUpdatedAt)}
                </Text>
              </Table.Td>

              {/* Actions Menu */}
              <Table.Td onClick={(e) => e.stopPropagation()}>
                <Menu shadow="md" width={200}>
                  <Menu.Target>
                    <ActionIcon variant="subtle" color="gray">
                      <IconDots size={16} />
                    </ActionIcon>
                  </Menu.Target>

                  <Menu.Dropdown>
                    <Menu.Label>Quick Actions</Menu.Label>
                    <Menu.Item
                      leftSection={<IconUserPlus size={14} />}
                      onClick={() => onAssign?.(incident.id!)}
                    >
                      {incident.coordinatorId ? 'Reassign' : 'Assign'} Coordinator
                    </Menu.Item>
                    <Menu.Item
                      leftSection={<IconEye size={14} />}
                      onClick={() => onRowClick?.(incident.id!)}
                    >
                      View Details
                    </Menu.Item>
                    <Menu.Divider />
                    <Menu.Item
                      leftSection={<IconClock size={14} />}
                      onClick={() => onPutOnHold?.(incident.id!)}
                    >
                      Put On Hold
                    </Menu.Item>
                    <Menu.Item
                      leftSection={<IconCheck size={14} />}
                      onClick={() => onClose?.(incident.id!)}
                      color="green"
                    >
                      Close Incident
                    </Menu.Item>
                  </Menu.Dropdown>
                </Menu>
              </Table.Td>
            </Table.Tr>
          );
        })}
      </Table.Tbody>
    </Table>
  );
};
