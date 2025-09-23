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
import { VettingStatusBadge } from './VettingStatusBadge';
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
  const [newStatus, setNewStatus] = useState('');
  const [reviewReasoning, setReviewReasoning] = useState('');

  const { data: application, isLoading, error, refetch } = useVettingApplicationDetail(applicationId);
  const { mutate: submitDecision, isPending: isSubmittingDecision } = useSubmitReviewDecision(
    () => {
      setNewStatus('');
      setReviewReasoning('');
      refetch();
    }
  );

  const handleStatusChange = () => {
    if (!newStatus || !reviewReasoning.trim()) {
      return;
    }

    const request: ReviewDecisionRequest = {
      decisionType: newStatus,
      reasoning: reviewReasoning,
      isFinalDecision: ['Approved', 'Rejected'].includes(newStatus)
    };

    submitDecision({ applicationId, decision: request });
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatDateOnly = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
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
        <Button leftSection={<IconArrowLeft size={16} />} onClick={onBack}>
          Back to Applications
        </Button>
      </Paper>
    );
  }

  const statusOptions = [
    { value: 'InReview', label: 'Move to In Review' },
    { value: 'PendingReferences', label: 'Request References' },
    { value: 'InterviewScheduled', label: 'Schedule Interview' },
    { value: 'Approved', label: 'Approve Application' },
    { value: 'Rejected', label: 'Reject Application' },
    { value: 'OnHold', label: 'Put On Hold' }
  ];

  return (
    <Stack gap="md">
      {/* Header Section - Match wireframe exactly */}
      <Stack gap="sm">
        {/* Title Row with Status Badge */}
        <Group justify="space-between" align="center">
          <Title order={2} style={{ color: '#880124' }}>
            Application - {application.fullName} ({application.sceneName})
          </Title>
          <VettingStatusBadge status={application.status} size="md" />
        </Group>

        {/* Action Buttons Row - Three buttons as per wireframe */}
        <Group gap="md">
          <Button
            leftSection={<IconCheck size={16} />}
            style={{ backgroundColor: '#FFC107', color: '#000' }}
            onClick={() => {
              setNewStatus('Approved');
              setReviewReasoning('');
            }}
            disabled={application.status === 'Approved'}
          >
            APPROVE APPLICATION
          </Button>
          <Button
            variant="outline"
            color="gray"
            onClick={() => {
              setNewStatus('OnHold');
              setReviewReasoning('');
            }}
            disabled={application.status === 'OnHold'}
          >
            PUT ON HOLD
          </Button>
          <Button
            color="red"
            leftSection={<IconX size={16} />}
            onClick={() => {
              setNewStatus('Rejected');
              setReviewReasoning('');
            }}
            disabled={application.status === 'Rejected'}
          >
            DENY APPLICATION
          </Button>
        </Group>
      </Stack>

      <Grid>
        {/* Left Column - Application Details */}
        <Grid.Col span={{ base: 12, lg: 8 }}>
          <Stack gap="md">
            {/* Application Information - 2-column layout as per wireframe */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Application Information
              </Title>
              <Grid>
                {/* Left Column */}
                <Grid.Col span={6}>
                  <Stack gap="md">
                    <div>
                      <Text fw={600} mb="xs">Scene Name:</Text>
                      <Text>{application.sceneName}</Text>
                    </div>
                    <div>
                      <Text fw={600} mb="xs">Real Name:</Text>
                      <Text>{application.fullName}</Text>
                    </div>
                    <div>
                      <Text fw={600} mb="xs">Email:</Text>
                      <Text>{application.email}</Text>
                    </div>
                    {application.pronouns && (
                      <div>
                        <Text fw={600} mb="xs">Pronouns:</Text>
                        <Text>{application.pronouns}</Text>
                      </div>
                    )}
                    <div>
                      <Text fw={600} mb="xs">Other Names/Handles:</Text>
                      <Text>{application.phone || 'Not provided'}</Text>
                    </div>
                    <div>
                      <Text fw={600} mb="xs">Tell Us About Yourself:</Text>
                      <Text style={{ whiteSpace: 'pre-wrap' }}>
                        {application.whyJoinCommunity}
                      </Text>
                    </div>
                  </Stack>
                </Grid.Col>

                {/* Right Column */}
                <Grid.Col span={6}>
                  <Stack gap="md">
                    <div>
                      <Text fw={600} mb="xs">Application Date:</Text>
                      <Text>{formatDateOnly(application.submittedAt)}</Text>
                    </div>
                    <div>
                      <Text fw={600} mb="xs">FetLife Handle:</Text>
                      <Text>@{application.sceneName}</Text>
                    </div>
                    <div>
                      <Text fw={600} mb="xs">How Found Us:</Text>
                      <Text>{application.experienceDescription}</Text>
                    </div>
                  </Stack>
                </Grid.Col>
              </Grid>
            </Card>

            {/* Experience Information */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Experience & Background
              </Title>
              <Stack gap="md">
                <Group>
                  <Badge color="blue" variant="light">
                    {application.experienceLevel}
                  </Badge>
                  <Text>•</Text>
                  <Text>{application.yearsExperience} years experience</Text>
                </Group>

                <div>
                  <Text fw={600} mb="xs">Experience Description:</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.experienceDescription}
                  </Text>
                </div>

                <div>
                  <Text fw={600} mb="xs">Safety Knowledge:</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.safetyKnowledge}
                  </Text>
                </div>

                <div>
                  <Text fw={600} mb="xs">Consent Understanding:</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.consentUnderstanding}
                  </Text>
                </div>
              </Stack>
            </Card>

            {/* Community Information */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Community Interest
              </Title>
              <Stack gap="md">
                <div>
                  <Text fw={600} mb="xs">Why Join Community:</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.whyJoinCommunity}
                  </Text>
                </div>

                <div>
                  <Text fw={600} mb="xs">Skills & Interests:</Text>
                  <Group gap="xs">
                    {application.skillsInterests.map((skill, index) => (
                      <Badge key={index} variant="dot" color="gray">
                        {skill}
                      </Badge>
                    ))}
                  </Group>
                </div>

                <div>
                  <Text fw={600} mb="xs">Expectations & Goals:</Text>
                  <Text style={{ whiteSpace: 'pre-wrap' }}>
                    {application.expectationsGoals}
                  </Text>
                </div>
              </Stack>
            </Card>

            {/* References */}
            {application.references.length > 0 && (
              <Card>
                <Title order={3} mb="md" style={{ color: '#880124' }}>
                  References
                </Title>
                <Stack gap="md">
                  {application.references.map((reference, index) => (
                    <Paper key={reference.id} p="md" style={{ background: '#FFF8F0' }}>
                      <Group justify="space-between" mb="xs">
                        <Text fw={600}>{reference.name}</Text>
                        <VettingStatusBadge status={reference.status} size="xs" />
                      </Group>
                      <Text size="sm" c="dimmed" mb="xs">
                        {reference.email} • {reference.relationship}
                      </Text>
                      {reference.contactedAt && (
                        <Text size="xs" c="dimmed">
                          Contacted: {formatDateOnly(reference.contactedAt)}
                        </Text>
                      )}
                      {reference.respondedAt && (
                        <Text size="xs" c="dimmed">
                          Responded: {formatDateOnly(reference.respondedAt)}
                        </Text>
                      )}
                    </Paper>
                  ))}
                </Stack>
              </Card>
            )}
          </Stack>
        </Grid.Col>

        {/* Right Column - Review Actions & History */}
        <Grid.Col span={{ base: 12, lg: 4 }}>
          <Stack gap="md">
            {/* Review Actions */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Review Actions
              </Title>
              <Stack gap="md">
                <div>
                  <Text size="sm" fw={600} mb="xs">Change Status:</Text>
                  <Select
                    placeholder="Select new status"
                    data={statusOptions}
                    value={newStatus}
                    onChange={(value) => setNewStatus(value || '')}
                  />
                </div>

                {newStatus && (
                  <div>
                    <Text size="sm" fw={600} mb="xs">Reasoning (Required):</Text>
                    <Textarea
                      placeholder="Provide reasoning for this status change..."
                      value={reviewReasoning}
                      onChange={(e) => setReviewReasoning(e.currentTarget.value)}
                      minRows={3}
                      required
                    />
                  </div>
                )}

                <Button
                  fullWidth
                  leftSection={<IconCheck size={16} />}
                  onClick={handleStatusChange}
                  disabled={!newStatus || !reviewReasoning.trim()}
                  loading={isSubmittingDecision}
                >
                  Submit Decision
                </Button>
              </Stack>
            </Card>

            {/* Application Timeline */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Timeline
              </Title>
              <Timeline active={-1} bulletSize={20} lineWidth={2}>
                <Timeline.Item
                  bullet={<IconCalendar size={12} />}
                  title="Application Submitted"
                >
                  <Text c="dimmed" size="sm">
                    {formatDate(application.submittedAt)}
                  </Text>
                </Timeline.Item>

                {application.reviewStartedAt && (
                  <Timeline.Item
                    bullet={<IconUser size={12} />}
                    title="Review Started"
                  >
                    <Text c="dimmed" size="sm">
                      {formatDate(application.reviewStartedAt)}
                    </Text>
                    {application.assignedReviewerName && (
                      <Text c="dimmed" size="sm">
                        Assigned to: {application.assignedReviewerName}
                      </Text>
                    )}
                  </Timeline.Item>
                )}

                {application.decisions.map((decision) => (
                  <Timeline.Item
                    key={decision.id}
                    bullet={<IconCheck size={12} />}
                    title={`Decision: ${decision.decisionType}`}
                  >
                    <Text c="dimmed" size="sm">
                      {formatDate(decision.createdAt)}
                    </Text>
                    <Text c="dimmed" size="sm">
                      By: {decision.reviewerName}
                    </Text>
                    {decision.reasoning && (
                      <Text size="sm" mt="xs">
                        {decision.reasoning}
                      </Text>
                    )}
                  </Timeline.Item>
                ))}
              </Timeline>
            </Card>

            {/* Notes */}
            {application.notes.length > 0 && (
              <Card>
                <Title order={3} mb="md" style={{ color: '#880124' }}>
                  Notes
                </Title>
                <Stack gap="md">
                  {application.notes.slice(0, 3).map((note) => (
                    <Paper key={note.id} p="sm" style={{ background: '#FFF8F0' }}>
                      <Text size="sm" mb="xs">
                        {note.content}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {note.reviewerName} • {formatDate(note.createdAt)}
                      </Text>
                    </Paper>
                  ))}
                  {application.notes.length > 3 && (
                    <Text size="xs" c="dimmed" ta="center">
                      +{application.notes.length - 3} more notes
                    </Text>
                  )}
                </Stack>
              </Card>
            )}
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
            color="red"
            size="sm"
            onClick={() => {
              // TODO: Implement save note functionality
              console.log('Save note clicked:', newNote);
            }}
            disabled={!newNote.trim()}
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
            <Button variant="light" onClick={() => setNotesModalOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                // TODO: Implement add note functionality
                setNewNote('');
                setNotesModalOpen(false);
              }}
              disabled={!newNote.trim()}
            >
              Add Note
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
};