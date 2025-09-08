import { Link, useNavigate } from 'react-router-dom';
import { Group, Button, Box } from '@mantine/core';
import { useUser, useIsAuthenticated, useAuthActions } from '../../stores/authStore';
import { useEffect, useState } from 'react';

/**
 * Navigation Component - Main header navigation
 * Matches the exact wireframe design with logo, nav items, and login button
 */
export const Navigation: React.FC = () => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();
  const { logout } = useAuthActions();
  const navigate = useNavigate();
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 100);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const handleLogout = async () => {
    try {
      logout();
      navigate('/');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  return (
    <Box
      component="header"
      data-testid="nav-main"
      className={`header ${isScrolled ? 'scrolled' : ''}`}
      style={{
        background: 'rgba(255, 248, 240, 0.95)',
        backdropFilter: 'blur(10px)',
        boxShadow: '0 2px 20px rgba(0,0,0,0.08)',
        padding: isScrolled ? '8px 0' : '18px 0',
        paddingLeft: '40px',
        paddingRight: '40px',
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
          fontSize: '30px',
          fontWeight: 800,
          color: 'var(--color-burgundy)',
          textDecoration: 'none',
          letterSpacing: '-0.5px',
          transition: 'all 0.3s ease',
          position: 'relative',
          height: 'inherit',
          padding: '0 24px',
        }}
      >
        WITCH CITY ROPE
      </Box>

      {/* Navigation Items */}
      <Group gap="var(--space-xl)" style={{ alignItems: 'center', marginRight: '30px' }} className="nav">
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

        {/* Login/User Section */}
        {isAuthenticated && user ? (
          <Group gap="var(--space-md)">
            <Box
              component={Link}
              to="/dashboard"
              data-testid="avatar-user"
              style={{
                color: 'var(--color-charcoal)',
                textDecoration: 'none',
                fontFamily: 'var(--font-heading)',
                fontSize: '14px',
                fontWeight: 500,
              }}
            >
              Welcome, {user.sceneName}
            </Box>
            <Box
              component="button"
              data-testid="button-logout"
              onClick={handleLogout}
              className="btn btn-primary-alt"
            >
              Logout
            </Box>
          </Group>
        ) : (
          <Box
            component={Link}
            to="/login"
            className="btn btn-primary"
          >
            Login
          </Box>
        )}
      </Group>

      {/* Mobile Menu Toggle */}
      <Box component="button" data-testid="button-mobile-menu" className="mobile-menu-toggle">
        <Box component="span" />
        <Box component="span" />
        <Box component="span" />
      </Box>
    </Box>
  );
};