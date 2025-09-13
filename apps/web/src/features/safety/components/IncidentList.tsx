// Incident List Component
// Table display of safety incidents with pagination and actions

import React from 'react';
import {
  Table,
  Badge,
  Group,
  ActionIcon,
  Text,
  Box,
  Pagination,
  Stack,
  Tooltip,
  Button
} from '@mantine/core';
import { IconEye, IconUser, IconUserOff } from '@tabler/icons-react';
import { 
  IncidentSummaryDto, 
  SEVERITY_CONFIGS, 
  STATUS_CONFIGS 
} from '../types/safety.types';

interface IncidentListProps {
  incidents: IncidentSummaryDto[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onIncidentSelect: (incidentId: string) => void;
  loading?: boolean;
}

export function IncidentList({
  incidents,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  onIncidentSelect,
  loading = false
}: IncidentListProps) {
  
  const totalPages = Math.ceil(totalCount / pageSize);
  
  const formatDate = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };
  
  const formatTime = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };
  
  if (incidents.length === 0 && !loading) {
    return (
      <Box ta="center" p="xl">
        <Text c="dimmed" size="lg">
          No incidents found
        </Text>
        <Text c="dimmed" size="sm">
          Try adjusting your search criteria or filters
        </Text>
      </Box>
    );
  }
  
  return (
    <Stack gap="md">
      {/* Table */}
      <Box style={{ overflowX: 'auto' }}>
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Reference</Table.Th>
              <Table.Th>Date</Table.Th>
              <Table.Th>Severity</Table.Th>
              <Table.Th>Location</Table.Th>
              <Table.Th>Status</Table.Th>
              <Table.Th>Reporter</Table.Th>
              <Table.Th>Assigned</Table.Th>
              <Table.Th>Actions</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {incidents.map((incident) => {
              const severityConfig = SEVERITY_CONFIGS[incident.severity];
              const statusConfig = STATUS_CONFIGS[incident.status];
              
              return (
                <Table.Tr key={incident.id}>
                  {/* Reference Number */}
                  <Table.Td>
                    <Text size="sm" fw={500} c="blue">
                      {incident.referenceNumber}
                    </Text>
                  </Table.Td>
                  
                  {/* Date */}
                  <Table.Td>
                    <Box>
                      <Text size="sm">{formatDate(incident.incidentDate)}</Text>
                      <Text size="xs" c="dimmed">{formatTime(incident.incidentDate)}</Text>
                    </Box>
                  </Table.Td>
                  
                  {/* Severity */}
                  <Table.Td>
                    <Badge
                      color={severityConfig.color}
                      variant="filled"
                      size="sm"
                      leftSection={<span style={{ fontSize: '10px' }}>{severityConfig.icon}</span>}
                    >
                      {severityConfig.label}
                    </Badge>
                  </Table.Td>
                  
                  {/* Location */}
                  <Table.Td>
                    <Text size="sm" style={{ maxWidth: '150px' }} truncate>
                      {incident.location}
                    </Text>
                  </Table.Td>
                  
                  {/* Status */}
                  <Table.Td>
                    <Badge
                      color={statusConfig.color}
                      variant="light"
                      size="sm"
                    >
                      {statusConfig.label}
                    </Badge>
                  </Table.Td>
                  
                  {/* Reporter Type */}
                  <Table.Td>
                    <Group gap="xs">
                      {incident.isAnonymous ? (
                        <Tooltip label="Anonymous report">
                          <Group gap={4}>
                            <IconUserOff size={14} color="#9CA3AF" />
                            <Text size="sm" c="dimmed">Anonymous</Text>
                          </Group>
                        </Tooltip>
                      ) : (
                        <Tooltip label="Identified report">
                          <Group gap={4}>
                            <IconUser size={14} color="#059669" />
                            <Text size="sm" c="green.7">Identified</Text>
                          </Group>
                        </Tooltip>
                      )}
                    </Group>
                  </Table.Td>
                  
                  {/* Assigned To */}
                  <Table.Td>
                    {incident.assignedUserName ? (
                      <Text size="sm">{incident.assignedUserName}</Text>
                    ) : (
                      <Text size="sm" c="dimmed" fs="italic">Unassigned</Text>
                    )}
                  </Table.Td>
                  
                  {/* Actions */}
                  <Table.Td>
                    <Tooltip label="View details">
                      <ActionIcon
                        variant="light"
                        color="blue"
                        onClick={() => onIncidentSelect(incident.id)}
                      >
                        <IconEye size={16} />
                      </ActionIcon>
                    </Tooltip>
                  </Table.Td>
                </Table.Tr>
              );
            })}
          </Table.Tbody>
        </Table>
      </Box>
      
      {/* Pagination */}
      {totalPages > 1 && (
        <Group justify="space-between" align="center">
          <Text size="sm" c="dimmed">
            Showing {Math.min((currentPage - 1) * pageSize + 1, totalCount)} to{' '}
            {Math.min(currentPage * pageSize, totalCount)} of {totalCount} incidents
          </Text>
          
          <Pagination
            value={currentPage}
            onChange={onPageChange}
            total={totalPages}
            size="sm"
            withEdges
          />
        </Group>
      )}
    </Stack>
  );
}