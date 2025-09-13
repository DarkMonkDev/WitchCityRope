import React, { useEffect, useRef } from 'react'
import { RouterProvider } from 'react-router-dom'
import { router } from './routes/router'
import { useAuthActions } from './stores/authStore'
import { useAuthRefresh } from './hooks/useAuthRefresh'
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
  const hasCheckedAuth = useRef(false);

  // Enable automatic token refresh
  useAuthRefresh();

  // Check authentication status on app load (only once)
  useEffect(() => {
    if (!hasCheckedAuth.current) {
      console.log('🔍 App.tsx: Initial auth check starting...');
      hasCheckedAuth.current = true;
      checkAuth().catch((error) => {
        console.error('🔍 App.tsx: Initial auth check failed:', error);
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Empty dependency array - auth check should only run once on mount

  return <RouterProvider router={router} />
}

export default App
// HMR test comment added at Sun Aug 17 04:37:55 PM EDT 2025
