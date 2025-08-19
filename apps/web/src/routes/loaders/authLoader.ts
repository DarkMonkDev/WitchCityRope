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
    
    // Attempt to get current session from server
    const response = await fetch('/api/auth/me', {
      credentials: 'include',
      headers: {
        'Accept': 'application/json'
      }
    });
    
    if (response.ok) {
      const userData = await response.json();
      // Update auth store with valid user data
      actions.login(userData);
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