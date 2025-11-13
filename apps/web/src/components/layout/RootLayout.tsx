import { Outlet, useLocation, ScrollRestoration } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';
import { Footer } from './Footer';

/**
 * Root Layout Component for React Router v7
 *
 * Provides the main application shell with:
 * - ScrollRestoration component for automatic scroll management on navigation
 *   (scrolls to top on new navigation, restores scroll position on back/forward)
 * - UtilityBar at the top
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Footer (accordion mobile-first design) - hidden on admin pages
 * - Matches the exact wireframe design structure
 */
export const RootLayout: React.FC = () => {
  const location = useLocation();

  // Hide footer on admin pages
  const isAdminPage = location.pathname.startsWith('/admin');

  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)', display: 'flex', flexDirection: 'column' }}>
      {/* React Router v7 scroll restoration - handles scroll to top on navigation
          and restores scroll position on browser back/forward */}
      <ScrollRestoration />

      {/* Utility Bar */}
      <UtilityBar />

      {/* Main Navigation */}
      <Navigation />

      {/* Main Content */}
      <Box component="main" style={{ flex: 1 }}>
        <Outlet />
      </Box>

      {/* Footer - hidden on admin pages */}
      {!isAdminPage && <Footer />}
    </Box>
  );
};