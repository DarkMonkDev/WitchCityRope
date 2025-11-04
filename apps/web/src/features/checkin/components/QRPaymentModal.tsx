// QRPaymentModal - Display QR code for digital payment (simplified, non-blocking)
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/functional-specification.md v2.0
// SIMPLIFIED: No SSE, no real-time detection, display-only
// Staff shows QR, closes modal, continues with others
// Attendee completes payment on their phone via existing ticket sales page
// Later, staff searches for attendee again - they'll have a ticket

import React from 'react';
import { Modal, Text, Stack, Button } from '@mantine/core';
import { QRCodeSVG } from 'qrcode.react';

export interface QRPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  eventId: string;
}

/**
 * SIMPLIFIED Modal for digital payment via QR code
 *
 * Changes from v1.0:
 * - Removed SSE/real-time payment detection
 * - Removed useKioskPaymentStream hook
 * - Removed payment success animation
 * - Simple display-only modal
 * - Staff closes immediately (non-blocking)
 * - Links to existing ticket sales page
 * - Manual status update workflow (staff searches again later)
 */
export const QRPaymentModal: React.FC<QRPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  eventId
}) => {
  // Generate QR code URL - links to existing ticket sales page
  // Per functional spec v2.0: Simple URL, no special parameters
  // Route: /checkout/:eventId (from router.tsx line 164)
  const ticketSalesUrl = `${window.location.origin}/checkout/${eventId}`;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Scan QR Code to Purchase Ticket"
      centered
      size="lg"
      styles={{
        title: {
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 700,
          fontSize: '20px',
          textTransform: 'uppercase',
          color: '#880124', // burgundy
        },
      }}
    >
      <Stack gap="lg" align="center">
        {/* Attendee Name */}
        <Text size="lg" fw={600} c="charcoal">
          {attendee.name}
        </Text>

        {/* QR Code */}
        <QRCodeSVG
          value={ticketSalesUrl}
          size={250}
          bgColor="#FFF8F0" // ivory
          fgColor="#2B2B2B" // charcoal
          level="H" // High error correction
          includeMargin
          style={{
            padding: '16px',
            background: 'white',
            borderRadius: '8px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
          }}
        />

        {/* Instructions - NON-BLOCKING WORKFLOW */}
        <Stack gap="sm" style={{ width: '100%' }}>
          <Text size="md" fw={600} c="charcoal" ta="center">
            How it works:
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            1. Attendee scans QR code with their phone
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            2. Opens our ticket sales page in their browser
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            3. They log in (or sign up if new)
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            4. They purchase ticket using PayPal
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            5. <Text component="span" fw={600}>You can close this window now</Text> - don't wait!
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            6. Continue checking in other attendees
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            7. Later (1-2 minutes), search for this attendee again
          </Text>
          <Text size="sm" c="dimmed" ta="left" pl="md">
            8. They'll show as "Ticket Purchased" - complete check-in
          </Text>
        </Stack>

        {/* URL Display (for reference) */}
        <Text size="xs" c="dimmed" ta="center" style={{ wordBreak: 'break-all' }}>
          {ticketSalesUrl}
        </Text>

        {/* Close Button - Non-blocking workflow */}
        <Button
          onClick={onClose}
          fullWidth
          size="lg"
          styles={{
            root: {
              background: 'linear-gradient(135deg, #9D4EDD 0%, #7B2CBF 100%)',
              color: '#FFF8F0',
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'Montserrat, sans-serif',
              fontWeight: 600,
              fontSize: '16px',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              height: '56px',
              boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
              '&:hover': {
                background: 'linear-gradient(135deg, #7B2CBF 0%, #9D4EDD 100%)',
                boxShadow: '0 6px 20px rgba(157, 78, 221, 0.5)',
                borderRadius: '6px 12px 6px 12px'
              }
            }
          }}
        >
          Close (Continue Checking In Others)
        </Button>
      </Stack>
    </Modal>
  );
};
