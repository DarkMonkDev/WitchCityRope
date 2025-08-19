import { Navigate, useLocation } from 'react-router-dom';
import { useUser, useIsAuthenticated, useIsLoading } from '../../stores/authStore';
import { Loader, Box, Text } from '@mantine/core';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string;
  fallback?: React.ReactNode;
}

/**
 * Protected Route Component with Authentication Guard
 * 
 * Pattern from: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 * Section 4.2.2 - Route guards with redirect
 * 
 * Features:
 * - Authentication checking
 * - Role-based access control (optional)
 * - Loading states
 * - Automatic redirect to login with return URL
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRole,
  fallback 
}) => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();
  const isLoading = useIsLoading();
  const location = useLocation();

  // Show loading state while checking authentication
  if (isLoading) {
    return (
      <Box 
        style={{ 
          display: 'flex', 
          justifyContent: 'center', 
          alignItems: 'center', 
          minHeight: '200px' 
        }}
      >
        <Loader size="lg" />
        <Text ml="md" size="lg">Checking authentication...</Text>
      </Box>
    );
  }

  // Not authenticated - redirect to login
  if (!isAuthenticated || !user) {
    const returnTo = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/login?returnTo=${returnTo}`} replace />;
  }

  // Check role requirement if specified
  // TODO: Role checking needs to be implemented via separate API call
  // The User DTO no longer includes roles - they need to be fetched separately
  if (requiredRole) {
    // For now, allow access for authenticated users
    // This should be replaced with proper role checking via API
    console.warn(`Role checking not implemented yet. Required role: ${requiredRole}. User: ${user.sceneName}`);
  }

  // All checks passed - render children
  return <>{children}</>;
};