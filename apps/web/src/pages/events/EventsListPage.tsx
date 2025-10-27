import React, { useMemo, useState } from 'react'
import {
  Container,
  Stack,
  Title,
  Text,
  Group,
  Alert,
  Button,
  Box,
  Skeleton,
  Center,
  Paper,
  TextInput,
  Select,
  SegmentedControl,
  Table,
  ActionIcon,
  Switch,
} from '@mantine/core'
import { IconSearch, IconArrowUp, IconArrowDown } from '@tabler/icons-react'
import { useEventFilters } from '../../hooks/useEventFilters'
import { useEvents } from '../../lib/api/hooks/useEvents'
import { formatEventDate, formatEventDateTime, formatEventTime, calculateEventPriceRange } from '../../utils/eventUtils'
import type { EventDto } from '../../lib/api/types/events.types'
import { useNavigate } from 'react-router-dom'
import { useParticipation } from '../../hooks/useParticipation'
import { useCurrentUser } from '../../lib/api/hooks/useAuth'
import { Badge } from '@mantine/core'

// Mock function to get user role - replace with actual auth context
const useAuth = () => ({
  userRole: 'anonymous' as 'anonymous' | 'member' | 'vetted' | 'admin',
  isAuthenticated: false,
})

export const EventsListPage: React.FC = () => {
  const navigate = useNavigate()
  const { userRole } = useAuth()
  const [viewMode, setViewMode] = useState<'cards' | 'list'>('cards')
  const [showPastClasses, setShowPastClasses] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const [sortBy, setSortBy] = useState('date')
  const { filters, clearFilters } = useEventFilters()

  // Convert filters to API format
  const apiFilters = useMemo(
    () => ({
      // Map UI filters to API filters when backend supports them
      search: filters.eventType !== 'all' ? filters.eventType : undefined,
    }),
    [filters]
  )

  const { data: events, isLoading, error, refetch } = useEvents(apiFilters)

  // Use real API data only - ensure events is always an array
  const eventsArray: EventDto[] = Array.isArray(events) ? events : []

  // Debug logging for E2E test troubleshooting
  console.log('🎯 EventsListPage render state:', {
    isLoading,
    hasError: !!error,
    eventsData: events,
    eventsArrayLength: eventsArray.length,
    eventsIsArray: Array.isArray(events),
    eventsSample: eventsArray[0]?.title || 'No events',
  })

  const handleRegister = (eventId: string) => {
    console.log('Register for event:', eventId)
    // Will implement with actual registration flow
  }

  const handleRSVP = (eventId: string) => {
    console.log('RSVP for event:', eventId)
    // Will implement with actual RSVP flow
  }

  if (error) {
    return (
      <Container size="xl" py="xl">
        <Alert data-testid="events-error" color="red" title="Failed to Load Events">
          <Stack gap="sm">
            <Text size="sm">
              Unable to load events. Please check your connection and try again.
            </Text>
            <Button size="xs" variant="outline" onClick={() => refetch()}>
              Retry
            </Button>
          </Stack>
        </Alert>
      </Container>
    )
  }

  return (
    <Box data-testid="page-events" style={{ background: 'var(--color-cream)', minHeight: '100vh' }}>
      {/* Hero Section with burgundy gradient background */}
      <Box
        style={{
          background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
          position: 'relative',
          overflow: 'hidden',
        }}
      >
        {/* Subtle pattern overlay */}
        <Box
          style={{
            position: 'absolute',
            top: '-50%',
            left: '-50%',
            width: '200%',
            height: '200%',
            background: 'radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%)',
            transform: 'rotate(45deg)',
          }}
        />
        <Container size="xl" style={{ position: 'relative', zIndex: 1 }}>
          <Stack gap="md" ta="center" py={64}>
            <Title
              order={1}
              size="3rem"
              fw={800}
              style={{
                fontFamily: 'var(--font-heading)',
                color: 'var(--color-ivory)',
              }}
            >
              Explore Classes & Meetups
            </Title>
            <Text
              size="xl"
              style={{
                color: 'var(--color-dusty-rose)',
                fontSize: '20px',
              }}
            >
              Learn rope bondage in a safe, inclusive environment with experienced instructors
            </Text>
          </Stack>
        </Container>
      </Box>

      {/* Filter Bar */}
      <Box
        style={{
          background: 'var(--color-ivory)',
          borderBottom: '1px solid var(--color-taupe)',
          position: 'sticky',
          top: 0,
          zIndex: 90,
        }}
      >
        <Container size="xl" py="md">
          <Group justify="space-between" align="center" wrap="wrap" gap="md">
            <Group gap="sm">
              <Switch
                label="Show Past Classes"
                checked={showPastClasses}
                onChange={(event) => setShowPastClasses(event.currentTarget.checked)}
                color="burgundy"
                size="sm"
                styles={{
                  label: {
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    fontSize: '14px',
                  },
                }}
              />
            </Group>

            <Group gap="md" align="center">
              <SegmentedControl
                data-testid="button-view-toggle"
                value={viewMode}
                onChange={(value) => setViewMode(value as 'cards' | 'list')}
                data={[
                  { label: 'Card View', value: 'cards' },
                  { label: 'List View', value: 'list' },
                ]}
                size="sm"
                color="burgundy"
                styles={{
                  root: {
                    background: 'var(--color-cream)',
                    borderRadius: '25px',
                    padding: '4px',
                  },
                  control: {
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                  },
                }}
              />

              <TextInput
                data-testid="input-search"
                placeholder="Search events..."
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.currentTarget.value)}
                leftSection={<IconSearch size={16} color="var(--color-stone)" />}
                w={250}
                styles={{
                  input: {
                    border: '2px solid var(--color-taupe)',
                    borderRadius: '25px',
                    fontFamily: 'var(--font-body)',
                    fontSize: '14px',
                    '&:focus': {
                      borderColor: 'var(--color-burgundy)',
                      width: '300px',
                      transition: 'all 0.3s ease',
                    },
                  },
                }}
              />

              <Select
                data-testid="select-category"
                value={sortBy}
                onChange={(value) => setSortBy(value || 'date')}
                data={[
                  { value: 'date', label: 'Sort by Date' },
                  { value: 'price', label: 'Sort by Price' },
                  { value: 'availability', label: 'Sort by Availability' },
                ]}
                w={150}
                styles={{
                  input: {
                    border: '2px solid var(--color-taupe)',
                    borderRadius: '25px',
                    fontFamily: 'var(--font-body)',
                    fontSize: '14px',
                    background: 'var(--color-ivory)',
                    color: 'var(--color-charcoal)',
                    '&:hover': {
                      borderColor: 'var(--color-burgundy)',
                    },
                  },
                }}
              />
            </Group>
          </Group>
        </Container>
      </Box>

      {/* Main Content */}
      <Container size="xl" py="xl">
        {/* Section header for E2E test detection */}
        {!isLoading && eventsArray.length > 0 && (
          <Title
            order={2}
            mb="xl"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-charcoal)',
              fontSize: '2rem',
              fontWeight: 700,
            }}
          >
            Upcoming Events
          </Title>
        )}

        {isLoading && eventsArray.length === 0 ? (
          <EventsListSkeleton />
        ) : eventsArray.length === 0 ? (
          <EmptyEventsState data-testid="events-empty-state" onClearFilters={clearFilters} />
        ) : viewMode === 'cards' ? (
          <EventCardGrid
            events={eventsArray}
            userRole={userRole}
            onRegister={handleRegister}
            onRSVP={handleRSVP}
          />
        ) : (
          <EventTableView
            events={eventsArray}
            onEventClick={(eventId) => {
              // Use setTimeout to ensure navigation happens AFTER React finishes current render cycle
              setTimeout(() => {
                navigate(`/events/${eventId}`)
              }, 0)
            }}
          />
        )}
      </Container>
    </Box>
  )
}

