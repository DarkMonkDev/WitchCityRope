/**
 * ParticipationCard - Event RSVP and Ticket Purchase Component
 *
 * CRITICAL BUSINESS RULES:
 *
 * 1. TERMINOLOGY:
 *    - ✅ ALWAYS use "RSVP" (never "register" or "registration")
 *    - ✅ ALWAYS use "Purchase Ticket" or "Buy Ticket" (never "register")
 *    - ❌ NEVER use the term "registration" anywhere in the UI
 *
 * 2. SOCIAL EVENT RSVP + TICKET LOGIC:
 *    - Social events show BOTH "RSVP" and "Purchase Ticket" buttons simultaneously
 *    - Users can RSVP without purchasing a ticket (free attendance)
 *    - Users can purchase a ticket without RSVPing first
 *    - AUTOMATIC RSVP: If user purchases ticket but hasn't RSVP'd, system automatically creates RSVP
 *    - Both actions are INDEPENDENT and PARALLEL (not sequential)
 *
 * 3. CLASS EVENT LOGIC:
 *    - Classes typically only show "Purchase Ticket" (tickets required)
 *    - May also support RSVP for free classes (configured per event)
 *
 * 4. CAPACITY TRACKING:
 *    - Social events: Track RSVPs separately from ticket sales
 *    - Classes: Track ticket sales
 *    - Display shows current/total (e.g., "45 / 50")
 *
 * 5. USER STATES:
 *    - Not logged in: Show login prompt
 *    - Logged in, no RSVP, no ticket: Show available actions
 *    - RSVP'd but no ticket: Show RSVP status + option to purchase ticket
 *    - Ticket purchased: Show ticket status (RSVP created automatically if social event)
 *    - Both RSVP + ticket: Show both statuses
 */
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Paper, Stack, Alert, Group, Text, Box, Badge, Button,
  LoadingOverlay, Progress, Modal, Textarea, Checkbox, Title, Divider
} from '@mantine/core';
import { IconUsers, IconTicket, IconCalendarCheck, IconCheck, IconAlertCircle, IconClock } from '@tabler/icons-react';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import {
  EnhancedParticipationStatusDto
} from '../../types/participation.types';
import { PayPalButton } from '../../features/payments/components/PayPalButton';
import type { PaymentEventInfo } from '../../features/payments/types/payment.types';
import { useConfirmPayPalPayment } from '../../lib/api/hooks/usePayments';
import { PaymentSummary } from '../../features/payments/components/PaymentSummary';
import type { components } from '@witchcityrope/shared-types/generated/api-types';
import { debugLog } from '../../utils/debug';
import { useVettingStatus } from '../../features/vetting/hooks/useVettingStatus';
import { formatUtcToLocalDate, formatUtcTimeRange } from '../../utils/eventUtils';
import { useEventTimeZone } from '../../hooks/useEventTimeZone';

type TicketTypeDto = components["schemas"]["TicketTypeDto"];

// Helper function to extract purchase amount from metadata JSON
const extractAmountFromMetadata = (metadata?: string): number => {
  if (!metadata) return 0;

  try {
    const parsed = JSON.parse(metadata);
    // Check for different possible field names in the metadata
    return parsed.purchaseAmount || parsed.amount || parsed.ticketAmount || 0;
  } catch (error) {
    console.warn('Failed to parse participation metadata:', metadata, error);
    return 0;
  }
};

interface TicketPriceRange {
  min: number;
  max: number;
  isSinglePrice: boolean;
}

interface ParticipationCardProps {
  eventId: string;
  eventTitle: string;
  eventType: 'social' | 'class';
  participation: EnhancedParticipationStatusDto | null;
  isLoading?: boolean;
  ticketTypeId?: string;
  onRSVP: (notes?: string, eventWaiverAccepted?: boolean) => void;
  onPurchaseTicket: (amount: number, slidingScalePercentage?: number) => void;
  onCancel: (type: 'rsvp' | 'ticket', reason?: string, ticketPurchaseIds?: string[]) => void;
  ticketPrice?: number;
  ticketPriceRange?: TicketPriceRange;
  eventStartDateTime?: string;
  eventEndDateTime?: string;
  eventInstructor?: string;
  eventLocation?: string;
  eventSessions?: Array<{
    id?: string;
    name?: string;
    sessionIdentifier?: string;
    startTime?: string;
    endTime?: string;
  }>;
}

