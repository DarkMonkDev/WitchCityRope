import React, { useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import {
  Container,
  Box,
  Text,
  Group,
  Stack,
  Paper,
  Badge,
  Stepper,
  Button,
  Divider,
  Grid,
  ActionIcon
} from '@mantine/core';
import { IconCalendarEvent, IconClock, IconMapPin, IconUser, IconArrowLeft } from '@tabler/icons-react';
import { CheckoutForm } from '../../components/checkout/CheckoutForm';
import { CheckoutConfirmation } from '../../components/checkout/CheckoutConfirmation';

interface CheckoutPageProps {}

type CheckoutStep = 'review' | 'payment' | 'confirmation';

interface EventInfo {
  id: string;
  title: string;
  startDateTime: string;
  endDateTime: string;
  instructor?: string;
  location?: string;
  price: number;
}

export const CheckoutPage: React.FC<CheckoutPageProps> = () => {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const location = useLocation();

  // Get event info from navigation state or use defaults
  const eventInfo: EventInfo = location.state?.eventInfo || {
    id: eventId || '',
    title: 'Event Registration',
    startDateTime: new Date().toISOString(),
    endDateTime: new Date().toISOString(),
    price: 50
  };

  const [currentStep, setCurrentStep] = useState<CheckoutStep>('review');
  const [quantity, setQuantity] = useState(1);
  const [paymentMethod, setPaymentMethod] = useState<'card' | 'paypal' | 'venmo'>('card');
  const [paymentDetails, setPaymentDetails] = useState<any>(null);

  const formatDateTime = (dateTime: string) => {
    const date = new Date(dateTime);
    return {
      date: date.toLocaleDateString('en-US', {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      }),
      time: date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      })
    };
  };

  const startDate = formatDateTime(eventInfo.startDateTime);
  const endDate = formatDateTime(eventInfo.endDateTime);
  const subtotal = eventInfo.price * quantity;
  const processingFee = 2.50;
  const total = subtotal + processingFee;

  const getStepNumber = (step: CheckoutStep): number => {
    switch (step) {
      case 'review': return 0;
      case 'payment': return 1;
      case 'confirmation': return 2;
      default: return 0;
    }
  };

  const handleReviewComplete = () => {
    setCurrentStep('payment');
  };

  const handlePaymentSuccess = (details: any) => {
    setPaymentDetails(details);
    setCurrentStep('confirmation');
  };

  const handleBack = () => {
    if (currentStep === 'payment') {
      setCurrentStep('review');
    } else {
      // Navigate back to the event or previous page
      navigate(-1);
    }
  };

  const handleComplete = () => {
    // Navigate to success page or back to event
    navigate(`/events/${eventId}`, {
      state: {
        message: 'Registration completed successfully!'
      }
    });
  };

  return (
    <Container size="xl" py="xl">
      {/* Header with Back Navigation */}
      <Box mb="xl">
        <Group gap="md" mb="lg">
          <ActionIcon
            variant="outline"
            size="lg"
            onClick={handleBack}
            style={{
              borderColor: 'var(--color-burgundy)',
              color: 'var(--color-burgundy)'
            }}
          >
            <IconArrowLeft size={20} />
          </ActionIcon>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '32px',
              fontWeight: 700,
              color: 'var(--color-burgundy)'
            }}
          >
            {currentStep === 'confirmation'
              ? 'Registration Complete!'
              : 'Complete Your Registration'
            }
          </Text>
        </Group>

        {/* Progress Stepper */}
        {currentStep !== 'confirmation' && (
          <Box mb="xl">
            <Stepper
              active={getStepNumber(currentStep)}
              size="sm"
              styles={{
                step: {
                  '&[data-completed]': {
                    backgroundColor: 'var(--color-success)',
                    borderColor: 'var(--color-success)'
                  },
                  '&[data-progress]': {
                    background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
                    borderColor: 'var(--color-burgundy)'
                  }
                },
                stepLabel: {
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 600,
                  fontSize: '14px'
                }
              }}
            >
              <Stepper.Step label="Review Event" />
              <Stepper.Step label="Payment" />
              <Stepper.Step label="Confirmation" />
            </Stepper>
          </Box>
        )}
      </Box>

      {/* Main Content Layout */}
      <Grid gutter="xl">
        {/* Left Column - Event Details (Persistent) */}
        {currentStep !== 'confirmation' && (
          <Grid.Col span={{ base: 12, md: 5 }}>
            <Paper
              style={{
                background: 'var(--color-cream)',
                borderRadius: '12px',
                padding: 'var(--space-lg)',
                border: '1px solid var(--color-taupe)',
                position: 'sticky',
                top: '20px'
              }}
            >
              <Text
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '24px',
                  fontWeight: 700,
                  color: 'var(--color-burgundy)',
                  marginBottom: 'var(--space-md)'
                }}
              >
                {eventInfo.title}
              </Text>

              <Group gap="lg" mb="md">
                <Group gap="xs">
                  <IconCalendarEvent size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    {startDate.date}
                  </Text>
                </Group>

                <Group gap="xs">
                  <IconClock size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    {startDate.time} - {endDate.time}
                  </Text>
                </Group>
              </Group>

              {eventInfo.instructor && (
                <Group gap="xs" mb="xs">
                  <IconUser size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    Instructor: {eventInfo.instructor}
                  </Text>
                </Group>
              )}

              {eventInfo.location && (
                <Group gap="xs" mb="md">
                  <IconMapPin size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    {eventInfo.location}
                  </Text>
                </Group>
              )}

              <Divider mb="md" color="var(--color-taupe)" />

              <Group justify="space-between" align="center" mb="md">
                <Group align="center" gap="sm">
                  <Text
                    style={{
                      fontWeight: 600,
                      color: 'var(--color-stone)'
                    }}
                  >
                    Purchasing
                  </Text>
                  <input
                    type="number"
                    value={quantity}
                    onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value) || 1))}
                    min="1"
                    max="10"
                    style={{
                      width: '60px',
                      padding: '8px',
                      border: '2px solid var(--color-taupe)',
                      borderRadius: '8px',
                      fontSize: '16px',
                      fontWeight: 600,
                      textAlign: 'center',
                      backgroundColor: 'var(--color-ivory)',
                      color: 'var(--color-charcoal)'
                    }}
                    disabled={currentStep === 'payment'}
                  />
                  <Text style={{ fontSize: '16px', color: 'var(--color-stone)', fontWeight: 600 }}>
                    ×
                  </Text>
                  <Text
                    style={{
                      fontSize: '16px',
                      fontWeight: 600,
                      color: 'var(--color-charcoal)'
                    }}
                  >
                    Single Person Ticket
                  </Text>
                </Group>
              </Group>

              {/* Order Summary */}
              <Box
                style={{
                  background: 'var(--color-ivory)',
                  borderRadius: '8px',
                  padding: 'var(--space-md)',
                  border: '1px solid var(--color-taupe)'
                }}
              >
                <Group justify="space-between" mb="sm">
                  <Text size="sm" c="var(--color-stone)">
                    Subtotal ({quantity} ticket{quantity > 1 ? 's' : ''})
                  </Text>
                  <Text size="sm" fw={600} c="var(--color-charcoal)">
                    ${subtotal.toFixed(2)}
                  </Text>
                </Group>

                <Group justify="space-between" mb="sm">
                  <Text size="sm" c="var(--color-stone)">
                    Processing Fee
                  </Text>
                  <Text size="sm" fw={600} c="var(--color-charcoal)">
                    ${processingFee.toFixed(2)}
                  </Text>
                </Group>

                <Divider my="sm" color="var(--color-taupe)" />

                <Group justify="space-between">
                  <Text fw={700} size="lg" c="var(--color-stone)">
                    Total
                  </Text>
                  <Text
                    style={{
                      fontFamily: 'var(--font-display)',
                      fontSize: '24px',
                      fontWeight: 700,
                      color: 'var(--color-burgundy)'
                    }}
                  >
                    ${total.toFixed(2)}
                  </Text>
                </Group>
              </Box>
            </Paper>
          </Grid.Col>
        )}

        {/* Right Column - Step Content */}
        <Grid.Col span={{ base: 12, md: currentStep === 'confirmation' ? 12 : 7 }}>
          {currentStep === 'review' && (
            <Paper
              style={{
                background: 'var(--color-ivory)',
                borderRadius: '12px',
                padding: 'var(--space-xl)',
                textAlign: 'center',
                border: '2px solid var(--color-taupe)'
              }}
            >
              <Text
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '24px',
                  fontWeight: 700,
                  color: 'var(--color-burgundy)',
                  marginBottom: 'var(--space-lg)'
                }}
              >
                Review Your Order
              </Text>

              <Text size="lg" c="var(--color-stone)" mb="xl">
                Please review your event details and ticket quantity before proceeding to payment.
              </Text>

              <Button
                onClick={handleReviewComplete}
                className="btn btn-primary"
                size="lg"
                style={{
                  width: '100%',
                  maxWidth: '400px'
                }}
              >
                Continue to Payment
              </Button>
            </Paper>
          )}

          {currentStep === 'payment' && (
            <Box>
              <CheckoutForm
                eventInfo={eventInfo}
                quantity={quantity}
                total={total}
                paymentMethod={paymentMethod}
                onPaymentMethodChange={setPaymentMethod}
                onPaymentSuccess={handlePaymentSuccess}
                onBack={() => setCurrentStep('review')}
              />
            </Box>
          )}

          {currentStep === 'confirmation' && paymentDetails && (
            <Box>
              <CheckoutConfirmation
                eventInfo={eventInfo}
                quantity={quantity}
                total={total}
                paymentDetails={paymentDetails}
                onClose={handleComplete}
              />
            </Box>
          )}
        </Grid.Col>
      </Grid>

      {/* Footer */}
      <Box mt="xl" pt="xl" style={{ borderTop: '1px solid var(--color-taupe)' }}>
        <Text size="xs" c="dimmed" ta="center">
          By completing this purchase, you agree to our{' '}
          <Text component="a" href="#" c="var(--color-burgundy)" td="underline">
            Terms of Service
          </Text>
          {' '}and{' '}
          <Text component="a" href="#" c="var(--color-burgundy)" td="underline">
            Refund Policy
          </Text>
        </Text>
      </Box>
    </Container>
  );
};