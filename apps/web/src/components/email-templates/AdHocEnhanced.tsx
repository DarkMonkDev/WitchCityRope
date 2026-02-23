import React, { useState } from 'react';
import { Stack, Group, Box, Paper, Title, Text, Radio, Button, Card, Modal, TextInput } from '@mantine/core';
import { DateTimePicker } from '@mantine/dates';
import { IconCalendar, IconTrash, IconDeviceFloppy } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import {
  emailTemplatesApi,
  type AdHocEmailTemplateDto,
} from '../../services/emailTemplates.api';

interface SaveTemplateModalProps {
  opened: boolean;
  onClose: () => void;
  emailData: {
    subject: string;
    htmlBody: string;
    plainTextBody: string;
  };
  onSave: (templateName: string) => Promise<void>;
}

/**
 * Save as Template Modal
 * Allows saving an ad hoc email as a reusable template
 */
const SaveTemplateModal: React.FC<SaveTemplateModalProps> = ({
  opened,
  onClose,
  emailData,
  onSave,
}) => {
  const [templateName, setTemplateName] = useState(emailData.subject);
  const [isSaving, setIsSaving] = useState(false);

  const handleSave = async () => {
    if (!templateName.trim()) {
      notifications.show({
        message: 'Template name is required',
        color: 'red',
      });
      return;
    }

    setIsSaving(true);
    try {
      await onSave(templateName);
      onClose();
      setTemplateName('');
    } catch (error) {
      // Error already handled in parent
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Save as Template"
      size="md"
      centered
      styles={{
        title: {
          fontFamily: "'Montserrat', sans-serif",
          fontWeight: 700,
          fontSize: '20px',
          color: '#880124',
        },
      }}
    >
      <Stack gap="md">
        <TextInput
          label="Template Name"
          value={templateName}
          onChange={(e) => setTemplateName(e.currentTarget.value)}
          placeholder="Enter template name..."
          required
          maxLength={100}
        />

        <Box
          p="sm"
          style={{
            background: 'rgba(136, 1, 36, 0.05)',
            borderRadius: '6px',
            border: '1px solid rgba(136, 1, 36, 0.1)',
          }}
        >
          <Text size="xs" c="dimmed" mb="xs">
            This template will be saved to the Ad Hoc templates section and can be reused for future sends.
          </Text>

          <Text size="xs" fw={600} c="burgundy" mb={4}>
            Template Preview:
          </Text>
          <Text size="xs" c="charcoal">
            Subject: {emailData.subject}
          </Text>
        </Box>

        <Group justify="flex-end" gap="sm">
          <Button variant="light" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>

          <Button
            onClick={handleSave}
            loading={isSaving}
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2',
              },
            }}
          >
            Save Template
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

interface DeleteTemplateModalProps {
  opened: boolean;
  onClose: () => void;
  templateName: string;
  onConfirm: () => Promise<void>;
}

/**
 * Delete Template Confirmation Modal
 */
const DeleteTemplateModal: React.FC<DeleteTemplateModalProps> = ({
  opened,
  onClose,
  templateName,
  onConfirm,
}) => {
  const [isDeleting, setIsDeleting] = useState(false);

  const handleConfirm = async () => {
    setIsDeleting(true);
    try {
      await onConfirm();
      onClose();
    } catch (error) {
      // Error handled in parent
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Delete Template?"
      size="md"
      centered
      styles={{
        title: {
          fontFamily: "'Montserrat', sans-serif",
          fontWeight: 700,
          fontSize: '20px',
          color: '#880124',
        },
      }}
    >
      <Stack gap="md">
        <Text size="sm">
          Are you sure you want to delete "{templateName}"?
        </Text>

        <Text size="sm" c="dimmed">
          This action cannot be undone.
        </Text>

        <Group justify="flex-end" gap="sm">
          <Button variant="light" onClick={onClose} disabled={isDeleting}>
            Cancel
          </Button>

          <Button
            onClick={handleConfirm}
            loading={isDeleting}
            color="red"
            styles={{
              root: {
                fontWeight: 600,
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2',
              },
            }}
          >
            Delete
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

interface SavedAdHocTemplatesProps {
  onUseTemplate: (template: AdHocEmailTemplateDto) => void;
}

/**
 * Saved Ad Hoc Templates Section
 * Displays saved templates with Use/Delete actions
 */
export const SavedAdHocTemplates: React.FC<SavedAdHocTemplatesProps> = ({
  onUseTemplate,
}) => {
  const queryClient = useQueryClient();
  const [deleteModalOpened, setDeleteModalOpened] = useState(false);
  const [templateToDelete, setTemplateToDelete] = useState<AdHocEmailTemplateDto | null>(null);

  // Fetch saved templates
  const {
    data: savedTemplates,
  } = useQuery<AdHocEmailTemplateDto[]>({
    queryKey: ['adhoc-templates'],
    queryFn: () => emailTemplatesApi.getAdHocTemplates(),
  });

  // Delete template mutation
  const deleteMutation = useMutation({
    mutationFn: (templateId: string) => emailTemplatesApi.deleteAdHocTemplate(templateId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adhoc-templates'] });
      notifications.show({
        message: 'Template deleted successfully',
        color: 'green',
      });
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to delete template',
        color: 'red',
      });
    },
  });

  const handleDeleteClick = (template: AdHocEmailTemplateDto) => {
    setTemplateToDelete(template);
    setDeleteModalOpened(true);
  };

  const handleConfirmDelete = async () => {
    if (templateToDelete) {
      await deleteMutation.mutateAsync(templateToDelete.id);
    }
  };

  // Don't render if no templates
  if (!savedTemplates || savedTemplates.length === 0) {
    return null;
  }

  return (
    <>
      <Box>
        <Title
          order={3}
          mb="md"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '18px',
            fontWeight: 700,
            color: '#880124',
          }}
        >
          Saved Ad Hoc Templates
        </Title>

        <Stack gap="md">
          {savedTemplates.map((template) => (
            <Card
              key={template.id}
              withBorder
              p="md"
              style={{
                borderColor: 'rgba(136, 1, 36, 0.1)',
                borderRadius: '8px',
              }}
            >
              <Group justify="space-between" wrap="nowrap">
                <Box style={{ flex: 1 }}>
                  <Text fw={600} c="burgundy" mb={4}>
                    📧 {template.templateName}
                  </Text>
                  <Text size="sm" c="charcoal">
                    Subject: {template.subject}
                  </Text>
                  <Text size="xs" c="dimmed" mt={4}>
                    Saved: {new Date(template.createdAt).toLocaleDateString()}
                  </Text>
                </Box>

                <Group gap="xs" wrap="nowrap">
                  <Button
                    variant="light"
                    color="blue"
                    size="sm"
                    onClick={() => onUseTemplate(template)}
                    styles={{
                      root: {
                        height: '36px',
                        paddingTop: '8px',
                        paddingBottom: '8px',
                        fontSize: '13px',
                        lineHeight: '1.2',
                        fontWeight: 600,
                      },
                    }}
                  >
                    Use
                  </Button>

                  <Button
                    variant="subtle"
                    color="red"
                    size="sm"
                    onClick={() => handleDeleteClick(template)}
                    leftSection={<IconTrash size={14} />}
                    styles={{
                      root: {
                        height: '36px',
                        paddingTop: '8px',
                        paddingBottom: '8px',
                        fontSize: '13px',
                        lineHeight: '1.2',
                        fontWeight: 600,
                      },
                    }}
                  >
                    Delete
                  </Button>
                </Group>
              </Group>
            </Card>
          ))}
        </Stack>
      </Box>

      {/* Delete Confirmation Modal */}
      {templateToDelete && (
        <DeleteTemplateModal
          opened={deleteModalOpened}
          onClose={() => setDeleteModalOpened(false)}
          templateName={templateToDelete.templateName}
          onConfirm={handleConfirmDelete}
        />
      )}
    </>
  );
};

