import React, { useState, useMemo } from 'react';
import { Container, Title, Button, Group, Box, Text, Center, Stack, Loader, Alert } from '@mantine/core';
import { Link } from 'react-router-dom';
import { IconAlertCircle } from '@tabler/icons-react';
import { VettingAlertBox } from './components/VettingAlertBox';
import { FilterBar } from './components/FilterBar';
import { EventCard } from './components/EventCard';
import { EventTable } from './components/EventTable';
import { useUserEvents, useVettingStatus } from '../../hooks/useDashboard';
import { useUser } from '../../stores/authStore';
import { useUserVolunteerShifts } from '../../features/volunteers/hooks/useVolunteerPositions';
import type { UserEventDto, VettingStatusDto } from '../../types/dashboard.types';
import type { VolunteerShiftWithEvent } from '../../components/dashboard/UserVolunteerShifts';

/**
 * User Dashboard - My Events Page
 *
 * Features:
 * - Page title: "{FirstName} Dashboard" with Edit Profile button
 * - Conditional vetting alert box
 * - Filter bar with past events toggle, view mode, search
 * - Grid (default) or Table view of user's registered events
 * - NO pricing/capacity - user dashboard context
 */
export const MyEventsPage: React.FC = () => {
  const [showPast, setShowPast] = useState(false);
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('grid');
  const [searchQuery, setSearchQuery] = useState('');

  // Fetch user from auth store
  const user = useUser();

  // Fetch data using TanStack Query hooks
  const { data: events, isLoading: eventsLoading, error: eventsError } = useUserEvents(showPast);
  const { data: vettingStatus, isLoading: vettingLoading } = useVettingStatus();

  // Fetch user's volunteer shifts
  const { data: volunteerShiftsResponse } = useUserVolunteerShifts();

  // Transform volunteer shifts to dashboard format
  const volunteerShifts: VolunteerShiftWithEvent[] = useMemo(() => {
    if (!volunteerShiftsResponse || !Array.isArray(volunteerShiftsResponse)) return [];

    return volunteerShiftsResponse.map((shift) => ({
      id: shift.signupId || '',
      eventId: '', // Note: Backend doesn't provide eventId in UserVolunteerShiftDto yet
      eventTitle: shift.eventTitle || 'Event',
      eventStartDate: shift.eventDate || new Date().toISOString(),
      eventLocation: shift.eventLocation || 'TBD',
      positionTitle: shift.positionTitle || 'Volunteer',
      sessionName: shift.sessionName || undefined,
      sessionStartTime: shift.shiftStartTime || undefined,
      sessionEndTime: shift.shiftEndTime || undefined
    }));
  }, [volunteerShiftsResponse]);

  // Filter events
  const filteredEvents = useMemo(() => {
    if (!events || events.length === 0) {
      return [];
    }

    let filtered = events;

    // Filter past events
    if (!showPast) {
      filtered = filtered.filter((event) => !event.isPastEvent);
    }

    // Filter by search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(
        (event) =>
          event.title.toLowerCase().includes(query) ||
          event.location.toLowerCase().includes(query) ||
          event.description?.toLowerCase().includes(query)
      );
    }

    return filtered;
  }, [events, showPast, searchQuery]);

  // Show loading state
  if (eventsLoading || vettingLoading) {
    return (
      <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
        <Container size="xl" py="xl">
          <Center py="xl">
            <Stack align="center" gap="md">
              <Loader size="lg" color="burgundy" />
              <Text>Loading your dashboard...</Text>
            </Stack>
          </Center>
        </Container>
      </Box>
    );
  }

  // Show error state
  if (eventsError) {
    return (
      <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
        <Container size="xl" py="xl">
          <Alert
            icon={<IconAlertCircle />}
            color="red"
            title="Error Loading Events"
            mb="lg"
          >
            <Text>
              {eventsError instanceof Error
                ? eventsError.message
                : 'Failed to load your events. Please try again.'}
            </Text>
          </Alert>
        </Container>
      </Box>
    );
  }

  return (
    <Box style={{ background: 'var(--color-cream)', minHeight: '100vh' }} pb="xl">
      <Container size="xl" py="xl">
        {/* Page Title Bar */}
        <Group justify="space-between" mb="lg" wrap="wrap">
          <Title
            order={1}
            tt="uppercase"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
              fontSize: '2rem',
            }}
          >
            {user?.sceneName || 'Your'} Dashboard
          </Title>
          <Button
            component={Link}
            to="/dashboard/profile-settings"
            variant="filled"
            color="burgundy"
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                transition: 'all 0.3s ease',
                height: 'auto',
                paddingTop: '10px',
                paddingBottom: '10px',
                paddingLeft: '20px',
                paddingRight: '20px',
                lineHeight: '1.2',
                display: 'flex',
                alignItems: 'center',
                backgroundColor: 'var(--color-burgundy)',
                color: 'white',
                '&:hover': {
                  borderRadius: '6px 12px 6px 12px',
                  backgroundColor: 'var(--color-burgundy-dark)',
                  transform: 'translateY(-2px)',
                  boxShadow: '0 4px 12px rgba(183, 109, 117, 0.3)',
                },
              },
            }}
          >
            Edit Profile
          </Button>
        </Group>

        {/* Conditional Vetting Alert */}
        {vettingStatus && vettingStatus.status !== 'Vetted' && (
          <VettingAlertBox status={vettingStatus} />
        )}

        {/* Filter Bar */}
        <FilterBar
          showPast={showPast}
          onShowPastChange={setShowPast}
          viewMode={viewMode}
          onViewModeChange={setViewMode}
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
        />

        {/* Event Display */}
        {filteredEvents.length === 0 ? (
          <EmptyEventsState />
        ) : viewMode === 'grid' ? (
          <Box
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
              gap: 'var(--space-lg)',
            }}
          >
            {filteredEvents.map((event) => (
              <EventCard
                key={event.id}
                event={event}
                volunteerShifts={volunteerShifts}
                className={event.isPastEvent ? 'past-event' : ''}
              />
            ))}
          </Box>
        ) : (
          <EventTable events={filteredEvents} />
        )}
      </Container>
    </Box>
  );
};

// Empty state component
const EmptyEventsState: React.FC = () => (
  <Center py="xl">
    <Stack align="center" gap="md">
      <Box
        style={{
          width: '120px',
          height: '120px',
          background: 'linear-gradient(135deg, var(--color-ivory) 0%, var(--color-cream) 100%)',
          border: '2px solid var(--color-rose-gold)',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '48px',
          color: 'var(--color-burgundy)',
          boxShadow: '0 4px 15px rgba(183, 109, 117, 0.2)',
          marginBottom: 'var(--space-lg)',
        }}
      >
        📅
      </Box>
      <Title
        order={3}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '28px',
          fontWeight: 700,
          color: 'var(--color-charcoal)',
          marginBottom: 'var(--space-sm)',
        }}
      >
        No Events Found
      </Title>
      <Text ta="center" style={{ fontSize: '18px', marginBottom: 'var(--space-xl)' }}>
        You haven't registered for any events yet.
      </Text>
      <Button
        component={Link}
        to="/events"
        variant="outline"
        color="burgundy"
        styles={{
          root: {
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'var(--font-heading)',
            fontWeight: 600,
            textTransform: 'uppercase',
            height: 'auto',
            paddingTop: '12px',
            paddingBottom: '12px',
            paddingLeft: '24px',
            paddingRight: '24px',
            lineHeight: '1.2',
            display: 'flex',
            alignItems: 'center',
          },
        }}
      >
        Browse Events
      </Button>
    </Stack>
  </Center>
);