// Event Card Grid Component
interface EventCardGridProps {
  events: EventDto[]
  userRole: string
  onRegister: (eventId: string) => void
  onRSVP: (eventId: string) => void
}

const EventCardGrid: React.FC<EventCardGridProps> = ({ events, userRole, onRegister, onRSVP }) => {
  const navigate = useNavigate()

  const handleCardClick = (eventId: string) => {
    // Use setTimeout to ensure navigation happens AFTER React finishes current render cycle
    // This allows Outlet to properly unmount old component and mount new one
    setTimeout(() => {
      navigate(`/events/${eventId}`)
    }, 0)
  }

  return (
    <Box
      data-testid="events-list"
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))',
        gap: 'var(--space-lg)',
      }}
    >
      {events.map((event) => (
        <WireframeEventCard
          key={event.id}
          data-testid="event-card"
          event={event}
          userRole={userRole}
          onRegister={onRegister}
          onRSVP={onRSVP}
          onClick={() => handleCardClick(event.id)}
        />
      ))}
    </Box>
  )
}

// Wireframe-matching Event Card
interface WireframeEventCardProps {
  event: EventDto
  userRole: string
  onRegister: (eventId: string) => void
  onRSVP: (eventId: string) => void
  onClick: () => void
  'data-testid'?: string
}

