import React from 'react';
import { Table, Text, Group, Badge, ActionIcon } from '@mantine/core';
import { IconTrash } from '@tabler/icons-react';
import { WCRButton } from '../ui';
import type { components } from '@witchcityrope/shared-types';

// Use auto-generated TicketTypeDto from backend instead of manual interface
// This prevents field-dropping bugs where manual interfaces miss new backend fields
export type EventTicketType = components['schemas']['TicketTypeDto'];

interface EventTicketTypesGridProps {
  ticketTypes: EventTicketType[];
  onEditTicketType: (ticketTypeId: string) => void;
  onDeleteTicketType: (ticketTypeId: string) => void;
  onAddTicketType: () => void;
  hasSessions?: boolean; // Optional prop to control ticket type creation
}

export const EventTicketTypesGrid: React.FC<EventTicketTypesGridProps> = ({
  ticketTypes,
  onEditTicketType,
  onDeleteTicketType,
  onAddTicketType,
  hasSessions = true, // Default to true for backward compatibility
}) => {
  const formatPriceRange = (ticketType: EventTicketType) => {
    const formatPrice = (price: number) =>
      new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(price);

    if (ticketType.pricingType === 'Fixed' && ticketType.price != null) {
      return formatPrice(ticketType.price);
    } else if (ticketType.pricingType === 'SlidingScale' && ticketType.minPrice != null && ticketType.maxPrice != null) {
      if (ticketType.minPrice === ticketType.maxPrice) {
        return formatPrice(ticketType.minPrice);
      }
      return `${formatPrice(ticketType.minPrice)} - ${formatPrice(ticketType.maxPrice)}`;
    }
    return 'N/A';
  };

  const formatSessions = (sessionIdentifiers?: string[]) => {
    if (!sessionIdentifiers || sessionIdentifiers.length === 0) return 'None';
    return sessionIdentifiers.join(', ');
  };

  const getQuantityDisplay = (quantity?: number) => {
    if (quantity === undefined) return 'Unlimited';
    return quantity.toString();
  };

  return (
    <div data-testid="ticket-types-section">
      <Text size="sm" c="dimmed" mb="lg">
        Configure different ticket options for your event. Each ticket type can include multiple sessions.
        Click on a row to edit ticket details.
      </Text>

      <Table
        striped
        highlightOnHover
        withTableBorder
        className="wcr-data-table"
        style={{
          backgroundColor: 'white',
          borderRadius: '12px',
          overflow: 'hidden',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
        }}
      >
        <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
          <Table.Tr>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Ticket Name
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Type
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Session(s)
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Price
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Quantity
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Sold
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
              Actions
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {ticketTypes.map((ticketType) => (
            <Table.Tr
              key={ticketType.id}
              data-testid="tickettype-row"
            >
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm" fw={500}>
                  {ticketType.name}
                </Text>
              </Table.Td>
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ textAlign: 'center', cursor: 'pointer' }}
              >
                <Badge
                  variant="light"
                  color={ticketType.pricingType === 'Fixed' ? 'blue' : 'green'}
                  size="sm"
                >
                  {ticketType.pricingType === 'Fixed' ? 'Fixed Price' : 'Sliding Scale'}
                </Badge>
              </Table.Td>
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm" fw={500}>
                  {formatSessions(ticketType.sessionIdentifiers)}
                </Text>
              </Table.Td>
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ cursor: 'pointer' }}
              >
                <Text size="sm">
                  {formatPriceRange(ticketType)}
                </Text>
              </Table.Td>
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ textAlign: 'center', cursor: 'pointer' }}
              >
                <Text size="sm">
                  {getQuantityDisplay(ticketType.quantityAvailable)}
                </Text>
              </Table.Td>
              <Table.Td
                onClick={() => onEditTicketType(ticketType.id || '')}
                style={{ textAlign: 'center', cursor: 'pointer' }}
              >
                <Text size="sm" fw={700}>
                  {ticketType.quantitySold ?? 0}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <ActionIcon
                  data-testid="button-delete-tickettype"
                  variant="subtle"
                  color="gray"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteTicketType(ticketType.id || '');
                  }}
                  aria-label={`Delete ${ticketType.name}`}
                  styles={{
                    root: {
                      '&:hover': { color: '#DC143C' }
                    }
                  }}
                >
                  <IconTrash size={20} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
          {ticketTypes.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={7}>
                <Text ta="center" c="dimmed" py="xl">
                  No ticket types created yet. Click "Add Ticket Type" to get started.
                </Text>
              </Table.Td>
            </Table.Tr>
          )}
        </Table.Tbody>
      </Table>

      <Group mt="md">
        <WCRButton
          variant="secondary"
          size="lg"
          onClick={onAddTicketType}
          disabled={!hasSessions}
          title={!hasSessions ? "Add at least one session before creating ticket types" : ""}
        >
          Add Ticket Type
        </WCRButton>
      </Group>
    </div>
  );
};