import React, { useState, useEffect } from 'react';
import {
  Container,
  Title,
  Text,
  Paper,
  Stack,
  Select,
  TextInput,
  Button,
  Alert,
  Group,
  Loader,
  Box,
} from '@mantine/core';
import { IconCheck, IconAlertCircle, IconSettings } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';

interface Settings {
  EventTimeZone: string;
  PreStartBufferMinutes: string;
}

// US Timezones only - exactly 4 as specified
const US_TIMEZONES = [
  { value: 'America/New_York', label: 'Eastern Time (ET)' },
  { value: 'America/Chicago', label: 'Central Time (CT)' },
  { value: 'America/Denver', label: 'Mountain Time (MT)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (PT)' },
];

/**
 * Admin Settings Page
 * Allows administrators to configure system-wide settings:
 * - Event timezone (4 US timezones only)
 * - Pre-start buffer time for registration/cancellation cutoff
 */
export const AdminSettingsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [localSettings, setLocalSettings] = useState<Settings>({
    EventTimeZone: 'America/New_York',
    PreStartBufferMinutes: '0',
  });

  // Fetch current settings
  const { data: settings, isLoading, error } = useQuery<Settings>({
    queryKey: ['adminSettings'],
    queryFn: async () => {
      const response = await api.get<Settings>('/api/admin/settings');
      return response.data;
    },
  });

  // Update local settings when data loads
  useEffect(() => {
    if (settings) {
      setLocalSettings(settings);
    }
  }, [settings]);

  // Update settings mutation
  const updateMutation = useMutation({
    mutationFn: async (updates: Settings) => {
      const response = await api.put('/api/admin/settings', {
        settings: updates,
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminSettings'] });
    },
  });

  const handleSave = () => {
    // Validate buffer minutes
    const bufferMinutes = parseInt(localSettings.PreStartBufferMinutes, 10);
    if (isNaN(bufferMinutes) || bufferMinutes < 0) {
      return; // Validation handled by form
    }

    updateMutation.mutate(localSettings);
  };

  const handleReset = () => {
    if (settings) {
      setLocalSettings(settings);
    }
  };

  const hasChanges =
    settings &&
    (localSettings.EventTimeZone !== settings.EventTimeZone ||
      localSettings.PreStartBufferMinutes !== settings.PreStartBufferMinutes);

  const bufferMinutesInt = parseInt(localSettings.PreStartBufferMinutes, 10);
  const hasValidationError = isNaN(bufferMinutesInt) || bufferMinutesInt < 0;

  if (isLoading) {
    return (
      <Container size="md" py="xl">
        <Group justify="center">
          <Loader size="lg" />
        </Group>
      </Container>
    );
  }

  if (error) {
    return (
      <Container size="md" py="xl">
        <Alert icon={<IconAlertCircle />} title="Error" color="red">
          Failed to load settings. Please try again later.
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="md" py="xl">
      {/* Header */}
      <Group mb="xl">
        <IconSettings size={32} color="#8B8680" />
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
          Settings
        </Title>
      </Group>

      <Text size="sm" c="dimmed" mb="xl">
        Configure system settings for event management
      </Text>

      {/* Success Alert */}
      {updateMutation.isSuccess && (
        <Alert
          icon={<IconCheck />}
          title="Success"
          color="green"
          mb="md"
        >
          Settings updated successfully!
        </Alert>
      )}

      {/* Error Alert */}
      {updateMutation.isError && (
        <Alert
          icon={<IconAlertCircle />}
          title="Error"
          color="red"
          mb="md"
        >
          {(updateMutation.error as Error)?.message || 'Failed to update settings'}
        </Alert>
      )}

      {/* Settings Form */}
      <Paper shadow="sm" p="xl" radius="md" withBorder>
        <Stack gap="lg">
          {/* Event Timezone Setting */}
          <Select
            label="Event Timezone"
            description="The timezone where your events occur. All event times will be interpreted in this timezone."
            data={US_TIMEZONES}
            value={localSettings.EventTimeZone}
            onChange={(value) =>
              setLocalSettings((prev) => ({
                ...prev,
                EventTimeZone: value || 'America/New_York',
              }))
            }
            required
            styles={{
              label: { fontWeight: 600, color: '#2B2B2B' },
              description: { fontSize: '14px' },
            }}
          />

          {/* Pre-Start Buffer Setting */}
          <TextInput
            label="Pre-Start Buffer (Minutes)"
            description="Minutes before event start when registration and cancellations close. Set to 0 to allow until event starts."
            placeholder="0"
            value={localSettings.PreStartBufferMinutes}
            onChange={(e) =>
              setLocalSettings((prev) => ({
                ...prev,
                PreStartBufferMinutes: e.target.value,
              }))
            }
            type="number"
            min={0}
            required
            error={hasValidationError ? 'Must be a non-negative number' : undefined}
            styles={{
              label: { fontWeight: 600, color: '#2B2B2B' },
              description: { fontSize: '14px' },
            }}
          />

          {/* Action Buttons */}
          <Group justify="space-between" mt="md">
            <Button
              variant="subtle"
              color="gray"
              onClick={handleReset}
              disabled={!hasChanges || updateMutation.isPending}
            >
              Reset Changes
            </Button>

            <Button
              variant="filled"
              color="#880124"
              onClick={handleSave}
              disabled={!hasChanges || hasValidationError || updateMutation.isPending}
              loading={updateMutation.isPending}
            >
              Save Settings
            </Button>
          </Group>
        </Stack>
      </Paper>

      {/* Information Box */}
      <Paper shadow="xs" p="md" mt="xl" style={{ background: '#F5F5F5', borderLeft: '4px solid #880124' }}>
        <Text size="sm" fw={600} mb="xs">
          About These Settings
        </Text>
        <Box>
          <Text size="sm" c="dimmed">
            <strong>Timezone:</strong> All events are assumed to occur in the selected timezone. Users in different
            timezones will see times converted to their local timezone when viewing events.
          </Text>
          <Text size="sm" c="dimmed" mt="xs">
            <strong>Buffer Time:</strong> Prevents last-minute registrations and cancellations. For example, setting
            this to 30 means registration closes 30 minutes before the event starts.
          </Text>
        </Box>
      </Paper>
    </Container>
  );
};
