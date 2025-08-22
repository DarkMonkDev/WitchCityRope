import React, { useState, useEffect } from 'react';
import { Box, Container, SimpleGrid, Text, Title, Button, Alert } from '@mantine/core';
import { Link } from 'react-router-dom';
import { Event } from '../../types/Event';
import { EventCard } from './EventCard';

interface EventsListProps {
  /** Section title */
  title?: string;
  /** Maximum number of events to display */
  maxEvents?: number;
  /** Show "View Full Calendar" button */
  showViewMore?: boolean;
  /** Custom loading state */
  isLoading?: boolean;
  /** Custom error state */
  error?: string | null;
  /** Custom events data */
  events?: Event[];
}

// Mock data with additional v7 styling properties
const mockEventsData = [
  {
    id: '1',
    title: 'Rope Fundamentals',
    description: 'Learn fundamental ties and safety practices in a supportive environment. Perfect for your first class or building strong foundations.',
    startDate: '2024-03-15T14:00:00Z',
    location: 'Salem Studio',
    price: '$35-55',
    status: { type: 'available' as const, text: '10 spots left' },
    details: { duration: '2.5 hours', level: 'Beginner', spots: 'Salem Studio' }
  },
  {
    id: '2',
    title: 'March Rope Jam',
    description: 'Practice your skills and connect with fellow rope enthusiasts in our welcoming community space. For vetted members.',
    startDate: '2024-03-21T19:00:00Z',
    location: 'Community Space',
    price: '$0-35',
    status: { type: 'available' as const, text: 'Open' },
    details: { duration: '3 hours', level: 'Members Only', spots: 'Community Space' }
  },
  {
    id: '3',
    title: 'Suspension Intensive',
    description: 'Take your skills to new heights! This workshop explores suspension basics with a strong focus on safety. Prerequisites apply.',
    startDate: '2024-03-23T13:00:00Z',
    location: 'Salem Studio',
    price: '$95',
    status: { type: 'limited' as const, text: '3 spots left' },
    details: { duration: '4 hours', level: 'Advanced', spots: 'Salem Studio' }
  },
  {
    id: '4',
    title: 'Intimacy & Rope',
    description: 'Explore the deeper connections possible through rope. This class focuses on communication, intimacy, and building trust.',
    startDate: '2024-03-29T18:00:00Z',
    location: 'Private Studio',
    price: '$75-95',
    status: { type: 'available' as const, text: '8 spots left' },
    details: { duration: '3 hours', level: 'Couples Welcome', spots: 'Private Studio' }
  }
];

export const EventsList: React.FC<EventsListProps> = ({
  title = "Upcoming Classes & Events",
  maxEvents = 4,
  showViewMore = true,
  isLoading: customLoading,
  error: customError,
  events: customEvents
}) => {
  const [events, setEvents] = useState<Event[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // If custom data is provided, use it
    if (customEvents) {
      setEvents(customEvents);
      setLoading(false);
      return;
    }

    const fetchEvents = async () => {
      try {
        // First try real API, fallback to mock data
        const response = await fetch('http://localhost:5655/api/events');
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        setEvents(data);
      } catch (err) {
        console.log('Using mock data for v7 design demonstration');
        // Use mock data for v7 design showcase
        setEvents(mockEventsData);
      } finally {
        setLoading(false);
      }
    };

    fetchEvents();
  }, [customEvents]);

  // Use custom states if provided
  const isLoading = customLoading ?? loading;
  const errorState = customError ?? error;

  if (isLoading) {
    return (
      <Box style={{ textAlign: 'center', padding: 'var(--space-xl)' }}>
        <Text>Loading events...</Text>
      </Box>
    );
  }

  if (errorState) {
    return (
      <Alert color="red" style={{ margin: 'var(--space-lg)' }}>
        <strong>Error:</strong> {errorState}
      </Alert>
    );
  }

  const displayEvents = events.slice(0, maxEvents);

  if (displayEvents.length === 0) {
    return (
      <Box style={{ textAlign: 'center', padding: 'var(--space-xl)' }}>
        <Title order={3} style={{ marginBottom: 'var(--space-sm)' }}>
          No events available
        </Title>
        <Text style={{ color: 'var(--color-stone)' }}>
          Check back soon for new classes and events.
        </Text>
      </Box>
    );
  }

  return (
    <Box
      component="section"
      style={{
        padding: 'var(--space-2xl) 40px',
        maxWidth: '1200px',
        margin: '0 auto',
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        marginTop: 'var(--space-2xl)',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
      }}
    >
      <Title
        order={2}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '48px',
          fontWeight: 800,
          marginBottom: 'var(--space-xl)',
          textAlign: 'center',
          color: 'var(--color-burgundy)',
          position: 'relative',
          textTransform: 'uppercase',
          letterSpacing: '3px',
        }}
      >
        {title}
        <Box
          style={{
            content: '""',
            display: 'block',
            width: '100px',
            height: '2px',
            background: 'linear-gradient(90deg, transparent, var(--color-rose-gold), transparent)',
            margin: 'var(--space-sm) auto 0',
          }}
        />
      </Title>

      <SimpleGrid
        cols={{ base: 1, sm: 2, lg: 2 }}
        spacing="lg"
        style={{ marginBottom: 'var(--space-xl)' }}
      >
        {displayEvents.map((event) => (
          <EventCard
            key={event.id}
            event={event}
            // Add v7 styling props if they exist in mock data
            {...(event as any).price && { price: (event as any).price }}
            {...(event as any).status && { status: (event as any).status }}
            {...(event as any).details && { details: (event as any).details }}
            onClick={() => {
              // Handle event click - could navigate to event details
              console.log('Event clicked:', event.id);
            }}
          />
        ))}
      </SimpleGrid>

      {showViewMore && (
        <Box style={{ textAlign: 'center', marginTop: 'var(--space-xl)' }}>
          <Button
            component={Link}
            to="/events"
            variant="v7-secondary"
          >
            View Full Calendar
          </Button>
        </Box>
      )}
    </Box>
  );
};