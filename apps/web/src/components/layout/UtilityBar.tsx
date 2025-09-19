import { Box, Group } from '@mantine/core'
import { Link } from 'react-router-dom'
import { useUser, useIsAuthenticated } from '../../stores/authStore'
import { useAuth } from '../../contexts/AuthContext'

/**
 * UtilityBar Component - Top utility navigation bar
 *
 * ⚠️ DO NOT CHANGE THE AUTHENTICATION PATTERN WITHOUT EXPLICIT DIRECTION ⚠️
 * The logout functionality has been tested and verified to work correctly.
 * TESTED AND VERIFIED ON: 2025-09-19
 *
 * This component displays the top utility bar with:
 * - User greeting (when authenticated)
 * - Navigation links (Private Lessons, Contact, Report an Incident)
 * - Logout button (when authenticated)
 *
 * CRITICAL Authentication Pattern (DO NOT CHANGE):
 * - Uses Zustand store for reading auth state (useUser, useIsAuthenticated)
 * - Uses AuthContext for logout action to ensure proper cleanup
 *
 * This dual approach is necessary because:
 * 1. Many components read from Zustand store directly for performance
 * 2. AuthContext provides the complete logout flow that cleans both stores
 *
 * The logout MUST use useAuth() from AuthContext, NOT the Zustand store directly.
 */
export const UtilityBar: React.FC = () => {
  // Get auth state from Zustand store (for reading state)
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()

  // Get logout function from AuthContext (for proper cleanup)
  // AuthContext logout handles:
  // 1. Clearing React Context state
  // 2. Clearing Zustand store state
  // 3. Clearing sessionStorage
  // 4. Calling API to clear httpOnly cookie
  // 5. Redirecting to login page
  const { logout } = useAuth()

  /**
   * Handle logout button click
   * Uses the AuthContext logout which ensures complete cleanup
   */
  const handleLogout = async () => {
    try {
      await logout()
      // No need for manual redirect - AuthContext logout handles it
    } catch (error) {
      console.error('Logout failed:', error)
      // AuthContext logout already handles error cases
    }
  }

  return (
    <Box
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
            data-testid="user-greeting"
            style={{
              color: 'var(--color-taupe)',
              fontFamily: 'var(--font-heading)',
              fontSize: '13px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              fontWeight: 500,
            }}
          >
            Welcome, {user.sceneName}
          </Box>
        ) : (
          <Box /> // Empty spacer when logged out
        )}

        {/* RIGHT: Existing links + logout */}
        <Group gap="var(--space-lg)">
          <Box
            component={Link}
            to="/incident-report"
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
            to="/contact"
            style={{
              color: 'var(--color-taupe)',
              textDecoration: 'none',
              transition: 'all 0.3s ease',
            }}
            className="utility-bar-link"
          >
            Contact
          </Box>

          {/* Logout link - only for authenticated users */}
          {isAuthenticated && (
            <Box
              component="button"
              data-testid="button-logout"
              onClick={handleLogout}
              style={{
                background: 'none',
                border: 'none',
                color: 'var(--color-taupe)',
                textDecoration: 'none',
                transition: 'all 0.3s ease',
                fontFamily: 'var(--font-heading)',
                fontSize: '13px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                cursor: 'pointer',
              }}
              className="utility-bar-link"
            >
              Logout
            </Box>
          )}
        </Group>
      </Group>
    </Box>
  )
}
