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
} from '@mantine/core';
import { IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { BackupManagementCard } from '../../features/admin/backup/components/BackupManagementCard';

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
