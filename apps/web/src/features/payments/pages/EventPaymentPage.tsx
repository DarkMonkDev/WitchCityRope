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
  Select
} from '@mantine/core';
import { IconArrowLeft, IconAlertCircle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

import { SlidingScaleSelector } from '../components/SlidingScaleSelector';
import { PaymentForm } from '../components/PaymentForm';
import { PaymentConfirmation } from '../components/PaymentConfirmation';
import { PaymentSummary } from '../components/PaymentSummary';
import { usePayment } from '../hooks/usePayment';
import { useSlidingScale } from '../hooks/useSlidingScale';
import { eventsManagementService } from '../../../api/services/eventsManagement.service';

import type { PaymentEventInfo } from '../types/payment.types';

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
  const [ticketTypes, setTicketTypes] = useState<any[]>([]);
  const [selectedTicketTypeId, setSelectedTicketTypeId] = useState<string | null>(null);

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

        console.log('EventPaymentPage: Loading event details for eventId:', eventId);

        // Fetch real event data from API
        const response = await eventsManagementService.getEventDetails(eventId);
        const eventDetails = response.data;

        console.log('EventPaymentPage: Received event details:', eventDetails);

        // Extract ticket types
        const eventTicketTypes = eventDetails.ticketTypes || [];
        setTicketTypes(eventTicketTypes);

        // Set initial selected ticket type (first one or from URL param)
        const initialTicketTypeId = searchParams.get('ticketTypeId') || eventTicketTypes[0]?.id || null;
        setSelectedTicketTypeId(initialTicketTypeId);

        // Find selected ticket type for pricing
        const selectedTicket = eventTicketTypes.find((tt: any) => tt.id === initialTicketTypeId) || eventTicketTypes[0];
        // Use correct price field based on pricing type
        const basePrice = selectedTicket?.pricingType === 'fixed'
          ? (selectedTicket?.price ?? 0)
          : (selectedTicket?.minPrice ?? 0);

        // Transform to PaymentEventInfo format
        const paymentEventInfo: PaymentEventInfo = {
          id: eventDetails.id,
          title: eventDetails.title,
          startDateTime: eventDetails.startDate || new Date().toISOString(),
          endDateTime: eventDetails.endDate || new Date().toISOString(),
          instructorName: 'Instructor TBD', // teacherIds is currently empty in API response
          location: eventDetails.location || 'TBD',
          basePrice,
          currency: 'USD',
          registrationId: registrationId
        };

        console.log('EventPaymentPage: Transformed payment event info:', paymentEventInfo);
        console.log('EventPaymentPage: Ticket types:', eventTicketTypes);
        console.log('EventPaymentPage: Selected ticket type ID:', initialTicketTypeId);
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
  const handlePaymentSuccess = (paymentDetails: any) => {
    console.log('Payment success received:', paymentDetails);

    // Create payment data object for confirmation page
    const paymentData = {
      id: typeof paymentDetails === 'string' ? paymentDetails : paymentDetails.id,
      transactionId: typeof paymentDetails === 'string' ? paymentDetails : paymentDetails.transactionId || paymentDetails.id,
      amount: finalAmount,
      currency: 'USD',
      status: 'completed',
      paymentMethod: typeof paymentDetails === 'string' ? 'credit_card' : paymentDetails.method || 'credit_card',
      createdAt: new Date().toISOString(),
      eventRegistrationId: registrationId || '',
      confirmationNumber: `WCR-${new Date().getFullYear()}-${String(Date.now()).slice(-6)}`,
      cardLast4: typeof paymentDetails === 'object' && paymentDetails.cardLast4 ? paymentDetails.cardLast4 : undefined
    };

    // Store the payment data
    setCompletedPayment(paymentData);

    // Move to confirmation step
    setCurrentStep(2);

    notifications.show({
      title: 'Payment Successful!',
      message: 'Your registration has been confirmed.',
      color: 'green'
    });
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
    navigate('/profile/registrations');
  };

  /**
   * Navigate to browse more events
   */
  const handleRegisterMore = () => {
    navigate('/events');
  };

  /**
   * Determine if selected ticket is fixed price (not sliding scale)
   */
  const selectedTicket = ticketTypes.find((tt: any) => tt.id === selectedTicketTypeId);

  // DEBUG: Log the ticket type to see what value it actually has
  console.log('🔍 CHECKOUT DEBUG:');
  console.log('  - Selected ticket:', selectedTicket);
  console.log('  - selectedTicket?.pricingType:', selectedTicket?.pricingType);
  console.log('  - All ticket types:', ticketTypes);

  const isFixedPrice = selectedTicket?.pricingType === 'fixed';
  console.log('  - isFixedPrice:', isFixedPrice);

  /**
   * Handle ticket type selection change
   */
  const handleTicketTypeChange = (ticketTypeId: string | null) => {
    if (!ticketTypeId) return;

    setSelectedTicketTypeId(ticketTypeId);

    // Update event info with new base price from selected ticket type
    const selectedTicket = ticketTypes.find((tt: any) => tt.id === ticketTypeId);
    if (selectedTicket && eventInfo) {
      // Use correct price field based on pricing type
      const newBasePrice = selectedTicket.pricingType === 'fixed'
        ? (selectedTicket.price ?? 0)
        : (selectedTicket.minPrice ?? 0);
      setEventInfo({
        ...eventInfo,
        basePrice: newBasePrice
      });
      updateDiscountPercentage(0); // Reset discount when ticket type changes
    }
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
      <Stack gap="xl">
        {/* Header */}
        <Group justify="space-between" align="center">
          <Button
            variant="subtle"
            leftSection={<IconArrowLeft size={16} />}
            onClick={handleBack}
            color="wcr"
          >
            Back
          </Button>
          <Text c="dimmed" size="sm">
            Secure Payment • SSL Encrypted
          </Text>
        </Group>

        {/* Progress Stepper */}
        <Stepper
          active={currentStep}
          color="#880124"
          iconSize={32}
          styles={{
            stepIcon: {
              borderWidth: 2
            }
          }}
        >
          <Stepper.Step
            label={isFixedPrice ? "Ticket Selection" : "Pricing"}
            description={isFixedPrice ? "Review ticket details" : "Choose your amount"}
          />
          <Stepper.Step
            label="Payment"
            description="Enter payment details"
          />
          <Stepper.Step
            label="Confirmation"
            description="Registration complete"
          />
        </Stepper>

        {/* Step Content */}
        <Group align="flex-start" gap="xl">
          {/* Main Content */}
          <Stack gap="lg" style={{ flex: 2 }}>
            {/* Step 1: Ticket Type and Pricing Selection */}
            {currentStep === 0 && (
              <>
                {/* Ticket Type Selection */}
                {ticketTypes.length > 1 && (
                  <Paper p="lg" radius="md" mb="lg" style={{ background: 'var(--mantine-color-gray-0)' }}>
                    <Stack gap="sm">
                      <Text fw={600} size="lg">Select Ticket Type</Text>
                      <Select
                        label="Ticket Type"
                        placeholder="Choose a ticket type"
                        value={selectedTicketTypeId}
                        onChange={handleTicketTypeChange}
                        data={ticketTypes.map((tt: any) => {
                          // Format price display based on pricing type
                          let priceDisplay = '';
                          if (tt.pricingType === 'fixed') {
                            // Fixed price: show the price field
                            const price = tt.price ?? 0;
                            priceDisplay = `$${price.toFixed(2)}`;
                          } else if (tt.pricingType === 'sliding-scale') {
                            // Sliding scale: show range
                            const minPrice = tt.minPrice ?? 0;
                            const maxPrice = tt.maxPrice ?? 0;
                            priceDisplay = `$${minPrice.toFixed(2)} - $${maxPrice.toFixed(2)}`;
                          } else {
                            // Fallback for unknown pricing type
                            priceDisplay = 'Price TBD';
                          }

                          return {
                            value: tt.id,
                            label: `${tt.name} - ${priceDisplay}`
                          };
                        })}
                        required
                        styles={{
                          input: {
                            borderColor: 'var(--mantine-color-wcr-6)',
                            '&:focus': {
                              borderColor: 'var(--mantine-color-wcr-7)',
                            }
                          }
                        }}
                      />
                      {/* Show selected ticket details */}
                      {selectedTicketTypeId && ticketTypes.find((tt: any) => tt.id === selectedTicketTypeId)?.sessionIdentifiers?.length > 0 && (
                        <Text size="sm" c="dimmed">
                          Includes sessions: {ticketTypes.find((tt: any) => tt.id === selectedTicketTypeId).sessionIdentifiers.join(', ')}
                        </Text>
                      )}
                    </Stack>
                  </Paper>
                )}

                {/* Pricing: Fixed Price or Sliding Scale */}
                {isFixedPrice ? (
                  // Fixed Price Display
                  <Paper p="xl" radius="md" style={{
                    background: 'linear-gradient(135deg, var(--mantine-color-gray-0), var(--mantine-color-gray-1))',
                    border: '2px solid var(--mantine-color-wcr-6)'
                  }}>
                    <Stack gap="md" align="center">
                      <Text size="xl" fw={600} c="dimmed">Ticket Price</Text>
                      <Text size="48px" fw={700} c="#880124" style={{ lineHeight: 1 }}>
                        ${eventInfo.basePrice.toFixed(2)}
                      </Text>
                      <Text size="sm" c="dimmed" ta="center">
                        This is a fixed-price ticket
                      </Text>
                    </Stack>
                  </Paper>
                ) : (
                  // Sliding Scale Selector
                  <SlidingScaleSelector
                    basePrice={eventInfo.basePrice}
                    currency={eventInfo.currency}
                    onAmountChange={(amount, percentage) => {
                      updateDiscountPercentage(percentage);
                    }}
                    title="Choose Your Payment Amount"
                    forceSliding={true}
                    minPrice={selectedTicket?.minPrice}
                    maxPrice={selectedTicket?.maxPrice}
                    defaultPrice={selectedTicket?.defaultPrice}
                  />
                )}

                <Group justify="flex-end">
                  <Button
                    onClick={handleContinue}
                    size="lg"
                    color="#880124"
                    styles={(theme) => ({
                      root: {
                        background: 'linear-gradient(135deg, #FFB800, #DAA520)',
                        border: 'none',
                        borderRadius: '12px 6px 12px 6px',
                        color: '#2C2C2C',
                        fontFamily: 'Montserrat, sans-serif',
                        fontWeight: 600,
                        transition: 'all 0.3s ease',
                        '&:hover': {
                          borderRadius: '6px 12px 6px 12px',
                          boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)',
                          transform: 'translateY(-1px)'
                        }
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
            <Box style={{ flex: 1, minWidth: 300 }}>
              <Box style={{ position: 'sticky', top: 20 }}>
                <PaymentSummary
                  eventInfo={eventInfo}
                  calculation={calculation}
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