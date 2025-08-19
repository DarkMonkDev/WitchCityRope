import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

/**
 * Authentication loader for protected routes
 * Validates user authentication and redirects if necessary
 * 
 * Pattern from: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 * Section 4.2.1 - Loader-based authentication
 */
export async function authLoader({ request }: LoaderFunctionArgs) {
  // Get current auth state from Zustand store
  const { isAuthenticated, user, actions } = useAuthStore.getState();
  
  // If user is authenticated, allow access
  if (isAuthenticated && user) {
    return { user };
  }
  
  // Check if we can validate auth with the server
  try {
    // Set loading state
    actions.setLoading(true);
    
    // Get JWT token for authentication
    const token = actions.getToken();
    if (!token) {
      // No valid JWT token available
      throw new Error('No valid token');
    }
    
    // Attempt to get current session from server
    const response = await fetch('/api/auth/user', {
      credentials: 'include',
      headers: {
        'Accept': 'application/json',
        'Authorization': `Bearer ${token}`
      }
    });
    
    if (response.ok) {
      const apiResponse = await response.json();
      const userData = apiResponse.data || apiResponse;
      
      // User is authenticated via JWT token - update store with current user data
      // Keep existing token since it's valid
      const currentState = useAuthStore.getState();
      
      actions.login(userData, currentState.token!, currentState.tokenExpiresAt!);
      return { user: userData };
    }
  } catch (error) {
    console.error('Auth validation failed:', error);
  } finally {
    actions.setLoading(false);
  }
  
  // User is not authenticated - redirect to login with return URL
  const returnTo = encodeURIComponent(new URL(request.url).pathname);
  throw redirect(`/login?returnTo=${returnTo}`);
}