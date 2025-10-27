import { Outlet } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';
import { ScrollToTop } from '../ScrollToTop';

/**
 * Root Layout Component for React Router v7
 *
 * Provides the main application shell with:
 * - ScrollToTop component for automatic scroll restoration on navigation
 * - UtilityBar at the top
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Matches the exact wireframe design structure
 */
export const RootLayout: React.FC = () => {
  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)' }}>
      {/* Scroll to top on route changes */}
      <ScrollToTop />

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