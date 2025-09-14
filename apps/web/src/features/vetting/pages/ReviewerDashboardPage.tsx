// Reviewer dashboard page for vetting team members
import React, { useState } from 'react';
import {
  Container,
  Box,
  Paper,
  Text,
  Group,
  Stack,
  Title,
  Modal,
  Tabs,
  Alert,
  Button,
  Badge,
  SimpleGrid,
  Textarea,
  Select,
  ActionIcon,
  Divider,
  LoadingOverlay
} from '@mantine/core';
import {
  IconUser,
  IconEye,
  IconNotes,
  IconCheck,
  IconX,
  IconInfoCircle,
  IconClock,
  IconAlertTriangle,
  IconArrowLeft,
  IconShieldCheck
} from '@tabler/icons-react';
import { useAuthStore } from '../../../stores/authStore';
import { ReviewerDashboard } from '../components/reviewer/ReviewerDashboard';
import { useQuery } from '@tanstack/react-query';
import { vettingApi } from '../api/vettingApi';
import type { ApplicationSummaryDto, ApplicationDetailResponse } from '../types/vetting.types';
import { APPLICATION_STATUS_CONFIGS, EXPERIENCE_LEVEL_CONFIGS } from '../types/vetting.types';

interface ReviewerDashboardPageProps {
  className?: string;
}

