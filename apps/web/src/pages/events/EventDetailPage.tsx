import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Container, Stack, Title, Text, Breadcrumbs,
  Anchor, Alert, Button, Box, Group, Paper,
  ActionIcon, List, Avatar, Skeleton, Center, Grid, Badge
} from '@mantine/core';
import {
  IconCalendar, IconClock, IconMapPin, IconUsers,
  IconShare, IconMail, IconBrandX, IconLink, IconCheck
} from '@tabler/icons-react';
import { formatUtcToLocalDate, formatUtcTimeRange, formatAbbreviatedDate } from '../../utils/eventUtils';
import { useEvent } from '../../lib/api/hooks/useEvents';
import { useParticipation, useCreateRSVP, useCancelRSVP, useCancelTicket } from '../../hooks/useParticipation';
import { ParticipationCard } from '../../components/events/ParticipationCard';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import type { EventDto } from '../../lib/api/types/events.types';
import { useVolunteerPositions } from '../../features/volunteers/hooks/useVolunteerPositions';
import { VolunteerPositionCard } from '../../features/volunteers/components/VolunteerPositionCard';
import { VolunteerEncouragementBox } from '../../components/events/VolunteerEncouragementBox';
import { UserVolunteerShifts } from '../../components/events/UserVolunteerShifts';
import { useVenue } from '../../lib/api/hooks/useVenues';
import { useTeacherProfiles } from '../../lib/api/hooks/useTeacherProfiles';
import type { components } from '@witchcityrope/shared-types';
import styles from './EventDetailPage.module.css'
import { useEventTimeZone } from '../../hooks/useEventTimeZone';

