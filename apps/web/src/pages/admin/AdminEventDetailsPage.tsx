import React, { useState, useCallback } from 'react';
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
import { useQueryClient } from '@tanstack/react-query';
import { eventKeys } from '../../lib/api/utils/cache';
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
  const queryClient = useQueryClient();
  const [isEditMode, setIsEditMode] = useState(false);
  const [publishStatus, setPublishStatus] = useState<string>('published');
  const [confirmModalOpen, setConfirmModalOpen] = useState(false);
  const [pendingStatus, setPendingStatus] = useState<string>('');
  const [formDirty, setFormDirty] = useState(false);
  const [initialFormData, setInitialFormData] = useState<EventFormData | null>(null);

  // Always call hooks unconditionally - use empty string if no id
  const { data: event, isLoading, error } = useEvent(id || '', !!id);
  const updateEventMutation = useUpdateEvent();
  
  // Convert EventDto to EventFormData - defined as a callback to use in effects
  const convertEventToFormData = useCallback((event: EventDtoType): EventFormData => {
    // Extract venue from location field (API returns location as string)
    const venueId = event.location || '';
    
    // Map eventType from API response
    const eventType = event.eventType === 'Class' ? 'class' as const : 
                     event.eventType === 'Social' ? 'social' as const : 
                     'class' as const; // Default fallback

    // Map volunteer positions from API response - handle field name differences
    const volunteerPositions = ((event as any)?.volunteerPositions || []).map((vp: any) => ({
      id: vp.id || '',
      positionName: vp.title || vp.name || '', // API returns 'title', form uses 'positionName'
      description: vp.description || '',
      sessions: vp.sessionId ? `Session ${vp.sessionId}` : 'All Sessions',
      startTime: '18:00', // Default as API doesn't include these
      endTime: '21:00',   // Default as API doesn't include these
      volunteersNeeded: vp.slotsNeeded || vp.volunteersNeeded || 0,
      volunteersAssigned: vp.slotsFilled || vp.volunteersAssigned || 0,
    }));

    return {
      eventType,
      title: event.title || '',
      shortDescription: event.description?.substring(0, 160) || '', // Take first 160 chars as short desc
      fullDescription: event.description || '',
      policies: '', // EventDto doesn't have policies field - will be preserved from initial load
      venueId, // Now properly extracted from API location field
      teacherIds: event.teacherIds || [], // Now maps from API response
      status: ((event as any)?.status as 'Draft' | 'Published' | 'Cancelled' | 'Completed') || 'Draft', // Map API status to form status
      sessions: (event.sessions as any) || [], // Now maps from API response
      ticketTypes: (event.ticketTypes as any) || [], // Now maps from API response
      volunteerPositions, // Now properly mapped from API response
    };
  }, []);

  // Memoized form change handler to prevent unnecessary re-renders
  const handleFormChange = useCallback(() => {
    setFormDirty(true);
  }, []);
  
  // Initialize publish status and form data from event
  React.useEffect(() => {
    if (event && !initialFormData) {
      // Use isPublished field from API response
      const status = (event as any)?.isPublished !== false ? 'published' : 'draft';
      setPublishStatus(status);

      // DEBUG: Log event data from API
      console.log('🔍 [DEBUG] Event data from API:', {
        eventId: (event as any).id,
        teacherIds: (event as any).teacherIds,
        hasTeacherIds: 'teacherIds' in (event as any),
        eventKeys: Object.keys(event as any)
      });

      // Store initial form data for change tracking
      // Only set this once when event data first loads
      const initialData = convertEventToFormData(event as EventDtoType);

      // DEBUG: Log converted form data
      console.log('🔍 [DEBUG] Converted initial form data:', {
        teacherIds: initialData.teacherIds,
        sessions: initialData.sessions,
        ticketTypes: initialData.ticketTypes,
        volunteerPositions: initialData.volunteerPositions,
        formDataKeys: Object.keys(initialData)
      });

      setInitialFormData(initialData);
    }
  }, [event, convertEventToFormData]); // Remove initialFormData from dependencies to prevent re-initialization  
  
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

  const handleEdit = () => {
    setIsEditMode(true);
  };

  const handleGoBack = () => {
    navigate('/admin/events');
  };

  const handleFormSubmit = async (data: EventFormData) => {
    if (!event || !id) return;

    try {
      // DEBUG: Log form data before processing
      console.log('🔍 [DEBUG] Form data before save:', {
        teacherIds: data.teacherIds,
        sessions: data.sessions,
        ticketTypes: data.ticketTypes,
        volunteerPositions: data.volunteerPositions,
        formDataKeys: Object.keys(data),
        fullFormData: data
      });

      // DEBUG: Compare initial vs current volunteerPositions
      console.log('🔍 [DEBUG] Volunteer positions comparison:', {
        initial: initialFormData?.volunteerPositions,
        current: data.volunteerPositions,
        areEqual: JSON.stringify(initialFormData?.volunteerPositions) === JSON.stringify(data.volunteerPositions),
        initialLength: initialFormData?.volunteerPositions?.length || 0,
        currentLength: data.volunteerPositions?.length || 0
      });

      // Get only changed fields for partial update
      const changedFields = initialFormData
        ? getChangedEventFields(id, data, initialFormData)
        : convertEventFormDataToUpdateDto(id, data);

      // DEBUG: Log what changes were detected
      console.log('🔍 [DEBUG] Changed fields to send to API:', {
        changedFields,
        hasTeacherIds: 'teacherIds' in changedFields,
        teacherIdsValue: changedFields.teacherIds,
        hasSessions: 'sessions' in changedFields,
        sessionsValue: changedFields.sessions,
        hasTicketTypes: 'ticketTypes' in changedFields,
        ticketTypesValue: changedFields.ticketTypes,
        hasVolunteerPositions: 'volunteerPositions' in changedFields,
        volunteerPositionsValue: changedFields.volunteerPositions
      });

      // Only proceed if there are changes
      console.log('🔍 [DEBUG] Checking if should save:', {
        changedFieldsKeys: Object.keys(changedFields),
        keyCount: Object.keys(changedFields).length,
        shouldSave: Object.keys(changedFields).length > 1
      });

      if (Object.keys(changedFields).length <= 1) { // Only id field means no changes
        notifications.show({
          title: 'No Changes',
          message: 'No changes detected to save.',
          color: 'blue'
        });
        return;
      }

      console.log('🔍 [DEBUG] About to call updateEventMutation with:', changedFields);

      // Perform the API update
      const updatedEvent = await updateEventMutation.mutateAsync(changedFields);

      // DEBUG: Log API response
      console.log('🔍 [DEBUG] API response after save:', {
        updatedEvent,
        hasTeacherIds: !!(updatedEvent as any).teacherIds,
        teacherIds: (updatedEvent as any).teacherIds,
        hasSessions: !!(updatedEvent as any).sessions,
        sessions: (updatedEvent as any).sessions,
        hasTicketTypes: !!(updatedEvent as any).ticketTypes,
        ticketTypes: (updatedEvent as any).ticketTypes,
        hasVolunteerPositions: !!(updatedEvent as any).volunteerPositions,
        volunteerPositions: (updatedEvent as any).volunteerPositions
      });

      // Update initial form data to new values for next change detection
      setInitialFormData(data);
      setIsEditMode(false);
      setFormDirty(false);

      notifications.show({
        title: 'Event Updated',
        message: 'Event details have been saved successfully.',
        color: 'green'
      });

      // Force a refresh of the event data to verify persistence
      console.log('🔍 [DEBUG] Triggering event data refresh to verify persistence...');
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(id) });

    } catch (error) {
      console.error('Failed to update event:', error);

      // Enhanced error reporting
      let errorMessage = 'Failed to update event. Please try again.';
      if (error instanceof Error) {
        errorMessage = error.message;
        console.error('🔍 [DEBUG] Error details:', {
          name: error.name,
          message: error.message,
          stack: error.stack
        });
      }

      notifications.show({
        title: 'Update Failed',
        message: errorMessage,
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
          {(event as any)?.title || 'New Event'}
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
      {initialFormData ? (
        <EventForm
          initialData={initialFormData}
          onSubmit={handleFormSubmit}
          onCancel={handleFormCancel}
          isSubmitting={updateEventMutation.isPending}
          onFormChange={handleFormChange}
          formDirty={formDirty}
        />
      ) : (
        <LoadingOverlay visible />
      )}
      
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

