import React, { useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { Container, Stack, Title, Text, Breadcrumbs, Anchor, Alert, Button, Box, Group, Paper, Skeleton, Grid, Badge } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconExternalLink, IconTicket } from '@tabler/icons-react';
import { formatUtcToLocalDate, formatUtcTimeRange, getTicketSessionInfo } from '../../utils/eventUtils';
import { useEvent } from '../../lib/api/hooks/useEvents';
import { useParticipation, useCreateRSVP, useCancelRSVP, useCancelTicket } from '../../hooks/useParticipation';
import { ParticipationCard, type ParticipationCardProps } from '../../components/events/ParticipationCard';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import { useVolunteerPositions } from '../../features/volunteers/hooks/useVolunteerPositions';
import { VolunteerPositionCard } from '../../features/volunteers/components/VolunteerPositionCard';
import { VolunteerEncouragementBox } from '../../components/events/VolunteerEncouragementBox';
import { UserVolunteerShifts } from '../../components/events/UserVolunteerShifts';
import { useVenue } from '../../lib/api/hooks/useVenues';
import { useTeacherProfiles } from '../../lib/api/hooks/useTeacherProfiles';
import type { EventDto, EventSessionDto, EventTicketTypeDto } from '../../lib/api/types/events.types';
import type { EnhancedParticipationStatusDto } from '../../types/participation.types';
import type { UserDto } from '../../lib/api/types/auth.types';
import type { components } from '@witchcityrope/shared-types';
import { hasRole, hasAnyRole } from '../../utils/roleUtils';
import styles from './EventDetailPage.module.css'
import { sanitizeHtml } from '../../lib/utils/sanitizeHtml'
import { useEventTimeZone } from '../../hooks/useEventTimeZone';
import { ProxyRsvpSection } from '../../features/proxy-rsvp/components/ProxyRsvpSection';