export const ParticipationCard: React.FC<ParticipationCardProps> = ({
  eventId,
  eventTitle,
  eventType,
  participation,
  isLoading = false,
  ticketTypeId,
  onRSVP,
  onPurchaseTicket,
  onCancel,
  ticketPrice = 50,
  ticketPriceRange,
  eventStartDateTime,
  eventEndDateTime,
  eventInstructor,
  eventLocation,
  eventSessions
}) => {
  // Format price display string based on range
  const formatPriceDisplay = (): string => {
    if (!ticketPriceRange) return `$${ticketPrice}`;
    if (ticketPriceRange.isSinglePrice) return `$${ticketPriceRange.min}`;
    return `$${ticketPriceRange.min} - $${ticketPriceRange.max}`;
  };
  const { data: user, isLoading: isLoadingUser } = useCurrentUser();
  const navigate = useNavigate();
  const isAuthenticated = !!user;
  const eventTimeZone = useEventTimeZone();

  // Check if user has submitted a vetting application
  const { data: vettingStatus } = useVettingStatus();
  const hasVettingApplication = vettingStatus?.hasApplication || false;

  // DEBUG: Log all relevant data for RSVP button troubleshooting
  debugLog('🔍 ParticipationCard DEBUG DATA:');
  debugLog('  - eventType:', eventType);
  debugLog('  - user (full object):', user);
  debugLog('  - isAuthenticated:', isAuthenticated);
  debugLog('  - participation:', participation);
  debugLog('  - participation?.canRSVP:', participation?.canRSVP);
  debugLog('  - participation?.hasRSVP:', participation?.hasRSVP);
  debugLog('  - isLoading:', isLoading);
  debugLog('  - isLoadingUser:', isLoadingUser);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelType, setCancelType] = useState<'rsvp' | 'ticket'>('rsvp');
  const [cancelReason, setCancelReason] = useState('');
  const [showPayPal, setShowPayPal] = useState(false);
  const [selectedAmount, setSelectedAmount] = useState(ticketPrice);
  const [slidingScalePercentage, setSlidingScalePercentage] = useState(0);
  const [rsvpTermsAccepted, setRsvpTermsAccepted] = useState(false);

  // Cancel mode state for selective ticket cancellation
  const [isCancelMode, setIsCancelMode] = useState(false);
  const [selectedTicketPurchaseIds, setSelectedTicketPurchaseIds] = useState<string[]>([]);

  // PayPal payment confirmation hook
  const confirmPayPalPayment = useConfirmPayPalPayment();

  // Show login prompt for anonymous users
  if (!isAuthenticated) {
    return (
      <ParticipationCardShell>
        <Alert
          icon={<IconAlertCircle size={16} />}
          title="Login Required"
          variant="light"
          color="blue"
        >
          <Text size="sm" mb="md">
            Please log in to RSVP for events or purchase tickets.
          </Text>
          <Button
            component="a"
            href={`/login?returnUrl=${encodeURIComponent(window.location.pathname)}`}
            variant="filled"
            color="blue"
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
            Log In
          </Button>
        </Alert>
      </ParticipationCardShell>
    );
  }

  // Check if user is banned (using the isActive property)
  if (user && typeof user === 'object' && 'isActive' in user && user.isActive === false) {
    return (
      <ParticipationCardShell>
        <Alert
          icon={<IconAlertCircle size={16} />}
          title="Access Denied"
          variant="light"
          color="red"
        >
          <Text size="sm">
            Your account access has been restricted. Please contact support for assistance.
          </Text>
        </Alert>
      </ParticipationCardShell>
    );
  }

  // Check vetting requirements for social events
  // Handle both legacy (roles array) and new (isVetted boolean + role string) user structures
  let isVetted = false;

  debugLog('🔍 VETTING LOGIC DEBUG:');
  debugLog('  - user exists:', !!user);
  debugLog('  - user type:', typeof user);

  if (user && typeof user === 'object') {
    debugLog('  - user.isVetted:', (user as any).isVetted);
    debugLog('  - user.role:', (user as any).role);
    debugLog('  - user.roles:', (user as any).roles);

    // New structure: Check isVetted boolean OR admin/teacher role
    if ('isVetted' in user && user.isVetted === true) {
      isVetted = true;
      debugLog('  ✅ isVetted = true (via isVetted boolean)');
    } else if ('role' in user && typeof user.role === 'string') {
      const adminTeacherRoles = ['Administrator', 'Teacher'];
      isVetted = adminTeacherRoles.includes(user.role);
      debugLog(`  - Checking role '${user.role}' against [${adminTeacherRoles.join(', ')}]: ${isVetted}`);
      if (isVetted) debugLog('  ✅ isVetted = true (via admin/teacher role)');
    }

    // Legacy structure: Check roles array (fallback)
    if (!isVetted && 'roles' in user && Array.isArray(user.roles)) {
      const legacyRoles = ['Vetted', 'Teacher', 'Administrator'];
      isVetted = user.roles.some(role => legacyRoles.includes(role));
      debugLog(`  - Checking roles array [${user.roles.join(', ')}] against [${legacyRoles.join(', ')}]: ${isVetted}`);
      if (isVetted) debugLog('  ✅ isVetted = true (via legacy roles array)');
    }
  }

  debugLog('  🎯 FINAL isVetted result:', isVetted);

  // Enhanced participation debugging
  debugLog('🔍 PARTICIPATION STATUS DEBUG:');
  debugLog('  - participation is null:', participation === null);
  debugLog('  - participation is undefined:', participation === undefined);
  debugLog('  - participation type:', typeof participation);
  debugLog('  - participation value:', participation);
  debugLog('  - isLoading:', isLoading);
  debugLog('  - isLoadingUser:', isLoadingUser);

  // Handle invalid participation data and normalize structure
  let validParticipation = (participation && typeof participation === 'object') ? participation : null;

  // Check if participation has the old structure (from API) and normalize it
  if (validParticipation && 'participationType' in validParticipation && !('hasRSVP' in validParticipation)) {
    debugLog('🔍 Converting old participation structure to new format');
    const participationAny = validParticipation as any;
    debugLog('🔍 Participation metadata:', participationAny.metadata);
    debugLog('🔍 Extracted amount:', extractAmountFromMetadata(participationAny.metadata));
    const hasRSVP = participationAny.participationType === 'RSVP' && participationAny.status === 'Active';
    const hasTicket = participationAny.participationType === 'Ticket' && participationAny.status === 'Active';

    // Create proper nested structure with actual backend data
    // CRITICAL: Use backend's canRSVP and canPurchaseTicket values, don't hardcode
    validParticipation = {
      ...(validParticipation as any),
      hasRSVP,
      hasTicket,
      // Use backend values if available, otherwise fallback to simple logic
      canRSVP: participationAny.canRSVP !== undefined ? participationAny.canRSVP : (!hasRSVP && !hasTicket),
      canPurchaseTicket: participationAny.canPurchaseTicket !== undefined ? participationAny.canPurchaseTicket : !hasTicket,
      // Create nested structures with proper date mapping
      rsvp: hasRSVP ? {
        id: participationAny.id || '',
        status: participationAny.status || 'Active',
        createdAt: participationAny.participationDate || new Date().toISOString(),
        canceledAt: undefined,
        cancelReason: undefined
      } : null,
      ticket: hasTicket ? {
        id: participationAny.id || '',
        status: participationAny.status || 'Active',
        amount: extractAmountFromMetadata(participationAny.metadata) || 0, // Extract from metadata field
        paymentStatus: 'Completed', // Default status
        createdAt: participationAny.participationDate || new Date().toISOString(),
        canceledAt: undefined,
        cancelReason: undefined
      } : null
    } as EnhancedParticipationStatusDto;

    debugLog('🔍 Converted participation:', validParticipation);
  }

  if (eventType === 'social' && !isVetted) {
    return (
      <ParticipationCardShell>
        <Box style={{ width: '100%', maxWidth: '100%', overflow: 'hidden' }}>
          <Alert
            title="Vetting Required"
            variant="light"
            color="orange"
            styles={{
              root: {
                width: '100%',
                maxWidth: '100%'
              },
              message: {
                width: '100%',
                maxWidth: '100%'
              }
            }}
          >
            <Text size="sm" mb={hasVettingApplication ? undefined : "md"} style={{ width: '100%', wordWrap: 'break-word' }}>
              This social event is limited to vetted members. Once you are vetted you will be allowed to RSVP for our social events.
            </Text>
            {!hasVettingApplication && (
              <Button
                component="a"
                href="/join"
                variant="outline"
                color="orange"
                fullWidth
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
                Start Vetting Process
              </Button>
            )}
          </Alert>
        </Box>
      </ParticipationCardShell>
    );
  }

  // Check capacity
  const isAtCapacity = validParticipation?.capacity && validParticipation.capacity.available <= 0;

  const handleRSVPClick = () => {
    // Direct RSVP without modal confirmation, passing event waiver acceptance
    onRSVP(undefined, rsvpTermsAccepted);
  };

  const handleCancelClick = (type: 'rsvp' | 'ticket') => {
    if (type === 'ticket') {
      // For tickets, enter cancel mode to allow selective cancellation
      setIsCancelMode(true);

      // Get ticket purchase IDs to determine if we should pre-select
      const ticketPurchases = (validParticipation as any)?.ticketPurchases;
      const ticketPurchaseMap = (validParticipation as any)?.ticketPurchaseSessionMap;
      const purchaseIds = ticketPurchases
        ? Object.keys(ticketPurchases)
        : (ticketPurchaseMap ? Object.keys(ticketPurchaseMap) : []);

      // Pre-select the ticket if there's exactly one, otherwise start with none selected
      if (purchaseIds.length === 1) {
        setSelectedTicketPurchaseIds([purchaseIds[0]]);
      } else {
        setSelectedTicketPurchaseIds([]);
      }
    } else {
      // For RSVP, use the modal
      setCancelType(type);
      setCancelModalOpen(true);
    }
  };

  const handleConfirmCancel = () => {
    onCancel(cancelType, cancelReason.trim() || undefined);
    setCancelModalOpen(false);
    setCancelReason('');
  };

  const handleCancelModal = () => {
    setCancelModalOpen(false);
    setCancelReason('');
  };

  // Toggle ticket purchase selection for cancellation
  const toggleTicketPurchaseSelection = (ticketPurchaseId: string) => {
    setSelectedTicketPurchaseIds(prev =>
      prev.includes(ticketPurchaseId)
        ? prev.filter(id => id !== ticketPurchaseId)
        : [...prev, ticketPurchaseId]
    );
  };

  // Confirm selective ticket cancellation
  const handleConfirmTicketCancel = () => {
    if (selectedTicketPurchaseIds.length > 0) {
      onCancel('ticket', undefined, selectedTicketPurchaseIds);
    }
    setIsCancelMode(false);
    setSelectedTicketPurchaseIds([]);
  };

  // Exit cancel mode without cancelling
  const handleExitCancelMode = () => {
    setIsCancelMode(false);
    setSelectedTicketPurchaseIds([]);
  };

  const handleTicketPurchase = () => {
    setSelectedAmount(ticketPrice);
    setSlidingScalePercentage(0);

    // Navigate to checkout page with event info and owned session IDs for filtering
    navigate(`/checkout/${eventId}`, {
      state: {
        eventInfo: {
          id: eventId,
          title: eventTitle,
          startDateTime: eventStartDateTime || new Date().toISOString(),
          endDateTime: eventEndDateTime || new Date().toISOString(),
          instructor: eventInstructor,
          location: eventLocation,
          price: ticketPrice
        },
        // Pass owned session IDs so checkout can filter out tickets that include them
        ownedSessionIds: (validParticipation as any)?.ownedSessionIds || []
      }
    });
  };

  const handlePayPalSuccess = async (paymentDetails: any) => {
    debugLog('🔍 PayPal payment successful:', paymentDetails);

    try {
      if (!ticketTypeId) {
        console.error('❌ Cannot process PayPal payment: ticketTypeId is required');
        return;
      }

      // Confirm payment with backend and create ticket
      await confirmPayPalPayment.mutateAsync({
        orderId: paymentDetails.id,
        paymentDetails,
        ticketTypeId
      });

      // Call the parent component's purchase handler for additional UI updates
      onPurchaseTicket(selectedAmount, slidingScalePercentage);

      // Hide PayPal form
      setShowPayPal(false);

    } catch (error) {
      console.error('❌ Failed to confirm PayPal payment:', error);
    }
  };

  const handlePayPalError = (error: string) => {
    console.error('❌ PayPal payment error:', error);
    // Keep PayPal form visible for retry
  };

  const handlePayPalCancel = () => {
    debugLog('🔍 PayPal payment cancelled');
    setShowPayPal(false);
  };


  // Create PayPal event info
  const paypalEventInfo: PaymentEventInfo = {
    id: eventId,
    title: eventTitle,
    startDateTime: new Date().toISOString(), // Would come from event data
    endDateTime: new Date().toISOString(),   // Would come from event data
    currency: 'USD',
    basePrice: selectedAmount,
    registrationId: eventId // Use event ID as registration ID for now
  };

  return (
    <>
      <ParticipationCardShell isLoading={isLoading}>
        <Stack gap="lg">
          {/* Capacity Status - Only show for single-session events or if no session data */}
          {validParticipation?.capacity && (!eventSessions || eventSessions.length <= 1) && (
            <Box>
              {/* Availability status text - same logic as PublicEventCard */}
              {(() => {
                const available = validParticipation.capacity.available;
                const current = validParticipation.capacity.current;
                const isSocialEvent = eventType === 'social';

                // Color logic: >10 = success, >3 = warning, ≤3 = error
                const getSpotColor = () => {
                  if (available > 10) return 'var(--color-success)';
                  if (available > 3) return 'var(--color-warning)';
                  return 'var(--color-error)';
                };

                // Status text logic matching PublicEventCard
                const getStatusText = () => {
                  if (isSocialEvent) {
                    if (available <= 0) return 'RSVPs Full';
                    if (available <= 3) return `Only ${available} left!`;
                    return `${current} RSVPs, ${available} left`;
                  } else {
                    if (available <= 0) return 'Sold Out';
                    if (available <= 3) return `Only ${available} left!`;
                    return `${current} sold, ${available} left`;
                  }
                };

                return (
                  <Text
                    fw={600}
                    ta="center"
                    mb="xs"
                    style={{
                      fontFamily: 'var(--font-heading)',
                      fontSize: '14px',
                      color: getSpotColor(),
                    }}
                  >
                    {getStatusText()}
                  </Text>
                );
              })()}
              <Progress
                value={(validParticipation.capacity.current / validParticipation.capacity.total) * 100}
                color={validParticipation.capacity.available <= 3 ? 'red' : validParticipation.capacity.available <= 10 ? 'yellow' : 'green'}
                size="sm"
                radius="md"
              />
            </Box>
          )}

          {/* Event Dates / Times - Show for multi-session events when user does NOT have a ticket */}
          {eventSessions && eventSessions.length > 1 &&
           (validParticipation as any)?.sessionAvailability &&
           !validParticipation?.hasTicket && (
            <Paper
              radius="md"
              p="lg"
              withBorder
              style={{
                backgroundColor: '#FAF6F2',
                border: '1px solid #D4A5A5'
              }}
            >
              <Stack gap="md">
                <Title order={4} c="#880124">
                  Available Sessions
                </Title>
                <Stack gap="xs">
                  {(validParticipation as any).sessionAvailability.map((session: any) => (
                    <Group key={session.sessionId} gap="xs" align="flex-start">
                      <IconTicket size={16} color="#6B0119" style={{ marginTop: 2, flexShrink: 0 }} />
                      <Box>
                        <Text size="sm" fw={500}>
                          {session.sessionName || session.sessionIdentifier || 'Session'}
                        </Text>
                        <Text size="sm" c="dimmed">
                          {session.startTime && formatUtcToLocalDate(session.startTime, eventTimeZone, {
                            weekday: 'short',
                            month: 'short',
                            day: 'numeric'
                          })}
                          {session.startTime && session.endTime && (
                            <> • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}</>
                          )}
                        </Text>
                        <Text size="sm" c="dimmed">
                          {session.soldCount || 0} sold, {session.availableCount || 0} available
                        </Text>
                      </Box>
                    </Group>
                  ))}
                </Stack>
              </Stack>
            </Paper>
          )}

          {/* Removed: Owned Sessions Alert - data is now shown in "Your Ticket(s)" box below */}

          {/* Capacity Full Alert */}
          {isAtCapacity && (
            <Alert color="red" title="Event Full">
              <Text size="sm">
                This event has reached capacity. Join the waitlist to be notified if spots become available.
              </Text>
            </Alert>
          )}

          {/* Your RSVP Status */}
          {validParticipation?.hasRSVP && !validParticipation?.hasTicket && (
            <Box
              style={{
                background: 'linear-gradient(135deg, var(--color-success-light) 0%, var(--color-cream) 100%)',
                borderRadius: '16px',
                padding: 'var(--space-lg)',
                border: '2px solid var(--color-success)',
                boxShadow: '0 4px 12px rgba(76, 175, 80, 0.15)',
                textAlign: 'center'
              }}
            >
              <Group gap="sm" mb="md" justify="center">
                <IconCalendarCheck size={24} color="var(--color-success)" />
                <Text fw={700} size="lg" c="var(--color-success)">RSVP Confirmed</Text>
              </Group>

              <Text size="sm" c="dimmed" mb="sm">
                Registered on {validParticipation.rsvp?.createdAt ?
                  new Date(validParticipation.rsvp.createdAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                  }) : 'Date unavailable'}
              </Text>

              {/* Cancel button - show only if backend allows */}
              {validParticipation.canCancelRSVP ? (
                <Button
                  variant="light"
                  color="red"
                  onClick={() => handleCancelClick('rsvp')}
                  fullWidth
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
                  Cancel RSVP
                </Button>
              ) : (
                <Alert color="orange" variant="light">
                  <Text size="sm">Cancellations are not available at this time</Text>
                </Alert>
              )}
            </Box>
          )}

          {/* Ticket Purchase Summary */}
          {validParticipation?.hasTicket && (
            <Box>
              {/* Custom display for purchased tickets */}
              <Paper
                radius="md"
                p="lg"
                withBorder
                style={{
                  backgroundColor: '#FAF6F2',
                  border: '1px solid #D4A5A5'
                }}
              >
                <Stack gap="md">
                  <Group justify="space-between" align="center">
                    <Title order={4} c="#880124">
                      {isCancelMode ? 'Select Tickets to Cancel' : 'Your Tickets'}
                    </Title>
                    {isCancelMode && (
                      <Text
                        size="sm"
                        c="dimmed"
                        style={{ cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={handleExitCancelMode}
                      >
                        Cancel
                      </Text>
                    )}
                  </Group>

                  {/* Cancel Mode: Show tickets grouped by purchase with checkboxes */}
                  {isCancelMode ? (
                    <Stack gap="md">
                      {/* Get ticket purchases from the map */}
                      {(() => {
                        // Use ticketPurchases (includes ticket type name) if available, fall back to ticketPurchaseSessionMap
                        const ticketPurchases = (validParticipation as any)?.ticketPurchases as Record<string, { ticketTypeName: string; sessionIds: string[]; canCancel?: boolean; cancellationMessage?: string | null }> | undefined;
                        const ticketPurchaseMap = (validParticipation as any)?.ticketPurchaseSessionMap as Record<string, string[]> | undefined;
                        debugLog('🎫 Cancel Mode - ticketPurchases:', ticketPurchases);
                        debugLog('🎫 Cancel Mode - ticketPurchaseMap:', ticketPurchaseMap);

                        // If no map available, this indicates a data issue - ticketPurchaseSessionMap should always be populated
                        if ((!ticketPurchases || Object.keys(ticketPurchases).length === 0) &&
                            (!ticketPurchaseMap || Object.keys(ticketPurchaseMap).length === 0)) {
                          debugLog('🎫 ERROR: ticketPurchases/ticketPurchaseSessionMap is empty - this is a data issue');
                          return (
                            <Alert color="red" variant="light">
                              <Text size="sm">Unable to load ticket details. Please contact support.</Text>
                            </Alert>
                          );
                        }

                        // Prefer ticketPurchases (has ticket type name), fall back to ticketPurchaseMap
                        const purchaseEntries = ticketPurchases
                          ? Object.entries(ticketPurchases).map(([id, info]) => ({
                              ticketPurchaseId: id,
                              sessionIds: info.sessionIds,
                              ticketTypeName: info.ticketTypeName,
                              canCancel: info.canCancel !== false, // Default to true if not specified
                              cancellationMessage: info.cancellationMessage || null
                            }))
                          : Object.entries(ticketPurchaseMap || {}).map(([id, sessionIds]) => ({
                              ticketPurchaseId: id,
                              sessionIds,
                              ticketTypeName: null as string | null,
                              canCancel: true, // Legacy data without the new field - assume cancelable
                              cancellationMessage: null as string | null
                            }));

                        // Render each ticket purchase as a selectable item
                        return purchaseEntries.map(({ ticketPurchaseId, sessionIds, ticketTypeName, canCancel, cancellationMessage }) => {
                          const ticketSessions = eventSessions?.filter(s => sessionIds.includes(s.id || '')) || [];
                          const isSelected = selectedTicketPurchaseIds.includes(ticketPurchaseId);

                          // Use ticket type name from API, or generate fallback if not available
                          const ticketName = ticketTypeName || (ticketSessions.length > 1
                            ? `${ticketSessions.length}-Session Ticket`
                            : ticketSessions[0]?.name || ticketSessions[0]?.sessionIdentifier || 'Event Ticket');

                          return (
                            <Box
                              key={ticketPurchaseId}
                              onClick={() => canCancel && toggleTicketPurchaseSelection(ticketPurchaseId)}
                              style={{
                                cursor: canCancel ? 'pointer' : 'not-allowed',
                                padding: '8px',
                                borderRadius: '8px',
                                backgroundColor: isSelected ? 'rgba(220, 53, 69, 0.1)' : 'transparent',
                                border: isSelected ? '1px solid rgba(220, 53, 69, 0.3)' : '1px solid transparent',
                                opacity: canCancel ? 1 : 0.6
                              }}
                            >
                              <Group gap="sm" align="flex-start">
                                <Checkbox
                                  checked={isSelected && canCancel}
                                  onChange={() => {}}
                                  readOnly
                                  disabled={!canCancel}
                                  color="red"
                                  mt={2}
                                />
                                <Box style={{ flex: 1 }}>
                                  <Text size="sm" fw={500}>{ticketName}</Text>
                                  {/* Show cancellation message if ticket can't be cancelled */}
                                  {!canCancel && cancellationMessage && (
                                    <Text size="xs" c="red" mt={4}>
                                      {cancellationMessage}
                                    </Text>
                                  )}
                                  <Stack gap={4} mt={4}>
                                    {ticketSessions.map((session, idx) => (
                                      <Text key={session.id || idx} size="xs" c="dimmed">
                                        {session.name || session.sessionIdentifier || `Session ${idx + 1}`}
                                        {session.startTime && (
                                          <> – {formatUtcToLocalDate(session.startTime, eventTimeZone, {
                                            weekday: 'short',
                                            month: 'short',
                                            day: 'numeric'
                                          })}
                                          {session.endTime && (
                                            <> • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}</>
                                          )}</>
                                        )}
                                      </Text>
                                    ))}
                                  </Stack>
                                </Box>
                              </Group>
                            </Box>
                          );
                        });
                      })()}

                      {/* Cancel mode action buttons */}
                      <Divider />
                      <Group justify="center">
                        <Button
                          variant="filled"
                          color="red"
                          onClick={handleConfirmTicketCancel}
                          disabled={selectedTicketPurchaseIds.length === 0}
                          styles={{
                            root: {
                              height: '40px',
                              fontSize: '14px'
                            }
                          }}
                        >
                          Cancel Selected ({selectedTicketPurchaseIds.length})
                        </Button>
                      </Group>
                    </Stack>
                  ) : (
                    /* Normal Mode: Show tickets grouped by purchase */
                    <>
                      <Stack gap="md">
                        {(() => {
                          // Get ticket purchases dictionary (includes ticket type name and price)
                          const ticketPurchases = validParticipation?.ticketPurchases as Record<string, { ticketTypeName?: string; sessionIds?: string[]; totalPrice?: number }> | undefined;

                          // If ticketPurchases exists and has entries, show grouped by purchase
                          if (ticketPurchases && Object.keys(ticketPurchases).length > 0) {
                            return Object.entries(ticketPurchases).map(([purchaseId, purchase]) => {
                              const ticketSessions = eventSessions?.filter(s => purchase.sessionIds?.includes(s.id || '')) || [];
                              const ticketName = purchase.ticketTypeName || (ticketSessions.length > 1
                                ? `${ticketSessions.length}-Session Ticket`
                                : ticketSessions[0]?.name || 'Event Ticket');

                              return (
                                <Box key={purchaseId}>
                                  <Group gap="xs" align="flex-start">
                                    <IconTicket size={16} color="#6B0119" style={{ marginTop: 2, flexShrink: 0 }} />
                                    <Box style={{ flex: 1 }}>
                                      <Group justify="space-between" align="center">
                                        <Text size="sm" fw={500}>{ticketName}</Text>
                                        {purchase.totalPrice != null && purchase.totalPrice > 0 && (
                                          <Text size="sm" fw={600} c="#880124">${purchase.totalPrice.toFixed(2)}</Text>
                                        )}
                                      </Group>
                                      <Stack gap={4} mt={4}>
                                        {ticketSessions.map((session, idx) => (
                                          <Text key={session.id || idx} size="xs" c="dimmed">
                                            {session.name || session.sessionIdentifier || `Session ${idx + 1}`}
                                            {session.startTime && (
                                              <> – {formatUtcToLocalDate(session.startTime, eventTimeZone, {
                                                weekday: 'short',
                                                month: 'short',
                                                day: 'numeric'
                                              })}
                                              {session.endTime && (
                                                <> • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}</>
                                              )}</>
                                            )}
                                          </Text>
                                        ))}
                                      </Stack>
                                    </Box>
                                  </Group>
                                </Box>
                              );
                            });
                          }

                          // Fallback: Show flat session list from ownedSessionIds (legacy or single ticket)
                          const ownedSessions = eventSessions?.filter(session =>
                            (validParticipation as any).ownedSessionIds?.includes(session.id)
                          ) || [];

                          if (ownedSessions.length > 0) {
                            return ownedSessions.map((session, index) => (
                              <Group key={session.id || index} gap="xs" align="flex-start">
                                <IconTicket size={16} color="#6B0119" style={{ marginTop: 2, flexShrink: 0 }} />
                                <Box>
                                  <Text size="sm" fw={500}>
                                    {session.name || session.sessionIdentifier || `Session ${index + 1}`}
                                  </Text>
                                  <Text size="sm" c="dimmed">
                                    {session.startTime && formatUtcToLocalDate(session.startTime, eventTimeZone, {
                                      weekday: 'short',
                                      month: 'short',
                                      day: 'numeric'
                                    })}
                                    {session.startTime && session.endTime && (
                                      <> • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}</>
                                    )}
                                  </Text>
                                </Box>
                              </Group>
                            ));
                          }

                          // Ultimate fallback: Generic ticket info
                          return (
                            <Group gap="xs" align="center">
                              <IconTicket size={16} color="#6B0119" />
                              <Text size="sm">Event Ticket</Text>
                            </Group>
                          );
                        })()}
                      </Stack>

                      {/* Total purchase amount - sum of all ticket purchases */}
                      {validParticipation.ticket?.amount != null && validParticipation.ticket.amount > 0 && (
                        <>
                          <Divider />
                          <Group justify="space-between" align="center">
                            <Text fw={500} size="sm">Total Paid:</Text>
                            <Text fw={700} c="#880124">
                              ${validParticipation.ticket.amount.toFixed(2)}
                            </Text>
                          </Group>
                        </>
                      )}
                    </>
                  )}
                </Stack>
              </Paper>

              {/* Cancel button - only show when NOT in cancel mode */}
              {!isCancelMode && (
                <Box mt="md">
                  {validParticipation.canCancelTicket ? (
                    <Button
                      variant="light"
                      color="red"
                      onClick={() => handleCancelClick('ticket')}
                      fullWidth
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
                      Cancel Ticket
                    </Button>
                  ) : (
                    <Alert color="orange" variant="light">
                      <Text size="sm">Cancellations are not available at this time</Text>
                    </Alert>
                  )}
                </Box>
              )}

              {/* Additional Sessions - Show when user can purchase more sessions */}
              {(validParticipation as any)?.canPurchaseAdditionalSessions && (
                <Box mt="md">
                  <Paper
                    radius="md"
                    p="lg"
                    withBorder
                    style={{
                      backgroundColor: '#FAF6F2',
                      border: '1px solid #D4A5A5'
                    }}
                  >
                    <Stack gap="md">
                      <Title order={4} c="#880124">
                        Additional Sessions
                      </Title>

                      {/* Show available sessions user doesn't own - same format as Your Tickets */}
                      <Stack gap="xs">
                        {eventSessions
                          ?.filter(session => !(validParticipation as any).ownedSessionIds?.includes(session.id))
                          .map((session, index) => (
                            <Group key={session.id || index} gap="xs" align="flex-start">
                              <IconTicket size={16} color="#6B0119" style={{ marginTop: 2, flexShrink: 0 }} />
                              <Box>
                                <Text size="sm" fw={500}>
                                  {session.name || session.sessionIdentifier || `Session ${index + 1}`}
                                </Text>
                                <Text size="sm" c="dimmed">
                                  {session.startTime && formatUtcToLocalDate(session.startTime, eventTimeZone, {
                                    weekday: 'short',
                                    month: 'short',
                                    day: 'numeric'
                                  })}
                                  {session.startTime && session.endTime && (
                                    <> • {formatUtcTimeRange(session.startTime, session.endTime, eventTimeZone)}</>
                                  )}
                                </Text>
                              </Box>
                            </Group>
                          ))}
                      </Stack>
                    </Stack>
                  </Paper>
                  <Box mt="md">
                    <Button
                      onClick={handleTicketPurchase}
                      fullWidth
                      variant="outline"
                      color="blue"
                      disabled={isLoading || isLoadingUser}
                      data-testid="button-purchase-additional-tickets"
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
                      Get Additional Tickets
                    </Button>
                  </Box>
                </Box>
              )}
            </Box>
          )}

          {/* RSVP and Ticket Actions */}
          {!isAtCapacity && (
            <Stack gap="md">
              {/* Social Event Pattern: RSVP first, then optional ticket */}
              {eventType === 'social' && (
                <>
                  {/* Check if we should show "sales not open" message */}
                  {!validParticipation?.canPurchaseTicket && (validParticipation as any)?.ticketPurchaseMessage ? (
                    // Show only the sales message when sales aren't open
                    <Text
                      size="md"
                      ta="center"
                      style={{
                        color: '#000000',
                        fontWeight: 700,
                        lineHeight: 1.5
                      }}
                    >
                      {(() => {
                        const backendMessage = (validParticipation as any).ticketPurchaseMessage;
                        // Transform "Sales open Dec 14" → "RSVP and Ticket Sales open Dec 14"
                        return backendMessage.replace('Sales', 'RSVP and Ticket Sales');
                      })()}
                    </Text>
                  ) : (
                    // Show normal RSVP and ticket purchase UI when sales are open
                    <>
                      {/* Show RSVP button only if user hasn't RSVP'd yet AND backend allows */}
                      {!validParticipation?.hasRSVP && validParticipation?.canRSVP && (
                        <Box>
                          {/* Terms of Service Acceptance */}
                          <Group gap="sm" align="center" justify="center" mb="md">
                            <Checkbox
                              id="rsvp-terms-checkbox"
                              checked={rsvpTermsAccepted}
                              onChange={(event) => setRsvpTermsAccepted(event.currentTarget.checked)}
                              size="md"
                              color="var(--color-burgundy)"
                              data-testid="rsvp-terms-checkbox"
                            />
                            <Text
                              component="label"
                              htmlFor="rsvp-terms-checkbox"
                              size="md"
                              style={{
                                cursor: 'pointer',
                                color: '#000000',
                                fontWeight: 700,
                                lineHeight: 1.5
                              }}
                            >
                              I agree to the{' '}
                              <a
                                href="/event-waiver"
                                target="_blank"
                                rel="noopener noreferrer"
                                style={{
                                  color: 'var(--color-burgundy)',
                                  textDecoration: 'underline',
                                  fontWeight: 700
                                }}
                                onClick={(e) => e.stopPropagation()}
                              >
                                Event Waiver
                              </a>
                            </Text>
                          </Group>

                          {/* RSVP Button */}
                          <Button
                            onClick={handleRSVPClick}
                            fullWidth
                            variant="filled"
                            color="green"
                            disabled={!rsvpTermsAccepted || isLoading || isLoadingUser}
                            loading={isLoading || isLoadingUser}
                            mb="md"
                            data-testid="button-rsvp"
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
                            RSVP Now (Free)
                          </Button>
                        </Box>
                      )}

                      {/* Show ticket purchase option when user hasn't purchased a ticket yet AND sales are open */}
                      {!validParticipation?.hasTicket && validParticipation?.canPurchaseTicket && (
                        <Box>
                          <Text size="sm" c="dimmed" ta="center" mb="sm">
                            {validParticipation?.hasRSVP
                              ? "Support the event with an optional ticket purchase"
                              : "Purchase a ticket for this event"}
                          </Text>
                          <Button
                            onClick={handleTicketPurchase}
                            fullWidth
                            variant="outline"
                            color="blue"
                            disabled={isLoading || isLoadingUser}
                            loading={isLoading || isLoadingUser}
                            data-testid="button-purchase-ticket"
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
                            Purchase Ticket ({formatPriceDisplay()})
                          </Button>
                        </Box>
                      )}
                    </>
                  )}
                </>
              )}

              {/* Class Pattern: Ticket purchase required */}
              {eventType === 'class' && !validParticipation?.hasTicket && (
                <Box>
                  {/* Check if we should show "sales not open" message */}
                  {!validParticipation?.canPurchaseTicket && (validParticipation as any)?.ticketPurchaseMessage ? (
                    // Show only the sales message when sales aren't open
                    <Text
                      size="md"
                      ta="center"
                      style={{
                        color: '#000000',
                        fontWeight: 700,
                        lineHeight: 1.5
                      }}
                    >
                      {(() => {
                        const backendMessage = (validParticipation as any).ticketPurchaseMessage;
                        // Transform "Sales open Dec 14" → "Ticket Sales open Dec 14"
                        return backendMessage.replace('Sales', 'Ticket Sales');
                      })()}
                    </Text>
                  ) : (
                    // Show normal ticket purchase UI when sales are open
                    validParticipation?.canPurchaseTicket && (
                      <>
                        <Box
                          p={{ base: 'xs', md: 'md' }}
                          mb={{ base: 'xs', md: 'md' }}
                          style={{
                            background: 'var(--color-cream)',
                            borderRadius: '12px',
                            textAlign: 'center'
                          }}
                        >
                          <Text fw={700} size="lg" c="var(--color-burgundy)" mb="xs">
                            Class Fee: {formatPriceDisplay()}
                          </Text>
                          {ticketPriceRange && !ticketPriceRange.isSinglePrice && (
                            <Text size="sm" c="dimmed">
                              Multiple ticket options available
                            </Text>
                          )}
                        </Box>

                        <Button
                          onClick={handleTicketPurchase}
                          fullWidth
                          variant="filled"
                          color="blue"
                          leftSection={<IconTicket size={18} />}
                          data-testid="button-purchase-ticket"
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
                      </>
                    )
                  )}
                </Box>
              )}
            </Stack>
          )}

          {/* Waitlist Option */}
          {isAtCapacity && (
            <Button
              onClick={() => {/* TODO: Implement waitlist */}}
              fullWidth
              variant="outline"
              color="gray"
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
              Join Waitlist
            </Button>
          )}
        </Stack>
      </ParticipationCardShell>

      {/* Cancel Confirmation Modal */}
      <Modal
        opened={cancelModalOpen}
        onClose={handleCancelModal}
        title={`Cancel ${cancelType === 'rsvp' ? 'RSVP' : 'Ticket'}`}
        size="md"
        centered
      >
        <Stack gap="md">
          <Text size="sm">
            Are you sure you want to cancel your {cancelType === 'rsvp' ? 'RSVP' : 'ticket purchase'} for this event?
            {cancelType === 'rsvp' && ' This will free up a spot for other attendees.'}
          </Text>

          <Textarea
            label="Reason for cancellation (optional)"
            placeholder="Let us know why you're canceling..."
            value={cancelReason}
            onChange={(event) => setCancelReason(event.currentTarget.value)}
            minRows={3}
            maxRows={5}
          />

          <Group justify="flex-end" gap="sm">
            <Button
              variant="outline"
              color="gray"
              onClick={handleCancelModal}
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
              Keep {cancelType === 'rsvp' ? 'RSVP' : 'Ticket'}
            </Button>
            <Button
              variant="filled"
              color="red"
              onClick={handleConfirmCancel}
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
              Cancel {cancelType === 'rsvp' ? 'RSVP' : 'Ticket'}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
};

// Reusable card shell
const ParticipationCardShell: React.FC<{
  children: React.ReactNode;
  isLoading?: boolean
}> = ({ children, isLoading = false }) => (
  <Paper
    style={{
      background: 'var(--color-ivory)',
      borderRadius: '16px',
      padding: 'var(--space-lg)',
      boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
      border: '1px solid rgba(183, 109, 117, 0.1)',
      position: 'relative',
      minHeight: '160px'
    }}
  >
    <LoadingOverlay visible={isLoading} />
    {children}
  </Paper>
);