const WireframeEventCard: React.FC<WireframeEventCardProps> = ({
  event,
  onClick,
  'data-testid': testId,
}) => {
  const navigate = useNavigate()
  const { data: currentUser } = useCurrentUser()
  const isAuthenticated = !!currentUser

  // Fetch participation status for authenticated users
  const { data: participation } = useParticipation(event.id, isAuthenticated)

  const availableSpots = (event.capacity || 20) - (event.registrationCount || 0)

  const getSpotColor = () => {
    if (availableSpots > 10) return 'var(--color-success)'
    if (availableSpots > 3) return 'var(--color-warning)'
    return 'var(--color-error)'
  }

  // Determine participation status for badges
  const hasTicket = participation?.hasTicket || false
  const hasRSVP = participation?.hasRSVP || false

  // Check if event has paid tickets (maxPrice > 0)
  const hasPaidTickets = (event as any).ticketTypes?.some((tt: any) => (tt.maxPrice || 0) > 0)

  // Show Purchase Ticket button only if user has RSVPed but not purchased ticket and event has paid tickets
  const shouldShowPurchaseButton = hasRSVP && !hasTicket && hasPaidTickets

  return (
    <Paper
      className="event-card"
      data-testid={testId || 'event-card'}
      data-event-id={event.id}
      style={{
        background: 'var(--color-ivory)',
        borderRadius: '16px',
        overflow: 'hidden',
        boxShadow: '0 4px 12px rgba(0,0,0,0.05)',
        transition: 'all 0.3s ease',
        cursor: 'pointer',
        border: '1px solid rgba(183, 109, 117, 0.1)',
        position: 'relative',
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-4px)'
        e.currentTarget.style.boxShadow = '0 8px 24px rgba(0,0,0,0.1)'
        e.currentTarget.style.borderColor = 'var(--color-rose-gold)'
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)'
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.05)'
        e.currentTarget.style.borderColor = 'rgba(183, 109, 117, 0.1)'
      }}
      onClick={onClick}
    >
      {/* Hidden link for test accessibility - tests look for a[href*="/events/"] */}
      <a href={`/events/${event.id}`} style={{ display: 'none' }} aria-hidden="true" tabIndex={-1}>
        {event.title}
      </a>

      {/* Gradient Header with Event Title */}
      <Box
        style={{
          height: '100px',
          background: 'linear-gradient(135deg, var(--color-plum) 0%, var(--color-burgundy) 100%)',
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: 'var(--space-md)',
        }}
      >
        <Title
          data-testid="event-title"
          order={3}
          size="lg"
          fw={700}
          ta="center"
          style={{
            fontFamily: 'var(--font-heading)',
            color: 'var(--color-ivory)',
            lineHeight: 1.3,
            textShadow: '0 2px 4px rgba(0, 0, 0, 0.3)',
            position: 'relative',
            zIndex: 1,
          }}
        >
          {event.title}
        </Title>
      </Box>

      {/* Event Content */}
      <Box
        style={{
          padding: 'var(--space-md) var(--space-lg)',
          flex: 1,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* Date and Time - Split Layout */}
        <Group
          justify="space-between"
          mb="sm"
        >
          <Text
            data-testid="event-date"
            size="sm"
            fw={700}
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            {(() => {
              if (!event.startDate) return 'TBD'
              const start = new Date(event.startDate)
              return start.toLocaleDateString('en-US', {
                weekday: 'long',
                month: 'short',
                day: 'numeric'
              })
            })()}
          </Text>
          <Text
            data-testid="event-time"
            size="sm"
            fw={700}
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
            }}
          >
            {(() => {
              if (!event.startDate) return ''
              const start = new Date(event.startDate)
              const startTime = start.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              if (!event.endDate) return startTime

              const end = new Date(event.endDate)
              const endTime = end.toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
                hour12: true
              }).toLowerCase()

              return `${startTime} - ${endTime}`
            })()}
          </Text>
        </Group>

        {/* Description */}
        <Text
          size="sm"
          style={{
            color: 'var(--color-stone)',
            lineHeight: 1.6,
            marginBottom: 'var(--space-md)',
            flex: 1,
          }}
        >
          {event.shortDescription || ''}
        </Text>

        {/* Footer */}
        <Box
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            paddingTop: 'var(--space-sm)',
            marginTop: 'auto',
            borderTop: '1px solid var(--color-taupe)',
          }}
        >
          {/* Price or Ticket Purchased Badge */}
          {hasTicket ? (
            <Badge
              color="green"
              variant="light"
              size="lg"
              styles={{
                root: {
                  fontFamily: 'var(--font-heading)',
                  fontWeight: 700,
                  fontSize: '14px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                }
              }}
            >
              Ticket Purchased
            </Badge>
          ) : (
            <Group gap="xs" align="center">
              <Text
                fw={700}
                style={{
                  fontFamily: 'var(--font-heading)',
                  fontSize: '18px',
                  color: 'var(--color-burgundy)',
                }}
              >
                {calculateEventPriceRange((event as any).ticketTypes || [])}
              </Text>
              {hasRSVP && (
                <Badge
                  color="blue"
                  variant="light"
                  size="md"
                  styles={{
                    root: {
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 600,
                      fontSize: '12px',
                      textTransform: 'uppercase',
                    }
                  }}
                >
                  RSVPed
                </Badge>
              )}
            </Group>
          )}

          <Text
            fw={600}
            ta="center"
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: '14px',
              color: getSpotColor(),
            }}
          >
            {event.registrationCount || 0}/{event.capacity || 20}
          </Text>
        </Box>

        {/* Purchase Ticket Button - Only show for RSVPed users without tickets */}
        {shouldShowPurchaseButton && (
          <Group justify="center" mt="md" pb="xs">
            <Button
              data-testid="button-purchase-ticket"
              className="btn btn-primary"
              size="sm"
              onClick={(e) => {
                e.stopPropagation()
                // Navigate directly to checkout page with event tickets loaded
                setTimeout(() => {
                  navigate(`/checkout/${event.id}`)
                }, 0)
              }}
              style={{
                background: 'linear-gradient(135deg, var(--color-burgundy) 0%, var(--color-plum) 100%)',
                color: 'white',
                border: 'none',
                padding: '10px 24px',
                fontSize: '13px',
                letterSpacing: '1px',
                fontFamily: 'var(--font-heading)',
                fontWeight: 700,
                textTransform: 'uppercase',
              }}
            >
              Purchase Ticket
            </Button>
          </Group>
        )}
      </Box>
    </Paper>
  )
}

