// QRPaymentModal - Display QR code for digital payment with real-time notification
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md

import React, { useState, useEffect } from 'react';
import { Modal, Text, Stack, Button, Progress, ThemeIcon } from '@mantine/core';
import { IconCheck } from '@tabler/icons-react';
import { QRCodeSVG } from 'qrcode.react';
import { useKioskPaymentStream } from '../hooks/useKioskPaymentStream';

export interface QRPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  eventId: string;
  sessionToken: string;
  onPaymentComplete: () => void;
}

/**
 * Modal for digital payment via QR code
 * Shows QR code that attendee scans to pay
 * Listens for real-time payment notifications via SSE
 * Displays success animation when payment received
 */
export const QRPaymentModal: React.FC<QRPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  eventId,
  sessionToken,
  onPaymentComplete
}) => {
  const [paymentReceived, setPaymentReceived] = useState(false);

  // Real-time payment stream (SSE)
  const { lastPayment, status } = useKioskPaymentStream(sessionToken);

  // Watch for payment completion
  useEffect(() => {
    if (lastPayment?.attendeeId === attendee.id) {
      console.log('[QR Payment] Payment received for attendee:', attendee.id);
      setPaymentReceived(true);

      // Show success for 2 seconds, then close
      setTimeout(() => {
        onPaymentComplete();
        onClose();
        setPaymentReceived(false); // Reset for next use
      }, 2000);
    }
  }, [lastPayment, attendee.id, onPaymentComplete, onClose]);

  // Reset payment state when modal closes
  useEffect(() => {
    if (!opened) {
      setPaymentReceived(false);
    }
  }, [opened]);

  // Generate QR code URL for payment
  const paymentUrl = `${window.location.origin}/events/${eventId}/purchase?attendeeId=${attendee.id}&returnUrl=checkin`;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={paymentReceived ? 'Payment Received!' : 'Scan to Pay'}
      centered
      size="lg"
      withCloseButton={!paymentReceived}
      closeOnClickOutside={false}
      styles={{
        title: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 700,
          fontSize: '20px',
          textTransform: 'uppercase',
          color: paymentReceived ? '#228B22' : '#880124',
        },
      }}
    >
      <Stack gap="lg" align="center">
        {/* Attendee Name */}
        <Text size="lg" fw={600} c="charcoal">
          Attendee: {attendee.name}
        </Text>

        {/* QR Code or Success Icon */}
        {paymentReceived ? (
          <ThemeIcon
            size={250}
            radius="50%"
            color="green"
            style={{
              animation: 'checkmark-pop 0.5s ease-out'
            }}
          >
            <IconCheck size={150} stroke={3} />
          </ThemeIcon>
        ) : (
          <QRCodeSVG
            value={paymentUrl}
            size={250}
            bgColor="#FFF8F0" // ivory
            fgColor="#2B2B2B" // charcoal
            level="M" // Medium error correction
            includeMargin
            style={{
              padding: '16px',
              background: 'white',
              borderRadius: '8px',
              boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
            }}
          />
        )}

        {/* Instructions or Success Message */}
        {paymentReceived ? (
          <Text size="md" c="green" fw={600} ta="center">
            Payment confirmed via PayPal
          </Text>
        ) : (
          <>
            <Text size="md" ta="center" c="charcoal">
              Scan with your phone to complete payment
            </Text>

            <Text size="xs" c="dimmed" ta="center" style={{ wordBreak: 'break-all' }}>
              {paymentUrl}
            </Text>

            {/* Progress Indicator */}
            <Stack gap="xs" style={{ width: '100%' }}>
              <Text size="sm" c="dimmed" ta="center">
                {status === 'connected' ? '⏳ Waiting for payment...' : '🔄 Connecting...'}
              </Text>
              <Progress
                value={100}
                animated
                color="red"
                size="sm"
                style={{ width: '100%' }}
              />
            </Stack>
          </>
        )}

        {/* Cancel Button (only when waiting) */}
        {!paymentReceived && (
          <Button
            variant="outline"
            color="red"
            onClick={onClose}
            mt="md"
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                lineHeight: '1.2'
              }
            }}
          >
            Cancel
          </Button>
        )}
      </Stack>

      {/* CSS for success animation */}
      <style>{`
        @keyframes checkmark-pop {
          0% {
            transform: scale(0);
            opacity: 0;
          }
          50% {
            transform: scale(1.2);
          }
          100% {
            transform: scale(1);
            opacity: 1;
          }
        }
      `}</style>
    </Modal>
  );
};
