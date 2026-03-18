/**
 * TicketAssignmentRow
 *
 * A single row in the ticket assignment section showing:
 * - "Ticket N: Your ticket (you)" for the purchaser's ticket (row 1)
 * - "Ticket N: Assign to [dropdown]" for additional tickets
 *
 * Design reference: ui-design.md Screen 2 - Assignment Rows Design
 * - Ticket 1: Always "Your ticket" (not editable)
 * - Ticket 2-N: Select dropdown with authorized contacts + "Assign later"
 * - Contacts filtered by vetting/existing tickets via usePrincipalContacts(eventId)
 */

import React from 'react';
import { Group, Text, Select, Badge, Paper, Stack } from '@mantine/core';
import type { PrincipalContactDto } from '../../authorized-contacts/types/authorizedContact.types';

interface TicketAssignmentRowProps {
  /** Ticket number (1-indexed) */
  ticketNumber: number;
  /** Whether this is the purchaser's own ticket (ticket 1) */
  isPurchaserTicket: boolean;
  /** Available contacts for assignment dropdown */
  contacts: PrincipalContactDto[];
  /** Currently selected contact user ID, or null for "Assign later" */
  selectedContactId: string | null;
  /** Callback when assignment changes */
  onAssignmentChange: (contactUserId: string | null) => void;
  /** Whether the dropdown is disabled */
  disabled?: boolean;
  /** Error message for this row (e.g., race condition) */
  error?: string | null;
}

export const TicketAssignmentRow: React.FC<TicketAssignmentRowProps> = ({
  ticketNumber,
  isPurchaserTicket,
  contacts,
  selectedContactId,
  onAssignmentChange,
  disabled = false,
  error = null,
}) => {
  if (isPurchaserTicket) {
    return (
      <Paper
        p="sm"
        style={{
          background: 'rgba(136, 1, 36, 0.03)',
          border: '1px solid var(--mantine-color-gray-3)',
        }}
      >
        <Group gap="sm" align="center" wrap="nowrap">
          <Text size="sm" fw={600} style={{ flexShrink: 0 }}>
            Ticket {ticketNumber}
          </Text>
          <Group gap="xs" align="center">
            <Text size="sm">Your ticket</Text>
            <Badge color="green" variant="light" size="sm">
              you
            </Badge>
          </Group>
        </Group>
      </Paper>
    );
  }

  // Build dropdown data: contacts + "Assign later"
  const dropdownData = [
    ...contacts.map((contact) => ({
      value: contact.userId,
      label: contact.sceneName,
    })),
    { value: '__assign_later__', label: 'Assign later' },
  ];

  // Map null to the "Assign later" sentinel value for the Select component
  const selectValue = selectedContactId ?? '__assign_later__';

  const handleChange = (value: string | null) => {
    if (value === '__assign_later__' || value === null) {
      onAssignmentChange(null);
    } else {
      onAssignmentChange(value);
    }
  };

  return (
    <Paper
      p="sm"
      style={{
        background: 'rgba(136, 1, 36, 0.03)',
        border: '1px solid var(--mantine-color-gray-3)',
      }}
    >
      <Stack gap="xs">
        <Group gap="sm" align="center" wrap="nowrap">
          <Text size="sm" fw={600} style={{ flexShrink: 0 }}>
            Ticket {ticketNumber}
          </Text>
          <Select
            placeholder="Select person or assign later..."
            data={dropdownData}
            value={selectValue}
            onChange={handleChange}
            disabled={disabled || contacts.length === 0}
            style={{ flex: 1 }}
            size="sm"
            allowDeselect={false}
            aria-label={`Assign ticket ${ticketNumber}`}
          />
        </Group>

        {error && (
          <Text size="xs" c="red" ml="sm">
            {error}
          </Text>
        )}

        {contacts.length === 0 && !disabled && (
          <Text size="xs" c="dimmed" ml="sm">
            Add authorized contacts in Profile Settings to assign tickets to others.
          </Text>
        )}
      </Stack>
    </Paper>
  );
};

TicketAssignmentRow.displayName = 'TicketAssignmentRow';
