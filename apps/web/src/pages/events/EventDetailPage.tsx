import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Container, Stack, Title, Text, Breadcrumbs,
  Anchor, Alert, Button, Box, Badge, Group, Paper,
  ActionIcon, List, Avatar, Skeleton, Center
} from '@mantine/core';
import {
  IconCalendar, IconClock, IconMapPin, IconUsers,
  IconShare, IconMail, IconBrandX, IconLink, IconCheck
} from '@tabler/icons-react';
import { formatEventDate, formatEventTime } from '../../utils/eventUtils';
import { useEvent } from '../../lib/api/hooks/useEvents';
import { useParticipation, useCreateRSVP, useCancelRSVP } from '../../hooks/useParticipation';
import { ParticipationCard } from '../../components/events/ParticipationCard';
import type { EventDto } from '../../lib/api/types/events.types';

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [selectedTicket, setSelectedTicket] = useState('single');

  const { data: event, isLoading, error } = useEvent(id!, !!id);
  const { data: participation, isLoading: participationLoading } = useParticipation(id!, !!id);
  const createRSVPMutation = useCreateRSVP();
  const cancelRSVPMutation = useCancelRSVP();
  
  if (isLoading) {
    return (
      <Box data-testid="page-event-detail" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
        <Container size="xl" py="xl">
          <EventDetailSkeleton />
        </Container>
      </Box>
    );
  }
  
  if (error || !event) {
    return (
      <Box data-testid="page-event-detail" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
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
      // TODO: Implement ticket cancellation
      console.log('Cancel ticket for:', id, 'Reason:', reason);
    }
  };

  // Determine event type based on event data
  const eventType = (event as any)?.eventType?.toLowerCase() === 'social' ? 'social' : 'class';

  // DEBUG: Log event type determination
  console.log('🔍 EventDetailPage DEBUG:');
  console.log('  - event.eventType:', (event as any)?.eventType);
  console.log('  - determined eventType:', eventType);

  return (
    <Box data-testid="page-event-detail" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Breadcrumb */}
      <Container size="xl" pt="md">
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
            Classes
          </Anchor>
          <Text style={{ color: 'var(--color-stone)' }}>{(event as any)?.title}</Text>
        </Breadcrumbs>
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
              <Badge
                size="lg"
                style={{
                  background: 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)',
                  color: 'var(--color-midnight)',
                  padding: '6px 20px',
                  borderRadius: '20px',
                  fontSize: '14px',
                  fontWeight: 700,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  marginBottom: 'var(--space-md)',
                  boxShadow: '0 2px 10px rgba(255, 191, 0, 0.3)'
                }}
              >
                Event
              </Badge>

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
                  <Text size="lg">{formatEventTime((event as any)?.startDate)}</Text>
                </Group>
                <Group gap="xs" style={{ color: 'var(--color-dusty-rose)', fontSize: '20px' }}>
                  <IconMapPin size={20} />
                  <Text size="lg">{(event as any)?.location}</Text>
                </Group>
              </Group>
            </Box>
          </Paper>

          {/* About This Event */}
          <ContentSection title="About This Event">
            <Text 
              style={{
                fontSize: '17px',
                lineHeight: 1.8,
                color: 'var(--color-charcoal)',
                marginBottom: 'var(--space-md)'
              }}
            >
              {(event as any)?.description}
            </Text>

            <Title 
              order={2}
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '28px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginTop: 'var(--space-lg)',
                marginBottom: 'var(--space-md)'
              }}
            >
              Event Details
            </Title>

            <Group justify="space-between" style={{ padding: '12px 0', borderBottom: '1px solid var(--color-cream)' }}>
              <Text style={{ color: 'var(--color-stone)', fontSize: '16px' }}>Start Time</Text>
              <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>
                {formatEventDate((event as any)?.startDate)} at {formatEventTime((event as any)?.startDate)}
              </Text>
            </Group>
            
            {(event as any)?.endDate && (
              <Group justify="space-between" style={{ padding: '12px 0', borderBottom: '1px solid var(--color-cream)' }}>
                <Text style={{ color: 'var(--color-stone)', fontSize: '16px' }}>End Time</Text>
                <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>
                  {formatEventDate((event as any)?.endDate)} at {formatEventTime((event as any)?.endDate)}
                </Text>
              </Group>
            )}
            
            <Group justify="space-between" style={{ padding: '12px 0', borderBottom: '1px solid var(--color-cream)' }}>
              <Text style={{ color: 'var(--color-stone)', fontSize: '16px' }}>Location</Text>
              <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>{(event as any)?.location}</Text>
            </Group>
            
            <Group justify="space-between" style={{ padding: '12px 0' }}>
              <Text style={{ color: 'var(--color-stone)', fontSize: '16px' }}>Capacity</Text>
              <Text fw={600} style={{ color: 'var(--color-charcoal)' }}>{(event as any)?.capacity || 'Unlimited'}</Text>
            </Group>
          </ContentSection>

          {/* Important Policies */}
          <ContentSection title="Important Policies">
            <Text mb="md">
              <strong>Refund Policy:</strong> Full refund available up to 48 hours before the event. No refunds within 48 hours of start time.
            </Text>
            <Text mb="md">
              <strong>Age Requirement:</strong> All participants must be 21 or older.
            </Text>
            <Text>
              <strong>Code of Conduct:</strong> All attendees must follow our{' '}
              <Anchor href="#" style={{ color: 'var(--color-burgundy)', textDecoration: 'underline' }}>
                Code of Conduct
              </Anchor>.
            </Text>
          </ContentSection>
        </Stack>

        {/* Right Column - Participation Card */}
        <Box style={{ position: 'sticky', top: '100px', height: 'fit-content' }}>
          <ParticipationCard
            eventId={id!}
            eventTitle={(event as any)?.title || 'Event'}
            eventType={eventType}
            participation={participation}
            isLoading={participationLoading || createRSVPMutation.isPending || cancelRSVPMutation.isPending}
            onRSVP={handleRSVP}
            onPurchaseTicket={handlePurchaseTicket}
            onCancel={handleCancel}
            ticketPrice={(event as any)?.capacity ? Math.round(50 + (availableSpots / (event as any).capacity) * 25) : 50}
          />
        </Box>
      </Container>
    </Box>
  );
};

// Content Section Component
interface ContentSectionProps {
  title: string;
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