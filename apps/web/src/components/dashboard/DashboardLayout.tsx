import React from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { Box, Text } from '@mantine/core';

interface DashboardLayoutProps {
  children?: React.ReactNode;
}

/**
 * Dashboard Layout Component
 * Provides the layout structure for all dashboard pages with left-side navigation
 * Uses the main site's Navigation component via RootLayout - no duplicate header
 */
export const DashboardLayout: React.FC<DashboardLayoutProps> = ({ children }) => {
  const location = useLocation();

  const menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: '📊' },
    { path: '/dashboard/events', label: 'Events', icon: '📅' },
    { path: '/dashboard/registrations', label: 'Registrations', icon: '📋' },
    { path: '/dashboard/profile', label: 'Profile', icon: '👤' },
    { path: '/dashboard/security', label: 'Security', icon: '🔒' },
    { path: '/dashboard/membership', label: 'Membership', icon: '🎯' },
  ];

  return (
    <Box>
      {/* Main Layout Grid - No header since RootLayout provides Navigation */}
      <Box
        style={{
          display: 'grid',
          gridTemplateColumns: '200px 1fr',
          minHeight: 'calc(100vh - 120px)', // Adjusted for both UtilityBar and Navigation
          backgroundColor: '#FAF6F2',
          '@media (max-width: 768px)': {
            gridTemplateColumns: '1fr',
            gap: 0,
          },
        }}
      >
        {/* Left Navigation */}
        <Box
          component="aside"
          data-testid="dashboard-nav"
          style={{
            backgroundColor: '#FAF6F2',
            padding: '16px 0',
            borderRight: '2px solid rgba(183, 109, 117, 0.1)',
          }}
        >
            <nav>
              <Box style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                {menuItems.map((item) => {
                  const isActive = 
                    item.path === '/dashboard' 
                      ? location.pathname === '/dashboard'
                      : location.pathname.startsWith(item.path);

                  return (
                    <Text
                      key={item.path}
                      component={Link}
                      to={item.path}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '12px',
                        padding: '12px 16px',
                        color: isActive ? '#FFF8F0' : '#2B2B2B',
                        textDecoration: 'none',
                        fontFamily: "'Montserrat', sans-serif",
                        fontWeight: 500,
                        fontSize: '14px',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        background: isActive 
                          ? 'linear-gradient(135deg, #880124, #614B79)' 
                          : 'transparent',
                        transition: 'all 0.3s ease',
                        borderRadius: 0,
                        position: 'relative',
                      }}
                      onMouseEnter={(e) => {
                        if (!isActive) {
                          e.currentTarget.style.background = 'rgba(183, 109, 117, 0.1)';
                          e.currentTarget.style.color = '#880124';
                          e.currentTarget.style.transform = 'translateX(4px)';
                        }
                      }}
                      onMouseLeave={(e) => {
                        if (!isActive) {
                          e.currentTarget.style.background = 'transparent';
                          e.currentTarget.style.color = '#2B2B2B';
                          e.currentTarget.style.transform = 'translateX(0)';
                        }
                      }}
                    >
                      <Text style={{ fontSize: '16px', width: '20px', textAlign: 'center' }}>
                        {item.icon}
                      </Text>
                      <Text>{item.label}</Text>
                    </Text>
                  );
                })}
              </Box>
            </nav>
          </Box>

        {/* Main Content */}
        <Box
          component="main"
          style={{
            backgroundColor: '#FAF6F2',
            padding: '16px 24px',
          }}
        >
          {children || <Outlet />}
        </Box>
      </Box>
    </Box>
  );
};