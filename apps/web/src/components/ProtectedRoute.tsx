import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useIsAuthenticated, useIsLoading } from '../stores/authStore'

interface ProtectedRouteProps {
  children: React.ReactNode
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const isAuthenticated = useIsAuthenticated();
  const isLoading = useIsLoading();
  const location = useLocation()

  // Show loading while checking authentication status
  if (isLoading) {
    return (
      <div
        className="min-h-screen bg-slate-900 flex items-center justify-center"
        data-testid="auth-loading"
      >
        <div className="text-white">Checking authentication...</div>
      </div>
    )
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}
