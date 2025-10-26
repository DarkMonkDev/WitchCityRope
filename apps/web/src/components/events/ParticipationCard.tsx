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
  LoadingOverlay, Progress, Modal, Textarea
} from '@mantine/core';
import { IconUsers, IconTicket, IconCalendarCheck, IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import {
  ParticipationStatusDto
} from '../../types/participation.types';
import { PayPalButton } from '../../features/payments/components/PayPalButton';
import type { PaymentEventInfo } from '../../features/payments/types/payment.types';
import { useConfirmPayPalPayment } from '../../lib/api/hooks/usePayments';
import { PaymentSummary } from '../../features/payments/components/PaymentSummary';
import type { components } from '@witchcityrope/shared-types/generated/api-types';

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

interface ParticipationCardProps {
  eventId: string;
  eventTitle: string;
  eventType: 'social' | 'class';
  participation: ParticipationStatusDto | null;
  isLoading?: boolean;
  onRSVP: (notes?: string) => void;
  onPurchaseTicket: (amount: number, slidingScalePercentage?: number) => void;
  onCancel: (type: 'rsvp' | 'ticket', reason?: string) => void;
  ticketPrice?: number;
  eventStartDateTime?: string;
  eventEndDateTime?: string;
  eventInstructor?: string;
  eventLocation?: string;
}

export const ParticipationCard: React.FC<ParticipationCardProps> = ({
  eventId,
  eventTitle,
  eventType,
  participation,
  isLoading = false,
  onRSVP,
  onPurchaseTicket,
  onCancel,
  ticketPrice = 50,
  eventStartDateTime,
  eventEndDateTime,
  eventInstructor,
  eventLocation
}) => {
  const { data: user, isLoading: isLoadingUser } = useCurrentUser();
  const navigate = useNavigate();
  const isAuthenticated = !!user;

  // DEBUG: Log all relevant data for RSVP button troubleshooting
  console.log('🔍 ParticipationCard DEBUG DATA:');
  console.log('  - eventType:', eventType);
  console.log('  - user (full object):', user);
  console.log('  - isAuthenticated:', isAuthenticated);
  console.log('  - participation:', participation);
  console.log('  - participation?.canRSVP:', participation?.canRSVP);
  console.log('  - participation?.hasRSVP:', participation?.hasRSVP);
  console.log('  - isLoading:', isLoading);
  console.log('  - isLoadingUser:', isLoadingUser);
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelType, setCancelType] = useState<'rsvp' | 'ticket'>('rsvp');
  const [cancelReason, setCancelReason] = useState('');
  const [showPayPal, setShowPayPal] = useState(false);
  const [selectedAmount, setSelectedAmount] = useState(ticketPrice);
  const [slidingScalePercentage, setSlidingScalePercentage] = useState(0);

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

  console.log('🔍 VETTING LOGIC DEBUG:');
  console.log('  - user exists:', !!user);
  console.log('  - user type:', typeof user);

  if (user && typeof user === 'object') {
    console.log('  - user.isVetted:', (user as any).isVetted);
    console.log('  - user.role:', (user as any).role);
    console.log('  - user.roles:', (user as any).roles);

    // New structure: Check isVetted boolean OR admin/teacher role
    if ('isVetted' in user && user.isVetted === true) {
      isVetted = true;
      console.log('  ✅ isVetted = true (via isVetted boolean)');
    } else if ('role' in user && typeof user.role === 'string') {
      const adminTeacherRoles = ['Administrator', 'Teacher'];
      isVetted = adminTeacherRoles.includes(user.role);
      console.log(`  - Checking role '${user.role}' against [${adminTeacherRoles.join(', ')}]: ${isVetted}`);
      if (isVetted) console.log('  ✅ isVetted = true (via admin/teacher role)');
    }

    // Legacy structure: Check roles array (fallback)
    if (!isVetted && 'roles' in user && Array.isArray(user.roles)) {
      const legacyRoles = ['Vetted', 'Teacher', 'Administrator'];
      isVetted = user.roles.some(role => legacyRoles.includes(role));
      console.log(`  - Checking roles array [${user.roles.join(', ')}] against [${legacyRoles.join(', ')}]: ${isVetted}`);
      if (isVetted) console.log('  ✅ isVetted = true (via legacy roles array)');
    }
  }

  console.log('  🎯 FINAL isVetted result:', isVetted);

  // Enhanced participation debugging
  console.log('🔍 PARTICIPATION STATUS DEBUG:');
  console.log('  - participation is null:', participation === null);
  console.log('  - participation is undefined:', participation === undefined);
  console.log('  - participation type:', typeof participation);
  console.log('  - participation value:', participation);
  console.log('  - isLoading:', isLoading);
  console.log('  - isLoadingUser:', isLoadingUser);

  // Handle invalid participation data and normalize structure
  let validParticipation = (participation && typeof participation === 'object') ? participation : null;

  // Check if participation has the old structure (from API) and normalize it
  if (validParticipation && 'participationType' in validParticipation && !('hasRSVP' in validParticipation)) {
    console.log('🔍 Converting old participation structure to new format');
    const participationAny = validParticipation as any;
    console.log('🔍 Participation metadata:', participationAny.metadata);
    console.log('🔍 Extracted amount:', extractAmountFromMetadata(participationAny.metadata));
    const hasRSVP = participationAny.participationType === 'RSVP' && participationAny.status === 'Active';
    const hasTicket = participationAny.participationType === 'Ticket' && participationAny.status === 'Active';

    // Create proper nested structure with actual backend data
    validParticipation = {
      ...(validParticipation as any),
      hasRSVP,
      hasTicket,
      canRSVP: !hasRSVP && !hasTicket,
      canPurchaseTicket: !hasTicket,
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
    } as ParticipationStatusDto;

    console.log('🔍 Converted participation:', validParticipation);
  }

  if (eventType === 'social' && !isVetted) {
    return (
      <ParticipationCardShell>
        <Alert
          icon={<IconAlertCircle size={16} />}
          title="Vetting Required"
          variant="light"
          color="orange"
        >
          <Text size="sm" mb="md">
            This social event is limited to vetted members. Complete the vetting process to attend.
          </Text>
          <Button
            component="a"
            href="/vetting"
            variant="outline"
            color="orange"
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
        </Alert>
      </ParticipationCardShell>
    );
  }

  // Check capacity
  const isAtCapacity = validParticipation?.capacity && validParticipation.capacity.available <= 0;

  const handleRSVPClick = () => {
    // Direct RSVP without modal confirmation
    onRSVP();
  };

  const handleCancelClick = (type: 'rsvp' | 'ticket') => {
    setCancelType(type);
    setCancelModalOpen(true);
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

  const handleTicketPurchase = () => {
    setSelectedAmount(ticketPrice);
    setSlidingScalePercentage(0);

    // Navigate to checkout page with event info
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
        }
      }
    });
  };

  const handlePayPalSuccess = async (paymentDetails: any) => {
    console.log('🔍 PayPal payment successful:', paymentDetails);

    try {
      // Confirm payment with backend and create ticket
      await confirmPayPalPayment.mutateAsync({
        orderId: paymentDetails.id,
        paymentDetails
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
    console.log('🔍 PayPal payment cancelled');
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
          {/* Capacity Status */}
          {validParticipation?.capacity && (
            <Box>
              <Group justify="space-between" mb="xs">
                <Text size="sm" fw={600}>Event Capacity</Text>
                <Text size="sm" c="dimmed">
                  {validParticipation.capacity.current} / {validParticipation.capacity.total}
                </Text>
              </Group>
              <Progress
                value={(validParticipation.capacity.current / validParticipation.capacity.total) * 100}
                color={validParticipation.capacity.available <= 3 ? 'red' : 'green'}
                size="sm"
                radius="md"
              />
              {validParticipation.capacity.available <= 3 && validParticipation.capacity.available > 0 && (
                <Text size="xs" c="orange" mt="xs" ta="center">
                  ⚠️ Only {validParticipation.capacity.available} spots left!
                </Text>
              )}
            </Box>
          )}

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
                boxShadow: '0 4px 12px rgba(76, 175, 80, 0.15)'
              }}
            >
              <Group gap="sm" mb="md">
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
              <Button
                variant="light"
                color="red"
                onClick={() => handleCancelClick('rsvp')}
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
            </Box>
          )}

          {/* Ticket Purchase Summary */}
          {validParticipation?.hasTicket && (
            <Box>
              <PaymentSummary
                eventInfo={{
                  id: eventId,
                  title: eventTitle,
                  startDateTime: eventStartDateTime || new Date().toISOString(),
                  endDateTime: eventEndDateTime || new Date().toISOString(),
                  currency: 'USD',
                  location: eventLocation,
                  instructorName: eventInstructor,
                  basePrice: validParticipation.ticket?.amount || 0,
                  registrationId: eventId
                }}
                selectedTickets={[
                  {
                    id: validParticipation.ticket?.id || '',
                    name: 'Event Ticket',
                    pricingType: 'Fixed' as const,
                    sessionIdentifiers: [],
                    price: validParticipation.ticket?.amount || 0,
                    minPrice: null,
                    maxPrice: null,
                    defaultPrice: null,
                    quantityAvailable: 0,
                    salesEndDate: null
                  } as TicketTypeDto
                ]}
                ticketPrices={{
                  [validParticipation.ticket?.id || '']: validParticipation.ticket?.amount || 0
                }}
                detailed={false}
                title="Your Ticket Purchase"
              />
              <Box mt="md">
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
              </Box>
            </Box>
          )}

          {/* RSVP and Ticket Actions */}
          {!isAtCapacity && (
            <Stack gap="md">
              {/* Social Event Pattern: RSVP first, then optional ticket */}
              {eventType === 'social' && (
                <>
                  {/* Show RSVP button only if user hasn't RSVP'd yet */}
                  {!validParticipation?.hasRSVP && (
                    (() => {
                      const canRSVPCondition = validParticipation?.canRSVP || validParticipation === null || isLoading;

                      console.log('🔍 RSVP BUTTON LOGIC DEBUG:');
                      console.log('  - validParticipation?.hasRSVP:', validParticipation?.hasRSVP);
                      console.log('  - validParticipation?.canRSVP:', validParticipation?.canRSVP);
                      console.log('  - validParticipation === null:', validParticipation === null);
                      console.log('  - isLoading:', isLoading);
                      console.log('  - canRSVPCondition:', canRSVPCondition);

                      return canRSVPCondition ? (
                        <Button
                          onClick={handleRSVPClick}
                          fullWidth
                          variant="filled"
                          color="green"
                          disabled={isLoading || isLoadingUser}
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
                      ) : null;
                    })()
                  )}

                  {/* Show ticket purchase option when user hasn't purchased a ticket yet */}
                  {!validParticipation?.hasTicket && (
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
                        Purchase Ticket (${ticketPrice})
                      </Button>
                    </Box>
                  )}
                </>
              )}

              {/* Class Pattern: Ticket purchase required */}
              {eventType === 'class' && !validParticipation?.hasTicket && (
                <Box>
                  <Box
                    style={{
                      background: 'var(--color-cream)',
                      borderRadius: '12px',
                      padding: 'var(--space-md)',
                      textAlign: 'center',
                      marginBottom: 'var(--space-md)'
                    }}
                  >
                    <Text fw={700} size="lg" c="var(--color-burgundy)" mb="xs">
                      Class Fee: ${ticketPrice}
                    </Text>
                    <Text size="sm" c="dimmed">
                      Sliding scale pricing available ($50-75 suggested)
                    </Text>
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
      minHeight: '200px'
    }}
  >
    <LoadingOverlay visible={isLoading} />
    {children}
  </Paper>
);

