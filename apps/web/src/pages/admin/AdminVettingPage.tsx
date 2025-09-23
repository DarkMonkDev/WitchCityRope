import React from 'react';
import { Container, Title, Text, Group, Button } from '@mantine/core';
import { IconAlertTriangle, IconUserPlus } from '@tabler/icons-react';
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

        {/* Action buttons aligned with title */}
        <Group gap="md">
          <Button
            leftSection={<IconAlertTriangle size={16} />}
            variant="filled"
            color="orange"
            size="md"
            styles={{
              root: {
                backgroundColor: '#DAA520',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            SEND REMINDER
          </Button>
          <Button
            leftSection={<IconUserPlus size={16} />}
            variant="filled"
            color="gray"
            size="md"
            styles={{
              root: {
                backgroundColor: '#8B8680',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                height: '44px',
                paddingTop: '12px',
                paddingBottom: '12px',
                fontSize: '14px',
                lineHeight: '1.2'
              }
            }}
          >
            CHANGE TO ON HOLD
          </Button>
        </Group>
      </Group>

      {/* Applications List */}
      <VettingApplicationsList />
    </Container>
  );
};