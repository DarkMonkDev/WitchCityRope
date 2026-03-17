import { Link } from 'react-router-dom'
import { Group, Button, Box, Stack, Drawer } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { IconX } from '@tabler/icons-react'
import { useUser, useIsAuthenticated } from '../../stores/authStore'
import { useLogout } from '../../features/auth/api/mutations'
import { useMenuVisibility } from '../../features/vetting/hooks/useMenuVisibility'
import { useEffect, useState, useCallback } from 'react'
import { hasRole } from '../../utils/roleUtils'

/**
 * Navigation Component - Main header navigation
 * Matches the exact wireframe design with logo, nav items, and login button
 * Conditionally shows "How to Join" based on vetting status
 * Includes mobile hamburger menu with slide-in navigation
 *
 * STANDARD AUTHENTICATION PATTERN - WitchCityRope Project
 * Pattern: TanStack Query Mutations + Zustand Store
 * See: /docs/standards-processes/frontend/authentication-pattern-guide.md
 */
export const Navigation: React.FC = () => {
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()
  const logoutMutation = useLogout()
  const { shouldShow: showHowToJoin } = useMenuVisibility()
  const [isScrolled, setIsScrolled] = useState(false)
  // Use Mantine's useDisclosure hook for mobile menu state
  // Replaces custom useState + useEffect for scroll locking
  // Drawer component handles scroll locking automatically via react-remove-scroll
  const [isMobileMenuOpen, { open: openMobileMenu, close: closeMobileMenu }] = useDisclosure(false)

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 100)
    }

    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  // Handle logout button click
  const handleLogout = useCallback(() => {
    closeMobileMenu() // Close mobile menu first
    logoutMutation.mutate(undefined)
  }, [logoutMutation, closeMobileMenu])

  return (
    <Box
      component="header"
      data-testid="nav-main"
      className={`header ${isScrolled ? 'scrolled' : ''}`}
      style={{
        background: 'rgba(255, 248, 240, 0.95)',
        backdropFilter: 'blur(10px)',
        boxShadow: '0 2px 20px rgba(0,0,0,0.08)',
        padding: `${isScrolled ? '8px' : '18px'} clamp(1.25rem, 2vw + 0.75rem, 2.5rem)`, // 20px mobile → 40px desktop
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        position: 'sticky',
        top: 0,
        zIndex: 100,
        transition: 'all 0.3s ease',
        borderBottom: `3px solid rgba(183, 109, 117, ${isScrolled ? '0.5' : '0.3'})`,
      }}
    >
      {/* Logo */}
      <Box
        component={Link}
        to="/"
        className="logo logo-underline-animation"
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: 'clamp(1.5rem, 1.5vw + 1rem, 1.75rem)', // 24px mobile → 28px desktop
          fontWeight: 800,
          color: 'var(--color-burgundy)',
          textDecoration: 'none',
          letterSpacing: '-0.5px',
          transition: 'all 0.3s ease',
          position: 'relative',
          height: 'inherit',
          padding: '0 clamp(0.75rem, 1.5vw + 0.25rem, 1.5rem)', // 12px mobile → 24px desktop
        }}
      >
        WITCH CITY ROPE
      </Box>

      {/* Navigation Items */}
      <Group
        gap="var(--space-xl)"
        style={{ alignItems: 'center', marginRight: '30px' }}
        className="nav"
      >
        {/* Admin link - only for administrators */}
        {/* Backend now returns user.roles - proper role-based access control */}
        {/* Type-safe role check using auto-generated UserRole */}
        {hasRole(user, 'Administrator') && (
          <Box
            component={Link}
            to="/admin"
            
            data-testid="link-admin"
            className="btn btn-primary"
          >
            Admin
          </Box>
        )}

        <Box
          component={Link}
          to="/events"
          
          data-testid="link-events"
          style={{
            color: 'var(--color-charcoal)',
            textDecoration: 'none',
            fontFamily: 'var(--font-heading)',
            fontWeight: 500,
            fontSize: '15px',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            transition: 'all 0.3s ease',
            position: 'relative',
          }}
          className="nav-underline-animation"
        >
          Events & Classes
        </Box>

        {/* Conditionally render "How to Join" based on vetting status */}
        {showHowToJoin && (
          <Box
            component={Link}
            to="/join"
            
            style={{
              color: 'var(--color-charcoal)',
              textDecoration: 'none',
              fontFamily: 'var(--font-heading)',
              fontWeight: 500,
              fontSize: '15px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              transition: 'all 0.3s ease',
              position: 'relative',
            }}
            className="nav-underline-animation"
          >
            How to Join
          </Box>
        )}

        <Box
          component={Link}
          to="/resources"
          
          style={{
            color: 'var(--color-charcoal)',
            textDecoration: 'none',
            fontFamily: 'var(--font-heading)',
            fontWeight: 500,
            fontSize: '15px',
            textTransform: 'uppercase',
            letterSpacing: '1px',
            transition: 'all 0.3s ease',
            position: 'relative',
          }}
          className="nav-underline-animation"
        >
          Resources
        </Box>

        {/* Dashboard CTA / Login Button */}
        {isAuthenticated && user ? (
          <Box
            component={Link}
            to="/dashboard"
            
            data-testid="link-dashboard"
            className="btn btn-primary"
          >
            Dashboard
          </Box>
        ) : (
          <Box component={Link} to="/login"  className="btn btn-primary">
            Login
          </Box>
        )}
      </Group>

      {/* Mobile Menu Toggle - Hamburger button that also acts as close button when menu is open */}
      <Box
        component="button"
        data-testid="button-mobile-menu"
        className={`mobile-menu-toggle ${isMobileMenuOpen ? 'open' : ''}`}
        onClick={isMobileMenuOpen ? closeMobileMenu : openMobileMenu}
        aria-label={isMobileMenuOpen ? 'Close mobile menu' : 'Open mobile menu'}
        aria-expanded={isMobileMenuOpen}
        aria-controls="mobile-menu"
      >
        <Box component="span" />
        <Box component="span" />
        <Box component="span" />
      </Box>

      {/* Mobile Menu Drawer - Replaces custom Box with position: fixed
          Mantine Drawer provides:
          - Automatic scroll locking via react-remove-scroll (no horizontal overflow bug)
          - Built-in overlay with proper z-index management
          - Accessibility attributes (aria-modal, role="dialog", etc.)
          - Smooth transitions without custom CSS
          - Proper focus trapping
      */}
      <Drawer
        opened={isMobileMenuOpen}
        onClose={closeMobileMenu}
        position="right"
        size="80%"
        padding={0}
        withCloseButton={false}
        styles={{
          content: {
            background: 'var(--color-ivory)',
            maxWidth: '320px',
          },
          overlay: {
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
          },
        }}
      >
        {/* Close button at top-right of drawer */}
        <Box
          component="button"
          onClick={closeMobileMenu}
          data-testid="mobile-menu-close"
          aria-label="Close menu"
          style={{
            position: 'absolute',
            top: '16px',
            right: '16px',
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            padding: '8px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'var(--color-burgundy)',
            zIndex: 10,
          }}
        >
          <IconX size={28} stroke={2} />
        </Box>

        <Stack
          gap="0"
          p="var(--space-lg)"
          pt="80px"
          style={{
            /* CRITICAL FIX: Ensure pointer events pass through to child links
             * Without this, Mantine Stack intercepts pointer events and prevents
             * links from being clickable on mobile menu (Playwright test failure)
             * See: react-developer-lessons-learned-2.md - Mobile Menu Pointer Events
             */
            pointerEvents: 'none',
          }}
        >
          {/* Dashboard CTA / Login Button - TOP OF MENU */}
          {isAuthenticated && user ? (
            <Button
              component={Link}
              to="/dashboard"
              
              onClick={closeMobileMenu}
              data-testid="mobile-link-dashboard"
              color="blue"
              fullWidth
              mb="var(--space-sm)"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                  borderRadius: '12px 6px 12px 6px',
                  background: 'var(--color-burgundy)',
                  color: 'var(--color-ivory)',
                  pointerEvents: 'auto', // Re-enable pointer events for clickability
                },
              }}
            >
              Dashboard
            </Button>
          ) : (
            <Button
              component={Link}
              to="/login"
              
              onClick={closeMobileMenu}
              data-testid="mobile-link-login"
              color="blue"
              fullWidth
              mb="var(--space-sm)"
              styles={{
                root: {
                  fontWeight: 600,
                  height: '44px',
                  paddingTop: '12px',
                  paddingBottom: '12px',
                  fontSize: '14px',
                  lineHeight: '1.2',
                  borderRadius: '12px 6px 12px 6px',
                  background: 'var(--color-burgundy)',
                  color: 'var(--color-ivory)',
                  pointerEvents: 'auto', // Re-enable pointer events for clickability
                },
              }}
            >
              Login
            </Button>
          )}

          {/* Admin link - only for administrators */}
          {hasRole(user, 'Administrator') && (
            <Box
              component={Link}
              to="/admin"
              
              onClick={closeMobileMenu}
              data-testid="mobile-link-admin"
              style={{
                color: 'var(--color-charcoal)',
                textDecoration: 'none',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '18px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                padding: 'var(--space-md) 0',
                borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
                pointerEvents: 'auto', // Re-enable pointer events for clickability
              }}
            >
              Admin
            </Box>
          )}

          {/* Events & Classes */}
          <Box
            component={Link}
            to="/events"
            
            onClick={closeMobileMenu}
            data-testid="mobile-link-events"
            style={{
              color: 'var(--color-charcoal)',
              textDecoration: 'none',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '18px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              padding: 'var(--space-md) 0',
              borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
              pointerEvents: 'auto', // Re-enable pointer events for clickability
            }}
          >
            Events & Classes
          </Box>

          {/* Conditionally render "How to Join" based on vetting status */}
          {showHowToJoin && (
            <Box
              component={Link}
              to="/join"
              
              onClick={closeMobileMenu}
              data-testid="mobile-link-join"
              style={{
                color: 'var(--color-charcoal)',
                textDecoration: 'none',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '18px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                padding: 'var(--space-md) 0',
                borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
                pointerEvents: 'auto', // Re-enable pointer events for clickability
              }}
            >
              How to Join
            </Box>
          )}

          {/* Private Lessons */}
          <Box
            component={Link}
            to="/private-lessons"
            
            onClick={closeMobileMenu}
            data-testid="mobile-link-private-lessons"
            style={{
              color: 'var(--color-charcoal)',
              textDecoration: 'none',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '18px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              padding: 'var(--space-md) 0',
              borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
              pointerEvents: 'auto', // Re-enable pointer events for clickability
            }}
          >
            Private Lessons
          </Box>

          {/* Resources */}
          <Box
            component={Link}
            to="/resources"
            
            onClick={closeMobileMenu}
            data-testid="mobile-link-resources"
            style={{
              color: 'var(--color-charcoal)',
              textDecoration: 'none',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '18px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              padding: 'var(--space-md) 0',
              borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
              pointerEvents: 'auto', // Re-enable pointer events for clickability
            }}
          >
            Resources
          </Box>

          {/* Report an Incident */}
          <Box
            component={Link}
            to="/safety/report"
            
            onClick={closeMobileMenu}
            data-testid="mobile-link-report-incident"
            style={{
              color: 'var(--color-charcoal)',
              textDecoration: 'none',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: '18px',
              textTransform: 'uppercase',
              letterSpacing: '1px',
              padding: 'var(--space-md) 0',
              borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
              pointerEvents: 'auto', // Re-enable pointer events for clickability
            }}
          >
            Report an Incident
          </Box>

          {/* Logout button - only for authenticated users */}
          {isAuthenticated && (
            <Box
              component="button"
              onClick={handleLogout}
              data-testid="mobile-button-logout"
              style={{
                background: 'none',
                border: 'none',
                color: 'var(--color-charcoal)',
                textDecoration: 'none',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '18px',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                padding: 'var(--space-md) 0',
                borderBottom: '1px solid rgba(183, 109, 117, 0.2)',
                pointerEvents: 'auto', // Re-enable pointer events for clickability
                cursor: 'pointer',
                textAlign: 'left',
                width: '100%',
              }}
            >
              Logout
            </Box>
          )}
        </Stack>
      </Drawer>
    </Box>
  )
}
