import React, { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Container, Stack, Title, Text, Breadcrumbs,
  Anchor, Alert, Button, Box, Group, Paper,
  ActionIcon, List, Avatar, Skeleton, Center
} from '@mantine/core';
import {
  IconCalendar, IconClock, IconMapPin, IconUsers,
  IconShare, IconMail, IconBrandX, IconLink, IconCheck
} from '@tabler/icons-react';
import { formatEventDate, formatEventTime } from '../../utils/eventUtils';
import { useEvent } from '../../lib/api/hooks/useEvents';
import { useParticipation, useCreateRSVP, useCancelRSVP, useCancelTicket } from '../../hooks/useParticipation';
import { ParticipationCard } from '../../components/events/ParticipationCard';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import type { EventDto } from '../../lib/api/types/events.types';
import { useVolunteerPositions } from '../../features/volunteers/hooks/useVolunteerPositions';
import { VolunteerPositionCard } from '../../features/volunteers/components/VolunteerPositionCard';
import { VolunteerEncouragementBox } from '../../components/events/VolunteerEncouragementBox';
import { UserVolunteerShifts } from '../../components/events/UserVolunteerShifts';
import styles from './EventDetailPage.module.css';

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [selectedTicket, setSelectedTicket] = useState('single');

  const { data: event, isLoading, error } = useEvent(id!, !!id);
  const { data: participation, isLoading: participationLoading } = useParticipation(id!, !!id);
  const { data: currentUser } = useCurrentUser();
  const { data: volunteerPositions, isLoading: volunteerLoading } = useVolunteerPositions(id!, !!id);
  const createRSVPMutation = useCreateRSVP();
  const cancelRSVPMutation = useCancelRSVP();
  const cancelTicketMutation = useCancelTicket();

  // Check if current user is admin
  const isAdmin = (currentUser as any)?.role === 'Administrator';
  
  if (isLoading) {
    return (
      <Box data-testid="event-details" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
        <Container size="xl" py="xl">
          <EventDetailSkeleton />
        </Container>
      </Box>
    );
  }
  
  if (error || !event) {
    return (
      <Box data-testid="event-details" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
        <Container size="xl" py="xl">
          <Alert color="red" title="Event Not Found">
            <Text>Sorry, we couldn't find this event. It may have been removed or the link is incorrect.</Text>
            <Button component="a" href="/events" mt="md" size="sm">
              Back to Events
            </Button>
          </Alert>
        </Container>
      </Box>
    );
  }

  const availableSpots = ((event as any)?.capacity || 0) - ((event as any)?.registrationCount || 0);
  const capacityPercentage = (event as any)?.capacity ? ((event as any)?.registrationCount || 0) / (event as any)?.capacity * 100 : 0;
  
  const getAvailabilityStatus = () => {
    if (availableSpots <= 0) return { status: 'sold-out', color: 'var(--color-stone)' };
    if (availableSpots <= 3) return { status: 'low', color: 'var(--color-error)' };
    return { status: 'available', color: 'var(--color-success)' };
  };
  
  const availability = getAvailabilityStatus();

  const handleRSVP = (notes?: string) => {
    if (!id) return;
    createRSVPMutation.mutate({
      eventId: id,
      notes
    });
  };

  const handlePurchaseTicket = (amount: number, slidingScalePercentage?: number) => {
    console.log('Purchasing ticket for:', (event as any)?.title, 'Amount:', amount, 'Sliding Scale:', slidingScalePercentage);
    console.log('✅ PayPal payment flow completed - ticket should be created by PayPal integration');
  };

  const handleCancel = (type: 'rsvp' | 'ticket', reason?: string) => {
    if (!id) return;

    if (type === 'rsvp') {
      cancelRSVPMutation.mutate({ eventId: id, reason });
    } else {
      cancelTicketMutation.mutate({ eventId: id, reason });
    }
  };

  // Determine event type based on event data
  const eventType = (event as any)?.eventType?.toLowerCase() === 'social' ? 'social' : 'class';

  // Find the appropriate ticket price - prefer "All Sessions" ticket if available
  const getTicketPrice = () => {
    const ticketTypes = (event as any)?.ticketTypes || [];
    if (ticketTypes.length === 0) return 50; // Default fallback

    // Look for "All" sessions ticket (e.g., "All 2 Days", "All Sessions")
    const allSessionsTicket = ticketTypes.find((tt: any) =>
      tt.name?.toLowerCase().includes('all')
    );

    if (allSessionsTicket) {
      return allSessionsTicket.minPrice || allSessionsTicket.maxPrice || 50;
    }

    // Fall back to first ticket type
    return ticketTypes[0]?.minPrice || ticketTypes[0]?.maxPrice || 50;
  };

  // DEBUG: Log event type determination
  console.log('🔍 EventDetailPage DEBUG:');
  console.log('  - event.eventType:', (event as any)?.eventType);
  console.log('  - determined eventType:', eventType);

  // Determine volunteer box visibility
  const hasVolunteerPositions = Array.isArray(volunteerPositions) && volunteerPositions.length > 0;
  const userVolunteerPositions = Array.isArray(volunteerPositions)
    ? volunteerPositions.filter(p => p.hasUserSignedUp === true)
    : [];
  const hasUserVolunteered = userVolunteerPositions.length > 0;
  const isEventFull = availableSpots <= 0;
  const hasParticipation = participation?.hasRSVP || participation?.hasTicket;
  const isAuthenticated = !!currentUser;

  // Show volunteer encouragement if:
  // - User is logged in
  // - User has NOT already volunteered
  // - Event has volunteer positions available
  // - NOT (event is full AND user doesn't have RSVP/ticket)
  const showVolunteerEncouragement =
    isAuthenticated &&
    !hasUserVolunteered &&
    hasVolunteerPositions &&
    !(isEventFull && !hasParticipation);

  // Scroll to volunteer section
  const handleScrollToVolunteers = () => {
    const volunteerSection = document.getElementById('volunteer-opportunities-section');
    if (volunteerSection) {
      volunteerSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  };

  return (
    <Box data-testid="event-details" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Breadcrumb */}
      <Container size="xl" pt="md">
        <Group justify="space-between" align="center">
          <Breadcrumbs separator="/" mb="md" styles={{
            breadcrumb: {
              color: 'var(--color-stone)',
              fontSize: '14px'
            }
          }}>
            <Anchor
              href="/"
              style={{
                color: 'var(--color-burgundy)',
                textDecoration: 'none',
                transition: 'color 0.3s ease'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.color = 'var(--color-burgundy-dark)';
                e.currentTarget.style.textDecoration = 'underline';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.color = 'var(--color-burgundy)';
                e.currentTarget.style.textDecoration = 'none';
              }}
            >
              Home
            </Anchor>
            <Anchor
              href="/events"
              style={{
                color: 'var(--color-burgundy)',
                textDecoration: 'none',
                transition: 'color 0.3s ease'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.color = 'var(--color-burgundy-dark)';
                e.currentTarget.style.textDecoration = 'underline';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.color = 'var(--color-burgundy)';
                e.currentTarget.style.textDecoration = 'none';
              }}
            >
              Events
            </Anchor>
            <Text style={{ color: 'var(--color-stone)' }}>{(event as any)?.title}</Text>
          </Breadcrumbs>

          {/* Admin Edit Link */}
          {isAdmin && (
            <Link
              to={`/admin/events/${id}`}
              style={{
                color: 'var(--color-error)',
                textDecoration: 'none',
                fontWeight: 700,
                fontSize: '14px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                transition: 'opacity 0.3s ease'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.opacity = '0.8';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.opacity = '1';
              }}
            >
              EDIT
            </Link>
          )}
        </Group>
      </Container>

      <Container size="xl" style={{ 
        display: 'grid', 
        gridTemplateColumns: '1fr 380px', 
        gap: 'var(--space-xl)',
        paddingBottom: 'var(--space-xl)'
      }}>
        {/* Left Column - Event Details */}
        <Stack gap="lg">
          {/* Event Hero Section */}
          <Paper
            data-testid="section-hero"
            style={{
              background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
              borderRadius: '24px',
              padding: 'var(--space-3xl)',
              paddingBottom: 'var(--space-2xl)',
              position: 'relative',
              overflow: 'hidden'
            }}
          >
            {/* Subtle overlay */}
            <Box
              style={{
                position: 'absolute',
                top: '-50%',
                left: '-50%',
                width: '200%',
                height: '200%',
                background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
                transform: 'rotate(45deg)'
              }}
            />
            
            <Box style={{ position: 'relative', zIndex: 1 }}>
              <Title
                order={1}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '48px',
                  fontWeight: 800,
                  color: 'var(--color-ivory)',
                  marginBottom: 'var(--space-md)',
                  lineHeight: 1.2
                }}
              >
                {(event as any)?.title}
              </Title>

              <Group gap="lg" style={{ flexWrap: 'wrap' }}>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconCalendar size={20} />
                  <Text size="lg">{formatEventDate((event as any)?.startDate)}</Text>
                </Group>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconClock size={20} />
                  <Text size="lg">
                    {formatEventTime((event as any)?.startDate)}
                    {(event as any)?.endDate && ` - ${formatEventTime((event as any)?.endDate)}`}
                  </Text>
                </Group>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconMapPin size={20} />
                  <Text size="lg">{(event as any)?.location}</Text>
                </Group>
              </Group>
            </Box>
          </Paper>

          {/* About This Event */}
          <ContentSection>
            <div
              className={styles.eventContent}
              style={{
                fontSize: '17px',
                lineHeight: 1.8,
                color: 'var(--color-charcoal)',
                marginBottom: 'var(--space-md)'
              }}
              dangerouslySetInnerHTML={{ __html: (event as any)?.description || '' }}
            />
          </ContentSection>

          {/* Volunteer Positions */}
          {volunteerPositions && Array.isArray(volunteerPositions) && volunteerPositions.length > 0 && (
            <div id="volunteer-opportunities-section">
              <ContentSection title="Volunteer Opportunities">
                <Stack gap="md">
                  <Text size="sm" c="dimmed" mb="sm">
                    Help make this event a success! Sign up for a volunteer position and you'll automatically be RSVPed to the event.
                  </Text>
                  {volunteerPositions.map((position) => (
                    <VolunteerPositionCard
                      key={position.id}
                      position={position}
                    />
                  ))}
                </Stack>
              </ContentSection>
            </div>
          )}

          {/* Policies */}
          {(event as any)?.policies && (
            <ContentSection>
              <div
                className={styles.eventContent}
                style={{
                  fontSize: '17px',
                  lineHeight: 1.8,
                  color: 'var(--color-charcoal)'
                }}
                dangerouslySetInnerHTML={{ __html: (event as any)?.policies }}
              />
            </ContentSection>
          )}
        </Stack>

        {/* Right Column - Participation Card and Volunteer Boxes */}
        <Box style={{ position: 'sticky', top: '100px', height: 'fit-content' }}>
          <Stack gap="lg">
            {/* Participation Card */}
            <ParticipationCard
              eventId={id!}
              eventTitle={(event as any)?.title || 'Event'}
              eventType={eventType}
              participation={participation}
              isLoading={participationLoading || createRSVPMutation.isPending || cancelRSVPMutation.isPending || cancelTicketMutation.isPending}
              onRSVP={handleRSVP}
              onPurchaseTicket={handlePurchaseTicket}
              onCancel={handleCancel}
              ticketPrice={getTicketPrice()}
              ticketTypes={(event as any)?.ticketTypes || []}
              eventStartDateTime={(event as any)?.startDate}
              eventEndDateTime={(event as any)?.endDate}
              eventInstructor={(event as any)?.instructor}
              eventLocation={(event as any)?.location}
            />

            {/* Volunteer Encouragement Box (if user hasn't volunteered) */}
            {showVolunteerEncouragement && (
              <VolunteerEncouragementBox onScrollToVolunteers={handleScrollToVolunteers} />
            )}

            {/* User's Volunteer Shifts (if user has volunteered) */}
            {hasUserVolunteered && userVolunteerPositions.length > 0 && (
              <UserVolunteerShifts positions={userVolunteerPositions} />
            )}
          </Stack>
        </Box>
      </Container>
    </Box>
  );
};

