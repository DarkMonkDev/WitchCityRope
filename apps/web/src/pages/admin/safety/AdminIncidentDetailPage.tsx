import React, { useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Container, Stack, Title, Card, Group, Button, Text, Loader, Alert, TextInput, ActionIcon } from '@mantine/core';
import { IconArrowLeft, IconUserPlus, IconAlertCircle, IconCheck, IconClock, IconX, IconEdit, IconDeviceFloppy } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { showNotification } from '@mantine/notifications';
import { IncidentDetailsCard } from '@/features/safety/components/IncidentDetailsCard';
import { PeopleInvolvedCard } from '@/features/safety/components/PeopleInvolvedCard';
import { InvestigationNotes } from '@/features/safety/components/InvestigationNotes';
import { GoogleDriveLinksSection } from '@/features/safety/components/GoogleDriveLinksSection';
import { CoordinatorAssignmentModal } from '@/features/safety/components/CoordinatorAssignmentModal';
import { IncidentStatusBadge } from '@/features/safety/components/IncidentStatusBadge';

type IncidentStatus = 'ReportSubmitted' | 'InformationGathering' | 'ReviewingFinalReport' | 'OnHold' | 'Closed';
type SeverityLevel = 'Low' | 'Medium' | 'High' | 'Critical';
type IncidentType = 'SafetyConcern' | 'BoundaryViolation' | 'Harassment' | 'OtherConcern';
type WhereOccurred = 'AtEvent' | 'Online' | 'PrivatePlay' | 'OtherSpace';
type SpokenToPersonStatus = 'Yes' | 'No' | 'NotApplicable';

interface SafetyIncidentDetailDto {
  id: string;
  referenceNumber: string;
  title?: string;
  status: IncidentStatus;
  description: string;
  isAnonymous: boolean;
  reporterName?: string;
  reporterEmail?: string;
  requestedFollowUp: boolean;
  incidentDate?: string;
  reportedAt?: string;
  location?: string;
  contactEmail?: string;
  contactName?: string;
  createdAt: string;
  updatedAt: string;
  involvedParties?: string;
  witnesses?: string;
  googleDriveFolderUrl?: string;
  googleDriveFinalReportUrl?: string;
  coordinatorId?: string;
  coordinatorName?: string;
  // New wireframe fields
  type?: IncidentType;
  whereOccurred?: WhereOccurred;
  eventName?: string;
  hasSpokenToPerson?: SpokenToPersonStatus;
  desiredOutcomes?: string; // Free-text from backend
  futureInteractionPreference?: string;
  anonymousDuringInvestigation?: boolean;
  anonymousInFinalReport?: boolean;
}

