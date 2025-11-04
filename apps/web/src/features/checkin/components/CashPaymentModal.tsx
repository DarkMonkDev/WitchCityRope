// CashPaymentModal - Modal for recording cash payments at the door
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md
// Updated for functional spec v2.0 - adds ticket type selector, allows $0.00

import React from 'react';
import {
  Modal,
  NumberInput,
  Textarea,
  Button,
  Text,
  Group,
  Stack,
  Select,
  Checkbox
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconX } from '@tabler/icons-react';
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
 * Updated for functional spec v2.0:
 * - Added ticket type selector (required)
 * - Allows $0.00 amounts for free tickets
 * - Validates amount between $0.00 and $1,000
 * - Includes optional notes field for special circumstances
 */
export const CashPaymentModal: React.FC<CashPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  ticketTypes,
  onSubmit
}) => {
  const form = useForm<CashPaymentData & { covidTestComplete: boolean }>({
    initialValues: {
      ticketTypeId: '',
      amount: 0,
      notes: '',
      covidTestComplete: false,
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
      covidTestComplete: (value) => {
        if (!value) return 'COVID test must be completed before check-in';
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

  const handleSubmit = async (values: CashPaymentData) => {
    try {
      await onSubmit(values);
      form.reset();
      onClose();
      notifications.show({
        title: 'Payment Recorded',
        message: `$${values.amount.toFixed(2)} cash payment recorded`,
        color: 'green',
        icon: <IconCheck />,
      });
    } catch (error) {
      notifications.show({
        title: 'Payment Failed',
        message: 'Failed to record payment. Please try again.',
        color: 'red',
        icon: <IconX />,
        autoClose: 5000,
      });
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Record Cash Payment"
      centered
      size="md"
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

          {/* Ticket Type Selector (UPDATED - handles both fixed and sliding scale) */}
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
            aria-describedby="ticket-type-helper"
          />
          <Text id="ticket-type-helper" size="xs" c="dimmed">
            Choose the ticket type being purchased
          </Text>

          {/* COVID Test Checkbox (NEW - required before check-in) */}
          <Checkbox
            label="COVID test complete"
            description="Attendee has completed COVID test screening"
            required
            size="md"
            styles={{
              label: {
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
                fontSize: '14px',
                color: '#4A4A4A',
              },
              description: {
                fontSize: '12px',
                color: '#6B7280',
                marginTop: '4px',
              },
            }}
            {...form.getInputProps('covidTestComplete', { type: 'checkbox' })}
            aria-label="COVID test complete"
          />

          {/* Amount Input (UPDATED - allows $0.00) */}
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
                color: '#4A4A4A', // smoke
                marginBottom: '8px',
              },
            }}
            {...form.getInputProps('amount')}
            aria-label="Payment amount in dollars"
            aria-describedby="amount-helper"
          />
          <Text id="amount-helper" size="xs" c="dimmed">
            Enter the cash amount received (can be $0.00 for free tickets)
          </Text>

          {/* Payment Method (Read-Only) */}
          <Text size="sm" c="dimmed">
            <Text fw={600} component="span">Payment Method:</Text> Cash
          </Text>

          {/* Notes Textarea (UPDATED - 500 chars max per spec) */}
          <Textarea
            label="Notes (Optional)"
            placeholder="For special circumstances (e.g., sliding scale, discount, comp ticket)"
            minRows={3}
            maxRows={5}
            maxLength={500}
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
            {...form.getInputProps('notes')}
          />
          <Text size="xs" c="dimmed" ta="right">
            {(form.values.notes?.length || 0)}/500 characters
          </Text>

          {/* Action Buttons */}
          <Group
            justify="flex-end"
            mt="md"
            gap="sm"
          >
            <Button
              variant="outline"
              color="red"
              onClick={onClose}
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
            <Button
              type="submit"
              styles={{
                root: {
                  background: 'linear-gradient(135deg, #FFBF00 0%, #FF8C00 100%)',
                  color: '#1A1A2E',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  fontSize: '14px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  lineHeight: '1.2',
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
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};
