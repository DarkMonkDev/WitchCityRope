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
      className={`header ${isScrolled ? 'scrolled' : ''}`}
      style={{
        background: 'rgba(255, 248, 240, 0.95)',
        backdropFilter: 'blur(10px)',
        boxShadow: '0 2px 20px rgba(0,0,0,0.08)',
        padding: isScrolled ? '12px 40px' : 'var(--space-sm) 40px',
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
        }}
      >
        WITCH CITY ROPE
      </Box>

      {/* Navigation Items */}
      <Group gap="var(--space-xl)" style={{ alignItems: 'center' }} className="nav">
        <Box
          component={Link}
          to="/events"
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
            <Button
              onClick={handleLogout}
              style={{
                background: 'linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%)',
                color: 'var(--color-ivory)',
                border: 'none',
                borderRadius: '12px 6px 12px 6px',
                padding: '14px 32px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '14px',
                textTransform: 'uppercase',
                letterSpacing: '1.5px',
                transition: 'all 0.3s ease',
                cursor: 'pointer',
                boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.borderRadius = '6px 12px 6px 12px';
                e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-electric-dark) 0%, var(--color-electric) 100%)';
                e.currentTarget.style.boxShadow = '0 6px 25px rgba(157, 78, 221, 0.5)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.borderRadius = '12px 6px 12px 6px';
                e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-electric) 0%, var(--color-electric-dark) 100%)';
                e.currentTarget.style.boxShadow = '0 4px 15px rgba(157, 78, 221, 0.4)';
              }}
            >
              Logout
            </Button>
          </Group>
        ) : (
          <Button
            component={Link}
            to="/login"
            style={{
              background: 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)',
              color: 'var(--color-midnight)',
              border: 'none',
              borderRadius: '12px 6px 12px 6px',
              padding: '14px 32px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 700,
              fontSize: '14px',
              textTransform: 'uppercase',
              letterSpacing: '1.5px',
              transition: 'all 0.3s ease',
              cursor: 'pointer',
              textDecoration: 'none',
              display: 'inline-block',
              boxShadow: '0 4px 15px rgba(255, 191, 0, 0.4)',
              position: 'relative',
              overflow: 'hidden',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.borderRadius = '6px 12px 6px 12px';
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-amber-dark) 0%, var(--color-amber) 100%)';
              e.currentTarget.style.boxShadow = '0 6px 25px rgba(255, 191, 0, 0.5)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.borderRadius = '12px 6px 12px 6px';
              e.currentTarget.style.background = 'linear-gradient(135deg, var(--color-amber) 0%, var(--color-amber-dark) 100%)';
              e.currentTarget.style.boxShadow = '0 4px 15px rgba(255, 191, 0, 0.4)';
            }}
          >
            Login
          </Button>
        )}
      </Group>

      {/* Mobile Menu Toggle */}
      <Box component="button" className="mobile-menu-toggle">
        <Box component="span" />
        <Box component="span" />
        <Box component="span" />
      </Box>
    </Box>
  );
};