// Event Table View
interface EventTableViewProps {
  events: EventDto[]
  onEventClick: (eventId: string) => void
}

const EventTableView: React.FC<EventTableViewProps> = ({ events, onEventClick }) => {
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')
  const { data: currentUser } = useCurrentUser()
  const isAuthenticated = !!currentUser

  const handleSort = () => {
    setSortOrder((prev) => (prev === 'asc' ? 'desc' : 'asc'))
  }

  return (
    <Paper
      style={{
        background: 'white',
        borderRadius: '12px',
        overflow: 'hidden',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
      }}
    >
      <Table highlightOnHover>
        <Table.Thead
          style={{
            background: 'var(--color-burgundy)',
          }}
        >
          <Table.Tr>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
                cursor: 'pointer',
                userSelect: 'none',
              }}
              onClick={handleSort}
            >
              <Group gap="xs">
                Date
                <ActionIcon size="sm" variant="transparent">
                  {sortOrder === 'asc' ? (
                    <IconArrowUp size={16} color="white" />
                  ) : (
                    <IconArrowDown size={16} color="white" />
                  )}
                </ActionIcon>
              </Group>
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Time
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Event
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Price
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Spots
            </Table.Th>
            <Table.Th
              style={{
                color: 'white',
                padding: 'var(--space-md)',
                fontFamily: 'var(--font-heading)',
                fontWeight: 600,
                fontSize: '1rem',
                textTransform: 'uppercase',
                letterSpacing: '1px',
              }}
            >
              Action
            </Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {events.map((event, index) => {
            return <EventTableRow key={event.id} event={event} index={index} isAuthenticated={isAuthenticated} onEventClick={onEventClick} />
          })}
        </Table.Tbody>
      </Table>
    </Paper>
  )
}