type VenueDto = components['schemas']['VenueDto'];
type UserProfileDto = components['schemas']['UserProfileDto'];

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string}>();
  const eventTimeZone = useEventTimeZone();
  const [selectedTicket, setSelectedTicket] = useState('single');

  // Scroll to top when page loads or event ID changes
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [id]);

  const { data: event, isLoading, error } = useEvent(id!, !!id);
  const { data: currentUser } = useCurrentUser();
  const isAuthenticated = !!currentUser;
  const { data: participation, isLoading: participationLoading } = useParticipation(id!, isAuthenticated, !!id);
  const { data: volunteerPositions, isLoading: volunteerLoading } = useVolunteerPositions(id!, !!id);
  const { data: venue, isLoading: venueLoading } = useVenue((event as any)?.venueId, !!event) as { data: VenueDto | null; isLoading: boolean };
  const { data: teachers = [], isLoading: teachersLoading } = useTeacherProfiles((event as any)?.teacherIds) as { data: UserProfileDto[]; isLoading: boolean };
  const createRSVPMutation = useCreateRSVP();
  const cancelRSVPMutation = useCancelRSVP();
  const cancelTicketMutation = useCancelTicket();

  // Check if current user is admin (type-safe using auto-generated UserRole)
  type UserRole = components['schemas']['UserRole'];
  const isAdmin = (currentUser as any)?.role === ('Administrator' as UserRole);
  
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

  const handleRSVP = (notes?: string, eventWaiverAccepted?: boolean) => {
    if (!id) return;
    createRSVPMutation.mutate({
      eventId: id,
      notes,
      eventWaiverAccepted: eventWaiverAccepted || false
    });
  };

  const handlePurchaseTicket = (amount: number, slidingScalePercentage?: number) => {
    // PayPal integration handles ticket creation
  };

  const handleCancel = (type: 'rsvp' | 'ticket', reason?: string, sessionIds?: string[]) => {
    if (!id) return;

    if (type === 'rsvp') {
      cancelRSVPMutation.mutate({ eventId: id, reason });
    } else {
      cancelTicketMutation.mutate({ eventId: id, reason, sessionIds });
    }
  };

  // Determine event type based on event data
  const eventType = (event as any)?.eventType?.toLowerCase() === 'social' ? 'social' : 'class';

  // Calculate ticket price display based on all available ticket types
  const getTicketPriceDisplay = (): { min: number; max: number; isSinglePrice: boolean } => {
    const ticketTypes = (event as any)?.ticketTypes || [];
    if (ticketTypes.length === 0) return { min: 50, max: 50, isSinglePrice: true }; // Default fallback

    let minPrice = Infinity;
    let maxPrice = -Infinity;

    ticketTypes.forEach((tt: any) => {
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

  // Filter and categorize ticket types based on new session-based timing fields
  const ticketTypes = (event as any)?.ticketTypes || [];

  // Only show ticket types that have future sessions (canPurchase or referenceSessionId exists)
  const displayableTickets = ticketTypes.filter((tt: any) =>
    tt.canPurchase || tt.referenceSessionId
  );

  // Separate into purchasable and unavailable tickets
  const purchasableTickets = displayableTickets.filter((tt: any) => tt.canPurchase);
  const unavailableTickets = displayableTickets.filter((tt: any) => !tt.canPurchase);

  // Determine volunteer box visibility
  const hasVolunteerPositions = Array.isArray(volunteerPositions) && volunteerPositions.length > 0;
  const userVolunteerPositions = Array.isArray(volunteerPositions)
    ? volunteerPositions.filter(p => p.hasUserSignedUp === true)
    : [];
  const hasUserVolunteered = userVolunteerPositions.length > 0;
  const isEventFull = availableSpots <= 0;
  const hasParticipation = participation?.hasRSVP || participation?.hasTicket;

  // Check if user is vetted (same logic as ParticipationCard)
  let isVetted = false;
  if (currentUser && typeof currentUser === 'object') {
    // New structure: Check isVetted boolean OR admin/teacher role
    if ('isVetted' in currentUser && currentUser.isVetted === true) {
      isVetted = true;
    } else if ('role' in currentUser && typeof currentUser.role === 'string') {
      const adminTeacherRoles = ['Administrator', 'Teacher'];
      isVetted = adminTeacherRoles.includes(currentUser.role);
    }
    // Legacy structure: Check roles array (fallback)
    if (!isVetted && 'roles' in currentUser && Array.isArray(currentUser.roles)) {
      const legacyRoles = ['Vetted', 'Teacher', 'Administrator'];
      isVetted = currentUser.roles.some(role => legacyRoles.includes(role));
    }
  }

  // Allow volunteering if:
  // - For social events: User is vetted (no ticket required)
  // - For class events: User must have a ticket purchased
  const canVolunteerBasedOnEventType = eventType === 'social'
    ? isVetted
    : (isVetted && participation?.hasTicket);

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

  // Extract ParticipationCard props for reuse (DRY pattern)
  const participationCardProps = {
    eventId: id!,
    eventTitle: (event as any)?.title || 'Event',
    eventType: eventType as 'social' | 'class',
    participation,
    isLoading: participationLoading || createRSVPMutation.isPending || cancelRSVPMutation.isPending || cancelTicketMutation.isPending,
    ticketTypeId: (event as any)?.ticketTypes?.[0]?.id,
    onRSVP: handleRSVP,
    onPurchaseTicket: handlePurchaseTicket,
    onCancel: handleCancel,
    ticketPrice: getTicketPriceDisplay().min,
    ticketPriceRange: getTicketPriceDisplay(),
    eventStartDateTime: (event as any)?.startDate,
    eventEndDateTime: (event as any)?.endDate,
    eventInstructor: (event as any)?.instructor,
    eventLocation: (event as any)?.location,
    eventSessions: (event as any)?.sessions,
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

      <Container
        size="xl"
        px={{ base: 'sm', md: 'xl' }}
        py={{ base: 'sm', md: 'xl' }}
      >
        {/* Event Hero Section - FULL WIDTH */}
        <Paper
          data-testid="section-hero"
          px={{ base: 28, md: 48 }}
          py={{ base: 28, md: 48 }}
          style={{
            background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
            borderRadius: '24px',
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
                {(event as any)?.title}
              </Title>

              {/* Separator - hidden on mobile */}
              <Text visibleFrom="md" style={{
                fontFamily: 'var(--font-heading)',
                fontSize: 'clamp(1.75rem, 1.11vw + 1.39rem, 3rem)',
                fontWeight: 800,
                color: 'var(--color-ivory)',
                lineHeight: 1.2
              }}>
                -
              </Text>

              {/* Location - moved to right of title */}
              <Text size="lg" style={{
                fontFamily: 'var(--font-heading)',
                fontSize: 'clamp(1.75rem, 1.11vw + 1.39rem, 3rem)',
                fontWeight: 800,
                color: 'var(--color-ivory)',
                lineHeight: 1.2
              }}>
                {(() => {
                  const hasVenueAccess = isVetted || (participation?.hasRSVP || participation?.hasTicket);
                  if (hasVenueAccess) {
                    return venue?.name || (event as any)?.venueLocation || 'Location TBD';
                  } else {
                    return (event as any)?.venueLocation || 'Location TBD';
                  }
                })()}
              </Text>
            </Group>

            <div className="html-content-light">
              {/* Session date/times - one line each */}
              {(() => {
                const sessions = ((event as any)?.sessions || []).slice().sort((a: any, b: any) =>
                  new Date(a.startTime || '').getTime() - new Date(b.startTime || '').getTime()
                );

                if (sessions.length === 0) {
                  return <h3>Date and Time coming soon</h3>;
                }

                return sessions.map((session: any, index: number) => (
                  <h3 key={session.id || index}>
                    <span className="date-part">
                      {formatUtcToLocalDate(session.startTime, eventTimeZone, { weekday: 'long', month: 'short', day: 'numeric' })}
                    </span>
                    <span className="time-part">
                      {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}
                    </span>
                  </h3>
                ));
              })()}
            </div>
          </Box>
        </Paper>

        {/* Mobile Participation Card - FULL WIDTH, mobile only */}
        <Box hiddenFrom="md" mt={{ base: 'sm', md: 'lg' }}>
          <ParticipationCard {...participationCardProps} />
        </Box>

        {/* Mobile Volunteer Shifts - FULL WIDTH, mobile only */}
        {hasUserVolunteered && userVolunteerPositions.length > 0 && (
          <Box hiddenFrom="md" mt="sm">
            <UserVolunteerShifts
              positions={userVolunteerPositions}
              eventId={id!}
            />
          </Box>
        )}

        {/* Main Content Grid - TWO COLUMNS on desktop */}
        <Grid gutter={{ base: 'xs', md: 'xl' }} mt={{ base: 'sm', md: 'lg' }}>
          {/* Left Column - Event Details */}
          <Grid.Col span={{ base: 12, md: 8 }}>
            <Stack gap="md">

          {/* About This Event */}
          <ContentSection>
            <div
              className="html-content"
              dangerouslySetInnerHTML={{ __html: (event as any)?.description || '' }}
            />
          </ContentSection>

          {/* Ticket Options Section - Session-based timing */}
          {displayableTickets.length > 0 && (
            <ContentSection title="Ticket Options">
              <Stack gap="md">
                {/* Purchasable Tickets */}
                {purchasableTickets.length > 0 && (
                  <Stack gap="sm">
                    {purchasableTickets.map((ticket: any) => (
                      <Paper
                        key={ticket.id}
                        p="md"
                        style={{
                          background: 'var(--color-cream)',
                          border: '1px solid var(--color-plum)',
                          borderRadius: '8px'
                        }}
                      >
                        <Group justify="space-between" align="flex-start" wrap="nowrap">
                          <Box style={{ flex: 1 }}>
                            <Text fw={600} size="md" mb="xs">{ticket.name}</Text>
                            {ticket.pricingType === 'SlidingScale' ? (
                              <Text size="sm" c="dimmed">
                                ${ticket.minPrice} - ${ticket.maxPrice} (Sliding Scale)
                              </Text>
                            ) : (
                              <Text size="sm" c="dimmed">
                                ${ticket.price}
                              </Text>
                            )}
                            {ticket.referenceSessionName && (
                              <Text size="xs" c="dimmed" mt="xs">
                                For: {ticket.referenceSessionName}
                              </Text>
                            )}
                            <Text size="xs" c="dimmed" mt="xs">
                              {ticket.quantityAvailable - ticket.quantitySold} / {ticket.quantityAvailable} available
                            </Text>
                          </Box>
                          <Badge color="green" variant="light">
                            Available Now
                          </Badge>
                        </Group>
                      </Paper>
                    ))}
                  </Stack>
                )}

                {/* Unavailable Tickets (not yet open or closed) */}
                {unavailableTickets.length > 0 && (
                  <>
                    {purchasableTickets.length > 0 && (
                      <Text size="sm" c="dimmed" mt="md">Other ticket options:</Text>
                    )}
                    <Stack gap="sm">
                      {unavailableTickets.map((ticket: any) => (
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
                          <Group justify="space-between" align="flex-start" wrap="nowrap">
                            <Box style={{ flex: 1 }}>
                              <Text fw={500} size="md" mb="xs">{ticket.name}</Text>
                              {ticket.availabilityMessage && (
                                <Text size="sm" c="dimmed" mb="xs">
                                  {ticket.availabilityMessage}
                                </Text>
                              )}
                              {ticket.referenceSessionName && (
                                <Text size="xs" c="dimmed">
                                  For: {ticket.referenceSessionName}
                                </Text>
                              )}
                            </Box>
                            <Badge color="gray" variant="light">
                              Not Available
                            </Badge>
                          </Group>
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
              </Stack>
            </ContentSection>
          )}

          {/* Venue Details - Conditional based on access */}
          {venue && (() => {
            const hasVenueAccess = isVetted || (participation?.hasRSVP || participation?.hasTicket);

            if (hasVenueAccess && venue.directions) {
              // Full venue details for vetted users or participants
              return (
                <ContentSection>
                  <div className="html-content">
                    <h2>{venue.name}</h2>
                    <h3>Directions</h3>
                    <p style={{ whiteSpace: 'pre-line' }}>{venue.directions}</p>
                    {venue.venueInformation && (
                      <>
                        <h3>Venue Information</h3>
                        <p style={{ whiteSpace: 'pre-line' }}>{venue.venueInformation}</p>
                      </>
                    )}
                  </div>
                </ContentSection>
              );
            } else if (!hasVenueAccess && (event as any)?.venueLocation) {
              // Limited location info for non-vetted non-participants
              return (
                <ContentSection title="Location">
                  <Stack gap="md">
                    <Text
                      style={{
                        fontSize: 'clamp(1rem, 0.19vw + 0.94rem, 1.06rem)',
                        lineHeight: 1.8,
                        color: 'var(--color-charcoal)',
                      }}
                    >
                      {(event as any)?.venueLocation}
                    </Text>

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
                        Full venue address and directions will be provided after registration.
                      </Text>
                    </Alert>
                  </Stack>
                </ContentSection>
              );
            }

            return null;
          })()}

          {/* Volunteer Positions */}
          {volunteerPositions && Array.isArray(volunteerPositions) && volunteerPositions.length > 0 && isAuthenticated && canVolunteerBasedOnEventType && (
            <div id="volunteer-opportunities-section">
              <ContentSection title="Volunteer Opportunities">
                <div className="html-content">
                  <p>
                    Help make this event a success! Sign up for a volunteer position and you'll automatically be RSVPed to the event.
                  </p>
                  <Stack gap="md" mt="md">
                    {volunteerPositions.map((position) => (
                      <VolunteerPositionCard
                        key={position.id}
                        position={position}
                        hasExistingParticipation={participation?.hasRSVP || participation?.hasTicket || false}
                      />
                    ))}

                    {/* Show message if all positions are filled or signup closed */}
                    {volunteerPositions.every((p) => p.isFullyStaffed || !p.canSignUp) &&
                     !volunteerPositions.some((p) => p.hasUserSignedUp) && (
                      <Alert color="gray" variant="light">
                        <Text size="sm">
                          All volunteer positions are either full or signup has closed.
                        </Text>
                      </Alert>
                    )}
                  </Stack>
                </div>
              </ContentSection>
            </div>
          )}

          {/* Teachers Section - Always visible */}
          {teachers && teachers.length > 0 && (
            <ContentSection title="Teachers">
              <Stack gap="lg">
                {teachers.map((teacher) => (
                  <Box key={(teacher as any).id}>
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
          {(event as any)?.policies && (
            <ContentSection>
              <div
                className="html-content"
                dangerouslySetInnerHTML={{ __html: (event as any)?.policies }}
              />
            </ContentSection>
          )}
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
}

const ContentSection: React.FC<ContentSectionProps> = ({ title, children }) => (
  <Paper
    className={styles.contentSection}
    px={{ base: 24, md: 40 }}
    py={{ base: 18, md: 40 }}
    style={{
      background: 'var(--color-ivory)',
      borderRadius: '16px',
      boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
      border: '1px solid rgba(183, 109, 117, 0.1)'
    }}
  >
    {title && (
      <Title
        order={2}
        mb={{ base: 'xs', md: 'md' }}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: 'var(--font-size-h2)', // 24px mobile → 36px desktop
          lineHeight: 'var(--line-height-h2)',
          fontWeight: 700,
          color: 'var(--color-burgundy)'
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