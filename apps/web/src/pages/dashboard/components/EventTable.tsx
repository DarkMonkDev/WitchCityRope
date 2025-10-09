import React, { useState } from 'react';
import { Table, Badge, Button, Box, ActionIcon, Group } from '@mantine/core';
import { IconArrowUp, IconArrowDown } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import type { UserEventDto } from '../../../types/dashboard.types';

interface EventTableProps {
  events: UserEventDto[];
}

/**
 * Event table view for user dashboard
 * Columns: Date | Time | Event Title | Status | Action
 * NO pricing or capacity columns - this is user dashboard
 */
export const EventTable: React.FC<EventTableProps> = ({ events }) => {
  const navigate = useNavigate();
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  const statusColors: Record<string, string> = {
    'RSVP Confirmed': 'blue',
    'Ticket Purchased': 'green',
    'Attended': 'grape',
  };

  const formatShortDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
    });
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  };

  const handleSort = () => {
    setSortOrder((prev) => (prev === 'asc' ? 'desc' : 'asc'));
  };

  const sortedEvents = [...events].sort((a, b) => {
    const dateA = new Date(a.startDate).getTime();
    const dateB = new Date(b.startDate).getTime();
    return sortOrder === 'asc' ? dateA - dateB : dateB - dateA;
  });

  return (
    <Box
      style={{
        background: 'white',
        borderRadius: '12px',
        overflow: 'hidden',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
      }}
    >
      <Table striped highlightOnHover>
        <Table.Thead
          style={{
            backgroundColor: 'var(--color-burgundy)',
          }}
        >
          <Table.Tr>
            <Table.Th
              onClick={handleSort}
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                cursor: 'pointer',
                userSelect: 'none',
              }}
            >
              <Group gap="xs">
                Date
                <ActionIcon size="sm" variant="transparent">
                  {sortOrder === 'asc' ? (
                    <IconArrowUp size={16} color="white" />
                  ) : (
                    <IconArrowDown size={16} color="white" />
                  )}
                </ActionIcon>
              </Group>
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Time
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Event Title
            </Table.Th>
            <Table.Th
              ta="center"
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Status
            </Table.Th>
            <Table.Th
              ta="center"
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Action
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {sortedEvents.map((event, index) => (
            <Table.Tr
              key={event.id}
              onClick={() => navigate(`/events/${event.id}`)}
              style={{
                cursor: 'pointer',
                backgroundColor: index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent',
                transition: 'background 0.2s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = 'rgba(136, 1, 36, 0.08)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor =
                  index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent';
              }}
            >
              <Table.Td
                fw={700}
                style={{
                  padding: 'var(--space-md)',
                  fontFamily: 'var(--font-body)',
                  color: 'var(--color-charcoal)',
                }}
              >
                {formatShortDate(event.startDate)}
              </Table.Td>
              <Table.Td
                style={{
                  padding: 'var(--space-md)',
                  fontFamily: 'var(--font-body)',
                  color: 'var(--color-charcoal)',
                }}
              >
                {formatTime(event.startDate)}
              </Table.Td>
              <Table.Td
                style={{
                  padding: 'var(--space-md)',
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 600,
                  color: 'var(--color-burgundy)',
                  fontSize: '1.1rem',
                }}
              >
                {event.title}
              </Table.Td>
              <Table.Td ta="center" style={{ padding: 'var(--space-md)' }}>
                <Badge color={statusColors[event.registrationStatus] || 'gray'} variant="light">
                  {event.registrationStatus}
                </Badge>
              </Table.Td>
              <Table.Td ta="center" style={{ padding: 'var(--space-md)' }}>
                <Button
                  size="xs"
                  variant="outline"
                  color="burgundy"
                  onClick={(e) => {
                    e.stopPropagation();
                    navigate(`/events/${event.id}`);
                  }}
                  styles={{
                    root: {
                      borderRadius: '12px 6px 12px 6px',
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 600,
                      textTransform: 'uppercase',
                      letterSpacing: '1.5px',
                      fontSize: '14px',
                      transition: 'all 0.3s ease',
                      height: 'auto',
                      minHeight: '44px',
                      padding: '14px 32px',
                      lineHeight: 1,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      border: '2px solid var(--color-burgundy)',
                      background: 'transparent',
                      color: 'var(--color-burgundy)',
                      position: 'relative',
                      overflow: 'hidden',
                      zIndex: 1,
                      '&::before': {
                        content: '""',
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        width: 0,
                        height: '100%',
                        background: 'var(--color-burgundy)',
                        transition: 'width 0.4s ease',
                        zIndex: -1,
                      },
                      '&:hover': {
                        borderRadius: '6px 12px 6px 12px',
                        color: 'var(--color-ivory)',
                        borderColor: 'var(--color-burgundy)',
                      },
                      '&:hover::before': {
                        width: '100%',
                      },
                    },
                  }}
                >
                  View Details
                </Button>
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </Box>
  );
};
