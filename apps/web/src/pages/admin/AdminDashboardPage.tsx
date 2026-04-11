import React from 'react';
import { Container, Title, Text, Grid, Paper, Box, Badge } from '@mantine/core';
import { IconCalendarEvent, IconUsers, IconSettings, IconChartBar, IconClipboardCheck, IconFileText, IconAlertTriangle, IconMail } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { useVettingStats } from '../../features/admin/vetting/hooks/useVettingStats';
import { useSafetyDashboard } from '../../features/safety/hooks/useSafetyIncidents';
import { useEvents } from '../../lib/api/hooks/useEvents';
import { useMembers } from '../../features/admin/members/hooks/useMembers';
import { useUser } from '../../stores/authStore';
import { hasRole, hasAnyRole } from '../../utils/roleUtils';
import { DASHBOARD_CARD_ROLES } from '../../constants/adminRoles';
import { isActiveMemberStatus } from '../../features/vetting/constants/vettingStatusConfig';

interface DashboardCard {
  title: string;
  description: string;
  icon: React.ReactNode;
  count?: number;
  countLabel?: string;
  link: string;
  color: string;
  badge?: string;
  testId?: string;
}

/**
 * Admin Dashboard Page
 *
 * Displays dashboard cards filtered by the current user's role.
 * Administrator sees all 8 cards. Other admin-capable roles only see
 * cards relevant to their permissions (defined in DASHBOARD_CARD_ROLES).
 *
 * Data hooks are always called (React rules of hooks) but the useMembers
 * hook is disabled for non-Administrator users to prevent 403 errors
 * from the admin-only /api/admin/users endpoint.
 */
