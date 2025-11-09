import React, { useState, useEffect } from 'react';
import {
  Stack,
  Group,
  Card,
  Text,
  TextInput,
  Button,
  Paper,
  Alert,
  Box,
  Loader,
} from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';
import { emailTemplatesApi, type GlobalEmailTemplateDto } from '../../services/emailTemplates.api';
import { notifications } from '@mantine/notifications';

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
  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState('');
  const [plainTextBody, setPlainTextBody] = useState('');
  const [invalidVariables, setInvalidVariables] = useState<string[]>([]);

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
    mutationFn: (data: { subject: string; htmlBody: string; plainTextBody: string }) => {
      if (!selectedTemplate) {
        throw new Error('No template selected');
      }
      return emailTemplatesApi.updateGlobalTemplate(selectedTemplate.id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-templates', 'global', category] });
      notifications.show({
        message: 'Template saved successfully',
        color: 'green',
      });
      setSelectedTemplate(null);
      setSubject('');
      setHtmlBody('');
      setPlainTextBody('');
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save template',
        color: 'red',
      });
    },
  });

  // Sync editor state when template is selected
  useEffect(() => {
    if (selectedTemplate) {
      setSubject(selectedTemplate.subject);
      setHtmlBody(selectedTemplate.htmlBody);
      setPlainTextBody(selectedTemplate.plainTextBody);
      setInvalidVariables([]);
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
      (v) => !selectedTemplate.variables.includes(v)
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
      subject,
      htmlBody,
      plainTextBody: plainText,
    });
  };

  // Handle cancel
  const handleCancel = () => {
    setSelectedTemplate(null);
    setSubject('');
    setHtmlBody('');
    setPlainTextBody('');
    setInvalidVariables([]);
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

  return (
    <Stack gap="xl">
      {/* Template Cards - Horizontal Scrollable Group */}
      <div>
        <Text size="sm" c="dimmed" mb="md">
          Click on a template card to edit it below
        </Text>

        <Group gap="md" style={{ flexWrap: 'wrap' }}>
          {templates.map((template) => (
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
                minWidth: '220px',
                maxWidth: '300px',
                flex: 1,
                position: 'relative',
                transition: 'all 0.3s ease',
                borderRadius: '12px',
              }}
              onClick={() => setSelectedTemplate(template)}
            >
              <Text fw={600} c="burgundy" mb={4}>
                {template.templateTypeName}
              </Text>

              <Text size="sm" c="stone" mb="xs">
                Subject: {template.subject}
              </Text>

              <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                Version {template.version} • Updated {new Date(template.lastModified).toLocaleDateString()}
              </Text>
            </Card>
          ))}
        </Group>
      </div>

      {/* Editor Panel - Shown when template selected */}
      {selectedTemplate && (
        <Paper shadow="sm" radius="md" p="xl" mt="xl" style={{ border: '1px solid rgba(136, 1, 36, 0.1)' }}>
          <Stack gap="md">
            {/* Header */}
            <Group justify="space-between">
              <Text fw={600} c="burgundy" size="lg">
                Currently Editing: {selectedTemplate.templateTypeName}
              </Text>
            </Group>

            {/* Subject Line */}
            <TextInput
              label="Subject Line"
              value={subject}
              onChange={(e) => setSubject(e.currentTarget.value)}
              required
              maxLength={200}
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
                {selectedTemplate.variables.join(', ')}
              </Text>
            </Box>

            {/* HTML Body Editor */}
            <div>
              <Text size="sm" fw={500} mb={4}>
                Email Content (HTML)
              </Text>
              <MantineTiptapEditor
                value={htmlBody}
                onChange={setHtmlBody}
                placeholder="Enter email template content..."
                minRows={12}
              />
            </div>

            {/* Variable Validation Warnings */}
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
                  {selectedTemplate.variables.join(', ')}
                </Text>
              </Alert>
            )}

            {/* Action Buttons */}
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
        </Paper>
      )}
    </Stack>
  );
};
