import React from 'react';
import { Modal, TextInput, NumberInput, Group, Button, Stack, MultiSelect, Alert, Radio, Text } from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconAlertCircle } from '@tabler/icons-react';
import type { EventSession } from './EventSessionsGrid';
import type { EventTicketType } from './EventTicketTypesGrid';

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
      pricingType: 'Fixed' as 'Fixed' | 'SlidingScale',
      price: 0,
      minPrice: 0,
      maxPrice: 0,
      defaultPrice: 0,
      sessionIdentifiers: [] as string[],
      quantityAvailable: 100,
      quantitySold: 0,
      maxQuantityPerPurchase: 3,
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
      sessionIdentifiers: (value) => {
        if (!value || value.length === 0) return 'At least one session must be selected';
        return null;
      },
      quantityAvailable: (value, values) => {
        if (value < 1) return 'Quantity must be at least 1';
        if (value < values.quantitySold) return 'Cannot be less than quantity already sold';
        return null;
      },
      maxQuantityPerPurchase: (value) => {
        if (value === undefined || value === null) return 'Required';
        if (value < 1) return 'Must be at least 1';
        if (value > 100) return 'Cannot exceed 100';
        return null;
      },
    },
  });

  const handleSubmit = form.onSubmit((values) => {
    // Build ticket data matching the auto-generated TicketTypeDto structure
    const ticketData: Omit<EventTicketType, 'id'> = {
      name: values.name,
      pricingType: values.pricingType,
      price: values.pricingType === 'Fixed' ? values.price : undefined,
      minPrice: values.pricingType === 'SlidingScale' ? values.minPrice : undefined,
      maxPrice: values.pricingType === 'SlidingScale' ? values.maxPrice : undefined,
      defaultPrice: values.pricingType === 'SlidingScale' ? values.defaultPrice : undefined,
      sessionIdentifiers: values.sessionIdentifiers,
      quantityAvailable: values.quantityAvailable,
      quantitySold: values.quantitySold,
      maxQuantityPerPurchase: values.maxQuantityPerPurchase,
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
      session?.startDate &&
      session?.startTime &&
      !session.id.startsWith('temp-') // Exclude temporary IDs
    )
    .map(session => ({
      value: session.sessionIdentifier || '',
      label: `${session.sessionIdentifier} - ${session.name}`,
    }));

  const selectOptions = sessionOptions;

  // Handle sessions selection
  const handleSessionsChange = (value: string[]) => {
    form.setFieldValue('sessionIdentifiers', value || []);
  };

  // Handle modal opening and data population
  React.useEffect(() => {
    if (opened) {
      if (ticketType) {
        // Populate form with existing ticket type data for editing
        // Uses auto-generated TicketTypeDto fields directly — no field name conversion needed
        form.setValues({
          name: ticketType.name || '',
          pricingType: ticketType.pricingType || 'Fixed',
          price: ticketType.price ?? 0,
          minPrice: ticketType.minPrice ?? 0,
          maxPrice: ticketType.maxPrice ?? 0,
          defaultPrice: ticketType.defaultPrice ?? 0,
          sessionIdentifiers: ticketType.sessionIdentifiers || [],
          quantityAvailable: ticketType.quantityAvailable ?? 100,
          quantitySold: ticketType.quantitySold ?? 0,
          maxQuantityPerPurchase: ticketType.maxQuantityPerPurchase ?? 3,
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

          <MultiSelect
            label="Sessions Included"
            placeholder="Select sessions this ticket grants access to"
            data={selectOptions}
            required
            searchable
            clearable
            value={form.values.sessionIdentifiers}
            onChange={handleSessionsChange}
            error={form.errors.sessionIdentifiers}
          />

          {/* Pricing Type Selection */}
          <div>
            <Text size="sm" fw={500} mb={5}>
              Pricing Type <Text component="span" c="red">*</Text>
            </Text>
            <Radio.Group
              value={form.values.pricingType}
              onChange={(value) => form.setFieldValue('pricingType', value as 'Fixed' | 'SlidingScale')}
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

          <NumberInput
            label="Max Per Purchase"
            description="Maximum tickets a person can buy at once for this type"
            placeholder="3"
            min={1}
            max={100}
            required
            {...form.getInputProps('maxQuantityPerPurchase')}
          />

          {ticketType && (
            <NumberInput
              label="Quantity Sold"
              placeholder="Already sold"
              min={0}
              disabled
              {...form.getInputProps('quantitySold')}
            />
          )}

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