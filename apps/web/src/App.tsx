import React, { useEffect, useRef } from 'react'
import { RouterProvider } from 'react-router-dom'
import { router } from './routes/router'
import { useAuthActions, useAuthStore } from './stores/authStore'
import { useAuthRefresh } from './hooks/useAuthRefresh'
import { useCSRFStore } from './stores/csrfStore'
import { debugLog, debugError } from './utils/debug'
import './App.css'

/**
 * Main App Component using React Router v7
 * 
 * Features:
 * - RouterProvider with data router (createBrowserRouter)
 * - Automatic auth check on app initialization (with safeguards)
 * - Automatic token refresh to prevent logouts
 * - Integration with Zustand auth store
 * - httpOnly cookie-based authentication
 */
function App() {
  const { checkAuth } = useAuthActions();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const csrfStore = useCSRFStore();
  const hasCheckedAuth = useRef(false);

  // Enable automatic token refresh
  useAuthRefresh();

  // Check authentication status on app load (only once)
  useEffect(() => {
    if (!hasCheckedAuth.current) {
      debugLog('🔍 App.tsx: Initial auth check starting...');
      hasCheckedAuth.current = true;
      checkAuth().catch((error) => {
        debugError('🔍 App.tsx: Initial auth check failed:', error);
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty dependency array - auth check should only run once on mount

  // Initialize CSRF protection on app load (before login)
  // CRITICAL: Must run BEFORE user attempts login, not AFTER authentication
  // Backend /api/antiforgery/token endpoint allows anonymous access for this reason
  // Using CSRF store to track initialization state
  useEffect(() => {
    debugLog('🔍 App.tsx: Initializing CSRF protection via store...');
    csrfStore.initialize();
    // Don't add catch here - store handles errors internally
  }, [csrfStore]); // Empty dependency array - initialize once on mount

  // Refresh CSRF token when authentication status changes
  useEffect(() => {
    if (isAuthenticated) {
      debugLog('🔍 App.tsx: User authenticated, refreshing CSRF protection...');
      csrfStore.initialize();
    }
  }, [isAuthenticated, csrfStore]); // Run when authentication status changes

  return <RouterProvider router={router} />
}

export default App
// HMR test comment added at Sun Aug 17 04:37:55 PM EDT 2025
