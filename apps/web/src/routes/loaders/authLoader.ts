import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';
import { apiClient } from '../../lib/api/client';

/**
 * Authentication loader for protected routes
 * Validates user authentication and redirects if necessary
 *
 * Updated for httpOnly cookie-based authentication
 * Uses apiClient with skipAutoRedirect to handle returnUrl properly
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
    // skipAutoRedirect: true prevents apiClient from redirecting on 401
    // so we can handle it here with the proper returnUrl
    const response = await apiClient.get('/api/auth/user', {
      skipAutoRedirect: true
    });

    console.log('Auth validation response:', response.status, response.statusText);

    const apiResponse = response.data;
    const userData = apiResponse.data || apiResponse;

    console.log('Server auth validation successful, user:', userData?.email);

    // User is authenticated via httpOnly cookie - update store with current user data
    actions.login(userData);
    return { user: userData };
  } catch (error: any) {
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
  const returnUrl = encodeURIComponent(requestUrl.pathname + requestUrl.search);
  console.log('Redirecting to login with returnUrl:', returnUrl);
  throw redirect(`/login?returnUrl=${returnUrl}`);
}