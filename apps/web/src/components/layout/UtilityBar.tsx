import { Box, Group } from '@mantine/core'
import { Link } from 'react-router-dom'
import { useUser, useIsAuthenticated } from '../../stores/authStore'
import { useLogout } from '../../features/auth/api/mutations'

/**
 * UtilityBar Component - Top utility navigation bar
 *
 * STANDARD AUTHENTICATION PATTERN - WitchCityRope Project
 * Last Updated: 2025-11-23
 * Pattern: TanStack Query Mutations + Zustand Store
 *
 * This component displays the top utility bar with:
 * - User greeting (when authenticated)
 * - Navigation links (Private Lessons, Contact, Report an Incident)
 * - Logout button (when authenticated)
 *
 * Authentication Pattern:
 * - Uses Zustand store for reading auth state (useUser, useIsAuthenticated)
 * - Uses TanStack Query mutation for logout action (useLogout)
 *
 * Why this pattern:
 * - Zustand provides fast, performant global state reads
 * - TanStack Query provides automatic loading/error states for mutations
 * - Consistent pattern across ALL auth operations (login, register, logout)
 *
 * See: /docs/standards-processes/frontend/authentication-pattern-guide.md
 */
export const UtilityBar: React.FC = () => {
  // Get auth state from Zustand store (for reading state)
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()

  // Get logout mutation from TanStack Query
  // Mutation handles:
  // 1. Calling API with CSRF token
  // 2. Clearing Zustand store state
  // 3. Clearing sessionStorage
  // 4. Clearing React Query cache
  // 5. Navigating to home page
  const logoutMutation = useLogout()

  /**
   * Handle logout button click
   * Uses TanStack Query mutation for automatic loading states
   */
  const handleLogout = () => {
    logoutMutation.mutate()
  }

  return (
    <Box
      className="utility-bar"
      style={{
        background: 'var(--color-midnight)',
        padding: '12px 0',
        paddingLeft: '40px',
        paddingRight: '40px',
        fontSize: '13px',
        color: 'var(--color-taupe)',
        fontFamily: 'var(--font-heading)',
        textTransform: 'uppercase',
        letterSpacing: '1px',
      }}
    >
      <Group justify="space-between" gap="var(--space-lg)">
        {/* LEFT: User greeting */}
        {isAuthenticated && user ? (
          <Box
            data-testid="user-menu"
            style={{
              color: 'var(--color-taupe)',
              fontFamily: 'var(--font-heading)',
              fontSize: '13px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              fontWeight: 500,
            }}
          >
            <Box data-testid="user-greeting">
              Welcome, {user.sceneName}
            </Box>
          </Box>
        ) : (
          <Box /> // Empty spacer when logged out
        )}

        {/* RIGHT: Existing links + logout */}
        <Group gap="var(--space-lg)">
          <Box
            component={Link}
            to="/safety/report"
            style={{
              color: 'var(--color-brass)',
              fontWeight: 600,
              textDecoration: 'none',
              transition: 'all 0.3s ease',
            }}
            className="utility-bar-link incident-link"
          >
            Report an Incident
          </Box>
          <Box
            component={Link}
            to="/private-lessons"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              transition: 'all 0.3s ease',
            }}
            className="utility-bar-link"
          >
            Private Lessons
          </Box>
          <Box
            component={Link}
            to="/contact-us"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              transition: 'all 0.3s ease',
            }}
            className="utility-bar-link"
          >
            Contact
          </Box>

          {/* Edit Profile link - Commented out per user request 2025-11-09 - can be reinstated if needed
          {isAuthenticated && (
            <Box
              component={Link}
              to="/dashboard/profile-settings"
              data-testid="link-edit-profile"
              style={{
                color: 'var(--color-taupe)',
                textDecoration: 'none',
                transition: 'all 0.3s ease',
              }}
              className="utility-bar-link"
            >
              Edit Profile
            </Box>
          )}
          */}

          {/* Logout link - only for authenticated users */}
          {isAuthenticated && (
            <Box
              component="button"
              data-testid="button-logout"
              onClick={handleLogout}
              disabled={logoutMutation.isPending}
              style={{
                background: 'none',
                border: 'none',
                color: logoutMutation.isPending ? 'var(--color-taupe-50)' : 'var(--color-taupe)',
                textDecoration: 'none',
                transition: 'all 0.3s ease',
                fontFamily: 'var(--font-heading)',
                fontSize: '13px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                cursor: logoutMutation.isPending ? 'not-allowed' : 'pointer',
                opacity: logoutMutation.isPending ? 0.6 : 1,
              }}
              className="utility-bar-link"
            >
              {logoutMutation.isPending ? 'Logging out...' : 'Logout'}
            </Box>
          )}
        </Group>
      </Group>
    </Box>
  )
}
