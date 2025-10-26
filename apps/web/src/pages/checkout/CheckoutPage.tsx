import React, { useEffect } from 'react';
import { useParams, Navigate } from 'react-router-dom';
import { Container, LoadingOverlay, Text } from '@mantine/core';

interface CheckoutPageProps {}

/**
 * Legacy CheckoutPage - Redirects to new EventPaymentPage
 * @deprecated Use EventPaymentPage instead
 */
export const CheckoutPage: React.FC<CheckoutPageProps> = () => {
  const { eventId } = useParams<{ eventId: string }>();

  // Log redirect for debugging
  useEffect(() => {
    console.log('CheckoutPage (deprecated): Redirecting to EventPaymentPage for eventId:', eventId);
  }, [eventId]);

  // Redirect to new EventPaymentPage with generated registrationId
  if (!eventId) {
    return (
      <Container size="md" py="xl">
        <Text c="red" ta="center">
          Error: Event ID is required
        </Text>
      </Container>
    );
  }

  // Generate a registration ID for the new flow
  const registrationId = `reg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  const redirectPath = `/checkout/${eventId}/${registrationId}`;

  console.log('CheckoutPage: Redirecting to:', redirectPath);

  return <Navigate to={redirectPath} replace />;
};