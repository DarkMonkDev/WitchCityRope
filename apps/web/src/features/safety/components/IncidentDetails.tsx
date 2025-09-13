// Incident Details Component
// Detailed view of a safety incident for admin management

import React, { useState } from 'react';
import {
  Box,
  Paper,
  Title,
  Text,
  Group,
  Stack,
  Badge,
  Button,
  Select,
  Textarea,
  Alert,
  Timeline,
  Divider,
  Grid,
  Loader,
  ActionIcon,
  Tooltip
} from '@mantine/core';
import {
  IconAlertTriangle,
  IconUser,
  IconUserOff,
  IconClock,
  IconMapPin,
  IconEdit,
  IconCheck,
  IconX,
  IconRefresh
} from '@tabler/icons-react';
import { useForm } from '@mantine/form';
import { useIncidentDetail, useUpdateIncident } from '../hooks/useSafetyIncidents';
import { 
  IncidentStatus, 
  UpdateIncidentRequest, 
  SEVERITY_CONFIGS, 
  STATUS_CONFIGS 
} from '../types/safety.types';

interface IncidentDetailsProps {
  incidentId: string;
  onClose: () => void;
}

export function IncidentDetails({ incidentId, onClose }: IncidentDetailsProps) {
  const [editMode, setEditMode] = useState(false);
  
  // Fetch incident details
  const { 
    data: incident, 
    isLoading, 
    error, 
    refetch 
  } = useIncidentDetail(incidentId);
  
  // Update incident mutation
  const updateIncidentMutation = useUpdateIncident();
  
  // Form for updating incident
  const form = useForm<UpdateIncidentRequest>({
    initialValues: {
      status: incident?.status,
      assignedTo: incident?.assignedTo || '',
      notes: ''
    }
  });
  
  // Update form when incident data loads
  React.useEffect(() => {
    if (incident) {
      form.setValues({
        status: incident.status,
        assignedTo: incident.assignedTo || '',
        notes: ''
      });
    }
  }, [incident]);
  
  const handleUpdate = async (values: UpdateIncidentRequest) => {
    if (!incident) return;
    
    try {
      await updateIncidentMutation.mutateAsync({
        incidentId: incident.id,
        request: values
      });
      setEditMode(false);
      form.setFieldValue('notes', ''); // Clear notes after successful update
    } catch (error) {
      console.error('Failed to update incident:', error);
    }
  };
  
  const formatDateTime = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };
  
  if (isLoading) {
    return (
      <Group justify="center" p="xl">
        <Loader size="lg" />
      </Group>
    );
  }
  
  if (error || !incident) {
    return (
      <Alert variant="light" color="red" icon={<IconAlertTriangle size={16} />}>
        <Text size="sm">
          {error instanceof Error && error.message.includes('403')
            ? 'Access denied - insufficient permissions'
            : 'Failed to load incident details. Please try again.'}
        </Text>
      </Alert>
    );
  }
  
  const severityConfig = SEVERITY_CONFIGS[incident.severity];
  const statusConfig = STATUS_CONFIGS[incident.status];
  
  return (
    <Stack gap="lg">
      {/* Header */}
      <Group justify="space-between" align="flex-start">
        <Box>
          <Group gap="md" align="center" mb="xs">
            <Title order={2} size="h3">
              {incident.referenceNumber}
            </Title>
            <Badge
              color={severityConfig.color}
              variant="filled"
              size="lg"
              leftSection={<span style={{ fontSize: '12px' }}>{severityConfig.icon}</span>}
            >
              {severityConfig.label}
            </Badge>
            <Badge color={statusConfig.color} variant="light" size="lg">
              {statusConfig.label}
            </Badge>
          </Group>
          <Text c="dimmed" size="sm">
            Reported on {formatDateTime(incident.reportedAt)}
          </Text>
        </Box>
        
        <Group>
          <Tooltip label="Refresh">
            <ActionIcon variant="light" onClick={() => refetch()}>
              <IconRefresh size={16} />
            </ActionIcon>
          </Tooltip>
          <Button
            variant={editMode ? 'light' : 'filled'}
            leftSection={<IconEdit size={16} />}
            onClick={() => setEditMode(!editMode)}
          >
            {editMode ? 'Cancel Edit' : 'Edit Incident'}
          </Button>
        </Group>
      </Group>
      
      <Grid gutter="lg">
        {/* Left Column - Incident Details */}
        <Grid.Col span={{ base: 12, md: 8 }}>
          <Stack gap="md">
            {/* Basic Information */}
            <Paper p="md" withBorder>
              <Title order={4} size="h5" mb="md">Incident Information</Title>
              
              <Stack gap="sm">
                <Group>
                  <IconClock size={16} />
                  <Box>
                    <Text size="sm" fw={500}>Incident Date</Text>
                    <Text size="sm" c="dimmed">{formatDateTime(incident.incidentDate)}</Text>
                  </Box>
                </Group>
                
                <Group>
                  <IconMapPin size={16} />
                  <Box>
                    <Text size="sm" fw={500}>Location</Text>
                    <Text size="sm">{incident.location}</Text>
                  </Box>
                </Group>
                
                <Group>
                  {incident.isAnonymous ? <IconUserOff size={16} /> : <IconUser size={16} />}
                  <Box>
                    <Text size="sm" fw={500}>Reporter</Text>
                    <Text size="sm" c={incident.isAnonymous ? 'dimmed' : 'blue'}>
                      {incident.isAnonymous ? 'Anonymous Report' : (incident.reporterName || 'Identified Reporter')}
                    </Text>
                  </Box>
                </Group>
              </Stack>
            </Paper>
            
            {/* Description */}
            <Paper p="md" withBorder>
              <Title order={4} size="h5" mb="md">Description</Title>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                {incident.description}
              </Text>
            </Paper>
            
            {/* Additional Information */}
            {(incident.involvedParties || incident.witnesses) && (
              <Paper p="md" withBorder>
                <Title order={4} size="h5" mb="md">Additional Information</Title>
                <Stack gap="sm">
                  {incident.involvedParties && (
                    <Box>
                      <Text size="sm" fw={500} mb="xs">Involved Parties</Text>
                      <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                        {incident.involvedParties}
                      </Text>
                    </Box>
                  )}
                  
                  {incident.witnesses && (
                    <Box>
                      <Text size="sm" fw={500} mb="xs">Witnesses</Text>
                      <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                        {incident.witnesses}
                      </Text>
                    </Box>
                  )}
                </Stack>
              </Paper>
            )}
            
            {/* Contact Information */}
            {!incident.isAnonymous && (incident.contactEmail || incident.contactPhone) && (
              <Paper p="md" withBorder>
                <Title order={4} size="h5" mb="md">Contact Information</Title>
                <Stack gap="xs">
                  {incident.contactEmail && (
                    <Group gap="xs">
                      <Text size="sm" fw={500}>Email:</Text>
                      <Text size="sm" c="blue">{incident.contactEmail}</Text>
                    </Group>
                  )}
                  {incident.contactPhone && (
                    <Group gap="xs">
                      <Text size="sm" fw={500}>Phone:</Text>
                      <Text size="sm">{incident.contactPhone}</Text>
                    </Group>
                  )}
                  {incident.requestFollowUp && (
                    <Text size="sm" c="orange" fs="italic">
                      Reporter requested follow-up contact
                    </Text>
                  )}
                </Stack>
              </Paper>
            )}
          </Stack>
        </Grid.Col>
        
        {/* Right Column - Management */}
        <Grid.Col span={{ base: 12, md: 4 }}>
          <Stack gap="md">
            {/* Assignment & Status */}
            <Paper p="md" withBorder>
              <Title order={4} size="h5" mb="md">Assignment & Status</Title>
              
              {editMode ? (
                <form onSubmit={form.onSubmit(handleUpdate)}>
                  <Stack gap="md">
                    <Select
                      label="Status"
                      data={Object.values(IncidentStatus).map(status => ({
                        value: status,
                        label: STATUS_CONFIGS[status].label
                      }))}
                      {...form.getInputProps('status')}
                    />
                    
                    <Select
                      label="Assign To"
                      placeholder="Select team member"
                      data={[
                        { value: '', label: 'Unassigned' },
                        // TODO: Fetch actual safety team members from API
                        { value: 'user1', label: 'Safety Team Member 1' },
                        { value: 'user2', label: 'Safety Team Member 2' }
                      ]}
                      {...form.getInputProps('assignedTo')}
                      clearable
                    />
                    
                    <Textarea
                      label="Update Notes"
                      placeholder="Describe the action taken..."
                      minRows={3}
                      {...form.getInputProps('notes')}
                      required
                    />
                    
                    <Group justify="flex-end">
                      <Button
                        variant="light"
                        leftSection={<IconX size={16} />}
                        onClick={() => setEditMode(false)}
                      >
                        Cancel
                      </Button>
                      <Button
                        type="submit"
                        leftSection={<IconCheck size={16} />}
                        loading={updateIncidentMutation.isPending}
                      >
                        Update
                      </Button>
                    </Group>
                  </Stack>
                </form>
              ) : (
                <Stack gap="sm">
                  <Box>
                    <Text size="sm" fw={500} mb="xs">Current Status</Text>
                    <Badge color={statusConfig.color} variant="light">
                      {statusConfig.label}
                    </Badge>
                  </Box>
                  
                  <Box>
                    <Text size="sm" fw={500} mb="xs">Assigned To</Text>
                    <Text size="sm" c={incident.assignedUserName ? undefined : 'dimmed'}>
                      {incident.assignedUserName || 'Unassigned'}
                    </Text>
                  </Box>
                </Stack>
              )}
            </Paper>
            
            {/* Audit Trail */}
            <Paper p="md" withBorder>
              <Title order={4} size="h5" mb="md">Activity Log</Title>
              
              {incident.auditTrail.length > 0 ? (
                <Timeline bulletSize={24} lineWidth={2}>
                  {incident.auditTrail.map((log) => (
                    <Timeline.Item
                      key={log.id}
                      bullet={<IconClock size={12} />}
                      title={log.actionType}
                    >
                      <Text size="sm" c="dimmed" mb="xs">
                        {log.actionDescription}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {log.userName || 'System'} • {formatDateTime(log.createdAt)}
                      </Text>
                    </Timeline.Item>
                  ))}
                </Timeline>
              ) : (
                <Text size="sm" c="dimmed" fs="italic">
                  No activity logged yet
                </Text>
              )}
            </Paper>
          </Stack>
        </Grid.Col>
      </Grid>
      
      {/* Update Error */}
      {updateIncidentMutation.error && (
        <Alert variant="light" color="red">
          <Text size="sm">
            Failed to update incident: {updateIncidentMutation.error instanceof Error 
              ? updateIncidentMutation.error.message 
              : 'Unknown error occurred'}
          </Text>
        </Alert>
      )}
    </Stack>
  );
}