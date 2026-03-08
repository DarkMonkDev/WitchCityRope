import React, { useState, useEffect, useRef } from 'react'
import { Card, Tabs, TextInput, Group, Text, Select, Stack, Title, MultiSelect, Badge, Table, Alert, Modal, Button, NumberInput, Box, Checkbox, SimpleGrid } from '@mantine/core'
import { useForm } from '@mantine/form'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../lib/api/client'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconAlertCircle, IconChevronUp, IconChevronDown } from '@tabler/icons-react'
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor'
import type { components } from '@witchcityrope/shared-types'

import { EventSessionsGrid, EventSession } from './EventSessionsGrid'
import { EventTicketTypesGrid, EventTicketType } from './EventTicketTypesGrid'
import { SessionFormModal } from './SessionFormModal'
import { TicketTypeFormModal } from './TicketTypeFormModal'
import { VolunteerPositionsGrid } from './VolunteerPositionsGrid'
import { VolunteerPosition } from './VolunteerPositionFormModal'
import { RemoveRsvpModal } from './RemoveRsvpModal'
import { RefundConfirmationModal } from '../payments/RefundConfirmationModal'
import { DeleteConfirmationModal, DeletionState } from './DeleteConfirmationModal'
import { WCRButton } from '../ui'
import { useTeachers, formatTeachersForMultiSelect } from '../../lib/api/hooks/useTeachers'
import {
  useEventParticipations,
  type EventParticipationDto,
} from '../../lib/api/hooks/useEventParticipations'
import { useUpdateEvent } from '../../lib/api/hooks/useEvents'
import { eventKeys } from '../../lib/api/utils/cache'
import { useEventTimeZone } from '../../hooks/useEventTimeZone'
import {
  emailTemplatesApi,
  type EventEmailTemplateDto,
  type UpdateEventTemplateRequest,
} from '../../services/emailTemplates.api'

/**
 * Extract user-friendly error message from API errors
 * Handles RFC 7807 Problem Details format from ASP.NET Core
 */
const getApiErrorMessage = (error: unknown, fallbackMessage: string): string => {
  // Check for axios error with response data
  if (error && typeof error === 'object' && 'response' in error) {
    const axiosError = error as { response?: { data?: { detail?: string; title?: string; message?: string } } };
    const data = axiosError.response?.data;
    if (data?.detail) return data.detail;
    if (data?.title) return data.title;
    if (data?.message) return data.message;
  }
  // Fall back to Error.message or default
  if (error instanceof Error) return error.message;
  return fallbackMessage;
}

/**
 * Generate a UUID with fallback for environments where crypto.randomUUID is not available
 * (e.g., non-secure contexts, older browsers, some test environments)
 */
const generateUUID = (): string => {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  // Fallback implementation
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
};


// Attendees Tab Panel Component
interface AttendeesTabPanelProps {
  eventId?: string
  rightSection?: React.ReactNode
}

const AttendeesTabPanel: React.FC<AttendeesTabPanelProps> = ({ eventId, rightSection }) => {
  const [sortColumn, setSortColumn] = useState<'name' | 'paid' | 'attended' | 'sessions'>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')

  // Fetch participations (RSVPs and tickets)
  const { data: participations = [], isLoading } = useEventParticipations(
    eventId || '',
    !!eventId
  ) as { data: EventParticipationDto[]; isLoading: boolean }

  // Group participations by user, combining RSVP + Ticket into single row
  const groupedParticipations = React.useMemo(() => {
    const grouped = new Map<string, EventParticipationDto & { ticketAmount?: number; checkedInSessions?: string[] }>()

    participations.forEach((p) => {
      const existing = grouped.get(p.userId ?? '')

      if (!existing) {
        // First entry for this user
        grouped.set(p.userId ?? '', {
          ...p,
          ticketAmount: p.participationType === 'Ticket' ? (p.amountPaid ?? 0) : undefined,
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          checkedInSessions: (p as any)?.checkedInSessions || [],
        })
      } else {
        // User already exists - merge ticket amount if this is a ticket purchase
        if (p.participationType === 'Ticket') {
          existing.ticketAmount = p.amountPaid ?? 0
        }
        // Prefer RSVP for main display, but keep ticket's check-in status if available
        if (p.participationType === 'RSVP') {
          existing.participationType = 'RSVP'
          existing.participationDate = p.participationDate
          existing.status = p.status
        }
        // Keep check-in status if ticket was checked in
        if (p.hasCheckedIn) {
          existing.hasCheckedIn = true
        }
        // Merge checked-in sessions (deduplicate to avoid showing same session twice)
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const pSessions = (p as any)?.checkedInSessions || []
        if (pSessions.length > 0) {
          const allSessions = [...(existing.checkedInSessions || []), ...pSessions]
          existing.checkedInSessions = [...new Set(allSessions)]
        }
      }
    })

    return Array.from(grouped.values())
  }, [participations])

  // Sort participations based on current sort settings
  const sortedParticipations = React.useMemo(() => {
    const sorted = [...groupedParticipations]

    sorted.sort((a, b) => {
      let compareValue = 0

      if (sortColumn === 'name') {
        compareValue = (a.userSceneName ?? '').localeCompare(b.userSceneName ?? '')
      } else if (sortColumn === 'paid') {
        const amountA = a.ticketAmount ?? 0
        const amountB = b.ticketAmount ?? 0
        compareValue = amountA - amountB
      } else if (sortColumn === 'attended') {
        // Sort by check-in status (true/false)
        // Convert boolean to number: true=1, false=0
        const aCheckedIn = a.hasCheckedIn ?? false
        const bCheckedIn = b.hasCheckedIn ?? false
        compareValue = Number(aCheckedIn) - Number(bCheckedIn)
      } else if (sortColumn === 'sessions') {
        // Sort by number of sessions attended
        const aSessionCount = a.checkedInSessions?.length ?? 0
        const bSessionCount = b.checkedInSessions?.length ?? 0
        compareValue = aSessionCount - bSessionCount
      }

      return sortDirection === 'asc' ? compareValue : -compareValue
    })

    return sorted
  }, [groupedParticipations, sortColumn, sortDirection])

  const handleSort = (column: 'name' | 'paid' | 'attended' | 'sessions') => {
    if (sortColumn === column) {
      // Toggle direction if clicking same column
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      // Set new column and default to ascending
      setSortColumn(column)
      setSortDirection('asc')
    }
  }

  const getSortIcon = (column: 'name' | 'paid' | 'attended' | 'sessions') => {
    if (sortColumn !== column) return null
    return sortDirection === 'asc' ? ' ↑' : ' ↓'
  }

  if (!eventId) {
    return (
      <Text ta="center" c="dimmed" py="xl">
        Save the event first to view attendees.
      </Text>
    )
  }

  return (
    <Stack gap="xl">
      <div data-testid="attendees-list">
        <Group justify="space-between" align="center" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
          <Title order={2} c="burgundy">
            Event Attendees
          </Title>
          {rightSection}
        </Group>
        <Text size="sm" c="dimmed" mb="lg">
          People with tickets (workshops) or RSVPs (social events) for this event.
        </Text>

        {isLoading ? (
          <Text ta="center" c="dimmed" py="xl">
            Loading attendees...
          </Text>
        ) : sortedParticipations.length === 0 ? (
          <Text ta="center" c="dimmed" py="xl">
            No attendees yet. Attendees will appear here when people purchase tickets or RSVP.
          </Text>
        ) : (
          <Table
            striped
            highlightOnHover
            withTableBorder
            data-testid="attendees-table"
            style={{
              backgroundColor: 'white',
              borderRadius: '12px',
              overflow: 'hidden',
              boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
            }}
          >
            <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
              <Table.Tr>
                <Table.Th
                  onClick={() => handleSort('name')}
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    cursor: 'pointer',
                    userSelect: 'none',
                  }}
                >
                  Name{getSortIcon('name')}
                </Table.Th>
                <Table.Th
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                  }}
                >
                  Email
                </Table.Th>
                <Table.Th
                  onClick={() => handleSort('paid')}
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    cursor: 'pointer',
                    userSelect: 'none',
                  }}
                >
                  Paid{getSortIcon('paid')}
                </Table.Th>
                <Table.Th
                  onClick={() => handleSort('attended')}
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    cursor: 'pointer',
                    userSelect: 'none',
                  }}
                >
                  Attended{getSortIcon('attended')}
                </Table.Th>
                <Table.Th
                  onClick={() => handleSort('sessions')}
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                    cursor: 'pointer',
                    userSelect: 'none',
                  }}
                >
                  Sessions Attended{getSortIcon('sessions')}
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {sortedParticipations.map((participation) => {
                const paidAmount = participation.ticketAmount ?? 0
                // Use check-in status from backend (generated DTO property)
                const hasCheckedIn = participation.hasCheckedIn ?? false
                const checkedInSessions = participation.checkedInSessions || []

                return (
                  <Table.Tr key={participation.id}>
                    <Table.Td>
                      <Text fw={500}>{participation.userSceneName}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {participation.userEmail}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" fw={500}>
                        ${paidAmount.toFixed(2)}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Badge
                        color={hasCheckedIn ? 'green' : 'gray'}
                        variant={hasCheckedIn ? 'filled' : 'light'}
                      >
                        {hasCheckedIn ? 'Yes' : 'No'}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      {checkedInSessions.length > 0 ? (
                        <Group gap="xs">
                          {checkedInSessions.map((sessionName, idx) => (
                            <Badge key={idx} variant="light" color="green" size="sm">
                              {sessionName}
                            </Badge>
                          ))}
                        </Group>
                      ) : (
                        <Text size="sm" c="dimmed">None</Text>
                      )}
                    </Table.Td>
                  </Table.Tr>
                )
              })}
            </Table.Tbody>
          </Table>
        )}
      </div>
    </Stack>
  )
}

export interface EventFormData {
  // Basic Info - Replaced eventType with boolean flags
  allowRsvps: boolean
  requireTicketPurchase: boolean
  vettedMembersOnly: boolean
  title: string
  shortDescription: string
  fullDescription: string
  policies: string
  venueId: string
  teacherIds: string[]

  // Status
  status: 'Draft' | 'Published' | 'Cancelled' | 'Completed'

  // Sessions and Tickets
  sessions: EventSession[]
  ticketTypes: EventTicketType[]

  // Volunteer Positions
  volunteerPositions: VolunteerPosition[]

  // Timing Controls (nullable, in hours relative to event start)
  registrationOpenHours?: number | null
  registrationCloseHours?: number | null
  cancellationCloseHours?: number | null
  volunteerRegistrationCloseHours?: number | null
  volunteerCancellationCloseHours?: number | null
}

interface EventFormProps {
  initialData?: Partial<EventFormData>
  onSubmit: (data: EventFormData) => void
  onCancel: () => void
  isSubmitting?: boolean
  onFormChange?: () => void
  formDirty?: boolean
  eventId?: string // For fetching participation data
  tabsRightSection?: React.ReactNode // Optional content to display to the right of tabs
  attendeesRightSection?: React.ReactNode // Optional content to display on Attendees section title row
}

