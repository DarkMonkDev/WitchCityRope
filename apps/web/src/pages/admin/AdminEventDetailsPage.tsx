import React, { useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
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
  SegmentedControl,
} from '@mantine/core'
import { IconArrowLeft } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { useEvent, useUpdateEvent } from '../../lib/api/hooks/useEvents'
import { useQueryClient } from '@tanstack/react-query'
import { eventKeys } from '../../lib/api/utils/cache'
import { EventForm, EventFormData } from '../../components/events/EventForm'
import { WCRButton } from '../../components/ui'
import {
  convertEventFormDataToUpdateDto,
  getChangedEventFields,
} from '../../utils/eventDataTransformation'
import type { components } from '@witchcityrope/shared-types'

// Type alias for cleaner usage
type EventDtoType = components['schemas']['EventDto']

/**
 * AdminEventDetailsPage - Combined view/edit page for events
 *
 * ARCHITECTURE:
 * This page serves DUAL PURPOSE - both viewing AND editing events
 * Route: /admin/events/:id
 *
 * How Users Get Here:
 * 1. Click a row in AdminEventsPage table → navigates here
 * 2. Save a new event in NewEventPage → navigates here
 * 3. Copy an event → navigates here
 *
 * Edit Mode:
 * - EventForm is ALWAYS rendered (no separate "view" vs "edit" modes for the form)
 * - Users can edit any field immediately
 * - Changes are tracked via formDirty state
 * - Save button appears when changes detected
 *
 * Publish Status:
 * - Draft/Published toggle at top
 * - Changes trigger confirmation modal
 * - Independent of form edits (can change status without editing form)
 *
 * Key State:
 * - isEditMode: Currently unused (form always editable) - may be legacy
 * - formDirty: Tracks if user has made changes
 * - publishStatus: Draft or Published state
 *
 * IMPORTANT for Tests:
 * - NO modal opens when navigating here from table
 * - EventForm renders directly on the page
 * - Look for EventForm component, not [role="dialog"]
 */
