import React, { useState } from 'react';
import {
  Container,
  Title,
  Box,
  Modal,
  Text,
  Button,
  Stack,
  Alert,
  TextInput,
  Group,
  Progress,
  Loader,
  Checkbox,
} from '@mantine/core';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { BackupManagementCard } from '../../features/admin/backup/components/BackupManagementCard';
import { backupApi } from '../../features/admin/backup/api/backupApi';

interface Backup {
  fileName: string;
  timestamp: string;
  sizeBytes: number;
  sizeFormatted: string;
  displayName?: string;
}

/**
 * Admin Backup Page
 * Allows administrators to manage database backups:
 * - Create manual backups
 * - Restore from existing backups
 * - Upload and restore local backup files
 * - Delete old backups
 * - View storage usage
 */
export const AdminBackupPage: React.FC = () => {
  const [restoreModalOpen, setRestoreModalOpen] = useState(false);
  const [backupToRestore, setBackupToRestore] = useState<Backup | null>(null);
  const [confirmationText, setConfirmationText] = useState('');
  const [isRestoring, setIsRestoring] = useState(false);
  const [restoreProgress, setRestoreProgress] = useState(0);
  const [restoreError, setRestoreError] = useState<string | null>(null);
  const [createPreBackup, setCreatePreBackup] = useState(true);

  const handleRestoreClick = (backup: Backup) => {
    setBackupToRestore(backup);
    setRestoreModalOpen(true);
    setConfirmationText('');
    setRestoreError(null);
    setCreatePreBackup(true);
  };

  const handleConfirmRestore = async () => {
    if (confirmationText !== 'RESTORE' || !backupToRestore) {
      return;
    }

    setIsRestoring(true);
    setRestoreError(null);
    setRestoreProgress(0);

    try {
      // Call restore API
      const response = await backupApi.restoreBackup({
        fileName: backupToRestore.fileName,
        confirmation: 'RESTORE',
        createPreBackup: createPreBackup,
      });

      // Progress simulation (40% while job starts)
      const progressInterval = setInterval(() => {
        setRestoreProgress(prev => {
          const increment = Math.random() * 3;
          return Math.min(prev + increment, 95);
        });
      }, 1000);

      // Poll for job completion
      const pollInterval = setInterval(async () => {
        try {
          const status = await backupApi.getJobStatus(response.jobId);

          if (status.status === 'succeeded') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setRestoreProgress(100);

            setTimeout(() => {
              setIsRestoring(false);
              setRestoreModalOpen(false);
              setBackupToRestore(null);
              setConfirmationText('');
              alert('Database restored successfully! The page will reload.');
              window.location.reload();
            }, 500);
          } else if (status.status === 'failed') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setIsRestoring(false);
            setRestoreError(status.error || 'Restore failed');
          }
        } catch {
          // Continue polling on error
        }
      }, 2000);

      // Timeout after 10 minutes
      setTimeout(() => {
        clearInterval(pollInterval);
        clearInterval(progressInterval);
        if (isRestoring) {
          setIsRestoring(false);
          setRestoreError('Restore timeout - check Hangfire dashboard');
        }
      }, 600000);

    } catch (err) {
      setIsRestoring(false);
      setRestoreError(err instanceof Error ? err.message : 'Failed to start restore');
    }
  };

  return (
    <Container size={1400} py="xl">
      {/* Page Header */}
      <Box mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-burgundy)',
            marginBottom: '0.5rem',
          }}
        >
          Database Backup Management
        </Title>
        <Text c="dimmed">
          Manage database backups and restores for WitchCityRope
        </Text>
      </Box>

      {/* Backup Management Card */}
      <BackupManagementCard onRestoreClick={handleRestoreClick} />

      {/* Restore Confirmation Modal */}
      <Modal
        opened={restoreModalOpen}
        onClose={() => !isRestoring && setRestoreModalOpen(false)}
        title="Confirm Database Restore"
        size="md"
        closeOnClickOutside={!isRestoring}
        closeOnEscape={!isRestoring}
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

          {restoreError && (
            <Alert color="red" icon={<IconAlertCircle />}>
              {restoreError}
            </Alert>
          )}

          {isRestoring ? (
            <Box>
              <Group gap="xs" mb="xs">
                <Loader size="sm" />
                <Text size="sm" c="dimmed">
                  Restoring database... {Math.round(restoreProgress)}%
                </Text>
              </Group>
              <Progress value={restoreProgress} size="sm" />
            </Box>
          ) : (
            <>
              <Checkbox
                label="Create pre-restore backup (recommended)"
                checked={createPreBackup}
                onChange={(e) => setCreatePreBackup(e.currentTarget.checked)}
              />

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
            </>
          )}

          <Group justify="flex-end">
            <Button
              variant="default"
              onClick={() => setRestoreModalOpen(false)}
              disabled={isRestoring}
            >
              Cancel
            </Button>
            <Button
              color="red"
              disabled={confirmationText !== 'RESTORE' || isRestoring}
              onClick={handleConfirmRestore}
              loading={isRestoring}
            >
              Restore Database
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Container>
  );
};