export const AdminIncidentDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [assignModalOpen, setAssignModalOpen] = useState(false);
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [editedTitle, setEditedTitle] = useState('');

  // Fetch incident details: GET /api/safety/admin/incidents/{id}
  const { data: incident, isLoading, error } = useQuery<SafetyIncidentDetailDto>({
    queryKey: ['safety', 'incident', id],
    queryFn: async () => {
      const response = await fetch(`/api/safety/admin/incidents/${id}`, {
        credentials: 'include'
      });
      if (!response.ok) {
        const status = response.status;
        if (status === 401) {
          throw new Error('Authentication required. Please log in.');
        } else if (status === 404) {
          throw new Error(`Incident with ID "${id}" was not found.`);
        }
        throw new Error('Failed to fetch incident');
      }
      return response.json();
    },
    enabled: !!id
  });

  // Status update mutation
  const statusMutation = useMutation<unknown, Error, { newStatus: IncidentStatus; notes?: string }>({
    mutationFn: async ({ newStatus, notes }) => {
      const response = await fetch(`/api/safety/admin/incidents/${id}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ newStatus, notes }),
      });
      if (!response.ok) throw new Error('Failed to update status');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['safety', 'incident', id] });
      queryClient.invalidateQueries({ queryKey: ['safety', 'incidents'] });
      showNotification({
        title: 'Success',
        message: 'Status updated successfully',
        color: 'green',
      });
    },
    onError: (error) => {
      showNotification({
        title: 'Error',
        message: error instanceof Error ? error.message : 'Failed to update status',
        color: 'red',
      });
    }
  });

  // Determine available actions based on current status
  const availableActions = useMemo(() => {
    if (!incident) return {
      canAdvanceStage: false,
      canPutOnHold: false,
      canClose: false
    };

    const isTerminal = incident.status === 'Closed';
    const isOnHold = incident.status === 'OnHold';

    return {
      canAdvanceStage: !isTerminal && !isOnHold,
      canPutOnHold: !isTerminal && !isOnHold,
      canClose: !isTerminal
    };
  }, [incident]);

  // Get next stage configuration based on current status
  const getNextStageConfig = (currentStatus: IncidentStatus) => {
    const configs: Record<IncidentStatus, {
      label: string;
      nextStatus: IncidentStatus;
      description: string;
    } | null> = {
      ReportSubmitted: {
        label: 'Begin Investigation',
        nextStatus: 'InformationGathering',
        description: 'Start gathering information'
      },
      InformationGathering: {
        label: 'Submit for Review',
        nextStatus: 'ReviewingFinalReport',
        description: 'Move to final report review'
      },
      ReviewingFinalReport: {
        label: 'Close Incident',
        nextStatus: 'Closed',
        description: 'Complete and close incident'
      },
      OnHold: null, // On hold doesn't have a direct next stage
      Closed: null  // Closed is terminal
    };

    return configs[currentStatus] || null;
  };

  const nextStageConfig = incident ? getNextStageConfig(incident.status) : null;

  const handleAdvanceStage = () => {
    if (!nextStageConfig) return;
    statusMutation.mutate({ newStatus: nextStageConfig.nextStatus });
  };

  const handlePutOnHold = () => {
    statusMutation.mutate({ newStatus: 'OnHold' });
  };

  const handleClose = () => {
    statusMutation.mutate({ newStatus: 'Closed' });
  };

  const handleEditTitle = () => {
    setEditedTitle(incident?.title || `Incident ${incident?.referenceNumber}`);
    setIsEditingTitle(true);
  };

  const handleSaveTitle = () => {
    // TODO: Add API endpoint for updating incident title
    // For now, just close the edit mode
    showNotification({
      title: 'Not Implemented',
      message: 'Title update endpoint not yet implemented',
      color: 'orange',
    });
    setIsEditingTitle(false);
  };

  const handleCancelEditTitle = () => {
    setIsEditingTitle(false);
    setEditedTitle('');
  };

  if (isLoading) {
    return (
      <Container size="lg" py="xl">
        <Stack align="center" justify="center" style={{ minHeight: '400px' }}>
          <Loader size="lg" />
          <Text c="dimmed">Loading incident details...</Text>
        </Stack>
      </Container>
    );
  }

  if (error) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
          {error instanceof Error ? error.message : 'Failed to load incident'}
        </Alert>
      </Container>
    );
  }

  if (!incident) {
    return (
      <Container size="lg" py="xl">
        <Alert icon={<IconAlertCircle size={16} />} color="yellow" title="Not Found">
          Incident not found
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="lg" py="xl">
      <Stack gap="lg">
        {/* Back button and Status Badge */}
        <Group justify="space-between" align="center">
          <Button
            variant="subtle"
            leftSection={<IconArrowLeft size={16} />}
            onClick={() => navigate('/admin/safety/incidents')}
            styles={{
              root: {
                width: 'fit-content',
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Back to Dashboard
          </Button>
          <IncidentStatusBadge status={incident.status} size="md" />
        </Group>

        {/* Header with reference number and actions */}
        <Card p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
          {/* Title Row with Assign Coordinator */}
          <Group justify="space-between" align="center" mb="md">
            <div style={{ flex: 1 }}>
              {isEditingTitle ? (
                <Group gap="xs" align="center">
                  <TextInput
                    value={editedTitle}
                    onChange={(e) => setEditedTitle(e.currentTarget.value)}
                    style={{ flex: 1, maxWidth: '600px' }}
                    styles={{
                      input: {
                        fontSize: '1.5rem',
                        fontWeight: 700,
                        color: '#880124'
                      }
                    }}
                  />
                  <Group gap="xs">
                    <button
                      className="btn btn-primary"
                      onClick={handleSaveTitle}
                      type="button"
                      style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
                    >
                      <IconDeviceFloppy size={16} />
                      <span>Save</span>
                    </button>
                    <button
                      className="btn btn-secondary"
                      onClick={handleCancelEditTitle}
                      type="button"
                    >
                      Cancel
                    </button>
                  </Group>
                </Group>
              ) : (
                <>
                  <Group gap="sm" align="center">
                    <Title order={2} style={{ color: '#880124', margin: 0 }}>
                      {incident.title || `Incident ${incident.referenceNumber}`}
                    </Title>
                    <Text
                      size="sm"
                      c="dimmed"
                      onClick={handleEditTitle}
                      style={{ cursor: 'pointer', textDecoration: 'underline' }}
                      data-testid="edit-title-button"
                    >
                      Edit
                    </Text>
                  </Group>
                  <Text size="sm" c="dimmed" mt={4}>
                    {incident.referenceNumber}
                  </Text>
                </>
              )}
            </div>

            {/* Assign Coordinator Button */}
            <button
              className="btn btn-secondary"
              onClick={() => setAssignModalOpen(true)}
              data-testid="assign-coordinator-button"
              type="button"
              style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <IconUserPlus size={16} />
              <span>{incident.coordinatorName ? 'Reassign' : 'Assign'} Coordinator</span>
            </button>
          </Group>

          {/* Coordinator info */}
          {incident.coordinatorName && (
            <Text mb="md" size="sm">
              <strong>Assigned to:</strong> {incident.coordinatorName}
            </Text>
          )}

          {/* Status Action Buttons - All on One Line */}
          <Group gap="md" wrap="nowrap">
            {/* Primary Action: Advance to Next Stage */}
            {nextStageConfig && availableActions.canAdvanceStage && (
              <button
                className="btn btn-primary"
                onClick={handleAdvanceStage}
                disabled={statusMutation.isPending}
                data-testid="advance-stage-button"
                type="button"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconCheck size={16} />
                <span>{nextStageConfig.label}</span>
              </button>
            )}

            {/* Put On Hold */}
            {availableActions.canPutOnHold && (
              <button
                className="btn btn-secondary"
                onClick={handlePutOnHold}
                disabled={statusMutation.isPending}
                data-testid="hold-button"
                type="button"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconClock size={16} />
                <span>Put On Hold</span>
              </button>
            )}

            {/* Close Incident */}
            {availableActions.canClose && incident.status !== 'Closed' && (
              <button
                className="btn btn-secondary"
                onClick={handleClose}
                disabled={statusMutation.isPending}
                data-testid="close-button"
                type="button"
                style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
              >
                <IconX size={16} />
                <span>Close Incident</span>
              </button>
            )}
          </Group>
        </Card>

        {/* Incident Details - reuse existing component */}
        <IncidentDetailsCard
          description={incident.description}
          isAnonymous={incident.isAnonymous}
          reporterName={incident.reporterName}
          reporterEmail={incident.reporterEmail}
          requestedFollowUp={incident.requestedFollowUp}
          incidentDate={incident.incidentDate}
          reportedAt={incident.reportedAt}
          location={incident.location}
          contactEmail={incident.contactEmail}
          contactName={incident.contactName}
          createdAt={incident.createdAt}
          updatedAt={incident.updatedAt}
          incidentType={incident.type}
          whereOccurred={incident.whereOccurred}
          eventName={incident.eventName}
          hasSpokenToPerson={incident.hasSpokenToPerson}
          desiredOutcomes={incident.desiredOutcomes}
          futureInteractionPreference={incident.futureInteractionPreference}
          anonymousDuringInvestigation={incident.anonymousDuringInvestigation}
          anonymousInFinalReport={incident.anonymousInFinalReport}
        />

        {/* People Involved - reuse existing component */}
        {(incident.involvedParties || incident.witnesses) && (
          <PeopleInvolvedCard
            involvedParties={incident.involvedParties}
            witnesses={incident.witnesses}
          />
        )}

        {/* Google Drive Links */}
        <GoogleDriveLinksSection
          incidentId={id!}
          folderUrl={incident.googleDriveFolderUrl}
          reportUrl={incident.googleDriveFinalReportUrl}
        />

        {/* Investigation Notes */}
        <InvestigationNotes incidentId={id!} />
      </Stack>

      {/* Modals */}
      <CoordinatorAssignmentModal
        opened={assignModalOpen}
        onClose={() => setAssignModalOpen(false)}
        incidentId={id!}
        currentCoordinatorId={incident.coordinatorId}
      />
    </Container>
  );
};
