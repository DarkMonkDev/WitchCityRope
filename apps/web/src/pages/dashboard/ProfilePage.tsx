import React from 'react';
import { Box, Title, Text } from '@mantine/core';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';

/**
 * Profile Page - Placeholder for profile management
 */
export const ProfilePage: React.FC = () => {
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
          Profile
        </Title>
        
        <Text size="lg" c="dimmed">
          Profile management page coming soon...
        </Text>
      </Box>
    </DashboardLayout>
  );
};