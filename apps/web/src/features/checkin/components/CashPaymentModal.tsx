// CashPaymentModal - Modal for recording cash payments at the door
// Source: /docs/functional-areas/events/new-work/2025-11-03-streamlined-checkin-workflow/design/ui-specifications.md

import React from 'react';
import {
  Modal,
  NumberInput,
  Textarea,
  Button,
  Text,
  Group,
  Stack
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { notifications } from '@mantine/notifications';
import { IconCheck, IconX } from '@tabler/icons-react';

export interface CashPaymentModalProps {
  opened: boolean;
  onClose: () => void;
  attendee: {
    id: string;
    name: string;
  };
  onSubmit: (data: CashPaymentData) => Promise<void>;
}

export interface CashPaymentData {
  amount: number;
  notes?: string;
}

/**
 * Modal for recording cash payments at the door
 * Validates amount between $0.01 and $1,000
 * Includes optional notes field for special circumstances
 */
export const CashPaymentModal: React.FC<CashPaymentModalProps> = ({
  opened,
  onClose,
  attendee,
  onSubmit
}) => {
  const form = useForm<CashPaymentData>({
    initialValues: {
      amount: 0,
      notes: '',
    },
    validate: {
      amount: (value) => {
        if (value < 0.01) return 'Amount must be at least $0.01';
        if (value > 1000) return 'Amount cannot exceed $1,000.00';
        return null;
      },
    },
  });

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

          {/* Amount Input */}
          <NumberInput
            label="Amount"
            placeholder="0.00"
            prefix="$"
            decimalScale={2}
            min={0.01}
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
            Enter the cash amount received
          </Text>

          {/* Payment Method (Read-Only) */}
          <Text size="sm" c="dimmed">
            <Text fw={600} component="span">Payment Method:</Text> Cash
          </Text>

          {/* Notes Textarea */}
          <Textarea
            label="Notes (Optional)"
            placeholder="For special circumstances (e.g., discount reason)"
            minRows={3}
            maxRows={5}
            maxLength={200}
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
            {(form.values.notes?.length || 0)}/200 characters
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
