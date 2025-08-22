import React from 'react';
import { Box, Title, Text } from '@mantine/core';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';

/**
 * Membership Page - Placeholder for membership management
 */
export const MembershipPage: React.FC = () => {
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
          Membership
        </Title>
        
        <Text size="lg" c="dimmed">
          Membership management page coming soon...
        </Text>
      </Box>
    </DashboardLayout>
  );
};