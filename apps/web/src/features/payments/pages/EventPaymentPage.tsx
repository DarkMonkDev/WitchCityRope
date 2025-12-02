// Event Payment Page
// Complete payment flow for event registration

import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import {
  Container,
  Stack,
  Group,
  Button,
  Stepper,
  Text,
  Alert,
  LoadingOverlay,
  Paper,
  Box,
  Checkbox
} from '@mantine/core';
import { IconArrowLeft, IconAlertCircle, IconCheck } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import type { components } from '@witchcityrope/shared-types/generated/api-types';
import { debugLog } from '../../../utils/debug';

import { SlidingScaleSelector } from '../components/SlidingScaleSelector';
import { PaymentForm } from '../components/PaymentForm';
import { PaymentConfirmation } from '../components/PaymentConfirmation';
import { PaymentSummary } from '../components/PaymentSummary';
import { usePayment } from '../hooks/usePayment';
import { useSlidingScale } from '../hooks/useSlidingScale';
import { usePurchaseTicket } from '../../../lib/api/hooks/usePayments';
import { eventsManagementService } from '../../../api/services/eventsManagement.service';
import { formatAbbreviatedDate } from '../../../utils/eventUtils';

import type { PaymentEventInfo } from '../types/payment.types';

// Use generated types from OpenAPI spec
type EventDto = components["schemas"]["EventDto"];
type TicketTypeDto = components["schemas"]["TicketTypeDto"];
type SessionDto = components["schemas"]["SessionDto"];

/**
 * Main event payment page with complete payment flow
 */
