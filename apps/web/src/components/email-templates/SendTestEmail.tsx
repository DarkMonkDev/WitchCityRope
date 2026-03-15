import React, { useState, useEffect } from 'react';
import {
  Stack,
  SimpleGrid,
  TextInput,
  Button,
  Text,
  Divider,
  Group,
  Alert,
} from '@mantine/core';
import { IconInfoCircle, IconSend } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { emailTemplatesApi, type GlobalEmailTemplateDto } from '../../services/emailTemplates.api';

interface SendTestEmailProps {
  /** The currently selected template (to know which variables to show and which template to send) */
  template: GlobalEmailTemplateDto;
  /** Current edited title (may differ from saved) */
  currentTitle: string;
  /** Current edited subject (may differ from saved) */
  currentSubject: string;
  /** Current edited HTML body (may differ from saved) */
  currentHtmlBody: string;
  /** Callback to trigger saving the template before sending test */
  onSaveTemplate: () => Promise<void>;
  /** Whether the template has unsaved changes */
  hasUnsavedChanges: boolean;
}

/**
 * Converts a snake_case string to Title Case.
 * e.g., "scene_name" -> "Scene Name"
 */
const toTitleCase = (snakeCase: string): string =>
  snakeCase
    .split('_')
    .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
    .join(' ');

/**
 * Inline Send Test Email component rendered inside the template editor.
 * Shows only the variables relevant to the currently selected template,
 * pre-filled from saved test data defaults.
 */
export const SendTestEmail: React.FC<SendTestEmailProps> = ({
  template,
  currentTitle: _currentTitle,
  currentSubject: _currentSubject,
  currentHtmlBody: _currentHtmlBody,
  onSaveTemplate,
  hasUnsavedChanges,
}) => {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState('');
  const [variableOverrides, setVariableOverrides] = useState<Record<string, string>>({});

  // Fetch available variables from the code registry endpoint
  const { data: rawVariables } = useQuery<string[]>({
    queryKey: ['email-template-variables', template.category, template.templateType],
    queryFn: () => emailTemplatesApi.getVariablesForTemplate(
      template.category ?? '',
      template.templateType ?? ''
    ),
    enabled: !!template.category && !!template.templateType,
  });

  // Strip {{ and }} braces for display and test data keys
  const templateVariables = (rawVariables || []).map((v) =>
    v.replace(/^\{\{/, '').replace(/\}\}$/, '')
  );

  // Fetch saved test data defaults
  const { data: testData } = useQuery<Record<string, string>>({
    queryKey: ['email-test-data'],
    queryFn: () => emailTemplatesApi.getTestData(),
  });

  // Pre-fill variable values from saved test data.
  // Depends on rawVariables so it re-runs when the registry API response arrives.
  useEffect(() => {
    if (testData && templateVariables.length > 0) {
      const prefilled: Record<string, string> = {};
      templateVariables.forEach((variable) => {
        prefilled[variable] = testData[variable] || '';
      });
      setVariableOverrides(prefilled);
    }
  }, [testData, template.id, rawVariables]);

  // Send test email mutation
  const sendMutation = useMutation({
    mutationFn: (request: { email: string; variableOverrides?: Record<string, string> }) =>
      emailTemplatesApi.sendTestEmail(template.id ?? '', request),
    onSuccess: (data: { message: string; templateType: string; sentTo: string }) => {
      queryClient.invalidateQueries({ queryKey: ['email-test-data'] });
      notifications.show({
        message: `Test email sent to ${data.sentTo}`,
        color: 'green',
      });
    },
    onError: (err: Error) => {
      notifications.show({
        message: err.message || 'Failed to send test email',
        color: 'red',
      });
    },
  });

  const handleSend = async () => {
    if (!email.trim()) {
      notifications.show({
        message: 'Please enter an email address',
        color: 'red',
      });
      return;
    }

    try {
      // Save template first if there are unsaved changes
      if (hasUnsavedChanges) {
        notifications.show({
          message: 'Saving template before sending test...',
          color: 'blue',
          autoClose: 2000,
        });
        await onSaveTemplate();
      }

      // Collect all variable values and send
      sendMutation.mutate({
        email: email.trim(),
        variableOverrides,
      });
    } catch (error: any) {
      notifications.show({
        message: error.message || 'Failed to save template before sending',
        color: 'red',
      });
    }
  };

  const handleVariableChange = (variable: string, value: string) => {
    setVariableOverrides((prev) => ({ ...prev, [variable]: value }));
  };

  return (
      <Stack gap="md">
        <Text fw={600} c="burgundy" size="lg">
          Send Test Email
        </Text>
        <Text size="sm" c="dimmed">
          Send a test version of this template to verify formatting and variable substitution.
        </Text>

        {templateVariables.length > 0 ? (
          <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
            {templateVariables.map((variable) => (
              <TextInput
                key={variable}
                label={toTitleCase(variable)}
                value={variableOverrides[variable] || ''}
                onChange={(e) => handleVariableChange(variable, e.currentTarget.value)}
              />
            ))}
          </SimpleGrid>
        ) : (
          <Alert icon={<IconInfoCircle />} variant="light" color="blue">
            This template has no dynamic variables. The email will be sent as-is.
          </Alert>
        )}

        <Divider />

        <Group justify="flex-start" align="flex-end" gap="sm">
          <TextInput
            label="Send to"
            placeholder="Enter email address..."
            value={email}
            onChange={(e) => setEmail(e.currentTarget.value)}
            style={{ flex: 1 }}
          />
          <Button
            onClick={handleSend}
            loading={sendMutation.isPending}
            leftSection={<IconSend size={16} />}
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
            Send Test
          </Button>
        </Group>

        <Alert icon={<IconInfoCircle />} variant="light" color="blue">
          Variable values will be saved as new defaults when you send the test email.
        </Alert>
      </Stack>
  );
};
