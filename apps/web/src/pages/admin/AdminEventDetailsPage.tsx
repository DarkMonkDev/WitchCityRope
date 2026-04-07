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
import { IconArrowLeft, IconLink, IconExternalLink } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { useEvent, useUpdateEvent } from '../../lib/api/hooks/useEvents'
import { useQueryClient } from '@tanstack/react-query'
import { eventKeys } from '../../lib/api/utils/cache'
import { EventForm, EventFormData } from '../../components/events/EventForm'
import { DEFAULT_EVENT_TIMEZONE } from '../../utils/eventUtils'
import { WCRButton } from '../../components/ui'
import {
  convertEventFormDataToUpdateDto,
  getChangedEventFields,
} from '../../utils/eventDataTransformation'
import type { components } from '@witchcityrope/shared-types'
import { GenerateCheckInLinkModal } from '../../features/checkin/components/GenerateCheckInLinkModal'

// Type alias for cleaner usage
type EventDtoType = components['schemas']['EventDto']

/**
 * Get the display date suffix for the page title.
 * Uses the first upcoming session's start date. If all sessions are in the past,
 * falls back to the last session's start date.
 * Formats as "Month Dayth" (e.g., "March 15th").
 */
function getSessionDateSuffix(sessions: any[]): string {
  if (!sessions || sessions.length === 0) return '';

  const now = new Date();

  // Sort sessions by startTime ascending
  const sorted = [...sessions]
    .filter((s) => s.startTime)
    .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime());

  if (sorted.length === 0) return '';

  // Find the first session with a start time in the future
  const firstFuture = sorted.find((s) => new Date(s.startTime) > now);
  // If no future sessions, use the last (most recent) session
  const targetSession = firstFuture || sorted[sorted.length - 1];

  // Format the date as "Month Day" in the event timezone, then add ordinal suffix
  const date = new Date(targetSession.startTime);
  const month = date.toLocaleDateString('en-US', {
    timeZone: DEFAULT_EVENT_TIMEZONE,
    month: 'long',
  });
  const day = parseInt(
    date.toLocaleDateString('en-US', {
      timeZone: DEFAULT_EVENT_TIMEZONE,
      day: 'numeric',
    }),
    10
  );

  // Add ordinal suffix (1st, 2nd, 3rd, 4th, etc.)
  const ordinal = (n: number): string => {
    if (n >= 11 && n <= 13) return `${n}th`;
    switch (n % 10) {
      case 1: return `${n}st`;
      case 2: return `${n}nd`;
      case 3: return `${n}rd`;
      default: return `${n}th`;
    }
  };

  return `${month} ${ordinal(day)}`;
}

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
  const [kioskModalOpen, setKioskModalOpen] = useState(false)

  // Always call hooks unconditionally - use empty string if no id
  const { data: event, isLoading, error } = useEvent(id || '', !!id)
  const updateEventMutation = useUpdateEvent()

  // Convert EventDto to EventFormData - defined as a callback to use in effects
  const convertEventToFormData = useCallback((event: EventDtoType): EventFormData => {
    const venueId = event.venueId?.toString() || ''
    const allowRsvps = event.allowRsvps ?? false
    const requireTicketPurchase = event.requireTicketPurchase ?? true
    const vettedMembersOnly = event.vettedMembersOnly ?? false

    // Map volunteer positions from API response - store sessionId directly (matches API format)
    const volunteerPositions = (event.volunteerPositions || []).map((vp) => ({
      id: vp.id || '',
      title: vp.title || '',
      description: vp.description || '',
      sessionId: vp.sessionId || null,  // Store sessionId directly - no conversion needed
      startTime: vp.startTime || '18:00',
      endTime: vp.endTime || '21:00',
      slotsNeeded: vp.slotsNeeded || 0,
      slotsFilled: vp.slotsFilled || 0,
      isPublicFacing: vp.isPublicFacing ?? true,
    }))

    // Map ticket types from API response, adding pricingType if missing
    const ticketTypes = (event.ticketTypes || []).map((tt) => {
      // Infer pricingType if not present in API data
      let pricingType: 'Fixed' | 'SlidingScale' = 'Fixed';
      let price: number | undefined;
      let minPrice: number | undefined;
      let maxPrice: number | undefined;
      let defaultPrice: number | undefined;

      if (tt.pricingType) {
        // New data structure - use as-is
        pricingType = tt.pricingType as 'Fixed' | 'SlidingScale';
        price = tt.price ?? undefined;
        minPrice = tt.minPrice ?? undefined;
        maxPrice = tt.maxPrice ?? undefined;
        defaultPrice = tt.defaultPrice ?? undefined;
      } else {
        // Legacy data structure - infer from minPrice/maxPrice
        if (tt.minPrice != null && tt.maxPrice != null) {
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
      allowRsvps,
      requireTicketPurchase,
      vettedMembersOnly,
      title: event.title || '',
      shortDescription: event.shortDescription || '',
      fullDescription: event.description || '',
      policies: event.policies || '',
      venueId,
      teacherIds: event.teacherIds || [],
      // EventDto has no 'status' field — derive from isPublished
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      status:
        ((event as any)?.status as 'Draft' | 'Published' | 'Cancelled' | 'Completed') || 'Draft',
      sessions: (event.sessions as any) || [],
      ticketTypes,
      volunteerPositions,
      registrationOpenHours: event.registrationOpenHours ?? null,
      registrationCloseHours: event.registrationCloseHours ?? null,
      cancellationCloseHours: event.cancellationCloseHours ?? null,
      volunteerRegistrationCloseHours: event.volunteerRegistrationCloseHours ?? null,
      volunteerCancellationCloseHours: event.volunteerCancellationCloseHours ?? null,
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
      const status = event.isPublished !== false ? 'published' : 'draft'
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
      // apiClient interceptor extracts RFC 9457 message to error.message
      notifications.show({
        title: `${action.charAt(0).toUpperCase() + action.slice(1)} Failed`,
        message: error instanceof Error ? error.message : 'An unexpected error occurred',
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
      {/* Breadcrumbs and Preview Link */}
      <Group justify="space-between" align="center" mb="xs">
        <Breadcrumbs separator="/">
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
        <Anchor
          href={`/events/${id}`}
          target="_blank"
          style={{
            color: 'var(--mantine-color-wcr-7)',
            textDecoration: 'none',
            fontWeight: 600,
            display: 'flex',
            alignItems: 'center',
            gap: '4px'
          }}
        >
          Preview <IconExternalLink size={16} />
        </Anchor>
      </Group>

      {/* Page Header */}
      <Title
        order={1}
        size="h1"
        ff="Source Sans 3, sans-serif"
        c="wcr.7"
        mb="xs"
        style={{ fontSize: '2.5rem', fontWeight: 700 }}
      >
        {/* Show event title with the next upcoming session date (or last session if all past) */}
        {(() => {
          const title = event?.title || 'New Event';
          const dateSuffix = getSessionDateSuffix(event?.sessions || []);
          return dateSuffix ? `${title} - ${dateSuffix}` : title;
        })()}
      </Title>

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
          tabsRightSection={
            !isEditMode && (
              <SegmentedControl
                value={publishStatus}
                onChange={handleStatusChange}
                color="wcr"
                data={[
                  { label: 'Draft', value: 'draft' },
                  { label: 'Published', value: 'published' },
                ]}
                size="lg"
                radius="md"
                styles={{
                  root: {
                    backgroundColor: 'transparent',
                    border: 'none',
                  },
                  label: {
                    fontSize: '1rem',
                    fontWeight: 600,
                  },
                  indicator: {
                    backgroundColor: 'rgb(136, 1, 36)',
                  },
                }}
              />
            )
          }
          attendeesRightSection={
            <WCRButton
              onClick={() => setKioskModalOpen(true)}
              variant="outline"
              size="md"
              leftSection={<IconLink size={16} />}
            >
              Checkin Link
            </WCRButton>
          }
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

      {/* Kiosk Link Generation Modal */}
      <GenerateCheckInLinkModal
        opened={kioskModalOpen}
        onClose={() => setKioskModalOpen(false)}
        eventId={id || ''}
        eventTitle={event?.title || 'Event'}
      />
    </Container>
  )
}
