import React, { useState } from 'react';
import { Box, Title, Text, Paper, Grid, Button, Loader, Alert, Badge, ActionIcon, Modal } from '@mantine/core';
import { IconEdit, IconTrash, IconPlus, IconEye } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useEvents } from '../../features/events/api/queries';
import { useCreateEvent, useUpdateEvent, useDeleteEvent } from '../../features/events/api/mutations';
import { EventForm, type EventFormData } from '../../components/events/EventForm';
import type { EventDto } from '@witchcityrope/shared-types';

// Helper function to format event dates and status
const formatEventDisplay = (event: EventDto) => {
  const startDate = new Date(event.startDateTime || '');
  const endDate = new Date(event.endDateTime || '');
  const now = new Date();
  
  const formatDate = (date: Date) => {
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  const formatTime = (date: Date) => {
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

  let status = 'upcoming';
  let statusColor = '#228B22';
  
  if (startDate > now) {
    status = 'upcoming';
    statusColor = '#228B22';
  } else if (endDate < now) {
    status = 'completed';
    statusColor = '#8B8680';
  } else {
    status = 'in progress';
    statusColor = '#DAA520';
  }

  return {
    ...event,
    formattedDate: formatDate(startDate),
    formattedTime: `${formatTime(startDate)} - ${formatTime(endDate)}`,
    status,
    statusColor,
    isPast: endDate < now
  };
};

/**
 * AdminEventsPage - Administrative interface for managing events
 * Route: /admin/events (requires authentication and admin role)
 */
export const AdminEventsPage: React.FC = () => {
  const { data: events, isLoading, error } = useEvents();
  
  // Modal state management
  const [eventModalOpen, setEventModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [editingEvent, setEditingEvent] = useState<EventDto | null>(null);
  const [eventToDelete, setEventToDelete] = useState<EventDto | null>(null);

  // Mutations
  const createEventMutation = useCreateEvent();
  const updateEventMutation = useUpdateEvent();
  const deleteEventMutation = useDeleteEvent();

  // Helper function to convert EventFormData to API format
  const convertFormDataToApiFormat = (formData: EventFormData) => {
    // Convert the form data to the format expected by the API
    return {
      title: formData.title,
      description: formData.fullDescription,
      // Map the form dates to API format - you may need to adjust this based on your actual API structure
      startDate: formData.sessions?.[0]?.startTime || new Date().toISOString(),
      endDate: formData.sessions?.[0]?.endTime || new Date().toISOString(),
      maxAttendees: formData.sessions?.[0]?.capacity || 0,
      location: formData.venueId,
    };
  };

  const handleCreateEvent = () => {
    setEditingEvent(null);
    setEventModalOpen(true);
  };

  const handleEditEvent = (eventId: string) => {
    const eventToEdit = events?.find(e => e.id === eventId);
    if (eventToEdit) {
      setEditingEvent(eventToEdit);
      setEventModalOpen(true);
    }
  };

  const handleDeleteEvent = (eventId: string) => {
    const eventToDelete = events?.find(e => e.id === eventId);
    if (eventToDelete) {
      setEventToDelete(eventToDelete);
      setDeleteModalOpen(true);
    }
  };

  const handleViewEvent = (eventId: string) => {
    window.open(`/events/${eventId}`, '_blank');
  };

  const handleSubmitEvent = async (formData: EventFormData) => {
    try {
      const apiData = convertFormDataToApiFormat(formData);
      
      if (editingEvent) {
        // Update existing event
        await updateEventMutation.mutateAsync({
          id: editingEvent.id!,
          data: apiData
        });
        notifications.show({
          title: 'Success',
          message: 'Event updated successfully!',
          color: 'green',
        });
      } else {
        // Create new event
        await createEventMutation.mutateAsync(apiData);
        notifications.show({
          title: 'Success', 
          message: 'Event created successfully!',
          color: 'green',
        });
      }
      
      setEventModalOpen(false);
      setEditingEvent(null);
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: editingEvent 
          ? 'Failed to update event. Please try again.'
          : 'Failed to create event. Please try again.',
        color: 'red',
      });
    }
  };

  const handleCancelEvent = () => {
    setEventModalOpen(false);
    setEditingEvent(null);
  };

  const confirmDeleteEvent = async () => {
    if (!eventToDelete?.id) return;
    
    try {
      await deleteEventMutation.mutateAsync(eventToDelete.id);
      notifications.show({
        title: 'Success',
        message: 'Event deleted successfully!',
        color: 'green',
      });
      setDeleteModalOpen(false);
      setEventToDelete(null);
    } catch (error) {
      notifications.show({
        title: 'Error',
        message: 'Failed to delete event. Please try again.',
        color: 'red',
      });
    }
  };

  const cancelDeleteEvent = () => {
    setDeleteModalOpen(false);
    setEventToDelete(null);
  };

  // Process events for admin view
  const adminEvents = events ? events.map(formatEventDisplay) : [];

  if (isLoading) {
    return (
      <Box p="xl" style={{ minHeight: '400px' }}>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Event Management
        </Title>
        
        <Box style={{ textAlign: 'center', padding: '40px' }}>
          <Loader size="lg" color="#880124" />
          <Text style={{ marginTop: '16px', color: '#8B8680' }}>Loading events...</Text>
        </Box>
      </Box>
    );
  }

  if (error) {
    return (
      <Box p="xl">
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '32px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Event Management
        </Title>
        
        <Alert color="red" style={{ background: 'rgba(220, 20, 60, 0.1)', border: '1px solid rgba(220, 20, 60, 0.3)' }}>
          Failed to load events. Please try refreshing the page.
        </Alert>
      </Box>
    );
  }

  return (
    <Box p="xl">
      <Box style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px' }}>
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
          Event Management
        </Title>

        <Button
          leftSection={<IconPlus size={16} />}
          onClick={handleCreateEvent}
          style={{
            background: '#880124',
            color: 'white',
            fontWeight: 600,
            fontSize: '16px',
            height: '48px',
            paddingLeft: '24px',
            paddingRight: '24px'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = '#6B0119';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = '#880124';
          }}
        >
          Create Event
        </Button>
      </Box>

      <Text
        mb="xl"
        style={{
          fontSize: '16px',
          color: '#8B8680',
          maxWidth: '600px'
        }}
      >
        Manage all events, workshops, and community gatherings. Create new events, edit existing ones, and monitor registrations.
      </Text>

      {/* Events List */}
      {adminEvents.length > 0 ? (
        <Grid>
          {adminEvents.map((event) => (
            <Grid.Col key={event.id} span={{ base: 12, md: 6, lg: 4 }}>
              <Paper
                className="admin-event-card"
                data-testid="admin-event"
                data-event-id={event.id}
                style={{
                  background: '#FFF8F0',
                  padding: '24px',
                  borderLeft: '4px solid #880124',
                  transition: 'all 0.3s ease',
                  height: '100%',
                  display: 'flex',
                  flexDirection: 'column',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-2px)';
                  e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.1)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = 'none';
                }}
              >
                {/* Status and Actions Header */}
                <Box style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '16px' }}>
                  <Badge
                    size="sm"
                    style={{
                      background: event.status === 'Published' ? '#228B2220' : '#8B868020',
                      color: event.status === 'Published' ? '#228B22' : '#8B8680',
                      border: `1px solid ${event.status === 'Published' ? '#228B22' : '#8B8680'}`,
                      textTransform: 'uppercase',
                      letterSpacing: '0.5px',
                    }}
                  >
                    {event.status}
                  </Badge>

                  <Box style={{ display: 'flex', gap: '8px' }}>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleViewEvent(event.id)}
                      style={{ color: '#8B8680' }}
                      title="View Event"
                    >
                      <IconEye size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleEditEvent(event.id)}
                      style={{ color: '#880124' }}
                      title="Edit Event"
                    >
                      <IconEdit size={16} />
                    </ActionIcon>
                    <ActionIcon
                      variant="subtle"
                      size="sm"
                      onClick={() => handleDeleteEvent(event.id)}
                      style={{ color: '#DC143C' }}
                      title="Delete Event"
                    >
                      <IconTrash size={16} />
                    </ActionIcon>
                  </Box>
                </Box>

                <Title
                  order={3}
                  mb="sm"
                  style={{
                    fontFamily: "'Montserrat', sans-serif",
                    fontSize: '18px',
                    fontWeight: 700,
                    color: '#2B2B2B',
                    lineHeight: 1.3,
                  }}
                >
                  {event.title}
                </Title>

                <Text
                  mb="md"
                  style={{
                    color: '#8B8680',
                    fontSize: '14px',
                    lineHeight: 1.5,
                    flex: 1,
                  }}
                >
                  {event.description || 'No description provided'}
                </Text>

                {/* Event Details */}
                <Box
                  style={{
                    background: 'rgba(183, 109, 117, 0.05)',
                    padding: '16px',
                    borderRadius: '6px',
                    marginTop: 'auto',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 600,
                      fontSize: '13px',
                      color: '#2B2B2B',
                      marginBottom: '4px',
                    }}
                  >
                    📅 {event.formattedDate}
                  </Text>
                  <Text
                    style={{
                      fontFamily: "'Montserrat', sans-serif",
                      fontWeight: 500,
                      fontSize: '13px',
                      color: '#8B8680',
                      marginBottom: '8px'
                    }}
                  >
                    🕐 {event.formattedTime}
                  </Text>
                  
                  {/* Attendance and Capacity */}
                  <Box style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                      👥 {event.currentAttendees || 0}/{event.capacity || 0}
                    </Text>
                    <Text style={{ fontSize: '12px', color: '#8B8680' }}>
                      ID: {event.id.slice(0, 8)}...
                    </Text>
                  </Box>
                </Box>
              </Paper>
            </Grid.Col>
          ))}
        </Grid>
      ) : (
        <Paper
          style={{
            textAlign: 'center',
            padding: '60px 24px',
            color: '#8B8680',
            background: '#FFF8F0',
            borderRadius: '8px',
            maxWidth: '500px',
            margin: '0 auto'
          }}
        >
          <Text style={{ fontSize: '64px', marginBottom: '20px', opacity: 0.5 }}>
            📋
          </Text>
          <Title
            order={3}
            style={{
              fontFamily: "'Montserrat', sans-serif",
              fontSize: '24px',
              fontWeight: 600,
              color: '#4A4A4A',
              marginBottom: '16px',
            }}
          >
            No Events Created Yet
          </Title>
          <Text style={{ fontSize: '16px', lineHeight: 1.6, marginBottom: '24px' }}>
            Get started by creating your first event. You can schedule workshops, 
            classes, and community gatherings for your members.
          </Text>
          <Button
            onClick={handleCreateEvent}
            style={{
              background: '#880124',
              color: 'white',
              fontWeight: 600,
            }}
          >
            Create Your First Event
          </Button>
        </Paper>
      )}

      {/* Event Create/Edit Modal */}
      <Modal
        opened={eventModalOpen}
        onClose={handleCancelEvent}
        title={editingEvent ? 'Edit Event' : 'Create New Event'}
        size="xl"
        centered
        styles={{
          title: {
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '24px',
            fontWeight: 700,
            color: '#880124',
          },
        }}
      >
        <EventForm
          initialData={editingEvent ? {
            title: editingEvent.title || '',
            shortDescription: editingEvent.description || '',
            fullDescription: editingEvent.description || '',
            policies: '',
            venueId: '',
            teacherIds: [],
            sessions: [],
            ticketTypes: [],
            eventType: 'class'
          } : undefined}
          onSubmit={handleSubmitEvent}
          onCancel={handleCancelEvent}
          isSubmitting={createEventMutation.isPending || updateEventMutation.isPending}
        />
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        opened={deleteModalOpen}
        onClose={cancelDeleteEvent}
        title="Confirm Delete"
        size="md"
        centered
        styles={{
          title: {
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '20px',
            fontWeight: 700,
            color: '#DC143C',
          },
        }}
      >
        <Box p="md">
          <Text mb="md" style={{ fontSize: '16px', lineHeight: 1.6 }}>
            Are you sure you want to delete the event "<strong>{eventToDelete?.title}</strong>"?
          </Text>
          <Text mb="xl" style={{ fontSize: '14px', color: '#8B8680' }}>
            This action cannot be undone. All registrations and associated data will be permanently removed.
          </Text>
          
          <Box style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
            <Button
              variant="outline"
              onClick={cancelDeleteEvent}
              style={{
                borderColor: '#8B8680',
                color: '#8B8680',
                fontWeight: 600,
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={confirmDeleteEvent}
              loading={deleteEventMutation.isPending}
              style={{
                background: '#DC143C',
                color: 'white',
                fontWeight: 600,
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = '#B91C3C';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = '#DC143C';
              }}
            >
              {deleteEventMutation.isPending ? 'Deleting...' : 'Delete Event'}
            </Button>
          </Box>
        </Box>
      </Modal>
    </Box>
  );
};