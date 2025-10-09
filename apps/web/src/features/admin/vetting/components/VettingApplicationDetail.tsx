import React, { useState, useMemo } from 'react';
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
  Card
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
  IconNotes,
  IconCalendarEvent
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
  const [scheduleInterviewModalOpen, setScheduleInterviewModalOpen] = useState(false);

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

  // Calculate days since submission
  const daysSinceSubmission = useMemo(() => {
    if (!application?.submittedAt) return 0;
    const submitted = new Date(application.submittedAt);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - submitted.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  }, [application?.submittedAt]);

  // Determine available actions based on current status
  const availableActions = useMemo(() => {
    if (!application) return {
      canApprove: false,
      canDeny: false,
      canHold: false,
      canSchedule: false,
      canRemind: false,
      canAdvanceStage: false,
      canSkipToApproved: false
    };

    const isTerminal = ['Approved', 'Denied', 'Withdrawn'].includes(application.status);
    const isOnHold = application.status === 'OnHold';

    return {
      canApprove: !isTerminal, // Legacy "Skip to Approved" action
      canDeny: !isTerminal,
      canHold: !isTerminal && !isOnHold,
      canSchedule: application.status === 'InterviewApproved',
      canRemind: true, // Can always send reminder
      canAdvanceStage: !isTerminal, // Can advance unless terminal
      canSkipToApproved: !isTerminal && application.status !== 'FinalReview' // Can skip unless already at final review or terminal
    };
  }, [application]);

  // Get next stage configuration based on current status
  const getNextStageConfig = (currentStatus: string) => {
    const configs: Record<string, {
      label: string;
      nextStatus: string;
      description: string;
      icon: typeof IconCheck;
    }> = {
      UnderReview: {
        label: 'Approve for Interview',
        nextStatus: 'InterviewApproved',
        description: 'Move to interview stage',
        icon: IconCheck
      },
      InterviewApproved: {
        label: 'Mark Interview Complete',
        nextStatus: 'FinalReview',
        description: 'Interview completed, move to final review',
        icon: IconCalendarEvent
      },
      FinalReview: {
        label: 'Approve Application',
        nextStatus: 'Approved',
        description: 'Grant full access',
        icon: IconCheck
      }
    };

    return configs[currentStatus] || null;
  };

  const nextStageConfig = getNextStageConfig(application?.status || '');

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatDateOnly = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    const dateStr = date.toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
    const timeStr = date.toLocaleTimeString(undefined, {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
    return `${dateStr} - ${timeStr}`;
  };

  // Helper to detect system-generated notes and extract status
  const isSystemGeneratedNote = (noteText: string): { isSystem: boolean; status?: string } => {
    // Map system-generated note text to corresponding status values
    // These match the simplified descriptions from backend GetSimplifiedActionDescription()
    const systemNotes: Record<string, string> = {
      'Approved for interview': 'InterviewApproved',
      'Interview completed': 'FinalReview',
      'Application approved': 'Approved',
      'Application denied': 'Denied',
      'Application placed on hold': 'OnHold',
      'Returned to review': 'UnderReview',
      'Application withdrawn': 'Withdrawn'
    };

    const status = systemNotes[noteText];
    return { isSystem: !!status, status };
  };

  const handleAdvanceStage = () => {
    if (!application || !nextStageConfig) return;

    // Don't auto-generate verbose reasoning - let backend create simplified text
    // Only send reasoning when admin manually enters notes
    const reasoning = undefined;

    // Use the appropriate mutation based on the next status
    if (nextStageConfig.nextStatus === 'Approved') {
      // Final approval
      approveApplication({
        applicationId: application.id,
        reasoning
      });
    } else {
      // Intermediate stage advancement
      submitDecision({
        applicationId: application.id,
        decision: {
          decisionType: nextStageConfig.nextStatus,
          reasoning,
          isFinalDecision: false
        }
      });
    }
  };

  const handleSkipToApproved = () => {
    if (application) {
      // Don't auto-generate verbose reasoning - let backend create simplified text
      // Only send reasoning when admin manually enters notes
      const reasoning = undefined;

      approveApplication({
        applicationId: application.id,
        reasoning
      });
    }
  };

  const handleApproveApplication = () => {
    // This is now the "Skip to Approved" action
    handleSkipToApproved();
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
        <Stack gap="md" align="center">
          <Text ta="center">Loading application details...</Text>
          <Text size="sm" c="dimmed">Application ID: {applicationId}</Text>
        </Stack>
      </Paper>
    );
  }

  if (error || !application) {
    const errorMessage = error?.message || error?.toString() || 'Application not found';
    const isNetworkError = errorMessage.includes('Network') || errorMessage.includes('fetch');
    const isAuthError = errorMessage.includes('401') || errorMessage.includes('Unauthorized');

    return (
      <Paper p="xl" radius="md">
        <Stack gap="md">
          <Alert
            icon={<IconAlertCircle size={16} />}
            color={isAuthError ? "orange" : "red"}
            title={isAuthError ? "Authentication Required" : "Error Loading Application"}
          >
            <Text>{errorMessage}</Text>
            <Text size="sm" c="dimmed" mt="xs">
              Application ID: {applicationId}
            </Text>
            {isNetworkError && (
              <Text size="sm" c="dimmed" mt="xs">
                Please check your internet connection and try again.
              </Text>
            )}
            {isAuthError && (
              <Text size="sm" c="dimmed" mt="xs">
                You may need to log in again to access this application.
              </Text>
            )}
          </Alert>
          <Button
            leftSection={<IconArrowLeft size={16} />}
            onClick={onBack}
            styles={{
              root: {
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Back to Applications
          </Button>
        </Stack>
      </Paper>
    );
  }


  return (
    <Stack gap="xs" mt={0}>
      {/* Breadcrumb Navigation with Status Badge - FIRST */}
      <Group justify="space-between" align="center">
        <Button
          variant="subtle"
          leftSection={<IconArrowLeft size={16} />}
          onClick={onBack}
          data-testid="back-to-applications-button"
          styles={{
            root: {
              height: '44px',
              paddingTop: '12px',
              paddingBottom: '12px',
              fontSize: '14px',
              lineHeight: '1.2',
              color: '#880124'
            }
          }}
        >
          Back to Applications
        </Button>
        <VettingStatusBadge status={application.status} size="lg" data-testid="status-badge" />
      </Group>

      {/* Header Section - Person's name - SECOND with reduced padding */}
      <Paper p="sm" radius="md" pt={0} style={{ background: '#FFF8F0' }}>
        <Title order={1} mt={0} style={{ color: '#880124', paddingLeft: '16px' }} data-testid="application-title">
          {application.sceneName}
        </Title>
      </Paper>

      {/* Action Buttons - Split Layout: Primary CTAs Left, Tertiary Actions Right */}
      <Group justify="space-between" gap="md" wrap="nowrap" mb="sm">
        {/* Left Side: Primary CTAs */}
        <Group gap="md" wrap="nowrap">
          {/* Primary Action 1: Advance to Next Stage */}
          {nextStageConfig && availableActions.canAdvanceStage && (
            <button
              className="btn btn-primary"
              onClick={handleAdvanceStage}
              disabled={!availableActions.canAdvanceStage || isSubmittingDecision || isApprovingApplication}
              data-testid="advance-stage-button"
              type="button"
            >
              {nextStageConfig.label}
            </button>
          )}

          {/* Primary Action 2: Skip to Approved */}
          {availableActions.canSkipToApproved && (
            <button
              className="btn btn-secondary"
              onClick={handleSkipToApproved}
              disabled={!availableActions.canSkipToApproved || isApprovingApplication}
              data-testid="skip-to-approved-button"
              type="button"
            >
              Skip to Approved
            </button>
          )}
        </Group>

        {/* Right Side: Tertiary Actions */}
        <Group gap="md" wrap="nowrap">
          {/* Tertiary Action: Reminder - Hide when application is approved */}
          {application.status !== 'Approved' && (
            <button
              className="btn btn-secondary"
              onClick={handleSendReminder}
              disabled={!availableActions.canRemind}
              data-testid="send-reminder-button"
              type="button"
              style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <IconMail size={16} />
              <span>Reminder</span>
            </button>
          )}

          {/* Tertiary Action: On Hold */}
          {availableActions.canHold && (
            <button
              className="btn btn-secondary"
              onClick={handlePutOnHold}
              disabled={!availableActions.canHold}
              data-testid="hold-button"
              type="button"
              style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <IconClock size={16} />
              <span>On Hold</span>
            </button>
          )}

          {/* Tertiary Action: Deny */}
          {availableActions.canDeny && (
            <button
              className="btn btn-secondary"
              onClick={handleDenyApplication}
              disabled={!availableActions.canDeny}
              data-testid="deny-application-button"
              type="button"
              style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
            >
              <IconX size={16} />
              <span>Deny</span>
            </button>
          )}
        </Group>
      </Group>

      {/* Single Column Layout - Removed right sidebar as per requirements */}
      <Grid>
        <Grid.Col span={12}>
          <Stack gap="md">
            {/* Application Details Section */}
            {/* Application Information - Inline layout for short answers, long answers at bottom */}
            <Card p="xl" data-testid="application-information-section">
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Application Details
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

      {/* Admin Notes Section */}
      <Card p="xl">
        <Group justify="space-between" align="center" mb="md">
          <Title order={3} style={{ color: '#880124' }}>
            Notes
          </Title>
          <button
            className={newNote.trim() ? "btn btn-primary" : "btn"}
            onClick={handleSaveNote}
            disabled={!newNote.trim()}
            data-testid="save-note-button"
            type="button"
          >
            Save Note
          </button>
        </Group>

        <Stack gap="md">
          {/* Add Note Text Area */}
          <Textarea
            placeholder="Add a note about this application..."
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

          {/* Notes List */}
          {application.notes.length > 0 ? (
            <Stack gap="sm">
              {application.notes.map((note) => {
                // Check if this is a system-generated status change note
                const { isSystem, status } = isSystemGeneratedNote(note.content);

                return (
                  <Paper key={note.id} p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
                    <Group justify="space-between" mb="xs">
                      <Group gap="xs">
                        {/* Show status badge for system-generated notes */}
                        {isSystem && status ? (
                          <VettingStatusBadge status={status} size="sm" />
                        ) : (
                          <IconNotes size={16} style={{ color: '#880124' }} />
                        )}
                        <Text fw={600} size="sm">{note.reviewerName}</Text>
                      </Group>
                      <Text size="sm" c="dimmed">
                        {formatTime(note.createdAt)}
                      </Text>
                    </Group>
                    <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                      {note.content}
                    </Text>
                    {note.tags && note.tags.length > 0 && (
                      <Group gap="xs" mt="xs">
                        {note.tags.map((tag, idx) => (
                          <Badge key={idx} size="sm" variant="light" color="gray">
                            {tag}
                          </Badge>
                        ))}
                      </Group>
                    )}
                  </Paper>
                );
              })}
            </Stack>
          ) : (
            <Text c="dimmed" size="sm" ta="center" py="md">
              No notes added yet
            </Text>
          )}
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
              styles={{
                root: {
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2'
                }
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
              styles={{
                root: {
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2'
                }
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

      {/* Schedule Interview Modal - Placeholder */}
      <Modal
        opened={scheduleInterviewModalOpen}
        onClose={() => setScheduleInterviewModalOpen(false)}
        title="Schedule Interview"
        centered
        size="md"
      >
        <Stack gap="md">
          <Alert color="blue" title="Coming Soon">
            <Text size="sm">
              Interview scheduling functionality will be implemented in the next phase.
              For now, please coordinate interview times manually with the applicant.
            </Text>
          </Alert>
          <Group justify="flex-end">
            <Button
              onClick={() => setScheduleInterviewModalOpen(false)}
              styles={{
                root: {
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2'
                }
              }}
            >
              Close
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
};