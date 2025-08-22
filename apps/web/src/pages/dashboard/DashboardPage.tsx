import React from 'react';
import { Box, Title, Text, Paper, Group, Button, Grid, Stack, Loader, Alert } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useCurrentUser } from '../../features/auth/api/queries';
import { useEvents } from '../../features/events/api/queries';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';
import type { Event } from '../../types/api.types';

// Helper function to format event for display
const formatEventForDashboard = (event: Event) => {
  const startDate = new Date(event.startDate);
  const endDate = new Date(event.endDate);
  
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
    status: event.isRegistrationOpen ? 'Open' : 'Closed',
    statusColor: event.isRegistrationOpen ? '#228B22' : '#DAA520',
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
    .filter(event => new Date(event.startDate) > new Date()) // Only future events
    .sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime()) // Sort by date
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

      {/* Events Section */}
      <Box mb="xl">
        <Title
          order={2}
          mb="lg"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '20px',
            fontWeight: 700,
            color: '#2B2B2B',
            textTransform: 'uppercase',
            letterSpacing: '0.5px',
          }}
        >
          Your Upcoming Events
        </Title>

        {eventsLoading ? (
          <Box style={{ textAlign: 'center', padding: '40px' }}>
            <Loader size="lg" color="#880124" />
            <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading your upcoming events...</Text>
          </Box>
        ) : eventsError ? (
          <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
            Failed to load events. Please try refreshing the page.
          </Alert>
        ) : upcomingEvents.length > 0 ? (
          <Stack gap="sm">
            {upcomingEvents.map((event) => {
              const { day, month } = formatDate(event.date);
              return (
                <Paper
                  key={event.id}
                  style={{
                    background: '#FFF8F0',
                    padding: '12px 16px',
                    borderLeft: '3px solid #880124',
                    display: 'grid',
                    gridTemplateColumns: '80px 1fr 120px 100px 80px',
                    gap: '16px',
                    alignItems: 'center',
                    transition: 'all 0.3s ease',
                    minHeight: '60px',
                    borderRadius: '0',
                  }}
                  onMouseEnter={(e) => {
                    e.currentTarget.style.transform = 'translateX(2px)';
                    e.currentTarget.style.boxShadow = '0 1px 8px rgba(0,0,0,0.1)';
                  }}
                  onMouseLeave={(e) => {
                    e.currentTarget.style.transform = 'translateX(0)';
                    e.currentTarget.style.boxShadow = 'none';
                  }}
                >
                  {/* Date */}
                  <Box
                    style={{
                      background: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
                      color: '#FFF8F0',
                      padding: '8px',
                      borderRadius: '6px',
                      textAlign: 'center',
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'center',
                    }}
                  >
                    <Text
                      style={{
                        fontFamily: "'Montserrat', sans-serif",
                        fontSize: '18px',
                        fontWeight: 800,
                        lineHeight: 1,
                      }}
                    >
                      {day}
                    </Text>
                    <Text
                      style={{
                        fontSize: '10px',
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        marginTop: '2px',
                      }}
                    >
                      {month}
                    </Text>
                  </Box>

                  {/* Title */}
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontSize: '16px',
                      fontWeight: 700,
                      color: '#2B2B2B',
                    }}
                  >
                    {event.title}
                  </Text>

                  {/* Time */}
                  <Text
                    style={{
                      fontSize: '14px',
                      color: '#8B8680',
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 500,
                    }}
                  >
                    {event.time}
                  </Text>

                  {/* Status */}
                  <Box
                    style={{
                      padding: '4px 10px',
                      borderRadius: '16px',
                      fontSize: '11px',
                      fontWeight: 600,
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                      textAlign: 'center',
                      background: `${event.statusColor}20`,
                      color: event.statusColor,
                      border: `1px solid ${event.statusColor}`,
                    }}
                  >
                    {event.status}
                  </Box>

                  {/* Action */}
                  <Box style={{ textAlign: 'center' }}>
                    <Text
                      component="a"
                      href="#"
                      style={{
                        fontSize: '12px',
                        color: '#880124',
                        textDecoration: 'none',
                        fontWeight: 600,
                        textTransform: 'uppercase',
                        letterSpacing: '0.5px',
                        padding: '4px 8px',
                        border: '1px solid #880124',
                        borderRadius: '4px',
                        transition: 'all 0.3s ease',
                      }}
                      onMouseEnter={(e) => {
                        e.currentTarget.style.background = '#880124';
                        e.currentTarget.style.color = '#FFF8F0';
                      }}
                      onMouseLeave={(e) => {
                        e.currentTarget.style.background = 'transparent';
                        e.currentTarget.style.color = '#880124';
                      }}
                    >
                      Details
                    </Text>
                  </Box>
                </Paper>
              );
            })}
          </Stack>
        ) : (
          <Paper
            style={{
              textAlign: 'center',
              padding: '24px 16px',
              color: '#8B8680',
              background: '#FFF8F0',
              borderRadius: '6px',
            }}
          >
            <Text style={{ fontSize: '36px', marginBottom: '12px', opacity: 0.5 }}>
              📅
            </Text>
            <Title
              order={3}
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontSize: '18px',
                fontWeight: 600,
                color: '#4A4A4A',
                marginBottom: '8px',
              }}
            >
              No upcoming events
            </Title>
            <Text style={{ fontSize: '14px', marginBottom: '16px' }}>
              Browse our events and sign up for classes and community gatherings.
            </Text>
          </Paper>
        )}
      </Box>

      {/* Quick Links */}
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