import React, { useEffect, useState } from 'react';
import {
  Box,
  Text,
  Group,
  Stack,
  Paper,
  Button,
  Divider,
  Badge
} from '@mantine/core';
import {
  IconCalendarEvent,
  IconClock,
  IconMapPin,
  IconUser,
  IconCheck,
  IconDownload,
  IconCalendarPlus
} from '@tabler/icons-react';

interface CheckoutConfirmationProps {
  eventInfo: {
    id: string;
    title: string;
    startDateTime: string;
    endDateTime: string;
    instructor?: string;
    location?: string;
    price: number;
  };
  quantity: number;
  total: number;
  paymentDetails: {
    id: string;
    method: string;
    amount: number;
    currency: string;
    cardLast4?: string;
    status: string;
    transactionId: string;
    confirmationNumber: string;
  };
  onClose: () => void;
}

// Confetti component
const Confetti: React.FC<{ style: React.CSSProperties }> = ({ style }) => (
  <Box
    style={{
      position: 'fixed',
      width: '10px',
      height: '10px',
      backgroundColor: 'var(--color-amber)',
      ...style,
      animation: 'confetti-fall 3s linear infinite',
      zIndex: 1000
    }}
  />
);

export const CheckoutConfirmation: React.FC<CheckoutConfirmationProps> = ({
  eventInfo,
  quantity,
  total,
  paymentDetails,
  onClose
}) => {
  const [showConfetti, setShowConfetti] = useState(true);

  useEffect(() => {
    // Add confetti animation styles to document
    const style = document.createElement('style');
    style.textContent = `
      @keyframes confetti-fall {
        0% {
          transform: translateY(-100vh) rotate(0deg);
          opacity: 1;
        }
        100% {
          transform: translateY(100vh) rotate(720deg);
          opacity: 0;
        }
      }
    `;
    document.head.appendChild(style);

    // Remove confetti after 5 seconds
    const timer = setTimeout(() => {
      setShowConfetti(false);
    }, 5000);

    return () => {
      clearTimeout(timer);
      document.head.removeChild(style);
    };
  }, []);

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

  const getPaymentMethodDisplay = () => {
    switch (paymentDetails.method) {
      case 'credit_card':
        return `Credit Card (****${paymentDetails.cardLast4})`;
      case 'paypal':
        return 'PayPal';
      case 'venmo':
        return 'Venmo';
      default:
        return paymentDetails.method;
    }
  };

  const confettiPositions = Array.from({ length: 20 }, (_, i) => ({
    left: `${Math.random() * 100}%`,
    animationDelay: `${Math.random() * 3}s`,
    animationDuration: `${3 + Math.random() * 2}s`,
    backgroundColor: [
      'var(--color-amber)',
      'var(--color-burgundy)',
      'var(--color-plum)',
      'var(--color-rose-gold)',
      'var(--color-copper)'
    ][Math.floor(Math.random() * 5)]
  }));

  return (
    <Box style={{ position: 'relative' }}>
      {/* Confetti Effect */}
      {showConfetti &&
        confettiPositions.map((pos, i) => (
          <Confetti
            key={i}
            style={{
              left: pos.left,
              animationDelay: pos.animationDelay,
              animationDuration: pos.animationDuration,
              backgroundColor: pos.backgroundColor
            }}
          />
        ))}

      <Stack gap="xl" style={{ textAlign: 'center' }}>
        {/* Success Icon */}
        <Box
          style={{
            width: '140px',
            height: '140px',
            background: 'linear-gradient(135deg, var(--color-success) 0%, #2E7D32 100%)',
            borderRadius: '50%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto',
            fontSize: '70px',
            color: 'var(--color-ivory)',
            boxShadow: '0 8px 32px rgba(34, 139, 34, 0.3)',
            animation: 'success-pulse 0.6s ease-out'
          }}
        >
          ✓
        </Box>

        {/* Success Title */}
        <Text
          style={{
            fontFamily: 'var(--font-display)',
            fontSize: '42px',
            fontWeight: 700,
            color: 'var(--color-burgundy)',
            animation: 'success-pulse 0.6s ease-out 0.1s backwards'
          }}
        >
          You're All Set!
        </Text>

        {/* Success Message */}
        <Text
          style={{
            fontSize: '20px',
            color: 'var(--color-stone)',
            lineHeight: 1.8,
            animation: 'success-pulse 0.6s ease-out 0.2s backwards'
          }}
        >
          Your registration for <strong>{eventInfo.title}</strong> is confirmed.<br />
          We can't wait to see you there!
        </Text>

        {/* Email Notice */}
        <Box
          style={{
            background: 'var(--color-info-light)',
            color: 'var(--color-info)',
            padding: 'var(--space-md)',
            borderRadius: '12px',
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-sm)',
            fontSize: '16px',
            animation: 'success-pulse 0.6s ease-out 0.3s backwards'
          }}
        >
          <Text style={{ fontSize: '24px' }}>📧</Text>
          <Text>A confirmation email has been sent to your registered email address</Text>
        </Box>

        {/* Order Details Card */}
        <Paper
          style={{
            background: 'var(--color-ivory)',
            borderRadius: '16px',
            padding: 'var(--space-2xl)',
            boxShadow: '0 8px 24px rgba(0,0,0,0.08)',
            border: '1px solid var(--color-taupe)',
            textAlign: 'left',
            animation: 'success-pulse 0.6s ease-out 0.4s backwards'
          }}
        >
          {/* Order Header */}
          <Group justify="space-between" align="center" mb="lg" style={{ borderBottom: '2px solid var(--color-taupe)', paddingBottom: 'var(--space-lg)' }}>
            <Text
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '16px',
                color: 'var(--color-stone)',
                fontWeight: 600
              }}
            >
              Confirmation Number:{' '}
              <Text
                component="span"
                style={{
                  color: 'var(--color-burgundy)',
                  fontWeight: 700
                }}
              >
                {paymentDetails.confirmationNumber}
              </Text>
            </Text>

            <Button
              variant="outline"
              size="sm"
              leftSection={<IconDownload size={16} />}
              onClick={() => window.print()}
              style={{
                borderColor: 'var(--color-burgundy)',
                color: 'var(--color-burgundy)'
              }}
            >
              Print Receipt
            </Button>
          </Group>

          {/* Event Information */}
          <Box mb="lg">
            <Text
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '24px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginBottom: 'var(--space-sm)'
              }}
            >
              {eventInfo.title}
            </Text>

            <Stack gap="xs">
              <Group gap="sm">
                <IconCalendarEvent size={16} color="var(--color-stone)" />
                <Text size="sm" c="var(--color-stone)">
                  {startDate.date}
                </Text>
              </Group>

              <Group gap="sm">
                <IconClock size={16} color="var(--color-stone)" />
                <Text size="sm" c="var(--color-stone)">
                  {startDate.time} - {endDate.time}
                </Text>
              </Group>

              {eventInfo.instructor && (
                <Group gap="sm">
                  <IconUser size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    Instructor: {eventInfo.instructor}
                  </Text>
                </Group>
              )}

              {eventInfo.location && (
                <Group gap="sm">
                  <IconMapPin size={16} color="var(--color-stone)" />
                  <Text size="sm" c="var(--color-stone)">
                    {eventInfo.location}
                  </Text>
                </Group>
              )}
            </Stack>
          </Box>

          {/* Ticket Information */}
          <Paper
            style={{
              background: 'var(--color-cream)',
              padding: 'var(--space-md)',
              borderRadius: '12px',
              marginBottom: 'var(--space-lg)'
            }}
          >
            <Group>
              <Group gap="lg" style={{ flex: 1 }}>
                <Box>
                  <Text
                    style={{
                      fontSize: '14px',
                      color: 'var(--color-stone)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      fontWeight: 600,
                      marginBottom: '4px'
                    }}
                  >
                    Ticket Type
                  </Text>
                  <Text
                    style={{
                      fontWeight: 600,
                      color: 'var(--color-charcoal)',
                      fontSize: '16px'
                    }}
                  >
                    Single Ticket × {quantity}
                  </Text>
                </Box>

                <Box>
                  <Text
                    style={{
                      fontSize: '14px',
                      color: 'var(--color-stone)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      fontWeight: 600,
                      marginBottom: '4px'
                    }}
                  >
                    Amount Paid
                  </Text>
                  <Text
                    style={{
                      fontWeight: 600,
                      color: 'var(--color-charcoal)',
                      fontSize: '16px'
                    }}
                  >
                    ${total.toFixed(2)}
                  </Text>
                </Box>

                <Box>
                  <Text
                    style={{
                      fontSize: '14px',
                      color: 'var(--color-stone)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      fontWeight: 600,
                      marginBottom: '4px'
                    }}
                  >
                    Payment Method
                  </Text>
                  <Text
                    style={{
                      fontWeight: 600,
                      color: 'var(--color-charcoal)',
                      fontSize: '16px'
                    }}
                  >
                    {getPaymentMethodDisplay()}
                  </Text>
                </Box>

                <Box>
                  <Text
                    style={{
                      fontSize: '14px',
                      color: 'var(--color-stone)',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      fontWeight: 600,
                      marginBottom: '4px'
                    }}
                  >
                    Transaction ID
                  </Text>
                  <Text
                    style={{
                      fontWeight: 600,
                      color: 'var(--color-charcoal)',
                      fontSize: '16px'
                    }}
                  >
                    {paymentDetails.transactionId}
                  </Text>
                </Box>
              </Group>
            </Group>
          </Paper>

          {/* Important Notes */}
          <Box
            style={{
              background: 'linear-gradient(135deg, rgba(136, 1, 36, 0.05) 0%, rgba(97, 75, 121, 0.05) 100%)',
              border: '2px solid var(--color-taupe)',
              borderRadius: '12px',
              padding: 'var(--space-lg)'
            }}
          >
            <Text
              style={{
                fontFamily: 'var(--font-heading)',
                fontSize: '18px',
                fontWeight: 700,
                color: 'var(--color-burgundy)',
                marginBottom: 'var(--space-md)'
              }}
            >
              What's Next?
            </Text>

            <Stack gap="sm">
              <Group gap="sm">
                <IconCheck size={16} color="var(--color-success)" />
                <Text size="sm" c="var(--color-stone)">
                  Check your email for detailed event information and what to bring
                </Text>
              </Group>
              <Group gap="sm">
                <IconCheck size={16} color="var(--color-success)" />
                <Text size="sm" c="var(--color-stone)">
                  Add this event to your calendar so you don't forget
                </Text>
              </Group>
              <Group gap="sm">
                <IconCheck size={16} color="var(--color-success)" />
                <Text size="sm" c="var(--color-stone)">
                  Review our safety guidelines and consent policies before attending
                </Text>
              </Group>
              <Group gap="sm">
                <IconCheck size={16} color="var(--color-success)" />
                <Text size="sm" c="var(--color-stone)">
                  Arrive 15 minutes early for check-in and orientation
                </Text>
              </Group>
              <Group gap="sm">
                <IconCheck size={16} color="var(--color-success)" />
                <Text size="sm" c="var(--color-stone)">
                  Bring your ID for verification at check-in
                </Text>
              </Group>
            </Stack>
          </Box>
        </Paper>

        {/* Action Buttons */}
        <Group justify="center" gap="md" style={{ animation: 'success-pulse 0.6s ease-out 0.5s backwards' }}>
          <Button
            component="a"
            href="/dashboard"
            leftSection={<IconCalendarEvent size={16} />}
            className="btn btn-primary"
          >
            View My Tickets
          </Button>

          <Button
            component="a"
            href="/events"
            variant="outline"
            leftSection={<IconCalendarPlus size={16} />}
            style={{
              borderColor: 'var(--color-burgundy)',
              color: 'var(--color-burgundy)'
            }}
          >
            Browse More Events
          </Button>
        </Group>

        {/* Share Section */}
        <Box style={{ paddingTop: 'var(--space-xl)', borderTop: '1px solid var(--color-taupe)', animation: 'success-pulse 0.6s ease-out 0.6s backwards' }}>
          <Text
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '18px',
              fontWeight: 600,
              color: 'var(--color-charcoal)',
              marginBottom: 'var(--space-md)'
            }}
          >
            Share Your Excitement!
          </Text>

          <Group justify="center" gap="sm">
            {[
              { icon: 'f', label: 'Facebook', color: '#1877F2' },
              { icon: '𝕏', label: 'Twitter', color: '#000000' },
              { icon: '📷', label: 'Instagram', color: '#E4405F' },
              { icon: '@', label: 'Email', color: '#34495E' }
            ].map((social) => (
              <Box
                key={social.label}
                component="button"
                style={{
                  width: '48px',
                  height: '48px',
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  textDecoration: 'none',
                  fontSize: '24px',
                  transition: 'all 0.3s ease',
                  background: 'var(--color-ivory)',
                  border: '2px solid var(--color-taupe)',
                  color: 'var(--color-burgundy)',
                  cursor: 'pointer'
                }}
                onClick={() => {
                  // Handle social sharing
                  console.log(`Share to ${social.label}`);
                }}
              >
                {social.icon}
              </Box>
            ))}
          </Group>
        </Box>
      </Stack>

      {/* Add success animation styles */}
      <style>
        {`
          @keyframes success-pulse {
            0% {
              transform: scale(0.8);
              opacity: 0;
            }
            50% {
              transform: scale(1.1);
            }
            100% {
              transform: scale(1);
              opacity: 1;
            }
          }
        `}
      </style>
    </Box>
  );
};