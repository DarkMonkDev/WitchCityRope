import React, { useState } from 'react';
import {
  Modal,
  Button,
  Stack,
  TextInput,
  Text,
  Group,
  ActionIcon,
  Table,
  Alert,
  NumberInput,
  Box,
  Badge,
  Tooltip,
  Select,
} from '@mantine/core';
import {
  IconLink,
  IconCopy,
  IconCheck,
  IconTrash,
  IconAlertCircle,
  IconClock,
  IconCalendar,
  IconExternalLink,
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

  const generateMutation = useGenerateSessionToken();
  const revokeMutation = useRevokeSessionToken();
  const { data: activeTokens, isLoading: isLoadingTokens } = useActiveSessionTokens(eventId, opened);
  const { data: sessionsData, isLoading: isLoadingSessions } = useEventSessions(eventId, opened);
  const eventTimeZone = useEventTimeZone();

  // Type-safe sessions (API returns SessionDto[] | undefined)
  const sessions = (sessionsData as SessionDto[] | undefined) || [];

  // Type-safe active tokens (API returns SessionTokenResponse[] | undefined)
  const tokens = (activeTokens as SessionTokenResponse[] | undefined) || [];

  // Type definition for session status
  type SessionStatus = 'available' | 'future' | 'past' | 'unavailable';

  interface SessionWithStatus extends SessionDto {
    status: SessionStatus;
    statusText: string;
  }

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
  const availableSessions = sessionsWithStatus.filter(s => s.status === 'available');

  // Auto-select all available sessions when modal opens
  React.useEffect(() => {
    if (opened && availableSessions.length > 0 && selectedSessionIds.length === 0) {
      setSelectedSessionIds(availableSessions.map(s => s.id));
    }
  }, [opened, availableSessions, selectedSessionIds.length]);

  // Toggle session selection
  const handleSessionToggle = (sessionId: string) => {
    setSelectedSessionIds(prev =>
      prev.includes(sessionId)
        ? prev.filter(id => id !== sessionId)
        : [...prev, sessionId]
    );
  };

  // Select/deselect all available sessions
  const handleSelectAllAvailable = () => {
    const allAvailableIds = availableSessions.map(s => s.id);
    const allSelected = allAvailableIds.every(id => selectedSessionIds.includes(id));

    if (allSelected) {
      // Deselect all
      setSelectedSessionIds([]);
    } else {
      // Select all available
      setSelectedSessionIds(allAvailableIds);
    }
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
      setGeneratedToken(result);

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
    onClose();
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Group gap="xs">
          <IconLink size={20} />
          <Text fw={600}>Generate Check-In Link</Text>
        </Group>
      }
      size="xl"
    >
      <Stack gap="lg">
        {/* Event Info */}
        <Alert variant="light" color="blue" icon={<IconAlertCircle />}>
          <Text size="sm" fw={600} mb={4}>
            Event: {eventTitle}
          </Text>
          <Text size="xs" c="dimmed">
            Generate a kiosk mode link for volunteer check-in staff. No login required on the check-in device.
          </Text>
        </Alert>

        {/* Generate New Token Section */}
        <Box>
          <Text fw={600} mb="md">
            Generate New Token
          </Text>
          <Stack gap="md">
            {/* Session Selection Table */}
            {sessionsWithStatus.length > 0 && (
              <Box>
                <Group justify="space-between" mb="xs">
                  <Text size="sm" fw={500}>Select Sessions</Text>
                  {availableSessions.length > 1 && (
                    <Button
                      variant="subtle"
                      size="xs"
                      onClick={handleSelectAllAvailable}
                    >
                      {availableSessions.every(s => selectedSessionIds.includes(s.id))
                        ? 'Deselect All'
                        : 'Select All Available'}
                    </Button>
                  )}
                </Group>
                <Table striped withTableBorder>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th style={{ width: 40 }}></Table.Th>
                      <Table.Th>Session</Table.Th>
                      <Table.Th>Date/Time</Table.Th>
                      <Table.Th>Status</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {sessionsWithStatus.map((session) => {
                      const isAvailable = session.status === 'available';
                      const isSelected = selectedSessionIds.includes(session.id);

                      return (
                        <Table.Tr
                          key={session.id}
                          style={{
                            opacity: isAvailable ? 1 : 0.6,
                            cursor: isAvailable ? 'pointer' : 'not-allowed'
                          }}
                          onClick={() => isAvailable && handleSessionToggle(session.id)}
                        >
                          <Table.Td>
                            <input
                              type="checkbox"
                              checked={isSelected}
                              disabled={!isAvailable}
                              onChange={() => handleSessionToggle(session.id)}
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

            <Group align="flex-end">
              <NumberInput
                label="Expires in (hours)"
                description="Token will automatically expire after this time"
                value={expiresInHours}
                onChange={(value) => setExpiresInHours(Number(value) || 12)}
                min={1}
                max={168} // 7 days max
                step={1}
                style={{ flex: 1 }}
                leftSection={<IconClock size={16} />}
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
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2',
                  },
                }}
                disabled={availableSessions.length > 0 && selectedSessionIds.length === 0}
              >
                Generate Link
              </Button>
            </Group>
          </Stack>
        </Box>

        {/* Generated Token Display */}
        {generatedToken && (
          <Alert variant="light" color="green" icon={<IconCheck />}>
            <Stack gap="sm">
              <Text size="sm" fw={600}>
                Link Generated Successfully
              </Text>
              {/* Show session names if available */}
              {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
              {((generatedToken as any)?.sessionNames && (generatedToken as any).sessionNames.length > 0) ? (
                <Text size="sm" c="dimmed">
                  {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                  <strong>Sessions:</strong> {(generatedToken as any).sessionNames.join(', ')}
                </Text>
              ) : (generatedToken as any)?.sessionName ? (
                <Text size="sm" c="dimmed">
                  {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                  <strong>Session:</strong> {(generatedToken as any).sessionName}
                </Text>
              ) : null}
              <TextInput
                label="Check-In URL"
                value={generatedToken.checkInUrl}
                readOnly
                rightSection={
                  <ActionIcon
                    variant="subtle"
                    color={copiedToken === generatedToken.token ? 'green' : 'blue'}
                    onClick={() => handleCopy(generatedToken.checkInUrl, generatedToken.token)}
                  >
                    {copiedToken === generatedToken.token ? <IconCheck size={16} /> : <IconCopy size={16} />}
                  </ActionIcon>
                }
              />
              <Button
                variant="outline"
                leftSection={<IconExternalLink size={16} />}
                onClick={() => window.open(generatedToken.checkInUrl, '_blank')}
                data-testid="open-checkin-link"
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
                Open
              </Button>
              <Group gap="md">
                <Text size="xs" c="dimmed">
                  Expires: {formatDate(generatedToken.expiresAt)}
                </Text>
                <Badge variant="light" color="green">
                  Active
                </Badge>
              </Group>
            </Stack>
          </Alert>
        )}

        {/* Active Tokens List */}
        <Box>
          <Text fw={600} mb="md">
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
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Token</Table.Th>
                  <Table.Th>Session</Table.Th>
                  <Table.Th>Created</Table.Th>
                  <Table.Th>Expires</Table.Th>
                  <Table.Th>Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {tokens.map((token) => (
                  <Table.Tr key={token.token}>
                    <Table.Td>
                      <Text size="sm" ff="monospace">
                        {formatTokenPrefix(token.token)}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                      {(token as any)?.sessionNames && (token as any).sessionNames.length > 0 ? (
                        <Text size="sm">
                          {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                          {(token as any).sessionNames.join(', ')}
                        </Text>
                      ) : (token as any)?.sessionName ? (
                        <Badge variant="light" color="blue">
                          {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                          {(token as any).sessionName}
                        </Badge>
                      ) : (
                        <Text size="sm" c="dimmed">All Sessions</Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {formatDate(token.createdAt)}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {formatDate(token.expiresAt)}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap="xs">
                        <Tooltip label="Copy URL">
                          <ActionIcon
                            variant="light"
                            color="blue"
                            onClick={() => handleCopy(token.checkInUrl, token.token)}
                          >
                            {copiedToken === token.token ? <IconCheck size={16} /> : <IconCopy size={16} />}
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Revoke Token">
                          <ActionIcon
                            variant="light"
                            color="red"
                            onClick={() => handleRevoke(token.token)}
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
