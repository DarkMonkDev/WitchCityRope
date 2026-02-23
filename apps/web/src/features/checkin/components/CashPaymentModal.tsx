// CashPaymentModal - Modal for recording cash payments at the door
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md
// Updated for functional spec v2.0 - adds ticket type selector, allows $0.00

import React, { useState } from 'react';
import {
  Modal,
  NumberInput,
  Button,
  Text,
  Stack,
  Select
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconCheck } from '@tabler/icons-react';
import type { CashPaymentData, TicketType } from '../types/checkin.types';

export interface CashPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  ticketTypes: TicketType[];
  onSubmit: (data: CashPaymentData) => Promise<void>;
}

/**
 * Modal for recording cash payments at the door
 * Updated workflow:
 * 1. Fill ticket type and amount (fields enabled from start)
 * 2. Click "COVID Test Complete" button
 * 3. Button changes to "Record Payment"
 * 4. Click "Record Payment" to submit and auto-check-in
 */
export const CashPaymentModal: React.FC<CashPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  ticketTypes,
  onSubmit
}) => {
  const [covidTestComplete, setCovidTestComplete] = useState(false);

  const form = useForm<CashPaymentData>({
    initialValues: {
      ticketTypeId: '',
      amount: 0,
      notes: '',
    },
    validate: {
      ticketTypeId: (value) => {
        if (!value) return 'Ticket type is required';
        return null;
      },
      amount: (value) => {
        if (value < 0) return 'Amount cannot be negative';
        if (value > 1000) return 'Amount cannot exceed $1,000.00';
        return null;
      },
    },
  });

  // Helper function to format ticket price display
  const formatTicketPrice = (ticket: TicketType): string => {
    if (ticket.price !== undefined && ticket.price !== null) {
      // Fixed price
      return `$${ticket.price.toFixed(2)}`;
    } else if (ticket.minPrice !== undefined && ticket.maxPrice !== undefined) {
      // Sliding scale
      return `$${ticket.minPrice.toFixed(2)} - $${ticket.maxPrice.toFixed(2)}`;
    }
    return 'Price not set';
  };

  const handleCovidTestComplete = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setCovidTestComplete(true);
  };

  const handleSubmit = async (values: CashPaymentData) => {
    try {
      await onSubmit(values);
      form.reset();
      setCovidTestComplete(false); // Reset for next use
      onClose();
      // KIOSK MODE: Notification disabled for streamlined UX
      // notifications.show({
      //   title: 'Payment & Check-In Complete',
      //   message: `$${values.amount.toFixed(2)} cash payment recorded`,
      //   color: 'green',
      //   icon: <IconCheck />,
      // });
    } catch (error) {
      // KIOSK MODE: Notification disabled for streamlined UX
      // notifications.show({
      //   title: 'Payment Failed',
      //   message: 'Failed to record payment. Please try again.',
      //   color: 'red',
      //   icon: <IconX />,
      //   autoClose: 5000,
      // });
      console.error('Payment failed:', error);
    }
  };

  const handleClose = () => {
    form.reset();
    setCovidTestComplete(false); // Reset state on close
    onClose();
  };

  // Debug: Log ticket types to console
  console.log('CashPaymentModal ticketTypes:', ticketTypes);

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title="Record Cash Payment"
      centered
      size="md"
      closeOnClickOutside={true}
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
      <form onSubmit={form.onSubmit(handleSubmit)}>
        <Stack gap="md">
          {/* Attendee Name Display */}
          <Text
            size="lg"
            fw={600}
            c="charcoal"
            style={{ marginBottom: '16px' }}
          >
            Attendee: {attendee.name}
          </Text>

          {/* Ticket Type Selector - Always enabled */}
          <Select
            label="Ticket Type"
            placeholder="Select ticket type"
            data={ticketTypes.map(t => ({
              value: t.id,
              label: `${t.name} - ${formatTicketPrice(t)}`
            }))}
            required
            size="md"
            styles={{
              label: {
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                color: '#4A4A4A',
                marginBottom: '8px',
              },
            }}
            {...form.getInputProps('ticketTypeId')}
            aria-label="Select ticket type"
          />

          {/* Amount Input - Always enabled */}
          <NumberInput
            label="Amount Paid"
            placeholder="0.00"
            prefix="$"
            decimalScale={2}
            min={0.00}
            max={1000}
            required
            hideControls
            size="md"
            styles={{
              label: {
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                color: '#4A4A4A',
                marginBottom: '8px',
              },
            }}
            {...form.getInputProps('amount')}
            aria-label="Payment amount in dollars"
          />
          <Text size="xs" c="dimmed">
            Enter the cash amount received (can be $0.00 for free tickets)
          </Text>

          {/* Payment Method (Read-Only) */}
          <Text size="sm" c="dimmed">
            <Text fw={600} component="span">Payment Method:</Text> Cash
          </Text>

          {/* Action Button - Shows checkmark icon for Covid Test first, then Record Payment */}
          {!covidTestComplete ? (
            <Button
              type="button"
              onClick={handleCovidTestComplete}
              fullWidth
              size="lg"
              leftSection={<IconCheck size={24} />}
              styles={{
                root: {
                  background: 'linear-gradient(135deg, #4CAF50 0%, #45a049 100%)',
                  color: 'white',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  fontSize: '16px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  height: '56px',
                  boxShadow: '0 4px 15px rgba(76, 175, 80, 0.4)',
                  '&:hover': {
                    background: 'linear-gradient(135deg, #45a049 0%, #4CAF50 100%)',
                    boxShadow: '0 6px 20px rgba(76, 175, 80, 0.5)',
                    borderRadius: '6px 12px 6px 12px'
                  }
                }
              }}
            >
              Covid Test
            </Button>
          ) : (
            <Button
              type="submit"
              fullWidth
              size="lg"
              styles={{
                root: {
                  background: 'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
                  color: '#1A1A2E',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  fontSize: '16px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  height: '56px',
                  boxShadow: '0 4px 15px rgba(255, 191, 0, 0.4)',
                  '&:hover': {
                    background: 'linear-gradient(135deg, #FF8C00 0%, #FFBF00 100%)',
                    boxShadow: '0 6px 20px rgba(255, 191, 0, 0.5)',
                    borderRadius: '6px 12px 6px 12px'
                  }
                }
              }}
            >
              Record Payment
            </Button>
          )}
        </Stack>
      </form>
    </Modal>
  );
};
