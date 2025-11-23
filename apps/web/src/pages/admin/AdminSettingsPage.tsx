import React, { useState, useEffect } from 'react';
import {
  Container,
  Title,
  Text,
  Stack,
  Select,
  TextInput,
  Button,
  Alert,
  Group,
  Loader,
  Box,
  Grid,
  Modal,
} from '@mantine/core';
import { IconCheck, IconAlertCircle, IconSettings, IconClock } from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { VenueManagementCard } from '../../components/admin/VenueManagementCard';
import { BackupManagementCard } from '../../features/admin/backup/components/BackupManagementCard';

interface Settings {
  EventTimeZone: string;
}

interface Backup {
  fileName: string;
  timestamp: string;
  sizeBytes: number;
  sizeFormatted: string;
  displayName?: string;
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
 */
export const AdminSettingsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [localSettings, setLocalSettings] = useState<Settings>({
    EventTimeZone: 'America/New_York',
  });

  // Backup restore modal state
  const [restoreModalOpen, setRestoreModalOpen] = useState(false);
  const [backupToRestore, setBackupToRestore] = useState<Backup | null>(null);
  const [confirmationText, setConfirmationText] = useState('');

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
    updateMutation.mutate(localSettings);
  };

  const handleRestoreClick = (backup: Backup) => {
    setBackupToRestore(backup);
    setRestoreModalOpen(true);
    setConfirmationText('');
  };

  const handleConfirmRestore = () => {
    if (confirmationText === 'RESTORE' && backupToRestore) {
      // TODO: Implement restore logic via API
      console.log('Restore backup:', backupToRestore.fileName);
      setRestoreModalOpen(false);
      setBackupToRestore(null);
      setConfirmationText('');
    }
  };

  const hasChanges =
    settings &&
    localSettings.EventTimeZone !== settings.EventTimeZone;

  if (isLoading) {
    return (
      <Container size={1400} py="xl">
        <Group justify="center">
          <Loader size="lg" />
        </Group>
      </Container>
    );
  }

  if (error) {
    return (
      <Container size={1400} py="xl">
        <Alert icon={<IconAlertCircle />} title="Error" color="red">
          Failed to load settings. Please try again later.
        </Alert>
      </Container>
    );
  }

  return (
    <Container size={1400} py="xl">
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

      {/* Two-Column Grid Layout */}
      <Grid gutter="xl">
        {/* Left Column - Time Zone Settings Card */}
        <Grid.Col span={{ base: 12, md: 6 }}>
          <Box
            style={{
              background: 'var(--color-ivory)',
              borderRadius: '16px',
              boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
              overflow: 'hidden',
              height: '100%',
            }}
          >
            {/* Card Header */}
            <Box
              style={{
                background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
                padding: 'var(--space-lg) var(--space-xl)',
                borderBottom: '1px solid var(--color-taupe)',
              }}
            >
              <Group gap="sm">
                <IconClock size={24} color="var(--color-ivory)" />
                <Title
                  order={3}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    fontSize: '20px',
                    fontWeight: 700,
                    color: 'var(--color-ivory)',
                  }}
                >
                  Time Zone Settings
                </Title>
              </Group>
            </Box>

            {/* Card Body */}
            <Box style={{ padding: 'var(--space-xl)' }}>
              <Stack gap="lg">
                {/* Event Timezone Setting */}
                <Box>
                  <Text
                    component="label"
                    style={{
                      display: 'block',
                      fontFamily: 'var(--font-heading)',
                      fontSize: '14px',
                      fontWeight: 600,
                      color: 'var(--color-smoke)',
                      marginBottom: 'var(--space-xs)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    Event Timezone
                  </Text>
                  <Select
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
                      input: {
                        fontFamily: 'var(--font-body)',
                        fontSize: '16px',
                        border: '2px solid var(--color-taupe)',
                        borderRadius: '8px',
                        background: 'var(--color-ivory)',
                        color: 'var(--color-charcoal)',
                        padding: 'var(--space-sm) var(--space-md)',
                        '&:focus': {
                          borderColor: 'var(--color-burgundy)',
                          boxShadow: '0 0 0 3px rgba(136, 1, 36, 0.1)',
                        },
                      },
                    }}
                  />
                  <Text
                    size="sm"
                    style={{
                      fontSize: '12px',
                      color: 'var(--color-smoke)',
                      marginTop: 'var(--space-xs)',
                    }}
                  >
                    The timezone where your events occur. All event times will be interpreted in this timezone.
                  </Text>
                </Box>

                {/* Action Buttons */}
                <Group justify="flex-end" mt="md">
                  <Button
                    variant="filled"
                    color="#880124"
                    onClick={handleSave}
                    disabled={!hasChanges || updateMutation.isPending}
                    loading={updateMutation.isPending}
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
                    Save Settings
                  </Button>
                </Group>

                {/* Settings information */}
                <Box mt="md">
                  <Stack gap="md">
                    <Box>
                      <Text
                        fw={600}
                        size="sm"
                        style={{
                          fontFamily: 'var(--font-heading)',
                          fontSize: '14px',
                          color: 'var(--color-smoke)',
                          marginBottom: 'var(--space-xs)',
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }}
                      >
                        Timezone
                      </Text>
                      <Text
                        size="sm"
                        c="dimmed"
                        style={{
                          fontSize: '14px',
                          lineHeight: '1.6',
                          color: 'var(--color-smoke)',
                        }}
                      >
                        All events are assumed to occur in the selected timezone. Users in different
                        timezones will see times converted to their local timezone when viewing events.
                      </Text>
                    </Box>

                    <Box>
                      <Text
                        fw={600}
                        size="sm"
                        style={{
                          fontFamily: 'var(--font-heading)',
                          fontSize: '14px',
                          color: 'var(--color-smoke)',
                          marginBottom: 'var(--space-xs)',
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }}
                      >
                        Note
                      </Text>
                      <Text
                        size="sm"
                        c="dimmed"
                        style={{
                          fontSize: '14px',
                          lineHeight: '1.6',
                          color: 'var(--color-smoke)',
                        }}
                      >
                        Changes to this setting will affect how event times are displayed and interpreted.
                      </Text>
                    </Box>
                  </Stack>
                </Box>
              </Stack>
            </Box>
          </Box>
        </Grid.Col>

        {/* Right Column - Venue Management Card */}
        <Grid.Col span={{ base: 12, md: 6 }}>
          <VenueManagementCard />
        </Grid.Col>

        {/* Full Width - Database Backup Management Card */}
        <Grid.Col span={{ base: 12, md: 12 }}>
          <BackupManagementCard onRestoreClick={handleRestoreClick} />
        </Grid.Col>
      </Grid>

      {/* Restore Confirmation Modal */}
      <Modal
        opened={restoreModalOpen}
        onClose={() => setRestoreModalOpen(false)}
        title="Confirm Database Restore"
        size="md"
      >
        <Stack gap="md">
          <Alert color="red" icon={<IconAlertCircle />}>
            This will REPLACE your current database with the selected backup!
          </Alert>

          {backupToRestore && (
            <Box>
              <Text fw={600} mb="xs">Backup to restore:</Text>
              <Text size="sm" c="dimmed">
                {backupToRestore.displayName || backupToRestore.fileName}
              </Text>
              <Text size="sm" c="dimmed">
                {new Date(backupToRestore.timestamp).toLocaleString()}
              </Text>
              <Text size="sm" c="dimmed">
                Size: {backupToRestore.sizeFormatted}
              </Text>
            </Box>
          )}

          <Box>
            <Text mb="xs">
              Type <strong>RESTORE</strong> to confirm:
            </Text>
            <TextInput
              value={confirmationText}
              onChange={(e) => setConfirmationText(e.currentTarget.value)}
              placeholder="RESTORE"
            />
          </Box>

          <Group justify="flex-end">
            <Button
              variant="default"
              onClick={() => setRestoreModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              color="red"
              disabled={confirmationText !== 'RESTORE'}
              onClick={handleConfirmRestore}
            >
              Restore Database
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Container>
  );
};
