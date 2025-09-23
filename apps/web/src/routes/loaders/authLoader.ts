import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

/**
 * Authentication loader for protected routes
 * Validates user authentication and redirects if necessary
 *
 * Updated for httpOnly cookie-based authentication
 * Enhanced with better error handling and debugging
 */
export async function authLoader({ request }: LoaderFunctionArgs) {
  const requestUrl = new URL(request.url);
  console.log('AuthLoader called for:', requestUrl.pathname);

  // Get current auth state from Zustand store
  const { isAuthenticated, user, actions } = useAuthStore.getState();

  console.log('AuthLoader state:', { isAuthenticated, hasUser: !!user });

  // If user is authenticated, allow access
  if (isAuthenticated && user) {
    console.log('User already authenticated, allowing access');
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

      console.log('Server auth validation successful, user:', userData?.email);

      // User is authenticated via httpOnly cookie - update store with current user data
      actions.login(userData);
      return { user: userData };
    } else {
      console.warn('Server auth validation failed:', response.status, response.statusText);
    }
  } catch (error) {
    console.error('Auth validation failed:', error);
  } finally {
    actions.setLoading(false);
  }

  // User is not authenticated - redirect to login with return URL
  const returnTo = encodeURIComponent(requestUrl.pathname);
  console.log('Redirecting to login with returnTo:', returnTo);
  throw redirect(`/login?returnTo=${returnTo}`);
}