export const EventPaymentPage: React.FC = () => {
  const { eventId, registrationId: urlRegistrationId } = useParams<{
    eventId: string;
    registrationId: string;
  }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Generate registration ID if not provided in URL
  const [registrationId] = useState(() =>
    urlRegistrationId || `reg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  );

  // Payment flow state
  const [currentStep, setCurrentStep] = useState(0);
  const [eventInfo, setEventInfo] = useState<PaymentEventInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [completedPayment, setCompletedPayment] = useState<any | null>(null);
  const [ticketTypes, setTicketTypes] = useState<TicketTypeDto[]>([]);
  const [sessions, setSessions] = useState<SessionDto[]>([]);
  const [selectedTicketTypeIds, setSelectedTicketTypeIds] = useState<string[]>([]);
  // Track the price for each selected ticket (ticketId -> price)
  const [ticketPrices, setTicketPrices] = useState<Record<string, number>>({});

  // Payment processing
  const { 
    processPayment, 
    processingState, 
    paymentData, 
    resetProcessingState 
  } = usePayment(registrationId);

  // Sliding scale management
  const {
    finalAmount,
    discountPercentage,
    calculation,
    updateDiscountPercentage
  } = useSlidingScale(eventInfo?.basePrice || 0, 0);

  // Ticket purchase mutation
  const purchaseTicket = usePurchaseTicket();

  /**
   * Load event information from API
   */
  useEffect(() => {
    const loadEventInfo = async () => {
      try {
        setIsLoading(true);
        setError(null);

        if (!eventId) {
          setError('Event ID is required');
          setIsLoading(false);
          return;
        }

        debugLog('EventPaymentPage: Loading event details for eventId:', eventId);

        // Fetch real event data from API using generated types
        const eventDetails: EventDto = await eventsManagementService.getEventDetails(eventId);

        debugLog('EventPaymentPage: Received event details:', eventDetails);

        // Extract ticket types and sessions
        const eventTicketTypes = eventDetails?.ticketTypes || [];
        const eventSessions = eventDetails?.sessions || [];
        setTicketTypes(eventTicketTypes);
        setSessions(eventSessions);

        // Auto-select ticket(s): if only one ticket, select it automatically; otherwise use URL param or empty
        let initialSelectedIds: string[] = [];
        const initialPrices: Record<string, number> = {};

        if (eventTicketTypes.length === 1) {
          // Only one ticket - auto-select it
          const ticket = eventTicketTypes[0];
          initialSelectedIds = [ticket.id || ''];
          // Set initial price based on ticket type
          const price = ticket.pricingType === 'Fixed'
            ? (ticket.price ?? 0)
            : (ticket.defaultPrice ?? ticket.minPrice ?? 0); // Use default price for sliding scale
          initialPrices[ticket.id || ''] = price;
        } else if (searchParams.get('ticketTypeId')) {
          // URL param provided - select that one
          const ticketId = searchParams.get('ticketTypeId')!;
          const ticket = eventTicketTypes.find(t => t.id === ticketId);
          if (ticket) {
            initialSelectedIds = [ticketId];
            const price = ticket.pricingType === 'Fixed'
              ? (ticket.price ?? 0)
              : (ticket.defaultPrice ?? ticket.minPrice ?? 0);
            initialPrices[ticketId] = price;
          }
        }

        setSelectedTicketTypeIds(initialSelectedIds);
        setTicketPrices(initialPrices);

        // Calculate base price from first selected ticket (or first available)
        const firstSelectedId = initialSelectedIds[0] || eventTicketTypes[0]?.id;
        const selectedTicket = eventTicketTypes.find((tt) => tt.id === firstSelectedId) || eventTicketTypes[0];
        // Use correct price field based on pricing type
        const basePrice = selectedTicket?.pricingType === 'Fixed'
          ? (selectedTicket?.price ?? 0)
          : (selectedTicket?.minPrice ?? 0);

        // Transform to PaymentEventInfo format
        const paymentEventInfo: PaymentEventInfo = {
          id: eventDetails?.id || '',
          title: eventDetails?.title || '',
          startDateTime: eventDetails?.startDate || new Date().toISOString(),
          endDateTime: eventDetails?.endDate || new Date().toISOString(),
          instructorName: 'Instructor TBD', // teacherIds is currently empty in API response
          location: '', // TODO: Fetch venue name using venueId once venue API is available
          basePrice,
          currency: 'USD',
          registrationId: registrationId
        };

        debugLog('EventPaymentPage: Transformed payment event info:', paymentEventInfo);
        debugLog('EventPaymentPage: Ticket types:', eventTicketTypes);
        debugLog('EventPaymentPage: Selected ticket type IDs:', initialSelectedIds);
        setEventInfo(paymentEventInfo);
      } catch (err: any) {
        console.error('EventPaymentPage: Error loading event info:', err);
        setError(err.message || 'Failed to load event information. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    if (eventId) {
      loadEventInfo();
    } else {
      setError('Missing event information');
      setIsLoading(false);
    }
  }, [eventId, registrationId]);

  /**
   * Handle successful payment
   */
  const handlePaymentSuccess = async (paymentDetails: any) => {
    debugLog('Payment success received:', paymentDetails);

    // Calculate total amount from selected tickets
    const totalAmount = Object.values(ticketPrices).reduce((sum, price) => sum + price, 0);

    // Create payment data object for confirmation page
    const paymentData = {
      id: typeof paymentDetails === 'string' ? paymentDetails : paymentDetails.id,
      transactionId: typeof paymentDetails === 'string' ? paymentDetails : paymentDetails.transactionId || paymentDetails.id,
      amount: totalAmount,
      currency: 'USD',
      status: 'completed',
      paymentMethod: typeof paymentDetails === 'string' ? 'credit_card' : paymentDetails.method || 'credit_card',
      createdAt: new Date().toISOString(),
      eventRegistrationId: registrationId || '',
      confirmationNumber: `WCR-${new Date().getFullYear()}-${String(Date.now()).slice(-6)}`,
      cardLast4: typeof paymentDetails === 'object' && paymentDetails.cardLast4 ? paymentDetails.cardLast4 : undefined
    };

    // Store the payment data locally
    setCompletedPayment(paymentData);

    try {
      // CRITICAL: Actually create the ticket purchase in the database
      debugLog('🔍 Creating ticket purchase for event:', eventId);
      debugLog('🔍 Payment details:', paymentData);
      debugLog('🔍 Total amount:', totalAmount);
      debugLog('🔍 Selected tickets:', selectedTickets.map(t => ({ id: t.id, name: t.name })));
      debugLog('🔍 Ticket prices:', ticketPrices);

      if (!eventId) {
        throw new Error('Event ID is required');
      }

      // Create a metadata object with purchase details
      const metadata = JSON.stringify({
        paymentMethodId: paymentData.transactionId,
        purchaseAmount: totalAmount,
        ticketCount: selectedTickets.length,
        ticketTypes: selectedTickets.map(t => ({ id: t.id, name: t.name, price: ticketPrices[t.id || ''] })),
        confirmationNumber: paymentData.confirmationNumber,
        cardLast4: paymentData.cardLast4
      });

      // Call the API to create the ticket purchase
      await purchaseTicket.mutateAsync({
        eventId: eventId,
        notes: metadata,
        paymentMethodId: paymentData.transactionId,
        eventWaiverAccepted: true // UI enforces checkbox must be checked before payment
      });

      debugLog('✅ Ticket purchase created successfully in database');

      // Move to confirmation step
      setCurrentStep(2);

      // Note: Success notification is already shown by usePurchaseTicket hook
    } catch (error: any) {
      console.error('❌ Failed to create ticket purchase:', error);

      // Extract the most helpful error message from API response
      const errorMessage =
        error.response?.data?.detail ||
        error.response?.data?.title ||
        error.response?.data?.message ||
        error.message ||
        'Payment succeeded but ticket creation failed. Please contact support.';

      notifications.show({
        title: 'Purchase Error',
        message: errorMessage,
        color: 'red',
        autoClose: false
      });
    }
  };

  /**
   * Handle payment error
   */
  const handlePaymentError = (errorMessage: string) => {
    notifications.show({
      title: 'Payment Failed',
      message: errorMessage,
      color: 'red'
    });
  };

  /**
   * Go back to previous step
   */
  const handleBack = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
      setCompletedPayment(null); // Reset completed payment
      resetProcessingState();
    } else {
      navigate(-1);
    }
  };

  /**
   * Continue to next step
   */
  const handleContinue = () => {
    if (currentStep < 2) {
      setCurrentStep(currentStep + 1);
    }
  };

  /**
   * Navigate to user registrations
   */
  const handleViewRegistrations = () => {
    navigate('/dashboard');
  };

  /**
   * Navigate to browse more events
   */
  const handleRegisterMore = () => {
    navigate('/events');
  };

  /**
   * Get all selected tickets
   */
  const selectedTickets = ticketTypes.filter((tt) => selectedTicketTypeIds.includes(tt.id || ''));

  /**
   * Determine pricing display based on first selected ticket
   * If multiple tickets, we'll show a summary instead
   */
  const firstSelectedTicket = selectedTickets[0];

  // DEBUG: Log the ticket type to see what value it actually has
  debugLog('🔍 CHECKOUT DEBUG:');
  debugLog('  - Selected ticket IDs:', selectedTicketTypeIds);
  debugLog('  - Selected tickets:', selectedTickets);
  debugLog('  - First selected ticket:', firstSelectedTicket);
  debugLog('  - All ticket types:', ticketTypes);

  // Check if ANY selected ticket has sliding scale pricing
  const hasAnySlidingScaleTicket = selectedTickets.some(ticket => ticket.pricingType === 'SlidingScale');
  const firstSlidingTicket = selectedTickets.find(ticket => ticket.pricingType === 'SlidingScale');
  const isSingleTicketSelected = selectedTickets.length === 1;
  debugLog('  - hasAnySlidingScaleTicket:', hasAnySlidingScaleTicket);
  debugLog('  - firstSlidingTicket:', firstSlidingTicket);
  debugLog('  - isSingleTicketSelected:', isSingleTicketSelected);

  /**
   * Handle ticket type checkbox toggle
   */
  const handleTicketTypeToggle = (ticketTypeId: string, checked: boolean) => {
    let newSelectedIds: string[];
    const newPrices = { ...ticketPrices };

    if (checked) {
      // Add ticket to selection
      newSelectedIds = [...selectedTicketTypeIds, ticketTypeId];

      // Set initial price for this ticket
      const ticket = ticketTypes.find(tt => tt.id === ticketTypeId);
      if (ticket) {
        const price = ticket.pricingType === 'Fixed'
          ? (ticket.price ?? 0)
          : (ticket.defaultPrice ?? ticket.minPrice ?? 0);
        newPrices[ticketTypeId] = price;
      }
    } else {
      // Remove ticket from selection
      newSelectedIds = selectedTicketTypeIds.filter(id => id !== ticketTypeId);
      // Remove price for this ticket
      delete newPrices[ticketTypeId];
    }

    setSelectedTicketTypeIds(newSelectedIds);
    setTicketPrices(newPrices);

    // Update event info with base price from first selected ticket
    if (newSelectedIds.length > 0) {
      const firstTicket = ticketTypes.find((tt) => tt.id === newSelectedIds[0]);
      if (firstTicket && eventInfo) {
        const newBasePrice = newPrices[newSelectedIds[0]] || 0;
        setEventInfo({
          ...eventInfo,
          basePrice: newBasePrice
        });
        updateDiscountPercentage(0); // Reset discount when ticket type changes
      }
    }
  };

  /**
   * Get formatted session dates for a ticket
   * Returns abbreviated date string like "Sun, Dec 1" or "Sun, Dec 1 • Sat, Dec 7"
   */
  const getTicketSessionDates = (ticket: TicketTypeDto): string => {
    if (!ticket.sessionIdentifiers || ticket.sessionIdentifiers.length === 0) {
      return '';
    }

    // Find sessions matching the ticket's session identifiers
    const matchingSessions = sessions.filter(session =>
      ticket.sessionIdentifiers?.includes(session.sessionIdentifier || '')
    );

    if (matchingSessions.length === 0) {
      return '';
    }

    // Format each session date and join with bullet separator
    const formattedDates = matchingSessions
      .filter(session => session.date) // Only include sessions with dates
      .map(session => formatAbbreviatedDate(session.date!))
      .join(' • ');

    return formattedDates;
  };

  // Show loading state
  if (isLoading) {
    return (
      <Container size="md" py="xl">
        <LoadingOverlay visible />
        <Text ta="center" c="dimmed">
          Loading payment information...
        </Text>
      </Container>
    );
  }

  // Show error state
  if (error || !eventInfo) {
    return (
      <Container size="md" py="xl">
        <Alert
          icon={<IconAlertCircle />}
          title="Error"
          color="red"
        >
          {error || 'Failed to load payment information'}
        </Alert>
        <Group justify="center" mt="md">
          <Button variant="outline" onClick={() => navigate(-1)}>
            Go Back
          </Button>
        </Group>
      </Container>
    );
  }

  return (
    <Container size="lg" py="xl">
      <Stack gap="md">
        {/* Header - Back button and secure payment notice */}
        <Group justify="space-between" align="center" mt={0} mb={{ base: 0, md: 'xs' }}>
          {/* Hide Back button on confirmation screen (Step 3) */}
          {currentStep < 2 ? (
            <Button
              variant="subtle"
              leftSection={<IconArrowLeft size={16} />}
              onClick={handleBack}
              color="wcr"
            >
              Back
            </Button>
          ) : (
            <Box />
          )}
          <Text c="dimmed" size="sm">
            Secure Payment • SSL Encrypted
          </Text>
        </Group>

        {/* Mobile Step Indicator - Simple Text */}
        <Box hiddenFrom="md" mb={0}>
          <Text
            size="lg"
            fw={500}
            c="wcr.7"
            ta="center"
            className="checkout-step-indicator"
          >
            Step {currentStep + 1} of 3: {
              currentStep === 0
                ? (!hasAnySlidingScaleTicket ? "Ticket Selection" : "Pricing")
                : currentStep === 1
                  ? "Payment"
                  : "Confirmation"
            }
          </Text>
        </Box>

        {/* Desktop/Tablet Progress Stepper - Full Version */}
        <Box visibleFrom="md" mb="1rem">
          <Stepper
            active={currentStep}
            color={currentStep === 2 ? 'green' : '#880124'}
            iconSize={32}
            styles={{
              stepIcon: {
                borderWidth: 2
              }
            }}
          >
            <Stepper.Step
              label={!hasAnySlidingScaleTicket ? "Ticket Selection" : "Pricing"}
              description={!hasAnySlidingScaleTicket ? "Review ticket details" : "Choose your amount"}
            />
            <Stepper.Step
              label="Payment"
              description="Enter payment details"
            />
            <Stepper.Step
              label="Confirmation"
              description="Registration complete"
              icon={currentStep === 2 ? <IconCheck size={18} /> : undefined}
            />
          </Stepper>
        </Box>

        {/* Step Content */}
        <Group align="flex-start" gap="xl">
          {/* Main Content */}
          <Stack gap="md" style={{ flex: 2 }}>
            {/* Step 1: Ticket Type and Pricing Selection */}
            {currentStep === 0 && (
              <>
                {/* Ticket Type Selection */}
                {ticketTypes.length > 0 && (
                  <Paper p="lg" pt={{ base: 14, md: 'lg' }} radius="md" mb={{ base: 0, md: 'xs' }} style={{ background: 'var(--mantine-color-gray-0)' }}>
                    <Stack gap="sm">
                      <Text fw={600} size="lg">
                        {ticketTypes.length === 1 ? 'Ticket' : 'Select Tickets'}
                      </Text>
                      <Stack gap="md">
                        {ticketTypes.map((tt) => {
                          // Format price display based on pricing type
                          let priceDisplay = '';
                          if (tt.pricingType === 'Fixed') {
                            const price = tt.price ?? 0;
                            priceDisplay = `$${price.toFixed(2)}`;
                          } else if (tt.pricingType === 'SlidingScale') {
                            const minPrice = tt.minPrice ?? 0;
                            const maxPrice = tt.maxPrice ?? 0;
                            priceDisplay = `$${minPrice.toFixed(2)} - $${maxPrice.toFixed(2)}`;
                          } else {
                            priceDisplay = 'Price TBD';
                          }

                          const isSelected = selectedTicketTypeIds.includes(tt.id || '');
                          const showCheckbox = ticketTypes.length > 1;

                          return (
                            <Paper
                              key={tt.id || Math.random()}
                              p="md"
                              style={{
                                background: isSelected ? 'rgba(136, 1, 36, 0.05)' : 'white',
                                border: isSelected
                                  ? '2px solid var(--mantine-color-wcr-6)'
                                  : '1px solid var(--mantine-color-gray-3)',
                                cursor: showCheckbox ? 'pointer' : 'default',
                              }}
                              onClick={() => showCheckbox && handleTicketTypeToggle(tt.id, !isSelected)}
                            >
                              <Group justify="space-between" wrap="nowrap">
                                <Group gap="sm" style={{ flex: 1 }}>
                                  {showCheckbox && (
                                    <Checkbox
                                      checked={isSelected}
                                      onChange={(e) => {
                                        e.stopPropagation();
                                        handleTicketTypeToggle(tt.id, e.currentTarget.checked);
                                      }}
                                      color="wcr"
                                    />
                                  )}
                                  <Box style={{ flex: 1 }}>
                                    <Text fw={600} size="md">{tt.name}</Text>
                                    {(() => {
                                      const sessionDates = getTicketSessionDates(tt);
                                      if (sessionDates) {
                                        return (
                                          <Text size="sm" c="dimmed" mt={4}>
                                            {sessionDates}
                                          </Text>
                                        );
                                      }
                                      return null;
                                    })()}
                                  </Box>
                                </Group>
                                <Text fw={700} size="lg" c="#880124" style={{ whiteSpace: 'nowrap' }}>
                                  {priceDisplay}
                                </Text>
                              </Group>
                            </Paper>
                          );
                        })}
                      </Stack>
                      {selectedTickets.length > 1 && (
                        <Text size="sm" c="dimmed" mt="xs">
                          {selectedTickets.length} tickets selected
                        </Text>
                      )}
                    </Stack>
                  </Paper>
                )}

                {/* Sliding Scale Selector - only show if any selected ticket has sliding scale */}
                {hasAnySlidingScaleTicket && firstSlidingTicket && (
                  <SlidingScaleSelector
                    basePrice={eventInfo.basePrice}
                    currency={eventInfo.currency}
                    onAmountChange={(amount, percentage) => {
                      updateDiscountPercentage(percentage);
                      // Update all sliding scale tickets' prices in real-time
                      const updatedPrices = { ...ticketPrices };
                      selectedTickets.forEach(ticket => {
                        if (ticket.pricingType === 'SlidingScale') {
                          updatedPrices[ticket.id!] = amount;
                        }
                      });
                      setTicketPrices(updatedPrices);
                    }}
                    title="Choose Your Payment Amount"
                    forceSliding={true}
                    minPrice={firstSlidingTicket.minPrice}
                    maxPrice={firstSlidingTicket.maxPrice}
                    defaultPrice={firstSlidingTicket.defaultPrice}
                  />
                )}

                {/* Multiple tickets notice */}
                {selectedTickets.length > 1 && (
                  <Alert color="blue" variant="light">
                    <Text size="sm">
                      You've selected {selectedTickets.length} tickets. {hasAnySlidingScaleTicket && 'The sliding scale amount will apply to all sliding scale tickets.'}
                    </Text>
                  </Alert>
                )}

                <Group justify="flex-end" mt={0}>
                  <Button
                    onClick={handleContinue}
                    size="lg"
                    color="#880124"
                    disabled={selectedTickets.length === 0}
                    styles={(theme) => ({
                      root: {
                        background: selectedTickets.length === 0
                          ? 'var(--mantine-color-gray-3)'
                          : 'linear-gradient(135deg, #FFB800, #DAA520)',
                        border: 'none',
                        borderRadius: '12px 6px 12px 6px',
                        color: selectedTickets.length === 0 ? 'var(--mantine-color-gray-6)' : '#2C2C2C',
                        fontFamily: 'Montserrat, sans-serif',
                        fontWeight: 600,
                        transition: 'all 0.3s ease',
                        cursor: selectedTickets.length === 0 ? 'not-allowed' : 'pointer',
                        '&:hover': selectedTickets.length > 0 ? {
                          borderRadius: '6px 12px 6px 12px',
                          boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)',
                          transform: 'translateY(-1px)'
                        } : {}
                      }
                    })}
                  >
                    Continue to Payment
                  </Button>
                </Group>
              </>
            )}

            {/* Step 2: Payment Form */}
            {currentStep === 1 && (
              <PaymentForm
                eventInfo={eventInfo}
                initialSlidingScale={discountPercentage}
                onPaymentSuccess={handlePaymentSuccess}
                onPaymentError={handlePaymentError}
              />
            )}

            {/* Step 3: Confirmation */}
            {currentStep === 2 && (completedPayment || paymentData) && (
              <PaymentConfirmation
                payment={completedPayment || paymentData || {} as any}
                eventInfo={eventInfo}
                onViewRegistrations={handleViewRegistrations}
                onRegisterMore={handleRegisterMore}
              />
            )}
          </Stack>

          {/* Sidebar - Payment Summary */}
          {currentStep < 2 && (
            <Box style={{ flex: 1, minWidth: 300 }} visibleFrom="md">
              <Box style={{ position: 'sticky', top: 20 }}>
                <PaymentSummary
                  eventInfo={eventInfo}
                  calculation={calculation}
                  selectedTickets={selectedTickets}
                  ticketPrices={ticketPrices}
                  sessions={sessions}
                  detailed={true}
                />
              </Box>
            </Box>
          )}
        </Group>

        {/* Security Notice */}
        {currentStep < 2 && (
          <Paper p="sm" radius="md" bg="gray.0">
            <Text size="xs" ta="center" c="dimmed">
              🔒 This is a secure 256-bit SSL encrypted payment. 
              Your payment information is protected and never stored on our servers.
            </Text>
          </Paper>
        )}
      </Stack>
    </Container>
  );
};