import React from 'react';
import { Box, Title, Text, Paper, Grid, Loader, Alert } from '@mantine/core';
import { useEvents } from '../../features/events/api/queries';
import type { EventDto } from '@witchcityrope/shared-types';

// Helper function to format event dates and status
const formatEventDisplay = (event: EventDto) => {
  const startDate = new Date(event.startDateTime || '');
  const endDate = new Date(event.endDateTime || '');
  const now = new Date();
  
  const formatDate = (date: Date) => {
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

  let status = 'upcoming';
  let statusColor = '#228B22';
  
  if (startDate > now) {
    status = 'upcoming';
    statusColor = '#228B22';
  } else if (endDate < now) {
    status = 'completed';
    statusColor = '#8B8680';
  } else {
    status = 'in progress';
    statusColor = '#DAA520';
  }

  return {
    ...event,
    formattedDate: formatDate(startDate),
    formattedTime: `${formatTime(startDate)} - ${formatTime(endDate)}`,
    status,
    statusColor,
    isPast: endDate < now,
    registrationStatus: event.status === 'Published' ? 'Open' : 'Closed'
  };
};

/**
 * PublicEventsPage - Displays all published events without authentication required
 * This is the page that users see when they visit /events
 */
export const PublicEventsPage: React.FC = () => {
  const { data: events, isLoading, error } = useEvents();

  // Filter to show only published events for public viewing
  const publishedEvents = events 
    ? events.filter(event => event.status === 'Published').map(formatEventDisplay)
    : [];

  if (isLoading) {
    return (
      <Box p="xl" style={{ minHeight: '400px' }}>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
            textAlign: 'center'
          }}
        >
          Upcoming Events
        </Title>
        
        <Box style={{ textAlign: 'center', padding: '40px' }}>
          <Loader size="lg" color="#880124" />
          <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading events...</Text>
        </Box>
      </Box>
    );
  }

  if (error) {
    return (
      <Box p="xl">
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
            textAlign: 'center'
          }}
        >
          Upcoming Events
        </Title>
        
        <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
          Failed to load events. Please try refreshing the page.
        </Alert>
      </Box>
    );
  }

  return (
    <Box p="xl" style={{ minHeight: '400px' }}>
      <Title
        order={1}
        mb="xl"
        style={{
          fontFamily: "'Montserrat', sans-serif",
          fontSize: '32px',
          fontWeight: 800,
          color: '#880124',
          textTransform: 'uppercase',
          letterSpacing: '-0.5px',
          textAlign: 'center'
        }}
      >
        Upcoming Events
      </Title>

      <Text
        mb="xl"
        style={{
          textAlign: 'center',
          fontSize: '18px',
          color: '#8B8680',
          maxWidth: '600px',
          margin: '0 auto 40px'
        }}
      >
        Join our community for workshops, classes, and social gatherings. All skill levels welcome.
      </Text>

      {/* Published Events Grid */}
      {publishedEvents.length > 0 ? (
        <Grid>
          {publishedEvents.map((event) => (
            <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
              <Paper
                className="event-card"
                data-testid="event-card"
                data-event-id={event.id}
                style={{
                  background: '#FFF8F0',
                  padding: '24px',
                  borderLeft: '4px solid #880124',
                  transition: 'all 0.3s ease',
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                  cursor: 'pointer'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-4px)';
                  e.currentTarget.style.boxShadow = '0 6px 20px rgba(0,0,0,0.1)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = 'none';
                }}
                onClick={() => {
                  // TODO: Navigate to event details page
                  console.log('Navigate to event details:', event.id);
                }}
              >
                <Title
                  order={3}
                  mb="sm"
                  className="event-title"
                  data-testid="event-title"
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontSize: '20px',
                    fontWeight: 700,
                    color: '#2B2B2B',
                    lineHeight: 1.3,
                  }}
                >
                  {event.title}
                </Title>

                <Text
                  mb="md"
                  style={{
                    color: '#8B8680',
                    fontSize: '14px',
                    lineHeight: 1.5,
                    flex: 1,
                  }}
                >
                  {event.description || 'Join us for this exciting event in our community.'}
                </Text>

                <Box
                  style={{
                    background: 'rgba(183, 109, 117, 0.05)',
                    padding: '16px',
                    borderRadius: '8px',
                    marginTop: 'auto',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 600,
                      fontSize: '14px',
                      color: '#2B2B2B',
                      marginBottom: '8px',
                    }}
                  >
                    📅 {event.formattedDate}
                  </Text>
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 500,
                      fontSize: '14px',
                      color: '#8B8680',
                      marginBottom: '12px'
                    }}
                  >
                    🕐 {event.formattedTime}
                  </Text>
                  
                  {/* Registration Status */}
                  <Box style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                      👥 {event.currentAttendees || 0}/{event.capacity || 0} attendees
                    </Text>
                    <Box
                      style={{
                        background: '#228B22',
                        color: 'white',
                        padding: '4px 8px',
                        borderRadius: '4px',
                        fontSize: '12px',
                        fontWeight: 600,
                        textTransform: 'uppercase'
                      }}
                    >
                      Registration Open
                    </Box>
                  </Box>
                </Box>
              </Paper>
            </Grid.Col>
          ))}
        </Grid>
      ) : (
        <Paper
          style={{
            textAlign: 'center',
            padding: '60px 24px',
            color: '#8B8680',
            background: '#FFF8F0',
            borderRadius: '8px',
            maxWidth: '500px',
            margin: '0 auto'
          }}
        >
          <Text style={{ fontSize: '64px', marginBottom: '20px', opacity: 0.5 }}>
            📅
          </Text>
          <Title
            order={3}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '24px',
              fontWeight: 600,
              color: '#4A4A4A',
              marginBottom: '16px',
            }}
          >
            No Events Currently Available
          </Title>
          <Text style={{ fontSize: '16px', lineHeight: 1.6 }}>
            We're working on scheduling new workshops and events. Check back soon or 
            follow our community updates for announcements.
          </Text>
        </Paper>
      )}
    </Box>
  );
};