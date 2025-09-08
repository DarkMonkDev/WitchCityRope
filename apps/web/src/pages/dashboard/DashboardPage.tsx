import React from 'react';
import { Box, Title, Text, Paper, Group, Button, Grid, Stack, Loader, Alert, SimpleGrid } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useCurrentUser } from '../../features/auth/api/queries';
import { useEvents } from '../../features/events/api/queries';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';
import { EventsWidget } from '../../components/dashboard/EventsWidget';
import { ProfileWidget } from '../../components/dashboard/ProfileWidget';
import { RegistrationHistory } from '../../components/dashboard/RegistrationHistory';
import { MembershipWidget } from '../../components/dashboard/MembershipWidget';
import type { EventDto } from '@witchcityrope/shared-types';

// Helper function to format event for display
const formatEventForDashboard = (event: EventDto) => {
  const startDate = new Date(event.startDateTime || '');
  const endDate = new Date(event.endDateTime || '');
  
  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };
  
  return {
    id: event.id,
    title: event.title,
    date: startDate.toISOString().split('T')[0], // Format as YYYY-MM-DD
    time: `${formatTime(startDate)} - ${formatTime(endDate)}`,
    status: event.status === 'Published' ? 'Open' : 'Closed',
    statusColor: event.status === 'Published' ? '#228B22' : '#DAA520',
  };
};

/**
 * Dashboard Landing Page
 * Shows user's upcoming events and quick actions
 * Uses real API data via TanStack Query hooks
 */
export const DashboardPage: React.FC = () => {
  const { data: user, isLoading: userLoading, error: userError } = useCurrentUser();
  const { data: events, isLoading: eventsLoading, error: eventsError } = useEvents();
  
  // Format events for dashboard display
  const upcomingEvents = events ? events
    .filter(event => event.startDateTime && new Date(event.startDateTime) > new Date()) // Only future events
    .sort((a, b) => {
      const dateA = new Date(a.startDateTime || 0).getTime();
      const dateB = new Date(b.startDateTime || 0).getTime();
      return dateA - dateB;
    }) // Sort by date
    .slice(0, 5) // Show only first 5
    .map(formatEventForDashboard) : [];

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const day = date.getDate();
    const month = date.toLocaleDateString('en-US', { month: 'short' });
    return { day, month };
  };

  return (
    <DashboardLayout>
      {/* Welcome Header */}
      <Box mb="xl">
        {userLoading ? (
          <Box style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Loader size="sm" color="#880124" />
            <Title
              order={1}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '28px',
                fontWeight: 800,
                color: '#880124',
                textTransform: 'uppercase',
                letterSpacing: '-0.5px',
              }}
            >
              Loading...
            </Title>
          </Box>
        ) : userError ? (
          <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
            Failed to load user information
          </Alert>
        ) : (
          <Title
            order={1}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '28px',
              fontWeight: 800,
              color: '#880124',
              textTransform: 'uppercase',
              letterSpacing: '-0.5px',
            }}
          >
            Welcome back, {user?.sceneName || 'User'}
          </Title>
        )}
      </Box>

      {/* Section Divider */}
      <Box
        style={{
          width: 'calc(100% + 24px)',
          height: '2px',
          background: 'linear-gradient(90deg, #880124, rgba(183, 109, 117, 0.3))',
          margin: '16px -24px 20px 0',
        }}
      />

      {/* Dashboard Cards Grid */}
      <SimpleGrid
        cols={{ base: 1, sm: 2 }}
        spacing="md"
        mb="xl"
      >
        <EventsWidget limit={3} />
        <ProfileWidget />
        <RegistrationHistory limit={4} showOnlyUpcoming={true} />
        <MembershipWidget />
      </SimpleGrid>

      {/* Quick Actions Section */}
      <Paper
        style={{
          background: '#FFF8F0',
          borderLeft: '3px solid #880124',
          padding: '16px',
          marginTop: '24px',
        }}
      >
        <Title
          order={3}
          mb="md"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '16px',
            fontWeight: 700,
            color: '#2B2B2B',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
          }}
        >
          Quick Actions
        </Title>

        <Grid>
          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Text
              component={Link}
              to="/events"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '8px',
                color: '#880124',
                textDecoration: 'none',
                borderRadius: '4px',
                transition: 'all 0.3s ease',
                fontSize: '14px',
                fontWeight: 500,
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = 'rgba(136, 1, 36, 0.05)';
                e.currentTarget.style.transform = 'translateX(2px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.transform = 'translateX(0)';
              }}
            >
              <span>📅</span>
              <span>Browse All Events</span>
            </Text>
          </Grid.Col>

          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Text
              component={Link}
              to="/dashboard/profile"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '8px',
                color: '#880124',
                textDecoration: 'none',
                borderRadius: '4px',
                transition: 'all 0.3s ease',
                fontSize: '14px',
                fontWeight: 500,
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = 'rgba(136, 1, 36, 0.05)';
                e.currentTarget.style.transform = 'translateX(2px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.transform = 'translateX(0)';
              }}
            >
              <span>👤</span>
              <span>Update Profile</span>
            </Text>
          </Grid.Col>

          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Text
              component={Link}
              to="/dashboard/membership"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '8px',
                color: '#880124',
                textDecoration: 'none',
                borderRadius: '4px',
                transition: 'all 0.3s ease',
                fontSize: '14px',
                fontWeight: 500,
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = 'rgba(136, 1, 36, 0.05)';
                e.currentTarget.style.transform = 'translateX(2px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.transform = 'translateX(0)';
              }}
            >
              <span>🎯</span>
              <span>Membership Status</span>
            </Text>
          </Grid.Col>

          <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
            <Text
              component={Link}
              to="/dashboard/security"
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '8px',
                padding: '8px',
                color: '#880124',
                textDecoration: 'none',
                borderRadius: '4px',
                transition: 'all 0.3s ease',
                fontSize: '14px',
                fontWeight: 500,
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = 'rgba(136, 1, 36, 0.05)';
                e.currentTarget.style.transform = 'translateX(2px)';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = 'transparent';
                e.currentTarget.style.transform = 'translateX(0)';
              }}
            >
              <span>🔒</span>
              <span>Security Settings</span>
            </Text>
          </Grid.Col>
        </Grid>
      </Paper>
    </DashboardLayout>
  );
};