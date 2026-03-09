import React, { useState, useEffect } from 'react';
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
  Switch,
  Badge,
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';
import { emailTemplatesApi, type GlobalEmailTemplateDto } from '../../services/emailTemplates.api';
import { notifications } from '@mantine/notifications';
import { SendAdHocEmail } from './SendAdHocEmail';
import { SendTestEmail } from './SendTestEmail';
import { EnhancedTemplateCard } from './EnhancedTemplateCard';
import { TriggerConfigModal, type TriggerConfig } from './TriggerConfigModal';
import { TriggerConfigPanel } from './TriggerConfigPanel';

interface EmailCategoryPanelProps {
  category: 'Vetting' | 'Events' | 'Admin' | 'Incident' | 'AdHoc';
}

/**
 * Reusable panel for displaying and editing email templates for a specific category
 * Based on EventForm Emails Tab pattern (lines 1228-1386)
 */
export const EmailCategoryPanel: React.FC<EmailCategoryPanelProps> = ({ category }) => {
  const queryClient = useQueryClient();

  // Local state for editor
  const [selectedTemplate, setSelectedTemplate] = useState<GlobalEmailTemplateDto | null>(null);
  const [title, setTitle] = useState('');
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');
  const [_plainTextBody, setPlainTextBody] = useState('');
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  // State for inline title editing
  const [isEditingTitle, setIsEditingTitle] = useState(false);
  const [editingTitle, setEditingTitle] = useState('');

  // State for Events tab editor tabs
  const [activeEditorTab, setActiveEditorTab] = useState<string>('email');

  // State for trigger config modal (non-Events categories that might need it in future)
  const [triggerModalOpened, setTriggerModalOpened] = useState(false);
  const [selectedTemplateForTrigger, _setSelectedTemplateForTrigger] = useState<GlobalEmailTemplateDto | null>(null);

  // Fetch global templates for this category
  const {
    data: templates,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['email-templates', 'global', category],
    queryFn: () => emailTemplatesApi.getGlobalTemplatesByCategory(category),
  });

  // Save template mutation
  const saveMutation = useMutation({
    mutationFn: (data: { title: string; subject: string; htmlBody: string; plainTextBody: string }) => {
      if (!selectedTemplate) {
        throw new Error('No template selected');
      }
      return emailTemplatesApi.updateGlobalTemplate(selectedTemplate.id ?? '', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
      notifications.show({
        message: 'Template saved successfully',
        color: 'green',
      });
      setSelectedTemplate(null);
      setTitle('');
      setSubject('');
      setHtmlBody('');
      setPlainTextBody('');
      setHasUnsavedChanges(false);
      setActiveEditorTab('email');
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save template',
        color: 'red',
      });
    },
  });

  // Save title-only mutation (doesn't deselect template)
  const saveTitleMutation = useMutation<unknown, Error, string>({
    mutationFn: (newTitle: string) => {
      if (!selectedTemplate) {
        throw new Error('No template selected');
      }
      return emailTemplatesApi.updateGlobalTemplate(selectedTemplate.id ?? '', {
        title: newTitle,
        subject: selectedTemplate.subject ?? '',
        htmlBody: selectedTemplate.htmlBody ?? '',
        plainTextBody: selectedTemplate.plainTextBody ?? '',
      });
    },
    onSuccess: (_data: unknown, newTitle: string) => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
      notifications.show({
        message: 'Title updated',
        color: 'green',
      });
      setTitle(newTitle);
      setIsEditingTitle(false);
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save title',
        color: 'red',
      });
    },
  });

  // Save trigger config mutation (Events tab only)
  const saveTriggerMutation = useMutation({
    mutationFn: (data: { id: string; config: TriggerConfig }) =>
      emailTemplatesApi.updateTriggerConfig(data.id, data.config),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
      notifications.show({
        message: 'Trigger configuration saved successfully',
        color: 'green',
      });
      setTriggerModalOpened(false);
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save trigger configuration',
        color: 'red',
      });
    },
  });

  // Toggle sending enabled mutation (all categories)
  // Provides unified enable/disable for email template sending.
  // Uses optimistic update pattern for responsive UX.
  const toggleSendingMutation = useMutation({
    mutationFn: (data: { id: string; enabled: boolean }) =>
      emailTemplatesApi.toggleSendingEnabled(data.id, data.enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to toggle sending enabled',
        color: 'red',
      });
      // Refetch to revert optimistic state
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
    },
  });

  // Sync editor state when template is selected
  useEffect(() => {
    if (selectedTemplate) {
      setTitle(selectedTemplate.title ?? '');
      setSubject(selectedTemplate.subject ?? '');
      setHtmlBody(selectedTemplate.htmlBody ?? '');
      setPlainTextBody(selectedTemplate.plainTextBody ?? '');
      setInvalidVariables([]);
      setHasUnsavedChanges(false);
      setIsEditingTitle(false);
    }
  }, [selectedTemplate]);

  // Real-time variable validation
  useEffect(() => {
    if (!selectedTemplate || !htmlBody) {
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
    const invalid = Array.from(extractedVars).filter(
      (v) => !selectedTemplate.variables?.includes(v)
    );
    setInvalidVariables(invalid);
  }, [subject, htmlBody, selectedTemplate]);

  // Convert plain text from HTML (simple strip tags approach)
  const generatePlainText = (html: string): string => {
    // Simple HTML to plain text conversion
    // TODO: Use a proper HTML-to-text library for production
    return html
      .replace(/<br\s*\/?>/gi, '\n')
      .replace(/<\/p>/gi, '\n\n')
      .replace(/<[^>]+>/g, '')
      .replace(/&nbsp;/g, ' ')
      .trim();
  };

  // Handle save
  const handleSave = () => {
    const plainText = generatePlainText(htmlBody);
    saveMutation.mutate({
      title,
      subject,
      htmlBody,
      plainTextBody: plainText,
    });
  };

  // Handle save (async version for SendTestEmail integration)
  const handleSaveAsync = async (): Promise<void> => {
    const plainText = generatePlainText(htmlBody);
    await saveMutation.mutateAsync({
      title,
      subject,
      htmlBody,
      plainTextBody: plainText,
    });
  };

  // Handle cancel
  const handleCancel = () => {
    setSelectedTemplate(null);
    setTitle('');
    setSubject('');
    setHtmlBody('');
    setPlainTextBody('');
    setInvalidVariables([]);
    setHasUnsavedChanges(false);
    setActiveEditorTab('email');
    setIsEditingTitle(false);
  };

  // Loading state
  if (isLoading) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Loader size="lg" />
        <Text mt="md" c="dimmed">
          Loading templates...
        </Text>
      </Box>
    );
  }

  // Error state
  if (error) {
    return (
      <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Templates">
        <Text>{(error as Error).message || 'Failed to load templates'}</Text>
      </Alert>
    );
  }

  // Empty state
  if (!templates || !Array.isArray(templates) || templates.length === 0) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Text c="dimmed" size="lg">
          No templates found for {category} category
        </Text>
      </Box>
    );
  }

  // For AdHoc category, skip the template cards and only render SendAdHocEmail
  if (category === 'AdHoc') {
    return <SendAdHocEmail />;
  }

  return (
    <Stack gap="xl">
      {/* Template Cards - Horizontal Scrollable Group */}
      <div>
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
          {templates.map((template) => (
            category === 'Events' ? (
              <EnhancedTemplateCard
                key={template.id}
                template={template}
                isSelected={selectedTemplate?.id === template.id}
                onClick={() => {
                  setSelectedTemplate(template);
                  setActiveEditorTab('email');
                }}
                onToggleSending={(id, enabled) => {
                  toggleSendingMutation.mutate({ id, enabled });
                }}
              />
            ) : (
              <Card
                key={template.id}
                withBorder
                p="md"
                style={{
                  cursor: 'pointer',
                  borderColor:
                    selectedTemplate?.id === template.id
                      ? 'var(--mantine-color-burgundy-6)'
                      : 'rgba(136, 1, 36, 0.1)',
                  backgroundColor:
                    selectedTemplate?.id === template.id ? 'rgba(136, 1, 36, 0.05)' : 'white',
                  transition: 'all 0.3s ease',
                  borderRadius: '12px',
                }}
                onClick={() => setSelectedTemplate(template)}
              >
                <Stack gap={4}>
                  {/* Row 1: Title (left) + Toggle + Enabled/Disabled badge (right) */}
                  <Group justify="space-between" wrap="nowrap">
                    <Text fw={600} c="burgundy" mb={0}>
                      {template.title || template.templateType}
                    </Text>

                    <Group gap="xs" wrap="nowrap">
                      <Switch
                        size="sm"
                        color="green"
                        checked={template.sendingEnabled}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleSendingMutation.mutate({
                            id: template.id ?? '',
                            enabled: e.currentTarget.checked,
                          });
                        }}
                        onClick={(e) => e.stopPropagation()}
                      />
                      <Badge
                        size="sm"
                        style={{
                          backgroundColor: template.sendingEnabled ? '#2e7d32' : '#c62828',
                          color: '#FFF8F0',
                          textTransform: 'none',
                          fontWeight: 600,
                          fontSize: '11px',
                        }}
                      >
                        {template.sendingEnabled ? 'Enabled' : 'Disabled'}
                      </Badge>
                    </Group>
                  </Group>

                  {/* Row 2: Last updated */}
                  <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                    Updated{' '}
                    {template.updatedAt
                      ? new Date(template.updatedAt).toLocaleDateString()
                      : 'Never'}
                  </Text>
                </Stack>
              </Card>
            )
          ))}
        </SimpleGrid>
      </div>

      {/* Editor Panel - Shown when template selected (non-Events: Email + Test Email tabs) */}
      {selectedTemplate && category !== 'Events' && (
        <Paper shadow="sm" radius="md" px="xl" pb="xl" mt={0} style={{ border: '1px solid rgba(136, 1, 36, 0.1)', paddingTop: '12px' }}>
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
            {/* Header row: title (left) + tabs (center) + close (right) */}
            <Group justify="space-between" mb="md" wrap="nowrap">
              <Group gap="sm" style={{ flex: 1, minWidth: 0 }}>
                {isEditingTitle ? (
                  <>
                    <TextInput
                      value={editingTitle}
                      onChange={(e) => setEditingTitle(e.currentTarget.value)}
                      maxLength={100}
                      style={{ flex: 1 }}
                      styles={{ input: { fontWeight: 600, fontSize: '18px', color: '#880124' } }}
                      autoFocus
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') saveTitleMutation.mutate(editingTitle);
                        if (e.key === 'Escape') setIsEditingTitle(false);
                      }}
                    />
                    <Text
                      component="a"
                      size="sm"
                      c="burgundy"
                      fw={600}
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => saveTitleMutation.mutate(editingTitle)}
                    >
                      Save
                    </Text>
                    <Text
                      component="a"
                      size="sm"
                      c="dimmed"
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => setIsEditingTitle(false)}
                    >
                      Cancel
                    </Text>
                  </>
                ) : (
                  <>
                    <Text fw={600} c="burgundy" size="lg" style={{ whiteSpace: 'nowrap' }}>
                      {title || selectedTemplate.templateType}
                    </Text>
                    <Text
                      component="a"
                      size="sm"
                      c="burgundy"
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => {
                        setEditingTitle(title);
                        setIsEditingTitle(true);
                      }}
                    >
                      Edit
                    </Text>
                  </>
                )}
              </Group>
              {!isEditingTitle && (
                <Tabs.List style={{ flex: 0, flexWrap: 'nowrap' }}>
                  <Tabs.Tab value="email">Email</Tabs.Tab>
                  <Tabs.Tab value="test">Test Email</Tabs.Tab>
                </Tabs.List>
              )}
              <Group justify="flex-end" style={{ flex: 1, minWidth: 0 }}>
                <Button variant="subtle" color="dimmed" size="compact-sm" onClick={handleCancel}>
                  Close
                </Button>
              </Group>
            </Group>

              {/* Email Tab */}
              <Tabs.Panel value="email" pt="lg">
                <Stack gap="md">
                  <TextInput
                    label="Subject Line"
                    value={subject}
                    onChange={(e) => { setSubject(e.currentTarget.value); setHasUnsavedChanges(true); }}
                    required
                    maxLength={200}
                  />

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
                      {(selectedTemplate.variables?.length ?? 0) > 0
                        ? selectedTemplate.variables?.join(', ')
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
                      onChange={(val) => { setHtmlBody(val); setHasUnsavedChanges(true); }}
                      placeholder="Enter email template content..."
                      minRows={12}
                    />
                  </div>

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
                        {selectedTemplate.variables?.join(', ')}
                      </Text>
                    </Alert>
                  )}

                  <Group justify="flex-end" gap="sm">
                    <Button variant="light" onClick={handleCancel}>
                      Cancel
                    </Button>
                    <Button
                      onClick={handleSave}
                      loading={saveMutation.isPending}
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
              </Tabs.Panel>

              {/* Test Email Tab */}
              <Tabs.Panel value="test" pt="lg">
                <SendTestEmail
                  template={selectedTemplate}
                  currentTitle={title}
                  currentSubject={subject}
                  currentHtmlBody={htmlBody}
                  onSaveTemplate={handleSaveAsync}
                  hasUnsavedChanges={hasUnsavedChanges}
                />
              </Tabs.Panel>
          </Tabs>
        </Paper>
      )}

      {/* Events Tabbed Editor Panel - Email / Trigger / Test Email */}
      {selectedTemplate && category === 'Events' && (
        <Paper shadow="sm" radius="md" px="xl" pb="xl" mt={0} style={{ border: '1px solid rgba(136, 1, 36, 0.1)', paddingTop: '12px' }}>
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
            {/* Header row: title (left) + tabs (center) + close (right) */}
            <Group justify="space-between" mb="md" wrap="nowrap">
              <Group gap="sm" style={{ flex: 1, minWidth: 0 }}>
                {isEditingTitle ? (
                  <>
                    <TextInput
                      value={editingTitle}
                      onChange={(e) => setEditingTitle(e.currentTarget.value)}
                      maxLength={100}
                      style={{ flex: 1 }}
                      styles={{ input: { fontWeight: 600, fontSize: '18px', color: '#880124' } }}
                      autoFocus
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') saveTitleMutation.mutate(editingTitle);
                        if (e.key === 'Escape') setIsEditingTitle(false);
                      }}
                    />
                    <Text
                      component="a"
                      size="sm"
                      c="burgundy"
                      fw={600}
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => saveTitleMutation.mutate(editingTitle)}
                    >
                      Save
                    </Text>
                    <Text
                      component="a"
                      size="sm"
                      c="dimmed"
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => setIsEditingTitle(false)}
                    >
                      Cancel
                    </Text>
                  </>
                ) : (
                  <>
                    <Text fw={600} c="burgundy" size="lg" style={{ whiteSpace: 'nowrap' }}>
                      {title || selectedTemplate.templateType}
                    </Text>
                    <Text
                      component="a"
                      size="sm"
                      c="burgundy"
                      style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                      onClick={() => {
                        setEditingTitle(title);
                        setIsEditingTitle(true);
                      }}
                    >
                      Edit
                    </Text>
                  </>
                )}
              </Group>
              {!isEditingTitle && (
                <Tabs.List style={{ flex: 0, flexWrap: 'nowrap' }}>
                  <Tabs.Tab value="email">Email</Tabs.Tab>
                  <Tabs.Tab value="trigger">Trigger</Tabs.Tab>
                  <Tabs.Tab value="test">Test Email</Tabs.Tab>
                </Tabs.List>
              )}
              <Group justify="flex-end" style={{ flex: 1, minWidth: 0 }}>
                <Button variant="subtle" color="dimmed" size="compact-sm" onClick={handleCancel}>
                  Close
                </Button>
              </Group>
            </Group>

              {/* Email Tab */}
              <Tabs.Panel value="email" pt="lg">
                <Stack gap="md">
                  <TextInput
                    label="Subject Line"
                    value={subject}
                    onChange={(e) => { setSubject(e.currentTarget.value); setHasUnsavedChanges(true); }}
                    required
                    maxLength={200}
                  />

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
                      {(selectedTemplate.variables?.length ?? 0) > 0
                        ? selectedTemplate.variables?.join(', ')
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
                      onChange={(val) => { setHtmlBody(val); setHasUnsavedChanges(true); }}
                      placeholder="Enter email template content..."
                      minRows={12}
                    />
                  </div>

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
                        {selectedTemplate.variables?.join(', ')}
                      </Text>
                    </Alert>
                  )}

                  <Group justify="flex-end" gap="sm">
                    <Button variant="light" onClick={handleCancel}>
                      Cancel
                    </Button>
                    <Button
                      onClick={handleSave}
                      loading={saveMutation.isPending}
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
              </Tabs.Panel>

              {/* Trigger Tab */}
              <Tabs.Panel value="trigger" pt="lg">
                <TriggerConfigPanel
                  template={selectedTemplate as any}
                  onSave={async (config) => {
                    await saveTriggerMutation.mutateAsync({
                      id: selectedTemplate.id ?? '',
                      config,
                    });
                  }}
                />
              </Tabs.Panel>

              {/* Test Email Tab */}
              <Tabs.Panel value="test" pt="lg">
                <SendTestEmail
                  template={selectedTemplate}
                  currentTitle={title}
                  currentSubject={subject}
                  currentHtmlBody={htmlBody}
                  onSaveTemplate={handleSaveAsync}
                  hasUnsavedChanges={hasUnsavedChanges}
                />
              </Tabs.Panel>
          </Tabs>
        </Paper>
      )}

      {/* Trigger Config Modal - kept for non-Events categories if needed in future */}
      {category !== 'Events' && selectedTemplateForTrigger && (
        <TriggerConfigModal
          opened={triggerModalOpened}
          onClose={() => setTriggerModalOpened(false)}
          template={selectedTemplateForTrigger as GlobalEmailTemplateDto & { triggerType?: 'FixedEvent' | 'TimeBased' | 'Manual'; sendingEnabled?: boolean; timingOffsetDays?: number; recipientGroup?: any }}
          onSave={async (config) => {
            await saveTriggerMutation.mutateAsync({ id: selectedTemplateForTrigger.id ?? '', config });
          }}
        />
      )}
    </Stack>
  );
};
