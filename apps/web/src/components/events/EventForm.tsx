import React, { useState, useEffect, useRef } from 'react'
import {
  Card,
  Tabs,
  TextInput,
  Group,
  Text,
  Radio,
  Select,
  Stack,
  Title,
  MultiSelect,
  Badge,
  Table,
  ActionIcon,
  Alert,
  Modal,
  Textarea,
  Button,
} from '@mantine/core'
import { useForm } from '@mantine/form'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconAlertCircle, IconChevronUp, IconChevronDown } from '@tabler/icons-react'
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor'
import type { components } from '@witchcityrope/shared-types'

import { EventSessionsGrid, EventSession } from './EventSessionsGrid'
import { EventTicketTypesGrid, EventTicketType } from './EventTicketTypesGrid'
import { SessionFormModal } from './SessionFormModal'
import { TicketTypeFormModal, EventTicketType as ModalTicketType } from './TicketTypeFormModal'
import { VolunteerPositionsGrid } from './VolunteerPositionsGrid'
import { VolunteerPosition } from './VolunteerPositionFormModal'
import { RemoveRsvpModal } from './RemoveRsvpModal'
import { RefundTicketModal } from './RefundTicketModal'
import { WCRButton } from '../ui'
import { useTeachers, formatTeachersForMultiSelect } from '../../lib/api/hooks/useTeachers'
import {
  useEventParticipations,
  type EventParticipationDto,
} from '../../lib/api/hooks/useEventParticipations'
import { useUpdateEvent } from '../../lib/api/hooks/useEvents'
import { eventKeys } from '../../lib/api/utils/cache'
import { emailTemplatesApi, type EventEmailTemplateDto, type UpdateEventTemplateRequest } from '../../services/emailTemplates.api'

// Helper function to extract purchase amount from metadata JSON
const extractAmountFromMetadata = (metadata?: string): number => {
  if (!metadata) return 0

  try {
    const parsed = JSON.parse(metadata)
    // Check for different possible field names in the metadata
    return parsed.purchaseAmount || parsed.amount || parsed.ticketAmount || 0
  } catch (error) {
    // If metadata is not JSON, it might be plain text - return 0
    return 0
  }
}

// Helper function to extract ticket name from metadata JSON
const extractTicketNameFromMetadata = (metadata?: string): string => {
  if (!metadata) return 'Standard Ticket'

  try {
    const parsed = JSON.parse(metadata)
    // Try to get ticket name from ticketTypes array
    if (parsed.ticketTypes && parsed.ticketTypes.length > 0) {
      return parsed.ticketTypes[0].name || 'Standard Ticket'
    }
    return 'Standard Ticket'
  } catch (error) {
    return 'Standard Ticket'
  }
}

// Attendees Tab Panel Component
interface AttendeesTabPanelProps {
  eventId?: string
}

