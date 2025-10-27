import React from 'react';
import { Table, Text, Group, Badge } from '@mantine/core';
import { WCRButton } from '../ui';
import type { components } from '@witchcityrope/shared-types';

export interface EventTicketType {
  id: string;
  name: string;
  pricingType: components["schemas"]["PricingType"]; // Use generated type from backend
  sessionIdentifiers: string[]; // ['S1', 'S2', 'S3']
  price?: number; // For fixed price tickets
  minPrice?: number; // For sliding scale tickets
  maxPrice?: number; // For sliding scale tickets
  defaultPrice?: number; // Default/suggested price for sliding scale
  quantityAvailable?: number;
  quantitySold?: number; // Number of tickets sold
  salesEndDate?: string;
}

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

    if (ticketType.pricingType === 'Fixed' && ticketType.price !== undefined) {
      return formatPrice(ticketType.price);
    } else if (ticketType.pricingType === 'SlidingScale' && ticketType.minPrice !== undefined && ticketType.maxPrice !== undefined) {
      if (ticketType.minPrice === ticketType.maxPrice) {
        return formatPrice(ticketType.minPrice);
      }
      return `${formatPrice(ticketType.minPrice)} - ${formatPrice(ticketType.maxPrice)}`;
    }
    return 'N/A';
  };

  const formatSalesEndDate = (dateString?: string) => {
    if (!dateString) return 'No limit';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      month: 'short', 
      day: 'numeric' 
    });
  };

  const formatSessions = (sessionIdentifiers: string[]) => {
    if (sessionIdentifiers.length === 0) return 'None';
    return sessionIdentifiers.join(', ');
  };

  const getQuantityDisplay = (quantity?: number) => {
    if (quantity === undefined) return 'Unlimited';
    return quantity.toString();
  };

  return (
    <div>
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
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Sales End
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {ticketTypes.map((ticketType) => (
            <Table.Tr
              key={ticketType.id}
              onClick={() => onEditTicketType(ticketType.id)}
              style={{ cursor: 'pointer' }}
            >
              <Table.Td>
                <Text size="sm" fw={500}>
                  {ticketType.name}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Badge
                  variant="light"
                  color={ticketType.pricingType === 'Fixed' ? 'blue' : 'green'}
                  size="sm"
                >
                  {ticketType.pricingType === 'Fixed' ? 'Fixed Price' : 'Sliding Scale'}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Text size="sm" fw={500}>
                  {formatSessions(ticketType.sessionIdentifiers)}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {formatPriceRange(ticketType)}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Text size="sm">
                  {getQuantityDisplay(ticketType.quantityAvailable)}
                </Text>
              </Table.Td>
              <Table.Td style={{ textAlign: 'center' }}>
                <Text size="sm" fw={700}>
                  {ticketType.quantitySold ?? 0}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {formatSalesEndDate(ticketType.salesEndDate)}
                </Text>
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