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
      {/* Header - Simplified since page wrapper handles back navigation */}
      <Group justify="space-between" align="center">
        <div>
          <Title order={2} style={{ color: '#880124' }}>
            Application #{application.applicationNumber}
          </Title>
          <Text c="dimmed">{application.sceneName}</Text>
        </div>
        <Group>
          <VettingStatusBadge status={application.status} size="md" />
          <Button
            leftSection={<IconNotes size={16} />}
            variant="light"
            onClick={() => setNotesModalOpen(true)}
          >
            Add Note
          </Button>
        </Group>
      </Group>

      <Grid>
        {/* Left Column - Application Details */}
        <Grid.Col span={{ base: 12, lg: 8 }}>
          <Stack gap="md">
            {/* Basic Information */}
            <Card>
              <Title order={3} mb="md" style={{ color: '#880124' }}>
                Applicant Information
              </Title>
              <Grid>
                <Grid.Col span={6}>
                  <Stack gap="xs">
                    <Group>
                      <IconUser size={16} />
                      <Text fw={600}>Full Name:</Text>
                    </Group>
                    <Text ml={24}>{application.fullName}</Text>
                  </Stack>
                </Grid.Col>
                <Grid.Col span={6}>
                  <Stack gap="xs">
                    <Text fw={600}>Scene Name:</Text>
                    <Text>{application.sceneName}</Text>
                  </Stack>
                </Grid.Col>
                <Grid.Col span={6}>
                  <Stack gap="xs">
                    <Group>
                      <IconMail size={16} />
                      <Text fw={600}>Email:</Text>
                    </Group>
                    <Text ml={24}>{application.email}</Text>
                  </Stack>
                </Grid.Col>
                <Grid.Col span={6}>
                  <Stack gap="xs">
                    <Group>
                      <IconPhone size={16} />
                      <Text fw={600}>Phone:</Text>
                    </Group>
                    <Text ml={24}>{application.phone || 'Not provided'}</Text>
                  </Stack>
                </Grid.Col>
                {application.pronouns && (
                  <Grid.Col span={6}>
                    <Stack gap="xs">
                      <Text fw={600}>Pronouns:</Text>
                      <Text>{application.pronouns}</Text>
                    </Stack>
                  </Grid.Col>
                )}
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