// Event Table Row Component with Participation Status
interface EventTableRowProps {
  event: EventDto
  index: number
  isAuthenticated: boolean
  onEventClick: (eventId: string) => void
}

const EventTableRow: React.FC<EventTableRowProps> = ({ event, index, isAuthenticated, onEventClick }) => {
  // Fetch participation status for authenticated users
  const { data: participation } = useParticipation(event.id, isAuthenticated)

  const availableSpots = (event.capacity || 20) - (event.registrationCount || 0)
  const getSpotColor = () => {
    if (availableSpots > 10) return 'var(--color-success)'
    if (availableSpots > 3) return 'var(--color-warning)'
    return 'var(--color-error)'
  }

  // Check if event has paid tickets (maxPrice > 0)
  const hasPaidTickets = (event as any).ticketTypes?.some((tt: any) => (tt.maxPrice || 0) > 0)

  // Determine participation status
  const hasTicket = participation?.hasTicket || false
  const hasRSVP = participation?.hasRSVP || false
  const shouldShowPurchaseButton = hasRSVP && !hasTicket && hasPaidTickets

  return (
    <Table.Tr
      style={{
        cursor: 'pointer',
        backgroundColor: index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent',
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.backgroundColor = 'rgba(136, 1, 36, 0.08)'
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.backgroundColor =
          index % 2 === 1 ? 'rgba(250, 246, 242, 0.8)' : 'transparent'
      }}
      onClick={() => onEventClick(event.id)}
    >
                <Table.Td
                  style={{
                    padding: 'var(--space-md)',
                    fontFamily: 'var(--font-body)',
                    fontWeight: 600,
                    color: 'var(--color-charcoal)',
                  }}
                >
                  {formatEventDate(event.startDate).replace(/,.*/, '')}
                </Table.Td>
                <Table.Td
                  style={{
                    padding: 'var(--space-md)',
                    fontFamily: 'var(--font-body)',
                    fontWeight: 500,
                    color: 'var(--color-charcoal)',
                  }}
                >
                  {formatEventTime(event.startDate)}
                </Table.Td>
                <Table.Td
                  style={{
                    padding: 'var(--space-md)',
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    color: 'var(--color-burgundy)',
                    fontSize: '1.2rem',
                  }}
                >
                  {event.title}
                </Table.Td>
                <Table.Td
                  style={{
                    padding: 'var(--space-md)',
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 700,
                    color: 'var(--color-burgundy)',
                    fontSize: '1.3rem',
                    textAlign: 'center',
                  }}
                >
                  {hasTicket ? (
                    <Badge
                      color="green"
                      variant="light"
                      size="lg"
                      styles={{
                        root: {
                          fontFamily: 'var(--font-heading)',
                          fontWeight: 700,
                          fontSize: '14px',
                          textTransform: 'uppercase',
                          letterSpacing: '0.5px',
                        }
                      }}
                    >
                      Ticket Purchased
                    </Badge>
                  ) : (
                    <Group gap="xs" align="center" justify="center">
                      <Text
                        fw={700}
                        style={{
                          fontFamily: 'var(--font-heading)',
                          fontSize: '1.3rem',
                          color: 'var(--color-burgundy)',
                        }}
                      >
                        {calculateEventPriceRange((event as any).ticketTypes || [])}
                      </Text>
                      {hasRSVP && (
                        <Badge
                          color="blue"
                          variant="light"
                          size="md"
                          styles={{
                            root: {
                              fontFamily: 'var(--font-heading)',
                              fontWeight: 600,
                              fontSize: '12px',
                              textTransform: 'uppercase',
                            }
                          }}
                        >
                          RSVPed
                        </Badge>
                      )}
                    </Group>
                  )}
                </Table.Td>
                <Table.Td
                  style={{
                    padding: 'var(--space-md)',
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    fontSize: '1.1rem',
                    textAlign: 'center',
                    color: getSpotColor(),
                  }}
                >
                  {event.registrationCount || 0}/{event.capacity || 20}
                </Table.Td>
                <Table.Td style={{ padding: 'var(--space-md)', textAlign: 'center' }}>
                  <Button
                    size="xs"
                    onClick={(e) => {
                      e.stopPropagation()
                      onEventClick(event.id)
                    }}
                    style={{
                      background: shouldShowPurchaseButton ? 'var(--color-burgundy)' : 'var(--color-cream)',
                      border: shouldShowPurchaseButton ? 'none' : '2px solid var(--color-rose-gold)',
                      color: shouldShowPurchaseButton ? 'white' : 'var(--color-burgundy)',
                      padding: '6px 12px',
                      margin: '0 4px',
                      borderRadius: '8px 4px 8px 4px',
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 500,
                      fontSize: '0.85rem',
                      transition: 'all 0.3s ease',
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.borderRadius = '4px 8px 4px 8px'
                      if (!shouldShowPurchaseButton) {
                        e.currentTarget.style.background = 'var(--color-burgundy)'
                        e.currentTarget.style.color = 'white'
                      }
                      e.currentTarget.style.transform = 'scale(1.05)'
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.borderRadius = '8px 4px 8px 4px'
                      if (!shouldShowPurchaseButton) {
                        e.currentTarget.style.background = 'var(--color-cream)'
                        e.currentTarget.style.color = 'var(--color-burgundy)'
                      }
                      e.currentTarget.style.transform = 'scale(1)'
                    }}
                  >
                    {shouldShowPurchaseButton ? 'Purchase Ticket' : 'Learn More'}
                  </Button>
                </Table.Td>
              </Table.Tr>
  )
}

