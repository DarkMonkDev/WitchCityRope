import { Outlet } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';

/**
 * Root Layout Component for React Router v7
 * 
 * Provides the main application shell with:
 * - UtilityBar at the top
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Matches the exact wireframe design structure
 */
export const RootLayout: React.FC = () => {
  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      {/* Utility Bar */}
      <UtilityBar />
      
      {/* Main Navigation */}
      <Navigation />

      {/* Main Content */}
      <Box component="main">
        <Outlet />
      </Box>
    </Box>
  );
};