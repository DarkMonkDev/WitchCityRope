import React, { useState, useEffect, useMemo } from 'react';
import {
  Stack,
  Group,
  Box,
  Paper,
  Title,
  Text,
  Select,
  TextInput,
  Textarea,
  Button,
  Modal,
  Alert,
  Checkbox,
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
  type AdHocEmailTemplateDto,
} from '../../services/emailTemplates.api';
import {
  SavedAdHocTemplates,
  ScheduledSendSection,
  SaveAsTemplateButton,
} from './AdHocEnhanced';

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
 * Helper to parse and validate email addresses from text input
 */
const parseEmails = (input: string): { valid: string[]; invalid: string[] } => {
  // Split by comma, semicolon, newline, or space
  const emails = input
    .split(/[,;\n\s]+/)
    .map((e) => e.trim().toLowerCase())
    .filter((e) => e.length > 0);

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  const valid: string[] = [];
  const invalid: string[] = [];

  emails.forEach((email) => {
    if (emailRegex.test(email)) {
      if (!valid.includes(email)) {
        valid.push(email);
      }
    } else {
      invalid.push(email);
    }
  });

  return { valid, invalid };
};

/**
 * SendAdHocEmail Component
 *
 * Allows admins to compose and send custom emails to specific user segments
 * or directly to specific email addresses.
 *
 * Flow:
 * 1. Select a saved template (optional) - loads into editor
 * 2. Compose/edit email content (subject + body)
 * 3. Select recipients (segment or direct email input)
 * 4. Preview and send
 */
