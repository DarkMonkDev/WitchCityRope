// SyncStatus component for CheckIn system
// Connection and synchronization status indicator

import React from 'react';
import {
  Group,
  Text,
  Badge,
  ActionIcon,
  Progress,
  Card,
  Stack,
  Alert,
  Button,
  Modal,
  ScrollArea,
  Divider
} from '@mantine/core';
import { 
  IconWifi, 
  IconWifiOff, 
  IconRefresh, 
  IconClock,
  IconAlertTriangle,
  IconCheck,
  IconX
} from '@tabler/icons-react';
import { useOfflineSync } from '../hooks/useOfflineSync';
import { TOUCH_TARGETS } from '../types/checkin.types';

interface SyncStatusProps {
  compact?: boolean;
  showDetails?: boolean;
  onSync?: () => void;
}

interface SyncDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSync: () => void;
  onClearQueue: () => void;
}

/**
 * Detailed sync information modal
 */
function SyncDetailsModal({ 
  isOpen, 
  onClose, 
  onSync, 
  onClearQueue 
}: SyncDetailsModalProps) {
  const { 
    isOnline, 
    pendingCount, 
    lastSync, 
    getSyncStatus 
  } = useOfflineSync();

  const [syncStatus, setSyncStatus] = React.useState<any>(null);

  React.useEffect(() => {
    if (isOpen) {
      getSyncStatus().then(setSyncStatus);
    }
  }, [isOpen, getSyncStatus]);

  return (
    <Modal
      opened={isOpen}
      onClose={onClose}
      title="Sync Status Details"
      size="md"
      styles={{
        title: { 
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 600
        }
      }}
    >
      <Stack gap="md">
        {/* Connection Status */}
        <Card padding="md" radius="md" style={{ backgroundColor: '#f8f9fa' }}>
          <Group align="center" gap="md">
            {isOnline ? (
              <>
                <IconWifi size={24} color="green" />
                <Stack gap="xs" style={{ flex: 1 }}>
                  <Text fw={500} c="green">Connected</Text>
                  <Text size="xs" c="dimmed">
                    All check-ins will sync immediately
                  </Text>
                </Stack>
              </>
            ) : (
              <>
                <IconWifiOff size={24} color="red" />
                <Stack gap="xs" style={{ flex: 1 }}>
                  <Text fw={500} c="red">Offline</Text>
                  <Text size="xs" c="dimmed">
                    Check-ins are being queued for sync
                  </Text>
                </Stack>
              </>
            )}
          </Group>
        </Card>

        {/* Sync Statistics */}
        <Card padding="md" radius="md">
          <Stack gap="sm">
            <Text fw={500} size="sm">Sync Information</Text>
            
            <Group justify="space-between">
              <Text size="sm">Pending Actions:</Text>
              <Badge 
                color={pendingCount > 0 ? "orange" : "green"}
                variant="filled"
              >
                {pendingCount}
              </Badge>
            </Group>

            <Group justify="space-between">
              <Text size="sm">Last Sync:</Text>
              <Text size="sm" c="dimmed">
                {lastSync ? new Date(lastSync).toLocaleString() : 'Never'}
              </Text>
            </Group>

            {syncStatus?.hasConflicts && (
              <Group justify="space-between">
                <Text size="sm">Conflicts:</Text>
                <Badge color="red" variant="filled">
                  {syncStatus.conflictCount}
                </Badge>
              </Group>
            )}
          </Stack>
        </Card>

        {/* Sync Actions */}
        <Stack gap="sm">
          <Button
            onClick={onSync}
            disabled={!isOnline || pendingCount === 0}
            leftSection={<IconRefresh size={16} />}
            fullWidth
            color="blue"
          >
            {pendingCount === 0 ? 'Nothing to Sync' : `Sync ${pendingCount} Actions`}
          </Button>

          {pendingCount > 0 && (
            <Button
              onClick={onClearQueue}
              variant="outline"
              color="red"
              fullWidth
              leftSection={<IconX size={16} />}
            >
              Clear Pending Queue
            </Button>
          )}
        </Stack>

        {/* Warnings */}
        {!isOnline && pendingCount > 0 && (
          <Alert color="yellow" variant="light">
            <Group align="flex-start" gap="xs">
              <IconAlertTriangle size={16} />
              <Stack gap="xs" style={{ flex: 1 }}>
                <Text size="sm" fw={500}>Offline Mode Active</Text>
                <Text size="xs">
                  {pendingCount} check-in actions are queued and will sync automatically 
                  when connection is restored.
                </Text>
              </Stack>
            </Group>
          </Alert>
        )}
      </Stack>
    </Modal>
  );
}

/**
 * Compact sync status indicator for headers/toolbars
 */
