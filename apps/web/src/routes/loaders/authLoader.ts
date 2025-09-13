import { LoaderFunctionArgs, redirect } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

/**
 * Authentication loader for protected routes
 * Validates user authentication and redirects if necessary
 * 
 * Updated for httpOnly cookie-based authentication
 */
export async function authLoader({ request }: LoaderFunctionArgs) {
  // Get current auth state from Zustand store
  const { isAuthenticated, user, actions } = useAuthStore.getState();
  
  // If user is authenticated, allow access
  if (isAuthenticated && user) {
    return { user };
  }
  
  // Check if we can validate auth with the server via httpOnly cookies
  try {
    // Set loading state
    actions.setLoading(true);
    
    // Attempt to get current session from server using httpOnly cookies
    const response = await fetch('/api/auth/user', {
      credentials: 'include' // Use httpOnly cookies for auth
    });
    
    if (response.ok) {
      const apiResponse = await response.json();
      const userData = apiResponse.data || apiResponse;
      
      // User is authenticated via httpOnly cookie - update store with current user data
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