export const AdminDashboardPage: React.FC = () => {
  const user = useUser();

  // Determine which features this user can access
  // Administrator sees everything; other roles see a subset of cards
  const isAdmin = hasRole(user, 'Administrator');

  /**
   * Check if the current user can see a specific dashboard card.
   * Looks up the card title in DASHBOARD_CARD_ROLES and checks if the
   * user has any of the allowed roles for that card.
   */
  const canSeeCard = (cardTitle: string): boolean => {
    if (isAdmin) return true; // Administrator sees all cards
    const allowedRoles = DASHBOARD_CARD_ROLES[cardTitle];
    if (!allowedRoles) return false;
    return hasAnyRole(user, allowedRoles);
  };

  // --- Data hooks ---
  // All hooks must be called unconditionally (React rules of hooks).
  // Hooks for admin-only data use `enabled` to prevent API calls that
  // would 403 for non-admin users.

  const { data: vettingStats } = useVettingStats();
  // useVettingStats already handles 403 gracefully (throwOnError: false, returns 0)

  const { data: safetyDashboard } = useSafetyDashboard();
  // useSafetyDashboard allows SafetyTeam + Administrator; handles 403 gracefully

  const { data: events } = useEvents();
  // useEvents hits a public endpoint — safe for all roles

  // CRITICAL: useMembers calls /api/admin/users which requires Administrator role.
  // Non-admin users would get 403 → throwOnError: true → page crash.
  // Setting enabled: false prevents the API call entirely for non-admin users.
  const { data: membersData } = useMembers({
    page: 1,
    pageSize: 10000, // Large number to get all members
    roleFilters: [],
    searchQuery: '',
    sortBy: 'CreatedAt',
    sortDirection: 'Desc',
  }, { enabled: isAdmin });

  // --- Computed counts ---

  // Calculate upcoming events (events with future start dates)
  const upcomingEventsCount = events
    ? (events as any).filter((event: any) => new Date(event.startDate) > new Date()).length
    : 0;

  // Calculate active members count (only meaningful for Administrator).
  //
  // Previously used magic integer comparisons (`=== 0 || === 1 || === 3`)
  // which required a comment to explain what each integer meant. The
  // `isActiveMemberStatus()` helper from vettingStatusConfig.ts replaces
  // the comparison with a named predicate that encapsulates the same
  // logic, sourced from the single-source status config.
  //
  // UserDto ships vettingStatus as a raw int (not the enum string) as of
  // Phase 2, so the helper uses its int bridge internally. Phase 3 will
  // normalize backend DTOs and this call site can switch to the plain
  // string version at that point.
  const activeMembersCount = membersData?.users
    ? membersData.users.filter(
        (member) => member.isActive && isActiveMemberStatus(member.vettingStatus)
      ).length
    : 0;

  // Calculate active incidents (not on hold or closed)
  // Active statuses: ReportSubmitted, InformationGathering, ReviewingFinalReport
  const activeIncidentsCount = safetyDashboard
    ? (safetyDashboard as any).statistics.newCount +
      (safetyDashboard as any).statistics.inProgressCount +
      (safetyDashboard as any).statistics.reviewingFinalReportCount
    : 0;

  // --- Card definitions ---
  // All cards are defined here; filtering happens via canSeeCard() below.
  // Card titles must match keys in DASHBOARD_CARD_ROLES exactly.
  const dashboardCards: DashboardCard[] = [
    {
      title: 'Events Management',
      description: 'Create, edit, and manage all community events, workshops, and classes',
      icon: <IconCalendarEvent size={32} />,
      count: upcomingEventsCount,
      countLabel: 'Upcoming Events',
      link: '/admin/events',
      color: '#880124',
    },
    {
      title: 'Member Management',
      description: 'Manage community members, roles, and permissions',
      icon: <IconUsers size={32} />,
      count: activeMembersCount,
      countLabel: 'Active Members',
      link: '/admin/members',
      color: '#DAA520',
    },
    {
      title: 'Vetting Applications',
      description: 'Review and manage member vetting applications',
      icon: <IconClipboardCheck size={32} />,
      count: vettingStats?.needsReviewCount ?? 0,
      countLabel: 'Needs Review',
      link: '/admin/vetting',
      color: '#9b4a75',
    },
    {
      title: 'Incident Reports',
      description: 'Manage safety incident reports and investigations',
      icon: <IconAlertTriangle size={32} />,
      count: activeIncidentsCount,
      countLabel: 'Active Incidents',
      link: '/admin/safety/incidents',
      color: '#7B2CBF',
      testId: 'incident-reports-card',
    },
    {
      title: 'Email Templates',
      description: 'Manage global email templates for all categories',
      icon: <IconMail size={32} />,
      link: '/admin/email-templates',
      color: '#FF6B35',
    },
    {
      title: 'Content Management',
      description: 'Manage CMS pages and view revision history',
      icon: <IconFileText size={32} />,
      link: '/admin/cms/revisions',
      color: '#2E8B57',
    },
    {
      title: 'Reports',
      description: 'View analytics dashboards, transaction charts, and payment reports',
      icon: <IconChartBar size={32} />,
      link: '/admin/reports',
      color: '#228B22',
    },
    {
      title: 'Settings',
      description: 'Configure system settings, venues, and pricing',
      icon: <IconSettings size={32} />,
      link: '/admin/settings',
      color: '#8B8680',
    }
  ];

  // Filter cards based on the current user's role permissions
  const visibleCards = dashboardCards.filter(card => canSeeCard(card.title));

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Box mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Admin Dashboard
        </Title>
      </Box>

      {/* Dashboard Cards Grid — only shows cards the user's role permits */}
      <Grid gutter="xl" mb="xl">
        {visibleCards.map((card, index) => (
          <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 3 }}>
            <Paper
              shadow="sm"
              radius="md"
              component={Link}
              to={card.link}
              data-testid={card.testId}
              style={{
                background: '#FFF8F0',
                border: '1px solid rgba(136, 1, 36, 0.1)',
                transition: 'all 0.3s ease',
                textDecoration: 'none',
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
                cursor: 'pointer',
              }}
              p={{ base: 'lg', md: 'xl' }}
              onMouseEnter={(e) => {
                e.currentTarget.style.transform = 'translateY(-4px)';
                e.currentTarget.style.boxShadow = '0 8px 24px rgba(0,0,0,0.12)';
                e.currentTarget.style.borderColor = card.color;
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.transform = 'translateY(0)';
                e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,0,0,0.12)';
                e.currentTarget.style.borderColor = 'rgba(136, 1, 36, 0.1)';
              }}
            >
              {card.badge && (
                <Badge
                  size="sm"
                  variant="filled"
                  style={{
                    position: 'absolute',
                    top: '12px',
                    right: '12px',
                    background: card.color,
                    color: 'white',
                  }}
                >
                  {card.badge}
                </Badge>
              )}

              {/* Mobile: Icon inline with title, Desktop: Icon above title */}
              <Box
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '12px',
                  marginBottom: '12px',
                }}
              >
                <Box style={{ color: card.color, flexShrink: 0 }}>
                  {card.icon}
                </Box>
                <Title
                  order={3}
                  size="h4"
                  style={{ color: '#2B2B2B', marginBottom: 0 }}
                  hiddenFrom="md"
                >
                  {card.title}
                </Title>
              </Box>

              {/* Desktop-only title (below icon) */}
              <Title
                order={3}
                size="h4"
                mb="xs"
                style={{ color: '#2B2B2B' }}
                visibleFrom="md"
              >
                {card.title}
              </Title>

              {/* Desktop-only description */}
              <Text
                size="sm"
                c="dimmed"
                mb="md"
                style={{ flex: 1 }}
                visibleFrom="md"
              >
                {card.description}
              </Text>

              {/* Mobile: Consolidated count "6 - Upcoming Events" */}
              {card.count !== undefined && (
                <Text
                  size="h4"
                  c="dimmed"
                  hiddenFrom="md"
                  style={{
                    marginLeft: '44px', // Icon width (32px) + gap (12px) = 44px
                    padding: 0,
                    margin: 0,
                  }}
                >
                  {card.count} - {card.countLabel}
                </Text>
              )}

              {/* Desktop: Count box with number + label */}
              {card.count !== undefined && (
                <Box
                  p="sm"
                  style={{
                    background: 'rgba(136, 1, 36, 0.05)',
                    borderRadius: '6px',
                    marginTop: 'auto',
                  }}
                  visibleFrom="md"
                >
                  <Text size="xl" fw={700} style={{ color: card.color }}>
                    {card.count}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {card.countLabel}
                  </Text>
                </Box>
              )}
            </Paper>
          </Grid.Col>
        ))}
      </Grid>
    </Container>
  );
};
