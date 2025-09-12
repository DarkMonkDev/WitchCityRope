import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Container,
  Stack,
  Title,
  Text,
  Breadcrumbs,
  Anchor,
  Alert,
  LoadingOverlay,
  Group,
  Modal,
  SegmentedControl
} from '@mantine/core';
import {
  IconArrowLeft,
  IconEdit
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useEvent, useUpdateEvent } from '../../lib/api/hooks/useEvents';
import { EventForm, EventFormData } from '../../components/events/EventForm';
import { WCRButton } from '../../components/ui';
import { convertEventFormDataToUpdateDto, hasEventFormDataChanged, getChangedEventFields } from '../../utils/eventDataTransformation';
import type { EventDto } from '@witchcityrope/shared-types';
import type { components } from '@witchcityrope/shared-types';

// Type alias for cleaner usage
type EventDtoType = components['schemas']['EventDto'];

export const AdminEventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [isEditMode, setIsEditMode] = useState(false);
  const [publishStatus, setPublishStatus] = useState<string>('published');
  const [confirmModalOpen, setConfirmModalOpen] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<string>('');
  const [formDirty, setFormDirty] = useState(false);
  const [initialFormData, setInitialFormData] = useState<EventFormData | null>(null);
  
  const { data: event, isLoading, error } = useEvent(id!, !!id);
  const updateEventMutation = useUpdateEvent();
  
  // Initialize publish status and form data from event
  React.useEffect(() => {
    if (event) {
      // Determine status from event data
      const status = event.status === 'Published' || !event.status ? 'published' : 'draft';
      setPublishStatus(status);
      
      // Store initial form data for change tracking
      const initialData = convertEventToFormData(event as EventDtoType);
      setInitialFormData(initialData);
    }
  }, [event]);  
  
  if (!id) {
    return (
      <Container size="xl" py="xl">
        <Alert color="red" title="Invalid Event ID">
          <Text>No event ID provided in the URL.</Text>
          <WCRButton 
            leftSection={<IconArrowLeft size={16} />}
            onClick={() => navigate('/admin/events')} 
            mt="md" 
            size="sm"
            variant="outline"
          >
            Back to Events List
          </WCRButton>
        </Alert>
      </Container>
    );
  }
  
  if (isLoading) {
    return (
      <Container size="xl" py="xl" data-testid="page-admin-event-details">
        <LoadingOverlay visible />
      </Container>
    );
  }
  
  if (error || !event) {
    return (
      <Container size="xl" py="xl" data-testid="page-admin-event-details">
        <Alert color="red" title="Event Not Found">
          <Text>
            Sorry, we couldn't find this event. It may have been removed or the link is incorrect.
          </Text>
          <WCRButton 
            leftSection={<IconArrowLeft size={16} />}
            onClick={() => navigate('/admin/events')} 
            mt="md" 
            size="sm"
            variant="outline"
          >
            Back to Events List
          </WCRButton>
        </Alert>
      </Container>
    );
  }

  // Convert EventDto to EventFormData
  const convertEventToFormData = (event: EventDtoType): EventFormData => {
    return {
      eventType: 'class', // Default since EventDto doesn't have this field, could map from event.eventType
      title: event.title || '',
      shortDescription: event.description?.substring(0, 160) || '', // Take first 160 chars as short desc
      fullDescription: event.description || '',
      policies: '', // EventDto doesn't have policies field
      venueId: '', // EventDto doesn't have location field in generated types
      teacherIds: [], // EventDto doesn't have teachers
      sessions: [], // EventDto doesn't have sessions
      ticketTypes: [], // EventDto doesn't have ticket types
    };
  };

  const handleEdit = () => {
    setIsEditMode(true);
  };

  const handleGoBack = () => {
    navigate('/admin/events');
  };

  const handleFormSubmit = async (data: EventFormData) => {
    if (!event || !id) return;
    
    try {
      // Get only changed fields for partial update
      const changedFields = initialFormData 
        ? getChangedEventFields(id, data, initialFormData)
        : convertEventFormDataToUpdateDto(id, data);
      
      // Only proceed if there are changes
      if (Object.keys(changedFields).length <= 1) { // Only id field means no changes
        notifications.show({
          title: 'No Changes',
          message: 'No changes detected to save.',
          color: 'blue'
        });
        return;
      }
      
      await updateEventMutation.mutateAsync(changedFields);
      
      // Update initial form data to new values
      setInitialFormData(data);
      setIsEditMode(false);
      setFormDirty(false);
      
      notifications.show({
        title: 'Event Updated',
        message: 'Event details have been saved successfully.',
        color: 'green'
      });
    } catch (error) {
      console.error('Failed to update event:', error);
      notifications.show({
        title: 'Update Failed',
        message: error instanceof Error ? error.message : 'Failed to update event. Please try again.',
        color: 'red'
      });
    }
  };

  const handleFormCancel = () => {
    setIsEditMode(false);
    setFormDirty(false);
  };

  const handleStatusChange = (value: string) => {
    if (value !== publishStatus) {
      setPendingStatus(value);
      setConfirmModalOpen(true);
    }
  };

  const confirmStatusChange = async () => {
    if (!event || !id) return;
    
    const action = pendingStatus === 'published' ? 'publish' : 'unpublish';
    const isPublished = pendingStatus === 'published';
    
    try {
      // Update only the isPublished field
      await updateEventMutation.mutateAsync({
        id,
        isPublished
      });
      
      setPublishStatus(pendingStatus);
      setConfirmModalOpen(false);
      
      notifications.show({
        title: `Event ${isPublished ? 'Published' : 'Unpublished'}`,
        message: `Event has been ${isPublished ? 'published and is now visible to the public' : 'moved to draft and is no longer visible publicly'}.`,
        color: isPublished ? 'green' : 'blue'
      });
    } catch (error) {
      console.error(`Failed to ${action} event:`, error);
      notifications.show({
        title: `${action.charAt(0).toUpperCase() + action.slice(1)} Failed`,
        message: error instanceof Error ? error.message : `Failed to ${action} event. Please try again.`,
        color: 'red'
      });
      // Reset pending status on error
      setPendingStatus('');
    }
  };

  const cancelStatusChange = () => {
    setConfirmModalOpen(false);
    setPendingStatus('');
  };

  return (
    <Container size="xl" py="md" data-testid="page-admin-event-details">
      {/* Breadcrumbs */}
      <Breadcrumbs separator="/" mb="md">
        <Anchor 
          onClick={handleGoBack}
          style={{ 
            color: 'var(--mantine-color-wcr-7)',
            cursor: 'pointer',
            textDecoration: 'none'
          }}
        >
          Admin Events
        </Anchor>
        <Text c="dimmed">{isEditMode ? 'Edit Event' : 'Event Details'}</Text>
      </Breadcrumbs>

      {/* Page Header */}
      <Group justify="space-between" align="center" mb="md">
        <Title 
          order={1} 
          size="h1"
          ff="Source Sans 3, sans-serif"
          c="wcr.7"
          style={{ fontSize: '2.5rem', fontWeight: 700 }}
        >
          {event?.title || 'New Event'}
        </Title>
        
        {!isEditMode && (
          <SegmentedControl
            value={publishStatus}
            onChange={handleStatusChange}
            data={[
              { label: 'DRAFT', value: 'draft' },
              { label: 'PUBLISHED', value: 'published' }
            ]}
            size="lg"
            styles={{
              root: {
                backgroundColor: 'var(--mantine-color-gray-1)'
              },
              control: {
                fontFamily: "Source Sans 3, sans-serif",
                fontSize: '1.5rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                padding: '12px 24px',
                height: 'auto'
              },
              label: {
                fontFamily: "Source Sans 3, sans-serif",
                fontSize: '1.5rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                color: 'var(--mantine-color-wcr-7)'
              }
            }}
          />
        )}
      </Group>

      {/* EventForm Component */}
      <EventForm
        initialData={convertEventToFormData(event as EventDtoType)}
        onSubmit={handleFormSubmit}
        onCancel={handleFormCancel}
        isSubmitting={updateEventMutation.isPending}
        onFormChange={() => setFormDirty(true)}
        formDirty={formDirty}
      />
      
      {/* Status Change Confirmation Modal */}
      <Modal
        opened={confirmModalOpen}
        onClose={cancelStatusChange}
        title="Confirm Status Change"
        centered
      >
        <Stack>
          <Text>
            Are you sure you want to {pendingStatus === 'published' ? 'publish' : 'unpublish'} this event?
          </Text>
          
          <Text size="sm" c="dimmed">
            {pendingStatus === 'published' 
              ? 'This event will become visible to the public and members can register.'
              : 'This event will be hidden from the public and no new registrations will be accepted.'
            }
          </Text>
          
          <Group justify="flex-end" mt="md">
            <WCRButton
              variant="outline"
              onClick={cancelStatusChange}
              size="sm"
            >
              Cancel
            </WCRButton>
            
            <WCRButton
              variant={pendingStatus === 'published' ? 'primary' : 'secondary'}
              onClick={confirmStatusChange}
              size="sm"
            >
              {pendingStatus === 'published' ? 'Publish Event' : 'Unpublish Event'}
            </WCRButton>
          </Group>
        </Stack>
      </Modal>
    </Container>
  );
};

