import React, { useState } from 'react';
import {
  Modal,
  Button,
  Stack,
  Text,
  Group,
  ActionIcon,
  Table,
  Alert,
  NumberInput,
  Box,
  Badge,
  Tooltip,
  Checkbox,
} from '@mantine/core';
import {
  IconCopy,
  IconCheck,
  IconTrash,
  IconAlertCircle,
  IconExternalLink,
  IconLink,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import {
  useGenerateSessionToken,
  useRevokeSessionToken,
  useActiveSessionTokens,
} from '../hooks/useSessionTokens';
import type { SessionTokenResponse } from '../api/sessionTokenApi';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';
import { useEventSessions } from '../../../lib/api/hooks/useEventSessions';
import type { SessionDto } from '../../../lib/api/services/eventSessions';

interface GenerateCheckInLinkModalProps {
  opened: boolean;
  onClose: () => void;
  eventId: string;
  eventTitle: string;
}

/**
 * Modal for generating and managing kiosk mode check-in links
 *
 * Features:
 * - Generate new session tokens with configurable expiration
 * - Copy check-in URL to clipboard
 * - View list of active tokens
 * - Revoke existing tokens
 *
 * Auth: Requires Administrator or EventOrganizer role
 */
export const GenerateCheckInLinkModal: React.FC<GenerateCheckInLinkModalProps> = ({
  opened,
  onClose,
  eventId,
  eventTitle,
}) => {
  const [generatedToken, setGeneratedToken] = useState<SessionTokenResponse | null>(null);
  const [expiresInHours, setExpiresInHours] = useState<number>(12);
  const [copiedToken, setCopiedToken] = useState<string | null>(null);
  const [selectedSessionIds, setSelectedSessionIds] = useState<string[]>([]);
  const [hasUserInteracted, setHasUserInteracted] = useState(false);

  const generateMutation = useGenerateSessionToken();
  const revokeMutation = useRevokeSessionToken();
  const { data: activeTokens, isLoading: isLoadingTokens } = useActiveSessionTokens(eventId, opened);
  const { data: sessionsData, isLoading: isLoadingSessions } = useEventSessions(eventId, opened);
  const eventTimeZone = useEventTimeZone();

  // Type-safe sessions (API returns SessionDto[] | undefined)
  const sessions = (sessionsData as SessionDto[] | undefined) || [];

  // Type-safe active tokens (API returns SessionTokenResponse[] | undefined)
  const tokens = (activeTokens as SessionTokenResponse[] | undefined) || [];

  // Calculate status for each session
  const sessionsWithStatus = React.useMemo(() => {
    if (sessions.length === 0) return [];

    const now = new Date();
    const twelveHoursInMs = 12 * 60 * 60 * 1000;

    return sessions.map((session) => {
      if (!session.startTime) {
        return { ...session, status: 'unavailable' as const, statusText: 'No start time' };
      }

      const sessionStart = new Date(session.startTime);
      const timeDiff = sessionStart.getTime() - now.getTime();
      const absTimeDiff = Math.abs(timeDiff);

      if (absTimeDiff <= twelveHoursInMs) {
        // Within ±12 hours - available
        return { ...session, status: 'available' as const, statusText: 'Available' };
      } else if (timeDiff > twelveHoursInMs) {
        // More than 12 hours in the future
        const hoursUntilAvailable = Math.ceil((timeDiff - twelveHoursInMs) / (60 * 60 * 1000));
        const statusText = hoursUntilAvailable > 24
          ? `Opens in ${Math.ceil(hoursUntilAvailable / 24)} days`
          : `Opens in ${hoursUntilAvailable} hours`;
        return { ...session, status: 'future' as const, statusText };
      } else {
        // More than 12 hours in the past
        return { ...session, status: 'past' as const, statusText: 'Past check-in window' };
      }
    }).sort((a, b) => {
      // Sort: available first, then future, then past
      const statusOrder = { available: 0, future: 1, past: 2, unavailable: 3 };
      return statusOrder[a.status] - statusOrder[b.status];
    });
  }, [sessions]);

  // Get only available sessions for validation
  // Memoize available sessions to prevent infinite re-renders
  const availableSessions = React.useMemo(
    () => sessionsWithStatus.filter(s => s.status === 'available'),
    [sessionsWithStatus]
  );

  // Get stable list of available session IDs
  const availableSessionIds = React.useMemo(
    () => availableSessions.map(s => s.id).filter((id): id is string => !!id),
    [availableSessions]
  );

  // Auto-select all available sessions when modal opens (only on initial open)
  React.useEffect(() => {
    if (opened && availableSessionIds.length > 0 && !hasUserInteracted) {
      setSelectedSessionIds(availableSessionIds);
    }
  }, [opened, availableSessionIds, hasUserInteracted]);

  // Toggle session selection
  const handleSessionToggle = (sessionId: string) => {
    setHasUserInteracted(true);
    setSelectedSessionIds(prev =>
      prev.includes(sessionId)
        ? prev.filter(id => id !== sessionId)
        : [...prev, sessionId]
    );
  };

  const handleGenerate = async () => {
    // Validate at least one session is selected
    if (availableSessions.length > 0 && selectedSessionIds.length === 0) {
      notifications.show({
        title: 'Session Required',
        message: 'Please select at least one session for this check-in token.',
        color: 'orange',
        icon: <IconAlertCircle />,
      });
      return;
    }

    try {
      const result = await generateMutation.mutateAsync({
        eventId,
        sessionIds: selectedSessionIds.length > 0 ? selectedSessionIds : undefined,
        expirationHours: expiresInHours
      });
      setGeneratedToken(result as SessionTokenResponse);

      // Get session names for success message
      const sessionCount = selectedSessionIds.length;
      const sessionText = sessionCount === 1
        ? sessionsWithStatus.find(s => s.id === selectedSessionIds[0])?.name || 'Session'
        : `${sessionCount} sessions`;

      notifications.show({
        title: 'Check-In Link Generated',
        message: `Session token created successfully for ${sessionText}. Copy the link below.`,
        color: 'green',
        icon: <IconCheck />,
      });
    } catch (error) {
      notifications.show({
        title: 'Generation Failed',
        message: error instanceof Error ? error.message : 'Failed to generate check-in token',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    }
  };

  const handleCopy = async (url: string, token: string) => {
    try {
      await navigator.clipboard.writeText(url);
      setCopiedToken(token);

      notifications.show({
        title: 'Copied to Clipboard',
        message: 'Check-in URL copied successfully',
        color: 'green',
        icon: <IconCheck />,
        autoClose: 2000,
      });

      // Reset copied state after 2 seconds
      setTimeout(() => setCopiedToken(null), 2000);
    } catch (error) {
      notifications.show({
        title: 'Copy Failed',
        message: 'Failed to copy URL to clipboard',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    }
  };

  const handleRevoke = async (token: string) => {
    try {
      await revokeMutation.mutateAsync({ token });

      // Clear generated token if it was the one revoked
      if (generatedToken?.token === token) {
        setGeneratedToken(null);
      }

      notifications.show({
        title: 'Token Revoked',
        message: 'Session token revoked successfully',
        color: 'green',
        icon: <IconCheck />,
      });
    } catch (error) {
      notifications.show({
        title: 'Revoke Failed',
        message: error instanceof Error ? error.message : 'Failed to revoke token',
        color: 'red',
        icon: <IconAlertCircle />,
      });
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      timeZone: eventTimeZone,
    });
  };

  const formatTokenPrefix = (token: string) => {
    return token.substring(0, 8) + '...';
  };

  const handleClose = () => {
    setGeneratedToken(null);
    setCopiedToken(null);
    setSelectedSessionIds([]);
    setHasUserInteracted(false);
    onClose();
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Text size="xl" fw={600}>Generate Check-In Link for {eventTitle}</Text>
      }
      size="xl"
      padding="xl"
      styles={{
        header: { paddingBottom: 18 },
      }}
    >
      <Stack gap="md">
        {/* Session Selection Table */}
        {sessionsWithStatus.length > 0 && (
          <Box>
            <Text fw={600} style={{ marginBottom: 0, paddingBottom: 0 }}>Select Sessions</Text>
            <Table striped withTableBorder>
              <Table.Thead style={{ backgroundColor: 'var(--mantine-color-wcr-7)' }}>
                <Table.Tr>
                  <Table.Th style={{ width: 40, backgroundColor: 'var(--mantine-color-wcr-7)' }}></Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Session</Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Date/Time</Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Status</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {sessionsWithStatus.map((session) => {
                  const isAvailable = session.status === 'available';
                  const sessionId = session.id ?? '';
                  const isSelected = selectedSessionIds.includes(sessionId);

                  return (
                    <Table.Tr
                      key={sessionId}
                      style={{
                        opacity: isAvailable ? 1 : 0.6,
                        cursor: isAvailable ? 'pointer' : 'not-allowed',
                        backgroundColor: isSelected ? 'var(--mantine-color-blue-1)' : undefined,
                      }}
                      onClick={() => isAvailable && handleSessionToggle(sessionId)}
                    >
                      <Table.Td>
                        <Checkbox
                          checked={isSelected}
                          disabled={!isAvailable}
                          onChange={() => handleSessionToggle(sessionId)}
                          onClick={(e) => e.stopPropagation()}
                          style={{ cursor: isAvailable ? 'pointer' : 'not-allowed' }}
                        />
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" fw={isAvailable ? 500 : 400}>
                          {session.name || 'Unnamed Session'}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">
                          {session.startTime ? formatDate(session.startTime) : 'No date'}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <Badge
                          variant="light"
                          color={
                            session.status === 'available' ? 'green' :
                            session.status === 'future' ? 'orange' :
                            'gray'
                          }
                        >
                          {session.statusText}
                        </Badge>
                      </Table.Td>
                    </Table.Tr>
                  );
                })}
              </Table.Tbody>
            </Table>
          </Box>
        )}

        {/* No sessions at all */}
        {!isLoadingSessions && sessions.length === 0 && (
          <Alert variant="light" color="yellow" icon={<IconAlertCircle />}>
            <Text size="sm">
              This event has no sessions configured. The token will apply to the entire event.
            </Text>
          </Alert>
        )}

        {/* All sessions outside window */}
        {!isLoadingSessions && sessions.length > 0 && availableSessions.length === 0 && (
          <Alert variant="light" color="yellow" icon={<IconAlertCircle />}>
            <Text size="sm">
              No sessions are currently within the check-in window (±12 hours from session start).
              See the table above for when each session becomes available.
            </Text>
          </Alert>
        )}

        {/* Token Generation Controls */}
        <Group align="center" justify="flex-end" mt={8}>
          <Text fw={600}>Expires in</Text>
          <NumberInput
            value={expiresInHours}
            onChange={(value) => setExpiresInHours(Number(value) || 12)}
            min={1}
            max={168} // 7 days max
            step={1}
            style={{ width: 80 }}
            rightSection={<Text size="sm" c="dimmed" pr="xs">hours</Text>}
            rightSectionWidth={50}
          />
          <Button
            onClick={handleGenerate}
            loading={generateMutation.isPending}
            leftSection={<IconLink size={16} />}
            variant="filled"
            color="blue"
            styles={{
              root: {
                fontWeight: 600,
                height: '36px',
                fontSize: '14px',
              },
            }}
            disabled={availableSessions.length > 0 && selectedSessionIds.length === 0}
          >
            Generate Link
          </Button>
        </Group>

        {/* Generated Token Display */}
        {generatedToken && (
          <Alert
            variant="light"
            color="green"
            icon={<IconCheck />}
            p="lg"
            mt="md"
            styles={{
              icon: { alignSelf: 'center' },
              wrapper: { alignItems: 'center' },
            }}
          >
            <Group justify="space-between" align="center" wrap="nowrap">
              <Stack gap={2}>
                <Text size="sm" fw={600}>
                  Link Generated Successfully
                </Text>
                <Text size="xs" c="dimmed">
                  Expires: {formatDate(generatedToken.expiresAt ?? '')}
                </Text>
              </Stack>
              <Group gap="sm">
                <Button
                  variant="outline"
                  size="xs"
                  leftSection={copiedToken === generatedToken.token ? <IconCheck size={14} /> : <IconCopy size={14} />}
                  color={copiedToken === generatedToken.token ? 'green' : 'blue'}
                  onClick={() => handleCopy(generatedToken.checkInUrl ?? '', generatedToken.token ?? '')}
                >
                  Copy URL
                </Button>
                <Button
                  variant="outline"
                  size="xs"
                  leftSection={<IconExternalLink size={14} />}
                  onClick={() => window.open(generatedToken.checkInUrl, '_blank')}
                  data-testid="open-checkin-link"
                >
                  Open
                </Button>
              </Group>
            </Group>
          </Alert>
        )}

        {/* Active Tokens List */}
        <Box>
          <Text fw={600} style={{ marginBottom: 0, paddingBottom: 0 }}>
            Active Tokens
          </Text>

          {isLoadingTokens ? (
            <Text size="sm" c="dimmed">
              Loading active tokens...
            </Text>
          ) : tokens.length === 0 ? (
            <Alert variant="light" color="gray">
              <Text size="sm" c="dimmed">
                No active tokens for this event. Generate one above to get started.
              </Text>
            </Alert>
          ) : (
            <Table striped highlightOnHover withTableBorder>
              <Table.Thead style={{ backgroundColor: 'var(--mantine-color-wcr-7)' }}>
                <Table.Tr>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Token</Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Session</Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Expires</Table.Th>
                  <Table.Th style={{ color: 'white', fontWeight: 600, backgroundColor: 'var(--mantine-color-wcr-7)' }}>Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {tokens.map((token) => (
                  <Table.Tr key={token.token}>
                    <Table.Td>
                      <Text size="sm" ff="monospace">
                        {formatTokenPrefix(token.token ?? '')}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      {token.sessionNames && token.sessionNames.length > 0 ? (
                        <Text size="sm">
                          {token.sessionNames.join(', ')}
                        </Text>
                      ) : token.sessionName ? (
                        <Badge variant="light" color="blue">
                          {token.sessionName}
                        </Badge>
                      ) : (
                        <Text size="sm" c="dimmed">All Sessions</Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {formatDate(token.expiresAt ?? '')}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap="xs">
                        <Tooltip label="Copy URL">
                          <ActionIcon
                            variant="light"
                            color="blue"
                            onClick={() => handleCopy(token.checkInUrl ?? '', token.token ?? '')}
                          >
                            {copiedToken === token.token ? <IconCheck size={16} /> : <IconCopy size={16} />}
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Revoke Token">
                          <ActionIcon
                            variant="light"
                            color="red"
                            onClick={() => handleRevoke(token.token ?? '')}
                            loading={revokeMutation.isPending}
                          >
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Box>
      </Stack>
    </Modal>
  );
};
