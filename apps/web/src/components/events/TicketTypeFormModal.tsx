import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, MultiSelect, Textarea, Switch } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
// Define the modal's own EventTicketType interface
export interface EventTicketType {
  id: string;
  name: string;
  description: string;
  price: number;
  sessionsIncluded: string[];
  quantityAvailable: number;
  quantitySold: number;
  allowMultiplePurchase: boolean;
  saleEndDate?: Date;
}
import type { EventSession } from './EventSessionsGrid';

interface TicketTypeFormModalProps {
  opened: boolean;
  onClose: () => void;
  onSubmit: (ticketType: Omit<EventTicketType, 'id'>) => void;
  ticketType?: EventTicketType | null;
  availableSessions: EventSession[];
}

export const TicketTypeFormModal: React.FC<TicketTypeFormModalProps> = ({
  opened,
  onClose,
  onSubmit,
  ticketType,
  availableSessions,
}) => {
  const form = useForm({
    initialValues: {
      name: '',
      description: '',
      price: 0,
      sessionsIncluded: [],
      quantityAvailable: 100,
      quantitySold: 0,
      allowMultiplePurchase: true,
      saleEndDate: null,
    },
    validate: {
      name: (value) => (!value ? 'Ticket name is required' : null),
      price: (value) => {
        if (value < 0) return 'Price cannot be negative';
        if (value > 9999) return 'Price cannot exceed $9,999';
        return null;
      },
      sessionsIncluded: (value) => {
        if (!value || value.length === 0) return 'At least one session must be selected';
        return null;
      },
      quantityAvailable: (value, values) => {
        if (value < 1) return 'Quantity must be at least 1';
        if (value < values.quantitySold) return 'Cannot be less than quantity already sold';
        return null;
      },
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    const ticketData: Omit<EventTicketType, 'id'> = {
      name: values.name,
      description: values.description,
      price: values.price,
      sessionsIncluded: values.sessionsIncluded,
      quantityAvailable: values.quantityAvailable,
      quantitySold: values.quantitySold,
      allowMultiplePurchase: values.allowMultiplePurchase,
      saleEndDate: values.saleEndDate || undefined,
    };
    onSubmit(ticketData);
    form.reset();
    onClose();
  });

  // Create session options for MultiSelect
  const sessionOptions = availableSessions.map(session => ({
    value: session.sessionIdentifier,
    label: `${session.sessionIdentifier} - ${session.name}`,
  }));

  // Add "All Sessions" option
  const allSessionsOption = {
    value: 'ALL',
    label: 'All Sessions (Multi-day Pass)',
  };

  const selectOptions = [allSessionsOption, ...sessionOptions];

  // Handle "All Sessions" selection
  const handleSessionsChange = (value: string[]) => {
    if (value.includes('ALL')) {
      // If "ALL" is selected, select all individual sessions
      form.setFieldValue('sessionsIncluded', ['ALL', ...sessionOptions.map(s => s.value)]);
    } else {
      form.setFieldValue('sessionsIncluded', value);
    }
    
    // Auto-set sale end date based on session selection
    updateSaleEndDate(value);
  };

  // Smart sale end date defaults
  const updateSaleEndDate = (selectedSessions: string[]) => {
    if (selectedSessions.length === 0 || !availableSessions.length) return;
    
    let targetDate: Date | null = null;
    
    if (selectedSessions.includes('ALL')) {
      // Use the earliest session start date/time
      const earliestSession = availableSessions.reduce((earliest, session) => {
        const sessionDateTime = new Date(`${session.date}T${session.startTime}`);
        const earliestDateTime = new Date(`${earliest.date}T${earliest.startTime}`);
        return sessionDateTime < earliestDateTime ? session : earliest;
      });
      targetDate = new Date(`${earliestSession.date}T${earliestSession.startTime}`);
    } else {
      // Find the selected session(s) and use the earliest
      const selectedSessionsData = availableSessions.filter(s => 
        selectedSessions.includes(s.sessionIdentifier)
      );
      
      if (selectedSessionsData.length > 0) {
        const earliestSelected = selectedSessionsData.reduce((earliest, session) => {
          const sessionDateTime = new Date(`${session.date}T${session.startTime}`);
          const earliestDateTime = new Date(`${earliest.date}T${earliest.startTime}`);
          return sessionDateTime < earliestDateTime ? session : earliest;
        });
        targetDate = new Date(`${earliestSelected.date}T${earliestSelected.startTime}`);
      }
    }
    
    // Only auto-set if no sale end date is currently set
    if (targetDate && !form.values.saleEndDate) {
      form.setFieldValue('saleEndDate', targetDate);
    }
  };

  // Handle modal opening and data population
  React.useEffect(() => {
    if (opened) {
      if (ticketType) {
        // Populate form with existing ticket type data for editing
        form.setValues({
          name: ticketType.name,
          description: ticketType.description,
          price: ticketType.price,
          sessionsIncluded: ticketType.sessionsIncluded,
          quantityAvailable: ticketType.quantityAvailable,
          quantitySold: ticketType.quantitySold,
          allowMultiplePurchase: ticketType.allowMultiplePurchase,
          saleEndDate: ticketType.saleEndDate || null,
        });
      } else {
        // Reset form for new ticket type
        form.reset();
      }
    }
  }, [opened, ticketType]);


  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={ticketType ? 'Edit Ticket Type' : 'Add Ticket Type'}
      size="lg"
      centered
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label="Ticket Name"
            placeholder="e.g., General Admission, VIP Pass"
            required
            {...form.getInputProps('name')}
          />

          <Textarea
            label="Description"
            placeholder="Brief description of what's included with this ticket"
            minRows={2}
            {...form.getInputProps('description')}
          />

          <MultiSelect
            label="Sessions Included"
            placeholder="Select sessions this ticket grants access to"
            data={selectOptions}
            required
            searchable
            clearable
            value={form.values.sessionsIncluded}
            onChange={handleSessionsChange}
            error={form.errors.sessionsIncluded}
          />

          <Group grow>
            <NumberInput
              label="Price ($)"
              placeholder="0.00"
              min={0}
              max={9999}
              decimalScale={2}
              fixedDecimalScale
              required
              {...form.getInputProps('price')}
            />
            <NumberInput
              label="Quantity Available"
              placeholder="Maximum tickets to sell"
              min={1}
              max={9999}
              required
              {...form.getInputProps('quantityAvailable')}
            />
          </Group>

          {ticketType && (
            <NumberInput
              label="Quantity Sold"
              placeholder="Already sold"
              min={0}
              disabled
              {...form.getInputProps('quantitySold')}
            />
          )}

          <Switch
            label="Allow multiple purchases per customer"
            checked={form.values.allowMultiplePurchase}
            {...form.getInputProps('allowMultiplePurchase', { type: 'checkbox' })}
          />

          <DateInput
            label="Sale End Date"
            placeholder="Select when ticket sales should end"
            data-testid="ticket-sale-end-date-input"
            {...form.getInputProps('saleEndDate')}
          />

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button
              type="submit"
              style={{
                background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                border: 'none',
                color: 'var(--mantine-color-dark-9)',
                fontWeight: 600,
              }}
            >
              {ticketType ? 'Update Ticket Type' : 'Add Ticket Type'}
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
};