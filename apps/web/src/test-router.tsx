/**
 * Test Router Implementation - React Router v7 with Protected Routes
 * 
 * This file demonstrates the validated patterns from our research without
 * the existing TanStack Query import conflicts.
 * 
 * To test this implementation:
 * 1. Temporarily replace the import in main.tsx
 * 2. Visit http://localhost:5173
 * 3. Navigate to /dashboard (should redirect to login)
 * 4. Login should work and redirect back to dashboard
 */

import React, { useEffect } from 'react';
import { createBrowserRouter, RouterProvider, Outlet, useRouteError, isRouteErrorResponse, Link } from 'react-router-dom';
import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { AppShell, Box, Group, Button, Text, Title, Paper, Loader } from '@mantine/core';
import { useUser, useIsAuthenticated, useAuthActions } from './stores/authStore';

// Simple HomePage component
const HomePage: React.FC = () => (
  <Box p="md">
    <Title order={1} mb="xl">WitchCityRope</Title>
    <Text size="lg" mb="md">Welcome to our React Router v7 implementation!</Text>
    <Group>
      <Button component={Link} to="/dashboard" variant="filled" color="violet">
        Go to Dashboard (Protected)
      </Button>
      <Button component={Link} to="/login" variant="outline">
        Login
      </Button>
    </Group>
  </Box>
);

// Simple LoginPage component
const LoginPage: React.FC = () => {
  const { login } = useAuthActions();
  
  const handleLogin = () => {
    // Mock login - in real app this would call API
    const mockUser = {
      id: '1',
      email: 'test@example.com',
      sceneName: 'TestUser',
      createdAt: '2025-08-19T10:00:00Z',
      lastLoginAt: '2025-08-19T10:00:00Z'
    };
    const mockToken = 'test-jwt-token';
    const mockExpiresAt = new Date(Date.now() + 60 * 60 * 1000);
    login(mockUser);
    
    // In real app, would redirect based on returnTo parameter
    window.location.href = '/dashboard';
  };

  return (
    <Box p="md" style={{ maxWidth: '400px', margin: '0 auto' }}>
      <Title order={1} mb="xl">Login</Title>
      <Paper shadow="sm" p="xl">
        <Text mb="md">Click the button below to simulate login:</Text>
        <Button onClick={handleLogin} fullWidth color="violet">
          Mock Login
        </Button>
      </Paper>
    </Box>
  );
};

// Dashboard component (protected)
const DashboardPage: React.FC = () => {
  const user = useUser();
  const { logout } = useAuthActions();

  const handleLogout = () => {
    logout();
    window.location.href = '/';
  };

  return (
    <Box p="md">
      <Title order={1} mb="xl">Dashboard</Title>
      <Paper shadow="sm" p="xl" mb="lg">
        <Text size="lg" mb="md">
          Welcome, {user?.sceneName}!
        </Text>
        <Text c="dimmed" mb="lg">
          This is a protected route. You can only see this because you're authenticated.
        </Text>
        <Group>
          <Text><strong>Email:</strong> {user?.email}</Text>
          <Text><strong>Scene Name:</strong> {user?.sceneName}</Text>
          <Text><strong>Created:</strong> {user?.createdAt ? new Date(user.createdAt).toLocaleDateString() : 'Unknown'}</Text>
        </Group>
      </Paper>
      <Button onClick={handleLogout} color="red">
        Logout
      </Button>
    </Box>
  );
};

// Navigation component
const Navigation: React.FC = () => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();
  const { logout } = useAuthActions();

  const handleLogout = () => {
    logout();
    window.location.href = '/';
  };

  return (
    <Box
      style={{
        height: '100%',
        padding: '0 1rem',
        display: 'flex',
        alignItems: 'center',
        backgroundColor: 'var(--mantine-color-dark-8)',
        borderBottom: '1px solid var(--mantine-color-dark-4)'
      }}
    >
      <Group justify="space-between" style={{ width: '100%' }}>
        <Group>
          <Text
            component={Link}
            to="/"
            size="xl"
            fw={700}
            c="violet"
            style={{ textDecoration: 'none' }}
          >
            WitchCityRope
          </Text>
        </Group>

        <Group>
          {isAuthenticated && user ? (
            <>
              <Text component={Link} to="/dashboard" c="dimmed" style={{ textDecoration: 'none' }}>
                Dashboard
              </Text>
              <Text c="dimmed">Welcome, {user.sceneName}</Text>
              <Button
                variant="filled"
                color="red"
                size="sm"
                onClick={handleLogout}
              >
                Logout
              </Button>
            </>
          ) : (
            <>
              <Text component={Link} to="/login" c="dimmed" style={{ textDecoration: 'none' }}>
                Login
              </Text>
            </>
          )}
        </Group>
      </Group>
    </Box>
  );
};

// Root layout
const RootLayout: React.FC = () => {
  return (
    <AppShell
      header={{ height: 60 }}
      padding="md"
      style={{ minHeight: '100vh' }}
    >
      <AppShell.Header>
        <Navigation />
      </AppShell.Header>
      <AppShell.Main>
        <Box style={{ maxWidth: '1200px', margin: '0 auto' }}>
          <Outlet />
        </Box>
      </AppShell.Main>
    </AppShell>
  );
};

// Error boundary
const RootErrorBoundary: React.FC = () => {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Title order={1} c="red" mb="md">
          {error.status} {error.statusText}
        </Title>
        <Text size="lg" mb="xl">
          {error.data || 'Something went wrong'}
        </Text>
        <Group justify="center">
          <Button component={Link} to="/" color="violet">
            Return Home
          </Button>
        </Group>
      </Box>
    );
  }

  return (
    <Box p="xl" style={{ textAlign: 'center' }}>
      <Title order={1} c="red" mb="md">Something went wrong</Title>
      <Text size="lg" mb="xl">
        {error instanceof Error ? error.message : 'Unknown error'}
      </Text>
      <Group justify="center">
        <Button component={Link} to="/" color="violet">
          Return Home
        </Button>
        <Button onClick={() => window.location.reload()}>
          Reload
        </Button>
      </Group>
    </Box>
  );
};

// Auth loader function (React Router v7 pattern)
async function authLoader({ request }: LoaderFunctionArgs) {
  // Get current auth state from Zustand store
  const { isAuthenticated, user, actions } = useAuthStore.getState();
  
  // If user is authenticated, allow access
  if (isAuthenticated && user) {
    return { user };
  }

  // User is not authenticated - redirect to login with return URL
  const returnTo = encodeURIComponent(new URL(request.url).pathname);
  throw redirect(`/login?returnTo=${returnTo}`);
}

// Import the auth store after it's used in the loader
import { useAuthStore } from './stores/authStore';

// Router configuration (React Router v7 pattern)
export const testRouter = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // Public routes
      { 
        index: true, 
        element: <HomePage /> 
      },
      { 
        path: "login", 
        element: <LoginPage /> 
      },
      
      // Protected routes using loader
      {
        path: "dashboard",
        element: <DashboardPage />,
        loader: authLoader
      }
    ]
  }
]);

// Test App component
const TestApp: React.FC = () => {
  const { checkAuth } = useAuthActions();

  // Check authentication status on app load
  useEffect(() => {
    // In a real app, this would call the API
    // For this test, we'll just set loading to false
    checkAuth().catch(() => {
      // Ignore errors in test mode
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty dependency array - auth check should only run once on mount

  return <RouterProvider router={testRouter} />;
};

export default TestApp;