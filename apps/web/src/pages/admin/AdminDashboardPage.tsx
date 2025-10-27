import React from 'react';
import { Container, Title, Text, Grid, Paper, Box, Group, Badge } from '@mantine/core';
import { IconCalendarEvent, IconUsers, IconSettings, IconChartBar, IconClipboardCheck, IconFileText, IconAlertTriangle } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { useVettingStats } from '../../features/admin/vetting/hooks/useVettingStats';
import { useSafetyDashboard } from '../../features/safety/hooks/useSafetyIncidents';
import { useEvents } from '../../lib/api/hooks/useEvents';

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

export const AdminDashboardPage: React.FC = () => {
  const { data: vettingStats } = useVettingStats();
  const { data: safetyDashboard } = useSafetyDashboard();
  const { data: events } = useEvents();

  // Calculate upcoming events (events with future start dates)
  const upcomingEventsCount = events
    ? events.filter(event => new Date(event.startDate) > new Date()).length
    : 0;

  // Calculate active incidents (not on hold or closed)
  // Active statuses: ReportSubmitted, InformationGathering, ReviewingFinalReport
  const activeIncidentsCount = safetyDashboard
    ? safetyDashboard.statistics.newCount +
      safetyDashboard.statistics.inProgressCount +
      safetyDashboard.statistics.reviewingFinalReportCount
    : 0;

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
      count: 156,
      countLabel: 'Active Members',
      link: '/admin/members',
      color: '#DAA520',
    },
    {
      title: 'Vetting Applications',
      description: 'Review and manage member vetting applications',
      icon: <IconClipboardCheck size={32} />,
      count: vettingStats?.underReviewCount ?? 0,
      countLabel: 'Under Review',
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
      title: 'Content Management',
      description: 'Manage CMS pages and view revision history',
      icon: <IconFileText size={32} />,
      link: '/admin/cms/revisions',
      color: '#FF6B35',
    },
    {
      title: 'Analytics',
      description: 'View attendance, revenue, and engagement metrics',
      icon: <IconChartBar size={32} />,
      link: '/admin/analytics',
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

      {/* Dashboard Cards Grid */}
      <Grid gutter="xl" mb="xl">
        {dashboardCards.map((card, index) => (
          <Grid.Col key={index} span={{ base: 12, sm: 6, lg: 3 }}>
            <Paper
              shadow="sm"
              p="xl"
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

              <Box style={{ color: card.color }} mb="md">
                {card.icon}
              </Box>

              <Title order={3} size="h4" mb="xs" style={{ color: '#2B2B2B' }}>
                {card.title}
              </Title>

              <Text size="sm" c="dimmed" mb="md" style={{ flex: 1 }}>
                {card.description}
              </Text>

              {card.count !== undefined && (
                <Box
                  p="sm"
                  style={{
                    background: 'rgba(136, 1, 36, 0.05)',
                    borderRadius: '6px',
                    marginTop: 'auto',
                  }}
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