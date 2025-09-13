// Admin Safety Page
// Protected admin page for safety team incident management

import React from 'react';
import { Container, Box, Alert, Text } from '@mantine/core';
import { IconShieldOff } from '@tabler/icons-react';
import { SafetyDashboard } from '../../features/safety/components/SafetyDashboard';
import { useSafetyTeamAccess } from '../../features/safety/hooks/useSafetyIncidents';

export function AdminSafetyPage() {
  const { hasAccess, isLoading, error } = useSafetyTeamAccess();
  
  // Show loading state
  if (isLoading) {
    return (
      <Container size="xl" py="xl">
        <Text ta="center">Loading safety dashboard...</Text>
      </Container>
    );
  }
  
  // Show access denied if user doesn't have safety team permissions
  if (!hasAccess) {
    return (
      <Container size="md" py="xl">
        <Alert 
          variant="light" 
          color="red" 
          icon={<IconShieldOff size={16} />}
          title="Access Denied"
        >
          <Text size="sm">
            You don't have permission to access the safety incident management system. 
            This area is restricted to safety team members only.
          </Text>
          <Text size="sm" mt="xs">
            If you believe you should have access, please contact an administrator.
          </Text>
        </Alert>
      </Container>
    );
  }
  
  return (
    <Container size="xl" py="xl">
      <SafetyDashboard />
    </Container>
  );
}