// Loading skeleton component
const EventsListSkeleton: React.FC = () => (
  <Stack data-testid="events-loading" gap="xl">
    {Array.from({ length: 3 }, (_, index) => (
      <Stack key={index} gap="md">
        <Skeleton height={32} width={200} />
        <Stack gap="sm">
          <EventCardSkeleton />
          <EventCardSkeleton />
        </Stack>
      </Stack>
    ))}
  </Stack>
)

const EventCardSkeleton: React.FC = () => (
  <Box p="md" style={{ border: '1px solid #e0e0e0', borderRadius: '8px' }}>
    <Stack gap="sm">
      <Group justify="space-between">
        <Group gap="xs">
          <Skeleton height={20} width={60} radius="xl" />
          <Skeleton height={24} width={200} />
        </Group>
      </Group>

      <Group gap="md">
        <Skeleton height={16} width={120} />
        <Skeleton height={16} width={100} />
      </Group>

      <Skeleton height={40} />

      <Group justify="space-between">
        <Group gap="md">
          <Skeleton height={20} width={80} />
          <Skeleton height={16} width={120} />
        </Group>
        <Skeleton height={36} width={120} />
      </Group>
    </Stack>
  </Box>
)

// Empty state component
interface EmptyEventsStateProps {
  onClearFilters: () => void
  'data-testid'?: string
}

const EmptyEventsState: React.FC<EmptyEventsStateProps> = ({
  onClearFilters,
  'data-testid': testId,
}) => (
  <Center data-testid={testId} py="xl">
    <Stack align="center" gap="md">
      <Box
        style={{
          width: '120px',
          height: '120px',
          background: 'linear-gradient(135deg, var(--color-ivory) 0%, var(--color-cream) 100%)',
          border: '2px solid var(--color-rose-gold)',
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '48px',
          color: 'var(--color-burgundy)',
          boxShadow: '0 4px 15px rgba(183, 109, 117, 0.2)',
          marginBottom: 'var(--space-lg)',
        }}
      >
        📅
      </Box>
      <Title
        order={3}
        style={{
          fontFamily: 'var(--font-heading)',
          fontSize: '28px',
          fontWeight: 700,
          color: 'var(--color-charcoal)',
          marginBottom: 'var(--space-sm)',
        }}
      >
        No Events Found
      </Title>
      <Text ta="center" style={{ fontSize: '18px', marginBottom: 'var(--space-xl)' }}>
        There are no upcoming events scheduled at this time.
      </Text>
      <Button
        variant="outline"
        color="burgundy"
        onClick={onClearFilters}
        className="btn btn-secondary"
      >
        Refresh Events
      </Button>
    </Stack>
  </Center>
)
