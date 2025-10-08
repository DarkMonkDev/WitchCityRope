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
        nextStatus: 'InterviewCompleted',
        description: 'Interview completed via Calendly',
        icon: IconCalendarEvent
      },
      InterviewCompleted: {
        label: 'Advance to Final Review',
        nextStatus: 'FinalReview',
        description: 'Move to final review',
        icon: IconCheck
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

  const handleAdvanceStage = () => {
    if (!application || !nextStageConfig) return;

    const reasoning = `Advanced to ${nextStageConfig.label}: ${nextStageConfig.description}`;

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
      const reasoning = 'Application approved - skipped to final approval';

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

  // Enhanced debugging for the application detail
  console.log('VettingApplicationDetail render:', {
    applicationId,
    isLoading,
    error: error?.message || error,
    hasApplication: !!application,
    applicationStatus: application?.status,
    timestamp: new Date().toISOString()
  });

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
    <Stack gap="xs">
      {/* Breadcrumb Navigation with Status Badge */}
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
        <VettingStatusBadge status={application.status} size="xl" data-testid="status-badge" />
      </Group>

      {/* Header Section - Just the name */}
      <Paper p="lg" radius="md" style={{ background: '#FFF8F0' }}>
        <Title order={1} style={{ color: '#880124', fontSize: '32px' }} data-testid="application-title">
          {application.sceneName}
        </Title>
      </Paper>

      {/* Action Buttons - Single Horizontal Row */}
      <Group gap="md" wrap="nowrap">
        {/* Primary Action 1: Advance to Next Stage */}
        {nextStageConfig && availableActions.canAdvanceStage && (
          <Button
            leftSection={React.createElement(nextStageConfig.icon, { size: 20 })}
            onClick={handleAdvanceStage}
            loading={isSubmittingDecision || isApprovingApplication}
            disabled={!availableActions.canAdvanceStage}
            data-testid="advance-stage-button"
            styles={{
              root: {
                background: 'linear-gradient(135deg, #9D4EDD, #7B2CBF)',
                height: '48px',
                fontSize: '14px',
                fontWeight: 600,
                borderRadius: '8px',
                boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
                transition: 'all 0.3s ease',
                '&:hover': {
                  boxShadow: '0 6px 20px rgba(157, 78, 221, 0.6)',
                  transform: 'scale(1.02)'
                },
                '&:disabled': {
                  background: 'linear-gradient(135deg, #9D4EDD, #7B2CBF)',
                  opacity: 0.5
                }
              }
            }}
          >
            {nextStageConfig.label}
          </Button>
        )}

        {/* Primary Action 2: Skip to Approved */}
        {availableActions.canSkipToApproved && (
          <Button
            variant="filled"
            color="green"
            leftSection={<IconCheck size={18} />}
            onClick={handleSkipToApproved}
            loading={isApprovingApplication}
            disabled={!availableActions.canSkipToApproved}
            data-testid="skip-to-approved-button"
            styles={{
              root: {
                height: '48px',
                fontWeight: 600,
                borderRadius: '8px',
                fontSize: '14px'
              }
            }}
          >
            Skip to Approved
          </Button>
        )}

        {/* Secondary Action: On Hold */}
        {availableActions.canHold && (
          <Button
            variant="outline"
            color="yellow"
            leftSection={<IconClock size={16} />}
            onClick={handlePutOnHold}
            disabled={!availableActions.canHold}
            data-testid="hold-button"
            styles={{
              root: {
                height: '48px',
                borderWidth: '1px',
                fontWeight: 500,
                fontSize: '14px'
              }
            }}
          >
            On Hold
          </Button>
        )}

        {/* Secondary Action: Reminder */}
        <Button
          variant="outline"
          color="gray"
          leftSection={<IconMail size={16} />}
          onClick={handleSendReminder}
          disabled={!availableActions.canRemind}
          data-testid="send-reminder-button"
          styles={{
            root: {
              height: '48px',
              borderWidth: '1px',
              fontWeight: 500,
              fontSize: '14px'
            }
          }}
        >
          Reminder
        </Button>

        {/* Secondary Action: Deny */}
        {availableActions.canDeny && (
          <Button
            variant="outline"
            color="red"
            leftSection={<IconX size={16} />}
            onClick={handleDenyApplication}
            disabled={!availableActions.canDeny}
            data-testid="deny-application-button"
            styles={{
              root: {
                height: '48px',
                borderWidth: '1px',
                fontWeight: 500,
                fontSize: '14px'
              }
            }}
          >
            Deny
          </Button>
        )}
      </Group>

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

      {/* Status History Timeline Section */}
      <Card>
        <Title order={3} mb="md" style={{ color: '#880124' }}>
          Status History
        </Title>
        <Timeline active={application.decisions.length - 1} bulletSize={24} lineWidth={2}>
          {application.decisions.length > 0 ? (
            application.decisions
              .slice()
              .reverse()
              .map((decision, index) => (
                <Timeline.Item
                  key={decision.id}
                  bullet={<IconClock size={12} />}
                  title={
                    <Group gap="sm">
                      <VettingStatusBadge status={decision.decisionType} size="sm" />
                      <Text fw={600}>{decision.decisionType}</Text>
                    </Group>
                  }
                >
                  <Text size="xs" c="dimmed" mt={4}>
                    {formatDate(decision.createdAt)} - By {decision.reviewerName}
                  </Text>
                  {decision.reasoning && (
                    <Text size="sm" mt="xs" style={{ whiteSpace: 'pre-wrap' }}>
                      {decision.reasoning}
                    </Text>
                  )}
                </Timeline.Item>
              ))
          ) : (
            <Text c="dimmed" size="sm">
              No status changes yet
            </Text>
          )}
        </Timeline>
      </Card>

      {/* Admin Notes Section */}
      <Card>
        <Group justify="space-between" align="center" mb="md">
          <Title order={3} style={{ color: '#880124' }}>
            Admin Notes
          </Title>
          <Button
            variant="filled"
            onClick={handleSaveNote}
            disabled={!newNote.trim()}
            data-testid="save-note-button"
            styles={{
              root: {
                backgroundColor: '#D4AF37',
                color: '#000000',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2',
                fontWeight: 600
              }
            }}
          >
            Save Note
          </Button>
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
              {application.notes.map((note) => (
                <Paper key={note.id} p="md" style={{ background: '#F5F5F5', borderRadius: '8px' }}>
                  <Group justify="space-between" mb="xs">
                    <Group gap="xs">
                      <IconNotes size={16} style={{ color: '#880124' }} />
                      <Text fw={600} size="sm">{note.reviewerName}</Text>
                    </Group>
                    <Text size="sm" c="dimmed">
                      {formatDate(note.createdAt)}
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
              ))}
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