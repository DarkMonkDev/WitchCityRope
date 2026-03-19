/**
 * AssignTicketDropdown
 *
 * Reusable dropdown/select component for choosing an authorized contact
 * to assign a ticket to. Uses the authorized-contacts feature's
 * usePrincipalContacts hook for data.
 *
 * Design reference: ui-design.md Screen 5 - Assign/Reassign dropdown
 *
 * Used in:
 * - Dashboard: assign unassigned tickets or reassign declined tickets
 * - Checkout: assign tickets during purchase (future)
 */

import React, { useState } from 'react'
import {
  Modal,
  Stack,
  Select,
  Button,
  Group,
  Text,
  Title,
  Loader,
  Center,
  Alert,
} from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { usePrincipalContacts } from '../../authorized-contacts/api/queries'

interface AssignTicketDropdownProps {
  /** Whether the modal is open */
  opened: boolean
  /** Callback to close the modal */
  onClose: () => void
  /** Event ID for filtering eligible contacts */
  eventId: string
  /** Callback when a contact is selected and confirmed */
  onAssign: (userId: string) => void
  /** Whether the assign action is in progress */
  isAssigning?: boolean
  /** Title for the modal - "Assign Ticket" or "Reassign Ticket" */
  title?: string
  /** Ticket type name for display context */
  ticketTypeName?: string
  /** Event title for display context */
  eventTitle?: string
}

export const AssignTicketDropdown: React.FC<AssignTicketDropdownProps> = ({
  opened,
  onClose,
  eventId,
  onAssign,
  isAssigning = false,
  title = 'Assign Ticket',
  ticketTypeName,
  eventTitle,
}) => {
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null)

  // Fetch eligible contacts for this event (filtered by vetting + existing tickets)
  const { data: contacts, isLoading, error } = usePrincipalContacts(eventId)

  // Transform contacts into Select data format
  const selectData = (contacts || []).map((contact) => ({
    value: contact.userId ?? '',
    label: contact.sceneName ?? '',
  }))

  const handleConfirm = () => {
    if (selectedUserId) {
      onAssign(selectedUserId)
    }
  }

  const handleClose = () => {
    if (!isAssigning) {
      setSelectedUserId(null)
      onClose()
    }
  }

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Title order={3} style={{ color: '#880124' }}>
          {title}
        </Title>
      }
      centered
      size="md"
      // Prevent modal from closing on outside clicks — the searchable Select dropdown
      // can cause unintended modal dismissal. Users close via X button or Escape key.
      closeOnClickOutside={false}
      data-testid="assign-ticket-modal"
    >
      <Stack gap="md">
        {/* Context information */}
        {(eventTitle || ticketTypeName) && (
          <Stack gap="xs">
            {eventTitle && (
              <Text size="sm">
                Event: <Text component="span" fw={600}>{eventTitle}</Text>
              </Text>
            )}
            {ticketTypeName && (
              <Text size="sm">
                Ticket: <Text component="span" fw={600}>{ticketTypeName}</Text>
              </Text>
            )}
          </Stack>
        )}

        {/* Loading state */}
        {isLoading && (
          <Center py="md">
            <Loader size="sm" color="burgundy" />
          </Center>
        )}

        {/* Error state */}
        {error && (
          <Alert icon={<IconAlertCircle size={16} />} color="red" variant="light">
            Failed to load contacts. Please try again.
          </Alert>
        )}

        {/* No eligible contacts */}
        {!isLoading && !error && selectData.length === 0 && (
          <Alert icon={<IconAlertCircle size={16} />} color="yellow" variant="light">
            <Text size="sm">
              No eligible contacts found. Your authorized contacts may already
              have tickets for this event, or they may not meet the event's
              vetting requirements.
            </Text>
          </Alert>
        )}

        {/* Contact selector */}
        {!isLoading && !error && selectData.length > 0 && (
          <>
            <Select
              label="Select a person"
              placeholder="Choose an authorized contact..."
              data={selectData}
              value={selectedUserId}
              onChange={setSelectedUserId}
              searchable
              clearable
              data-testid="assign-contact-select"
            />
            <Text size="xs" c="dimmed">
              Only people who have authorized you as their delegate appear here.
            </Text>
          </>
        )}

        {/* Action buttons */}
        <Group justify="flex-end" gap="sm" mt="md">
          <Button
            variant="subtle"
            color="gray"
            onClick={handleClose}
            disabled={isAssigning}
            style={{
              minHeight: 40,
              height: 'auto',
              padding: '10px 20px',
              lineHeight: 1.4,
            }}
          >
            Cancel
          </Button>
          <Button
            color="burgundy"
            onClick={handleConfirm}
            loading={isAssigning}
            disabled={!selectedUserId || isAssigning}
            data-testid="assign-confirm-button"
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                transition: 'all 0.3s ease',
                height: 'auto',
                minHeight: 40,
                paddingTop: '10px',
                paddingBottom: '10px',
                paddingLeft: '20px',
                paddingRight: '20px',
                lineHeight: '1.2',
              },
            }}
          >
            {title}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

AssignTicketDropdown.displayName = 'AssignTicketDropdown'