export function CompactSyncStatus({ onSync }: { onSync?: () => void }) {
  const { isOnline, pendingCount } = useOfflineSync();

  return (
    <Group align="center" gap="xs">
      {isOnline ? (
        <IconWifi size={16} color="green" />
      ) : (
        <IconWifiOff size={16} color="red" />
      )}
      
      {pendingCount > 0 && (
        <Badge size="sm" color="orange" variant="filled">
          {pendingCount}
        </Badge>
      )}

      {onSync && pendingCount > 0 && isOnline && (
        <ActionIcon
          size="sm"
          variant="subtle"
          onClick={onSync}
          aria-label="Sync pending actions"
        >
          <IconRefresh size={14} />
        </ActionIcon>
      )}
    </Group>
  );
}

/**
 * Main sync status component with full details
 */
export function SyncStatus({ 
  compact = false, 
  showDetails = false,
  onSync
}: SyncStatusProps) {
  const [showModal, setShowModal] = React.useState(false);
  const { 
    isOnline, 
    pendingCount, 
    lastSync, 
    triggerSync,
    clearPendingActions
  } = useOfflineSync();

  const handleSync = React.useCallback(() => {
    triggerSync();
    onSync?.();
  }, [triggerSync, onSync]);

  const handleClearQueue = React.useCallback(() => {
    clearPendingActions();
    setShowModal(false);
  }, [clearPendingActions]);

  if (compact) {
    return <CompactSyncStatus onSync={handleSync} />;
  }

  return (
    <>
      <Card 
        shadow="sm" 
        padding="md" 
        radius="md"
        style={{ cursor: showDetails ? 'pointer' : 'default' }}
        onClick={showDetails ? () => setShowModal(true) : undefined}
      >
        <Group justify="space-between" align="center">
          <Group align="center" gap="md">
            {/* Connection Icon */}
            {isOnline ? (
              <IconWifi size={20} color="green" />
            ) : (
              <IconWifiOff size={20} color="red" />
            )}

            {/* Status Info */}
            <Stack gap="xs">
              <Group align="center" gap="xs">
                <Text fw={500} size="sm">
                  {isOnline ? 'Online' : 'Offline'}
                </Text>
                
                {pendingCount > 0 && (
                  <Badge color="orange" variant="filled" size="sm">
                    {pendingCount} pending
                  </Badge>
                )}
              </Group>

              {lastSync && (
                <Text size="xs" c="dimmed">
                  Last sync: {new Date(lastSync).toLocaleString()}
                </Text>
              )}
            </Stack>
          </Group>

          {/* Actions */}
          <Group align="center" gap="xs">
            {pendingCount > 0 && isOnline && (
              <ActionIcon
                variant="subtle"
                onClick={(e) => {
                  e.stopPropagation();
                  handleSync();
                }}
                size="lg"
                aria-label="Sync pending actions"
                style={{
                  minWidth: TOUCH_TARGETS.MINIMUM,
                  minHeight: TOUCH_TARGETS.MINIMUM
                }}
              >
                <IconRefresh size={16} />
              </ActionIcon>
            )}

            {showDetails && (
              <ActionIcon
                variant="subtle"
                size="sm"
                aria-label="View sync details"
              >
                <IconClock size={14} />
              </ActionIcon>
            )}
          </Group>
        </Group>

        {/* Offline Warning */}
        {!isOnline && (
          <Alert 
            color="yellow" 
            variant="light" 
            mt="sm"
            styles={{
              root: { padding: '8px 12px' }
            }}
          >
            <Text size="xs">
              Operating in offline mode. Check-ins will sync when connection is restored.
            </Text>
          </Alert>
        )}
      </Card>

      {/* Details Modal */}
      <SyncDetailsModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        onSync={handleSync}
        onClearQueue={handleClearQueue}
      />
    </>
  );
}

/**
 * Sync progress indicator for active synchronization
 */
export function SyncProgress({ 
  isVisible, 
  progress, 
  message 
}: { 
  isVisible: boolean; 
  progress?: number; 
  message?: string;
}) {
  if (!isVisible) return null;

  return (
    <Card 
      padding="md" 
      radius="md"
      style={{
        position: 'fixed',
        top: '20px',
        left: '50%',
        transform: 'translateX(-50%)',
        zIndex: 1000,
        minWidth: '300px'
      }}
    >
      <Stack gap="sm">
        <Group align="center" gap="xs">
          <IconRefresh 
            size={16} 
            style={{ animation: 'spin 1s linear infinite' }} 
          />
          <Text size="sm" fw={500}>
            {message || 'Syncing...'}
          </Text>
        </Group>
        
        {progress !== undefined && (
          <Progress
            value={progress}
            color="blue"
            size="sm"
            radius="md"
          />
        )}
      </Stack>

      <style>{`
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
      `}</style>
    </Card>
  );
}