export const EventForm: React.FC<EventFormProps> = ({
  initialData,
  onSubmit,
  onCancel,
  isSubmitting = false,
  onFormChange,
  formDirty = false,
  eventId,
  tabsRightSection,
  attendeesRightSection,
}) => {
  const [activeTab, setActiveTab] = useState<string>('basic-info')
  const [activeEmailTemplate, setActiveEmailTemplate] = useState<string | null>(null)
  const [_rsvpTimingOpen, _setRsvpTimingOpen] = useState(false)
  const [_volunteerTimingOpen, _setVolunteerTimingOpen] = useState(false)

  // Track timing-specific changes separately
  const [rsvpTimingDirty, setRsvpTimingDirty] = useState(false)
  const [volunteerTimingDirty, setVolunteerTimingDirty] = useState(false)
  const [initialTimingValues, setInitialTimingValues] = useState({
    rsvp: {
      registrationOpenHours: initialData?.registrationOpenHours ?? null,
      registrationCloseHours: initialData?.registrationCloseHours ?? null,
      cancellationCloseHours: initialData?.cancellationCloseHours ?? null,
    },
    volunteer: {
      volunteerRegistrationCloseHours: initialData?.volunteerRegistrationCloseHours ?? null,
      volunteerCancellationCloseHours: initialData?.volunteerCancellationCloseHours ?? null,
    },
  })

  const queryClient = useQueryClient()
  const eventTimeZone = useEventTimeZone()

  // Fetch teachers from API
  const { data: teachersData, isLoading: teachersLoading, error: teachersError } = useTeachers()

  // Fetch active venues from API
  type VenueDto = components['schemas']['VenueDto']
  const { data: venuesData } = useQuery<VenueDto[]>({
    queryKey: ['admin', 'venues', 'active'],
    queryFn: async () => {
      const response = await apiClient.get<VenueDto[]>('/api/admin/venues/active')
      return response.data || []
    },
  })

  // Fetch event participations for admin view (only if eventId provided)
  const {
    data: participationsData,
    isLoading: participationsLoading,
    error: participationsError,
  } = useEventParticipations(eventId || '', !!eventId)

  // Mutation for updating event data immediately
  const updateEventMutation = useUpdateEvent()

  // Email template mutations
  const resetTemplateMutation = useMutation({
    mutationFn: async ({ eventId, templateType }: { eventId: string; templateType: string }) => {
      await emailTemplatesApi.deleteEventTemplate(eventId, templateType)
    },
    onSuccess: () => {
      if (eventId) {
        // Refresh templates list
        emailTemplatesApi.getEventTemplates(eventId).then(setEventTemplates)
      }
      notifications.show({
        title: 'Success',
        message: 'Template reset to default',
        color: 'green',
        icon: <IconCheck />,
      })
      setResetModalOpen(false)
      setTemplateToReset(null)
    },
    onError: (error: Error) => {
      console.error('Failed to reset template:', error)
      notifications.show({
        title: 'Error',
        message: 'Failed to reset template to default',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    },
  })

  // Modal state management
  const [sessionModalOpen, setSessionModalOpen] = useState(false)
  const [ticketModalOpen, setTicketModalOpen] = useState(false)
  const [editingSession, setEditingSession] = useState<EventSession | null>(null)
  const [editingTicketType, setEditingTicketType] = useState<EventTicketType | null>(null)

  // Delete confirmation modal state
  const [deleteModalOpen, setDeleteModalOpen] = useState(false)
  const [deleteItemType, setDeleteItemType] = useState<'session' | 'ticketType'>('session')
  const [deleteItemId, setDeleteItemId] = useState<string>('')
  const [deleteItemName, setDeleteItemName] = useState<string>('')
  const [deletionCheckResponse, setDeletionCheckResponse] = useState<any>(null)
  const [_isCheckingDeletion, setIsCheckingDeletion] = useState(false)
  const [isDeletingItem, setIsDeletingItem] = useState(false)

  // RSVP/Ticket removal modal state
  const [removeRsvpModalOpen, setRemoveRsvpModalOpen] = useState(false)
  const [refundTicketModalOpen, setRefundTicketModalOpen] = useState(false)
  const [selectedParticipant, setSelectedParticipant] = useState<EventParticipationDto | null>(null)

  // RSVP table sorting
  const [rsvpSortColumn, setRsvpSortColumn] = useState<'name' | 'email' | 'status' | 'date'>('name')
  const [rsvpSortDirection, setRsvpSortDirection] = useState<'asc' | 'desc'>('asc')

  // Tickets table sorting
  const [ticketsSortColumn, setTicketsSortColumn] = useState<
    'name' | 'ticketType' | 'status' | 'sessions' | 'date' | 'amount'
  >('name')
  const [ticketsSortDirection, setTicketsSortDirection] = useState<'asc' | 'desc'>('asc')

  // Email templates state
  const [eventTemplates, setEventTemplates] = useState<EventEmailTemplateDto[]>([])
  const [isLoadingTemplates, setIsLoadingTemplates] = useState(false)
  const [resetModalOpen, setResetModalOpen] = useState(false)
  const [templateToReset, setTemplateToReset] = useState<EventEmailTemplateDto | null>(null)

  // Helper function to toggle sort
  const handleRsvpSort = (column: typeof rsvpSortColumn) => {
    if (rsvpSortColumn === column) {
      setRsvpSortDirection(rsvpSortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setRsvpSortColumn(column)
      setRsvpSortDirection('asc')
    }
  }

  const handleTicketsSort = (column: typeof ticketsSortColumn) => {
    if (ticketsSortColumn === column) {
      setTicketsSortDirection(ticketsSortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      setTicketsSortColumn(column)
      setTicketsSortDirection('asc')
    }
  }

  // Helper function to render sort icon
  const renderSortIcon = (
    columnName: string,
    currentColumn: string,
    currentDirection: 'asc' | 'desc'
  ) => {
    if (columnName !== currentColumn) return null
    return currentDirection === 'asc' ? (
      <IconChevronUp size={14} style={{ marginLeft: 4 }} />
    ) : (
      <IconChevronDown size={14} style={{ marginLeft: 4 }} />
    )
  }

  // Form state management
  const form = useForm<EventFormData>({
    initialValues: {
      // Default to class-like event (requireTicketPurchase = true, allowRsvps = false)
      allowRsvps: false,
      requireTicketPurchase: true,
      vettedMembersOnly: false,
      title: '',
      shortDescription: '',
      fullDescription: '',
      policies: '',
      venueId: '',
      teacherIds: [],
      status: 'Draft',
      sessions: [],
      ticketTypes: [],
      volunteerPositions: [],
      registrationOpenHours: null,
      registrationCloseHours: -12,
      cancellationCloseHours: -12,
      volunteerRegistrationCloseHours: 24,
      volunteerCancellationCloseHours: 48,
      ...initialData,
    },
    validate: {
      title: (value) => (!value ? 'Event title is required' : null),
      shortDescription: (value) => {
        if (!value) return 'Short description is required'
        if (value.length > 160) return 'Short description must be 160 characters or less'
        return null
      },
      fullDescription: (value) => (!value ? 'Full description is required' : null),
      venueId: (value) => (!value ? 'Venue selection is required' : null),
      registrationOpenHours: (value) => {
        if (value !== null && value !== undefined && value < -24) {
          return 'Cannot be more than 24 hours after event start'
        }
        return null
      },
      registrationCloseHours: (value) => {
        if (value !== null && value !== undefined && value < -24) {
          return 'Cannot be more than 24 hours after event start'
        }
        return null
      },
      cancellationCloseHours: (value) => {
        if (value !== null && value !== undefined && value < -24) {
          return 'Cannot be more than 24 hours after event start'
        }
        return null
      },
      volunteerRegistrationCloseHours: (value) => {
        if (value !== null && value !== undefined && value < -24) {
          return 'Cannot be more than 24 hours after event start'
        }
        return null
      },
      volunteerCancellationCloseHours: (value) => {
        if (value !== null && value !== undefined && value < -24) {
          return 'Cannot be more than 24 hours after event start'
        }
        return null
      },
    },
  })

  // Update form values when initialData changes (for loading from API)
  // Only update when initialData actually changes (not on every render)
  const initialDataRef = useRef<string>('')

  useEffect(() => {
    if (initialData && Object.keys(initialData).length > 0) {
      const newInitialDataStr = JSON.stringify(initialData)

      // Only update if initialData has actually changed from last time
      if (newInitialDataStr !== initialDataRef.current) {
        initialDataRef.current = newInitialDataStr

        form.setValues({
          allowRsvps: false,
          requireTicketPurchase: true,
          vettedMembersOnly: false,
          title: '',
          shortDescription: '',
          fullDescription: '',
          policies: '',
          venueId: '',
          teacherIds: [],
          status: 'Draft',
          sessions: [],
          ticketTypes: [],
          volunteerPositions: [],
          registrationOpenHours: null,
          registrationCloseHours: null,
          cancellationCloseHours: null,
          volunteerRegistrationCloseHours: null,
          volunteerCancellationCloseHours: null,
          ...initialData,
        })
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialData])

  // Track form changes
  const previousValues = useRef(form.values)
  const onFormChangeRef = useRef(onFormChange)

  // Update the ref when the callback changes
  useEffect(() => {
    onFormChangeRef.current = onFormChange
  }, [onFormChange])

  useEffect(() => {
    // Compare current values with previous values to detect changes
    if (JSON.stringify(form.values) !== JSON.stringify(previousValues.current)) {
      if (onFormChangeRef.current) {
        onFormChangeRef.current()
      }
      previousValues.current = form.values
    }
  }, [form.values]) // Remove onFormChange from dependency array to prevent loops

  // Update initial timing values when initialData changes (form loaded with event data)
  useEffect(() => {
    if (initialData) {
      setInitialTimingValues({
        rsvp: {
          registrationOpenHours: initialData.registrationOpenHours ?? null,
          registrationCloseHours: initialData.registrationCloseHours ?? null,
          cancellationCloseHours: initialData.cancellationCloseHours ?? null,
        },
        volunteer: {
          volunteerRegistrationCloseHours: initialData.volunteerRegistrationCloseHours ?? null,
          volunteerCancellationCloseHours: initialData.volunteerCancellationCloseHours ?? null,
        },
      })
    }
  }, [initialData])

  // Track RSVP/Ticket timing changes separately
  useEffect(() => {
    const hasRsvpTimingChanged =
      form.values.registrationOpenHours !== initialTimingValues.rsvp.registrationOpenHours ||
      form.values.registrationCloseHours !== initialTimingValues.rsvp.registrationCloseHours ||
      form.values.cancellationCloseHours !== initialTimingValues.rsvp.cancellationCloseHours

    setRsvpTimingDirty(hasRsvpTimingChanged)
  }, [
    form.values.registrationOpenHours,
    form.values.registrationCloseHours,
    form.values.cancellationCloseHours,
    initialTimingValues.rsvp,
  ])

  // Track Volunteer timing changes separately
  useEffect(() => {
    const hasVolunteerTimingChanged =
      form.values.volunteerRegistrationCloseHours !==
        initialTimingValues.volunteer.volunteerRegistrationCloseHours ||
      form.values.volunteerCancellationCloseHours !==
        initialTimingValues.volunteer.volunteerCancellationCloseHours

    setVolunteerTimingDirty(hasVolunteerTimingChanged)
  }, [
    form.values.volunteerRegistrationCloseHours,
    form.values.volunteerCancellationCloseHours,
    initialTimingValues.volunteer,
  ])

  // Fetch event email templates when Emails tab is active
  useEffect(() => {
    if (activeTab === 'emails' && eventId) {
      setIsLoadingTemplates(true)
      emailTemplatesApi
        .getEventTemplates(eventId)
        .then((templates) => {
          setEventTemplates(templates)
        })
        .catch((error) => {
          console.error('Failed to fetch event templates:', error)
          notifications.show({
            title: 'Error',
            message: 'Failed to load email templates',
            color: 'red',
            icon: <IconAlertCircle />,
          })
        })
        .finally(() => {
          setIsLoadingTemplates(false)
        })
    }
  }, [activeTab, eventId])

  // Format venues for Select dropdown from API data
  const venues =
    venuesData && Array.isArray(venuesData)
      ? venuesData.map((venue) => ({
          value: venue.id!.toString(),
          label: venue.name!,
        }))
      : []

  // Format teachers for MultiSelect (with fallback to empty array)
  const availableTeachers =
    teachersData && Array.isArray(teachersData) ? formatTeachersForMultiSelect(teachersData) : []

  // Create venue mutation
  // Session management handlers
  const handleEditSession = (sessionId: string) => {
    const session = form.values.sessions.find((s) => s.id === sessionId)
    if (session) {
      setEditingSession(session)
      setSessionModalOpen(true)
    }
  }

  const handleDeleteSession = async (sessionId: string) => {
    if (!eventId) return

    const session = form.values.sessions.find((s) => s.id === sessionId)
    if (!session) return

    setDeleteItemType('session')
    setDeleteItemId(sessionId)
    setDeleteItemName(session.name || 'Unnamed Session')
    setIsCheckingDeletion(true)
    setDeleteModalOpen(true)

    try {
      // Call API to check if session can be deleted
      const response = await apiClient.get(
        `/api/events/${eventId}/sessions/${sessionId}/can-delete`
      )
      setDeletionCheckResponse(response.data)
    } catch (error: any) {
      console.error('Failed to check session deletion eligibility:', error)
      notifications.show({
        title: 'Error',
        message: 'Failed to check if session can be deleted',
        color: 'red',
        icon: <IconAlertCircle />,
      })
      setDeleteModalOpen(false)
    } finally {
      setIsCheckingDeletion(false)
    }
  }

  const handleAddSession = () => {
    setEditingSession(null)
    setSessionModalOpen(true)
  }

  const handleSessionSubmit = async (sessionData: Omit<EventSession, 'id'>) => {
    let updatedSessions: EventSession[]

    if (editingSession) {
      // Update existing session
      updatedSessions = form.values.sessions.map((session) =>
        session.id === editingSession.id ? { ...sessionData, id: editingSession.id } : session
      )
    } else {
      // Add new session
      const newSession: EventSession = {
        ...sessionData,
        id: generateUUID(),
      }
      updatedSessions = [...form.values.sessions, newSession]
    }

    // Update form state immediately for UI feedback
    form.setFieldValue('sessions', updatedSessions)

    // If we have an eventId, save to database immediately
    if (eventId) {
      try {
        await updateEventMutation.mutateAsync({
          id: eventId,
          sessions: updatedSessions,
        })

        notifications.show({
          title: 'Session Saved',
          message: `Session "${sessionData.name}" has been saved successfully.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })

        // Refresh the event data
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
      } catch (error) {
        notifications.show({
          title: 'Save Failed',
          message: getApiErrorMessage(error, 'Failed to save session. Please try again.'),
          color: 'red',
          icon: <IconAlertCircle size={16} />,
        })
      }
    }
  }

  // Ticket type management handlers
  const handleEditTicketType = (ticketTypeId: string) => {
    const ticketType = form.values.ticketTypes.find((t) => t.id === ticketTypeId)
    if (ticketType) {
      setEditingTicketType(ticketType)
      setTicketModalOpen(true)
    }
  }

  const handleDeleteTicketType = async (ticketTypeId: string) => {
    if (!eventId) return

    const ticketType = form.values.ticketTypes.find((t) => t.id === ticketTypeId)
    if (!ticketType) return

    setDeleteItemType('ticketType')
    setDeleteItemId(ticketTypeId)
    setDeleteItemName(ticketType.name || 'Unnamed Ticket Type')
    setIsCheckingDeletion(true)
    setDeleteModalOpen(true)

    try {
      // Call API to check if ticket type can be deleted
      const response = await apiClient.get(
        `/api/events/${eventId}/ticket-types/${ticketTypeId}/can-delete`
      )
      setDeletionCheckResponse(response.data)
    } catch (error: any) {
      console.error('Failed to check ticket type deletion eligibility:', error)
      notifications.show({
        title: 'Error',
        message: 'Failed to check if ticket type can be deleted',
        color: 'red',
        icon: <IconAlertCircle />,
      })
      setDeleteModalOpen(false)
    } finally {
      setIsCheckingDeletion(false)
    }
  }

  const handleAddTicketType = () => {
    setEditingTicketType(null)
    setTicketModalOpen(true)
  }

  // Handle confirmed deletion from modal
  const handleConfirmDeletion = async () => {
    if (!eventId || !deleteItemId) return

    setIsDeletingItem(true)

    try {
      if (deleteItemType === 'session') {
        await apiClient.delete(`/api/events/${eventId}/sessions/${deleteItemId}`)

        notifications.show({
          title: 'Session Deleted',
          message: `Session "${deleteItemName}" has been deleted successfully.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })
      } else {
        await apiClient.delete(`/api/events/${eventId}/ticket-types/${deleteItemId}`)

        notifications.show({
          title: 'Ticket Type Deleted',
          message: `Ticket type "${deleteItemName}" has been deleted successfully.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })
      }

      // Refresh the event data
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })

      // Close modal
      setDeleteModalOpen(false)
      setDeletionCheckResponse(null)
    } catch (error: any) {
      console.error('Failed to delete item:', error)
      notifications.show({
        title: 'Delete Failed',
        message:
          error instanceof Error ? error.message : `Failed to delete ${deleteItemType}. Please try again.`,
        color: 'red',
        icon: <IconAlertCircle size={16} />,
      })
    } finally {
      setIsDeletingItem(false)
    }
  }

  const handleTicketTypeSubmit = async (ticketTypeData: Omit<EventTicketType, 'id'>) => {
    // Modal now uses the same auto-generated TicketTypeDto type — no format conversion needed
    let updatedTicketTypes: EventTicketType[]

    if (editingTicketType) {
      // Update existing ticket type, preserving server-computed fields from the original
      updatedTicketTypes = form.values.ticketTypes.map((ticketType) =>
        ticketType.id === editingTicketType.id
          ? { ...ticketType, ...ticketTypeData }
          : ticketType
      )
    } else {
      // Add new ticket type with a client-generated ID
      const newTicketType: EventTicketType = {
        ...ticketTypeData,
        id: generateUUID(),
      }
      updatedTicketTypes = [...form.values.ticketTypes, newTicketType]
    }

    // Update form state immediately for UI feedback
    form.setFieldValue('ticketTypes', updatedTicketTypes)

    // If we have an eventId, save to database immediately
    if (eventId) {
      try {
        // Transform ticket types to only include relevant price fields based on pricing type
        const ticketTypesForApi = updatedTicketTypes.map((ticket) => {
          const baseTicket = {
            id: ticket.id,
            name: ticket.name,
            pricingType: ticket.pricingType,
            quantityAvailable: ticket.quantityAvailable,
            sessionIdentifiers: ticket.sessionIdentifiers,
            // ✅ REMOVED: salesEndDate field removed from backend DTO
            // salesEndDate: ticket.salesEndDate,
          }

          // Only include price fields relevant to the pricing type
          if (ticket.pricingType === 'Fixed') {
            return {
              ...baseTicket,
              price: ticket.price,
            }
          } else {
            // SlidingScale
            return {
              ...baseTicket,
              minPrice: ticket.minPrice,
              maxPrice: ticket.maxPrice,
              defaultPrice: ticket.defaultPrice,
            }
          }
        })

        await updateEventMutation.mutateAsync({
          id: eventId,
          ticketTypes: ticketTypesForApi,
        })

        notifications.show({
          title: 'Ticket Type Saved',
          message: `Ticket type "${ticketTypeData.name}" has been saved successfully.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })

        // Refresh the event data
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
      } catch (error) {
        notifications.show({
          title: 'Save Failed',
          message: getApiErrorMessage(error, 'Failed to save ticket type. Please try again.'),
          color: 'red',
          icon: <IconAlertCircle size={16} />,
        })
      }
    }
  }

  // No conversion needed — modal now uses the same auto-generated TicketTypeDto type

  // Convert volunteer positions from frontend format to API format
  // Now a simple pass-through since frontend stores sessionId directly (no lookup needed)
  const convertVolunteerPositionsForApi = (positions: VolunteerPosition[]) => {
    return positions.map((vp) => ({
      id: vp.id,
      title: vp.title,
      description: vp.description,
      slotsNeeded: vp.slotsNeeded,
      slotsFilled: vp.slotsFilled,
      sessionId: vp.sessionId,  // Direct pass-through - no conversion needed
      startTime: vp.startTime,
      endTime: vp.endTime,
      isPublicFacing: vp.isPublicFacing,
    }))
  }

  // Volunteer position management handlers
  const handleDeleteVolunteerPosition = async (positionId: string) => {
    const deletedPosition = form.values.volunteerPositions.find((p) => p.id === positionId)
    const updatedPositions = form.values.volunteerPositions.filter(
      (position) => position.id !== positionId
    )

    // Update form state immediately for UI feedback
    form.setFieldValue('volunteerPositions', updatedPositions)

    // If we have an eventId, save to database immediately
    if (eventId) {
      try {
        const volunteerPositionsForApi = convertVolunteerPositionsForApi(updatedPositions)

        await updateEventMutation.mutateAsync({
          id: eventId,
          volunteerPositions: volunteerPositionsForApi,
        })

        notifications.show({
          title: 'Position Deleted',
          message: `Volunteer position "${deletedPosition?.title || 'Position'}" has been deleted.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })

        // Refresh the event data
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
      } catch (error) {
        // Rollback form state on error
        form.setFieldValue('volunteerPositions', [...updatedPositions, deletedPosition!].filter(Boolean))

        notifications.show({
          title: 'Delete Failed',
          message: getApiErrorMessage(error, 'Failed to delete volunteer position. Please try again.'),
          color: 'red',
          icon: <IconAlertCircle size={16} />,
        })
      }
    }
  }

  const handleVolunteerPositionSubmit = async (
    positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'>,
    positionId?: string
  ) => {
    let updatedPositions: VolunteerPosition[]

    if (positionId) {
      // Update existing position
      const existingPosition = form.values.volunteerPositions.find((p) => p.id === positionId)
      if (existingPosition) {
        updatedPositions = form.values.volunteerPositions.map((position) =>
          position.id === positionId
            ? {
                ...positionData,
                id: positionId,
                slotsFilled: existingPosition.slotsFilled,
              }
            : position
        )
      } else {
        return // Position not found, exit early
      }
    } else {
      // Add new position
      const newPosition: VolunteerPosition = {
        ...positionData,
        id: generateUUID(),
        slotsFilled: 0, // Start with no volunteers filled
      }
      updatedPositions = [...form.values.volunteerPositions, newPosition]
    }

    // Update form state immediately for UI feedback
    form.setFieldValue('volunteerPositions', updatedPositions)

    // If we have an eventId, save to database immediately
    if (eventId) {
      try {
        const volunteerPositionsForApi = convertVolunteerPositionsForApi(updatedPositions)

        await updateEventMutation.mutateAsync({
          id: eventId,
          volunteerPositions: volunteerPositionsForApi,
        })

        notifications.show({
          title: 'Position Saved',
          message: `Volunteer position "${positionData.title}" has been saved successfully.`,
          color: 'green',
          icon: <IconCheck size={16} />,
        })

        // Refresh the event data
        queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
      } catch (error) {
        notifications.show({
          title: 'Save Failed',
          message: getApiErrorMessage(error, 'Failed to save volunteer position. Please try again.'),
          color: 'red',
          icon: <IconAlertCircle size={16} />,
        })
      }
    }
  }

  const handleSubmit = form.onSubmit((values) => {
    onSubmit(values)
  })

  // RSVP/Ticket removal handlers
  const handleRemoveRsvpClick = (participation: EventParticipationDto) => {
    // Find ticket purchase amount for this user (if any)
    const userTicket = (participationsData as EventParticipationDto[])?.find(
      (p) => p.userId === participation.userId && p.participationType === 'Ticket'
    )

    // Add ticketAmount to the participation object
    const participationWithTicket = {
      ...participation,
      ticketAmount: userTicket?.amountPaid ?? 0,
    }

    setSelectedParticipant(participationWithTicket as any)
    setRemoveRsvpModalOpen(true)
  }

  const handleRefundTicketClick = (participation: EventParticipationDto) => {
    setSelectedParticipant(participation)
    setRefundTicketModalOpen(true)
  }

  const handleRemoveRsvpConfirm = async () => {
    if (!selectedParticipant || !eventId) return

    try {
      const response = await apiClient.delete(
        `/api/admin/events/${eventId}/participations/${selectedParticipant.userId}`
      )

      if (response.status !== 200 && response.status !== 204) {
        throw new Error('Failed to remove RSVP')
      }

      notifications.show({
        message: 'RSVP removed successfully',
        color: 'green',
        autoClose: 3000,
      })

      // Refetch participations to update the tables
      queryClient.invalidateQueries({ queryKey: eventKeys.participations(eventId) })

      // Close modal
      setRemoveRsvpModalOpen(false)
      setSelectedParticipant(null)
    } catch (error) {
      notifications.show({
        message: 'Failed to remove RSVP',
        color: 'red',
        autoClose: 5000,
      })
    }
  }

  const handleRefundTicketConfirm = async (
    refundAmount: number,
    refundReason: string,
    cancelTicket: boolean,
    alsoRemoveRsvp: boolean
  ) => {
    // Use ticketId (TicketPurchaseId) for the refund endpoint, not the attendance id
    const ticketPurchaseId = selectedParticipant?.ticketId ?? selectedParticipant?.id
    if (!selectedParticipant || !ticketPurchaseId) {
      throw new Error('No transaction ID available for refund')
    }

    try {
      const response = await apiClient.post(
        `/api/payments/transactions/${ticketPurchaseId}/refund`,
        {
          refundAmount,
          refundReason,
          cancelTicket,
          alsoRemoveRsvp,
        }
      )

      if (response.status !== 200) {
        throw new Error('Failed to process request')
      }

      // Refetch participations to update the tables
      if (eventId) {
        queryClient.invalidateQueries({ queryKey: eventKeys.participations(eventId) })
      }

      // Close modal and clear selection
      setRefundTicketModalOpen(false)
      setSelectedParticipant(null)
    } catch (error: any) {
      // Re-throw to let modal handle the error display
      throw error
    }
  }

  // Email template state for editing
  const [templateSubject, setTemplateSubject] = useState<string>('')
  const [templateContent, setTemplateContent] = useState<string>('')
  const [targetSessions, setTargetSessions] = useState<string[]>(['all'])

  // Get currently selected template
  const selectedTemplate = eventTemplates.find((t) => t.templateType === activeEmailTemplate)

  // Update editor state when active template changes
  useEffect(() => {
    if (selectedTemplate) {
      setTemplateSubject(selectedTemplate.subject || '')
      setTemplateContent(selectedTemplate.htmlBody || '')
      setTargetSessions(selectedTemplate.targetSessions || ['all'])
    } else {
      // Reset editor for ad-hoc or when no template selected
      setTemplateSubject('')
      setTemplateContent('')
      setTargetSessions(['all'])
    }
  }, [activeEmailTemplate, selectedTemplate])

  // Email template helper functions
  const getActiveTemplateTitle = () => {
    if (activeEmailTemplate === 'ad-hoc') {
      return 'Ad-Hoc Email'
    }
    return selectedTemplate?.templateType || activeEmailTemplate
  }

  const getTemplateSubject = () => {
    return templateSubject
  }

  const getTemplateContent = () => {
    return templateContent
  }

  // Save template mutation
  const saveTemplateMutation = useMutation({
    mutationFn: async ({
      eventId,
      templateType,
      request,
    }: {
      eventId: string
      templateType: string
      request: UpdateEventTemplateRequest
    }) => {
      await emailTemplatesApi.updateEventTemplate(eventId, templateType, request)
    },
    onSuccess: () => {
      if (eventId) {
        emailTemplatesApi.getEventTemplates(eventId).then(setEventTemplates)
      }
      notifications.show({
        title: 'Success',
        message: 'Template saved successfully',
        color: 'green',
        icon: <IconCheck />,
      })
    },
    onError: () => {
      notifications.show({
        title: 'Error',
        message: 'Failed to save template',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    },
  })

  const handleSaveTemplate = () => {
    if (!eventId || !activeEmailTemplate || activeEmailTemplate === 'ad-hoc') return

    saveTemplateMutation.mutate({
      eventId,
      templateType: activeEmailTemplate,
      request: {
        subject: templateSubject,
        htmlBody: templateContent,
        plainTextBody: templateContent.replace(/<[^>]*>/g, ''), // Strip HTML for plain text
        targetSessions: targetSessions,
      },
    })
  }

  // Save RSVP/Ticket timing fields only
  const handleSaveRsvpTiming = async () => {
    if (!eventId) return

    try {
      // Create partial update with ALL timing fields to prevent data loss
      // Backend detects timing-only update and updates all timing fields,
      // so we must include volunteer timing to preserve those values
      await updateEventMutation.mutateAsync({
        id: eventId,
        registrationOpenHours: form.values.registrationOpenHours,
        registrationCloseHours: form.values.registrationCloseHours,
        cancellationCloseHours: form.values.cancellationCloseHours,
        volunteerRegistrationCloseHours: form.values.volunteerRegistrationCloseHours,
        volunteerCancellationCloseHours: form.values.volunteerCancellationCloseHours,
      })

      // Update initial values to current values
      setInitialTimingValues((prev) => ({
        ...prev,
        rsvp: {
          registrationOpenHours: form.values.registrationOpenHours ?? null,
          registrationCloseHours: form.values.registrationCloseHours ?? null,
          cancellationCloseHours: form.values.cancellationCloseHours ?? null,
        },
      }))
      setRsvpTimingDirty(false)

      notifications.show({
        title: 'Timing Saved',
        message: 'RSVP/Ticket timing settings have been saved successfully.',
        color: 'green',
      })

      // Refresh event data
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
    } catch (error) {
      notifications.show({
        title: 'Save Failed',
        message: getApiErrorMessage(error, 'Failed to save timing settings.'),
        color: 'red',
      })
    }
  }

  // Save Volunteer timing fields only
  const handleSaveVolunteerTiming = async () => {
    if (!eventId) return

    try {
      // Create partial update with ALL timing fields to prevent data loss
      // Backend detects timing-only update and updates all timing fields,
      // so we must include RSVP timing to preserve those values
      await updateEventMutation.mutateAsync({
        id: eventId,
        registrationOpenHours: form.values.registrationOpenHours,
        registrationCloseHours: form.values.registrationCloseHours,
        cancellationCloseHours: form.values.cancellationCloseHours,
        volunteerRegistrationCloseHours: form.values.volunteerRegistrationCloseHours,
        volunteerCancellationCloseHours: form.values.volunteerCancellationCloseHours,
      })

      // Update initial values to current values
      setInitialTimingValues((prev) => ({
        ...prev,
        volunteer: {
          volunteerRegistrationCloseHours: form.values.volunteerRegistrationCloseHours ?? null,
          volunteerCancellationCloseHours: form.values.volunteerCancellationCloseHours ?? null,
        },
      }))
      setVolunteerTimingDirty(false)

      notifications.show({
        title: 'Timing Saved',
        message: 'Volunteer timing settings have been saved successfully.',
        color: 'green',
      })

      // Refresh event data
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) })
    } catch (error) {
      notifications.show({
        title: 'Save Failed',
        message: getApiErrorMessage(error, 'Failed to save timing settings.'),
        color: 'red',
      })
    }
  }

  return (
    <Card
      shadow="md"
      radius="lg"
      p="xl"
      style={{ backgroundColor: 'white' }}
      data-testid="event-form"
    >
      <form onSubmit={handleSubmit}>
        <Tabs
          value={activeTab}
          onChange={(value) => setActiveTab(value ?? 'basic-info')}
          variant="pills"
          radius="md"
          data-testid="tabs-event-management"
          classNames={{ tab: 'wcr-admin-tab' }}
        >
          <Tabs.List
            style={{
              backgroundColor: 'var(--mantine-color-gray-0)',
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              padding: 'var(--mantine-spacing-md)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <Group gap="xs">
              <Tabs.Tab value="basic-info" data-testid="tab-basic-info">
                Basic Info
              </Tabs.Tab>
              <Tabs.Tab value="setup" data-testid="setup-tab">
                Sessions / Ticket Types
              </Tabs.Tab>
              <Tabs.Tab value="rsvp-tickets" data-testid="rsvp-tickets-tab">
                RSVP/Tickets
              </Tabs.Tab>
              <Tabs.Tab value="volunteers" data-testid="tab-volunteers">
                Volunteers
              </Tabs.Tab>
              <Tabs.Tab value="emails" data-testid="tab-emails">
                Emails
              </Tabs.Tab>
              <Tabs.Tab value="attendees" data-testid="attendees-tab">
                Attendees
              </Tabs.Tab>
            </Group>
            {tabsRightSection && <div style={{ marginLeft: 'auto' }}>{tabsRightSection}</div>}
          </Tabs.List>

          {/* Basic Info Tab */}
          <Tabs.Panel value="basic-info" pt="xl" data-testid="panel-basic-info">
            <Stack gap="xl">
              {/* Event Registration Options - Moved above Event Details */}
              <SimpleGrid cols={form.values.allowRsvps ? 3 : 2} spacing="md" mb={0}>
                <Checkbox
                  label="Vetted Members Only"
                  description="Restrict attendance to vetted members only"
                  {...form.getInputProps('vettedMembersOnly', { type: 'checkbox' })}
                />
                <Checkbox
                  label="Allow RSVPs"
                  description="Enable free RSVPs for this event. When checked, you can choose whether payment is required or optional."
                  {...form.getInputProps('allowRsvps', { type: 'checkbox' })}
                />
                {form.values.allowRsvps && (
                  <Checkbox
                    label="Require Payment to Attend"
                    description="When checked, attendees must purchase a ticket to enter (online ahead of time or at the door). When unchecked, this is a free event — an RSVP is all that's needed to attend."
                    {...form.getInputProps('requireTicketPurchase', { type: 'checkbox' })}
                  />
                )}
              </SimpleGrid>

              {/* Event Details Section */}
              <div>
                <Group justify="space-between" align="center" mb="md" style={{ borderBottom: '2px solid var(--mantine-color-burgundy-3)', paddingBottom: '8px' }}>
                  <Title order={2} c="burgundy">
                    Event Details
                  </Title>
                  <Group gap="sm">
                    <WCRButton variant="outline" onClick={onCancel} size="sm">
                      Cancel
                    </WCRButton>
                    <WCRButton
                      type="submit"
                      loading={isSubmitting}
                      variant="secondary"
                      size="sm"
                      disabled={!formDirty}
                    >
                      {isSubmitting ? 'Saving...' : 'Save'}
                    </WCRButton>
                  </Group>
                </Group>

                {/* Event Title and Short Description - Two Column Layout */}
                <Group grow align="flex-start" gap="md" mb="md">
                  {/* Event Title */}
                  <TextInput
                    label="Event Title"
                    placeholder="Enter event title"
                    required
                    {...form.getInputProps('title')}
                  />

                  {/* Short Description */}
                  <TextInput
                    label="Short Description (160 Char Max)"
                    placeholder="Brief description for cards and grid views"
                    required
                    maxLength={160}
                    {...form.getInputProps('shortDescription')}
                  />
                </Group>

                {/* Full Description */}
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Full Event Description{' '}
                    <Text component="span" c="red">
                      *
                    </Text>
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    This detailed description will be visible on the public events page
                  </Text>
                  <div style={{ minHeight: form.values.fullDescription ? 'auto' : '100px' }}>
                    <MantineTiptapEditor
                      value={form.values.fullDescription}
                      onChange={(content) => form.setFieldValue('fullDescription', content)}
                      minRows={form.values.fullDescription ? undefined : 3}
                      placeholder="Enter detailed event description..."
                    />
                  </div>
                  {form.errors.fullDescription && (
                    <Text size="xs" c="red" mt={5}>
                      {form.errors.fullDescription}
                    </Text>
                  )}
                </div>

                {/* Policies & Procedures */}
                <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                  <Text size="sm" fw={500} mb={5}>
                    Policies & Procedures
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    Studio-specific policies, prerequisites, safety requirements, etc. (managed by
                    studio/admin, teachers cannot edit)
                  </Text>
                  <div style={{ minHeight: form.values.policies ? 'auto' : '100px' }}>
                    <MantineTiptapEditor
                      value={form.values.policies}
                      onChange={(content) => form.setFieldValue('policies', content)}
                      minRows={form.values.policies ? undefined : 3}
                      placeholder="Enter policies and procedures..."
                    />
                  </div>
                  {form.errors.policies && (
                    <Text size="xs" c="red" mt={5}>
                      {form.errors.policies}
                    </Text>
                  )}
                </div>
              </div>

              {/* Venue and Teachers Section - Two Column Layout */}
              <Group grow align="flex-start" gap="xl">
                {/* Venue Section */}
                <div>
                  <Title
                    order={2}
                    c="burgundy"
                    mb="md"
                    style={{
                      borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                      paddingBottom: '8px',
                    }}
                  >
                    Venue
                  </Title>
                  <Select
                    label="Venue"
                    placeholder="Select venue..."
                    data={venues}
                    required
                    {...form.getInputProps('venueId')}
                  />
                </div>

                {/* Teachers/Instructors Section */}
                <div>
                  <Title
                    order={2}
                    c="burgundy"
                    mb="md"
                    style={{
                      borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                      paddingBottom: '8px',
                    }}
                  >
                    Teachers/Instructors
                  </Title>
                  {/* DEBUG: Log teacher selection data */}
                  {/* {console.log('🔍 [DEBUG] Teacher selection data:', {
                    teachersData,
                    teachersLoading,
                    teachersError,
                    availableTeachers,
                    currentTeacherIds: form.values.teacherIds,
                    teacherInputProps: form.getInputProps('teacherIds')
                  })} */}

                  {teachersError && (
                    <Alert color="red" mb="md" title="Error Loading Teachers">
                      Failed to load teachers list. Using form without teacher selection.
                    </Alert>
                  )}

                  <MultiSelect
                    label="Select Teachers"
                    placeholder={
                      teachersLoading ? 'Loading teachers...' : 'Choose teachers for this event'
                    }
                    data={availableTeachers}
                    searchable
                    disabled={teachersLoading || !!teachersError}
                    {...form.getInputProps('teacherIds')}
                  />
                </div>
              </Group>

              {/* Save Buttons */}
              <Group justify="flex-end" mt="xl">
                <WCRButton variant="outline" onClick={onCancel} size="lg">
                  Cancel
                </WCRButton>
                <WCRButton
                  type="submit"
                  loading={isSubmitting}
                  variant="secondary"
                  size="lg"
                  disabled={!formDirty}
                >
                  {isSubmitting ? 'Saving...' : 'Save'}
                </WCRButton>
              </Group>
            </Stack>
          </Tabs.Panel>

          {/* Setup Tab - Combined Sessions and Tickets */}
          <Tabs.Panel value="setup" pt="xl" data-testid="setup-tab">
            <Stack gap="xl">
              {/* Event Sessions Section */}
              <div data-testid="sessions-section">
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Event Sessions
                </Title>
                <EventSessionsGrid
                  sessions={form.values.sessions}
                  onEditSession={handleEditSession}
                  onDeleteSession={handleDeleteSession}
                  onAddSession={handleAddSession}
                />
              </div>

              {/* Ticket Types Section */}
              <div data-testid="tickets-section">
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Ticket Types
                </Title>
                <EventTicketTypesGrid
                  ticketTypes={form.values.ticketTypes}
                  onEditTicketType={handleEditTicketType}
                  onDeleteTicketType={handleDeleteTicketType}
                  onAddTicketType={handleAddTicketType}
                  hasSessions={form.values.sessions.length > 0}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Emails Tab - EXACT WIREFRAME MATCH */}
          <Tabs.Panel value="emails" pt="xl" data-testid="panel-emails">
            <Stack gap="xl">
              <Title
                order={2}
                c="burgundy"
                mb="md"
                style={{
                  borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                  paddingBottom: '8px',
                }}
              >
                Email Templates
              </Title>

              <Text size="sm" c="dimmed" mb="lg">
                Click on a template card to edit it below, or select "Send Ad-Hoc Email" to send
                one-time messages.
              </Text>

              {/* Template Cards Container - Dynamic from API */}
              {isLoadingTemplates ? (
                <Text c="dimmed">Loading email templates...</Text>
              ) : (
                <Group gap="md" style={{ flexWrap: 'wrap' }}>
                  {/* Send Ad-Hoc Email Card - Always Present */}
                  <Card
                    withBorder
                    p="md"
                    style={{
                      cursor: 'pointer',
                      borderColor:
                        activeEmailTemplate === 'ad-hoc'
                          ? 'var(--mantine-color-burgundy-6)'
                          : 'var(--mantine-color-rose-3)',
                      backgroundColor:
                        activeEmailTemplate === 'ad-hoc' ? 'rgba(136, 1, 36, 0.05)' : 'white',
                      minWidth: '220px',
                      flex: 1,
                      maxWidth: '300px',
                      position: 'relative',
                      transition: 'all 0.3s ease',
                    }}
                    onClick={() => setActiveEmailTemplate('ad-hoc')}
                  >
                    <Text fw={600} c="burgundy" mb={4}>
                      Send Ad-Hoc Email
                    </Text>
                    <Text size="sm" c="stone" mb="xs">
                      Send one-time messages to specific groups
                    </Text>
                    <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                      Any recipients
                    </Text>
                  </Card>

                  {/* Dynamic Template Cards from API */}
                  {eventTemplates.map((template) => (
                    <Card
                      key={template.id}
                      withBorder
                      p="md"
                      style={{
                        cursor: 'pointer',
                        borderColor:
                          activeEmailTemplate === template.templateType
                            ? 'var(--mantine-color-burgundy-6)'
                            : 'var(--mantine-color-rose-3)',
                        backgroundColor:
                          activeEmailTemplate === template.templateType
                            ? 'rgba(136, 1, 36, 0.05)'
                            : 'white',
                        minWidth: '220px',
                        flex: 1,
                        maxWidth: '300px',
                        position: 'relative',
                        transition: 'all 0.3s ease',
                      }}
                      onClick={() => setActiveEmailTemplate(template.templateType!)}
                    >
                      {/* Customization Badge */}
                      {template.isCustomized ? (
                        <Badge
                          color="green"
                          size="sm"
                          style={{ position: 'absolute', top: 8, right: 8 }}
                        >
                          ✓ Customized
                        </Badge>
                      ) : (
                        <Badge
                          color="gray"
                          size="sm"
                          variant="light"
                          style={{ position: 'absolute', top: 8, right: 8 }}
                        >
                          (Default)
                        </Badge>
                      )}

                      <Text fw={600} c="burgundy" mb={4}>
                        {template.templateType}
                      </Text>
                      <Text size="sm" c="stone" mb="xs">
                        {template.subject}
                      </Text>
                      <Text size="xs" c="dimmed" style={{ fontStyle: 'italic' }}>
                        {template.targetSessions?.join(', ') || 'All sessions'}
                      </Text>
                    </Card>
                  ))}
                </Group>
              )}

              {/* Unified Editor Section - Only show when a template is selected */}
              {activeEmailTemplate && (
                <div style={{ marginTop: 'var(--mantine-spacing-xl)' }}>
                  <div style={{ marginBottom: 'var(--mantine-spacing-md)' }}>
                    <Text fw={600} c="burgundy">
                      Currently Editing: {getActiveTemplateTitle()}
                    </Text>
                  </div>

                {/* Recipient Group (shown for ad-hoc) */}
                {activeEmailTemplate === 'ad-hoc' && (
                  <Select
                    label="Recipient Group"
                    data={[
                      { value: 'all-tickets', label: 'All Ticket Holders' },
                      { value: 'all-rsvps', label: 'All RSVPs' },
                      { value: 'volunteers', label: 'All Volunteers' },
                      { value: 'everyone', label: 'Everyone' },
                      { value: 'teachers', label: 'Teachers' },
                      { value: 'session-s1', label: 'S1 Attendees' },
                      { value: 'session-s2', label: 'S2 Attendees' },
                      { value: 'session-s3', label: 'S3 Attendees' },
                    ]}
                    mb="md"
                  />
                )}

                {/* Target Sessions (shown for templates) */}
                {activeEmailTemplate !== 'ad-hoc' && (
                  <MultiSelect
                    label="Target Sessions"
                    description="Which sessions should trigger this email? Hold Ctrl/Cmd for multiple selections."
                    data={[
                      { value: 'all', label: 'All Sessions' },
                      { value: 's1', label: 'S1' },
                      { value: 's2', label: 'S2' },
                      { value: 's3', label: 'S3' },
                    ]}
                    value={targetSessions}
                    onChange={setTargetSessions}
                    mb="md"
                  />
                )}

                <TextInput
                  label="Subject Line"
                  value={getTemplateSubject()}
                  onChange={(e) => setTemplateSubject(e.currentTarget.value)}
                  mb="md"
                />

                <div>
                  <Text size="sm" fw={500} mb={5}>
                    Email Content
                  </Text>
                  <Text size="xs" c="dimmed" mb="xs">
                    Available variables: {'{name}'}, {'{event}'}, {'{date}'}, {'{time}'},{' '}
                    {'{venue}'}, {'{venue_address}'}
                  </Text>
                  <MantineTiptapEditor
                    value={getTemplateContent()}
                    onChange={setTemplateContent}
                    minRows={10}
                    placeholder="Enter email content..."
                  />
                </div>

                <Group mt="md" justify="space-between">
                  <div>
                    {/* Reset to Default button - only show for customized templates */}
                    {selectedTemplate && selectedTemplate.isCustomized && (
                      <Button
                        variant="light"
                        color="red"
                        onClick={() => {
                          setTemplateToReset(selectedTemplate)
                          setResetModalOpen(true)
                        }}
                        styles={{
                          root: {
                            fontWeight: 600,
                            height: '44px',
                            paddingTop: '12px',
                            paddingBottom: '12px',
                            fontSize: '14px',
                            lineHeight: '1.2',
                          },
                        }}
                      >
                        Reset to Default
                      </Button>
                    )}
                  </div>
                  <div>
                    {activeEmailTemplate === 'ad-hoc' ? (
                      <WCRButton variant="primary" size="lg" disabled>
                        Send Email
                      </WCRButton>
                    ) : (
                      <WCRButton
                        variant="primary"
                        size="lg"
                        onClick={handleSaveTemplate}
                        disabled={!eventId || saveTemplateMutation.isPending}
                      >
                        {saveTemplateMutation.isPending ? 'Saving...' : 'Save Changes'}
                      </WCRButton>
                    )}
                  </div>
                </Group>
              </div>
              )}
            </Stack>
          </Tabs.Panel>

          {/* Reset Template Confirmation Modal */}
          <Modal
            opened={resetModalOpen}
            onClose={() => {
              setResetModalOpen(false)
              setTemplateToReset(null)
            }}
            title={<Title order={3}>Reset Template to Default?</Title>}
          >
            <Text mb="md">
              Are you sure you want to reset <strong>{templateToReset?.templateType}</strong> to
              the global default template? This will delete your customizations and cannot be
              undone.
            </Text>

            <Group justify="flex-end" mt="lg">
              <Button
                variant="default"
                onClick={() => {
                  setResetModalOpen(false)
                  setTemplateToReset(null)
                }}
              >
                Cancel
              </Button>
              <Button
                color="red"
                onClick={() => {
                  if (eventId && templateToReset) {
                    resetTemplateMutation.mutate({
                      eventId,
                      templateType: templateToReset.templateType!,
                    })
                  }
                }}
                loading={resetTemplateMutation.isPending}
              >
                Reset to Default
              </Button>
            </Group>
          </Modal>

          {/* Volunteers Tab - Modal-based consistent with other tabs */}
          <Tabs.Panel value="volunteers" pt="xl" data-testid="panel-volunteers">
            <Stack gap="xl">
              {/* Timing Settings - Always Visible */}
              <Box>
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Volunteer Timing Controls
                </Title>

                <Box>
                  <Box
                    id="volunteer-timing-settings"
                    p="md"
                    style={{
                      background:
                        'linear-gradient(135deg, var(--mantine-color-burgundy-0) 0%, var(--mantine-color-plum-0) 100%)',
                      borderRadius: '8px',
                      border: '1px solid var(--mantine-color-burgundy-2)',
                    }}
                  >
                    <Stack gap="md">
                      <Text size="sm" c="dimmed" mb="md">
                        Control when volunteers can sign up and cancel their shifts. Examples: 24 =
                        1 day before, 48 = 2 days before, 168 = 1 week before. Positive values =
                        hours before event start. Negative values = hours after event start. Leave
                        blank for no restrictions (always open).
                      </Text>

                      {/* Two-column layout with inline labels within each column */}
                      <Group grow align="flex-start">
                        {/* Column 1: Volunteer Registration Closes */}
                        <Stack gap="xs">
                          <Group gap="xs" align="center" wrap="nowrap">
                            <Text size="sm" fw={500} style={{ whiteSpace: 'nowrap' }}>
                              Volunteer Registration Closes:
                            </Text>
                            <NumberInput
                              placeholder="Not Set = Never Closes"
                              min={-24}
                              max={8760}
                              step={0.5}
                              decimalScale={1}
                              allowNegative={true}
                              value={form.values.volunteerRegistrationCloseHours ?? undefined}
                              onChange={(value) =>
                                form.setFieldValue(
                                  'volunteerRegistrationCloseHours',
                                  typeof value === 'number' ? value : null
                                )
                              }
                              error={form.errors.volunteerRegistrationCloseHours}
                              aria-label="Volunteer Registration Closes"
                              aria-describedby="volunteer-registration-close-help"
                              style={{ flex: 1 }}
                            />
                          </Group>
                        </Stack>

                        {/* Column 2: Volunteer Cancellation Closes */}
                        <Stack gap="xs">
                          <Group gap="xs" align="center" wrap="nowrap">
                            <Text size="sm" fw={500} style={{ whiteSpace: 'nowrap' }}>
                              Volunteer Cancellation Closes:
                            </Text>
                            <NumberInput
                              placeholder="Not Set = Always can cancel"
                              min={-24}
                              max={8760}
                              step={0.5}
                              decimalScale={1}
                              allowNegative={true}
                              value={form.values.volunteerCancellationCloseHours ?? undefined}
                              onChange={(value) =>
                                form.setFieldValue(
                                  'volunteerCancellationCloseHours',
                                  typeof value === 'number' ? value : null
                                )
                              }
                              error={form.errors.volunteerCancellationCloseHours}
                              aria-label="Volunteer Cancellation Closes"
                              aria-describedby="volunteer-cancellation-close-help"
                              style={{ flex: 1 }}
                            />
                          </Group>
                        </Stack>
                      </Group>

                      {/* Save Timing Button */}
                      <Group justify="flex-start" mt="md">
                        <WCRButton
                          onClick={handleSaveVolunteerTiming}
                          loading={updateEventMutation.isPending}
                          variant="secondary"
                          size="lg"
                          disabled={!volunteerTimingDirty}
                        >
                          {updateEventMutation.isPending ? 'Saving...' : 'Save'}
                        </WCRButton>
                      </Group>
                    </Stack>
                  </Box>
                </Box>
              </Box>

              {/* Volunteer Positions */}
              <div>
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Volunteer Positions
                </Title>
                <VolunteerPositionsGrid
                  positions={form.values.volunteerPositions}
                  onPositionSubmit={handleVolunteerPositionSubmit}
                  onDeletePosition={handleDeleteVolunteerPosition}
                  availableSessions={form.values.sessions.map((s) => ({
                    id: s.id || '',
                    sessionIdentifier: s.sessionIdentifier ?? '',
                    name: s.name ?? '',
                  }))}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* RSVP/Tickets Tab - Updated per requirements */}
          <Tabs.Panel value="rsvp-tickets" pt="xl" data-testid="rsvp-tickets-tab">
            <Stack gap="xl">
              {/* Timing Settings - Always Visible */}
              <Box>
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Sales and Cancellation Timing
                </Title>

                <Box>
                  <Box
                    id="rsvp-timing-settings"
                    p="md"
                    style={{
                      background:
                        'linear-gradient(135deg, var(--mantine-color-burgundy-0) 0%, var(--mantine-color-plum-0) 100%)',
                      borderRadius: '8px',
                      border: '1px solid var(--mantine-color-burgundy-2)',
                    }}
                  >
                    <Stack gap="md">
                      <Text size="sm" c="dimmed" mb="md">
                        Control when users can register and cancel. Examples: 24 = 1 day before, 48
                        = 2 days before, 168 = 1 week before. Positive values = hours before event
                        start. Negative values = hours after event start. Leave blank for no
                        restrictions (always open).
                      </Text>

                      {/* Three-column layout: all timing fields on same line */}
                      <Group grow align="flex-start">
                        {/* Column 1: RSVP/Sales Starts */}
                        <Stack gap="xs">
                          <Group gap="xs" align="center" wrap="nowrap">
                            <Text size="sm" fw={500} style={{ whiteSpace: 'nowrap' }}>
                              RSVP/Sales Starts:
                            </Text>
                            <NumberInput
                              placeholder="Not Set = Always Open"
                              min={-24}
                              max={8760}
                              allowNegative={true}
                              value={form.values.registrationOpenHours ?? undefined}
                              onChange={(value) =>
                                form.setFieldValue(
                                  'registrationOpenHours',
                                  typeof value === 'number' ? value : null
                                )
                              }
                              error={form.errors.registrationOpenHours}
                              aria-label="RSVP/Sales Starts"
                              aria-describedby="registration-open-help"
                              style={{ flex: 1 }}
                            />
                          </Group>
                        </Stack>

                        {/* Column 2: RSVP/Sales Ends */}
                        <Stack gap="xs">
                          <Group gap="xs" align="center" wrap="nowrap">
                            <Text size="sm" fw={500} style={{ whiteSpace: 'nowrap' }}>
                              RSVP/Sales Ends:
                            </Text>
                            <NumberInput
                              placeholder="Not Set = Never Ends"
                              min={-24}
                              max={8760}
                              step={0.5}
                              decimalScale={1}
                              allowNegative={true}
                              value={form.values.registrationCloseHours ?? undefined}
                              onChange={(value) =>
                                form.setFieldValue(
                                  'registrationCloseHours',
                                  typeof value === 'number' ? value : null
                                )
                              }
                              error={form.errors.registrationCloseHours}
                              aria-label="RSVP/Sales Ends"
                              aria-describedby="registration-close-help"
                              style={{ flex: 1 }}
                            />
                          </Group>
                        </Stack>

                        {/* Column 3: Cancellation Closes */}
                        <Stack gap="xs">
                          <Group gap="xs" align="center" wrap="nowrap">
                            <Text size="sm" fw={500} style={{ whiteSpace: 'nowrap' }}>
                              Cancellation Closes:
                            </Text>
                            <NumberInput
                              placeholder="Not Set = Never Closes"
                              min={-24}
                              max={8760}
                              step={0.5}
                              decimalScale={1}
                              allowNegative={true}
                              value={form.values.cancellationCloseHours ?? undefined}
                              onChange={(value) =>
                                form.setFieldValue(
                                  'cancellationCloseHours',
                                  typeof value === 'number' ? value : null
                                )
                              }
                              error={form.errors.cancellationCloseHours}
                              aria-label="Cancellation Closes"
                              aria-describedby="cancellation-close-help"
                              style={{ flex: 1 }}
                            />
                          </Group>
                        </Stack>
                      </Group>

                      {/* Save Timing Button */}
                      <Group justify="flex-start" mt="md">
                        <WCRButton
                          onClick={handleSaveRsvpTiming}
                          loading={updateEventMutation.isPending}
                          variant="secondary"
                          size="lg"
                          disabled={!rsvpTimingDirty}
                        >
                          {updateEventMutation.isPending ? 'Saving...' : 'Save'}
                        </WCRButton>
                      </Group>
                    </Stack>
                  </Box>
                </Box>
              </Box>
              {/* RSVPs Table - Show only if RSVPs are allowed */}
              {form.values.allowRsvps && (
                <div data-testid="rsvps-section">
                  <Title
                    order={2}
                    c="burgundy"
                    mb="md"
                    style={{
                      borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                      paddingBottom: '8px',
                    }}
                  >
                    RSVPs Management
                  </Title>
                  <Text size="sm" c="dimmed" mb="lg">
                    View and manage all RSVPs for this social event.
                  </Text>

                  <Table
                    striped
                    highlightOnHover
                    withTableBorder
                    data-testid="rsvps-table"
                    style={{
                      backgroundColor: 'white',
                      borderRadius: '12px',
                      overflow: 'hidden',
                      boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
                    }}
                  >
                    <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
                      <Table.Tr>
                        <Table.Th
                          onClick={() => handleRsvpSort('name')}
                          style={{
                            color: 'white',
                            fontWeight: 600,
                            textTransform: 'uppercase',
                            letterSpacing: '1px',
                            cursor: 'pointer',
                            userSelect: 'none',
                          }}
                        >
                          <Group gap={4}>
                            Name
                            {renderSortIcon('name', rsvpSortColumn, rsvpSortDirection)}
                          </Group>
                        </Table.Th>
                        <Table.Th
                          onClick={() => handleRsvpSort('email')}
                          style={{
                            color: 'white',
                            fontWeight: 600,
                            textTransform: 'uppercase',
                            letterSpacing: '1px',
                            cursor: 'pointer',
                            userSelect: 'none',
                          }}
                        >
                          <Group gap={4}>
                            Email
                            {renderSortIcon('email', rsvpSortColumn, rsvpSortDirection)}
                          </Group>
                        </Table.Th>
                        <Table.Th
                          onClick={() => handleRsvpSort('status')}
                          style={{
                            color: 'white',
                            fontWeight: 600,
                            textTransform: 'uppercase',
                            letterSpacing: '1px',
                            cursor: 'pointer',
                            userSelect: 'none',
                          }}
                        >
                          <Group gap={4}>
                            Status
                            {renderSortIcon('status', rsvpSortColumn, rsvpSortDirection)}
                          </Group>
                        </Table.Th>
                        <Table.Th
                          onClick={() => handleRsvpSort('date')}
                          style={{
                            color: 'white',
                            fontWeight: 600,
                            textTransform: 'uppercase',
                            letterSpacing: '1px',
                            cursor: 'pointer',
                            userSelect: 'none',
                          }}
                        >
                          <Group gap={4}>
                            RSVP Date
                            {renderSortIcon('date', rsvpSortColumn, rsvpSortDirection)}
                          </Group>
                        </Table.Th>
                        <Table.Th
                          style={{
                            color: 'white',
                            fontWeight: 600,
                            textTransform: 'uppercase',
                            letterSpacing: '1px',
                          }}
                        >
                          Actions
                        </Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {participationsLoading ? (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="dimmed" py="xl">
                              Loading RSVPs...
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      ) : participationsError ? (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="red" py="xl">
                              Error loading RSVPs: {participationsError.message}
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      ) : participationsData &&
                        (participationsData as EventParticipationDto[]).length > 0 ? (
                        (participationsData as EventParticipationDto[])
                          .filter((p) => p.participationType === 'RSVP')
                          .sort((a, b) => {
                            let aVal: any, bVal: any
                            switch (rsvpSortColumn) {
                              case 'name':
                                aVal = (a.userSceneName ?? '').toLowerCase()
                                bVal = (b.userSceneName ?? '').toLowerCase()
                                break
                              case 'email':
                                aVal = (a.userEmail ?? '').toLowerCase()
                                bVal = (b.userEmail ?? '').toLowerCase()
                                break
                              case 'status':
                                aVal = a.status
                                bVal = b.status
                                break
                              case 'date':
                                aVal = new Date(a.participationDate ?? '').getTime()
                                bVal = new Date(b.participationDate ?? '').getTime()
                                break
                              default:
                                aVal = a.userSceneName
                                bVal = b.userSceneName
                            }
                            if (rsvpSortDirection === 'asc') {
                              return aVal < bVal ? -1 : aVal > bVal ? 1 : 0
                            } else {
                              return aVal > bVal ? -1 : aVal < bVal ? 1 : 0
                            }
                          })
                          .map((participation) => (
                            <Table.Tr key={participation.id}>
                              <Table.Td>
                                <Text fw={500}>{participation.userSceneName}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm" c="dimmed">
                                  {participation.userEmail}
                                </Text>
                              </Table.Td>
                              <Table.Td>
                                <Badge
                                  color={participation.status === 'Active' ? 'green' : 'red'}
                                  variant="light"
                                >
                                  {participation.status}
                                </Badge>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm">
                                  {new Date(participation.participationDate ?? '').toLocaleDateString('en-US', { timeZone: eventTimeZone })}
                                </Text>
                              </Table.Td>
                              <Table.Td>
                                {participation.status === 'Active' && (
                                  <Text
                                    size="sm"
                                    c="red"
                                    style={{ cursor: 'pointer', textDecoration: 'underline' }}
                                    onClick={() => handleRemoveRsvpClick(participation)}
                                    data-testid={`remove-rsvp-${participation.id}`}
                                  >
                                    Remove
                                  </Text>
                                )}
                              </Table.Td>
                            </Table.Tr>
                          ))
                      ) : (
                        <Table.Tr>
                          <Table.Td colSpan={5}>
                            <Text ta="center" c="dimmed" py="xl">
                              No RSVPs yet. RSVPs will appear here once people respond to
                              invitations.
                            </Text>
                          </Table.Td>
                        </Table.Tr>
                      )}
                    </Table.Tbody>
                  </Table>
                </div>
              )}

              {/* Tickets Sold Table */}
              <div data-testid="tickets-sold-section">
                <Title
                  order={2}
                  c="burgundy"
                  mb="md"
                  style={{
                    borderBottom: '2px solid var(--mantine-color-burgundy-3)',
                    paddingBottom: '8px',
                  }}
                >
                  Tickets Sold
                </Title>
                <Text size="sm" c="dimmed" mb="lg">
                  View all sold tickets for this event.
                </Text>

                <Table
                  striped
                  highlightOnHover
                  withTableBorder
                  data-testid="tickets-sold-table"
                  style={{
                    backgroundColor: 'white',
                    borderRadius: '12px',
                    overflow: 'hidden',
                    boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
                  }}
                >
                  <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
                    <Table.Tr>
                      <Table.Th
                        onClick={() => handleTicketsSort('name')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Ticket Holder
                          {renderSortIcon('name', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        onClick={() => handleTicketsSort('ticketType')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Ticket Name
                          {renderSortIcon('ticketType', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        onClick={() => handleTicketsSort('status')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Status
                          {renderSortIcon('status', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        onClick={() => handleTicketsSort('sessions')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Sessions
                          {renderSortIcon('sessions', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        onClick={() => handleTicketsSort('date')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Purchase Date
                          {renderSortIcon('date', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        onClick={() => handleTicketsSort('amount')}
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                          cursor: 'pointer',
                          userSelect: 'none',
                        }}
                      >
                        <Group gap={4}>
                          Amount Paid
                          {renderSortIcon('amount', ticketsSortColumn, ticketsSortDirection)}
                        </Group>
                      </Table.Th>
                      <Table.Th
                        style={{
                          color: 'white',
                          fontWeight: 600,
                          textTransform: 'uppercase',
                          letterSpacing: '1px',
                        }}
                      >
                        Actions
                      </Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {participationsLoading ? (
                      <Table.Tr>
                        <Table.Td colSpan={7}>
                          <Text ta="center" c="dimmed" py="xl">
                            Loading tickets...
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    ) : participationsError ? (
                      <Table.Tr>
                        <Table.Td colSpan={7}>
                          <Text ta="center" c="red" py="xl">
                            Error loading tickets: {participationsError.message}
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    ) : participationsData &&
                      (participationsData as EventParticipationDto[]).length > 0 ? (
                      // Group ticket participations by ticketId to deduplicate multi-session tickets
                      (() => {
                        const ticketParticipations = (participationsData as EventParticipationDto[])
                          .filter((p) => p.participationType === 'Ticket');

                        // Group by ticketId, combining session names
                        const groupedByTicketId = new Map<string, EventParticipationDto>();
                        ticketParticipations.forEach((p) => {
                          const key = p.ticketId ?? p.id; // Use ticketId if available, otherwise id
                          if (!key) return;

                          const existing = groupedByTicketId.get(key);
                          if (!existing) {
                            // First entry for this ticket purchase
                            groupedByTicketId.set(key, { ...p });
                          } else {
                            // Merge session names (avoid duplicates)
                            const existingSessions = existing.sessionNames?.split(', ') || [];
                            const newSessions = p.sessionNames?.split(', ') || [];
                            const allSessions = [...new Set([...existingSessions, ...newSessions])];
                            existing.sessionNames = allSessions.join(', ');
                          }
                        });

                        return Array.from(groupedByTicketId.values());
                      })()
                        .sort((a, b) => {
                          let aVal: any, bVal: any
                          switch (ticketsSortColumn) {
                            case 'name':
                              aVal = (a.userSceneName ?? '').toLowerCase()
                              bVal = (b.userSceneName ?? '').toLowerCase()
                              break
                            case 'ticketType':
                              aVal = (a.ticketTypeName ?? '').toLowerCase()
                              bVal = (b.ticketTypeName ?? '').toLowerCase()
                              break
                            case 'status':
                              aVal = a.status
                              bVal = b.status
                              break
                            case 'sessions':
                              aVal = (a.sessionNames ?? '').toLowerCase()
                              bVal = (b.sessionNames ?? '').toLowerCase()
                              break
                            case 'date':
                              aVal = new Date(a.participationDate ?? '').getTime()
                              bVal = new Date(b.participationDate ?? '').getTime()
                              break
                            case 'amount':
                              aVal = a.amountPaid ?? 0
                              bVal = b.amountPaid ?? 0
                              break
                            default:
                              aVal = a.userSceneName
                              bVal = b.userSceneName
                          }
                          if (ticketsSortDirection === 'asc') {
                            return aVal < bVal ? -1 : aVal > bVal ? 1 : 0
                          } else {
                            return aVal > bVal ? -1 : aVal < bVal ? 1 : 0
                          }
                        })
                        .flatMap((participation) => {
                          // Build array: parent ticket row + refund sub-rows
                          // Using flatMap ensures sub-rows stay attached to parent during sorting
                          const rows = [];

                          // Determine status display
                          const statusColor = participation.status === 'Active'
                            ? 'green'
                            : participation.status === 'Cancelled'
                              ? 'red'
                              : 'orange';

                          const statusLabel = participation.status === 'Active' && (participation.totalRefunded ?? 0) > 0
                            ? `Active (Partially Refunded)`
                            : participation.status;

                          // Parent ticket row
                          rows.push(
                            <Table.Tr key={participation.id}>
                              <Table.Td>
                                <Text fw={500}>{participation.userSceneName}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm">{participation.ticketTypeName ?? 'RSVP'}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Badge size="sm" color={statusColor} variant="light">
                                  {statusLabel}
                                </Badge>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm">{participation.sessionNames}</Text>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm">
                                  {new Date(participation.participationDate ?? '').toLocaleDateString()}
                                </Text>
                              </Table.Td>
                              <Table.Td>
                                <Text size="sm" fw={500}>
                                  ${(participation.amountPaid ?? 0).toFixed(2)}
                                </Text>
                              </Table.Td>
                              <Table.Td>
                                {participation.status === 'Active' && participation.ticketId && (
                                  <Text
                                    size="sm"
                                    c="red"
                                    style={{ cursor: 'pointer', textDecoration: 'underline' }}
                                    onClick={() => handleRefundTicketClick(participation)}
                                    data-testid={`refund-ticket-${participation.id}`}
                                  >
                                    Cancel/Refund
                                  </Text>
                                )}
                                {participation.status === 'Cancelled' && (
                                  <Text size="xs" c="dimmed">Cancelled</Text>
                                )}
                              </Table.Td>
                            </Table.Tr>
                          );

                          // Refund sub-rows (indented, stay attached to parent)
                          if (participation.refundHistory && participation.refundHistory.length > 0) {
                            participation.refundHistory.forEach((refund: any, idx: number) => {
                              rows.push(
                                <Table.Tr
                                  key={`${participation.id}-refund-${refund.id ?? idx}`}
                                  style={{ backgroundColor: '#FFF8F0' }}
                                >
                                  <Table.Td>
                                    <Text size="xs" c="dimmed" pl="md">
                                      ↳ Refund #{idx + 1}
                                    </Text>
                                  </Table.Td>
                                  <Table.Td />
                                  <Table.Td>
                                    <Badge
                                      size="xs"
                                      color={refund.status === 'Completed' ? 'green' : refund.status === 'Failed' ? 'red' : 'yellow'}
                                      variant="light"
                                    >
                                      {refund.status}
                                    </Badge>
                                  </Table.Td>
                                  <Table.Td colSpan={2}>
                                    <Text size="xs" c="dimmed" lineClamp={1} title={refund.reason}>
                                      {refund.reason}
                                    </Text>
                                  </Table.Td>
                                  <Table.Td>
                                    <Text size="xs" fw={500} c="red">
                                      -${refund.amount.toFixed(2)}
                                    </Text>
                                  </Table.Td>
                                  <Table.Td>
                                    <Text size="xs" c="dimmed">
                                      {new Date(refund.processedAt).toLocaleDateString()}
                                    </Text>
                                  </Table.Td>
                                </Table.Tr>
                              );
                            });
                          }

                          return rows;
                        })
                    ) : (
                      <Table.Tr>
                        <Table.Td colSpan={6}>
                          <Text ta="center" c="dimmed" py="xl">
                            No tickets sold yet. Ticket purchases will appear here once people buy
                            tickets.
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    )}
                  </Table.Tbody>
                </Table>
              </div>
            </Stack>
          </Tabs.Panel>

          {/* Attendees Tab - New tab for people who actually attended */}
          <Tabs.Panel value="attendees" pt="xl" data-testid="attendees-tab">
            <AttendeesTabPanel eventId={eventId} rightSection={attendeesRightSection} />
          </Tabs.Panel>
        </Tabs>
      </form>

      {/* Session Form Modal */}
      <SessionFormModal
        opened={sessionModalOpen}
        onClose={() => {
          setSessionModalOpen(false)
          setEditingSession(null)
        }}
        onSubmit={handleSessionSubmit}
        session={editingSession}
        existingSessions={form.values.sessions || []}
      />

      {/* Ticket Type Form Modal */}
      <TicketTypeFormModal
        opened={ticketModalOpen}
        onClose={() => {
          setTicketModalOpen(false)
          setEditingTicketType(null)
        }}
        onSubmit={handleTicketTypeSubmit}
        ticketType={editingTicketType || null}
        availableSessions={form.values.sessions || []}
      />

      {/* Modal removed - now using inline editing in VolunteerPositionsGrid */}

      {/* RSVP Removal Modal */}
      {selectedParticipant && (
        <RemoveRsvpModal
          opened={removeRsvpModalOpen}
          onClose={() => {
            setRemoveRsvpModalOpen(false)
            setSelectedParticipant(null)
          }}
          participant={{
            userId: selectedParticipant.userId ?? '',
            name: selectedParticipant.userSceneName ?? '',
            hasTicket:
              selectedParticipant.participationType === 'Ticket' ||
              (participationsData as EventParticipationDto[])?.some(
                (p) => p.userId === selectedParticipant.userId && p.participationType === 'Ticket'
              ),
            ticketAmount: selectedParticipant.amountPaid ?? 0,
            volunteerShifts: [], // TODO: Add volunteer shift data when available
          }}
          eventName={form.values.title || 'this event'}
          onConfirm={handleRemoveRsvpConfirm}
        />
      )}

      {/* Ticket Refund / Cancel Modal */}
      {selectedParticipant && (
        <RefundConfirmationModal
          opened={refundTicketModalOpen}
          onClose={() => {
            setRefundTicketModalOpen(false)
            setSelectedParticipant(null)
          }}
          payment={{
            id: selectedParticipant.ticketId ?? selectedParticipant.id ?? '',
            userName: selectedParticipant.userSceneName ?? '',
            userEmail: selectedParticipant.userEmail ?? '',
            amount: Number(selectedParticipant.amountPaid ?? 0),
            paymentMethod: selectedParticipant.paymentMethod || 'Paid Ticket',
            paymentDate: selectedParticipant.participationDate ?? '',
            description:
              selectedParticipant.sessionNames !== 'All Sessions'
                ? `Sessions: ${selectedParticipant.sessionNames}`
                : undefined,
            remainingRefundableAmount: Number(selectedParticipant.remainingRefundable ?? selectedParticipant.amountPaid ?? 0),
            refundHistory: selectedParticipant.refundHistory?.map((r: any) => ({
              id: r.id,
              amount: r.amount,
              reason: r.reason,
              status: r.status,
              processedAt: r.processedAt,
              processedByName: r.processedByName,
            })),
            totalRefunded: selectedParticipant.totalRefunded,
          }}
          allowRsvps={form.values.allowRsvps}
          hasRsvp={
            // Check if this user has an active RSVP for this event
            (participationsData as EventParticipationDto[])?.some(
              (p) =>
                p.userId === selectedParticipant.userId &&
                p.participationType === 'RSVP' &&
                p.status === 'Active'
            ) ?? false
          }
          onConfirm={handleRefundTicketConfirm}
        />
      )}

      {/* Delete Confirmation Modal (Session or Ticket Type) */}
      {deletionCheckResponse && (
        <DeleteConfirmationModal
          opened={deleteModalOpen}
          onClose={() => {
            setDeleteModalOpen(false)
            setDeletionCheckResponse(null)
          }}
          onConfirm={handleConfirmDeletion}
          itemType={deleteItemType}
          itemName={deleteItemName}
          deletionState={
            (() => {
              if (deletionCheckResponse.blockReason === 'ticketsSold') return 'ticketsSold' as DeletionState
              if (deletionCheckResponse.blockReason === 'onlySession') return 'onlySession' as DeletionState
              if (deletionCheckResponse.blockReason === 'cascadeBlocking') return 'cascadeBlocking' as DeletionState
              return 'canDelete' as DeletionState
            })()
          }
          rsvpCount={deletionCheckResponse.rsvpCount || 0}
          ticketsSoldCount={deletionCheckResponse.ticketsSoldCount || 0}
          volunteerShifts={deletionCheckResponse.volunteerShifts || []}
          affectedTicketTypes={
            deletionCheckResponse.affectedTicketTypes?.map((tt: any) => ({
              name: tt.name,
              ticketsSold: tt.ticketsSold,
            })) || []
          }
          isLoading={isDeletingItem}
        />
      )}
    </Card>
  )
}
