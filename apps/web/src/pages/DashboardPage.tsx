import { useLoaderData } from 'react-router-dom';
import { Box, Title, Text, Paper, Group, Button } from '@mantine/core';
import { useAuth, useAuthActions } from '../stores/authStore';
import type { User } from '../stores/authStore';

/**
 * Dashboard Page - Protected route example
 * 
 * This page is only accessible to authenticated users
 * Uses React Router v7 loader data for initial user data
 * Also demonstrates integration with Zustand auth store
 */
export const DashboardPage: React.FC = () => {
  const loaderData = useLoaderData() as { user: User };
  const { user, isAuthenticated } = useAuth();
  const { logout } = useAuthActions();

  // Use the most current user data (store may be more up-to-date than loader)
  const currentUser = user || loaderData.user;

  const handleLogout = async () => {
    try {
      logout();
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  return (
    <Box p="md">
      <Title order={1} mb="xl">
        Dashboard
      </Title>

      <Paper shadow="sm" p="xl" mb="lg">
        <Title order={2} mb="md">
          Welcome back!
        </Title>
        
        <Text size="lg" mb="md">
          Hello, {currentUser?.sceneName || currentUser?.firstName || 'User'}!
        </Text>
        
        <Text c="dimmed" mb="lg">
          You are successfully authenticated and viewing a protected page.
        </Text>

        <Group>
          <Text><strong>Email:</strong> {currentUser?.email}</Text>
          <Text><strong>User ID:</strong> {currentUser?.id}</Text>
          <Text><strong>Roles:</strong> {currentUser?.roles?.join(', ') || 'None'}</Text>
        </Group>
      </Paper>

      <Paper shadow="sm" p="xl" mb="lg">
        <Title order={3} mb="md">
          Authentication Status
        </Title>
        
        <Group>
          <Text>
            <strong>Authenticated:</strong> {isAuthenticated ? 'Yes' : 'No'}
          </Text>
          <Text>
            <strong>Source:</strong> {user ? 'Zustand Store' : 'Router Loader'}
          </Text>
        </Group>
      </Paper>

      <Paper shadow="sm" p="xl">
        <Title order={3} mb="md">
          Quick Actions
        </Title>
        
        <Group>
          <Button 
            variant="outline" 
            color="blue"
            onClick={() => window.location.reload()}
          >
            Refresh Page
          </Button>
          
          <Button 
            variant="filled" 
            color="red"
            onClick={handleLogout}
          >
            Logout
          </Button>
        </Group>
      </Paper>
    </Box>
  );
};