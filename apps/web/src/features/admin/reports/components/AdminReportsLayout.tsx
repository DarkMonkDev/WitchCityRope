import React from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { Box, Text } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import {
  IconLayoutDashboard,
  IconCreditCard,
  IconChartBar,
  IconArrowLeft,
} from '@tabler/icons-react';

/**
 * Admin Reports Layout Component
 *
 * Provides a left-nav layout for all report pages, following the same pattern
 * as DashboardLayout.tsx. The nav is extensible: adding a report requires
 * only one entry in navItems and one route in router.tsx.
 *
 * Desktop: CSS Grid with 250px left nav + 1fr content area.
 * Mobile (<=768px): Vertical layout with horizontal scrollable tab nav on top.
 */

interface NavItem {
  path: string;
  label: string;
  icon: React.FC<{ size?: number | string }>;
  /** When true, use exact pathname match instead of startsWith */
  exact?: boolean;
}

const navItems: NavItem[] = [
  { path: '/admin/reports', label: 'Dashboard', icon: IconLayoutDashboard, exact: true },
  { path: '/admin/reports/payments', label: 'Payments', icon: IconCreditCard },
  { path: '/admin/reports/transactions', label: 'Transactions', icon: IconChartBar },
];

export const AdminReportsLayout: React.FC = () => {
  const location = useLocation();
  // Match DashboardLayout breakpoint for mobile detection
  const isMobile = useMediaQuery('(max-width: 768px)');

  const isItemActive = (item: NavItem): boolean => {
    if (item.exact) {
      return location.pathname === item.path;
    }
    return location.pathname.startsWith(item.path);
  };

  return (
    <Box>
      <Box
        style={{
          display: 'grid',
          // Desktop: fixed left nav, fluid content. Mobile: single column.
          gridTemplateColumns: isMobile ? '1fr' : '250px 1fr',
          minHeight: 'calc(100vh - 120px)',
          backgroundColor: '#FAF6F2',
        }}
      >
        {/* Navigation — left sidebar on desktop, horizontal tabs on mobile */}
        <Box
          component="aside"
          data-testid="reports-nav"
          style={
            isMobile
              ? {
                  backgroundColor: '#FAF6F2',
                  padding: '12px 16px 0',
                  borderBottom: '2px solid rgba(183, 109, 117, 0.1)',
                  overflowX: 'auto',
                  WebkitOverflowScrolling: 'touch',
                }
              : {
                  backgroundColor: '#FAF6F2',
                  padding: '16px 0',
                  borderRight: '2px solid rgba(183, 109, 117, 0.1)',
                }
          }
        >
          <nav>
            {/* "Back to Admin" link */}
            <Text
              component={Link}
              to="/admin"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: isMobile ? '8px 12px' : '8px 16px',
                marginBottom: isMobile ? '8px' : '12px',
                color: '#880124',
                textDecoration: 'none',
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 600,
                fontSize: '12px',
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                transition: 'all 0.2s ease',
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateX(-2px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateX(0)';
              }}
            >
              <IconArrowLeft size={14} />
              Back to Admin
            </Text>

            {/* Nav items — vertical stack on desktop, horizontal row on mobile */}
            <Box
              style={{
                display: 'flex',
                flexDirection: isMobile ? 'row' : 'column',
                gap: '4px',
              }}
            >
              {navItems.map((item) => {
                const isActive = isItemActive(item);
                const Icon = item.icon;

                return (
                  <Text
                    key={item.path}
                    component={Link}
                    to={item.path}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '12px',
                      padding: isMobile ? '10px 16px' : '12px 16px',
                      color: isActive ? '#FFF8F0' : '#2B2B2B',
                      textDecoration: 'none',
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 500,
                      fontSize: '14px',
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      // Active state uses burgundy-to-plum gradient, matching DashboardLayout
                      background: isActive
                        ? 'linear-gradient(135deg, #880124, #614B79)'
                        : 'transparent',
                      transition: 'all 0.3s ease',
                      borderRadius: isMobile ? '8px 8px 0 0' : 0,
                      whiteSpace: 'nowrap',
                      position: 'relative',
                    }}
                    onMouseEnter={(e) => {
                      if (!isActive) {
                        // Rose-gold tint on hover, matching DashboardLayout hover styling
                        e.currentTarget.style.background = 'rgba(183, 109, 117, 0.1)';
                        e.currentTarget.style.color = '#880124';
                        if (!isMobile) {
                          e.currentTarget.style.transform = 'translateX(4px)';
                        }
                      }
                    }}
                    onMouseLeave={(e) => {
                      if (!isActive) {
                        e.currentTarget.style.background = 'transparent';
                        e.currentTarget.style.color = '#2B2B2B';
                        if (!isMobile) {
                          e.currentTarget.style.transform = 'translateX(0)';
                        }
                      }
                    }}
                  >
                    <Icon size={18} />
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
            padding: isMobile ? '16px' : '16px 24px',
          }}
        >
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
};
