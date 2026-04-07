import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { apiClient } from '../../lib/api/client';
import { hasAnyRole } from '../../utils/roleUtils';
import { ADMIN_CAPABLE_ROLES } from '../../constants/adminRoles';

/**
 * Admin-specific loader for protected admin routes
 *
 * SECURITY: Validates both authentication AND admin-capable role.
 * Users with any role in ADMIN_CAPABLE_ROLES (Administrator, EventOrganizer,
 * SafetyTeam, VettingTeam) can access admin routes. The AdminDashboardPage
 * then filters which cards each role can see.
 *
 * Uses apiClient with skipAutoRedirect to handle returnUrl properly.
 *
 * Redirects:
 * - Not authenticated → /login with returnUrl (must match LoginPage's searchParams.get('returnUrl'))
 * - Authenticated but no admin-capable role → /unauthorized (403)
 */
export async function adminLoader({ request }: LoaderFunctionArgs) {
  const requestUrl = new URL(request.url);
  console.log('AdminLoader called for:', requestUrl.pathname);

  // Get current auth state from Zustand store
  const { isAuthenticated, user, actions } = useAuthStore.getState();

  console.log('AdminLoader state:', {
    isAuthenticated,
    hasUser: !!user,
    role: user?.role
  });

  // If user is authenticated and already has role data, check immediately
  if (isAuthenticated && user) {
    console.log('User already authenticated, checking role...');

    // Check if user has any admin-capable role
    if (!hasAnyRole(user, ADMIN_CAPABLE_ROLES)) {
      console.warn('Access denied - user lacks admin-capable role:', {
        email: user.email,
        role: user.role,
        path: requestUrl.pathname
      });

      // Redirect to unauthorized page (403)
      throw redirect('/unauthorized');
    }

    console.log('Admin access granted for:', user.email);
    return { user };
  }

  // Check if we can validate auth with the server via httpOnly cookies
  try {
    // Set loading state
    actions.setLoading(true);

    console.log('Attempting server auth validation...');

    // Attempt to get current session from server using httpOnly cookies
    // skipAutoRedirect: true prevents apiClient from redirecting on 401
    // so we can handle it here with the proper returnTo
    const response = await apiClient.get('/api/auth/user', {
      skipAutoRedirect: true
    });

    console.log('Auth validation response:', response.status, response.statusText);

    const apiResponse = response.data;
    const userData = apiResponse.data || apiResponse;

    console.log('Server auth validation successful, user:', userData?.email, 'role:', userData?.role);

    // User is authenticated - check role before granting access
    if (!hasAnyRole(userData, ADMIN_CAPABLE_ROLES)) {
      console.warn('Access denied - authenticated user lacks admin-capable role:', {
        email: userData.email,
        role: userData.role,
        path: requestUrl.pathname
      });

      // Update auth store with user data
      actions.login(userData);

      // Redirect to unauthorized page (403)
      throw redirect('/unauthorized');
    }

    // User is authenticated AND has admin-capable role - update store and allow access
    actions.login(userData);
    console.log('Admin access granted for:', userData.email);
    return { user: userData };
  } catch (error: any) {
    // If it's a redirect, re-throw it
    if (error instanceof Response && error.status === 302) {
      throw error;
    }

    // Check if this is a 401 (expected when not authenticated)
    if (error.response?.status === 401) {
      console.log('User not authenticated (401)');
    } else {
      console.error('Auth validation failed:', error.message);
    }
  } finally {
    actions.setLoading(false);
  }

  // User is not authenticated - redirect to login with return URL
  // Must use 'returnUrl' param (not 'returnTo') to match LoginPage.tsx which reads searchParams.get('returnUrl')
  const returnUrl = encodeURIComponent(requestUrl.pathname + requestUrl.search);
  console.log('Redirecting to login with returnUrl:', returnUrl);
  throw redirect(`/login?returnUrl=${returnUrl}`);
}
