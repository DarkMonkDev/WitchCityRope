import React, { useState, useMemo, useCallback } from 'react';
import {
  Stack,
  Group,
  SimpleGrid,
  Card,
  Text,
  TextInput,
  Button,
  Paper,
  Alert,
  Box,
  Loader,
  Tabs,
  Title,
  Modal,
  MultiSelect,
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';
import {
  emailTemplatesApi,
  type EventEmailTemplateDto,
  type GlobalEmailTemplateDto,
} from '../../services/emailTemplates.api';
import { notifications } from '@mantine/notifications';
import { SendTestEmail } from './SendTestEmail';
import { EnhancedTemplateCard, type EnhancedTemplateCardProps } from './EnhancedTemplateCard';
import type { EventRecipientGroup } from './EnhancedTemplateCard';
import { TriggerConfigPanel } from './TriggerConfigPanel';
import type { TriggerConfig } from './TriggerConfigModal';
import { useTemplateVariableValidation } from '../../hooks/useTemplateVariableValidation';
import { htmlToPlainText } from '../../utils/htmlToPlainText';
import type { components } from '@witchcityrope/shared-types';

/**
 * SessionDto type from auto-generated backend types.
 * Used to build the Target Sessions multiselect dynamically from actual event sessions.
 */
type SessionDto = components['schemas']['SessionDto'];

/** Reuse the card's template prop type to avoid unsafe `as` casts */
type TemplateCardData = EnhancedTemplateCardProps['template'];

interface EventEmailTemplatePanelProps {
  /** The event ID to fetch/save event-specific template overrides */
  eventId: string;
  /** Sessions from the event form, used to build Target Sessions multiselect options */
  sessions: SessionDto[];
}

/**
 * EventEmailTemplatePanel - Event-specific email template management.
 *
 * Mirrors the EmailCategoryPanel pattern (click-to-select cards, 3-tab editor,
 * variable registry, test email) but uses event-specific APIs and preserves
 * event-only features like Reset to Default, Target Sessions, and Ad-Hoc email.
 *
 * Extracted from EventForm.tsx to reduce its size and align the event email
 * templates UI with the global email templates UI.
 */
export const EventEmailTemplatePanel: React.FC<EventEmailTemplatePanelProps> = ({
  eventId,
  sessions,
}) => {
  const queryClient = useQueryClient();

  // ---------------------------------------------------------------
  // State: selected template & editor
  // ---------------------------------------------------------------
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | null>(null);
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
  const [targetSessions, setTargetSessions] = useState<string[]>(['all']);

  // State for the 3-tab editor (Email | Trigger | Test Email)
  const [activeEditorTab, setActiveEditorTab] = useState<string>('email');

  // State for ad-hoc card selection (separate from template selection)
  const [isAdHocSelected, setIsAdHocSelected] = useState(false);

  // State for reset-to-default confirmation modal
  const [resetModalOpen, setResetModalOpen] = useState(false);
  const [templateToReset, setTemplateToReset] = useState<EventEmailTemplateDto | null>(null);

  // ---------------------------------------------------------------
  // Data fetching: event templates via react-query
  // ---------------------------------------------------------------
  const {
    data: templates,
    isLoading,
    error,
  } = useQuery<EventEmailTemplateDto[]>({
    queryKey: ['email-templates', 'event', eventId],
    queryFn: () => emailTemplatesApi.getEventTemplates(eventId),
    enabled: !!eventId,
  });

  // Derive selectedTemplate from templates array + selectedTemplateId.
  // This avoids the stale-object-reference bug where a react-query refetch
  // creates new objects and a useEffect on the object wipes unsaved changes.
  const selectedTemplate = useMemo(
    () => templates?.find((t) => t.id === selectedTemplateId) ?? null,
    [templates, selectedTemplateId]
  );

  // ---------------------------------------------------------------
  // Variable validation via shared hook (DRY — also used by EmailCategoryPanel)
  // ---------------------------------------------------------------
  const { availableVariables, invalidVariables } = useTemplateVariableValidation(
    'Events',
    selectedTemplate?.templateType ?? undefined,
    subject,
    htmlBody
  );

  // ---------------------------------------------------------------
  // Sort: enabled templates first (memoized to avoid re-sorting on every render)
  // ---------------------------------------------------------------
  const sortedTemplates = useMemo(
    () =>
      [...(templates || [])].sort((a, b) => {
        if (a.sendingEnabled !== b.sendingEnabled) {
          return a.sendingEnabled ? -1 : 1;
        }
        return 0;
      }),
    [templates]
  );

  // ---------------------------------------------------------------
  // Mutations
  // ---------------------------------------------------------------

  /** Save event template content (subject, htmlBody, targetSessions) */
  const saveTemplateMutation = useMutation<EventEmailTemplateDto, Error, void>({
    mutationFn: async () => {
      if (!selectedTemplate?.templateType) {
        throw new Error('No template selected');
      }
      return emailTemplatesApi.updateEventTemplate(eventId, selectedTemplate.templateType, {
        subject,
        htmlBody,
        plainTextBody: htmlToPlainText(htmlBody),
        targetSessions,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'event', eventId] });
      notifications.show({
        message: 'Template saved successfully',
        color: 'green',
      });
      setHasUnsavedChanges(false);
    },
    onError: (err: Error) => {
      notifications.show({
        message: err.message || 'Failed to save template',
        color: 'red',
      });
    },
  });

  /** Save trigger configuration for event template */
  const saveTriggerMutation = useMutation({
    mutationFn: async (config: TriggerConfig) => {
      if (!selectedTemplate?.templateType) {
        throw new Error('No template selected');
      }
      // Trigger saves go through updateEventTemplate with override fields.
      // We pass through the current content so the backend copy-on-edit pattern
      // creates the override with both content + trigger config.
      return emailTemplatesApi.updateEventTemplate(eventId, selectedTemplate.templateType, {
        subject: selectedTemplate.subject || '',
        htmlBody: selectedTemplate.htmlBody || '',
        plainTextBody: selectedTemplate.plainTextBody || '',
        overrideSendingEnabled: config.sendingEnabled,
        overrideTimingOffsetDays: config.timingOffsetDays,
        overrideRecipientGroup: config.recipientGroup || null,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'event', eventId] });
      notifications.show({
        message: 'Trigger configuration saved successfully',
        color: 'green',
      });
    },
    onError: (err: Error) => {
      notifications.show({
        message: err.message || 'Failed to save trigger configuration',
        color: 'red',
      });
    },
  });

  /**
   * Toggle sending enabled/disabled for an event template.
   * Uses updateEventTemplate with overrideSendingEnabled since there is
   * no dedicated toggle endpoint for event-level templates.
   * NOTE: This sends full content as a side effect — a dedicated PATCH
   * endpoint would be architecturally cleaner (tracked as tech debt).
   */
  const toggleSendingMutation = useMutation({
    mutationFn: async ({ template, enabled }: { template: EventEmailTemplateDto; enabled: boolean }) => {
      if (!template.templateType) {
        throw new Error('Template type is required');
      }
      return emailTemplatesApi.updateEventTemplate(eventId, template.templateType, {
        subject: template.subject || '',
        htmlBody: template.htmlBody || '',
        plainTextBody: template.plainTextBody || '',
        overrideSendingEnabled: enabled,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'event', eventId] });
    },
    onError: (err: Error) => {
      notifications.show({
        message: err.message || 'Failed to toggle sending enabled',
        color: 'red',
      });
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'event', eventId] });
    },
  });

  /** Reset event template to global default (delete the override) */
  const resetTemplateMutation = useMutation({
    mutationFn: async (templateType: string) => {
      await emailTemplatesApi.deleteEventTemplate(eventId, templateType);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'event', eventId] });
      notifications.show({
        message: 'Template reset to default',
        color: 'green',
      });
      setResetModalOpen(false);
      setTemplateToReset(null);
      // Deselect the template since it was reset
      setSelectedTemplateId(null);
      setActiveEditorTab('email');
    },
    onError: () => {
      notifications.show({
        message: 'Failed to reset template to default',
        color: 'red',
      });
    },
  });

  // ---------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------

  /** Handle clicking a template card — select it and populate the editor */
  const handleSelectTemplate = useCallback((template: EventEmailTemplateDto) => {
    setSelectedTemplateId(template.id ?? null);
    setSubject(template.subject ?? '');
    setHtmlBody(template.htmlBody ?? '');
    setTargetSessions(template.targetSessions ?? ['all']);
    setHasUnsavedChanges(false);
    setIsAdHocSelected(false);
    setActiveEditorTab('email');
  }, []);

  /** Handle clicking the Ad-Hoc card */
  const handleSelectAdHoc = useCallback(() => {
    setIsAdHocSelected(true);
    setSelectedTemplateId(null);
    setSubject('');
    setHtmlBody('');
    setTargetSessions(['all']);
    setHasUnsavedChanges(false);
  }, []);

  /** Close the editor panel */
  const handleCloseEditor = useCallback(() => {
    setSelectedTemplateId(null);
    setIsAdHocSelected(false);
    setSubject('');
    setHtmlBody('');
    setTargetSessions(['all']);
    setHasUnsavedChanges(false);
    setActiveEditorTab('email');
  }, []);

  /** Save template (sync version for button click) */
  const handleSave = () => {
    saveTemplateMutation.mutate();
  };

  /** Save template (async version for SendTestEmail integration) */
  const handleSaveAsync = async (): Promise<void> => {
    await saveTemplateMutation.mutateAsync();
  };

  // ---------------------------------------------------------------
  // Type mapping: EventEmailTemplateDto → card/trigger/test shapes
  // ---------------------------------------------------------------

  /**
   * Map EventEmailTemplateDto to the shape shared by EnhancedTemplateCard and TriggerConfigPanel.
   * Both components expect GlobalEmailTemplateDto & trigger extension fields.
   * The optional `showCustomSuffix` flag appends " - CUSTOM" to the title for cards.
   */
  const mapToGlobalShape = useCallback(
    (template: EventEmailTemplateDto, showCustomSuffix = false): TemplateCardData => ({
      id: template.id,
      title:
        showCustomSuffix && template.isCustomized
          ? `${template.templateType} - CUSTOM`
          : template.templateType,
      subject: template.subject,
      htmlBody: template.htmlBody,
      plainTextBody: template.plainTextBody,
      templateType: template.templateType,
      category: 'Events',
      triggerType: (template.triggerType as 'FixedEvent' | 'TimeBased' | 'Manual') || undefined,
      sendingEnabled: template.sendingEnabled ?? false,
      timingOffsetDays: template.timingOffsetDays ?? undefined,
      recipientGroup: (template.recipientGroup as EventRecipientGroup) || undefined,
    }),
    []
  );

  /**
   * Map EventEmailTemplateDto to GlobalEmailTemplateDto for SendTestEmail.
   * SendTestEmail needs category + templateType to fetch variables from the code registry.
   *
   * NOTE: Uses globalTemplateId so the send-test API resolves the correct global template.
   * For customized templates, the test email will use global template content.
   * A future backend endpoint could support event-specific test emails.
   */
  const mapForTestEmail = useCallback(
    (template: EventEmailTemplateDto): GlobalEmailTemplateDto =>
      ({
        id: template.globalTemplateId || template.id,
        category: 'Events',
        templateType: template.templateType,
        title: template.templateType,
        subject: template.subject,
        htmlBody: template.htmlBody,
        plainTextBody: template.plainTextBody,
      }) as GlobalEmailTemplateDto,
    []
  );

  // ---------------------------------------------------------------
  // Build target sessions options dynamically from event sessions
  // ---------------------------------------------------------------
  const targetSessionOptions = useMemo(
    () => [
      { value: 'all', label: 'All Sessions' },
      ...sessions.map((s) => ({
        value: s.id || s.sessionIdentifier || '',
        label: s.name || s.sessionIdentifier || 'Unnamed Session',
      })),
    ],
    [sessions]
  );

  // ---------------------------------------------------------------
  // Render: Loading / Error / Empty states
  // ---------------------------------------------------------------
  if (isLoading) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Loader size="lg" />
        <Text mt="md" c="dimmed">
          Loading email templates...
        </Text>
      </Box>
    );
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Templates">
        <Text>{(error as Error).message || 'Failed to load templates'}</Text>
      </Alert>
    );
  }

  return (
    <Stack gap="xl">
      {/* Section Title */}
      <Title
        order={2}
        c="burgundy"
        mb="md"
        style={{
          borderBottom: '2px solid var(--mantine-color-burgundy-3)',
          paddingBottom: '8px',
        }}
      >
        Email Templates
      </Title>

      <Text size="sm" c="dimmed" mb="lg">
        Click a template card to edit its content, configure triggers, or send a test email.
        Full ad-hoc email sending is available on the global Email Templates page.
      </Text>

      {/* Template Cards Grid */}
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
        {/* Ad-Hoc Email Card — always present */}
        <Card
          withBorder
          p="md"
          style={{
            cursor: 'pointer',
            borderColor: isAdHocSelected
              ? 'var(--mantine-color-burgundy-6)'
              : 'rgba(136, 1, 36, 0.1)',
            backgroundColor: isAdHocSelected ? 'rgba(136, 1, 36, 0.05)' : 'white',
            transition: 'all 0.3s ease',
            borderRadius: '12px',
          }}
          onClick={handleSelectAdHoc}
        >
          <Stack gap={4}>
            <Text fw={600} c="burgundy">
              Send Ad-Hoc Email
            </Text>
            <Text size="sm" c="dimmed" mb="xs">
              Send one-time messages to specific groups
            </Text>
            <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
              Available on the global Email Templates page
            </Text>
          </Stack>
        </Card>

        {/* Dynamic Template Cards — click-to-select pattern */}
        {sortedTemplates.map((template) => (
          <EnhancedTemplateCard
            key={template.id}
            template={mapToGlobalShape(template, true)}
            isSelected={selectedTemplateId === template.id}
            onClick={() => handleSelectTemplate(template)}
            onToggleSending={(_, enabled) => {
              toggleSendingMutation.mutate({ template, enabled });
            }}
          />
        ))}
      </SimpleGrid>

      {/* Ad-Hoc Info Panel — directs users to global Email Templates page */}
      {isAdHocSelected && (
        <Paper
          shadow="sm"
          radius="md"
          px="xl"
          pb="xl"
          mt={0}
          style={{ border: '1px solid rgba(136, 1, 36, 0.1)', paddingTop: '12px' }}
        >
          <Group justify="space-between" mb="md" wrap="nowrap">
            <Text fw={600} c="burgundy" size="lg">
              Ad-Hoc Email
            </Text>
            <Button variant="subtle" color="dimmed" size="compact-sm" onClick={handleCloseEditor}>
              Close
            </Button>
          </Group>

          <Alert icon={<IconAlertCircle />} color="blue" variant="light">
            <Text size="sm">
              Full ad-hoc email sending with recipient segments, scheduling, and saved templates
              is available on the global <strong>Email Templates</strong> page under the{' '}
              <strong>Ad Hoc</strong> tab. Event-specific ad-hoc sending will be available in a
              future update.
            </Text>
          </Alert>
        </Paper>
      )}

      {/* Template Editor Panel — 3-tab interface (Email | Trigger | Test Email) */}
      {selectedTemplate && (
        <Paper
          shadow="sm"
          radius="md"
          px="xl"
          pb="xl"
          mt={0}
          style={{ border: '1px solid rgba(136, 1, 36, 0.1)', paddingTop: '12px' }}
        >
          <Tabs
            value={activeEditorTab}
            onChange={(val) => setActiveEditorTab(val || 'email')}
            variant="pills"
            radius="md"
            color="burgundy"
            styles={{
              tab: {
                fontWeight: 600,
                transition: 'all 0.2s ease',
                '&[data-active]': {
                  backgroundColor: '#880124',
                  color: 'white',
                },
                '&:hover:not([data-active])': {
                  backgroundColor: 'rgba(136, 1, 36, 0.05)',
                },
              },
            }}
          >
            {/* Header row: template name (left) + tabs (center) + close (right) */}
            <Group justify="space-between" mb="md" wrap="nowrap">
              <Group gap="sm" style={{ flex: 1, minWidth: 0 }}>
                <Text fw={600} c="burgundy" size="lg" style={{ whiteSpace: 'nowrap' }}>
                  {selectedTemplate.isCustomized
                    ? `${selectedTemplate.templateType} - CUSTOM`
                    : selectedTemplate.templateType}
                </Text>
              </Group>

              <Tabs.List style={{ flex: 0, flexWrap: 'nowrap' }}>
                <Tabs.Tab value="email">Email</Tabs.Tab>
                <Tabs.Tab value="trigger">Trigger</Tabs.Tab>
                <Tabs.Tab value="test">Test Email</Tabs.Tab>
              </Tabs.List>

              <Group justify="flex-end" style={{ flex: 1, minWidth: 0 }}>
                <Button variant="subtle" color="dimmed" size="compact-sm" onClick={handleCloseEditor}>
                  Close
                </Button>
              </Group>
            </Group>

            {/* ===== Email Tab ===== */}
            <Tabs.Panel value="email" pt="lg">
              <Stack gap="md">
                <TextInput
                  label="Subject Line"
                  value={subject}
                  onChange={(e) => {
                    setSubject(e.currentTarget.value);
                    setHasUnsavedChanges(true);
                  }}
                  required
                  maxLength={200}
                />

                {/* Target Sessions multiselect — dynamic from event sessions */}
                <MultiSelect
                  label="Target Sessions"
                  description="Which sessions should trigger this email? Hold Ctrl/Cmd for multiple selections."
                  data={targetSessionOptions}
                  value={targetSessions}
                  onChange={(val) => {
                    setTargetSessions(val);
                    setHasUnsavedChanges(true);
                  }}
                />

                {/* Available Variables box (fetched from code registry via shared hook) */}
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
                  <Text size="xs" c="dimmed" mb="xs">
                    {availableVariables.length > 0
                      ? availableVariables.join(', ')
                      : 'No dynamic variables for this template'}
                  </Text>
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                    Note: Contact emails (support@witchcityrope.com, info@witchcityrope.com,
                    events@witchcityrope.com) and system URL are hardcoded in templates.
                  </Text>
                </Box>

                <div>
                  <Text size="sm" fw={500} mb={4}>
                    Email Content (HTML)
                  </Text>
                  <MantineTiptapEditor
                    value={htmlBody}
                    onChange={(val) => {
                      setHtmlBody(val);
                      setHasUnsavedChanges(true);
                    }}
                    placeholder="Enter email template content..."
                    minRows={12}
                  />
                </div>

                {/* Variable validation warning */}
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
                      The email may not render correctly when sent. Available variables:{' '}
                      {availableVariables.join(', ')}
                    </Text>
                  </Alert>
                )}

                {/* Action buttons: Reset to Default (left) + Cancel/Save (right) */}
                <Group justify="space-between" gap="sm">
                  <div>
                    {/* Reset to Default — only for customized event templates */}
                    {selectedTemplate.isCustomized && (
                      <Button
                        variant="light"
                        color="red"
                        onClick={() => {
                          setTemplateToReset(selectedTemplate);
                          setResetModalOpen(true);
                        }}
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
                        Reset to Default
                      </Button>
                    )}
                  </div>
                  <Group gap="sm">
                    <Button variant="light" onClick={handleCloseEditor}>
                      Cancel
                    </Button>
                    <Button
                      onClick={handleSave}
                      loading={saveTemplateMutation.isPending}
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
                </Group>
              </Stack>
            </Tabs.Panel>

            {/* ===== Trigger Tab ===== */}
            <Tabs.Panel value="trigger" pt="lg">
              <TriggerConfigPanel
                template={mapToGlobalShape(selectedTemplate)}
                onSave={async (config) => {
                  await saveTriggerMutation.mutateAsync(config);
                }}
              />
            </Tabs.Panel>

            {/* ===== Test Email Tab ===== */}
            <Tabs.Panel value="test" pt="lg">
              <SendTestEmail
                template={mapForTestEmail(selectedTemplate)}
                currentTitle={selectedTemplate.templateType ?? ''}
                currentSubject={subject}
                currentHtmlBody={htmlBody}
                onSaveTemplate={handleSaveAsync}
                hasUnsavedChanges={hasUnsavedChanges}
              />
            </Tabs.Panel>
          </Tabs>
        </Paper>
      )}

      {/* Reset Template Confirmation Modal */}
      <Modal
        opened={resetModalOpen}
        onClose={() => {
          setResetModalOpen(false);
          setTemplateToReset(null);
        }}
        title={<Title order={3}>Reset Template to Default?</Title>}
        centered
      >
        <Text mb="md">
          Are you sure you want to reset <strong>{templateToReset?.templateType}</strong> to
          the global default template? This will delete your customizations and cannot be
          undone.
        </Text>

        <Group justify="flex-end" mt="lg">
          <Button
            variant="default"
            onClick={() => {
              setResetModalOpen(false);
              setTemplateToReset(null);
            }}
          >
            Cancel
          </Button>
          <Button
            color="red"
            onClick={() => {
              if (templateToReset?.templateType) {
                resetTemplateMutation.mutate(templateToReset.templateType);
              }
            }}
            loading={resetTemplateMutation.isPending}
          >
            Reset to Default
          </Button>
        </Group>
      </Modal>
    </Stack>
  );
};
