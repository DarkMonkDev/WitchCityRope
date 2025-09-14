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
  Box
} from '@mantine/core';
import { IconArrowLeft, IconAlertCircle } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';

import { SlidingScaleSelector } from '../components/SlidingScaleSelector';
import { PaymentForm } from '../components/PaymentForm';
import { PaymentConfirmation } from '../components/PaymentConfirmation';
import { PaymentSummary } from '../components/PaymentSummary';
import { usePayment } from '../hooks/usePayment';
import { useSlidingScale } from '../hooks/useSlidingScale';

import type { PaymentEventInfo } from '../types/payment.types';

/**
 * Main event payment page with complete payment flow
 */
export const EventPaymentPage: React.FC = () => {
  const { eventId, registrationId } = useParams<{
    eventId: string;
    registrationId: string;
  }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  // Payment flow state
  const [currentStep, setCurrentStep] = useState(0);
  const [eventInfo, setEventInfo] = useState<PaymentEventInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
   * Load event information from URL params or API
   */
  useEffect(() => {
    const loadEventInfo = async () => {
      try {
        setIsLoading(true);
        
        // In a real implementation, you'd fetch from API
        // For now, we'll use search params or mock data
        const mockEventInfo: PaymentEventInfo = {
          id: eventId || '',
          title: searchParams.get('title') || 'Shibari Fundamentals Workshop',
          startDateTime: searchParams.get('startDateTime') || '2025-10-15T14:00:00',
          endDateTime: searchParams.get('endDateTime') || '2025-10-15T18:00:00',
          instructorName: searchParams.get('instructor') || 'Master Kenji',
          location: searchParams.get('location') || 'Salem Community Center',
          basePrice: parseFloat(searchParams.get('price') || '40'),
          currency: 'USD',
          registrationId: registrationId || ''
        };

        setEventInfo(mockEventInfo);
      } catch (err) {
        setError('Failed to load event information');
        console.error('Error loading event info:', err);
      } finally {
        setIsLoading(false);
      }
    };

    if (eventId && registrationId) {
      loadEventInfo();
    } else {
      setError('Missing event or registration information');
      setIsLoading(false);
    }
  }, [eventId, registrationId, searchParams]);

  /**
   * Handle successful payment
   */
  const handlePaymentSuccess = (paymentId: string) => {
    setCurrentStep(2); // Move to confirmation step
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
          icon={<IconAlertCircle size={16} />}
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
            label="Pricing" 
            description="Choose your amount"
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
            {/* Step 1: Sliding Scale Selection */}
            {currentStep === 0 && (
              <>
                <SlidingScaleSelector
                  basePrice={eventInfo.basePrice}
                  currency={eventInfo.currency}
                  onAmountChange={(amount, percentage) => {
                    updateDiscountPercentage(percentage);
                  }}
                  title="Choose Your Payment Amount"
                />

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
            {currentStep === 2 && paymentData && (
              <PaymentConfirmation
                payment={paymentData || {} as any}
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