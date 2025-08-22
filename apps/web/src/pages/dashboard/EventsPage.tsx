import React from 'react';
import { Box, Title, Text } from '@mantine/core';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';

/**
 * Events Page - Placeholder for events management
 */
export const EventsPage: React.FC = () => {
  return (
    <DashboardLayout>
      <Box>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Events
        </Title>
        
        <Text size="lg" c="dimmed">
          Events management page coming soon...
        </Text>
      </Box>
    </DashboardLayout>
  );
};