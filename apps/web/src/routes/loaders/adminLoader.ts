import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

/**
 * Admin-specific loader for protected admin routes
 *
 * SECURITY: Validates both authentication AND admin role
 * Only users with role "Administrator" can access admin routes
 *
 * Redirects:
 * - Not authenticated → /login with returnTo
 * - Authenticated but not admin → /unauthorized (403)
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

    // Check if user has Administrator role
    if (user.role !== 'Administrator') {
      console.warn('Access denied - user lacks Administrator role:', {
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
    const response = await fetch('/api/auth/user', {
      credentials: 'include' // Use httpOnly cookies for auth
    });

    console.log('Auth validation response:', response.status, response.statusText);

    if (response.ok) {
      const apiResponse = await response.json();
      const userData = apiResponse.data || apiResponse;

      console.log('Server auth validation successful, user:', userData?.email, 'role:', userData?.role);

      // User is authenticated - check role before granting access
      if (userData.role !== 'Administrator') {
        console.warn('Access denied - authenticated user lacks Administrator role:', {
          email: userData.email,
          role: userData.role,
          path: requestUrl.pathname
        });

        // Update auth store with user data
        actions.login(userData);

        // Redirect to unauthorized page (403)
        throw redirect('/unauthorized');
      }

      // User is authenticated AND has admin role - update store and allow access
      actions.login(userData);
      console.log('Admin access granted for:', userData.email);
      return { user: userData };
    } else {
      console.warn('Server auth validation failed:', response.status, response.statusText);
    }
  } catch (error) {
    // If it's a redirect, re-throw it
    if (error instanceof Response && error.status === 302) {
      throw error;
    }

    console.error('Auth validation failed:', error);
  } finally {
    actions.setLoading(false);
  }

  // User is not authenticated - redirect to login with return URL
  const returnTo = encodeURIComponent(requestUrl.pathname);
  console.log('Redirecting to login with returnTo:', returnTo);
  throw redirect(`/login?returnTo=${returnTo}`);
}
