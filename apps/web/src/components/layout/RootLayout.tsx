import { useEffect } from 'react';
import { Outlet, useLocation, ScrollRestoration } from 'react-router-dom';
import { Box } from '@mantine/core';
import { Navigation } from './Navigation';
import { UtilityBar } from './UtilityBar';
import { Footer } from './Footer';

/**
 * ScrollToTop Component
 *
 * Forces scroll to top (Y=0) on every route change.
 * Required because React Router v7's ScrollRestoration has internal logic
 * that can partially restore scroll position even with preventScrollReset={false}.
 *
 * This explicit override ensures consistent scroll-to-top behavior for all
 * navigation (Link clicks, navigate() calls, AND browser back/forward).
 *
 * NOTE: This component scrolls to top on EVERY pathname change, including
 * browser back/forward navigation. This is intentional for our use case
 * where we want consistent "scroll to top" behavior across all navigation types.
 *
 * See: /tests/playwright/scroll-restoration.spec.ts for test coverage
 */
function ScrollToTop() {
  const { pathname } = useLocation();

  useEffect(() => {
    // Scroll to top on pathname change
    // Use requestAnimationFrame to wait for browser paint cycle
    // Then use instant scroll behavior to override any smooth scrolling
    // This ensures we override React Router's ScrollRestoration component
    requestAnimationFrame(() => {
      window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    });
  }, [pathname]);

  return null;
}

/**
 * Root Layout Component for React Router v7
 *
 * Provides the main application shell with:
 * - ScrollToTop component for manual scroll-to-top on every navigation
 * - ScrollRestoration component for React Router's scroll management
 * - UtilityBar at the top
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Footer (accordion mobile-first design) - hidden on admin pages
 * - Matches the exact wireframe design structure
 *
 * SCROLL RESTORATION STRATEGY:
 * 1. ScrollToTop runs first - ensures Y=0 on every navigation
 * 2. ScrollRestoration runs second - handles React Router internals
 * 3. This combination fixes reverse navigation bug (Events → Homepage)
 *    where ScrollRestoration alone would scroll to 246px instead of 0px
 *
 * NOTE: Both components will scroll to top on ALL navigation, including
 * browser back/forward. This is the intended behavior for this application.
 */
export const RootLayout: React.FC = () => {
  const location = useLocation();

  // Hide footer on admin pages
  const isAdminPage = location.pathname.startsWith('/admin');

  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)', display: 'flex', flexDirection: 'column' }}>
      {/* Manual scroll-to-top on every navigation
          CRITICAL: This component MUST come BEFORE ScrollRestoration
          to ensure scroll happens before React Router processes the navigation.

          Without this, reverse navigation (Events → Homepage) scrolls to 246px
          instead of 0px due to React Router v7's internal scroll restoration logic.
      */}
      <ScrollToTop />

      {/* React Router v7 scroll restoration
          Handles React Router's internal scroll management.
          Combined with ScrollToTop above, ensures reliable scroll-to-top behavior.
      */}
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