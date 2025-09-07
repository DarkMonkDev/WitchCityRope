import React from 'react';
import { Box, Title, Text, Paper, Grid, Button, Loader, Alert, Badge, ActionIcon } from '@mantine/core';
import { IconEdit, IconTrash, IconPlus, IconEye } from '@tabler/icons-react';
import { useEvents } from '../../features/events/api/queries';
import type { EventDto } from '@witchcityrope/shared-types';

// Helper function to format event dates and status
const formatEventDisplay = (event: EventDto) => {
  const startDate = new Date(event.startDateTime || '');
  const endDate = new Date(event.endDateTime || '');
  const now = new Date();
  
  const formatDate = (date: Date) => {
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
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
    isPast: endDate < now
  };
};

/**
 * AdminEventsPage - Administrative interface for managing events
 * Route: /admin/events (requires authentication and admin role)
 */
export const AdminEventsPage: React.FC = () => {
  const { data: events, isLoading, error } = useEvents();

  const handleCreateEvent = () => {
    // TODO: Navigate to event creation form
    console.log('Create new event');
    alert('Event creation form coming soon!');
  };

  const handleEditEvent = (eventId: string) => {
    // TODO: Navigate to event edit form
    console.log('Edit event:', eventId);
    alert(`Edit event ${eventId} - Form coming soon!`);
  };

  const handleDeleteEvent = (eventId: string) => {
    // TODO: Implement event deletion with confirmation
    console.log('Delete event:', eventId);
    if (confirm('Are you sure you want to delete this event? This action cannot be undone.')) {
      alert(`Delete event ${eventId} - API call coming soon!`);
    }
  };

  const handleViewEvent = (eventId: string) => {
    // TODO: Navigate to public event view
    console.log('View event:', eventId);
    window.open(`/events/${eventId}`, '_blank');
  };

  // Process events for admin view
  const adminEvents = events ? events.map(formatEventDisplay) : [];

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
          }}
        >
          Event Management
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
          }}
        >
          Event Management
        </Title>
        
        <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
          Failed to load events. Please try refreshing the page.
        </Alert>
      </Box>
    );
  }

  return (
    <Box p="xl">
      <Box style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px' }}>
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Event Management
        </Title>

        <Button
          leftSection={<IconPlus size={16} />}
          onClick={handleCreateEvent}
          style={{
            background: '#880124',
            color: 'white',
            fontWeight: 600,
            fontSize: '16px',
            height: '48px',
            paddingLeft: '24px',
            paddingRight: '24px'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = '#6B0119';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = '#880124';
          }}
        >
          Create Event
        </Button>
      </Box>

      <Text
        mb="xl"
        style={{
          fontSize: '16px',
          color: '#8B8680',
          maxWidth: '600px'
        }}
      >
        Manage all events, workshops, and community gatherings. Create new events, edit existing ones, and monitor registrations.
      </Text>

      {/* Events List */}
      {adminEvents.length > 0 ? (
        <Grid>
          {adminEvents.map((event) => (
            <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
              <Paper
                className="admin-event-card"
                data-testid="admin-event"
                data-event-id={event.id}
                style={{
                  background: '#FFF8F0',
                  padding: '24px',
                  borderLeft: '4px solid #880124',
                  transition: 'all 0.3s ease',
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = 'none';
                }}
              >
                {/* Status and Actions Header */}
                <Box style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
                  <Badge
                    size="sm"
                    style={{
                      background: event.status === 'Published' ? '#228B2220' : '#8B868020',
                      color: event.status === 'Published' ? '#228B22' : '#8B8680',
                      border: `1px solid ${event.status === 'Published' ? '#228B22' : '#8B8680'}`,
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    {event.status}
                  </Badge>

                  <Box style={{ display: 'flex', gap: '8px' }}>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleViewEvent(event.id)}
                      style={{ color: '#8B8680' }}
                      title="View Event"
                    >
                      <IconEye size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleEditEvent(event.id)}
                      style={{ color: '#880124' }}
                      title="Edit Event"
                    >
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleDeleteEvent(event.id)}
                      style={{ color: '#DC143C' }}
                      title="Delete Event"
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Box>
                </Box>

                <Title
                  order={3}
                  mb="sm"
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
                  mb="md"
                  style={{
                    color: '#8B8680',
                    fontSize: '14px',
                    lineHeight: 1.5,
                    flex: 1,
                  }}
                >
                  {event.description || 'No description provided'}
                </Text>

                {/* Event Details */}
                <Box
                  style={{
                    background: 'rgba(183, 109, 117, 0.05)',
                    padding: '16px',
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
                      marginBottom: '8px'
                    }}
                  >
                    🕐 {event.formattedTime}
                  </Text>
                  
                  {/* Attendance and Capacity */}
                  <Box style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                      👥 {event.currentAttendees || 0}/{event.capacity || 0}
                    </Text>
                    <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                      ID: {event.id.slice(0, 8)}...
                    </Text>
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
            📋
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
            No Events Created Yet
          </Title>
          <Text style={{ fontSize: '16px', lineHeight: 1.6, marginBottom: '24px' }}>
            Get started by creating your first event. You can schedule workshops, 
            classes, and community gatherings for your members.
          </Text>
          <Button
            onClick={handleCreateEvent}
            style={{
              background: '#880124',
              color: 'white',
              fontWeight: 600,
            }}
          >
            Create Your First Event
          </Button>
        </Paper>
      )}
    </Box>
  );
};