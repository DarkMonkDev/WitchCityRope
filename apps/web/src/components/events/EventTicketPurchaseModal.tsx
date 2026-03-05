import React, { useState } from 'react';
import { Modal, Title, Text, Stack, Group, Button, Card, Badge, NumberInput, Divider, Radio, Alert, Box } from '@mantine/core';
import { IconTicket, IconCreditCard, IconAlertCircle } from '@tabler/icons-react'
import { useEventTimeZone } from '../../hooks/useEventTimeZone';
import type { EventTicketType } from './EventTicketTypesGrid';

export interface TicketPurchaseData {
  eventId: string;
  ticketTypeId: string;
  quantity: number;
  paymentMethod: 'paypal' | 'venmo';
  totalAmount: number;
}

interface EventInfo {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
}

interface EventTicketPurchaseModalProps {
  opened: boolean;
  onClose: () => void;
  onPurchase: (purchaseData: TicketPurchaseData) => void;
  event: EventInfo;
  ticketTypes: EventTicketType[];
  isPurchasing?: boolean;
}

/**
 * EventTicketPurchaseModal - Modal for purchasing tickets for Classes and Social Events
 * Used for both Classes (required payment) and Social Events (optional payment upgrade)
 */
export const EventTicketPurchaseModal: React.FC<EventTicketPurchaseModalProps> = ({
  opened,
  onClose,
  onPurchase,
  event,
  ticketTypes,
  isPurchasing = false,
}) => {
  const eventTimeZone = useEventTimeZone();
  const [selectedTicketTypeId, setSelectedTicketTypeId] = useState<string>('');
  const [quantity, setQuantity] = useState(1);
  const [paymentMethod, setPaymentMethod] = useState<'paypal' | 'venmo'>('paypal');

  const selectedTicketType = ticketTypes.find(t => t.id === selectedTicketTypeId);
  const available = selectedTicketType
    ? (selectedTicketType.quantityAvailable ?? 0) - (selectedTicketType.quantitySold ?? 0)
    : 0;
  
  const effectivePrice = selectedTicketType?.price || 0;
  
  const totalAmount = effectivePrice * quantity;

  const handlePurchase = () => {
    if (!selectedTicketType) return;

    onPurchase({
      eventId: event.id,
      ticketTypeId: selectedTicketType.id || '',
      quantity,
      paymentMethod,
      totalAmount,
    });
  };

  const handleClose = () => {
    // Reset form on close
    setSelectedTicketTypeId('');
    setQuantity(1);
    setPaymentMethod('paypal');
    onClose();
  };

  const isFormValid = selectedTicketTypeId && quantity > 0 && quantity <= available;
  // Currently only 1 ticket per customer is supported
  const canPurchaseQuantity = quantity === 1;

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Group gap="sm">
          <IconTicket size={24} style={{ color: '#880124' }} />
          <Title order={3}>Purchase Tickets</Title>
        </Group>
      }
      size="lg"
      centered
    >
      <Stack gap="lg">
        {/* Event Info */}
        <Card p="md" style={{ background: 'rgba(136, 1, 36, 0.05)', border: '1px solid rgba(136, 1, 36, 0.1)' }}>
          <Title order={4} mb="xs">{event.title}</Title>
          <Text size="sm" c="dimmed">
            {new Date(event.startDate).toLocaleDateString('en-US', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
              hour: 'numeric',
              minute: '2-digit'
            ,
      timeZone: eventTimeZone})}
          </Text>
        </Card>

        {/* Ticket Selection */}
        <Stack gap="md">
          <Title order={5}>Select Ticket Type</Title>
          
          {ticketTypes.map((ticket) => {
            const ticketAvailable = (ticket.quantityAvailable ?? 0) - (ticket.quantitySold ?? 0);
            const isTicketSoldOut = ticketAvailable <= 0;
            const isNotPurchasable = ticket.canPurchase === false;
            const isDisabled = isTicketSoldOut || isNotPurchasable;
            const ticketPrice = ticket.price ?? 0;

            return (
              <Card
                key={ticket.id}
                p="md"
                style={{
                  cursor: isDisabled ? 'not-allowed' : 'pointer',
                  border: selectedTicketTypeId === ticket.id
                    ? '2px solid #880124'
                    : '1px solid rgba(136, 1, 36, 0.1)',
                  background: isDisabled
                    ? 'rgba(0,0,0,0.05)'
                    : selectedTicketTypeId === ticket.id
                      ? 'rgba(136, 1, 36, 0.05)'
                      : 'white',
                  opacity: isDisabled ? 0.6 : 1,
                }}
                onClick={() => !isDisabled && setSelectedTicketTypeId(ticket.id || '')}
              >
                <Group justify="space-between" align="flex-start">
                  <Stack gap="xs" style={{ flex: 1 }}>
                    <Group gap="sm">
                      <Radio
                        checked={selectedTicketTypeId === ticket.id}
                        onChange={() => !isDisabled && setSelectedTicketTypeId(ticket.id || '')}
                        disabled={isDisabled}
                      />
                      <Text fw={600} size="lg">{ticket.name}</Text>
                      {isTicketSoldOut && (
                        <Badge color="red" size="sm" variant="light">Sold Out</Badge>
                      )}
                      {isNotPurchasable && !isTicketSoldOut && (
                        <Badge color="gray" size="sm" variant="light">Not Available</Badge>
                      )}
                    </Group>

                    {/* Show availability message for non-purchasable tickets */}
                    {isNotPurchasable && ticket.availabilityMessage && (
                      <Text size="sm" c="orange" ml="30px" fw={500}>
                        {ticket.availabilityMessage}
                      </Text>
                    )}

                    {/* Show reference session info if available */}
                    {ticket.referenceSessionName && (
                      <Text size="xs" c="dimmed" ml="30px">
                        For: {ticket.referenceSessionName}
                      </Text>
                    )}

                    <Group gap="sm" ml="30px">
                      <Text size="xs" c="dimmed">
                        Sessions: {(ticket.sessionIdentifiers || []).join(', ')}
                      </Text>
                      {!isNotPurchasable && (
                        <Text size="xs" c="dimmed">
                          {ticketAvailable} available
                        </Text>
                      )}
                    </Group>
                  </Stack>

                  <Box ta="right">
                    <Text fw={600} size="xl" style={{ color: '#880124' }}>
                      ${ticketPrice.toFixed(2)}
                    </Text>
                  </Box>
                </Group>
              </Card>
            );
          })}
        </Stack>

        {/* Quantity Selection */}
        {selectedTicketType && (
          <Stack gap="md">
            <Divider />
            
            <Group justify="space-between" align="center">
              <Box>
                <Text fw={600} mb="xs">Quantity</Text>
                <NumberInput
                  value={quantity}
                  onChange={(value) => setQuantity(Number(value) || 1)}
                  min={1}
                  max={Math.min(available, 1)}
                  style={{ width: '120px' }}
                  disabled={quantity === 1}
                />
                <Text size="xs" c="dimmed" mt="xs">
                  Limit 1 per person
                </Text>
              </Box>
              
              <Box ta="right">
                <Text size="sm" c="dimmed">Total Amount</Text>
                <Text fw={700} size="xl" style={{ color: '#880124' }}>
                  ${totalAmount.toFixed(2)}
                </Text>
              </Box>
            </Group>
          </Stack>
        )}

        {/* Payment Method */}
        {selectedTicketType && (
          <Stack gap="md">
            <Divider />

            <Box>
              <Group gap="sm" mb="md">
                <IconCreditCard size={20} />
                <Text fw={600}>Payment Method</Text>
              </Group>

              <Radio.Group
                value={paymentMethod}
                onChange={(value) => setPaymentMethod(value as 'paypal' | 'venmo')}
              >
                <Stack gap="sm">
                  <Radio value="paypal" label="PayPal" />
                  <Radio value="venmo" label="Venmo (Coming Soon)" disabled />
                </Stack>
              </Radio.Group>

              <Alert
                icon={<IconAlertCircle />}
                color="blue"
                variant="light"
                mt="md"
              >
                <Text size="sm">
                  You'll be redirected to PayPal to complete your payment securely.
                  Your ticket confirmation will be sent via email once payment is processed.
                </Text>
              </Alert>
            </Box>
          </Stack>
        )}

        {/* PayPal Payment Integration */}
        {selectedTicketType && paymentMethod === 'paypal' && (
          <Stack gap="md">
            <Divider />

            <Box>
              <Text fw={600} mb="md">Complete Payment</Text>
              {/* PayPal integration will be added here */}
              <Alert color="blue" variant="light">
                <Text size="sm">
                  PayPal payment integration will be implemented here.
                  For now, use the Purchase button below to simulate the payment flow.
                </Text>
              </Alert>
            </Box>
          </Stack>
        )}

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="lg">
          <Button
            variant="subtle"
            onClick={handleClose}
            disabled={isPurchasing}
          >
            Cancel
          </Button>

          <Button
            onClick={handlePurchase}
            loading={isPurchasing}
            disabled={!isFormValid || !canPurchaseQuantity}
            leftSection={<IconTicket size={20} />}
            styles={{
              root: {
                background: '#880124',
                color: 'white',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            Purchase {quantity > 1 ? `${quantity} Tickets` : 'Ticket'} - ${totalAmount.toFixed(2)}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};