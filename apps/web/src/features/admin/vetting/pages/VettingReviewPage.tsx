import React from 'react';
import { Container, Title, Text, Stack } from '@mantine/core';
import { VettingReviewGrid } from '../components/VettingReviewGrid';

/**
 * Vetting Review Page
 *
 * Main admin page for reviewing and managing vetting applications.
 * Uses the VettingReviewGrid component to display applications.
 */
export const VettingReviewPage: React.FC = () => {
  return (
    <Container size="xl" py="xl">
      <Stack gap="lg">
        {/* Page Header */}
        <Stack gap="xs">
          <Title
            order={1}
            style={{
              fontFamily: 'Montserrat, sans-serif',
              fontSize: '32px',
              fontWeight: 700,
              textTransform: 'uppercase',
              color: '#880124'
            }}
          >
            Vetting Administration
          </Title>
          <Text c="dimmed" size="sm">
            Review and manage member vetting applications
          </Text>
        </Stack>

        {/* Vetting Review Grid */}
        <VettingReviewGrid />
      </Stack>
    </Container>
  );
};
