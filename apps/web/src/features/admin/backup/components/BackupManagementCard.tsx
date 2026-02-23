import { useState, useEffect } from 'react';
import {
  Paper,
  Box,
  Group,
  Stack,
  Button,
  Table,
  Text,
  Title,
  Progress,
  Alert,
  Modal,
  Loader,
  TextInput,
} from '@mantine/core';
import { IconDatabase, IconAlertCircle, IconRefresh, IconUpload, IconDownload, IconTrash, IconRotateClockwise } from '@tabler/icons-react';
import { backupApi } from '../api/backupApi';
import type { components } from '@witchcityrope/shared-types';

type BackupListItem = components['schemas']['BackupListItem'];

interface Backup extends BackupListItem {
  displayName?: string;
}

interface BackupManagementCardProps {
  onRestoreClick: (backup: Backup) => void;
}

export function BackupManagementCard({ onRestoreClick }: BackupManagementCardProps) {
  // All state from source component - UNCHANGED
  const [backups, setBackups] = useState<Backup[]>([]);
  const [loading, setLoading] = useState(true);
  const [isBackingUp, setIsBackingUp] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [deletingFile, setDeletingFile] = useState<string | null>(null);
  const [downloadingFile, setDownloadingFile] = useState<string | null>(null);
  const [editingName, setEditingName] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [storageInfo, setStorageInfo] = useState<{
    totalSizeFormatted: string;
    percentUsed: number;
  } | null>(null);
  const [isRestoring, setIsRestoring] = useState(false);
  const [downloadModalOpen, setDownloadModalOpen] = useState(false);

  // All useEffect hooks - UNCHANGED
  useEffect(() => {
    loadBackups();
    loadStorageInfo();
  }, []);

  // All handler functions - UNCHANGED (just copied from source)
  const loadBackups = async () => {
    try {
      setLoading(true);
      const data = await backupApi.listBackups();

      // Load saved display names from localStorage
      const savedNames = localStorage.getItem('backupDisplayNames');
      const displayNames = savedNames ? JSON.parse(savedNames) : {};

      setBackups((data.backups ?? []).map(b => ({
        ...b,
        displayName: displayNames[(b.fileName ?? '')] || (b.fileName ?? '').replace(/\.(dump|sql)$/, '').replace('backup-', '')
      })));
    } catch (err) {
      console.error('Failed to load backups:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadStorageInfo = async () => {
    try {
      const data = await backupApi.getStorageSummary();
      setStorageInfo({
        totalSizeFormatted: data.totalSizeFormatted,
        percentUsed: data.percentUsed
      });
    } catch (err) {
      console.error('Failed to load storage info:', err);
    }
  };

  const handleBackupClick = async () => {
    setIsBackingUp(true);
    setError(null);
    setProgress(0);

    try {
      const response = await backupApi.triggerBackup();

      // Simulate progress
      const progressInterval = setInterval(() => {
        setProgress(prev => (prev >= 90 ? 90 : prev + 10));
      }, 500);

      // Poll for job completion
      const pollInterval = setInterval(async () => {
        try {
          const status = await backupApi.getJobStatus(response.jobId);

          if (status.status === 'succeeded') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setProgress(100);
            setIsBackingUp(false);
            await loadBackups();
            await loadStorageInfo();
          } else if (status.status === 'failed') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setIsBackingUp(false);
            setError(status.error || 'Backup failed');
          }
        } catch (err) {
          // Continue polling if there's an error
        }
      }, 2000);

      // Timeout after 5 minutes
      setTimeout(() => {
        clearInterval(pollInterval);
        clearInterval(progressInterval);
        if (isBackingUp) {
          setIsBackingUp(false);
          setError('Backup timeout - check Hangfire dashboard');
        }
      }, 300000);

    } catch (err) {
      setIsBackingUp(false);
      setError(err instanceof Error ? err.message : 'Failed to start backup');
    }
  };

  const handleDelete = async (fileName: string) => {
    if (!confirm(`Are you sure you want to delete this backup?\n\n${fileName}\n\nThis action cannot be undone.`)) {
      return;
    }

    setDeletingFile(fileName);
    try {
      await backupApi.deleteBackup(fileName);
      await loadBackups();
      await loadStorageInfo();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Failed to delete backup');
    } finally {
      setDeletingFile(null);
    }
  };

  const handleDownload = async (fileName: string, event?: React.MouseEvent) => {
    // Prevent event propagation that might cause loops
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }

    setDownloadingFile(fileName);
    setDownloadModalOpen(true);
    try {
      const blob = await backupApi.downloadBackup(fileName);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();

      // Cleanup after a delay to ensure download starts properly
      setTimeout(() => {
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        setDownloadingFile(null);
        setDownloadModalOpen(false);
      }, 1000); // Longer delay to ensure download dialog appears
    } catch (err) {
      setDownloadingFile(null);
      setDownloadModalOpen(false);
      alert(err instanceof Error ? err.message : 'Failed to download backup');
    }
  };

  const handleLocalFileRestore = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.name.endsWith('.dump') && !file.name.endsWith('.sql')) {
      alert('Please select a .dump or .sql file');
      event.target.value = '';
      return;
    }

    if (!confirm(`Are you sure you want to restore from this local file?\n\nFile: ${file.name}\n\nThis will REPLACE your current database. A pre-backup will be created automatically.`)) {
      event.target.value = '';
      return;
    }

    setIsRestoring(true);
    setError(null);
    setProgress(0);

    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('createPreBackup', 'true');

      // Use XMLHttpRequest to track upload progress
      const xhr = new XMLHttpRequest();

      // Track upload progress (0-40%)
      xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable) {
          const uploadProgress = Math.round((e.loaded / e.total) * 40); // 0-40%
          setProgress(uploadProgress);
        }
      });

      const uploadPromise = new Promise<any>((resolve, reject) => {
        xhr.addEventListener('load', () => {
          if (xhr.status >= 200 && xhr.status < 300) {
            try {
              resolve(JSON.parse(xhr.responseText));
            } catch {
              reject(new Error('Failed to parse response'));
            }
          } else {
            try {
              const errorData = JSON.parse(xhr.responseText);
              reject(new Error(errorData.error || 'Failed to upload and restore backup'));
            } catch {
              reject(new Error(`Upload failed with status ${xhr.status}`));
            }
          }
        });

        xhr.addEventListener('error', () => reject(new Error('Network error during upload')));
        xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')));

        xhr.open('POST', '/api/admin/backup/upload-and-restore');
        xhr.send(formData);
      });

      const data = await uploadPromise;

      // Upload complete, now show processing progress (40-100%)
      const progressInterval = setInterval(() => {
        setProgress(prev => {
          // Slower, more realistic progress: 40% -> 100% over ~30 seconds
          const increment = Math.random() * 2; // Random 0-2% increments
          const newProgress = Math.min(prev + increment, 98); // Cap at 98% until job actually completes
          return newProgress;
        });
      }, 1000); // Every second

      // Poll for job completion
      const pollInterval = setInterval(async () => {
        try {
          const status = await backupApi.getJobStatus(data.jobId);

          if (status.status === 'succeeded') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setProgress(100);

            // Show success for a moment before hiding
            setTimeout(() => {
              setIsRestoring(false);
              alert('Local file restored successfully!');
              loadBackups();
              loadStorageInfo();
            }, 500);
          } else if (status.status === 'failed') {
            clearInterval(pollInterval);
            clearInterval(progressInterval);
            setIsRestoring(false);
            setError(status.error || 'Restore failed');
          }
        } catch (err) {
          // Continue polling if there's an error
        }
      }, 2000);

      // Timeout after 10 minutes
      setTimeout(() => {
        clearInterval(pollInterval);
        clearInterval(progressInterval);
        if (isRestoring) {
          setIsRestoring(false);
          setError('Restore timeout - check Hangfire dashboard');
        }
      }, 600000);

    } catch (err) {
      setIsRestoring(false);
      setError(err instanceof Error ? err.message : 'Failed to upload and restore backup');
    } finally {
      // Reset the file input
      event.target.value = '';
    }
  };

  const handleNameClick = (backup: Backup) => {
    setEditingName(backup.fileName);
    setEditValue(backup.displayName || backup.fileName);
  };

  const handleNameBlur = () => {
    if (editingName) {
      // Update local state
      setBackups(prev => prev.map(b =>
        b.fileName === editingName
          ? { ...b, displayName: editValue }
          : b
      ));

      // Save to localStorage for persistence
      const savedNames = localStorage.getItem('backupDisplayNames');
      const displayNames = savedNames ? JSON.parse(savedNames) : {};
      displayNames[editingName] = editValue;
      localStorage.setItem('backupDisplayNames', JSON.stringify(displayNames));
    }
    setEditingName(null);
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
  };

  const formatDate = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

  const lastBackup = backups.length > 0 ? backups[0] : null;

  return (
    <>
      {/* Download Modal */}
      <Modal
        opened={downloadModalOpen}
        onClose={() => setDownloadModalOpen(false)}
        withCloseButton={false}
        centered
        size="md"
      >
        <Stack align="center" gap="md">
          <Loader size="xl" />
          <Title order={3}>Downloading Backup...</Title>
          <Text c="dimmed">{downloadingFile}</Text>
        </Stack>
      </Modal>

      {/* Main Card */}
      <Paper
        style={{
          background: 'var(--color-ivory)',
          borderRadius: '16px',
          boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
        }}
      >
        {/* Card Header with gradient */}
        <Box
          style={{
            background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
            padding: 'var(--space-lg) var(--space-xl)',
            borderRadius: '16px 16px 0 0',
          }}
        >
          <Group gap="sm" justify="space-between">
            <Group gap="sm">
              <IconDatabase size={24} color="var(--color-ivory)" />
              <Title
                order={3}
                style={{
                  fontFamily: 'var(--font-heading)',
                  color: 'var(--color-ivory)',
                  margin: 0,
                }}
              >
                Database Backup Manager
              </Title>
            </Group>
            {isBackingUp && (
              <Text style={{ color: 'var(--color-amber)', fontWeight: 600 }}>
                Processing
              </Text>
            )}
          </Group>
          {storageInfo && (
            <Text size="sm" style={{ color: 'var(--color-taupe)', marginTop: '0.5rem' }}>
              Space Used: {storageInfo.totalSizeFormatted} / 250 GB ({storageInfo.percentUsed.toFixed(2)}%)
            </Text>
          )}
        </Box>

        {/* Card Body */}
        <Box p="xl">
          <Text c="dimmed" mb="md">
            Create manual backups, restore from previous backups, and manage backup storage.
            All backups are stored securely in DigitalOcean Spaces.
          </Text>

          {/* Action Buttons Row */}
          <Group justify="space-between" mb="lg" pb="lg" style={{ borderBottom: '1px solid var(--color-dusty-rose)' }}>
            <Group gap="md">
              <Button
                onClick={handleBackupClick}
                disabled={isBackingUp}
                leftSection={<IconRefresh size={16} />}
                color="blue"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2'
                  }
                }}
              >
                {isBackingUp ? 'Backing Up...' : 'Backup Now'}
              </Button>
              {lastBackup && !isBackingUp && (
                <Text c="dimmed" size="sm">
                  Last backup: {formatTimestamp(lastBackup.timestamp)}
                </Text>
              )}
            </Group>

            <div>
              <input
                type="file"
                id="localFileInput"
                accept=".dump,.sql"
                onChange={handleLocalFileRestore}
                style={{ display: 'none' }}
              />
              <Button
                component="label"
                htmlFor="localFileInput"
                disabled={isRestoring || isBackingUp}
                leftSection={<IconUpload size={16} />}
                color="green"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2'
                  }
                }}
              >
                {isRestoring ? 'Restoring...' : 'Local File Restore'}
              </Button>
            </div>
          </Group>

          {/* Progress Bar */}
          {(isBackingUp || isRestoring) && (
            <Box mb="lg">
              <Group gap="xs" mb="xs">
                <Loader size="sm" />
                <Text size="sm" c="dimmed">
                  {isRestoring ? `Restoring from local file... ${progress}%` : `Creating backup... ${progress}%`}
                </Text>
              </Group>
              <Progress value={progress} size="sm" />
            </Box>
          )}

          {/* Error Message */}
          {error && (
            <Alert
              icon={<IconAlertCircle />}
              color="red"
              title="Error"
              mb="lg"
            >
              {error}
            </Alert>
          )}

          {/* Backups Table */}
          {loading ? (
            <Group justify="center" py="xl">
              <Loader size="lg" />
            </Group>
          ) : backups.length === 0 ? (
            <Box ta="center" py="xl">
              <Text c="dimmed">
                No backups yet. Click "Backup Now" to create your first backup.
              </Text>
            </Box>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Name</Table.Th>
                  <Table.Th>Date</Table.Th>
                  <Table.Th style={{ textAlign: 'center' }}>Size</Table.Th>
                  <Table.Th style={{ textAlign: 'center' }}>Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {backups.map((backup) => (
                  <Table.Tr key={backup.fileName}>
                    <Table.Td>
                      {editingName === backup.fileName ? (
                        <TextInput
                          value={editValue}
                          onChange={(e) => setEditValue(e.currentTarget.value)}
                          onBlur={handleNameBlur}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') handleNameBlur();
                            if (e.key === 'Escape') {
                              setEditingName(null);
                              setEditValue('');
                            }
                          }}
                          autoFocus
                          size="sm"
                        />
                      ) : (
                        <Text
                          onClick={() => handleNameClick(backup)}
                          style={{
                            cursor: 'pointer',
                            color: 'var(--color-burgundy)',
                          }}
                          title="Click to edit name"
                        >
                          {backup.displayName || backup.fileName}
                        </Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{formatDate(backup.timestamp)}</Text>
                    </Table.Td>
                    <Table.Td style={{ textAlign: 'center' }}>
                      <Text size="sm">{backup.sizeFormatted}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap="xs" justify="center">
                        <Button
                          onClick={() => onRestoreClick(backup)}
                          color="green"
                          size="xs"
                          leftSection={<IconRotateClockwise size={14} />}
                        >
                          Restore
                        </Button>
                        <Button
                          onClick={(e) => handleDownload(backup.fileName, e)}
                          disabled={downloadingFile === backup.fileName}
                          color="blue"
                          size="xs"
                          leftSection={<IconDownload size={14} />}
                        >
                          {downloadingFile === backup.fileName ? '⏳' : 'Download'}
                        </Button>
                        <Button
                          onClick={() => handleDelete(backup.fileName)}
                          disabled={deletingFile === backup.fileName}
                          color="red"
                          size="xs"
                          leftSection={<IconTrash size={14} />}
                        >
                          {deletingFile === backup.fileName ? '...' : 'Delete'}
                        </Button>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Box>
      </Paper>
    </>
  );
}
