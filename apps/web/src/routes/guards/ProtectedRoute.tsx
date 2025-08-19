import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../stores/authStore';
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
  const { user, isAuthenticated, isLoading } = useAuth();
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
  if (requiredRole && !user.roles.includes(requiredRole)) {
    if (fallback) {
      return <>{fallback}</>;
    }
    
    return (
      <Box p="xl" style={{ textAlign: 'center' }}>
        <Text size="xl" c="red" mb="md">
          Access Denied
        </Text>
        <Text c="dimmed">
          You don't have permission to access this page.
        </Text>
        <Text c="dimmed" size="sm" mt="md">
          Required role: {requiredRole}
        </Text>
      </Box>
    );
  }

  // All checks passed - render children
  return <>{children}</>;
};