export const SendAdHocEmail: React.FC = () => {
  const queryClient = useQueryClient();

  // Email content state
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');

  // Recipient mode state
  const [useDirectEmails, setUseDirectEmails] = useState(false);
  const [selectedSegment, setSelectedSegment] = useState<UserSegment | null>(null);
  const [directEmailInput, setDirectEmailInput] = useState('');

  // Validation state
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

  // Modal state
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  // Scheduled send state
  const [sendTiming, setSendTiming] = useState<'immediate' | 'scheduled'>('immediate');
  const [scheduledDate, setScheduledDate] = useState<Date | null>(null);

  // Parse direct emails
  const parsedEmails = useMemo(() => parseEmails(directEmailInput), [directEmailInput]);

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
    enabled: selectedSegment !== null && !useDirectEmails,
  });

  // Send email mutation
  const sendMutation = useMutation({
    mutationFn: (data: {
      subject: string;
      htmlBody: string;
      segment?: UserSegment;
      recipientEmails?: string[];
      scheduledSendAt?: Date;
    }) => {
      // Generate plain text from HTML
      const plainTextBody = data.htmlBody
        .replace(/<br\s*\/?>/gi, '\n')
        .replace(/<\/p>/gi, '\n\n')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/g, ' ')
        .trim();

      // If scheduled, use scheduleAdHocEmail endpoint
      if (data.scheduledSendAt) {
        return emailTemplatesApi.scheduleAdHocEmail({
          subject: data.subject,
          htmlBody: data.htmlBody,
          plainTextBody,
          segment: data.segment || null,
          recipientEmails: data.recipientEmails || null,
          recipientGroup: data.segment || null,
          scheduledSendAt: data.scheduledSendAt.toISOString(),
        });
      }

      // Otherwise use immediate send
      return emailTemplatesApi.sendAdHocEmail({
        subject: data.subject,
        htmlBody: data.htmlBody,
        plainTextBody,
        segment: data.segment || null,
        recipientEmails: data.recipientEmails || null,
        recipientGroup: data.segment || null,
      });
    },
    onSuccess: () => {
      const recipientCount = useDirectEmails
        ? parsedEmails.valid.length
        : segments?.find((s) => s.segment === selectedSegment)?.count || 0;

      const message = sendTiming === 'scheduled'
        ? `Email scheduled for ${recipientCount} recipient${recipientCount !== 1 ? 's' : ''}`
        : `Email sent to ${recipientCount} recipient${recipientCount !== 1 ? 's' : ''} successfully`;

      notifications.show({
        message,
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

  // Save template mutation
  const saveTemplateMutation = useMutation({
    mutationFn: (templateName: string) => {
      const plainTextBody = htmlBody
        .replace(/<br\s*\/?>/gi, '\n')
        .replace(/<\/p>/gi, '\n\n')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/g, ' ')
        .trim();

      return emailTemplatesApi.saveAsTemplate({
        templateName,
        subject,
        htmlBody,
        plainTextBody,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adhoc-templates'] });
      notifications.show({
        message: 'Template saved successfully',
        color: 'green',
      });
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save template',
        color: 'red',
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
    if (useDirectEmails) {
      if (parsedEmails.valid.length === 0) return;
      sendMutation.mutate({
        subject,
        htmlBody,
        recipientEmails: parsedEmails.valid,
        scheduledSendAt: sendTiming === 'scheduled' ? scheduledDate || undefined : undefined,
      });
    } else {
      if (!selectedSegment) return;
      sendMutation.mutate({
        subject,
        htmlBody,
        segment: selectedSegment,
        scheduledSendAt: sendTiming === 'scheduled' ? scheduledDate || undefined : undefined,
      });
    }
    setShowConfirmModal(false);
  };

  // Handler for using a saved template
  const handleUseTemplate = (template: AdHocEmailTemplateDto) => {
    setSubject(template.subject);
    setHtmlBody(template.htmlBody);
    notifications.show({
      message: 'Template loaded into editor',
      color: 'blue',
    });
  };

  const handleCancel = () => {
    if (subject || htmlBody || directEmailInput) {
      if (window.confirm('Discard unsaved email?')) {
        handleReset();
      }
    } else {
      handleReset();
    }
  };

  const handleReset = () => {
    setSubject('');
    setHtmlBody('');
    setUseDirectEmails(false);
    setSelectedSegment(null);
    setDirectEmailInput('');
    setInvalidVariables([]);
    setSendTiming('immediate');
    setScheduledDate(null);
  };

  // Clear recipient when switching modes
  const handleDirectEmailToggle = (checked: boolean) => {
    setUseDirectEmails(checked);
    if (checked) {
      setSelectedSegment(null);
    } else {
      setDirectEmailInput('');
    }
  };

  // Computed validation
  const hasValidRecipients = useDirectEmails
    ? parsedEmails.valid.length > 0
    : selectedSegment !== null;

  const isValid =
    hasValidRecipients &&
    subject.trim().length > 0 &&
    htmlBody.trim().length > 0 &&
    invalidVariables.length === 0 &&
    (sendTiming === 'immediate' || (sendTiming === 'scheduled' && scheduledDate !== null));

  // Get current segment data
  const currentSegment = segments?.find((s) => s.segment === selectedSegment);
  const recipientCount = useDirectEmails
    ? parsedEmails.valid.length
    : currentSegment?.count || 0;

  // Prepare segment options for Select dropdown
  const segmentOptions =
    segments?.map((segment) => ({
      value: segment.segment || '',
      label: `${segment.description || segment.segment} (${segment.count} users)`,
    })) || [];

  return (
    <Stack gap="xl">
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
          Send custom emails to specific user groups or individual recipients
        </Text>
      </Box>

      {/* 1. Saved Templates Section */}
      <SavedAdHocTemplates onUseTemplate={handleUseTemplate} />

      {/* 2. Email Content Editor */}
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

      {/* 3. Recipient Selection */}
      <Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
        <Stack gap="md">
          {/* Direct Email Toggle */}
          <Checkbox
            label="Input Recipient Emails Directly"
            checked={useDirectEmails}
            onChange={(e) => handleDirectEmailToggle(e.currentTarget.checked)}
            styles={{
              label: {
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                color: '#880124',
              },
            }}
          />

          {/* Segment Selector (when not using direct emails) */}
          {!useDirectEmails && (
            <>
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

              {!segmentsError && (
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
              )}

              {selectedSegment && (
                <Text size="lg" fw={600} c="burgundy">
                  Selected: {recipientCount} recipients
                </Text>
              )}
            </>
          )}

          {/* Direct Email Input (when using direct emails) */}
          {useDirectEmails && (
            <>
              <Textarea
                label="Recipient Emails"
                required
                value={directEmailInput}
                onChange={(e) => setDirectEmailInput(e.currentTarget.value)}
                placeholder="Enter email addresses (comma, semicolon, or newline separated)..."
                minRows={4}
                maxRows={8}
                autosize
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

              {/* Email validation feedback */}
              {directEmailInput.trim() && (
                <Box>
                  {parsedEmails.valid.length > 0 && (
                    <Text size="sm" c="green" fw={500}>
                      ✓ {parsedEmails.valid.length} valid email{parsedEmails.valid.length !== 1 ? 's' : ''} entered
                    </Text>
                  )}
                  {parsedEmails.invalid.length > 0 && (
                    <Text size="sm" c="red" mt={4}>
                      ✗ Invalid: {parsedEmails.invalid.join(', ')}
                    </Text>
                  )}
                </Box>
              )}
            </>
          )}
        </Stack>
      </Paper>

      {/* 4. Recipient Preview (for segment mode only) */}
      {!useDirectEmails && selectedSegment && (
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

      {/* 5. Scheduled Send Section */}
      <ScheduledSendSection
        sendTiming={sendTiming}
        scheduledDate={scheduledDate}
        onSendTimingChange={setSendTiming}
        onScheduledDateChange={setScheduledDate}
      />

      {/* 6. Action Buttons - Desktop */}
      <Group justify="flex-end" gap="sm" visibleFrom="sm">
        <Button variant="light" onClick={handleCancel} disabled={sendMutation.isPending}>
          Cancel
        </Button>

        <SaveAsTemplateButton
          subject={subject}
          htmlBody={htmlBody}
          onSave={async (name) => {
            await saveTemplateMutation.mutateAsync(name);
          }}
        />

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
          {sendTiming === 'immediate' ? 'Send Now' : 'Schedule Send'}
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
          {sendTiming === 'immediate' ? 'Send Now' : 'Schedule Send'}
        </Button>

        <SaveAsTemplateButton
          subject={subject}
          htmlBody={htmlBody}
          onSave={async (name) => {
            await saveTemplateMutation.mutateAsync(name);
          }}
        />

        <Button
          variant="light"
          onClick={handleCancel}
          disabled={sendMutation.isPending}
          fullWidth
        >
          Cancel
        </Button>
      </Stack>

      {/* 7. Confirmation Modal */}
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
              You are about to send this email to {recipientCount} recipient{recipientCount !== 1 ? 's' : ''}. This action
              cannot be undone.
            </Text>
          </Alert>

          {/* Send Details */}
          <Paper p="sm" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
            <Stack gap="xs">
              <Group gap="xs">
                <Text size="sm" fw={600} c="burgundy">
                  Recipients:
                </Text>
                <Text size="sm">
                  {useDirectEmails
                    ? `${recipientCount} email address${recipientCount !== 1 ? 'es' : ''}`
                    : `${currentSegment?.description || selectedSegment} (${recipientCount} users)`}
                </Text>
              </Group>

              {useDirectEmails && parsedEmails.valid.length <= 5 && (
                <Box>
                  <Text size="xs" c="dimmed">
                    {parsedEmails.valid.join(', ')}
                  </Text>
                </Box>
              )}

              <Box>
                <Text size="sm" fw={600} c="burgundy" mb={4}>
                  Subject:
                </Text>
                <Text size="sm" c="dimmed">
                  {subject}
                </Text>
              </Box>

              {sendTiming === 'scheduled' && scheduledDate && (
                <Group gap="xs">
                  <Text size="sm" fw={600} c="burgundy">
                    Scheduled for:
                  </Text>
                  <Text size="sm">{scheduledDate.toLocaleString()}</Text>
                </Group>
              )}
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
              {sendTiming === 'immediate' ? 'Send Now' : 'Schedule Send'}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
};
