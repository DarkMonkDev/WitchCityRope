import React, { useState } from 'react';
import {
  Modal,
  Title,
  Text,
  Stack,
  Group,
  Button,
  Card,
  Badge,
  Select,
  NumberInput,
  Divider,
  Radio,
  Alert,
  Box,
} from '@mantine/core';
import { IconTicket, IconCreditCard, IconAlertCircle } from '@tabler/icons-react';

export interface TicketPurchaseData {
  eventId: string;
  ticketTypeId: string;
  quantity: number;
  paymentMethod: 'paypal' | 'venmo';
  totalAmount: number;
}

interface EventTicketType {
  id: string;
  name: string;
  description: string;
  price: number;
  sessionsIncluded: string[];
  quantityAvailable: number;
  quantitySold: number;
  allowMultiplePurchase: boolean;
  isEarlyBird: boolean;
  earlyBirdDiscount?: number;
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
  const [selectedTicketTypeId, setSelectedTicketTypeId] = useState<string>('');
  const [quantity, setQuantity] = useState(1);
  const [paymentMethod, setPaymentMethod] = useState<'paypal' | 'venmo'>('paypal');

  const selectedTicketType = ticketTypes.find(t => t.id === selectedTicketTypeId);
  const available = selectedTicketType 
    ? selectedTicketType.quantityAvailable - selectedTicketType.quantitySold 
    : 0;
  
  const effectivePrice = selectedTicketType && selectedTicketType.isEarlyBird && selectedTicketType.earlyBirdDiscount
    ? selectedTicketType.price * (1 - selectedTicketType.earlyBirdDiscount / 100)
    : selectedTicketType?.price || 0;
  
  const totalAmount = effectivePrice * quantity;

  const handlePurchase = () => {
    if (!selectedTicketType) return;

    onPurchase({
      eventId: event.id,
      ticketTypeId: selectedTicketType.id,
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
  const canPurchaseQuantity = selectedTicketType?.allowMultiplePurchase || quantity === 1;

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
            })}
          </Text>
        </Card>

        {/* Ticket Selection */}
        <Stack gap="md">
          <Title order={5}>Select Ticket Type</Title>
          
          {ticketTypes.map((ticket) => {
            const ticketAvailable = ticket.quantityAvailable - ticket.quantitySold;
            const isTicketSoldOut = ticketAvailable <= 0;
            const ticketEffectivePrice = ticket.isEarlyBird && ticket.earlyBirdDiscount
              ? ticket.price * (1 - ticket.earlyBirdDiscount / 100)
              : ticket.price;

            return (
              <Card
                key={ticket.id}
                p="md"
                style={{
                  cursor: isTicketSoldOut ? 'not-allowed' : 'pointer',
                  border: selectedTicketTypeId === ticket.id 
                    ? '2px solid #880124' 
                    : '1px solid rgba(136, 1, 36, 0.1)',
                  background: isTicketSoldOut 
                    ? 'rgba(0,0,0,0.05)' 
                    : selectedTicketTypeId === ticket.id 
                      ? 'rgba(136, 1, 36, 0.05)' 
                      : 'white',
                  opacity: isTicketSoldOut ? 0.6 : 1,
                }}
                onClick={() => !isTicketSoldOut && setSelectedTicketTypeId(ticket.id)}
              >
                <Group justify="space-between" align="flex-start">
                  <Stack gap="xs" style={{ flex: 1 }}>
                    <Group gap="sm">
                      <Radio
                        checked={selectedTicketTypeId === ticket.id}
                        onChange={() => !isTicketSoldOut && setSelectedTicketTypeId(ticket.id)}
                        disabled={isTicketSoldOut}
                      />
                      <Text fw={600} size="lg">{ticket.name}</Text>
                      {ticket.isEarlyBird && (
                        <Badge color="green" size="sm" variant="light">Early Bird</Badge>
                      )}
                      {isTicketSoldOut && (
                        <Badge color="red" size="sm" variant="light">Sold Out</Badge>
                      )}
                    </Group>
                    
                    <Text size="sm" c="dimmed" ml="30px">
                      {ticket.description}
                    </Text>
                    
                    <Group gap="sm" ml="30px">
                      <Text size="xs" c="dimmed">
                        Sessions: {ticket.sessionsIncluded.join(', ')}
                      </Text>
                      <Text size="xs" c="dimmed">
                        {ticketAvailable} available
                      </Text>
                    </Group>
                  </Stack>
                  
                  <Box ta="right">
                    {ticket.isEarlyBird && ticket.earlyBirdDiscount ? (
                      <Stack gap={2} align="flex-end">
                        <Text size="sm" c="dimmed" td="line-through">
                          ${ticket.price.toFixed(2)}
                        </Text>
                        <Text fw={600} size="xl" c="green">
                          ${ticketEffectivePrice.toFixed(2)}
                        </Text>
                        <Text size="xs" c="green">
                          Save {ticket.earlyBirdDiscount}%
                        </Text>
                      </Stack>
                    ) : (
                      <Text fw={600} size="xl" style={{ color: '#880124' }}>
                        ${ticketEffectivePrice.toFixed(2)}
                      </Text>
                    )}
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
                  max={Math.min(available, selectedTicketType.allowMultiplePurchase ? 10 : 1)}
                  style={{ width: '120px' }}
                  disabled={!selectedTicketType.allowMultiplePurchase && quantity === 1}
                />
                {!selectedTicketType.allowMultiplePurchase && (
                  <Text size="xs" c="dimmed" mt="xs">
                    Limit 1 per person
                  </Text>
                )}
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
                  <Radio value="venmo" label="Venmo" />
                </Stack>
              </Radio.Group>
              
              <Alert 
                icon={<IconAlertCircle size={16} />}
                color="blue" 
                variant="light" 
                mt="md"
              >
                <Text size="sm">
                  You'll be redirected to {paymentMethod === 'paypal' ? 'PayPal' : 'Venmo'} to complete your payment.
                  Your ticket confirmation will be sent via email once payment is processed.
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
            style={{
              background: '#880124',
              color: 'white',
            }}
            leftSection={<IconTicket size={20} />}
          >
            Purchase {quantity > 1 ? `${quantity} Tickets` : 'Ticket'} - ${totalAmount.toFixed(2)}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};