import { Box, Group } from '@mantine/core'
import { Link, useNavigate } from 'react-router-dom'
import { useUser, useIsAuthenticated, useAuthActions } from '../../stores/authStore'

/**
 * UtilityBar Component - Top utility navigation bar
 * Matches the exact wireframe design with dark midnight background
 * and taupe colored links with hover animations
 *
 * Updated to show user greeting on LEFT and logout link on RIGHT for authenticated users
 */
export const UtilityBar: React.FC = () => {
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()
  const { logout } = useAuthActions()
  const navigate = useNavigate()

  const handleLogout = async () => {
    try {
      await logout()
      navigate('/')
    } catch (error) {
      console.error('Logout failed:', error)
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
