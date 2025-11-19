import React, { useState, useEffect } from 'react';
import {
  Stack,
  Group,
  Box,
  Paper,
  Title,
  Text,
  Select,
  TextInput,
  Button,
  Modal,
  Alert,
  Divider,
} from '@mantine/core';
import { IconAlertCircle, IconCheck, IconSend } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';
import {
  emailTemplatesApi,
  type UserSegmentDto,
  type UserPreviewDto,
  type UserSegment,
} from '../../services/emailTemplates.api';

/**
 * Available variables for ad-hoc emails
 * These are the only variables allowed in ad-hoc email templates
 */
const AD_HOC_VARIABLES = [
  '{{user_name}}',
  '{{reset_url}}',
  '{{verification_url}}',
];

/**
 * SendAdHocEmail Component
 *
 * Allows admins to compose and send custom emails to specific user segments.
 * Features:
 * - Segment selector with live recipient counts
 * - Rich text editor for email content
 * - Variable validation
 * - Preview of first 10 recipients
 * - Confirmation dialog before sending
 * - Success/error notifications
 *
 * Based on UI Designer specifications
 */
export const SendAdHocEmail: React.FC = () => {
  const queryClient = useQueryClient();

  // Segment selection state
  const [selectedSegment, setSelectedSegment] = useState<UserSegment | null>(null);

  // Email content state
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');

  // Validation state
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

  // Modal state
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  // Fetch user segments with counts
  const {
    data: segments,
    isLoading: segmentsLoading,
    error: segmentsError,
  } = useQuery<UserSegmentDto[]>({
    queryKey: ['email-segments'],
    queryFn: () => emailTemplatesApi.getUserSegments(),
  });

  // Fetch preview recipients when segment changes
  const {
    data: previewRecipients,
    isLoading: previewLoading,
    error: previewError,
  } = useQuery<UserPreviewDto[]>({
    queryKey: ['email-segment-preview', selectedSegment],
    queryFn: () =>
      selectedSegment
        ? emailTemplatesApi.getSegmentPreview(selectedSegment)
        : Promise.resolve([]),
    enabled: selectedSegment !== null,
  });

  // Send email mutation
  const sendMutation = useMutation({
    mutationFn: (data: { subject: string; htmlBody: string; segment: UserSegment }) => {
      // Generate plain text from HTML
      const plainTextBody = data.htmlBody
        .replace(/<br\s*\/?>/gi, '\n')
        .replace(/<\/p>/gi, '\n\n')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/g, ' ')
        .trim();

      return emailTemplatesApi.sendAdHocEmail({
        subject: data.subject,
        htmlBody: data.htmlBody,
        plainTextBody,
        segment: data.segment,
        recipientEmails: null,
        recipientGroup: data.segment,
      });
    },
    onSuccess: (response) => {
      const currentSegment = segments?.find((s) => s.segment === selectedSegment);
      notifications.show({
        message: `Email sent to ${currentSegment?.count || 0} recipients successfully`,
        color: 'green',
        icon: <IconCheck size={16} />,
      });
      handleReset();
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to send email',
        color: 'red',
        icon: <IconAlertCircle size={16} />,
      });
    },
  });

  // Real-time variable validation
  useEffect(() => {
    if (!subject && !htmlBody) {
      setInvalidVariables([]);
      return;
    }

    // Extract variables from subject + htmlBody
    const variablePattern = /\{\{([a-zA-Z0-9_]+)\}\}/g;
    const extractedVars = new Set<string>();

    // Extract from subject
    let match;
    while ((match = variablePattern.exec(subject)) !== null) {
      extractedVars.add(`{{${match[1]}}}`);
    }

    // Extract from HTML body
    variablePattern.lastIndex = 0; // Reset regex
    while ((match = variablePattern.exec(htmlBody)) !== null) {
      extractedVars.add(`{{${match[1]}}}`);
    }

    // Compare with allowed variables
    const invalid = Array.from(extractedVars).filter((v) => !AD_HOC_VARIABLES.includes(v));
    setInvalidVariables(invalid);
  }, [subject, htmlBody]);

  // Handlers
  const handleSendClick = () => setShowConfirmModal(true);

  const handleConfirmSend = () => {
    if (!selectedSegment) return;

    sendMutation.mutate({
      subject,
      htmlBody,
      segment: selectedSegment,
    });
    setShowConfirmModal(false);
  };

  const handleCancel = () => {
    if (subject || htmlBody) {
      if (window.confirm('Discard unsaved email?')) {
        handleReset();
      }
    } else {
      handleReset();
    }
  };

  const handleReset = () => {
    setSelectedSegment(null);
    setSubject('');
    setHtmlBody('');
    setInvalidVariables([]);
  };

  // Computed validation
  const isValid =
    selectedSegment !== null &&
    subject.trim().length > 0 &&
    htmlBody.trim().length > 0 &&
    invalidVariables.length === 0;

  // Get current segment data
  const currentSegment = segments?.find((s) => s.segment === selectedSegment);
  const recipientCount = currentSegment?.count || 0;

  // Prepare segment options for Select dropdown
  const segmentOptions =
    segments?.map((segment) => ({
      value: segment.segment || '',
      label: `${segment.description || segment.segment} (${segment.count} users)`,
    })) || [];

  return (
    <Stack gap="xl" mt="3xl">
      <Divider size="md" />

      {/* Section Header */}
      <Box>
        <Title
          order={2}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '24px',
            fontWeight: 700,
            color: '#880124',
          }}
        >
          Send Ad-Hoc Email
        </Title>
        <Text size="sm" c="dimmed">
          Send custom emails to specific user groups
        </Text>
      </Box>

      {/* Error Loading Segments */}
      {segmentsError && (
        <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Segments">
          <Text size="sm">Failed to load user segments. Please refresh the page.</Text>
          <Button
            size="xs"
            variant="light"
            onClick={() => queryClient.invalidateQueries({ queryKey: ['email-segments'] })}
            mt="sm"
          >
            Retry
          </Button>
        </Alert>
      )}

      {/* Recipient Selector */}
      {!segmentsError && (
        <Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
          <Stack gap="md">
            <Select
              label="Select Recipients"
              required
              data={segmentOptions}
              value={selectedSegment}
              onChange={(value) => setSelectedSegment(value as UserSegment)}
              placeholder="Choose a user group..."
              searchable
              maxDropdownHeight={300}
              disabled={segmentsLoading}
              styles={{
                label: {
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  fontSize: '14px',
                  color: '#880124',
                  marginBottom: '8px',
                },
              }}
            />

            {selectedSegment && (
              <Text size="lg" fw={600} c="burgundy">
                Selected: {recipientCount} recipients
              </Text>
            )}
          </Stack>
        </Paper>
      )}

      {/* Email Content Editor */}
      <Paper p="xl" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
        <Stack gap="md">
          {/* Subject Line */}
          <TextInput
            label="Subject Line"
            required
            value={subject}
            onChange={(e) => setSubject(e.currentTarget.value)}
            maxLength={200}
            placeholder="Enter email subject..."
            styles={{
              label: {
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                color: '#880124',
              },
            }}
          />

          {/* Available Variables */}
          <Box
            p="sm"
            style={{
              background: 'rgba(136, 1, 36, 0.05)',
              borderRadius: '6px',
              border: '1px solid rgba(136, 1, 36, 0.1)',
            }}
          >
            <Text size="xs" fw={600} c="burgundy" mb={4}>
              Available Variables:
            </Text>
            <Text size="xs" c="dimmed">
              {AD_HOC_VARIABLES.join(', ')}
            </Text>
          </Box>

          {/* Rich Text Editor */}
          <div>
            <Text size="sm" fw={500} mb={4}>
              Email Content (HTML) *
            </Text>
            <MantineTiptapEditor
              value={htmlBody}
              onChange={setHtmlBody}
              placeholder="Compose your email message..."
              minRows={12}
            />
          </div>

          {/* Variable Validation Warning */}
          {invalidVariables.length > 0 && (
            <Alert
              icon={<IconAlertCircle />}
              color="yellow"
              variant="light"
              title="Unknown Variables Detected"
            >
              <Text size="sm">
                These variables are not in the allowed list: {invalidVariables.join(', ')}
              </Text>
              <Text size="xs" mt="xs" c="dimmed">
                Available variables: {AD_HOC_VARIABLES.join(', ')}
              </Text>
            </Alert>
          )}
        </Stack>
      </Paper>

      {/* Recipient Preview */}
      {selectedSegment && (
        <Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
          <Stack gap="xs">
            <Text size="sm" fw={600} c="burgundy">
              Preview Recipients
            </Text>

            {previewLoading ? (
              <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
                Loading preview...
              </Text>
            ) : previewError ? (
              <Text size="sm" c="red">
                Failed to load recipient preview. You can still send the email.
              </Text>
            ) : previewRecipients && previewRecipients.length > 0 ? (
              <>
                <Text size="xs" c="dimmed" mb="xs">
                  First {Math.min(10, recipientCount)} of {recipientCount} recipients:
                </Text>

                <Stack gap={4}>
                  {previewRecipients.slice(0, 10).map((recipient, index) => (
                    <Text key={index} size="sm" c="charcoal">
                      • {recipient.email} ({recipient.sceneName})
                    </Text>
                  ))}
                </Stack>

                {recipientCount > 10 && (
                  <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
                    ... and {recipientCount - 10} more
                  </Text>
                )}
              </>
            ) : (
              <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
                No recipients found for this segment
              </Text>
            )}
          </Stack>
        </Paper>
      )}

      {/* Action Buttons - Desktop */}
      <Group justify="flex-end" gap="sm" visibleFrom="sm">
        <Button variant="light" onClick={handleCancel} disabled={sendMutation.isPending}>
          Cancel
        </Button>

        <Button
          onClick={handleSendClick}
          loading={sendMutation.isPending}
          disabled={!isValid || sendMutation.isPending}
          rightSection={<IconSend size={16} />}
          styles={{
            root: {
              fontWeight: 600,
              height: '44px',
              fontSize: '14px',
            },
          }}
        >
          Send Email
        </Button>
      </Group>

      {/* Action Buttons - Mobile */}
      <Stack gap="sm" hiddenFrom="sm">
        <Button
          onClick={handleSendClick}
          loading={sendMutation.isPending}
          disabled={!isValid || sendMutation.isPending}
          fullWidth
          rightSection={<IconSend size={16} />}
          styles={{
            root: {
              fontWeight: 600,
              height: '48px',
              fontSize: '16px',
            },
          }}
        >
          Send Email
        </Button>

        <Button
          variant="light"
          onClick={handleCancel}
          disabled={sendMutation.isPending}
          fullWidth
        >
          Cancel
        </Button>
      </Stack>

      {/* Confirmation Modal */}
      <Modal
        opened={showConfirmModal}
        onClose={() => setShowConfirmModal(false)}
        title="Confirm Send"
        centered
        size="md"
        styles={{
          title: {
            fontFamily: 'Montserrat, sans-serif',
            fontWeight: 700,
            fontSize: '20px',
            color: '#880124',
          },
        }}
      >
        <Stack gap="md">
          {/* Warning Message */}
          <Alert color="yellow" variant="light" icon={<IconAlertCircle />}>
            <Text size="sm" fw={600}>
              You are about to send this email to {recipientCount} recipients. This action
              cannot be undone.
            </Text>
          </Alert>

          {/* Send Details */}
          <Paper p="sm" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
            <Stack gap="xs">
              <Group gap="xs">
                <Text size="sm" fw={600} c="burgundy">
                  Segment:
                </Text>
                <Text size="sm">{currentSegment?.description || selectedSegment}</Text>
              </Group>

              <Group gap="xs">
                <Text size="sm" fw={600} c="burgundy">
                  Recipients:
                </Text>
                <Text size="sm">{recipientCount} users</Text>
              </Group>

              <Box>
                <Text size="sm" fw={600} c="burgundy" mb={4}>
                  Subject:
                </Text>
                <Text size="sm" c="dimmed">
                  {subject}
                </Text>
              </Box>
            </Stack>
          </Paper>

          {/* Action Buttons */}
          <Group justify="flex-end" gap="sm" mt="md">
            <Button variant="light" onClick={() => setShowConfirmModal(false)}>
              Cancel
            </Button>

            <Button
              onClick={handleConfirmSend}
              color="burgundy"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                },
              }}
            >
              Send Now
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
};
