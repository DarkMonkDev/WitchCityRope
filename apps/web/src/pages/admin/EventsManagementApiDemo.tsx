/**
 * Events Management API Demo
 * Demonstrates the integration between frontend and backend APIs
 * Using real API calls instead of mock data
 * @created 2025-09-06
 */

import React, { useState } from 'react';
import { 
  Container, 
  Title, 
  Paper, 
  Group, 
  Button, 
  Text, 
  Card, 
  Badge, 
  Stack,
  Grid,
  Alert,
  Loader,
  Tabs,
  Table
} from '@mantine/core';
import { IconInfoCircle, IconCalendar, IconUsers, IconCurrency } from '@tabler/icons-react';
import { useEventsManagement, useEventDetails, useEventAvailability } from '../../hooks/useEventsManagement';
import { useLegacyEvents, useLegacyEventDetails } from '../../hooks/useLegacyEventsApi';
import { apiClient } from '../../lib/api/client';
import type { EventSummaryDto } from '@witchcityrope/shared-types';
import type { LegacyEventDto } from '../../api/services/legacyEventsApi.service';

export const EventsManagementApiDemo: React.FC = () => {
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState('current-api');
  
  // Current API (working)
  const { 
    data: legacyEvents, 
    isLoading: legacyEventsLoading, 
    error: legacyEventsError,
    refetch: refetchLegacyEvents
  } = useLegacyEvents();
  
  const { 
    data: legacyEventDetails, 
    isLoading: legacyDetailsLoading, 
    error: legacyDetailsError 
  } = useLegacyEventDetails(selectedEventId || '', !!selectedEventId && activeTab === 'current-api');
  
  // Future API (Events Management DTOs - for demonstration) - DISABLED to prevent errors
  const { 
    data: events, 
    isLoading: eventsLoading, 
    error: eventsError,
    refetch: refetchEvents
  } = useEventsManagement({}, { enabled: false }); // Disabled - endpoints don't exist yet
  
  const { 
    data: eventDetails, 
    isLoading: detailsLoading, 
    error: detailsError 
  } = useEventDetails(selectedEventId || '', false); // Disabled - endpoints don't exist yet
  
  const { 
    data: eventAvailability, 
    isLoading: availabilityLoading, 
    error: availabilityError 
  } = useEventAvailability(selectedEventId || '', false); // Disabled - endpoints don't exist yet

  const handleEventSelect = (eventId: string) => {
    setSelectedEventId(eventId === selectedEventId ? null : eventId);
  };

  return (
    <Container size="xl" py="xl">
      <Title order={1} ta="center" mb="xl" c="burgundy">
        Events Management API Integration Demo
      </Title>

      <Stack spacing="xl">
        {/* API Selector */}
        <Paper shadow="sm" radius="md" p="lg">
          <Title order={2} c="burgundy" mb="md">API Integration Demo</Title>
          <Tabs value={activeTab} onTabChange={setActiveTab}>
            <Tabs.List>
              <Tabs.Tab value="current-api">Current API (Working)</Tabs.Tab>
              <Tabs.Tab value="future-api">Future Events Management API</Tabs.Tab>
            </Tabs.List>

            <Tabs.Panel value="current-api" pt="md">
              <Alert icon={<IconInfoCircle size="1rem" />} color="blue" mb="md">
                <strong>Current API Integration:</strong> This demonstrates the working API integration with the existing backend endpoints.
              </Alert>
              {/* Current API Events */}
              <Group justify="space-between" mb="md">
                <Title order={3} c="dark">Published Events (Current API)</Title>
                <Button variant="light" onClick={refetchLegacyEvents}>
                  Refresh Events
                </Button>
              </Group>

              {legacyEventsLoading && (
                <Group justify="center" p="xl">
                  <Loader size="lg" />
                  <Text>Loading events...</Text>
                </Group>
              )}

              {legacyEventsError && (
                <Alert icon={<IconInfoCircle size="1rem" />} color="red" mb="md">
                  Error loading events: {legacyEventsError.message}
                </Alert>
              )}

              {legacyEvents && legacyEvents.length > 0 && (
                <Grid>
                  {legacyEvents.map((event: LegacyEventDto) => (
                    <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
                      <Card 
                        shadow="sm" 
                        padding="lg" 
                        radius="md" 
                        withBorder
                        style={{ 
                          cursor: 'pointer',
                          border: selectedEventId === event.id ? '2px solid var(--mantine-color-burgundy-6)' : undefined
                        }}
                        onClick={() => handleEventSelect(event.id)}
                      >
                        <Stack mt="md" spacing="xs">
                          <Title order={4}>{event.title}</Title>
                          <Text size="sm" c="dimmed" lineClamp={2}>
                            {event.description}
                          </Text>
                          
                          <Group gap="xs" mt="xs">
                            <IconCalendar size="1rem" />
                            <Text size="xs">
                              {new Date(event.startDate).toLocaleDateString()}
                            </Text>
                          </Group>
                          
                          <Text size="xs" c="dimmed">
                            Location: {event.location}
                          </Text>
                        </Stack>
                      </Card>
                    </Grid.Col>
                  ))}
                </Grid>
              )}

              {selectedEventId && legacyEventDetails && (
                <Paper shadow="sm" radius="md" p="lg" mt="xl">
                  <Title order={3} mb="md">Selected Event Details</Title>
                  <Stack spacing="md">
                    <div>
                      <Title order={4}>{legacyEventDetails.title}</Title>
                      <Text c="dimmed">{legacyEventDetails.description}</Text>
                    </div>
                    <Group>
                      <Text fw={500}>Date:</Text>
                      <Text>{new Date(legacyEventDetails.startDate).toLocaleString()}</Text>
                    </Group>
                    <Group>
                      <Text fw={500}>Location:</Text>
                      <Text>{legacyEventDetails.location}</Text>
                    </Group>
                    <Text size="xs" c="dimmed" mt="md">
                      <strong>Integration Status:</strong> Successfully connected to backend API at {apiClient.defaults.baseURL}
                    </Text>
                  </Stack>
                </Paper>
              )}
            </Tabs.Panel>

            <Tabs.Panel value="future-api" pt="md">
              <Alert icon={<IconInfoCircle size="1rem" />} color="orange" mb="md">
                <strong>Future Events Management API:</strong> This shows the TypeScript types and hooks ready for the new EventSummaryDto, EventDetailsDto, and EventAvailabilityDto endpoints. 
                <br /><strong>Note:</strong> API calls are disabled to prevent errors since these endpoints are not yet implemented.
              </Alert>
              
              {eventsLoading && (
                <Group justify="center" p="xl">
                  <Loader size="lg" />
                  <Text>Loading events...</Text>
                </Group>
              )}

              {eventsError && (
                <Alert icon={<IconInfoCircle size="1rem" />} color="red" mb="md">
                  Error loading events: {eventsError.message}
                </Alert>
              )}

              <Paper p="lg" bg="gray.0" mt="md">
                <Title order={4} mb="md">Events Management Types Preview</Title>
                <Text size="sm" mb="md">These TypeScript types are ready for the Events Management API:</Text>
                
                <Stack spacing="md">
                  <div>
                    <Text fw={500} size="sm" mb="xs">EventSummaryDto:</Text>
                    <Text size="xs" c="dimmed" style={{ fontFamily: 'monospace' }}>
                      eventId, title, shortDescription, eventType, startDate, endDate, venue, totalCapacity, availableSpots, hasMultipleSessions, sessionCount, lowestPrice, highestPrice, isPublished, createdAt, updatedAt
                    </Text>
                  </div>
                  
                  <div>
                    <Text fw={500} size="sm" mb="xs">EventDetailsDto:</Text>
                    <Text size="xs" c="dimmed" style={{ fontFamily: 'monospace' }}>
                      Full event details + VenueDto + EventOrganizerDto[] + EventSessionDto[] + TicketTypeDto[]
                    </Text>
                  </div>
                  
                  <div>
                    <Text fw={500} size="sm" mb="xs">EventAvailabilityDto:</Text>
                    <Text size="xs" c="dimmed" style={{ fontFamily: 'monospace' }}>
                      Real-time availability + SessionAvailabilityDto[] + TicketTypeAvailabilityDto[]
                    </Text>
                  </div>
                </Stack>
                
                <Alert color="green" mt="md">
                  <strong>Integration Ready:</strong> All TypeScript types, API service methods, and TanStack Query hooks are implemented and ready to connect to the Events Management endpoints once they're fully deployed.
                </Alert>
              </Paper>
            </Tabs.Panel>
          </Tabs>
        </Paper>
      </Stack>
    </Container>
  );
};