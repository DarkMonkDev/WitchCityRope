import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, MultiSelect, Textarea, Switch, Alert, Radio, Text } from '@mantine/core';
import { DateInput } from '@mantine/dates';
import { useForm } from '@mantine/form';
import { IconAlertCircle } from '@tabler/icons-react';
import type { components } from '@witchcityrope/shared-types';
import type { EventSession } from './EventSessionsGrid';

// Define the modal's own EventTicketType interface
export interface EventTicketType {
  id: string;
  name: string;
  description: string;
  pricingType: components["schemas"]["PricingType"]; // Use generated type from backend
  price?: number; // For fixed price tickets
  minPrice?: number; // For sliding scale tickets
  maxPrice?: number; // For sliding scale tickets
  defaultPrice?: number; // Default/suggested price for sliding scale
  sessionsIncluded: string[];
  quantityAvailable: number;
  quantitySold: number;
  allowMultiplePurchase: boolean;
}

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
      pricingType: 'Fixed' as components["schemas"]["PricingType"],
      price: 0,
      minPrice: 0,
      maxPrice: 0,
      defaultPrice: 0,
      sessionsIncluded: [],
      quantityAvailable: 100,
      quantitySold: 0,
      allowMultiplePurchase: false,
    },
    validate: {
      name: (value) => (!value ? 'Ticket name is required' : null),
      price: (value, values) => {
        if (values.pricingType === 'Fixed') {
          if (value < 0) return 'Price cannot be negative';
          if (value > 9999) return 'Price cannot exceed $9,999';
        }
        return null;
      },
      minPrice: (value, values) => {
        if (values.pricingType === 'SlidingScale') {
          if (value < 0) return 'Min price cannot be negative';
          if (value > 9999) return 'Min price cannot exceed $9,999';
          if (value > values.maxPrice) return 'Min price cannot be greater than max price';
        }
        return null;
      },
      maxPrice: (value, values) => {
        if (values.pricingType === 'SlidingScale') {
          if (value < 0) return 'Max price cannot be negative';
          if (value > 9999) return 'Max price cannot exceed $9,999';
          if (value < values.minPrice) return 'Max price cannot be less than min price';
        }
        return null;
      },
      defaultPrice: (value, values) => {
        if (values.pricingType === 'SlidingScale') {
          if (value < values.minPrice) return 'Default price cannot be less than min price';
          if (value > values.maxPrice) return 'Default price cannot be greater than max price';
        }
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
      pricingType: values.pricingType,
      price: values.pricingType === 'Fixed' ? values.price : undefined,
      minPrice: values.pricingType === 'SlidingScale' ? values.minPrice : undefined,
      maxPrice: values.pricingType === 'SlidingScale' ? values.maxPrice : undefined,
      defaultPrice: values.pricingType === 'SlidingScale' ? values.defaultPrice : undefined,
      sessionsIncluded: values.sessionsIncluded,
      quantityAvailable: values.quantityAvailable,
      quantitySold: values.quantitySold,
      allowMultiplePurchase: values.allowMultiplePurchase,
    };
    onSubmit(ticketData);
    form.reset();
    onClose();
  });

  // Create session options for MultiSelect with safety checks
  // CRITICAL: Only show persisted sessions (those with valid IDs and complete date/time data)
  // This prevents crashes when unsaved sessions are selected
  const sessionOptions = availableSessions
    .filter(session =>
      session?.sessionIdentifier &&
      session?.name &&
      session?.id &&
      session?.date &&
      session?.startTime &&
      !session.id.startsWith('temp-') // Exclude temporary IDs
    )
    .map(session => ({
      value: session.sessionIdentifier,
      label: `${session.sessionIdentifier} - ${session.name}`,
    }));

  const selectOptions = sessionOptions;

  // Handle sessions selection
  const handleSessionsChange = (value: string[]) => {
    form.setFieldValue('sessionsIncluded', value || []);
  };

  // Handle modal opening and data population
  React.useEffect(() => {
    if (opened) {
      if (ticketType) {
        // Populate form with existing ticket type data for editing
        form.setValues({
          name: ticketType.name,
          description: ticketType.description || '',
          pricingType: ticketType.pricingType || 'Fixed',
          price: ticketType.price || 0,
          minPrice: ticketType.minPrice || 0,
          maxPrice: ticketType.maxPrice || 0,
          defaultPrice: ticketType.defaultPrice || 0,
          sessionsIncluded: ticketType.sessionsIncluded,
          quantityAvailable: ticketType.quantityAvailable,
          quantitySold: ticketType.quantitySold,
          allowMultiplePurchase: ticketType.allowMultiplePurchase,
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
          {sessionOptions.length === 0 && (
            <Alert icon={<IconAlertCircle />} color="orange" title="Save Event First">
              Please save the event with your sessions before creating tickets.
              Tickets can only be created for saved sessions with complete date and time information.
            </Alert>
          )}

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

          {/* Pricing Type Selection */}
          <div>
            <Text size="sm" fw={500} mb={5}>
              Pricing Type <Text component="span" c="red">*</Text>
            </Text>
            <Radio.Group
              value={form.values.pricingType}
              onChange={(value) => form.setFieldValue('pricingType', value as components["schemas"]["PricingType"])}
            >
              <Group mt="xs">
                <Radio value="Fixed" label="Fixed Price" />
                <Radio value="SlidingScale" label="Sliding Scale (Pay What You Can)" />
              </Group>
            </Radio.Group>
          </div>

          {/* Conditional Pricing Fields */}
          {form.values.pricingType === 'Fixed' ? (
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
          ) : (
            <>
              <Group grow>
                <NumberInput
                  label="Minimum Price ($)"
                  placeholder="0.00"
                  min={0}
                  max={9999}
                  decimalScale={2}
                  fixedDecimalScale
                  required
                  {...form.getInputProps('minPrice')}
                />
                <NumberInput
                  label="Maximum Price ($)"
                  placeholder="0.00"
                  min={0}
                  max={9999}
                  decimalScale={2}
                  fixedDecimalScale
                  required
                  {...form.getInputProps('maxPrice')}
                />
              </Group>
              <Group grow>
                <NumberInput
                  label="Default/Suggested Price ($)"
                  placeholder="0.00"
                  min={0}
                  max={9999}
                  decimalScale={2}
                  fixedDecimalScale
                  required
                  {...form.getInputProps('defaultPrice')}
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
            </>
          )}

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
            label="Allow multiple purchases per customer (Coming soon)"
            checked={form.values.allowMultiplePurchase}
            {...form.getInputProps('allowMultiplePurchase', { type: 'checkbox' })}
          />

          <Group justify="flex-end" mt="md">
            <Button variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={sessionOptions.length === 0}
              styles={{
                root: {
                  background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
                  border: 'none',
                  color: 'var(--mantine-color-dark-9)',
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                }
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