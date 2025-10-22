import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Container,
  Title,
  Text,
  Group,
  Button,
  Stack,
  Paper,
  Table,
  Badge,
  TextInput,
  Loader,
  Alert
} from '@mantine/core';
import {
  IconArrowLeft,
  IconAlertCircle
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { MantineTiptapEditor } from '../../../../components/forms/MantineTiptapEditor';
import { emailTemplatesApi } from '../services/emailTemplates.api';
import type { EmailTemplateResponse } from '../types/emailTemplates.types';

export const EmailTemplates: React.FC = () => {
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<EmailTemplateResponse[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<EmailTemplateResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load templates on mount
  useEffect(() => {
    loadTemplates();
  }, []);

  const loadTemplates = async () => {
    try {
      setIsLoading(true);
      setError(null);
      console.log('EmailTemplates: Loading templates from API');

      const data = await emailTemplatesApi.getEmailTemplates();

      console.log('EmailTemplates: Templates loaded successfully', {
        count: data.length
      });

      setTemplates(data);
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to load email templates';
      console.error('EmailTemplates: Error loading templates:', errorMessage);
      setError(errorMessage);

      notifications.show({
        title: 'Error Loading Templates',
        message: errorMessage,
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToVetting = () => {
    navigate('/admin/vetting');
  };

  const handleSelectTemplate = (template: EmailTemplateResponse) => {
    setSelectedTemplate(template);
  };

  const handleSaveTemplate = async () => {
    if (!selectedTemplate) {
      console.warn('EmailTemplates: No template selected to save');
      return;
    }

    try {
      setIsSaving(true);
      console.log('EmailTemplates: Saving template', {
        id: selectedTemplate.id,
        subject: selectedTemplate.subject
      });

      // Call API to update template
      const updatedTemplate = await emailTemplatesApi.updateEmailTemplate(
        selectedTemplate.id,
        {
          subject: selectedTemplate.subject,
          htmlBody: selectedTemplate.htmlBody,
          plainTextBody: selectedTemplate.plainTextBody || '' // Use empty string if not set
        }
      );

      console.log('EmailTemplates: Template saved successfully', {
        id: updatedTemplate.id,
        version: updatedTemplate.version
      });

      // Update template in list with the response from API
      setTemplates(prev => prev.map(t =>
        t.id === updatedTemplate.id ? updatedTemplate : t
      ));

      // Close editor
      setSelectedTemplate(null);

      // Show success notification
      notifications.show({
        title: 'Template Saved',
        message: 'Email template has been updated successfully',
        color: 'green'
      });
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to save email template';
      console.error('EmailTemplates: Error saving template:', errorMessage);

      // Show error notification
      notifications.show({
        title: 'Error Saving Template',
        message: errorMessage,
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });

      // Keep editor open so user doesn't lose changes
    } finally {
      setIsSaving(false);
    }
  };

  const getTypeBadgeColor = (templateTypeName: string) => {
    // Map template type names to badge colors
    const lowerType = templateTypeName.toLowerCase();
    if (lowerType.includes('received')) return 'blue';
    if (lowerType.includes('scheduled')) return 'yellow';
    if (lowerType.includes('approved')) return 'green';
    if (lowerType.includes('denied')) return 'red';
    if (lowerType.includes('hold')) return 'orange';
    return 'gray';
  };

  // Show loading state
  if (isLoading) {
    return (
      <Container size="xl" py="xl">
        <Stack align="center" py="xl">
          <Loader size="lg" />
          <Text c="dimmed">Loading email templates...</Text>
        </Stack>
      </Container>
    );
  }

  // Show error state
  if (error && templates.length === 0) {
    return (
      <Container size="xl" py="xl">
        <Alert
          icon={<IconAlertCircle />}
          color="red"
          title="Error Loading Templates"
        >
          <Stack gap="md">
            <Text>{error}</Text>
            <Button onClick={loadTemplates} variant="light" color="red">
              Try Again
            </Button>
          </Stack>
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Group justify="space-between" align="center" mb="xl">
        <div>
          <Group mb="sm">
            <Button
              variant="subtle"
              leftSection={<IconArrowLeft size={16} />}
              onClick={handleBackToVetting}
              size="sm"
            >
              Back to Vetting Applications
            </Button>
          </Group>
          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '32px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Vetting Email Templates
          </Title>
        </div>
      </Group>

      {/* Templates Table */}
      <Paper shadow="sm" radius="md">
        <Table striped highlightOnHover>
          <Table.Thead style={{ backgroundColor: '#880124', color: 'white' }}>
            <Table.Tr>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  TEMPLATE NAME
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  TYPE
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  SUBJECT
                </Text>
              </Table.Th>
              <Table.Th style={{ backgroundColor: '#880124', borderBottom: 'none' }}>
                <Text fw={600} size="sm" style={{ color: 'white', textTransform: 'uppercase' }}>
                  LAST MODIFIED
                </Text>
              </Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {templates.map((template) => (
              <Table.Tr
                key={template.id}
                onClick={() => handleSelectTemplate(template)}
                style={{
                  cursor: 'pointer',
                  backgroundColor: selectedTemplate?.id === template.id
                    ? 'rgba(136, 1, 36, 0.05)'
                    : undefined
                }}
              >
                <Table.Td>
                  <Text size="sm" fw={600}>
                    {template.templateTypeName}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Badge color={getTypeBadgeColor(template.templateTypeName)} variant="light">
                    {template.templateTypeName}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {template.subject}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {new Date(template.lastModified).toLocaleDateString()}
                  </Text>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>

        {templates.length === 0 && (
          <Stack align="center" py="xl">
            <Text c="dimmed" size="lg">
              No email templates found
            </Text>
          </Stack>
        )}
      </Paper>

      {/* Template Editor Section - Shows when template is selected */}
      {selectedTemplate && (
        <Paper shadow="sm" radius="md" p="xl" mt="xl">
          <Stack gap="md">
            {/* Header with template name */}
            <Group justify="space-between">
              <div>
                <Title order={3} style={{
                  fontFamily: "'Montserrat', sans-serif",
                  fontSize: '24px',
                  fontWeight: 700,
                  color: '#880124'
                }}>
                  Edit Template: {selectedTemplate.templateTypeName}
                </Title>
                <Text size="sm" c="dimmed" mt="xs">
                  Available variables: {selectedTemplate.variables || 'No variables available'}
                </Text>
              </div>
              <Button
                variant="subtle"
                onClick={() => setSelectedTemplate(null)}
                disabled={isSaving}
              >
                Close Editor
              </Button>
            </Group>

            {/* Subject Field */}
            <TextInput
              label="Subject Line"
              placeholder="Enter email subject"
              value={selectedTemplate.subject}
              onChange={(e) => {
                setSelectedTemplate({
                  ...selectedTemplate,
                  subject: e.currentTarget.value
                })
              }}
              required
              disabled={isSaving}
              styles={{
                label: { fontWeight: 600, marginBottom: 8 }
              }}
            />

            {/* Email Content - TipTap Editor */}
            <div>
              <Text fw={600} size="sm" mb={8}>
                Email Content
              </Text>
              <MantineTiptapEditor
                value={selectedTemplate.htmlBody || ''}
                onChange={(html) => {
                  setSelectedTemplate({
                    ...selectedTemplate,
                    htmlBody: html
                  })
                }}
                placeholder="Enter email template content..."
                minRows={12}
              />
              <Text size="xs" c="dimmed" mt="xs">
                Use variables like {'{{scene_name}}'} in your template. They will be replaced with actual values when emails are sent.
              </Text>
            </div>

            {/* Action Buttons */}
            <Group justify="flex-end" mt="md">
              <Button
                variant="light"
                onClick={() => setSelectedTemplate(null)}
                disabled={isSaving}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSaveTemplate}
                loading={isSaving}
                styles={{
                  root: {
                    backgroundColor: '#880124',
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2'
                  }
                }}
              >
                Save Template
              </Button>
            </Group>
          </Stack>
        </Paper>
      )}
    </Container>
  );
};