const AttendeesTabPanel: React.FC<AttendeesTabPanelProps> = ({ eventId }) => {
  const [sortColumn, setSortColumn] = useState<'name' | 'paid' | 'attended'>('name')
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc')

  // Fetch participations (RSVPs and tickets)
  const { data: participations = [], isLoading } = useEventParticipations(
    eventId || '',
    !!eventId
  ) as { data: EventParticipationDto[]; isLoading: boolean }

  // Group participations by user, combining RSVP + Ticket into single row
  const groupedParticipations = React.useMemo(() => {
    const grouped = new Map<string, EventParticipationDto & { ticketAmount?: number }>()

    participations.forEach(p => {
      const existing = grouped.get(p.userId)

      if (!existing) {
        // First entry for this user
        grouped.set(p.userId, {
          ...p,
          ticketAmount: p.participationType === 'Ticket' ? (p.amountPaid ?? 0) : undefined
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
        compareValue = a.userSceneName.localeCompare(b.userSceneName)
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
      }

      return sortDirection === 'asc' ? compareValue : -compareValue
    })

    return sorted
  }, [groupedParticipations, sortColumn, sortDirection])

  const handleSort = (column: 'name' | 'paid' | 'attended') => {
    if (sortColumn === column) {
      // Toggle direction if clicking same column
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
    } else {
      // Set new column and default to ascending
      setSortColumn(column)
      setSortDirection('asc')
    }
  }

  const getSortIcon = (column: 'name' | 'paid' | 'attended') => {
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
        <Title
          order={2}
          c="burgundy"
          mb="md"
          style={{
            borderBottom: '2px solid var(--mantine-color-burgundy-3)',
            paddingBottom: '8px',
          }}
        >
          Event Attendees
        </Title>
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
                  style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px',
                  }}
                >
                  Status
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {sortedParticipations.map((participation) => {
                const paidAmount = participation.ticketAmount ?? 0
                // Use check-in status from backend (generated DTO property)
                const hasCheckedIn = participation.hasCheckedIn ?? false

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
                      {participation.status === 'Cancelled' ? (
                        <Badge color="gray" variant="filled">
                          Cancelled
                        </Badge>
                      ) : null}
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
  // Basic Info
  eventType: 'class' | 'social'
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
}) => {
  const [activeTab, setActiveTab] = useState<string>('basic-info')
  const [activeEmailTemplate, setActiveEmailTemplate] = useState<string>('confirmation')
  const queryClient = useQueryClient()

  // Fetch teachers from API
  const { data: teachersData, isLoading: teachersLoading, error: teachersError } = useTeachers()

  // Fetch active venues from API
  type VenueDto = components['schemas']['VenueDto']
  const { data: venuesData } = useQuery<VenueDto[]>({
    queryKey: ['admin', 'venues', 'active'],
    queryFn: async () => {
      const response = await api.get<{ data: VenueDto[] }>('/api/admin/venues/active')
      return response.data.data || []
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
    onError: (error) => {
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
  const [venueModalOpen, setVenueModalOpen] = useState(false)
  const [editingSession, setEditingSession] = useState<EventSession | null>(null)
  const [editingTicketType, setEditingTicketType] = useState<EventTicketType | null>(null)

  // RSVP/Ticket removal modal state
  const [removeRsvpModalOpen, setRemoveRsvpModalOpen] = useState(false)
  const [refundTicketModalOpen, setRefundTicketModalOpen] = useState(false)
  const [selectedParticipant, setSelectedParticipant] = useState<EventParticipationDto | null>(null)

  // Venue form state
  const [venueName, setVenueName] = useState('')
  const [venueDirections, setVenueDirections] = useState('')
  const [venueNotes, setVenueNotes] = useState('')

  // RSVP table sorting
  const [rsvpSortColumn, setRsvpSortColumn] = useState<'name' | 'email' | 'status' | 'date'>('name')
  const [rsvpSortDirection, setRsvpSortDirection] = useState<'asc' | 'desc'>('asc')

  // Tickets table sorting
  const [ticketsSortColumn, setTicketsSortColumn] = useState<'name' | 'ticketType' | 'status' | 'sessions' | 'date' | 'amount'>('name')
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
  const renderSortIcon = (columnName: string, currentColumn: string, currentDirection: 'asc' | 'desc') => {
    if (columnName !== currentColumn) return null
    return currentDirection === 'asc' ?
      <IconChevronUp size={14} style={{ marginLeft: 4 }} /> :
      <IconChevronDown size={14} style={{ marginLeft: 4 }} />
  }

  // Form state management
  const form = useForm<EventFormData>({
    initialValues: {
      eventType: 'class',
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
          eventType: 'class',
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

  // Fetch event email templates when Emails tab is active
  useEffect(() => {
    if (activeTab === 'emails' && eventId) {
      setIsLoadingTemplates(true)
      emailTemplatesApi.getEventTemplates(eventId)
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
  type CreateVenueRequest = components['schemas']['CreateVenueRequest']
  const createVenueMutation = useMutation({
    mutationFn: async (data: CreateVenueRequest) => {
      const response = await api.post<{ data: VenueDto }>('/api/admin/venues', data)
      return response.data.data
    },
    onSuccess: (newVenue) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'venues', 'active'] })
      notifications.show({
        title: 'Success',
        message: 'Venue created successfully',
        color: 'green',
        icon: <IconCheck />,
      })
      // Set the newly created venue as selected
      if (newVenue?.id) {
        form.setFieldValue('venueId', newVenue.id.toString())
      }
      // Reset form and close modal
      setVenueName('')
      setVenueDirections('')
      setVenueNotes('')
      setVenueModalOpen(false)
    },
    onError: (error: any) => {
      notifications.show({
        title: 'Error',
        message: error.response?.data?.message || 'Failed to create venue',
        color: 'red',
        icon: <IconAlertCircle />,
      })
    },
  })

  // Venue modal handlers
  const handleAddVenueClick = () => {
    setVenueName('')
    setVenueDirections('')
    setVenueNotes('')
    setVenueModalOpen(true)
  }

  const handleCreateVenue = () => {
    if (!venueName.trim()) {
      notifications.show({
        title: 'Validation Error',
        message: 'Venue name is required',
        color: 'red',
      })
      return
    }

    createVenueMutation.mutate({
      name: venueName.trim(),
      directions: venueDirections.trim() || null,
      notes: venueNotes.trim() || null,
    })
  }

  // Session management handlers
  const handleEditSession = (sessionId: string) => {
    const session = form.values.sessions.find((s) => s.id === sessionId)
    if (session) {
      setEditingSession(session)
      setSessionModalOpen(true)
    }
  }

  const handleDeleteSession = (sessionId: string) => {
    const updatedSessions = form.values.sessions.filter((session) => session.id !== sessionId)
    form.setFieldValue('sessions', updatedSessions)
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
        id: crypto.randomUUID(),
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
          message:
            error instanceof Error ? error.message : 'Failed to save session. Please try again.',
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

  const handleDeleteTicketType = (ticketTypeId: string) => {
    const updatedTicketTypes = form.values.ticketTypes.filter(
      (ticket) => ticket.id !== ticketTypeId
    )
    form.setFieldValue('ticketTypes', updatedTicketTypes)
  }

  const handleAddTicketType = () => {
    setEditingTicketType(null)
    setTicketModalOpen(true)
  }

  const handleTicketTypeSubmit = async (ticketTypeData: Omit<ModalTicketType, 'id'>) => {
    // Convert from modal format to grid format
    const gridFormatTicketType: Omit<EventTicketType, 'id'> = {
      name: ticketTypeData.name,
      pricingType: ticketTypeData.pricingType,
      sessionIdentifiers: ticketTypeData.sessionsIncluded,
      price: ticketTypeData.price,
      minPrice: ticketTypeData.minPrice,
      maxPrice: ticketTypeData.maxPrice,
      defaultPrice: ticketTypeData.defaultPrice,
      quantityAvailable: ticketTypeData.quantityAvailable,
      salesEndDate: ticketTypeData.saleEndDate?.toISOString(),
    }

    let updatedTicketTypes: EventTicketType[]

    if (editingTicketType) {
      // Update existing ticket type
      updatedTicketTypes = form.values.ticketTypes.map((ticketType) =>
        ticketType.id === editingTicketType.id
          ? { ...gridFormatTicketType, id: editingTicketType.id }
          : ticketType
      )
    } else {
      // Add new ticket type
      const newTicketType: EventTicketType = {
        ...gridFormatTicketType,
        id: crypto.randomUUID(),
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
            salesEndDate: ticket.salesEndDate,
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
          message:
            error instanceof Error
              ? error.message
              : 'Failed to save ticket type. Please try again.',
          color: 'red',
          icon: <IconAlertCircle size={16} />,
        })
      }
    }
  }

  // Convert grid format to modal format for editing
  const convertTicketTypeForModal = (ticketType: EventTicketType): ModalTicketType => {
    return {
      id: ticketType.id,
      name: ticketType.name,
      description: '', // Not stored in grid format currently
      pricingType: ticketType.pricingType || 'Fixed',
      price: ticketType.price,
      minPrice: ticketType.minPrice,
      maxPrice: ticketType.maxPrice,
      defaultPrice: ticketType.defaultPrice,
      sessionsIncluded: ticketType.sessionIdentifiers,
      quantityAvailable: ticketType.quantityAvailable || 100,
      quantitySold: 0, // Not tracked in current grid format
      allowMultiplePurchase: true, // Default value
      saleEndDate: ticketType.salesEndDate ? new Date(ticketType.salesEndDate) : undefined,
    }
  }

  // Volunteer position management handlers
  const handleDeleteVolunteerPosition = (positionId: string) => {
    const updatedPositions = form.values.volunteerPositions.filter(
      (position) => position.id !== positionId
    )
    form.setFieldValue('volunteerPositions', updatedPositions)
  }

  const handleVolunteerPositionSubmit = (
    positionData: Omit<VolunteerPosition, 'id' | 'slotsFilled'>,
    positionId?: string
  ) => {
    if (positionId) {
      // Update existing position
      const existingPosition = form.values.volunteerPositions.find((p) => p.id === positionId)
      if (existingPosition) {
        const updatedPositions = form.values.volunteerPositions.map((position) =>
          position.id === positionId
            ? {
                ...positionData,
                id: positionId,
                slotsFilled: existingPosition.slotsFilled,
              }
            : position
        )
        form.setFieldValue('volunteerPositions', updatedPositions)
      }
    } else {
      // Add new position
      const newPosition: VolunteerPosition = {
        ...positionData,
        id: crypto.randomUUID(),
        slotsFilled: 0, // Start with no volunteers filled
      }
      form.setFieldValue('volunteerPositions', [...form.values.volunteerPositions, newPosition])
    }
  }

  const handleSubmit = form.onSubmit((values) => {
    onSubmit(values)
  })

  // RSVP/Ticket removal handlers
  const handleRemoveRsvpClick = (participation: EventParticipationDto) => {
    // Find ticket purchase amount for this user (if any)
    const userTicket = (participationsData as EventParticipationDto[])?.find(
      p => p.userId === participation.userId && p.participationType === 'Ticket'
    )

    // Add ticketAmount to the participation object
    const participationWithTicket = {
      ...participation,
      ticketAmount: userTicket?.amountPaid ?? 0
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
      const response = await api.delete(`/api/admin/events/${eventId}/participations/${selectedParticipant.userId}`)

      if (response.status !== 200 && response.status !== 204) {
        throw new Error('Failed to remove RSVP')
      }

      notifications.show({
        message: 'RSVP removed successfully',
        color: 'green',
        autoClose: 3000
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
        autoClose: 5000
      })
    }
  }

  const handleRefundTicketConfirm = async (alsoRemoveRsvp: boolean) => {
    if (!selectedParticipant || !eventId) return

    // TODO: Replace with actual admin API endpoint when backend is ready
    // await fetch(`/api/admin/events/${eventId}/tickets/${selectedParticipant.userId}/refund`, {
    //   method: 'POST',
    //   body: JSON.stringify({ alsoRemoveRsvp })
    // })

    notifications.show({
      message: 'Ticket refunded successfully',
      color: 'green',
      autoClose: 3000
    })

    // Refetch participations to update the tables
    queryClient.invalidateQueries({ queryKey: ['events', eventId, 'participations'] })
  }

  // Email template state for editing
  const [templateSubject, setTemplateSubject] = useState<string>('')
  const [templateContent, setTemplateContent] = useState<string>('')
  const [targetSessions, setTargetSessions] = useState<string[]>(['all'])

  // Get currently selected template
  const selectedTemplate = eventTemplates.find(t => t.templateType === activeEmailTemplate)

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
    return selectedTemplate?.templateTypeName || activeEmailTemplate
  }

  const getTemplateSubject = () => {
    return templateSubject
  }

  const getTemplateContent = () => {
    return templateContent
  }

  // Save template mutation
  const saveTemplateMutation = useMutation({
    mutationFn: async ({ eventId, templateType, request }: {
      eventId: string;
      templateType: string;
      request: UpdateEventTemplateRequest;
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
          onChange={setActiveTab}
          variant="pills"
          radius="md"
          data-testid="tabs-event-management"
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
              <Tabs.Tab value="volunteers" data-testid="tab-volunteers">
                Volunteers
              </Tabs.Tab>
              <Tabs.Tab value="emails" data-testid="tab-emails">
                Emails
              </Tabs.Tab>
              <Tabs.Tab value="rsvp-tickets" data-testid="rsvp-tickets-tab">
                RSVP/Tickets
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
              {/* Event Details Section */}
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
                  Event Details
                </Title>

                {/* Event Type Toggle - First line in Event Details area */}
                <Group align="center" mb="lg">
                  <Text fw={500} size="sm">
                    Event Type
                  </Text>
                  <Radio.Group {...form.getInputProps('eventType')}>
                    <Group gap="sm">
                      <WCRButton
                        variant={form.values.eventType === 'class' ? 'primary' : 'outline'}
                        size="sm"
                        onClick={() => form.setFieldValue('eventType', 'class')}
                        style={{
                          borderColor:
                            form.values.eventType === 'class'
                              ? 'var(--mantine-color-burgundy-6)'
                              : 'var(--mantine-color-gray-4)',
                        }}
                      >
                        Class (Ticket Only)
                      </WCRButton>
                      <WCRButton
                        variant={form.values.eventType === 'social' ? 'primary' : 'outline'}
                        size="sm"
                        onClick={() => form.setFieldValue('eventType', 'social')}
                        style={{
                          borderColor:
                            form.values.eventType === 'social'
                              ? 'var(--mantine-color-burgundy-6)'
                              : 'var(--mantine-color-gray-4)',
                        }}
                      >
                        Social Event (RSVP & Ticket)
                      </WCRButton>
                      <Radio value="class" label="" style={{ display: 'none' }} />
                      <Radio value="social" label="" style={{ display: 'none' }} />
                    </Group>
                  </Radio.Group>
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
                  <Stack gap="md">
                    <Select
                      label="Venue"
                      placeholder="Select venue..."
                      data={venues}
                      required
                      {...form.getInputProps('venueId')}
                    />
                    <WCRButton variant="outline" size="md" onClick={handleAddVenueClick}>
                      Create New Venue
                    </WCRButton>
                  </Stack>
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
                        {template.templateTypeName}
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

              {/* Add Template Controls */}
              <Group align="end" gap="sm">
                <Select
                  label=""
                  placeholder="Select template to add..."
                  data={[
                    { value: 'reminder-1week', label: 'Reminder - 1 Week Before' },
                    { value: 'waitlist', label: 'Waitlist Notification' },
                    { value: 'survey', label: 'Post-Event Survey' },
                    { value: 'schedule-change', label: 'Schedule Change Alert' },
                  ]}
                  style={{ flex: 1 }}
                />
                <WCRButton variant="primary" size="lg" disabled>
                  Add Email Template
                </WCRButton>
              </Group>

              {/* Unified Editor Section */}
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
              Are you sure you want to reset <strong>{templateToReset?.templateTypeName}</strong> to
              the global default template? This will delete your customizations and cannot be undone.
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
                    sessionIdentifier: s.sessionIdentifier,
                    name: s.name,
                  }))}
                />
              </div>
            </Stack>
          </Tabs.Panel>

          {/* RSVP/Tickets Tab - Updated per requirements */}
          <Tabs.Panel value="rsvp-tickets" pt="xl" data-testid="rsvp-tickets-tab">
            <Stack gap="xl">
              {/* RSVPs Table - Hidden for CLASS events */}
              {form.values.eventType === 'social' && (
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
                                aVal = a.userSceneName.toLowerCase()
                                bVal = b.userSceneName.toLowerCase()
                                break
                              case 'email':
                                aVal = a.userEmail.toLowerCase()
                                bVal = b.userEmail.toLowerCase()
                                break
                              case 'status':
                                aVal = a.status
                                bVal = b.status
                                break
                              case 'date':
                                aVal = new Date(a.participationDate).getTime()
                                bVal = new Date(b.participationDate).getTime()
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
                                  {new Date(participation.participationDate).toLocaleDateString()}
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
                      (participationsData as EventParticipationDto[])
                        .filter((p) => p.participationType === 'Ticket')
                        .sort((a, b) => {
                          let aVal: any, bVal: any
                          switch (ticketsSortColumn) {
                            case 'name':
                              aVal = a.userSceneName.toLowerCase()
                              bVal = b.userSceneName.toLowerCase()
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
                              aVal = a.sessionNames.toLowerCase()
                              bVal = b.sessionNames.toLowerCase()
                              break
                            case 'date':
                              aVal = new Date(a.participationDate).getTime()
                              bVal = new Date(b.participationDate).getTime()
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
                        .map((participation) => (
                          <Table.Tr key={participation.id}>
                            <Table.Td>
                              <Text fw={500}>{participation.userSceneName}</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">{participation.ticketTypeName ?? 'RSVP'}</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm" c={participation.status === 'Active' ? 'green' : 'red'}>
                                {participation.status}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">{participation.sessionNames}</Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm">
                                {new Date(participation.participationDate).toLocaleDateString()}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm" fw={500}>
                                ${(participation.amountPaid ?? 0).toFixed(2)}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              {participation.status === 'Active' && (
                                <Text
                                  size="sm"
                                  c="red"
                                  style={{ cursor: 'pointer', textDecoration: 'underline' }}
                                  onClick={() => handleRefundTicketClick(participation)}
                                  data-testid={`refund-ticket-${participation.id}`}
                                >
                                  Refund
                                </Text>
                              )}
                            </Table.Td>
                          </Table.Tr>
                        ))
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
            <AttendeesTabPanel eventId={eventId} />
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
        ticketType={editingTicketType ? convertTicketTypeForModal(editingTicketType) : null}
        availableSessions={form.values.sessions || []}
      />

      {/* Add Venue Modal */}
      <Modal
        opened={venueModalOpen}
        onClose={() => setVenueModalOpen(false)}
        title="Add New Venue"
        centered
        size="md"
      >
        <Stack gap="md">
          <TextInput
            label="Venue Name"
            placeholder="Enter venue name"
            value={venueName}
            onChange={(e) => setVenueName(e.currentTarget.value)}
            required
            withAsterisk
          />

          <Textarea
            label="Directions"
            placeholder="Enter directions to the venue (optional)"
            value={venueDirections}
            onChange={(e) => setVenueDirections(e.currentTarget.value)}
            minRows={3}
            maxRows={6}
            maxLength={500}
          />

          <Textarea
            label="Notes"
            placeholder="Enter any additional notes about the venue (optional)"
            value={venueNotes}
            onChange={(e) => setVenueNotes(e.currentTarget.value)}
            minRows={3}
            maxRows={6}
            maxLength={1000}
          />

          <Group justify="flex-end" mt="md">
            <WCRButton
              variant="outline"
              onClick={() => setVenueModalOpen(false)}
              disabled={createVenueMutation.isPending}
            >
              Cancel
            </WCRButton>
            <WCRButton
              variant="primary"
              onClick={handleCreateVenue}
              loading={createVenueMutation.isPending}
            >
              Create Venue
            </WCRButton>
          </Group>
        </Stack>
      </Modal>

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
            userId: selectedParticipant.userId,
            name: selectedParticipant.userSceneName,
            hasTicket: selectedParticipant.participationType === 'Ticket' ||
                       (participationsData as EventParticipationDto[])?.some(
                         p => p.userId === selectedParticipant.userId && p.participationType === 'Ticket'
                       ),
            ticketAmount: selectedParticipant.amountPaid ?? 0,
            volunteerShifts: [] // TODO: Add volunteer shift data when available
          }}
          eventName={form.values.title || 'this event'}
          onConfirm={handleRemoveRsvpConfirm}
        />
      )}

      {/* Ticket Refund Modal */}
      {selectedParticipant && (
        <RefundTicketModal
          opened={refundTicketModalOpen}
          onClose={() => {
            setRefundTicketModalOpen(false)
            setSelectedParticipant(null)
          }}
          participant={{
            userId: selectedParticipant.userId,
            name: selectedParticipant.userSceneName,
            ticketAmount: selectedParticipant.amountPaid ?? 0,
            hasRsvp: (participationsData as EventParticipationDto[])?.some(
              p => p.userId === selectedParticipant.userId && p.participationType === 'RSVP'
            ) || false,
            volunteerShifts: [] // TODO: Add volunteer shift data when available
          }}
          eventName={form.values.title || 'this event'}
          onConfirm={handleRefundTicketConfirm}
        />
      )}
    </Card>
  )
}
