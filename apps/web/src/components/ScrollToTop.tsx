import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

/**
 * ScrollToTop Component
 *
 * Scrolls the window to the top whenever the route changes.
 * This prevents the page from loading in the middle when navigating
 * between pages (e.g., clicking event cards to view event details).
 *
 * Usage: Add this component to your layout or router configuration.
 */
export const ScrollToTop = () => {
  const { pathname } = useLocation();

  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);

  return null;
};
