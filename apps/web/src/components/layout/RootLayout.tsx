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
 *
 * ORIGINAL IMPLEMENTATION (REVERTED):
 * - Uses requestAnimationFrame to wait for browser paint cycle
 * - Then uses instant scroll behavior to override any smooth scrolling
 * - This ensures we override React Router's ScrollRestoration component
 * - This version was working on desktop
 *
 * This works in conjunction with ScrollRestoration component below.
 *
 * See: /docs/lessons-learned/react-developer-lessons-learned-2.md (lines 60-170)
 * See: /tests/playwright/scroll-restoration.spec.ts for test coverage
 */
function ScrollToTop() {
  const { pathname } = useLocation();

  useEffect(() => {
    // Scroll to top on pathname change
    // Use double requestAnimationFrame to ensure we're after layout AND paint
    // This is more reliable than single rAF, especially when DevTools is open
    // Chrome throttles rAF differently when DevTools is docked
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
      });
    });
  }, [pathname]);

  return null;
}

/**
 * Root Layout Component for React Router v7
 *
 * Provides the main application shell with:
 * - ScrollToTop component (requestAnimationFrame) + ScrollRestoration with getKey for scroll-to-top
 * - UtilityBar at the top
 * - Navigation header
 * - Main content area with Outlet for route rendering
 * - Footer (accordion mobile-first design) - hidden on admin pages
 * - Matches the exact wireframe design structure
 *
 * SCROLL RESTORATION STRATEGY (DUAL SYSTEM):
 * Uses BOTH ScrollToTop (requestAnimationFrame) + ScrollRestoration (with getKey) to ensure
 * scroll-to-top works correctly.
 *
 * Why both are needed:
 * - ScrollRestoration handles React Router's internal state
 * - ScrollToTop (requestAnimationFrame) overrides any position restoration
 * - requestAnimationFrame waits for browser paint cycle before scrolling
 * - REVERTED to original implementation that was working on desktop
 *
 * See: /docs/lessons-learned/react-developer-lessons-learned-2.md (lines 60-170)
 */
export const RootLayout: React.FC = () => {
  const location = useLocation();

  // Hide footer on admin pages
  const isAdminPage = location.pathname.startsWith('/admin');

  return (
    <Box style={{ minHeight: '100vh', background: 'var(--color-cream)', display: 'flex', flexDirection: 'column' }}>
      {/* Dual scroll-to-top system (REVERTED to original implementation)

          1. ScrollToTop component (requestAnimationFrame):
             - Waits for browser paint cycle before scrolling
             - Uses behavior: 'instant' to override smooth scrolling
             - Ensures scroll-to-top overrides React Router's ScrollRestoration

          2. ScrollRestoration with getKey:
             - Handles React Router's internal state management
             - getKey returns pathname to force scroll-to-top on route changes
             - Without this, navigation can scroll to wrong positions

          This original implementation was working on desktop. Mobile may need
          separate fix after confirming desktop functionality.
      */}
      <ScrollToTop />

      <ScrollRestoration
        getKey={(location) => {
          // Always return pathname to force scroll-to-top on route changes
          // This prevents React Router from restoring previous scroll positions
          return location.pathname;
        }}
      />

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