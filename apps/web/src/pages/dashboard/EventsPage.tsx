import React from 'react';
import { Box, Title, Text, Paper, Grid, Loader, Alert, Stack, Badge } from '@mantine/core';
import { useEvents } from '../../lib/api/hooks/useEvents';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';
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
 * Events Page - Shows user's registered events and event history
 * Uses real API data via TanStack Query hooks
 */
export const EventsPage: React.FC = () => {
  const { data: events, isLoading, error } = useEvents();

  // For now, show all events - in the future this would be filtered to user's registered events
  // TODO: Add API endpoint to get user's registered events only
  const userEvents = Array.isArray(events) ? events.map(formatEventDisplay) : [];
  
  // Separate upcoming and past events
  const upcomingEvents = userEvents.filter(event => !event.isPast);
  const pastEvents = userEvents.filter(event => event.isPast);

  if (isLoading) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Your Events
          </Title>
          
          <Box style={{ textAlign: 'center', padding: '40px' }}>
            <Loader size="lg" color="#880124" />
            <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading your events...</Text>
          </Box>
        </Box>
      </DashboardLayout>
    );
  }

  if (error) {
    return (
      <DashboardLayout>
        <Box>
          <Title
            order={1}
            mb="xl"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Your Events
          </Title>
          
          <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
            Failed to load your events. Please try refreshing the page.
          </Alert>
        </Box>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout>
      <Box>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Your Events
        </Title>

        {/* Note about future functionality */}
        <Alert 
          color="blue" 
          mb="xl" 
          style={{ 
            background: 'rgba(59, 130, 246, 0.1)', 
            border: '1px solid rgba(59, 130, 246, 0.3)',
            borderRadius: '6px'
          }}
        >
          <Text style={{ fontSize: '14px' }}>
            <strong>Note:</strong> Currently showing all events. Future update will filter to show only your registered events and event history.
          </Text>
        </Alert>

        {/* Upcoming Events Section */}
        {upcomingEvents.length > 0 && (
          <Box mb="xl">
            <Title
              order={2}
              mb="lg"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Upcoming Events ({upcomingEvents.length})
            </Title>
            
            <Grid>
              {upcomingEvents.map((event) => (
                <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
                  <Paper
                    style={{
                      background: '#FFF8F0',
                      padding: '20px',
                      borderLeft: '4px solid #880124',
                      transition: 'all 0.3s ease',
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = 'translateY(-4px)';
                      e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = 'translateY(0)';
                      e.currentTarget.style.boxShadow = 'none';
                    }}
                  >
                    <Box mb="sm">
                      <Badge
                        size="sm"
                        style={{
                          background: `${event.statusColor}20`,
                          color: event.statusColor,
                          border: `1px solid ${event.statusColor}`,
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }}
                      >
                        {event.status}
                      </Badge>
                    </Box>

                    <Title
                      order={3}
                      mb="xs"
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontSize: '18px',
                        fontWeight: 700,
                        color: '#2B2B2B',
                        lineHeight: 1.3,
                      }}
                    >
                      {event.title}
                    </Title>

                    <Text
                      mb="sm"
                      style={{
                        color: '#8B8680',
                        fontSize: '14px',
                        lineHeight: 1.5,
                        flex: 1,
                      }}
                    >
                      {event.description || 'No description available'}
                    </Text>

                    <Box
                      style={{
                        background: 'rgba(183, 109, 117, 0.05)',
                        padding: '12px',
                        borderRadius: '6px',
                        marginTop: 'auto',
                      }}
                    >
                      <Text
                        style={{
                          fontFamily: "'Montserrat', sans-serif",
                          fontWeight: 600,
                          fontSize: '13px',
                          color: '#2B2B2B',
                          marginBottom: '4px',
                        }}
                      >
                        📅 {event.formattedDate}
                      </Text>
                      <Text
                        style={{
                          fontFamily: "'Montserrat', sans-serif",
                          fontWeight: 500,
                          fontSize: '13px',
                          color: '#8B8680',
                        }}
                      >
                        🕐 {event.formattedTime}
                      </Text>
                      
                      {/* Capacity Info */}
                      <Box style={{ marginTop: '8px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                          👥 {event.currentAttendees || 0}/{event.capacity || 0} attendees
                        </Text>
                        {event.status === 'Published' && (
                          <Badge size="xs" color="green">Open</Badge>
                        )}
                      </Box>
                    </Box>
                  </Paper>
                </Grid.Col>
              ))}
            </Grid>
          </Box>
        )}

        {/* Past Events Section */}
        {pastEvents.length > 0 && (
          <Box>
            <Title
              order={2}
              mb="lg"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 700,
                color: '#2B2B2B',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Past Events ({pastEvents.length})
            </Title>
            
            <Grid>
              {pastEvents.map((event) => (
                <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
                  <Paper
                    style={{
                      background: '#FAF6F2',
                      padding: '20px',
                      borderLeft: '4px solid #B76D75',
                      transition: 'all 0.3s ease',
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      opacity: 0.85,
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.opacity = '1';
                      e.currentTarget.style.transform = 'translateY(-2px)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.opacity = '0.85';
                      e.currentTarget.style.transform = 'translateY(0)';
                    }}
                  >
                    <Box mb="sm">
                      <Badge
                        size="sm"
                        style={{
                          background: `${event.statusColor}20`,
                          color: event.statusColor,
                          border: `1px solid ${event.statusColor}`,
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }}
                      >
                        {event.status}
                      </Badge>
                    </Box>

                    <Title
                      order={3}
                      mb="xs"
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontSize: '18px',
                        fontWeight: 700,
                        color: '#4A4A4A',
                        lineHeight: 1.3,
                      }}
                    >
                      {event.title}
                    </Title>

                    <Text
                      mb="sm"
                      style={{
                        color: '#8B8680',
                        fontSize: '14px',
                        lineHeight: 1.5,
                        flex: 1,
                      }}
                    >
                      {event.description || 'No description available'}
                    </Text>

                    <Box
                      style={{
                        background: 'rgba(183, 109, 117, 0.08)',
                        padding: '12px',
                        borderRadius: '6px',
                        marginTop: 'auto',
                      }}
                    >
                      <Text
                        style={{
                          fontFamily: "'Montserrat', sans-serif",
                          fontWeight: 600,
                          fontSize: '13px',
                          color: '#4A4A4A',
                          marginBottom: '4px',
                        }}
                      >
                        📅 {event.formattedDate}
                      </Text>
                      <Text
                        style={{
                          fontFamily: "'Montserrat', sans-serif",
                          fontWeight: 500,
                          fontSize: '13px',
                          color: '#8B8680',
                        }}
                      >
                        🕐 {event.formattedTime}
                      </Text>
                    </Box>
                  </Paper>
                </Grid.Col>
              ))}
            </Grid>
          </Box>
        )}

        {/* No Events State */}
        {userEvents.length === 0 && (
          <Paper
            style={{
              textAlign: 'center',
              padding: '40px 24px',
              color: '#8B8680',
              background: '#FFF8F0',
              borderRadius: '6px',
            }}
          >
            <Text style={{ fontSize: '48px', marginBottom: '16px', opacity: 0.5 }}>
              📅
            </Text>
            <Title
              order={3}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '20px',
                fontWeight: 600,
                color: '#4A4A4A',
                marginBottom: '12px',
              }}
            >
              No Events Found
            </Title>
            <Text style={{ fontSize: '16px', marginBottom: '20px', maxWidth: '400px', margin: '0 auto 20px' }}>
              You haven't registered for any events yet. Browse our available classes and community gatherings.
            </Text>
            
            <Box component="a" href="/events" className="btn btn-primary">
              Browse All Events
            </Box>
          </Paper>
        )}
      </Box>
    </DashboardLayout>
  );
};