export const ReviewerDashboardPage: React.FC<ReviewerDashboardPageProps> = ({
  className
}) => {
  const [selectedApplication, setSelectedApplication] = useState<ApplicationSummaryDto | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<string>('details');

  const { user } = useAuthStore();

  // Get detailed application data when one is selected
  const { 
    data: applicationDetail, 
    isLoading: isLoadingDetail,
    error: detailError
  } = useQuery({
    queryKey: ['application-detail', selectedApplication?.id],
    queryFn: () => selectedApplication ? vettingApi.getApplicationDetail(selectedApplication.id) : null,
    enabled: !!selectedApplication,
    retry: 1
  });

  const handleApplicationSelect = (application: ApplicationSummaryDto) => {
    setSelectedApplication(application);
    setIsDetailModalOpen(true);
    setActiveTab('details');
  };

  const handleCloseModal = () => {
    setIsDetailModalOpen(false);
    setSelectedApplication(null);
    setActiveTab('details');
  };

  // Check if user has reviewer permissions
  const hasReviewerAccess = user?.roles?.includes('VettingReviewer') || user?.roles?.includes('VettingAdmin');

  if (!hasReviewerAccess) {
    return (
      <Container size="md" py="xl" className={className}>
        <Alert
          icon={<IconShieldCheck />}
          color="red"
          title="Access Denied"
        >
          <Text size="sm">
            You don't have permission to access the vetting dashboard. This area is restricted 
            to approved vetting team members only.
          </Text>
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="xl" py="lg" className={className}>
      {/* Main Dashboard */}
      <ReviewerDashboard onApplicationSelect={handleApplicationSelect} />

      {/* Application Detail Modal */}
      <Modal
        opened={isDetailModalOpen}
        onClose={handleCloseModal}
        title={
          <Group gap="md">
            <ActionIcon variant="subtle" onClick={handleCloseModal}>
              <IconArrowLeft size={18} />
            </ActionIcon>
            <Box>
              <Text size="lg" fw={600} c="wcr.7">
                Application Review
              </Text>
              {selectedApplication && (
                <Text size="sm" c="dimmed">
                  #{selectedApplication.applicationNumber} • {selectedApplication.isAnonymous ? 'Anonymous' : selectedApplication.sceneName}
                </Text>
              )}
            </Box>
          </Group>
        }
        size="xl"
        centered
        overlayProps={{ opacity: 0.55, blur: 3 }}
        styles={{
          modal: { maxHeight: '90vh', overflow: 'hidden' },
          body: { padding: 0, height: '100%' }
        }}
      >
        <Box pos="relative" style={{ height: '80vh' }}>
          <LoadingOverlay visible={isLoadingDetail} />
          
          {detailError && (
            <Alert
              icon={<IconAlertTriangle />}
              color="red"
              title="Error Loading Application"
              m="md"
            >
              <Text size="sm">
                Unable to load application details. Please try again or contact support.
              </Text>
            </Alert>
          )}

          {applicationDetail && (
            <Tabs value={activeTab} onTabChange={setActiveTab} keepMounted={false} style={{ height: '100%' }}>
              <Tabs.List grow>
                <Tabs.Tab value="details" icon={<IconEye size={16} />}>
                  Application Details
                </Tabs.Tab>
                <Tabs.Tab value="references" icon={<IconUser size={16} />}>
                  References
                </Tabs.Tab>
                <Tabs.Tab value="review" icon={<IconNotes size={16} />}>
                  Review & Decision
                </Tabs.Tab>
              </Tabs.List>

              {/* Application Details Tab */}
              <Tabs.Panel value="details" p="md" style={{ height: 'calc(100% - 42px)', overflowY: 'auto' }}>
                <Stack gap="lg">
                  {/* Status & Assignment */}
                  <Paper p="md" withBorder>
                    <Group justify="apart" mb="md">
                      <Group gap="md">
                        <Badge
                          color={APPLICATION_STATUS_CONFIGS[applicationDetail.status]?.color || 'gray'}
                          variant="filled"
                          size="lg"
                        >
                          {APPLICATION_STATUS_CONFIGS[applicationDetail.status]?.label || applicationDetail.status}
                        </Badge>
                        
                        {applicationDetail.assignedReviewerName && (
                          <Text size="sm" c="dimmed">
                            Assigned to: {applicationDetail.assignedReviewerName}
                          </Text>
                        )}
                      </Group>
                      
                      <Group gap="xs">
                        <IconClock size={16} />
                        <Text size="sm" c="dimmed">
                          Submitted {new Date(applicationDetail.submittedAt).toLocaleDateString()}
                        </Text>
                      </Group>
                    </Group>
                  </Paper>

                  {/* Personal Information */}
                  <Paper p="md" withBorder>
                    <Text fw={600} size="md" mb="md" c="wcr.7">
                      Personal Information
                    </Text>
                    <SimpleGrid cols={2} breakpoints={[{ maxWidth: 'sm', cols: 1 }]}>
                      <Box>
                        <Text size="sm" fw={500} c="dimmed">Full Name</Text>
                        <Text size="sm">{applicationDetail.personalInfo.fullName}</Text>
                      </Box>
                      <Box>
                        <Text size="sm" fw={500} c="dimmed">Scene Name</Text>
                        <Text size="sm">{applicationDetail.personalInfo.sceneName}</Text>
                      </Box>
                      {applicationDetail.personalInfo.pronouns && (
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Pronouns</Text>
                          <Text size="sm">{applicationDetail.personalInfo.pronouns}</Text>
                        </Box>
                      )}
                      <Box>
                        <Text size="sm" fw={500} c="dimmed">Email</Text>
                        <Text size="sm">{applicationDetail.personalInfo.email}</Text>
                      </Box>
                      {applicationDetail.personalInfo.phone && (
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Phone</Text>
                          <Text size="sm">{applicationDetail.personalInfo.phone}</Text>
                        </Box>
                      )}
                    </SimpleGrid>
                  </Paper>

                  {/* Experience */}
                  <Paper p="md" withBorder>
                    <Text fw={600} size="md" mb="md" c="wcr.7">
                      Experience & Knowledge
                    </Text>
                    <Stack gap="md">
                      <SimpleGrid cols={2} breakpoints={[{ maxWidth: 'sm', cols: 1 }]}>
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Experience Level</Text>
                          <Text size="sm">{EXPERIENCE_LEVEL_CONFIGS[applicationDetail.experience.level]?.label}</Text>
                        </Box>
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Years of Experience</Text>
                          <Text size="sm">{applicationDetail.experience.yearsExperience} years</Text>
                        </Box>
                      </SimpleGrid>
                      
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Experience Description</Text>
                        <Paper p="sm" bg="gray.0" withBorder>
                          <Text size="sm">{applicationDetail.experience.description}</Text>
                        </Paper>
                      </Box>
                      
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Safety Knowledge</Text>
                        <Paper p="sm" bg="gray.0" withBorder>
                          <Text size="sm">{applicationDetail.experience.safetyKnowledge}</Text>
                        </Paper>
                      </Box>
                      
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Consent Understanding</Text>
                        <Paper p="sm" bg="gray.0" withBorder>
                          <Text size="sm">{applicationDetail.experience.consentUnderstanding}</Text>
                        </Paper>
                      </Box>
                    </Stack>
                  </Paper>

                  {/* Community Understanding */}
                  <Paper p="md" withBorder>
                    <Text fw={600} size="md" mb="md" c="wcr.7">
                      Community Understanding
                    </Text>
                    <Stack gap="md">
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Why Join Community</Text>
                        <Paper p="sm" bg="gray.0" withBorder>
                          <Text size="sm">{applicationDetail.community.whyJoin}</Text>
                        </Paper>
                      </Box>
                      
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Skills & Interests</Text>
                        <Group gap="xs">
                          {applicationDetail.community.skillsInterests.map(skill => (
                            <Badge key={skill} size="sm" color="wcr.7" variant="light">
                              {skill}
                            </Badge>
                          ))}
                        </Group>
                      </Box>
                      
                      <Box>
                        <Text size="sm" fw={500} c="dimmed" mb="xs">Expectations & Goals</Text>
                        <Paper p="sm" bg="gray.0" withBorder>
                          <Text size="sm">{applicationDetail.community.expectations}</Text>
                        </Paper>
                      </Box>
                    </Stack>
                  </Paper>
                </Stack>
              </Tabs.Panel>

              {/* References Tab */}
              <Tabs.Panel value="references" p="md" style={{ height: 'calc(100% - 42px)', overflowY: 'auto' }}>
                <Stack gap="lg">
                  {applicationDetail.references.map((reference, index) => (
                    <Paper key={reference.id} p="md" withBorder>
                      <Group justify="apart" mb="md">
                        <Text fw={600} size="md" c="wcr.7">
                          Reference #{index + 1}
                        </Text>
                        <Badge
                          color={reference.status === 'responded' ? 'green' : reference.status === 'contacted' ? 'yellow' : 'gray'}
                          variant="filled"
                        >
                          {reference.status}
                        </Badge>
                      </Group>
                      
                      <SimpleGrid cols={2} breakpoints={[{ maxWidth: 'sm', cols: 1 }]} mb="md">
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Name</Text>
                          <Text size="sm">{reference.name}</Text>
                        </Box>
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Email</Text>
                          <Text size="sm">{reference.email}</Text>
                        </Box>
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Relationship</Text>
                          <Text size="sm">{reference.relationship}</Text>
                        </Box>
                        <Box>
                          <Text size="sm" fw={500} c="dimmed">Contact Status</Text>
                          <Text size="sm">
                            {reference.contactedAt 
                              ? `Contacted ${new Date(reference.contactedAt).toLocaleDateString()}`
                              : 'Not contacted yet'
                            }
                          </Text>
                        </Box>
                      </SimpleGrid>
                      
                      {reference.response && (
                        <Paper p="sm" bg="green.0" withBorder>
                          <Text size="sm" fw={500} mb="xs">Reference Response</Text>
                          <Stack gap="xs">
                            <Text size="xs"><strong>Recommendation:</strong> {reference.response.recommendation}</Text>
                            <Text size="xs"><strong>Relationship Duration:</strong> {reference.response.relationshipDuration}</Text>
                            <Text size="xs"><strong>Experience Assessment:</strong> {reference.response.experienceAssessment}</Text>
                            {reference.response.additionalComments && (
                              <Text size="xs"><strong>Additional Comments:</strong> {reference.response.additionalComments}</Text>
                            )}
                          </Stack>
                        </Paper>
                      )}
                    </Paper>
                  ))}
                </Stack>
              </Tabs.Panel>

              {/* Review & Decision Tab */}
              <Tabs.Panel value="review" p="md" style={{ height: 'calc(100% - 42px)', overflowY: 'auto' }}>
                <Stack gap="lg">
                  {/* Previous Notes */}
                  {applicationDetail.reviewNotes && applicationDetail.reviewNotes.length > 0 && (
                    <Paper p="md" withBorder>
                      <Text fw={600} size="md" mb="md" c="wcr.7">
                        Review Notes
                      </Text>
                      <Stack gap="sm">
                        {applicationDetail.reviewNotes.map(note => (
                          <Paper key={note.id} p="sm" bg="blue.0" withBorder>
                            <Group justify="apart" mb="xs">
                              <Text size="sm" fw={500}>{note.reviewerName}</Text>
                              <Text size="xs" c="dimmed">
                                {new Date(note.createdAt).toLocaleDateString()}
                              </Text>
                            </Group>
                            <Text size="sm">{note.content}</Text>
                          </Paper>
                        ))}
                      </Stack>
                    </Paper>
                  )}

                  {/* Decision Actions */}
                  <Paper p="md" withBorder>
                    <Text fw={600} size="md" mb="md" c="wcr.7">
                      Review Decision
                    </Text>
                    
                    <Stack gap="md">
                      <Textarea
                        label="Review Notes"
                        placeholder="Add your review notes here..."
                        minRows={3}
                        maxRows={6}
                      />
                      
                      <Select
                        label="Decision"
                        placeholder="Select decision"
                        data={[
                          { value: 'approve', label: 'Approve Application' },
                          { value: 'request-info', label: 'Request Additional Information' },
                          { value: 'schedule-interview', label: 'Schedule Interview' },
                          { value: 'deny', label: 'Deny Application' }
                        ]}
                      />
                      
                      <Group justify="right" gap="md">
                        <Button variant="outline" color="gray">
                          Save Draft
                        </Button>
                        <Button color="green" leftSection={<IconCheck size={16} />}>
                          Submit Decision
                        </Button>
                      </Group>
                    </Stack>
                  </Paper>
                </Stack>
              </Tabs.Panel>
            </Tabs>
          )}
        </Box>
      </Modal>
    </Container>
  );
};