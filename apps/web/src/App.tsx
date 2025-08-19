import React, { useEffect } from 'react'
import { RouterProvider } from 'react-router-dom'
import { router } from './routes/router'
import { useAuthActions } from './stores/authStore'
import './App.css'

/**
 * Main App Component using React Router v7
 * 
 * Features:
 * - RouterProvider with data router (createBrowserRouter)
 * - Automatic auth check on app initialization
 * - Integration with Zustand auth store
 * - No need for separate AuthProvider context
 */
function App() {
  const { checkAuth } = useAuthActions();

  // Check authentication status on app load
  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  return <RouterProvider router={router} />
}

export default App
// HMR test comment added at Sun Aug 17 04:37:55 PM EDT 2025
