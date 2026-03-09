import React, { useState, useEffect } from 'react';
import {
  Stack,
  Paper,
  SimpleGrid,
  TextInput,
  Button,
  Text,
  Divider,
  Group,
  Box,
  Loader,
  Alert,
} from '@mantine/core';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import { emailTemplatesApi } from '../../services/emailTemplates.api';

/**
 * Variable groups for email template test data.
 * Each group contains a display name and the snake_case variable names.
 */
const VARIABLE_GROUPS = [
  {
    name: 'Global',
    variables: ['user_name', 'system_url', 'custom_message', 'custom_content'],
  },
  {
    name: 'Vetting',
    variables: [
      'scene_name',
      'application_number',
      'submission_date',
      'application_date',
      'status_change_date',
      'current_status',
      'interview_link',
      'approval_date',
      'hold_reason',
      'required_actions',
      'review_date',
    ],
  },
  {
    name: 'Events',
    variables: [
      'attendee_name',
      'event_title',
      'session_date',
      'session_time',
      'venue_name',
      'venue_address',
      'ticket_type',
      'total_paid',
      'confirmation_number',
      'session_name',
      'ticket_sessions_list',
      'ticket_sessions_list_text',
      'cancelled_sessions_list',
      'cancelled_sessions_list_text',
    ],
  },
  {
    name: 'Volunteers',
    variables: [
      'volunteer_name',
      'volunteer_role',
      'shift_start',
      'shift_end',
    ],
  },
  {
    name: 'Admin',
    variables: [
      'account_email',
      'reset_url',
      'action_required',
      'deadline_date',
      'verification_url',
      'refund_amount',
      'original_amount',
      'payment_method',
      'timing_message',
      'refund_reason',
      'refund_id',
    ],
  },
  {
    name: 'Incident',
    variables: [
      'reporter_name',
      'incident_number',
      'incident_date',
      'coordinator_name',
      'next_steps',
      'status',
    ],
  },
  {
    name: 'Ad Hoc',
    variables: ['recipient_name'],
  },
];

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
 * Email Test Data Tab - Manages all default variable values used when sending test emails.
 * Displays variables organized by category with editable text inputs.
 * Values persist in the database via the Settings table.
 */
export const EmailTestDataTab: React.FC = () => {
  const queryClient = useQueryClient();
  const [localValues, setLocalValues] = useState<Record<string, string>>({});

  // Fetch saved test data
  const {
    data: testData,
    isLoading,
    error,
  } = useQuery<Record<string, string>>({
    queryKey: ['email-test-data'],
    queryFn: () => emailTemplatesApi.getTestData(),
  });

  // Populate local state when data loads
  useEffect(() => {
    if (testData) {
      setLocalValues(testData);
    }
  }, [testData]);

  // Save mutation
  const saveMutation = useMutation<void, Error, Record<string, string>>({
    mutationFn: (data: Record<string, string>) => emailTemplatesApi.saveTestData(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['email-test-data'] });
      notifications.show({
        message: 'Test data defaults saved successfully',
        color: 'green',
        icon: <IconCheck size={16} />,
      });
    },
    onError: (error: any) => {
      notifications.show({
        message: error.message || 'Failed to save test data',
        color: 'red',
      });
    },
  });

  const handleChange = (variable: string, value: string) => {
    setLocalValues((prev) => ({ ...prev, [variable]: value }));
  };

  const handleSave = () => {
    saveMutation.mutate(localValues);
  };

  // Loading state
  if (isLoading) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Loader size="lg" />
        <Text mt="md" c="dimmed">
          Loading test data...
        </Text>
      </Box>
    );
  }

  // Error state
  if (error) {
    return (
      <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Test Data">
        <Text>{(error as Error).message || 'Failed to load test data'}</Text>
      </Alert>
    );
  }

  return (
    <Paper shadow="sm" radius="md" p="xl" style={{ border: '1px solid rgba(136, 1, 36, 0.1)' }}>
      <Stack gap="md">
        <Text fw={600} c="burgundy" size="lg">
          Email Template Test Data
        </Text>
        <Text size="sm" c="dimmed" mb="lg">
          Default values used when sending test emails. Changes are saved and persist across
          sessions.
        </Text>

        {VARIABLE_GROUPS.map((group, index) => (
          <React.Fragment key={group.name}>
            {index > 0 && <Divider my="md" />}
            <Text fw={600} c="burgundy" size="md" mb="xs">
              {group.name}
            </Text>
            <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="sm">
              {group.variables.map((variable) => (
                <TextInput
                  key={variable}
                  label={toTitleCase(variable)}
                  value={localValues[variable] || ''}
                  onChange={(e) => handleChange(variable, e.currentTarget.value)}
                />
              ))}
            </SimpleGrid>
          </React.Fragment>
        ))}

        <Divider my="md" />

        <Group justify="flex-end">
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
            Save Defaults
          </Button>
        </Group>
      </Stack>
    </Paper>
  );
};