export const AdminEventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isEditMode, setIsEditMode] = useState(false) // NOTE: Currently unused - form always editable
  const [publishStatus, setPublishStatus] = useState<string>('published')
  const [confirmModalOpen, setConfirmModalOpen] = useState(false)
  const [pendingStatus, setPendingStatus] = useState<string>('')
  const [formDirty, setFormDirty] = useState(false)
  const [initialFormData, setInitialFormData] = useState<EventFormData | null>(null)

  // Always call hooks unconditionally - use empty string if no id
  const { data: event, isLoading, error } = useEvent(id || '', !!id)
  const updateEventMutation = useUpdateEvent()

  // Convert EventDto to EventFormData - defined as a callback to use in effects
  const convertEventToFormData = useCallback((event: EventDtoType): EventFormData => {
    // Extract venue from location field (API returns location as string)
    const venueId = event.location || ''

    // Map eventType from API response
    const eventType =
      event.eventType === 'Class'
        ? ('class' as const)
        : event.eventType === 'Social'
          ? ('social' as const)
          : ('class' as const) // Default fallback

    // Map volunteer positions from API response - now using consistent field names
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const volunteerPositions = ((event as any)?.volunteerPositions || []).map((vp: any) => ({
      id: vp.id || '',
      title: vp.title || '',
      description: vp.description || '',
      sessions: vp.sessionId ? `Session ${vp.sessionId}` : 'All Sessions',
      startTime: '18:00', // Default as API doesn't include these
      endTime: '21:00', // Default as API doesn't include these
      slotsNeeded: vp.slotsNeeded || 0,
      slotsFilled: vp.slotsFilled || 0,
    }))

    // Map ticket types from API response, adding pricingType if missing
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const ticketTypes = ((event as any)?.ticketTypes || []).map((tt: any) => {
      // Infer pricingType if not present in API data
      let pricingType: 'Fixed' | 'SlidingScale' = 'Fixed';
      let price: number | undefined;
      let minPrice: number | undefined;
      let maxPrice: number | undefined;
      let defaultPrice: number | undefined;

      if (tt.pricingType) {
        // New data structure - use as-is
        pricingType = tt.pricingType;
        price = tt.price;
        minPrice = tt.minPrice;
        maxPrice = tt.maxPrice;
        defaultPrice = tt.defaultPrice;
      } else {
        // Legacy data structure - infer from minPrice/maxPrice
        if (tt.minPrice !== undefined && tt.maxPrice !== undefined) {
          if (tt.minPrice === tt.maxPrice) {
            // Same min and max = fixed price
            pricingType = 'Fixed';
            price = tt.minPrice;
          } else {
            // Different min and max = sliding scale
            pricingType = 'SlidingScale';
            minPrice = tt.minPrice;
            maxPrice = tt.maxPrice;
            defaultPrice = tt.minPrice; // Use minPrice as default if not specified
          }
        }
      }

      return {
        ...tt,
        pricingType,
        price,
        minPrice,
        maxPrice,
        defaultPrice,
      };
    });

    return {
      eventType,
      title: event.title || '',
      shortDescription: event.shortDescription || '',
      fullDescription: event.description || '',
      policies: event.policies || '',
      venueId, // Now properly extracted from API location field
      teacherIds: event.teacherIds || [], // Now maps from API response
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      status:
        ((event as any)?.status as 'Draft' | 'Published' | 'Cancelled' | 'Completed') || 'Draft', // Map API status to form status
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      sessions: (event.sessions as any) || [], // Now maps from API response
      ticketTypes, // Now properly mapped with pricingType
      volunteerPositions, // Now properly mapped from API response
    }
  }, [])

  // Memoized form change handler to prevent unnecessary re-renders
  const handleFormChange = useCallback(() => {
    setFormDirty(true)
  }, [])

  // Initialize publish status and form data from event
  React.useEffect(() => {
    if (event) {
      // Use isPublished field from API response
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const status = (event as any)?.isPublished !== false ? 'published' : 'draft'
      setPublishStatus(status)

      // Convert event to form data
      const newFormData = convertEventToFormData(event as EventDtoType)

      // Only update if form data has actually changed (prevents unnecessary re-renders)
      if (JSON.stringify(newFormData) !== JSON.stringify(initialFormData)) {
        setInitialFormData(newFormData)
      }
    }
  }, [event, convertEventToFormData, initialFormData])

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
    )
  }

  if (isLoading) {
    return (
      <Container size="xl" py="xl" data-testid="page-admin-event-details">
        <LoadingOverlay visible />
      </Container>
    )
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
    )
  }

  const handleGoBack = () => {
    navigate('/admin/events')
  }

  const handleFormSubmit = async (data: EventFormData) => {
    if (!event || !id) return

    try {
      // Get only changed fields for partial update
      const changedFields = initialFormData
        ? getChangedEventFields(id, data, initialFormData)
        : convertEventFormDataToUpdateDto(id, data)

      // Only proceed if there are changes

      if (Object.keys(changedFields).length <= 1) {
        // Only id field means no changes
        notifications.show({
          title: 'No Changes',
          message: 'No changes detected to save.',
          color: 'blue',
        })
        return
      }

      // Perform the API update
      await updateEventMutation.mutateAsync(changedFields)

      // Update initial form data to new values for next change detection
      setInitialFormData(data)
      setIsEditMode(false)
      setFormDirty(false)

      notifications.show({
        title: 'Event Updated',
        message: 'Event details have been saved successfully.',
        color: 'green',
      })

      // Force a refresh of the event data to verify persistence
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(id) })
    } catch (error) {
      // Enhanced error reporting
      let errorMessage = 'Failed to update event. Please try again.'
      if (error instanceof Error) {
        errorMessage = error.message
      }

      notifications.show({
        title: 'Update Failed',
        message: errorMessage,
        color: 'red',
      })
    }
  }

  const handleFormCancel = () => {
    setIsEditMode(false)
    setFormDirty(false)
  }

  const handleStatusChange = (value: string) => {
    if (value !== publishStatus) {
      setPendingStatus(value)
      setConfirmModalOpen(true)
    }
  }

  const confirmStatusChange = async () => {
    if (!event || !id) return

    const action = pendingStatus === 'published' ? 'publish' : 'unpublish'
    const isPublished = pendingStatus === 'published'

    try {
      // Update only the isPublished field
      await updateEventMutation.mutateAsync({
        id,
        isPublished,
      })

      setPublishStatus(pendingStatus)
      setConfirmModalOpen(false)

      notifications.show({
        title: `Event ${isPublished ? 'Published' : 'Unpublished'}`,
        message: `Event has been ${isPublished ? 'published and is now visible to the public' : 'moved to draft and is no longer visible publicly'}.`,
        color: isPublished ? 'green' : 'blue',
      })
    } catch (error) {
      notifications.show({
        title: `${action.charAt(0).toUpperCase() + action.slice(1)} Failed`,
        message:
          error instanceof Error ? error.message : `Failed to ${action} event. Please try again.`,
        color: 'red',
      })
      // Reset pending status on error
      setPendingStatus('')
    }
  }

  const cancelStatusChange = () => {
    setConfirmModalOpen(false)
    setPendingStatus('')
  }

  return (
    <Container size="xl" py="md" data-testid="page-admin-event-details">
      {/* Breadcrumbs */}
      <Breadcrumbs separator="/" mb="md">
        <Anchor
          onClick={handleGoBack}
          style={{
            color: 'var(--mantine-color-wcr-7)',
            cursor: 'pointer',
            textDecoration: 'none',
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
          {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
          {(event as any)?.title || 'New Event'}
        </Title>

        {!isEditMode && (
          <SegmentedControl
            value={publishStatus}
            onChange={handleStatusChange}
            data={[
              { label: 'DRAFT', value: 'draft' },
              { label: 'PUBLISHED', value: 'published' },
            ]}
            size="lg"
            styles={{
              root: {
                backgroundColor: 'var(--mantine-color-gray-1)',
              },
              control: {
                fontFamily: 'Source Sans 3, sans-serif',
                fontSize: '1.5rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                padding: '12px 24px',
                height: 'auto',
              },
              label: {
                fontFamily: 'Source Sans 3, sans-serif',
                fontSize: '1.5rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                color: 'var(--mantine-color-wcr-7)',
              },
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
          eventId={id}
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
            Are you sure you want to {pendingStatus === 'published' ? 'publish' : 'unpublish'} this
            event?
          </Text>

          <Text size="sm" c="dimmed">
            {pendingStatus === 'published'
              ? 'This event will become visible to the public and members can participate.'
              : 'This event will be hidden from the public and no new participation will be accepted.'}
          </Text>

          <Group justify="flex-end" mt="md">
            <WCRButton variant="outline" onClick={cancelStatusChange} size="sm">
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
  )
}
