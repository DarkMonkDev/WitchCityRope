import React from 'react';
import { Container, Title, Text, Group } from '@mantine/core';
import { VettingApplicationsList } from '../../features/admin/vetting/components/VettingApplicationsList';

/**
 * Admin Vetting Applications List Page
 *
 * This page shows the list of vetting applications following the wireframe.
 * Row clicks navigate to the detail page at /admin/vetting/applications/:id
 *
 * Route: /admin/vetting
 */
export const AdminVettingPage: React.FC = () => {
  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Group justify="space-between" align="center" mb="xl">
        <div>
          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '32px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Vetting Applications
          </Title>
          <Text size="lg" c="dimmed" mt="xs">
            Review and manage member vetting applications
          </Text>
        </div>
      </Group>

      {/* Applications List */}
      <VettingApplicationsList />
    </Container>
  );
};