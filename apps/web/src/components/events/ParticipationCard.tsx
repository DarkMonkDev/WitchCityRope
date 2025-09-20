// ParticipationCard component for RSVP and ticket purchase
import React, { useState } from 'react';
import {
  Paper, Stack, Alert, Group, Text, Box, Badge, Button,
  LoadingOverlay, Progress
} from '@mantine/core';
import { IconUsers, IconTicket, IconCalendarCheck, IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { useCurrentUser } from '../../lib/api/hooks/useAuth';
import {
  ParticipationStatusDto
} from '../../types/participation.types';
import { PayPalButton } from '../../features/payments/components/PayPalButton';
import type { PaymentEventInfo } from '../../features/payments/types/payment.types';
import { useConfirmPayPalPayment } from '../../lib/api/hooks/usePayments';

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
  ticketPrice = 50
}) => {
  const { data: user, isLoading: isLoadingUser } = useCurrentUser();
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
            href="/login"
            size="sm"
            className="btn btn-primary"
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
      const adminTeacherRoles = ['Admin', 'Administrator', 'Teacher'];
      isVetted = adminTeacherRoles.includes(user.role);
      console.log(`  - Checking role '${user.role}' against [${adminTeacherRoles.join(', ')}]: ${isVetted}`);
      if (isVetted) console.log('  ✅ isVetted = true (via admin/teacher role)');
    }

    // Legacy structure: Check roles array (fallback)
    if (!isVetted && 'roles' in user && Array.isArray(user.roles)) {
      const legacyRoles = ['Vetted', 'Teacher', 'Administrator', 'Admin'];
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
    const hasRSVP = validParticipation.participationType === 'RSVP' && validParticipation.status === 'Active';
    const hasTicket = validParticipation.participationType === 'Ticket' && validParticipation.status === 'Active';

    validParticipation = {
      ...validParticipation,
      hasRSVP,
      hasTicket,
      canRSVP: !hasRSVP && !hasTicket,
      canPurchaseTicket: !hasTicket
    };

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
            size="sm"
            className="btn btn-secondary"
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

  const handleConfirmCancel = (reason?: string) => {
    onCancel(cancelType, reason);
    setCancelModalOpen(false);
  };

  const handleTicketPurchase = () => {
    setSelectedAmount(ticketPrice);
    setSlidingScalePercentage(0);
    setShowPayPal(true);
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

          {/* Current Participation Status */}
          {(validParticipation?.hasRSVP || validParticipation?.hasTicket) && (
            <Box
              style={{
                background: 'var(--color-cream)',
                borderRadius: '12px',
                padding: 'var(--space-md)',
                border: '2px solid var(--color-success)'
              }}
            >
              <Group gap="sm" mb="sm">
                <IconCalendarCheck size={20} color="var(--color-success)" />
                <Text fw={600} c="var(--color-success)">You're Participating!</Text>
              </Group>

              {validParticipation.hasRSVP && (
                <Group justify="space-between" mb="sm">
                  <Badge
                    color="green"
                    variant="filled"
                    style={{ borderRadius: '12px 6px 12px 6px' }}
                  >
                    ✓ RSVP Confirmed
                  </Badge>
                  <button
                    className="btn btn-secondary"
                    onClick={() => handleCancelClick('rsvp')}
                    style={{ fontSize: '14px', padding: '6px 12px' }}
                  >
                    Cancel RSVP
                  </button>
                </Group>
              )}

              {validParticipation.hasTicket && (
                <Group justify="space-between" mb="sm">
                  <Badge
                    color="red"
                    variant="filled"
                    style={{ borderRadius: '12px 6px 12px 6px' }}
                  >
                    🎫 Ticket Purchased
                  </Badge>
                  <button
                    className="btn btn-secondary"
                    onClick={() => handleCancelClick('ticket')}
                    style={{ fontSize: '14px', padding: '6px 12px' }}
                  >
                    Cancel Ticket
                  </button>
                </Group>
              )}
            </Box>
          )}

          {/* RSVP and Ticket Actions */}
          {!isAtCapacity && (
            <Stack gap="md">
              {/* Social Event Pattern: RSVP first, then optional ticket */}
              {eventType === 'social' && (
                <>
                  {/* Show RSVP status or button */}
                  {validParticipation?.hasRSVP ? (
                    <Box
                      style={{
                        background: 'var(--color-success-light)',
                        borderRadius: '12px',
                        padding: 'var(--space-md)',
                        border: '2px solid var(--color-success)',
                        marginBottom: 'var(--space-md)'
                      }}
                    >
                      <Group gap="sm">
                        <IconCheck size={20} color="var(--color-success)" />
                        <Text fw={600} c="var(--color-success)">You're RSVP'd!</Text>
                      </Group>
                      <Text size="sm" c="dimmed" mt="xs">
                        Your spot is confirmed for this event
                      </Text>
                    </Box>
                  ) : (
                    (() => {
                      const canRSVPCondition = validParticipation?.canRSVP || validParticipation === null || isLoading;

                      console.log('🔍 RSVP BUTTON LOGIC DEBUG:');
                      console.log('  - validParticipation?.hasRSVP:', validParticipation?.hasRSVP);
                      console.log('  - validParticipation?.canRSVP:', validParticipation?.canRSVP);
                      console.log('  - validParticipation === null:', validParticipation === null);
                      console.log('  - isLoading:', isLoading);
                      console.log('  - canRSVPCondition:', canRSVPCondition);

                      return canRSVPCondition;
                    })() && (
                      <button
                        className="btn btn-primary"
                        onClick={handleRSVPClick}
                        style={{ width: '100%', marginBottom: 'var(--space-md)' }}
                        disabled={isLoading || isLoadingUser}
                      >
                        {isLoading || isLoadingUser ? 'Loading...' : 'RSVP Now (Free)'}
                      </button>
                    )
                  )}

                  {/* Show ticket purchase option when:
                      1. participation allows ticket purchase OR participation is still loading
                      2. AND user hasn't already purchased a ticket
                  */}
                  {(validParticipation?.canPurchaseTicket || (validParticipation === null && !isLoading)) && !validParticipation?.hasTicket && (
                    <Box>
                      <Text size="sm" c="dimmed" ta="center" mb="sm">
                        Support the event with an optional ticket purchase
                      </Text>
                      <button
                        className="btn btn-secondary"
                        onClick={handleTicketPurchase}
                        style={{ width: '100%' }}
                        disabled={isLoading || isLoadingUser}
                      >
                        {isLoading || isLoadingUser ? 'Loading...' : `Purchase Ticket ($${ticketPrice})`}
                      </button>
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

                  {!showPayPal ? (
                    <button
                      className="btn btn-primary"
                      onClick={handleTicketPurchase}
                      style={{ width: '100%' }}
                    >
                      <IconTicket size={18} style={{ marginRight: '8px' }} />
                      Purchase Ticket with PayPal
                    </button>
                  ) : (
                    <Box>
                      <PayPalButton
                        eventInfo={paypalEventInfo}
                        amount={selectedAmount}
                        slidingScalePercentage={slidingScalePercentage}
                        onPaymentSuccess={handlePayPalSuccess}
                        onPaymentError={handlePayPalError}
                        onPaymentCancel={handlePayPalCancel}
                        disabled={confirmPayPalPayment.isPending}
                      />

                      <Button
                        variant="subtle"
                        size="sm"
                        onClick={() => setShowPayPal(false)}
                        mt="sm"
                        style={{ width: '100%' }}
                      >
                        Cancel Payment
                      </Button>
                    </Box>
                  )}
                </Box>
              )}
            </Stack>
          )}

          {/* Waitlist Option */}
          {isAtCapacity && (
            <button
              className="btn btn-secondary"
              onClick={() => {/* TODO: Implement waitlist */}}
              style={{ width: '100%' }}
            >
              Join Waitlist
            </button>
          )}
        </Stack>
      </ParticipationCardShell>
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

