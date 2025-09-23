import React, { useState } from 'react';
import {
  Paper,
  Stack,
  Title,
  Text,
  Group,
  Badge,
  Button,
  Divider,
  Grid,
  Textarea,
  Select,
  Modal,
  Box,
  Alert,
  ActionIcon,
  Card,
  Timeline
} from '@mantine/core';
import {
  IconArrowLeft,
  IconEdit,
  IconCheck,
  IconX,
  IconClock,
  IconUser,
  IconMail,
  IconPhone,
  IconCalendar,
  IconAlertCircle,
  IconNotes
} from '@tabler/icons-react';
import { useVettingApplicationDetail } from '../hooks/useVettingApplicationDetail';
import { useSubmitReviewDecision } from '../hooks/useSubmitReviewDecision';
import { useApproveApplication } from '../hooks/useApproveApplication';
import { vettingAdminApi } from '../services/vettingAdminApi';
import { VettingStatusBadge } from './VettingStatusBadge';
import { OnHoldModal } from './OnHoldModal';
import { SendReminderModal } from './SendReminderModal';
import { DenyApplicationModal } from './DenyApplicationModal';
import type { ApplicationDetailResponse, ReviewDecisionRequest } from '../types/vetting.types';

interface VettingApplicationDetailProps {
  applicationId: string;
  onBack: () => void;
}

