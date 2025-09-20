import React, { useState } from 'react';
import {
  Modal,
  Box,
  Text,
  Group,
  Stack,
  Paper,
  Badge,
  Stepper,
  Button,
  Divider
} from '@mantine/core';
import { IconCalendarEvent, IconClock, IconMapPin, IconUser } from '@tabler/icons-react';
import { CheckoutForm } from './CheckoutForm';
import { CheckoutConfirmation } from './CheckoutConfirmation';

interface CheckoutModalProps {
  opened: boolean;
  onClose: () => void;
  eventInfo: {
    id: string;
    title: string;
    startDateTime: string;
    endDateTime: string;
    instructor?: string;
    location?: string;
    price: number;
  };
  onSuccess: (paymentDetails: any) => void;
}

type CheckoutStep = 'review' | 'payment' | 'confirmation';

export const CheckoutModal: React.FC<CheckoutModalProps> = ({
  opened,
  onClose,
  eventInfo,
  onSuccess
}) => {
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
    onSuccess(details);
  };

  const handleClose = () => {
    // Reset state when closing
    setCurrentStep('review');
    setQuantity(1);
    setPaymentMethod('card');
    setPaymentDetails(null);
    onClose();
  };

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      size="lg"
      centered
      withCloseButton={currentStep !== 'confirmation'}
      closeOnClickOutside={currentStep !== 'confirmation'}
      closeOnEscape={currentStep !== 'confirmation'}
      styles={{
        content: {
          backgroundColor: 'var(--color-ivory)',
          borderRadius: '16px',
          border: '1px solid var(--color-taupe)'
        },
        header: {
          backgroundColor: 'var(--color-ivory)',
          borderBottom: '2px solid var(--color-taupe)',
          paddingBottom: 'var(--space-md)'
        },
        title: {
          fontFamily: 'var(--font-heading)',
          fontSize: '24px',
          fontWeight: 700,
          color: 'var(--color-burgundy)'
        }
      }}
      title={
        currentStep === 'confirmation'
          ? 'Registration Complete!'
          : 'Complete Your Registration'
      }
    >
      <Box style={{ padding: 'var(--space-md)' }}>
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

        {/* Event Information Card */}
        {currentStep !== 'confirmation' && (
          <Paper
            style={{
              background: 'var(--color-cream)',
              borderRadius: '12px',
              padding: 'var(--space-md)',
              marginBottom: 'var(--space-lg)',
              border: '1px solid var(--color-taupe)'
            }}
            mb="lg"
          >
            <Text
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '20px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginBottom: 'var(--space-sm)'
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

            <Group justify="space-between" align="center">
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
          </Paper>
        )}

        {/* Step Content */}
        {currentStep === 'review' && (
          <Stack gap="lg">
            <Paper
              style={{
                background: 'var(--color-ivory)',
                borderRadius: '12px',
                padding: 'var(--space-lg)',
                textAlign: 'center',
                border: '2px solid var(--color-taupe)'
              }}
            >
              <Group justify="space-between" mb="md">
                <Text style={{ fontSize: '18px', color: 'var(--color-stone)' }}>
                  Subtotal ({quantity} ticket{quantity > 1 ? 's' : ''})
                </Text>
                <Text style={{ fontSize: '18px', fontWeight: 600, color: 'var(--color-charcoal)' }}>
                  ${subtotal.toFixed(2)}
                </Text>
              </Group>

              <Group justify="space-between" mb="md">
                <Text style={{ fontSize: '18px', color: 'var(--color-stone)' }}>
                  Processing Fee
                </Text>
                <Text style={{ fontSize: '18px', fontWeight: 600, color: 'var(--color-charcoal)' }}>
                  ${processingFee.toFixed(2)}
                </Text>
              </Group>

              <Divider mb="md" color="var(--color-taupe)" />

              <Group justify="space-between" mb="lg">
                <Text style={{ fontSize: '24px', color: 'var(--color-stone)' }}>
                  Total Amount
                </Text>
                <Text
                  style={{
                    fontFamily: 'var(--font-display)',
                    fontSize: '36px',
                    fontWeight: 700,
                    color: 'var(--color-burgundy)'
                  }}
                >
                  ${total.toFixed(2)}
                </Text>
              </Group>

              <Button
                onClick={handleReviewComplete}
                className="btn btn-primary"
                style={{ width: '100%' }}
              >
                Continue to Payment
              </Button>
            </Paper>
          </Stack>
        )}

        {currentStep === 'payment' && (
          <CheckoutForm
            eventInfo={eventInfo}
            quantity={quantity}
            total={total}
            paymentMethod={paymentMethod}
            onPaymentMethodChange={setPaymentMethod}
            onPaymentSuccess={handlePaymentSuccess}
            onBack={() => setCurrentStep('review')}
          />
        )}

        {currentStep === 'confirmation' && paymentDetails && (
          <CheckoutConfirmation
            eventInfo={eventInfo}
            quantity={quantity}
            total={total}
            paymentDetails={paymentDetails}
            onClose={handleClose}
          />
        )}
      </Box>
    </Modal>
  );
};