type VenueDto = components['schemas']['VenueDto'];
type UserProfileDto = components['schemas']['UserProfileDto'];

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string}>();
  const navigate = useNavigate();
  const eventTimeZone = useEventTimeZone();
  const isMobile = useMediaQuery('(max-width: 991px)');

  // Scroll to top when page loads or event ID changes
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [id]);

  const { data: eventData, isLoading, error } = useEvent(id!, !!id);
  const event = eventData as EventDto | undefined;
  const { data: currentUserData } = useCurrentUser();
  const currentUser = currentUserData as UserDto | undefined;
  const isAuthenticated = !!currentUser;
  const { data: participationData, isLoading: participationLoading } = useParticipation(id!, isAuthenticated, !!id);
  const participation = participationData as EnhancedParticipationStatusDto | undefined;
  const { data: volunteerPositions } = useVolunteerPositions(id!, !!id);
  const { data: venueData } = useVenue(event?.venueId, !!event);
  const venue = venueData as VenueDto | null | undefined;
  const { data: teachersData = [] } = useTeacherProfiles(event?.teacherIds);
  const teachers = teachersData as UserProfileDto[];
  const createRSVPMutation = useCreateRSVP();
  const cancelRSVPMutation = useCancelRSVP();
  const cancelTicketMutation = useCancelTicket();

  // Check if current user is admin (multi-role aware)
  const isAdmin = hasRole(currentUser, 'Administrator');
  
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

  const availableSpots = (event.capacity || 0) - (event.registrationCount || 0);
  

  const handleRSVP = (notes?: string, eventWaiverAccepted?: boolean) => {
    if (!id) return;
    createRSVPMutation.mutate({
      eventId: id,
      notes,
      eventWaiverAccepted: eventWaiverAccepted || false
    });
  };

  const handleCancel = (type: 'rsvp' | 'ticket', reason?: string, ticketPurchaseIds?: string[]) => {
    if (!id) return;

    if (type === 'rsvp') {
      cancelRSVPMutation.mutate({ eventId: id, reason });
    } else if (ticketPurchaseIds && ticketPurchaseIds.length > 0) {
      cancelTicketMutation.mutate({ eventId: id, reason, ticketPurchaseIds });
    }
  };

  // Navigate to checkout page — same handler and destination as the ParticipationCard's
  // "Purchase Ticket" button, reused here so the Ticket Options box has its own purchase entry point.
  const handleTicketPurchase = () => {
    navigate(`/checkout/${id}`, {
      state: {
        eventInfo: {
          id,
          title: event.title || 'Event',
          startDateTime: event.startDate || new Date().toISOString(),
          endDateTime: event.endDate || new Date().toISOString(),
          instructor: undefined,
          location: event.venueLocation,
          price: getTicketPriceDisplay().min
        },
        // Pass owned session IDs so checkout can filter out tickets the user already has
        ownedSessionIds: participation?.ownedSessionIds || []
      }
    });
  };

  // Extract boolean flags from event data
  const allowRsvps = event.allowRsvps ?? false;
  const requireTicketPurchase = event.requireTicketPurchase ?? true;
  const vettedMembersOnly = event.vettedMembersOnly ?? false;

  // Calculate ticket price display based on all available ticket types
  const getTicketPriceDisplay = (): { min: number; max: number; isSinglePrice: boolean } => {
    const ticketTypes: EventTicketTypeDto[] = (event.ticketTypes || []) as EventTicketTypeDto[];
    if (ticketTypes.length === 0) return { min: 50, max: 50, isSinglePrice: true }; // Default fallback

    let minPrice = Infinity;
    let maxPrice = -Infinity;

    ticketTypes.forEach((tt: EventTicketTypeDto) => {
      // For sliding scale tickets, use minPrice and maxPrice
      if (tt.pricingType === 'SlidingScale') {
        if (tt.minPrice != null) {
          minPrice = Math.min(minPrice, tt.minPrice);
        }
        if (tt.maxPrice != null) {
          maxPrice = Math.max(maxPrice, tt.maxPrice);
        }
      }
      // For fixed price tickets, use the price for both min and max
      else if (tt.price != null) {
        minPrice = Math.min(minPrice, tt.price);
        maxPrice = Math.max(maxPrice, tt.price);
      }
    });

    // If no valid prices found, use default
    if (minPrice === Infinity || maxPrice === -Infinity) {
      return { min: 50, max: 50, isSinglePrice: true };
    }

    // Check if all tickets have the same price
    const isSinglePrice = minPrice === maxPrice;

    return { min: minPrice, max: maxPrice, isSinglePrice };
  };

  // Filter and categorize ticket types based on timing, availability, and user ownership.
  //
  // Three categories:
  //   1. Purchased — user already owns a ticket covering ANY session this ticket covers
  //   2. Available — canPurchase is true AND no session overlap with owned tickets
  //   3. Unavailable — timing window closed or all sessions passed (canPurchase is false)
  //
  // Session overlap check: ticket types expose sessionIdentifiers (codes like "Default"),
  // while participation.ownedSessionIds contains GUIDs. We bridge them via event.sessions
  // which has both id (GUID) and sessionIdentifier (code).
  const ticketTypes: EventTicketTypeDto[] = (event.ticketTypes || []) as EventTicketTypeDto[];
  const eventSessions: EventSessionDto[] = event.sessions || [];
  const ownedSessionIds: string[] = participation?.ownedSessionIds || [];

  // Build lookup: session code → session GUID so we can compare ticket type session codes
  // against the user's owned session GUIDs
  const sessionCodeToId: Record<string, string> = {};
  eventSessions.forEach((s: EventSessionDto) => {
    if (s.sessionIdentifier && s.id) {
      sessionCodeToId[s.sessionIdentifier] = s.id;
    }
  });

  // Check if a ticket type covers ANY session the user already owns
  const isTicketOwnedByUser = (tt: EventTicketTypeDto): boolean => {
    const codes: string[] = tt.sessionIdentifiers || [];
    if (codes.length === 0 || ownedSessionIds.length === 0) return false;
    return codes.some((code: string) => {
      const sessionId = sessionCodeToId[code];
      return sessionId && ownedSessionIds.includes(sessionId);
    });
  };

  // Only show ticket types that have future sessions (canPurchase or referenceSessionId exists)
  // AND the user doesn't already own a ticket covering any of this ticket's sessions —
  // if they do, the ticket type is no longer relevant and is hidden entirely.
  const displayableTickets: EventTicketTypeDto[] = ticketTypes.filter((tt: EventTicketTypeDto) =>
    (tt.canPurchase || tt.referenceSessionId) && !isTicketOwnedByUser(tt)
  );

  // Separate into purchasable and unavailable tickets
  const purchasableTickets: EventTicketTypeDto[] = displayableTickets.filter((tt: EventTicketTypeDto) => tt.canPurchase);
  const unavailableTickets: EventTicketTypeDto[] = displayableTickets.filter((tt: EventTicketTypeDto) => !tt.canPurchase);

  // Determine volunteer box visibility
  const hasVolunteerPositions = Array.isArray(volunteerPositions) && volunteerPositions.length > 0;
  const userVolunteerPositions = Array.isArray(volunteerPositions)
    ? volunteerPositions.filter(p => p.hasUserSignedUp === true)
    : [];
  const hasUserVolunteered = userVolunteerPositions.length > 0;
  const isEventFull = availableSpots <= 0;
  const hasParticipation = participation?.hasRSVP || participation?.hasTicket;

  // Check if user is vetted (same logic as ParticipationCard)
  const isVetted = currentUser?.isVetted === true ||
    hasAnyRole(currentUser, ['Administrator', 'Teacher']);

  // Allow volunteering if:
  // - For social events (RSVP only, no ticket required): User must be vetted
  // - For classes (ticket required): User must have a ticket purchased (any ticket holder can volunteer)
  const canVolunteerBasedOnEventType = (allowRsvps && !requireTicketPurchase)
    ? isVetted
    : participation?.hasTicket;

  // Check if ANY volunteer position has signup window open
  // If ALL positions have canSignUp === false, don't show encouragement box
  const hasOpenSignupPositions = Array.isArray(volunteerPositions) &&
    volunteerPositions.some(p => p.canSignUp !== false);

  // Show volunteer encouragement if:
  // - User is logged in
  // - User can volunteer based on event type (social: vetted, class: has ticket)
  // - User has NOT already volunteered
  // - Event has volunteer positions available
  // - At least ONE position has signup window open (canSignUp !== false)
  // - NOT (event is full AND user doesn't have RSVP/ticket)
  const showVolunteerEncouragement =
    isAuthenticated &&
    canVolunteerBasedOnEventType &&
    !hasUserVolunteered &&
    hasVolunteerPositions &&
    hasOpenSignupPositions &&
    !(isEventFull && !hasParticipation);

  // Scroll to volunteer section
  const handleScrollToVolunteers = () => {
    const volunteerSection = document.getElementById('volunteer-opportunities-section');
    if (volunteerSection) {
      volunteerSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  };

  // Determine if user can purchase tickets from ticket options section
  const canPurchaseFromOptions: boolean =
    purchasableTickets.length > 0 && isAuthenticated && Boolean(
      (participation?.canPurchaseTicket && !participation?.hasTicket) ||
      participation?.canPurchaseAdditionalSessions
    );

  // Extract ParticipationCard props for reuse (DRY pattern)
  const participationCardProps: Omit<ParticipationCardProps, 'isMobile'> = {
    eventId: id!,
    eventTitle: event.title || 'Event',
    allowRsvps,
    requireTicketPurchase,
    vettedMembersOnly,
    participation: participation ?? null,
    isLoading: participationLoading || createRSVPMutation.isPending || cancelRSVPMutation.isPending || cancelTicketMutation.isPending,
    ticketTypeId: event.ticketTypes?.[0]?.id,
    onRSVP: handleRSVP,
    onPurchaseTicket: () => { /* PayPal integration handles ticket creation */ },
    onCancel: handleCancel,
    ticketPrice: getTicketPriceDisplay().min,
    ticketPriceRange: getTicketPriceDisplay(),
    eventStartDateTime: event.startDate,
    eventEndDateTime: event.endDate,
    eventInstructor: undefined,
    eventLocation: event.venueLocation ?? undefined,
    eventSessions: event.sessions,
  };

  // Ticket options section rendered outside JSX to avoid TypeScript inference depth limit
  const ticketOptionsSection: React.ReactNode = displayableTickets.length > 0 ? (
    <ContentSection title="Ticket Options" isMobile={isMobile}>
      <Stack gap="md">
        {/* Purchasable Tickets — available and user does not own any overlapping sessions */}
        {purchasableTickets.length > 0 && (
          <Stack gap="sm">
            {purchasableTickets.map((ticket: EventTicketTypeDto) => (
              <Paper
                key={ticket.id}
                p="md"
                style={{
                  background: 'var(--color-cream)',
                  border: '1px solid var(--color-plum)',
                  borderRadius: '8px'
                }}
              >
                <Stack gap={0}>
                  {/* Title row: ticket name, price, and badge */}
                  <Group justify="space-between" align="center" wrap="nowrap">
                    <Text fw={600} size="md">
                      {ticket.name}
                      {' · '}
                      <Text span c="dimmed">
                        {ticket.pricingType === 'SlidingScale'
                          ? `$${ticket.minPrice} - $${ticket.maxPrice}`
                          : `$${ticket.price}`
                        }
                      </Text>
                    </Text>
                    <Badge color="green" variant="light" style={{ flexShrink: 0 }}>
                      Available Now
                    </Badge>
                  </Group>

                  {/* Session names — resolved from sessionIdentifiers via shared utility.
                      Shows all sessions this ticket covers (important for multi-session tickets). */}
                  {(() => {
                    const sessionInfo = getTicketSessionInfo(
                      ticket.sessionIdentifiers || [], eventSessions
                    );
                    if (sessionInfo.length > 0) {
                      return (
                        <Stack gap={2} mt={2}>
                          {sessionInfo.map((session, idx) => (
                            <Text key={idx} size="md" c="dimmed">
                              {session.name} - {session.date}
                            </Text>
                          ))}
                        </Stack>
                      );
                    }
                    return null;
                  })()}

                  {/* Bottom row: availability (left) and purchase button (right) —
                      matches volunteer card layout with spots filled + sign up button */}
                  <Group justify="space-between" align="center" mt={4}>
                    <Text size="md" c="dimmed">
                      {(ticket.quantityAvailable ?? 0) - (ticket.quantitySold ?? 0)} / {ticket.quantityAvailable ?? 0} available
                    </Text>
                    {canPurchaseFromOptions && (
                      <Button
                        onClick={handleTicketPurchase}
                        variant="filled"
                        color="blue"
                        fullWidth={isMobile}
                        leftSection={<IconTicket size={18} />}
                        data-testid="ticket-options-purchase-button"
                        styles={{
                          root: {
                            height: '44px',
                            paddingTop: '12px',
                            paddingBottom: '12px',
                            fontSize: '14px',
                            lineHeight: '1.2'
                          }
                        }}
                      >
                        Purchase Ticket
                      </Button>
                    )}
                  </Group>
                </Stack>
              </Paper>
            ))}
          </Stack>
        )}

        {/* Unavailable Tickets (not yet open or closed, and not already owned) */}
        {unavailableTickets.length > 0 && (
          <>
            {purchasableTickets.length > 0 && (
              <Text size="md" c="dimmed" mt="md">Other ticket options:</Text>
            )}
            <Stack gap="sm">
              {unavailableTickets.map((ticket: EventTicketTypeDto) => (
                <Paper
                  key={ticket.id}
                  p="md"
                  style={{
                    background: 'rgba(136, 1, 36, 0.05)',
                    border: '1px solid rgba(136, 1, 36, 0.1)',
                    borderRadius: '8px',
                    opacity: 0.7
                  }}
                >
                  <Stack gap={0}>
                    {/* Title row with badge */}
                    <Group justify="space-between" align="center" wrap="nowrap">
                      <Text fw={500} size="md">{ticket.name}</Text>
                      <Badge color="gray" variant="light" style={{ flexShrink: 0 }}>
                        Not Available
                      </Badge>
                    </Group>

                    {/* Session names — resolved from sessionIdentifiers via shared utility */}
                    {(() => {
                      const sessionInfo = getTicketSessionInfo(
                        ticket.sessionIdentifiers || [], eventSessions
                      );
                      if (sessionInfo.length > 0) {
                        return (
                          <Stack gap={2} mt={2}>
                            {sessionInfo.map((session, idx) => (
                              <Text key={idx} size="md" c="dimmed">
                                {session.name} - {session.date}
                              </Text>
                            ))}
                          </Stack>
                        );
                      }
                      return null;
                    })()}

                    {/* Availability message */}
                    {ticket.availabilityMessage && (
                      <Text size="md" c="dimmed" mt={2}>
                        {ticket.availabilityMessage}
                      </Text>
                    )}
                  </Stack>
                </Paper>
              ))}
            </Stack>
          </>
        )}

        {/* All ticket sales ended */}
        {displayableTickets.length === 0 && ticketTypes.length > 0 && (
          <Alert color="gray" variant="light">
            <Text size="sm">
              All ticket sales have ended for this event.
            </Text>
          </Alert>
        )}

        {/* Purchase button is now inside each individual ticket card */}
      </Stack>
    </ContentSection>
  ) : null;

  // Venue section rendered outside JSX to avoid TypeScript inference depth limit
  const venueSection: React.ReactNode = venue ? (() => {
    const hasVenueAccess = isVetted || (participation?.hasRSVP || participation?.hasTicket);

    if (hasVenueAccess && venue.directions) {
      return (
        <ContentSection isMobile={isMobile}>
          <div className="html-content">
            <h2>Venue - {venue.name}</h2>
            <div dangerouslySetInnerHTML={{ __html: venue.directions }} />
            {venue.venueInformation && (
              <>
                <h3>Venue Information</h3>
                <p style={{ whiteSpace: 'pre-line' }}>{venue.venueInformation}</p>
              </>
            )}
          </div>
        </ContentSection>
      );
    } else if (!hasVenueAccess && event.venueLocation) {
      return (
        <ContentSection title={`Location - ${event.venueLocation}`} isMobile={isMobile}>
          <Stack gap="md">
            <Alert
              color="blue"
              variant="light"
              styles={{
                root: {
                  border: '1px solid var(--color-plum)',
                  background: 'rgba(155, 74, 117, 0.05)',
                },
                message: {
                  color: 'var(--color-charcoal)',
                },
              }}
            >
              <Text size="sm">
                Full venue address and directions will be provided once a ticket is purchased or you have RSVPed to the event.
              </Text>
            </Alert>
          </Stack>
        </ContentSection>
      );
    }

    return null;
  })() : null;

  return (
    <Box data-testid="event-details" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Breadcrumb */}
      <Container size="xl" pt={{ base: 'xs', md: 'md' }} pb={{ base: 'xs', md: 0 }} px={{ base: 'sm', md: 'xl' }}>
        <Group justify="space-between" align="center">
          <Breadcrumbs separator="/" mb={0} styles={{
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
            <Text style={{ color: 'var(--color-stone)' }}>{event.title}</Text>
          </Breadcrumbs>

          {/* Admin Edit Link */}
          {isAdmin && (
            <Link
              to={`/admin/events/${id}`}
              style={{
                color: 'var(--mantine-color-wcr-7)',
                textDecoration: 'none',
                fontWeight: 600,
                display: 'flex',
                alignItems: 'center',
                gap: '4px'
              }}
            >
              Edit <IconExternalLink size={16} />
            </Link>
          )}
        </Group>
      </Container>

      <Container
        size="xl"
        px={{ base: 0, md: 'xl' }}
        pt={{ base: 0, md: 'md' }}
        pb={{ base: 0, md: 'xl' }}
      >
        {/* Event Hero Section - FULL WIDTH */}
        <Paper
          data-testid="section-hero"
          px={{ base: 16, md: 48 }}
          py={{ base: 16, md: 48 }}
          style={{
            background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
            borderRadius: isMobile ? 0 : '24px',
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
            {/* Title row with location on right */}
            <Group justify="flex-start" align="flex-start" wrap="wrap" gap="md">
              <Title
                order={1}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: 'clamp(1.75rem, 1.11vw + 1.39rem, 3rem)', // 28px mobile → 48px desktop
                  fontWeight: 800,
                  color: 'var(--color-ivory)',
                  lineHeight: 1.2
                }}
              >
                {event.title}
              </Title>

            </Group>

            <div className="html-content-light">
              {/* Session date/times - one line each */}
              {(() => {
                const sessions = (event.sessions || []).slice().sort((a: EventSessionDto, b: EventSessionDto) =>
                  new Date(a.startTime || '').getTime() - new Date(b.startTime || '').getTime()
                );

                if (sessions.length === 0) {
                  return <h3>Date and Time coming soon</h3>;
                }

                return sessions.map((session: EventSessionDto, index: number) => (
                  <h3 key={session.id || index}>
                    <span className="date-part">
                      {formatUtcToLocalDate(session.startTime || '', eventTimeZone, { weekday: 'long', month: 'short', day: 'numeric' })}
                    </span>
                    <span className="time-part">
                      {formatUtcTimeRange(session.startTime || '', session.endTime || '', eventTimeZone)}
                    </span>
                  </h3>
                ));
              })()}
            </div>
          </Box>
        </Paper>

        {/* Mobile Participation Card - FULL WIDTH, mobile only */}
        <Box hiddenFrom="md" mt={{ base: 0, md: 'lg' }}>
          <ParticipationCard {...participationCardProps} isMobile={isMobile} />
          {/* Proxy RSVP Section - mobile */}
          <Box px="md" mt="xs">
            <ProxyRsvpSection
              eventId={id!}
              eventTitle={event.title || 'Event'}
              allowRsvps={allowRsvps}
              isAuthenticated={isAuthenticated}
              vettedMembersOnly={vettedMembersOnly}
              isVetted={isVetted}
              hasParticipation={!!hasParticipation}
              isAtCapacity={isEventFull}
              remainingPerPerson={participation?.remainingPerPerson}
            />
          </Box>
        </Box>

        {/* Mobile Volunteer Shifts - FULL WIDTH, mobile only */}
        {hasUserVolunteered && userVolunteerPositions.length > 0 && (
          <Box hiddenFrom="md" mt={0}>
            <UserVolunteerShifts
              positions={userVolunteerPositions}
              eventId={id!}
              isMobile={isMobile}
            />
          </Box>
        )}

        {/* Main Content Grid - TWO COLUMNS on desktop */}
        <Grid gutter={{ base: 0, md: 'xl' }} mt={{ base: 0, md: 'lg' }}>
          {/* Left Column - Event Details */}
          <Grid.Col span={{ base: 12, md: 8 }}>
            <Stack gap={isMobile ? 0 : 'md'}>
              <>
          {/* About This Event */}
          <ContentSection isMobile={isMobile}>
            <div
              className="html-content"
              dangerouslySetInnerHTML={{ __html: sanitizeHtml(event.description || '') }}
            />
          </ContentSection>

          {/* Ticket Options Section - Session-based timing */}
          {ticketOptionsSection}

          {/* Venue Details - Conditional based on access */}
          {venueSection}

          {/* Volunteer Positions */}
          {volunteerPositions && Array.isArray(volunteerPositions) && volunteerPositions.length > 0 && isAuthenticated && canVolunteerBasedOnEventType && (
            <div id="volunteer-opportunities-section">
              <ContentSection isMobile={isMobile}>
                <div className="html-content">
                  <h2>Volunteer Opportunities</h2>
                  <p style={{ marginBottom: 0 }}>
                    Help make this event a success! Sign up for a volunteer position and you&apos;ll automatically be RSVPed to the event.
                  </p>
                </div>
                <Stack gap="md" mt={0}>
                  {[...volunteerPositions]
                    .sort((a, b) => {
                      // Sort by sessionStartTime, soonest first
                      const timeA = a.sessionStartTime ? new Date(a.sessionStartTime).getTime() : Infinity;
                      const timeB = b.sessionStartTime ? new Date(b.sessionStartTime).getTime() : Infinity;
                      return timeA - timeB;
                    })
                    .map((position) => (
                      <VolunteerPositionCard
                        key={position.id}
                        position={position}
                        hasExistingParticipation={participation?.hasRSVP || participation?.hasTicket || false}
                      />
                    ))}
                </Stack>
              </ContentSection>
            </div>
          )}

          {/* Teachers Section - Always visible */}
          {teachers.length > 0 && (
            <ContentSection title="Teachers" isMobile={isMobile}>
              <Stack gap="lg">
                {teachers.map((teacher) => (
                  <Box key={teacher.userId}>
                    <Text
                      style={{
                        fontFamily: 'var(--font-heading)',
                        fontSize: 'clamp(1.125rem, 0.56vw + 0.95rem, 1.25rem)', // 18px mobile → 20px desktop
                        fontWeight: 700,
                        color: 'var(--color-burgundy)',
                        marginBottom: 'var(--space-xs)',
                      }}
                    >
                      {teacher.sceneName || `${teacher.firstName} ${teacher.lastName}`}
                    </Text>
                    {teacher.bio && (
                      <Text
                        style={{
                          fontSize: 'clamp(1rem, 0.19vw + 0.94rem, 1.06rem)', // 16px mobile → 17px desktop
                          lineHeight: 1.8,
                          color: 'var(--color-charcoal)',
                          whiteSpace: 'pre-line',
                        }}
                      >
                        {teacher.bio}
                      </Text>
                    )}
                  </Box>
                ))}
              </Stack>
            </ContentSection>
          )}

          {/* Policies */}
          {event.policies && (
            <ContentSection isMobile={isMobile}>
              <div
                className="html-content"
                dangerouslySetInnerHTML={{ __html: sanitizeHtml(event.policies || '') }}
              />
            </ContentSection>
          )}
              </>
            </Stack>
          </Grid.Col>

          {/* Right Column - Participation Card and Volunteer Boxes */}
          <Grid.Col span={{ base: 12, md: 4 }}>
            <Box
              pos={{ md: 'sticky' }}
              top={{ md: 20 }}
              style={{ height: 'fit-content' }}
            >
          <Stack gap="md">
            {/* Desktop Participation Card - Shown only on desktop */}
            <Box visibleFrom="md">
              <ParticipationCard {...participationCardProps} />
              {/* Proxy RSVP Section - desktop */}
              <ProxyRsvpSection
                eventId={id!}
                eventTitle={event.title || 'Event'}
                allowRsvps={allowRsvps}
                isAuthenticated={isAuthenticated}
                vettedMembersOnly={vettedMembersOnly}
                isVetted={isVetted}
                hasParticipation={!!hasParticipation}
                isAtCapacity={isEventFull}
                remainingPerPerson={participation?.remainingPerPerson}
              />
            </Box>

            {/* Volunteer Encouragement Box (if user hasn't volunteered) - Desktop only */}
            {showVolunteerEncouragement && (
              <Box visibleFrom="md">
                <VolunteerEncouragementBox onScrollToVolunteers={handleScrollToVolunteers} />
              </Box>
            )}

            {/* User's Volunteer Shifts (if user has volunteered) - Desktop only */}
            {hasUserVolunteered && userVolunteerPositions.length > 0 && (
              <Box visibleFrom="md">
                <UserVolunteerShifts
                  positions={userVolunteerPositions}
                  eventId={id!}
                />
              </Box>
            )}
              </Stack>
            </Box>
          </Grid.Col>
        </Grid>
      </Container>
    </Box>
  );
};

// Content Section Component
interface ContentSectionProps {
  title?: string;
  children: React.ReactNode;
  isMobile?: boolean;
}

const ContentSection: React.FC<ContentSectionProps> = ({ title, children, isMobile }) => (
  <Paper
    className={styles.contentSection}
    px={{ base: 0, md: 40 }}
    pt={{ base: 0, md: 30 }}
    pb={{ base: 0, md: 40 }}
    style={{
      background: 'var(--color-ivory)',
      borderRadius: isMobile ? 0 : '16px',
      boxShadow: isMobile ? 'none' : '0 4px 12px rgba(0,0,0,0.05)',
      border: isMobile ? 'none' : '1px solid rgba(183, 109, 117, 0.1)',
      borderBottom: isMobile ? '1px solid rgba(183, 109, 117, 0.1)' : undefined
    }}
  >
    {title && <div className="html-content"><h2>{title}</h2></div>}
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