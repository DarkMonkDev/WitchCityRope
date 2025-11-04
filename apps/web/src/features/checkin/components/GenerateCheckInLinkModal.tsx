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
} from '@mantine/core';
import {
  IconLink,
  IconCopy,
  IconCheck,
  IconTrash,
  IconAlertCircle,
  IconClock,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import {
  useGenerateSessionToken,
  useRevokeSessionToken,
  useActiveSessionTokens,
} from '../hooks/useSessionTokens';
import type { SessionTokenResponse } from '../api/sessionTokenApi';

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

  const generateMutation = useGenerateSessionToken();
  const revokeMutation = useRevokeSessionToken();
  const { data: activeTokens, isLoading: isLoadingTokens } = useActiveSessionTokens(eventId, opened);

  // Type-safe active tokens (API returns SessionTokenResponse[] | undefined)
  const tokens = (activeTokens as SessionTokenResponse[] | undefined) || [];

  const handleGenerate = async () => {
    try {
      const result = await generateMutation.mutateAsync({ eventId, expirationHours: expiresInHours });
      setGeneratedToken(result);

      notifications.show({
        title: 'Check-In Link Generated',
        message: 'Session token created successfully. Copy the link below.',
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
    });
  };

  const formatTokenPrefix = (token: string) => {
    return token.substring(0, 8) + '...';
  };

  const handleClose = () => {
    setGeneratedToken(null);
    setCopiedToken(null);
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
            >
              Generate Link
            </Button>
          </Group>
        </Box>

        {/* Generated Token Display */}
        {generatedToken && (
          <Alert variant="light" color="green" icon={<IconCheck />}>
            <Stack gap="sm">
              <Text size="sm" fw={600}>
                Link Generated Successfully
              </Text>
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
