import React from 'react';
import { Table, Button, Text, Group, ActionIcon, Badge } from '@mantine/core';
import { IconEdit, IconTrash } from '@tabler/icons-react';

export interface EventTicketType {
  id: string;
  name: string;
  type: 'Single' | 'Couples';
  sessionIdentifiers: string[]; // ['S1', 'S2', 'S3']
  minPrice: number;
  maxPrice: number;
  quantityAvailable?: number;
  salesEndDate?: string;
}

interface EventTicketTypesGridProps {
  ticketTypes: EventTicketType[];
  onEditTicketType: (ticketTypeId: string) => void;
  onDeleteTicketType: (ticketTypeId: string) => void;
  onAddTicketType: () => void;
}

export const EventTicketTypesGrid: React.FC<EventTicketTypesGridProps> = ({
  ticketTypes,
  onEditTicketType,
  onDeleteTicketType,
  onAddTicketType,
}) => {
  const handleDeleteClick = (ticketTypeId: string, ticketTypeName: string) => {
    if (window.confirm(`Are you sure you want to delete ticket type "${ticketTypeName}"?`)) {
      onDeleteTicketType(ticketTypeId);
    }
  };

  const formatPriceRange = (minPrice: number, maxPrice: number) => {
    const formatPrice = (price: number) => 
      new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0,
      }).format(price);

    if (minPrice === maxPrice) {
      return formatPrice(minPrice);
    }
    return `${formatPrice(minPrice)} - ${formatPrice(maxPrice)}`;
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
        Click Edit to modify complex settings.
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
              Edit
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Ticket Name
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Type
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Sessions Included
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Price Range
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Quantity
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Sales End
            </Table.Th>
            <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
              Delete
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {ticketTypes.map((ticketType) => (
            <Table.Tr key={ticketType.id}>
              <Table.Td>
                <Button
                  size="xs"
                  variant="subtle"
                  color="burgundy"
                  leftSection={<IconEdit size={14} />}
                  onClick={() => onEditTicketType(ticketType.id)}
                  style={{
                    backgroundColor: 'var(--mantine-color-gray-0)',
                    border: '1px solid var(--mantine-color-burgundy-3)',
                    color: 'var(--mantine-color-burgundy-6)',
                  }}
                >
                  Edit
                </Button>
              </Table.Td>
              <Table.Td>
                <Text size="sm" fw={500}>
                  {ticketType.name}
                </Text>
              </Table.Td>
              <Table.Td>
                <Badge
                  variant="light"
                  color={ticketType.type === 'Single' ? 'blue' : 'pink'}
                  size="sm"
                >
                  {ticketType.type}
                </Badge>
              </Table.Td>
              <Table.Td>
                <Text size="sm" fw={500}>
                  {formatSessions(ticketType.sessionIdentifiers)}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {formatPriceRange(ticketType.minPrice, ticketType.maxPrice)}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {getQuantityDisplay(ticketType.quantityAvailable)}
                </Text>
              </Table.Td>
              <Table.Td>
                <Text size="sm">
                  {formatSalesEndDate(ticketType.salesEndDate)}
                </Text>
              </Table.Td>
              <Table.Td>
                <ActionIcon
                  variant="filled"
                  color="red"
                  size="sm"
                  onClick={() => handleDeleteClick(ticketType.id, ticketType.name)}
                >
                  <IconTrash size={14} />
                </ActionIcon>
              </Table.Td>
            </Table.Tr>
          ))}
          {ticketTypes.length === 0 && (
            <Table.Tr>
              <Table.Td colSpan={8}>
                <Text ta="center" c="dimmed" py="xl">
                  No ticket types created yet. Click "Add Ticket Type" to get started.
                </Text>
              </Table.Td>
            </Table.Tr>
          )}
        </Table.Tbody>
      </Table>

      <Group mt="md" justify="space-between" align="center">
        <Button
          leftSection="+"
          onClick={onAddTicketType}
          style={{
            backgroundColor: 'var(--mantine-color-amber-6)',
            color: 'var(--mantine-color-gray-9)',
            borderRadius: '12px 6px 12px 6px',
          }}
        >
          Add Ticket Type
        </Button>
        
        <Text size="xs" c="dimmed" fs="italic">
          💡 Tip: Click Edit for complex settings like session selection and sale periods. Simple fields can be edited inline.
        </Text>
      </Group>
    </div>
  );
};