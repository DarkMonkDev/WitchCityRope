import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Title, Text, Paper, Button, Loader, Alert, Badge, Stack } from '@mantine/core';
import { useEvent } from '../../features/events/api/queries';

/**
 * EventDetailsPage - Displays detailed information about a specific event
 * Route: /events/:id
 */
export const EventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: event, isLoading, error } = useEvent(id || '');

  const handleBackToEvents = () => {
    navigate('/events');
  };

  const handleRSVP = () => {
    // TODO: Implement RSVP functionality
    console.log('RSVP clicked for event:', id);
    alert('RSVP functionality coming soon!');
  };

  if (isLoading) {
    return (
      <Box p="xl" style={{ minHeight: '400px' }}>
        <Box style={{ textAlign: 'center', padding: '60px' }}>
          <Loader size="lg" color="#880124" />
          <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading event details...</Text>
        </Box>
      </Box>
    );
  }

  if (error || !event) {
    return (
      <Box p="xl">
        <Button 
          variant="subtle" 
          onClick={handleBackToEvents}
          mb="lg"
          style={{ color: '#880124' }}
        >
          ← Back to Events
        </Button>
        
        <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
          Event not found or failed to load. Please try again or go back to the events list.
        </Alert>
      </Box>
    );
  }

  // Format dates
  const startDate = new Date(event.startDateTime || '');
  const endDate = new Date(event.endDateTime || '');
  
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

  const isRegistrationOpen = event.status === 'Published';
  const currentAttendees = event.currentAttendees || 0;
  const capacity = event.capacity || 0;
  const spotsRemaining = capacity - currentAttendees;

  return (
    <Box p="xl" style={{ maxWidth: '800px', margin: '0 auto' }}>
      <Button 
        variant="subtle" 
        onClick={handleBackToEvents}
        mb="lg"
        style={{ color: '#880124' }}
      >
        ← Back to Events
      </Button>

      <Paper
        style={{
          background: '#FFF8F0',
          padding: '40px',
          borderRadius: '12px',
          border: '1px solid rgba(136, 1, 36, 0.1)',
        }}
      >
        {/* Event Title and Status */}
        <Box mb="lg">
          <Badge
            size="lg"
            mb="md"
            style={{
              background: isRegistrationOpen ? '#228B2220' : '#8B868020',
              color: isRegistrationOpen ? '#228B22' : '#8B8680',
              border: `1px solid ${isRegistrationOpen ? '#228B22' : '#8B8680'}`,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            {isRegistrationOpen ? 'Registration Open' : 'Registration Closed'}
          </Badge>

          <Title
            order={1}
            mb="md"
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '36px',
              fontWeight: 800,
              color: '#2B2B2B',
              lineHeight: 1.2,
            }}
          >
            {event.title}
          </Title>
        </Box>

        {/* Event Description */}
        <Box mb="xl">
          <Text
            style={{
              fontSize: '18px',
              lineHeight: 1.6,
              color: '#4A4A4A',
            }}
          >
            {event.description || 'Join us for this exciting event in our community. Details will be provided closer to the event date.'}
          </Text>
        </Box>

        {/* Event Details */}
        <Stack gap="lg" mb="xl">
          <Paper
            style={{
              background: 'rgba(183, 109, 117, 0.05)',
              padding: '24px',
              borderRadius: '8px',
              border: '1px solid rgba(183, 109, 117, 0.1)',
            }}
          >
            <Title order={4} mb="md" style={{ color: '#2B2B2B', fontFamily: "'Montserrat', sans-serif" }}>
              📅 Event Details
            </Title>
            
            <Stack gap="sm">
              <Box>
                <Text fw={600} style={{ color: '#2B2B2B', marginBottom: '4px' }}>Date:</Text>
                <Text style={{ color: '#8B8680' }}>{formatDate(startDate)}</Text>
              </Box>
              
              <Box>
                <Text fw={600} style={{ color: '#2B2B2B', marginBottom: '4px' }}>Time:</Text>
                <Text style={{ color: '#8B8680' }}>
                  {formatTime(startDate)} - {formatTime(endDate)}
                </Text>
              </Box>
              
              <Box>
                <Text fw={600} style={{ color: '#2B2B2B', marginBottom: '4px' }}>Capacity:</Text>
                <Text style={{ color: '#8B8680' }}>
                  {currentAttendees} / {capacity} attendees
                  {spotsRemaining > 0 && ` (${spotsRemaining} spots remaining)`}
                </Text>
              </Box>
            </Stack>
          </Paper>
        </Stack>

        {/* RSVP Section */}
        <Box
          style={{
            textAlign: 'center',
            padding: '24px',
            background: 'rgba(136, 1, 36, 0.05)',
            borderRadius: '8px',
            border: '1px solid rgba(136, 1, 36, 0.1)',
          }}
        >
          {isRegistrationOpen && spotsRemaining > 0 ? (
            <>
              <Text mb="lg" style={{ fontSize: '16px', color: '#4A4A4A' }}>
                Ready to join us? Reserve your spot today.
              </Text>
              <Button
                size="lg"
                onClick={handleRSVP}
                style={{
                  background: '#880124',
                  color: 'white',
                  fontWeight: 600,
                  fontSize: '16px',
                  padding: '12px 32px',
                  borderRadius: '8px',
                  transition: 'all 0.3s ease'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.background = '#6B0119';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background = '#880124';
                }}
              >
                RSVP Now
              </Button>
            </>
          ) : spotsRemaining <= 0 ? (
            <>
              <Text mb="md" style={{ fontSize: '16px', color: '#8B8680' }}>
                This event is currently full.
              </Text>
              <Button
                size="lg"
                variant="outline"
                onClick={handleRSVP}
                style={{
                  borderColor: '#8B8680',
                  color: '#8B8680',
                  fontWeight: 600,
                }}
              >
                Join Waitlist
              </Button>
            </>
          ) : (
            <Text style={{ fontSize: '16px', color: '#8B8680' }}>
              Registration is currently closed for this event.
            </Text>
          )}
        </Box>
      </Paper>
    </Box>
  );
};