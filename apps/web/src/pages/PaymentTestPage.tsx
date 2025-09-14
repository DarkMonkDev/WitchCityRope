// Payment System Test Page
// Demonstrates the payment components without full integration

import React, { useState } from 'react';
import {
  Container,
  Stack,
  Group,
  Text,
  Title,
  Button,
  Paper,
  Divider,
  Badge,
  Alert,
  Box
} from '@mantine/core';
import { IconInfoCircle, IconTestPipe } from '@tabler/icons-react';
import { 
  SlidingScaleSelector, 
  CompactSlidingScaleSelector,
  PaymentSummary,
  CompactPaymentSummary
} from '../features/payments';
import type { PaymentEventInfo } from '../features/payments';

/**
 * Test page for payment system components
 */
export const PaymentTestPage: React.FC = () => {
  const [selectedAmount, setSelectedAmount] = useState(40);
  const [discountPercentage, setDiscountPercentage] = useState(0);

  // Mock event data
  const mockEventInfo: PaymentEventInfo = {
    id: 'test-event-123',
    title: 'Shibari Fundamentals Workshop',
    startDateTime: '2025-10-15T14:00:00',
    endDateTime: '2025-10-15T18:00:00',
    instructorName: 'Master Kenji',
    location: 'Salem Community Center',
    basePrice: 40,
    currency: 'USD',
    registrationId: 'test-registration-456'
  };

  // Mock sliding scale calculation
  const mockCalculation = {
    originalAmount: 40,
    discountPercentage: discountPercentage,
    finalAmount: selectedAmount,
    discountAmount: 40 - selectedAmount,
    display: {
      original: '$40.00',
      final: `$${selectedAmount.toFixed(2)}`,
      discount: `$${(40 - selectedAmount).toFixed(2)}`,
      percentage: `${Math.round(discountPercentage)}%`
    }
  };

  const handleAmountChange = (amount: number, percentage: number) => {
    setSelectedAmount(amount);
    setDiscountPercentage(percentage);
  };

  return (
    <Container size="xl" py="xl">
      <Stack gap="xl">
        {/* Header */}
        <Paper p="lg" radius="md" withBorder>
          <Group gap="md">
            <IconTestPipe size={32} color="#880124" />
            <Box>
              <Title order={1} c="#880124">
                Payment System Component Test
              </Title>
              <Text c="dimmed">
                Interactive demonstration of payment system components
              </Text>
            </Box>
            <Badge variant="light" color="blue" size="lg">
              Development Test
            </Badge>
          </Group>
        </Paper>

        {/* Important Notice */}
        <Alert 
          icon={<IconInfoCircle />}
          color="blue"
          variant="light"
        >
          <Text fw={500} mb={4}>Test Environment Notice</Text>
          <Text size="sm">
            This is a development test page for payment system components. 
            No real payments will be processed. PayPal integration uses sandbox 
            environment for testing purposes.
          </Text>
        </Alert>

        {/* Component Demos */}
        <Stack gap="xl">
          {/* Sliding Scale Selector - Full Version */}
          <Paper p="lg" radius="md" withBorder>
            <Stack gap="md">
              <Title order={2} c="#880124">
                1. Sliding Scale Selector (Full)
              </Title>
              <Text c="dimmed" mb="md">
                Main sliding scale interface with dignified messaging and honor-based system
              </Text>
              
              <SlidingScaleSelector
                basePrice={mockEventInfo.basePrice}
                currency={mockEventInfo.currency}
                onAmountChange={handleAmountChange}
                title="Choose Your Payment Amount"
              />

              <Text size="sm" c="dimmed" style={{ fontStyle: 'italic' }}>
                Selected: ${selectedAmount.toFixed(2)} 
                {discountPercentage > 0 && ` (${Math.round(discountPercentage)}% sliding scale)`}
              </Text>
            </Stack>
          </Paper>

          {/* Sliding Scale Selector - Compact Version */}
          <Paper p="lg" radius="md" withBorder>
            <Stack gap="md">
              <Title order={2} c="#880124">
                2. Sliding Scale Selector (Compact)
              </Title>
              <Text c="dimmed" mb="md">
                Compact version for mobile or space-constrained layouts
              </Text>
              
              <Box maw={400}>
                <CompactSlidingScaleSelector
                  basePrice={mockEventInfo.basePrice}
                  currency={mockEventInfo.currency}
                  onAmountChange={handleAmountChange}
                />
              </Box>
            </Stack>
          </Paper>

          <Divider />

          {/* Payment Summary - Full Version */}
          <Paper p="lg" radius="md" withBorder>
            <Stack gap="md">
              <Title order={2} c="#880124">
                3. Payment Summary (Full)
              </Title>
              <Text c="dimmed" mb="md">
                Detailed payment summary with event information and price breakdown
              </Text>
              
              <Box maw={500}>
                <PaymentSummary
                  eventInfo={mockEventInfo}
                  calculation={mockCalculation}
                  detailed={true}
                />
              </Box>
            </Stack>
          </Paper>

          {/* Payment Summary - Compact Version */}
          <Paper p="lg" radius="md" withBorder>
            <Stack gap="md">
              <Title order={2} c="#880124">
                4. Payment Summary (Compact)
              </Title>
              <Text c="dimmed" mb="md">
                Compact summary for mobile checkout or sidebar display
              </Text>
              
              <Box maw={350}>
                <CompactPaymentSummary
                  eventInfo={mockEventInfo}
                  calculation={mockCalculation}
                />
              </Box>
            </Stack>
          </Paper>

          {/* Integration Guide */}
          <Paper p="lg" radius="md" bg="#FAF6F2" withBorder>
            <Stack gap="md">
              <Title order={3} c="#880124">
                Integration Status
              </Title>
              
              <Stack gap="sm">
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">TypeScript types matching backend DTOs</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">Sliding scale calculation logic</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">Dignified sliding scale interface</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">Mantine v7 component integration</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">Mobile responsive design</Text>
                </Group>

                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">PayPal integration with sandbox environment</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="green" variant="light">✓</Badge>
                  <Text size="sm">PayPal button with Venmo support</Text>
                </Group>
                
                <Group gap="xs">
                  <Badge color="orange" variant="light">⚠</Badge>
                  <Text size="sm">Complete payment flow integration (requires backend)</Text>
                </Group>
              </Stack>

              <Text size="sm" c="dimmed" mt="md">
                PayPal integration is ready for testing. Backend payment endpoints 
                need to be implemented to complete the full payment flow.
              </Text>
            </Stack>
          </Paper>

          {/* Test Navigation */}
          <Group justify="center" mt="xl">
            <Button
              variant="light"
              color="wcr"
              onClick={() => window.history.back()}
            >
              Back to Previous Page
            </Button>
            
            <Button
              color="#880124"
              onClick={() => {
                const url = `/events/test-event/payment/test-registration?title=${encodeURIComponent(mockEventInfo.title)}&price=${mockEventInfo.basePrice}&instructor=${encodeURIComponent(mockEventInfo.instructorName!)}&location=${encodeURIComponent(mockEventInfo.location!)}&startDateTime=${mockEventInfo.startDateTime}&endDateTime=${mockEventInfo.endDateTime}`;
                window.open(url, '_blank');
              }}
              styles={(theme) => ({
                root: {
                  background: 'linear-gradient(135deg, #FFB800, #DAA520)',
                  border: 'none',
                  borderRadius: '12px 6px 12px 6px',
                  color: '#2C2C2C',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    borderRadius: '6px 12px 6px 12px',
                    boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)',
                    transform: 'translateY(-1px)'
                  }
                }
              })}
            >
              Open Full Payment Flow
            </Button>
          </Group>
        </Stack>
      </Stack>
    </Container>
  );
};