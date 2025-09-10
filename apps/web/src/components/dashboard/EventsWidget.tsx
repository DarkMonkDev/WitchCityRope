import React from 'react';
import { Box, Text, Stack, Loader, Alert, Group, Badge } from '@mantine/core';
import { Link } from 'react-router-dom';
import { DashboardCard } from './DashboardCard';
import { useEvents } from '../../features/events/api/queries';
import type { EventDto } from '@witchcityrope/shared-types';

// Helper function to format event for display
const formatEventForWidget = (event: EventDto) => {
  // Safely handle potentially null/undefined date strings
  const startDateString = event.startDateTime;
  const endDateString = event.endDateTime;
  
  // Only create Date objects if we have valid date strings
  const startDate = startDateString ? new Date(startDateString) : null;
  const endDate = endDateString ? new Date(endDateString) : null;
  
  // Validate that dates are actually valid Date objects
  const isStartDateValid = startDate && !isNaN(startDate.getTime());
  const isEndDateValid = endDate && !isNaN(endDate.getTime());
  
  const formatTime = (date: Date) => {
    try {
      return date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
    } catch (error) {
      return 'TBD';
    }
  };
  
  // Fallback values for invalid dates
  const fallbackDate = new Date().toISOString().split('T')[0];
  const fallbackTime = 'TBD';
  
  return {
    id: event.id,
    title: event.title || 'Untitled Event',
    date: isStartDateValid ? startDate!.toISOString().split('T')[0] : fallbackDate,
    time: isStartDateValid && isEndDateValid 
      ? `${formatTime(startDate!)} - ${formatTime(endDate!)}`
      : isStartDateValid 
        ? `${formatTime(startDate!)} - ${fallbackTime}`
        : fallbackTime,
    status: event.status === 'Published' ? 'Open' : 'Closed',
    statusColor: event.status === 'Published' ? '#228B22' : '#DAA520',
    isUpcoming: isStartDateValid ? startDate! > new Date() : false,
  };
};

interface EventsWidgetProps {
  limit?: number;
  showPastEvents?: boolean;
}

/**
 * Events Widget for Dashboard
 * Shows upcoming events in a card format
 * Integrates with TanStack Query for real-time data
 */
export const EventsWidget: React.FC<EventsWidgetProps> = ({ 
  limit = 3, 
  showPastEvents = false 
}) => {
  const { data: events, isLoading, error } = useEvents();
  
  // Filter and format events
  const displayEvents = React.useMemo(() => {
    if (!events) return [];
    
    const filteredEvents = events
      .map(formatEventForWidget)
      .filter(event => showPastEvents || event.isUpcoming)
      .sort((a, b) => {
        const dateA = new Date(a.date).getTime();
        const dateB = new Date(b.date).getTime();
        return dateA - dateB;
      })
      .slice(0, limit);
      
    return filteredEvents;
  }, [events, limit, showPastEvents]);

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    
    // Handle invalid dates gracefully
    if (isNaN(date.getTime())) {
      return { day: '??', month: 'TBD' };
    }
    
    const day = date.getDate();
    const month = date.toLocaleDateString('en-US', { month: 'short' });
    return { day, month };
  };

  return (
    <DashboardCard
      title="Upcoming Events"
      icon="📅"
      status={`${displayEvents.length} Events`}
      action="View All Events"
      onActionClick={() => window.location.href = '/events'}
      loading={isLoading}
    >
      {isLoading ? (
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Loader size="md" color="#880124" />
          <Text size="sm" c="dimmed" mt="sm">
            Loading events...
          </Text>
        </Box>
      ) : error ? (
        <Alert 
          color="red" 
          variant="light"
          style={{ 
            background: 'rgba(220, 20, 60, 0.05)', 
            border: '1px solid rgba(220, 20, 60, 0.2)' 
          }}
        >
          Failed to load events
        </Alert>
      ) : displayEvents.length > 0 ? (
        <Stack gap="xs">
          {displayEvents.map((event) => {
            const { day, month } = formatDate(event.date);
            return (
              <Box
                key={event.id}
                style={{
                  display: 'flex',
                  gap: '12px',
                  padding: '12px',
                  background: '#FFF8F0',
                  borderRadius: '6px',
                  border: '1px solid rgba(183, 109, 117, 0.15)',
                  transition: 'all 0.2s ease',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(136, 1, 36, 0.3)';
                  e.currentTarget.style.transform = 'translateX(2px)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.15)';
                  e.currentTarget.style.transform = 'translateX(0)';
                }}
              >
                {/* Date Badge */}
                <Box
                  style={{
                    background: 'linear-gradient(135deg, #880124, #614B79)',
                    color: '#FFF8F0',
                    padding: '6px 8px',
                    borderRadius: '4px',
                    textAlign: 'center',
                    minWidth: '50px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontSize: '14px',
                      fontWeight: 800,
                      lineHeight: 1,
                    }}
                  >
                    {day}
                  </Text>
                  <Text
                    style={{
                      fontSize: '9px',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      marginTop: '1px',
                      opacity: 0.9,
                    }}
                  >
                    {month}
                  </Text>
                </Box>

                {/* Event Details */}
                <Box style={{ flex: 1 }}>
                  <Group justify="space-between" mb={4}>
                    <Text
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontSize: '14px',
                        fontWeight: 600,
                        color: '#2B2B2B',
                        lineHeight: 1.2,
                      }}
                    >
                      {event.title}
                    </Text>
                    
                    <Badge
                      size="xs"
                      style={{
                        backgroundColor: `${event.statusColor}20`,
                        color: event.statusColor,
                        border: `1px solid ${event.statusColor}40`,
                      }}
                    >
                      {event.status}
                    </Badge>
                  </Group>
                  
                  <Text
                    size="xs"
                    c="dimmed"
                    style={{
                      color: '#8B8680',
                      fontFamily: "'Montserrat', sans-serif",
                    }}
                  >
                    {event.time}
                  </Text>
                </Box>
              </Box>
            );
          })}
        </Stack>
      ) : (
        <Box style={{ textAlign: 'center', padding: '20px 0' }}>
          <Text
            style={{
              fontSize: '24px',
              marginBottom: '8px',
              opacity: 0.5,
            }}
          >
            📅
          </Text>
          <Text
            size="sm"
            c="dimmed"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontWeight: 500,
            }}
          >
            No upcoming events
          </Text>
          <Text
            size="xs"
            c="dimmed"
            mt={4}
          >
            <Text
              component={Link}
              to="/events"
              style={{
                color: '#880124',
                textDecoration: 'none',
                fontWeight: 600,
              }}
            >
              Browse all events →
            </Text>
          </Text>
        </Box>
      )}
    </DashboardCard>
  );
};