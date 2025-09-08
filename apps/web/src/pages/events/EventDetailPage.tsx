import React from 'react';
import { useParams } from 'react-router-dom';
import { 
  Container, Stack, Title, Text, Breadcrumbs, 
  Anchor, Alert, Button 
} from '@mantine/core';

export const EventDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();

  return (
    <Container size="xl" py="md">
      <Stack gap="xl">
        {/* Breadcrumbs */}
        <Breadcrumbs separator="›" mt="md">
          <Anchor href="/">Home</Anchor>
          <Anchor href="/events">Events & Classes</Anchor>
          <Text>Event Details</Text>
        </Breadcrumbs>

        {/* Placeholder Content */}
        <Alert color="blue" title="Coming Soon">
          <Stack gap="sm">
            <Text>
              Event detail page for event ID: {id}
            </Text>
            <Text size="sm" c="dimmed">
              This page will be implemented in Week 2 with full event details,
              registration forms, and instructor information.
            </Text>
            <Button 
              component="a" 
              href="/events" 
              color="burgundy" 
              size="sm"
            >
              ← Back to Events List
            </Button>
          </Stack>
        </Alert>
      </Stack>
    </Container>
  );
};