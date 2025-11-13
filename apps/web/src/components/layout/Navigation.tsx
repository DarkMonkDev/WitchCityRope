import { Link } from 'react-router-dom'
import { Group, Button, Box, Stack } from '@mantine/core'
import { useUser, useIsAuthenticated } from '../../stores/authStore'
import { useMenuVisibility } from '../../features/vetting/hooks/useMenuVisibility'
import { useEffect, useState, useCallback } from 'react'
import type { components } from '@witchcityrope/shared-types'

/**
 * Navigation Component - Main header navigation
 * Matches the exact wireframe design with logo, nav items, and login button
 * Conditionally shows "How to Join" based on vetting status
 * Includes mobile hamburger menu with slide-in navigation
 */
export const Navigation: React.FC = () => {
  const user = useUser()
  const isAuthenticated = useIsAuthenticated()
  const { shouldShow: showHowToJoin } = useMenuVisibility()
  const [isScrolled, setIsScrolled] = useState(false)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 100)
    }

    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  // Toggle mobile menu
  const toggleMobileMenu = useCallback(() => {
    setIsMobileMenuOpen((prev) => !prev)
  }, [])

  // Close mobile menu when link is clicked
  const closeMobileMenu = useCallback(() => {
    setIsMobileMenuOpen(false)
  }, [])

  // Prevent body scroll when mobile menu is open
  useEffect(() => {
    if (isMobileMenuOpen) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = ''
    }

    return () => {
      document.body.style.overflow = ''
    }
  }, [isMobileMenuOpen])

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
          fontSize: 'clamp(1.5rem, 1.5vw + 1rem, 1.875rem)', // 24px mobile → 30px desktop
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
        {user?.role === ('Administrator' as components['schemas']['UserRole']) && (
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
          <Box component={Link} to="/login" className="btn btn-primary">
            Login
          </Box>
        )}
      </Group>

      {/* Mobile Menu Toggle */}
      <Box
        component="button"
        data-testid="button-mobile-menu"
        className={`mobile-menu-toggle ${isMobileMenuOpen ? 'open' : ''}`}
        onClick={toggleMobileMenu}
        aria-label={isMobileMenuOpen ? 'Close mobile menu' : 'Open mobile menu'}
        aria-expanded={isMobileMenuOpen}
        aria-controls="mobile-menu"
      >
        <Box component="span" />
        <Box component="span" />
        <Box component="span" />
      </Box>

      {/* Mobile Menu Overlay */}
      {isMobileMenuOpen && (
        <Box
          className="mobile-menu-overlay"
          onClick={closeMobileMenu}
          aria-hidden="true"
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            width: '100vw',
            height: '100vh',
            background: 'rgba(0, 0, 0, 0.5)',
            zIndex: 999,
          }}
        />
      )}

      {/* Mobile Menu */}
      <Box
        id="mobile-menu"
        className={`mobile-menu ${isMobileMenuOpen ? 'open' : ''}`}
        role="navigation"
        aria-label="Mobile navigation menu"
        style={{
          position: 'fixed',
          top: 0,
          right: isMobileMenuOpen ? 0 : '-100%',
          width: '80%',
          maxWidth: '320px',
          height: '100vh',
          background: 'var(--color-ivory)',
          zIndex: 1000,
          transition: 'right 0.3s ease',
          overflowY: 'auto',
          boxShadow: isMobileMenuOpen ? '-4px 0 20px rgba(0, 0, 0, 0.15)' : 'none',
        }}
      >
        <Stack gap="0" p="var(--space-lg)" pt="80px">
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
                },
              }}
            >
              Login
            </Button>
          )}

          {/* Admin link - only for administrators */}
          {user?.role === ('Administrator' as components['schemas']['UserRole']) && (
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
            }}
          >
            Report an Incident
          </Box>
        </Stack>
      </Box>
    </Box>
  )
}
