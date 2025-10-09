import React, { useState, useMemo } from 'react';
import { Container, Title, Button, Group, Box, Text, Center, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';
import { VettingAlertBox } from './components/VettingAlertBox';
import { FilterBar } from './components/FilterBar';
import { EventCard } from './components/EventCard';
import { EventTable } from './components/EventTable';
import type { UserEventDto, VettingStatusDto } from '../../types/dashboard.types';

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

  // TODO: Replace with actual API calls using TanStack Query
  // const { data: events, isLoading, error } = useQuery({
  //   queryKey: ['user-events', showPast],
  //   queryFn: () => dashboardService.getUserEvents(user.id, showPast)
  // });
  // const { data: vettingStatus } = useQuery({
  //   queryKey: ['vetting-status'],
  //   queryFn: () => dashboardService.getVettingStatus(user.id)
  // });

  // Mock data for development - replace with actual API data
  const mockEvents: UserEventDto[] = [
    {
      id: '1',
      title: 'Halloween Rope Social',
      startDate: new Date(2025, 9, 31, 19, 0).toISOString(),
      endDate: new Date(2025, 9, 31, 22, 0).toISOString(),
      location: 'Salem Community Center',
      description: 'Join us for a spooky evening of rope bondage and Halloween fun!',
      registrationStatus: 'RSVP Confirmed',
      isSocialEvent: true,
      hasTicket: false,
      isPastEvent: false,
    },
    {
      id: '2',
      title: 'Single Column Ties Workshop',
      startDate: new Date(2025, 10, 8, 14, 0).toISOString(),
      endDate: new Date(2025, 10, 8, 17, 0).toISOString(),
      location: 'Rope Studio',
      description: 'Learn fundamental single column tie techniques for rope bondage.',
      registrationStatus: 'Ticket Purchased',
      isSocialEvent: false,
      hasTicket: true,
      isPastEvent: false,
    },
    {
      id: '3',
      title: 'Suspension Safety Workshop',
      startDate: new Date(2025, 8, 15, 15, 0).toISOString(),
      endDate: new Date(2025, 8, 15, 18, 0).toISOString(),
      location: 'Rope Studio',
      description: 'Comprehensive safety training for rope suspension.',
      registrationStatus: 'Attended',
      isSocialEvent: false,
      hasTicket: true,
      isPastEvent: true,
    },
  ];

  const mockVettingStatus: VettingStatusDto = {
    status: 'Pending',
    lastUpdatedAt: new Date().toISOString(),
    message: '',
    interviewScheduleUrl: null,
    reapplyInfoUrl: null,
  };

  // Mock user data - replace with actual auth context
  const mockUser = {
    firstName: 'ShadowKnot',
  };

  // Filter events
  const filteredEvents = useMemo(() => {
    let filtered = mockEvents;

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
  }, [mockEvents, showPast, searchQuery]);

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
            {mockUser.firstName} Dashboard
          </Title>
          <Button
            component={Link}
            to="/dashboard/profile-settings"
            variant="outline"
            color="rose-gold"
            style={{
              borderRadius: '12px 6px 12px 6px',
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '1px',
              transition: 'all 0.3s ease',
            }}
            styles={{
              root: {
                '&:hover': {
                  borderRadius: '6px 12px 6px 12px',
                },
              },
            }}
          >
            Edit Profile
          </Button>
        </Group>

        {/* Conditional Vetting Alert */}
        <VettingAlertBox status={mockVettingStatus} />

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
        style={{
          borderRadius: '12px 6px 12px 6px',
          fontFamily: 'var(--font-heading)',
          fontWeight: 600,
          textTransform: 'uppercase',
        }}
      >
        Browse Events
      </Button>
    </Stack>
  </Center>
);
