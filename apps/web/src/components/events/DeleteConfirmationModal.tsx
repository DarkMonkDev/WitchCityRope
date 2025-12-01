import React from 'react';
import {
  Modal,
  Stack,
  Text,
  Button,
  Group,
  Title,
  Alert,
  List,
  Badge,
} from '@mantine/core';
import { IconAlertTriangle, IconAlertCircle } from '@tabler/icons-react';

export type DeletionState = 'canDelete' | 'ticketsSold' | 'onlySession' | 'cascadeBlocking';

interface DeleteConfirmationModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: () => Promise<void>;
  itemType: 'session' | 'ticketType';
  itemName: string;
  deletionState: DeletionState;
  rsvpCount?: number;
  ticketsSoldCount?: number;
  volunteerShifts?: string[];
  affectedTicketTypes?: Array<{ name: string; ticketsSold: number }>;
  isLoading?: boolean;
}

export const DeleteConfirmationModal: React.FC<DeleteConfirmationModalProps> = ({
  opened,
  onClose,
  onConfirm,
  itemType,
  itemName,
  deletionState,
  rsvpCount = 0,
  ticketsSoldCount = 0,
  volunteerShifts = [],
  affectedTicketTypes = [],
  isLoading = false,
}) => {
  const canDelete = deletionState === 'canDelete';
  const isBlocked =
    deletionState === 'ticketsSold' ||
    deletionState === 'onlySession' ||
    deletionState === 'cascadeBlocking';

  const handleSubmit = async () => {
    if (!canDelete) return;
    await onConfirm();
  };

  const handleClose = () => {
    if (!isLoading) {
      onClose();
    }
  };

  // Build title and icon based on deletion state
  const getTitleProps = () => {
    if (isBlocked) {
      return {
        icon: <IconAlertCircle size={24} />,
        color: '#DC143C', // Red for blocked
        text: `Cannot Delete ${itemType === 'session' ? 'Session' : 'Ticket Type'}`,
      };
    }
    return {
      icon: <IconAlertTriangle size={24} />,
      color: '#DAA520', // Yellow for warning
      text: `Delete ${itemType === 'session' ? 'Session' : 'Ticket Type'}?`,
    };
  };

  const titleProps = getTitleProps();

  // Build alert message based on deletion state
  const getAlertContent = () => {
    switch (deletionState) {
      case 'canDelete':
        return {
          color: 'yellow' as const,
          title: `This ${itemType === 'session' ? 'session' : 'ticket type'} has ${rsvpCount} RSVPs but no tickets sold`,
          variant: 'light' as const,
        };
      case 'ticketsSold':
        return {
          color: 'red' as const,
          title: `This ${itemType === 'session' ? 'session' : 'ticket type'} has ${ticketsSoldCount} tickets sold`,
          variant: 'filled' as const,
        };
      case 'onlySession':
        return {
          color: 'red' as const,
          title: 'Cannot delete the only session in an event',
          variant: 'filled' as const,
        };
      case 'cascadeBlocking':
        return {
          color: 'red' as const,
          title: 'This session is included in ticket types with sales',
          variant: 'filled' as const,
        };
      default:
        return {
          color: 'red' as const,
          title: 'Cannot delete',
          variant: 'filled' as const,
        };
    }
  };

  const alertContent = getAlertContent();

  // Build impact list based on deletion state
  const getImpactList = () => {
    if (!canDelete) {
      if (deletionState === 'onlySession') {
        return ['Delete the entire event instead'];
      }
      if (deletionState === 'ticketsSold') {
        return ['Cannot be deleted to protect member purchases'];
      }
      if (deletionState === 'cascadeBlocking' && affectedTicketTypes.length > 0) {
        return ['Affected ticket types:'];
      }
      return [];
    }

    // canDelete state - show what will happen
    const impacts: string[] = [];
    if (rsvpCount > 0) {
      impacts.push(`Cancel ${rsvpCount} RSVP${rsvpCount > 1 ? 's' : ''} and notify members by email`);
    }
    if (volunteerShifts.length > 0) {
      impacts.push(`Cancel ${volunteerShifts.length} volunteer shift${volunteerShifts.length > 1 ? 's' : ''}`);
      volunteerShifts.forEach((shift) => {
        impacts.push(`  • ${shift}`);
      });
    }
    return impacts;
  };

  const impactList = getImpactList();

  // Get button tooltip for disabled state
  const getButtonTooltip = () => {
    if (!isBlocked) return undefined;
    switch (deletionState) {
      case 'ticketsSold':
        return 'Cannot delete - tickets sold';
      case 'onlySession':
        return 'Cannot delete only session';
      case 'cascadeBlocking':
        return 'Cannot delete - dependent tickets sold';
      default:
        return 'Cannot delete';
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Group gap="xs">
          <span style={{ color: titleProps.color }}>{titleProps.icon}</span>
          <Title order={3} style={{ color: titleProps.color }}>
            {titleProps.text}
          </Title>
        </Group>
      }
      centered
      size="md"
      data-testid="delete-confirmation-modal"
    >
      <Stack gap="md">
        {/* Item Info */}
        <Text size="sm">
          {itemType === 'session' ? 'Session:' : 'Ticket Type:'}{' '}
          <Text component="span" fw={600}>
            "{itemName}"
          </Text>
        </Text>

        {/* Alert Box */}
        <Alert
          icon={isBlocked ? <IconAlertCircle size={20} /> : <IconAlertTriangle size={20} />}
          title={alertContent.title}
          color={alertContent.color}
          variant={alertContent.variant}
          styles={{
            root: {
              borderLeft: `4px solid ${isBlocked ? '#DC143C' : '#DAA520'}`,
            },
          }}
        >
          {/* Impact List or Blocking Reason */}
          {impactList.length > 0 && (
            <List size="sm" spacing="xs" withPadding>
              {impactList.map((item, index) => (
                <List.Item key={index}>
                  <Text size="sm">{item}</Text>
                </List.Item>
              ))}
            </List>
          )}

          {/* Show affected ticket types for cascadeBlocking */}
          {deletionState === 'cascadeBlocking' && affectedTicketTypes.length > 0 && (
            <Stack gap="xs" mt="xs">
              {affectedTicketTypes.map((ticketType, index) => (
                <Group key={index} justify="space-between">
                  <Text size="sm" fw={500}>
                    {ticketType.name}
                  </Text>
                  <Badge color="red" variant="filled" size="sm">
                    {ticketType.ticketsSold} sold
                  </Badge>
                </Group>
              ))}
            </Stack>
          )}

          {/* Additional context for blocked states */}
          {deletionState === 'onlySession' && (
            <Text size="sm" mt="xs">
              Delete the entire event instead
            </Text>
          )}

          {deletionState === 'ticketsSold' && (
            <Text size="sm" mt="xs">
              Cannot be deleted to protect member purchases
            </Text>
          )}
        </Alert>

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="md">
          <Button
            variant="light"
            onClick={handleClose}
            disabled={isLoading}
            data-testid="delete-cancel-button"
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
            Cancel
          </Button>
          <Button
            color="red"
            onClick={handleSubmit}
            loading={isLoading}
            disabled={isBlocked}
            title={getButtonTooltip()}
            data-testid="delete-confirm-button"
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
            Delete {itemType === 'session' ? 'Session' : 'Ticket Type'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};