interface ScheduledSendSectionProps {
  sendTiming: 'immediate' | 'scheduled';
  scheduledDate: Date | null;
  onSendTimingChange: (value: 'immediate' | 'scheduled') => void;
  onScheduledDateChange: (date: Date | null) => void;
}

/**
 * Scheduled Send Section
 * Radio group + DateTimePicker for scheduled ad hoc emails
 */
export const ScheduledSendSection: React.FC<ScheduledSendSectionProps> = ({
  sendTiming,
  scheduledDate,
  onSendTimingChange,
  onScheduledDateChange,
}) => {
  return (
    <Paper p="md" withBorder style={{ borderColor: 'rgba(136, 1, 36, 0.1)' }}>
      <Stack gap="md">
        <Text size="sm" fw={600} c="burgundy">
          SEND TIMING
        </Text>

        <Radio.Group
          value={sendTiming}
          onChange={(value) => onSendTimingChange(value as 'immediate' | 'scheduled')}
        >
          <Stack gap="sm">
            <Radio value="immediate" label="Send immediately" />
            <Radio value="scheduled" label="Schedule for later" />
          </Stack>
        </Radio.Group>

        {/* DateTimePicker - only shown when scheduled is selected */}
        {sendTiming === 'scheduled' && (
          <DateTimePicker
            label="Send on"
            placeholder="Pick date and time"
            value={scheduledDate}
            onChange={onScheduledDateChange}
            minDate={new Date()} // Prevent past dates
            valueFormat="MMM DD, YYYY hh:mm A"
            clearable
            required
            leftSection={<IconCalendar size={18} style={{ color: '#B8956A' }} />}
          />
        )}
      </Stack>
    </Paper>
  );
};

interface SaveAsTemplateButtonProps {
  subject: string;
  htmlBody: string;
  onSave: (templateName: string) => Promise<void>;
}

/**
 * Save as Template Button + Modal
 * Wraps the save functionality with a button trigger
 */
export const SaveAsTemplateButton: React.FC<SaveAsTemplateButtonProps> = ({
  subject,
  htmlBody,
  onSave,
}) => {
  const [modalOpened, setModalOpened] = useState(false);

  // Validation
  const canSave = subject.trim().length > 0 && htmlBody.trim().length > 0;

  // Generate plain text from HTML
  const plainTextBody = htmlBody
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/p>/gi, '\n\n')
    .replace(/<[^>]+>/g, '')
    .replace(/&nbsp;/g, ' ')
    .trim();

  return (
    <>
      <Button
        variant="light"
        color="burgundy"
        onClick={() => setModalOpened(true)}
        disabled={!canSave}
        leftSection={<IconDeviceFloppy size={16} />}
        styles={{
          root: {
            fontWeight: 600,
            height: '44px',
            paddingTop: '12px',
            paddingBottom: '12px',
            fontSize: '14px',
            lineHeight: '1.2',
          },
        }}
      >
        Save as Template
      </Button>

      <SaveTemplateModal
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        emailData={{ subject, htmlBody, plainTextBody }}
        onSave={onSave}
      />
    </>
  );
};
