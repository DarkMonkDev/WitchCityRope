import { Navigate, Outlet, useLocation } from 'react-router-dom'

/**
 * URL normalizer that redirects uppercase URLs to lowercase.
 * Prevents 404 errors when users type URLs with mixed casing (e.g., /Admin/Events).
 *
 * Behavior:
 * - If the URL path contains uppercase characters, redirects to the lowercase version
 * - Preserves query parameters (?key=value) and hash fragments (#section)
 * - Uses `replace` navigation to avoid polluting browser history with redirects
 * - If the URL is already lowercase, renders child routes via <Outlet />
 */
export function LowercaseUrlRedirect() {
  const location = useLocation()

  // Check if the path has any uppercase characters
  if (location.pathname !== location.pathname.toLowerCase()) {
    return (
      <Navigate
        to={`${location.pathname.toLowerCase()}${location.search}${location.hash}`}
        replace
      />
    )
  }

  return <Outlet />
}
