import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, Title, Text, Paper, Button, Loader, Alert, Badge, Stack, Group, Card, Divider } from '@mantine/core';
import { IconTicket, IconUsers, IconClock } from '@tabler/icons-react';
import { useEvent } from '../../features/events/api/queries';
import { usePurchaseTicket, useRSVPForEvent, type TicketPurchaseData, type RSVPData } from '../../features/events/api/mutations';
import { EventTicketPurchaseModal } from '../../components/events/EventTicketPurchaseModal';
import { EventRSVPModal } from '../../components/events/EventRSVPModal';
import type { EventTicketType } from '../../components/events/TicketTypeFormModal';

/**
 * EventDetailsPage - Displays detailed information about a specific event
 * Route: /events/:id
 */
export const EventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [ticketPurchaseModalOpened, setTicketPurchaseModalOpened] = useState(false);
  const [rsvpModalOpened, setRSVPModalOpened] = useState(false);
  
  const { data: event, isLoading, error } = useEvent(id || '');
  const purchaseTicketMutation = usePurchaseTicket();
  const rsvpForEventMutation = useRSVPForEvent();

  const handleBackToEvents = () => {
    navigate('/events');
  };

  const handleOpenTicketPurchase = () => {
    setTicketPurchaseModalOpened(true);
  };

  const handleOpenRSVP = () => {
    setRSVPModalOpened(true);
  };

  const handlePurchaseTicket = async (purchaseData: TicketPurchaseData) => {
    await purchaseTicketMutation.mutateAsync(purchaseData);
  };

  const handleRSVP = async (rsvpData: RSVPData) => {
    await rsvpForEventMutation.mutateAsync(rsvpData);
    
    // If user also wants to buy ticket, open ticket purchase modal
    if (rsvpData.alsoWantsToBuyTicket) {
      setRSVPModalOpened(false);
      setTicketPurchaseModalOpened(true);
    }
  };

  // Mock ticket types for demonstration - in real app, these would come from API
  const mockTicketTypes: EventTicketType[] = [
    {
      id: '1',
      name: 'General Admission',
      description: 'Access to all public sessions and activities',
      price: 45.00,
      sessionsIncluded: ['S1', 'S2', 'S3'],
      quantityAvailable: 50,
      quantitySold: 12,
      allowMultiplePurchase: true,
      isEarlyBird: false,
    },
    {
      id: '2',
      name: 'VIP Access',
      description: 'Includes all sessions plus exclusive Q&A and priority seating',
      price: 75.00,
      sessionsIncluded: ['ALL'],
      quantityAvailable: 20,
      quantitySold: 3,
      allowMultiplePurchase: false,
      isEarlyBird: true,
      earlyBirdDiscount: 15,
    },
    {
      id: '3',
      name: 'Student Rate',
      description: 'Discounted rate for students with valid ID',
      price: 25.00,
      sessionsIncluded: ['S1', 'S2'],
      quantityAvailable: 15,
      quantitySold: 8,
      allowMultiplePurchase: false,
      isEarlyBird: false,
    },
  ];

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

  // Determine event type - default to 'class' if not specified
  const eventType = event.eventType || 'class'; // 'class' or 'social'
  const isEventOpen = event.status === 'Published';
  const currentAttendees = event.currentAttendees || 0;
  const capacity = event.capacity || 0;
  const spotsRemaining = capacity - currentAttendees;
  
  // Different UI logic based on event type
  const isClass = eventType === 'Workshop';
  const isSocialEvent = eventType === 'Social';
  const hasTicketTypes = mockTicketTypes.length > 0;

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
              background: isEventOpen ? '#228B2220' : '#8B868020',
              color: isEventOpen ? '#228B22' : '#8B8680',
              border: `1px solid ${isEventOpen ? '#228B22' : '#8B8680'}`,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            {isEventOpen 
              ? (isClass ? 'Tickets Available' : 'RSVP & Tickets Available') 
              : (isClass ? 'Ticket Sales Closed' : 'Event Closed')}
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
              <Group gap="sm">
                <IconClock size={20} />
                Event Details
              </Group>
            </Title>
            
            <Group gap="xl" grow>
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
                <Group gap="xs">
                  <IconUsers size={16} style={{ color: '#8B8680' }} />
                  <Text style={{ color: '#8B8680' }}>
                    {currentAttendees} / {capacity} attendees
                  </Text>
                </Group>
                {spotsRemaining > 0 && (
                  <Text size="sm" c="green" fw={500}>
                    {spotsRemaining} spots remaining
                  </Text>
                )}
              </Box>
            </Group>
          </Paper>

          {/* Ticket Types */}
          <Paper
            style={{
              background: '#FFF8F0',
              padding: '24px',
              borderRadius: '8px',
              border: '1px solid rgba(136, 1, 36, 0.1)',
            }}
          >
            <Title order={4} mb="md" style={{ color: '#2B2B2B', fontFamily: "'Montserrat', sans-serif" }}>
              <Group gap="sm">
                <IconTicket size={20} />
                Available Tickets
              </Group>
            </Title>
            
            <Stack gap="md">
              {mockTicketTypes.map((ticket) => {
                const available = ticket.quantityAvailable - ticket.quantitySold;
                const isTicketSoldOut = available <= 0;
                const effectivePrice = ticket.isEarlyBird && ticket.earlyBirdDiscount
                  ? ticket.price * (1 - ticket.earlyBirdDiscount / 100)
                  : ticket.price;

                return (
                  <Card
                    key={ticket.id}
                    style={{
                      padding: '16px',
                      border: '1px solid rgba(136, 1, 36, 0.1)',
                      borderRadius: '8px',
                      background: isTicketSoldOut ? 'rgba(0,0,0,0.05)' : 'white',
                      opacity: isTicketSoldOut ? 0.7 : 1,
                    }}
                  >
                    <Group justify="space-between" align="flex-start">
                      <Box style={{ flex: 1 }}>
                        <Group gap="sm" mb="xs">
                          <Text fw={600} size="lg">
                            {ticket.name}
                          </Text>
                          {ticket.isEarlyBird && (
                            <Badge color="green" size="sm" variant="light">
                              Early Bird
                            </Badge>
                          )}
                          {isTicketSoldOut && (
                            <Badge color="red" size="sm" variant="light">
                              Sold Out
                            </Badge>
                          )}
                        </Group>
                        <Text size="sm" c="dimmed" mb="xs">
                          {ticket.description}
                        </Text>
                        <Text size="xs" c="dimmed">
                          Sessions: {ticket.sessionsIncluded.join(', ')}
                        </Text>
                        <Text size="xs" c="dimmed">
                          {available} of {ticket.quantityAvailable} available
                        </Text>
                      </Box>
                      
                      <Box ta="right">
                        {ticket.isEarlyBird && ticket.earlyBirdDiscount ? (
                          <Stack gap={2} align="flex-end">
                            <Text
                              size="sm"
                              c="dimmed"
                              td="line-through"
                            >
                              ${ticket.price.toFixed(2)}
                            </Text>
                            <Text fw={600} size="xl" c="green">
                              ${effectivePrice.toFixed(2)}
                            </Text>
                            <Text size="xs" c="green">
                              Save {ticket.earlyBirdDiscount}%
                            </Text>
                          </Stack>
                        ) : (
                          <Text fw={600} size="xl" style={{ color: '#880124' }}>
                            ${effectivePrice.toFixed(2)}
                          </Text>
                        )}
                      </Box>
                    </Group>
                  </Card>
                );
              })}
            </Stack>
          </Paper>
        </Stack>

        {/* Action Section - Different UI based on event type */}
        <Box
          style={{
            textAlign: 'center',
            padding: '24px',
            background: 'rgba(136, 1, 36, 0.05)',
            borderRadius: '8px',
            border: '1px solid rgba(136, 1, 36, 0.1)',
          }}
        >
          {isEventOpen ? (
            <>
              {/* Classes: Ticket Purchase Only */}
              {isClass && (
                <>
                  <Text mb="lg" style={{ fontSize: '16px', color: '#4A4A4A' }}>
                    {spotsRemaining > 0 
                      ? 'Ready to learn? Purchase your tickets to secure your spot in this class.' 
                      : 'Class is sold out, but you can still join the waitlist.'}
                  </Text>
                  <Group justify="center" gap="md">
                    <Button
                      size="lg"
                      onClick={handleOpenTicketPurchase}
                      loading={purchaseTicketMutation.isPending}
                      leftSection={<IconTicket size={20} />}
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
                      {spotsRemaining > 0 ? 'Buy Tickets' : 'Join Waitlist'}
                    </Button>
                    
                    {spotsRemaining > 0 && hasTicketTypes && (
                      <Text size="sm" c="dimmed" style={{ alignSelf: 'center' }}>
                        Starting from ${Math.min(...mockTicketTypes.map(t => 
                          t.isEarlyBird && t.earlyBirdDiscount 
                            ? t.price * (1 - t.earlyBirdDiscount / 100)
                            : t.price
                        )).toFixed(2)}
                      </Text>
                    )}
                  </Group>
                </>
              )}

              {/* Social Events: Both RSVP and Ticket Purchase */}
              {isSocialEvent && (
                <>
                  <Text mb="lg" style={{ fontSize: '16px', color: '#4A4A4A' }}>
                    Join our community gathering! RSVP for free or purchase tickets for additional perks.
                  </Text>
                  <Group justify="center" gap="md">
                    <Button
                      size="lg"
                      onClick={handleOpenRSVP}
                      loading={rsvpForEventMutation.isPending}
                      variant="outline"
                      leftSection={<IconUsers size={20} />}
                      style={{
                        borderColor: '#880124',
                        color: '#880124',
                        fontWeight: 600,
                        fontSize: '16px',
                        padding: '12px 32px',
                        borderRadius: '8px',
                        transition: 'all 0.3s ease'
                      }}
                      onMouseEnter={(e) => {
                        e.currentTarget.style.background = 'rgba(136, 1, 36, 0.1)';
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.background = 'transparent';
                      }}
                    >
                      RSVP (Free)
                    </Button>
                    
                    {hasTicketTypes && (
                      <Button
                        size="lg"
                        onClick={handleOpenTicketPurchase}
                        loading={purchaseTicketMutation.isPending}
                        leftSection={<IconTicket size={20} />}
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
                        Buy Tickets
                      </Button>
                    )}
                  </Group>
                  
                  {hasTicketTypes && (
                    <Text size="sm" c="dimmed" mt="sm">
                      RSVP is free • Tickets start from ${Math.min(...mockTicketTypes.map(t => 
                        t.isEarlyBird && t.earlyBirdDiscount 
                          ? t.price * (1 - t.earlyBirdDiscount / 100)
                          : t.price
                      )).toFixed(2)}
                    </Text>
                  )}
                </>
              )}
            </>
          ) : (
            <Text style={{ fontSize: '16px', color: '#8B8680' }}>
              {isClass ? 'Ticket sales are currently closed for this class.' : 'This event is currently closed.'}
            </Text>
          )}
        </Box>
      </Paper>

      {/* Ticket Purchase Modal */}
      <EventTicketPurchaseModal
        opened={ticketPurchaseModalOpened}
        onClose={() => setTicketPurchaseModalOpened(false)}
        onPurchase={handlePurchaseTicket}
        event={{
          id: event?.id || '',
          title: event?.title || '',
          startDate: event?.startDateTimeTime || event?.startDateTime || '',
          endDate: event?.endDateTimeTime || event?.endDateTime || '',
        }}
        ticketTypes={mockTicketTypes}
        isPurchasing={purchaseTicketMutation.isPending}
      />

      {/* RSVP Modal (only for Social Events) */}
      {isSocialEvent && (
        <EventRSVPModal
          opened={rsvpModalOpened}
          onClose={() => setRSVPModalOpened(false)}
          onRSVP={handleRSVP}
          onPurchaseTicket={handleOpenTicketPurchase}
          event={{
            id: event?.id || '',
            title: event?.title || '',
            startDate: event?.startDateTimeTime || event?.startDateTime || '',
            endDate: event?.endDateTimeTime || event?.endDateTime || '',
          }}
          isRSVPing={rsvpForEventMutation.isPending}
          hasTicketTypes={hasTicketTypes}
        />
      )}
    </Box>
  );
};