export const VettingApplicationDetail: React.FC<VettingApplicationDetailProps> = ({
  applicationId,
  onBack
}) => {
  const [notesModalOpen, setNotesModalOpen] = useState(false);
  const [newNote, setNewNote] = useState('');
  const [onHoldModalOpen, setOnHoldModalOpen] = useState(false);
  const [reminderModalOpen, setReminderModalOpen] = useState(false);
  const [denyModalOpen, setDenyModalOpen] = useState(false);

  const { data: application, isLoading, error, refetch } = useVettingApplicationDetail(applicationId);
  const { mutate: submitDecision, isPending: isSubmittingDecision } = useSubmitReviewDecision(
    () => {
      refetch();
    }
  );
  const { mutate: approveApplication, isPending: isApprovingApplication } = useApproveApplication(
    () => {
      refetch();
    }
  );


  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatDateOnly = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const handleApproveApplication = () => {
    if (application) {
      const reasoning = application.status === 'UnderReview'
        ? 'Application approved for interview based on initial review'
        : 'Application approved for membership';

      approveApplication({
        applicationId: application.id,
        reasoning
      });
    }
  };

  const handlePutOnHold = () => {
    setOnHoldModalOpen(true);
  };

  const handleSendReminder = () => {
    setReminderModalOpen(true);
  };

  const handleDenyApplication = () => {
    setDenyModalOpen(true);
  };

  const handleSaveNote = async () => {
    if (!newNote.trim()) return;

    try {
      await vettingAdminApi.addApplicationNote(applicationId, newNote.trim());
      setNewNote('');
      refetch();
      // Note: Success notification will be shown by the API service
    } catch (error: any) {
      console.error('Failed to save note:', error);
      // Error notification will be shown by the API service
    }
  };

  if (isLoading) {
    return (
      <Paper p="xl" radius="md">
        <Text ta="center">Loading application details...</Text>
      </Paper>
    );
  }

  if (error || !application) {
    return (
      <Paper p="xl" radius="md">
        <Alert icon={<IconAlertCircle size={16} />} color="red" mb="md">
          Error loading application: {error?.message || 'Application not found'}
        </Alert>
        <Button
          leftSection={<IconArrowLeft size={16} />}
          onClick={onBack}
          style={{
            minHeight: 40,
            height: 'auto',
            padding: '10px 20px',
            lineHeight: 1.4
          }}
        >
          Back to Applications
        </Button>
      </Paper>
    );
  }


  return (
    <Stack gap="md">
      {/* Breadcrumb Navigation */}
      <Group>
        <Button
          variant="subtle"
          leftSection={<IconArrowLeft size={16} />}
          onClick={onBack}
          data-testid="back-to-applications-button"
          style={{
            minHeight: 40,
            height: 'auto',
            padding: '10px 20px',
            lineHeight: 1.4,
            color: '#880124'
          }}
        >
          Back to Applications
        </Button>
      </Group>

      {/* Header Section - Direct header without title section */}
      <Stack gap="sm">
        {/* Title Row with Status Badge - Make status badge bigger */}
        <Group justify="space-between" align="center">
          <Title order={2} style={{ color: '#880124' }} data-testid="application-title">
            Application - {application.fullName} ({application.sceneName})
          </Title>
          <VettingStatusBadge status={application.status} size="xl" data-testid="application-status-badge" />
        </Group>

        {/* Action Buttons Row - Fixed button text cutoff and conditional primary action */}
        <Group gap="md">
          <Button
            leftSection={<IconCheck size={16} />}
            onClick={handleApproveApplication}
            loading={isApprovingApplication}
            disabled={['Approved', 'InterviewScheduled'].includes(application.status)}
            data-testid="approve-application-button"
            style={{
              backgroundColor: '#FFC107',
              color: '#000',
              minHeight: 56,
              paddingTop: 12,
              paddingBottom: 12,
              paddingLeft: 24,
              paddingRight: 24,
              fontSize: 16,
              fontWeight: 600,
              lineHeight: 1.4
            }}
          >
            {application.status === 'UnderReview' ? 'APPROVE FOR INTERVIEW' : 'APPROVE APPLICATION'}
          </Button>
          <Button
            variant="outline"
            color="gray"
            onClick={handlePutOnHold}
            disabled={application.status === 'OnHold'}
            data-testid="put-on-hold-button"
            style={{
              minHeight: 56,
              paddingTop: 12,
              paddingBottom: 12,
              paddingLeft: 24,
              paddingRight: 24,
              fontSize: 16,
              fontWeight: 600,
              lineHeight: 1.4
            }}
          >
            PUT ON HOLD
          </Button>
          <Button
            color="red"
            leftSection={<IconX size={16} />}
            onClick={handleDenyApplication}
            disabled={application.status === 'Rejected'}
            data-testid="deny-application-button"
            style={{
              minHeight: 56,
              paddingTop: 12,
              paddingBottom: 12,
              paddingLeft: 24,
              paddingRight: 24,
              fontSize: 16,
              fontWeight: 600,
              lineHeight: 1.4
            }}
          >
            DENY APPLICATION
          </Button>
        </Group>
      </Stack>

      {/* Single Column Layout - Removed right sidebar as per requirements */}
      <Grid>
        <Grid.Col span={12}>
          <Stack gap="md">
            {/* Application Information - Inline layout for short answers, long answers at bottom */}
            <Card data-testid="application-information-section">
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Application Information
              </Title>

              {/* Short answers - inline layout */}
              <Grid mb="xl">
                {/* Left Column */}
                <Grid.Col span={6}>
                  <Stack gap="md">
                    <Group gap="md" wrap="nowrap" data-testid="scene-name-field">
                      <Text fw={600} style={{ minWidth: '120px' }}>Scene Name:</Text>
                      <Text>{application.sceneName}</Text>
                    </Group>
                    <Group gap="md" wrap="nowrap" data-testid="real-name-field">
                      <Text fw={600} style={{ minWidth: '120px' }}>Real Name:</Text>
                      <Text>{application.fullName}</Text>
                    </Group>
                    <Group gap="md" wrap="nowrap" data-testid="email-field">
                      <Text fw={600} style={{ minWidth: '120px' }}>Email:</Text>
                      <Text>{application.email}</Text>
                    </Group>
                    {application.pronouns && (
                      <Group gap="md" wrap="nowrap">
                        <Text fw={600} style={{ minWidth: '120px' }}>Pronouns:</Text>
                        <Text>{application.pronouns}</Text>
                      </Group>
                    )}
                  </Stack>
                </Grid.Col>

                {/* Right Column */}
                <Grid.Col span={6}>
                  <Stack gap="md">
                    <Group gap="md" wrap="nowrap">
                      <Text fw={600} style={{ minWidth: '140px' }}>Application Date:</Text>
                      <Text>{formatDateOnly(application.submittedAt)}</Text>
                    </Group>
                    <Group gap="md" wrap="nowrap">
                      <Text fw={600} style={{ minWidth: '140px' }}>FetLife Handle:</Text>
                      <Text>@{application.sceneName}</Text>
                    </Group>
                    <Group gap="md" wrap="nowrap">
                      <Text fw={600} style={{ minWidth: '140px' }}>Other Names/Handles:</Text>
                      <Text>{application.phone || 'Not provided'}</Text>
                    </Group>
                  </Stack>
                </Grid.Col>
              </Grid>

              {/* Long answers - full width at bottom */}
              <Stack gap="xl">
                <div>
                  <Text fw={600} mb="xs">Why do you want to join WitchCityRope?</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.whyJoinCommunity}
                  </Text>
                </div>
                <div>
                  <Text fw={600} mb="xs">What is your rope experience thus far?</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.experienceDescription}
                  </Text>
                </div>
              </Stack>
            </Card>

          </Stack>
        </Grid.Col>
      </Grid>

      {/* Notes and Status History Section - Match wireframe layout */}
      <Card>
        <Group justify="space-between" align="center" mb="md">
          <Title order={3} style={{ color: '#880124' }}>
            Notes and Status History
          </Title>
          <Button
            variant="filled"
            size="sm"
            onClick={handleSaveNote}
            disabled={!newNote.trim()}
            data-testid="save-note-button"
            style={{
              backgroundColor: '#D4AF37',
              color: '#000000',
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
              fontWeight: 600
            }}
          >
            SAVE NOTE
          </Button>
        </Group>

        <Stack gap="md">
          {/* Add Note Text Area */}
          <Textarea
            placeholder="Add Note"
            value={newNote}
            onChange={(e) => setNewNote(e.currentTarget.value)}
            minRows={4}
            data-testid="add-note-textarea"
            styles={{
              input: {
                borderRadius: '8px',
                border: '1px solid #E0E0E0',
              }
            }}
          />

          {/* Status History Entries */}
          <Stack gap="sm">
            {application.decisions.map((decision) => (
              <Paper key={decision.id} p="md" style={{ background: '#FFF8F0', borderRadius: '8px' }}>
                <Group justify="space-between" mb="xs">
                  <Group>
                    <VettingStatusBadge status={decision.decisionType} size="sm" />
                    <Text fw={600}>{decision.decisionType}</Text>
                  </Group>
                  <Text size="sm" c="dimmed">
                    {formatDate(decision.createdAt)}
                  </Text>
                </Group>
                {decision.reasoning && (
                  <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                    {decision.reasoning}
                  </Text>
                )}
                <Text size="xs" c="dimmed" mt="xs">
                  By: {decision.reviewerName}
                </Text>
              </Paper>
            ))}

            {application.notes.map((note) => (
              <Paper key={note.id} p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
                <Group justify="space-between" mb="xs">
                  <Text fw={600} size="sm">Note</Text>
                  <Text size="sm" c="dimmed">
                    {formatDate(note.createdAt)}
                  </Text>
                </Group>
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                  {note.content}
                </Text>
                <Text size="xs" c="dimmed" mt="xs">
                  By: {note.reviewerName}
                </Text>
              </Paper>
            ))}

            {application.decisions.length === 0 && application.notes.length === 0 && (
              <Text c="dimmed" ta="center" py="md">
                No status history or notes yet
              </Text>
            )}
          </Stack>
        </Stack>
      </Card>

      {/* Add Note Modal */}
      <Modal
        opened={notesModalOpen}
        onClose={() => setNotesModalOpen(false)}
        title="Add Note"
        centered
      >
        <Stack gap="md">
          <Textarea
            placeholder="Enter your note..."
            value={newNote}
            onChange={(e) => setNewNote(e.currentTarget.value)}
            minRows={4}
          />
          <Group justify="flex-end">
            <Button
              variant="light"
              onClick={() => setNotesModalOpen(false)}
              style={{
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={() => {
                // TODO: Implement add note functionality
                setNewNote('');
                setNotesModalOpen(false);
              }}
              disabled={!newNote.trim()}
              style={{
                minHeight: 40,
                height: 'auto',
                padding: '10px 20px',
                lineHeight: 1.4
              }}
            >
              Add Note
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* OnHold Modal */}
      <OnHoldModal
        opened={onHoldModalOpen}
        onClose={() => setOnHoldModalOpen(false)}
        applicationId={applicationId}
        applicantName={application?.fullName || 'Unknown'}
        onSuccess={() => refetch()}
      />

      {/* Send Reminder Modal */}
      <SendReminderModal
        opened={reminderModalOpen}
        onClose={() => setReminderModalOpen(false)}
        applicationId={applicationId}
        applicantName={application?.fullName || 'Unknown'}
        onSuccess={() => refetch()}
      />

      {/* Deny Application Modal */}
      <DenyApplicationModal
        opened={denyModalOpen}
        onClose={() => setDenyModalOpen(false)}
        applicationId={applicationId}
        applicantName={application?.fullName || 'Unknown'}
        onSuccess={() => refetch()}
      />
    </Stack>
  );
};