// Content Section Component
interface ContentSectionProps {
  title?: string;
  children: React.ReactNode;
}

const ContentSection: React.FC<ContentSectionProps> = ({ title, children }) => (
  <Paper
    style={{
      background: 'var(--color-ivory)',
      borderRadius: '16px',
      padding: 'var(--space-xl)',
      boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
      border: '1px solid rgba(183, 109, 117, 0.1)',
      position: 'relative',
      overflow: 'hidden'
    }}
    onMouseEnter={(e) => {
      const shimmer = document.createElement('div');
      shimmer.style.position = 'absolute';
      shimmer.style.top = '-50%';
      shimmer.style.left = '-50%';
      shimmer.style.width = '200%';
      shimmer.style.height = '200%';
      shimmer.style.background = 'linear-gradient(45deg, transparent 30%, rgba(183, 109, 117, 0.05) 50%, transparent 70%)';
      shimmer.style.transform = 'rotate(45deg)';
      shimmer.style.animation = 'shimmer 0.5s ease';
      shimmer.style.pointerEvents = 'none';
      e.currentTarget.appendChild(shimmer);
      setTimeout(() => shimmer.remove(), 500);
    }}
  >
    {title && (
      <Title
        order={2}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '28px',
          fontWeight: 700,
          color: 'var(--color-burgundy)',
          marginBottom: 'var(--space-md)'
        }}
      >
        {title}
      </Title>
    )}
    {children}
  </Paper>
);


// Loading skeleton component
const EventDetailSkeleton: React.FC = () => (
  <Stack gap="xl">
    {/* Hero skeleton */}
    <Paper p="xl" style={{ borderRadius: '24px' }}>
      <Stack gap="md">
        <Skeleton height={24} width={120} />
        <Skeleton height={48} width="80%" />
        <Group gap="lg">
          <Skeleton height={20} width={150} />
          <Skeleton height={20} width={100} />
          <Skeleton height={20} width={200} />
        </Group>
      </Stack>
    </Paper>

    {/* Content skeleton */}
    <Paper p="xl" style={{ borderRadius: '16px' }}>
      <Skeleton height={28} width={200} mb="md" />
      <Stack gap="sm">
        <Skeleton height={16} />
        <Skeleton height={16} />
        <Skeleton height={16} width="80%" />
      </Stack>
    </Paper>
  </Stack>
);