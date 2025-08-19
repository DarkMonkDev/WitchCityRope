import { Outlet } from 'react-router-dom';
import { AppShell, Box } from '@mantine/core';
import { Navigation } from './Navigation';

/**
 * Root Layout Component for React Router v7
 * 
 * Provides the main application shell with:
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Consistent layout across all routes
 */
export const RootLayout: React.FC = () => {
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