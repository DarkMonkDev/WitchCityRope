import React from 'react';
import {
  Box,
  Title,
  Text,
  Paper,
  Group,
  Badge,
  Button,
  Stack,
  Tabs,
  Table,
  Select,
  TextInput,
  ActionIcon,
  Pagination,
  Alert,
  Loader,
  Modal,
  SimpleGrid,
  Card,
  Divider,
} from '@mantine/core';
import { DashboardLayout } from '../../components/dashboard/DashboardLayout';
import { useDisclosure, useMediaQuery } from '@mantine/hooks';

// Mock registration data - replace with real API integration later
interface EventRegistration {
  id: string;
  eventTitle: string;
  eventDate: string;
  eventTime: string;
  registrationDate: string;
  status: 'upcoming' | 'completed' | 'cancelled';
  paymentStatus: 'paid' | 'pending' | 'refunded' | 'free';
  attendanceStatus?: 'attended' | 'no-show' | 'pending';
  eventType: 'workshop' | 'social' | 'performance' | 'class';
  price: number;
  canCancel: boolean;
  refundAmount?: number;
}

// Mock data
const mockRegistrations: EventRegistration[] = [
  {
    id: '1',
    eventTitle: 'Beginner Rope Fundamentals',
    eventDate: '2025-09-15',
    eventTime: '7:00 PM - 9:00 PM',
    registrationDate: '2025-08-20',
    status: 'upcoming',
    paymentStatus: 'paid',
    attendanceStatus: 'pending',
    eventType: 'workshop',
    price: 35.00,
    canCancel: true,
  },
  {
    id: '2',
    eventTitle: 'Advanced Suspension Workshop',
    eventDate: '2025-08-25',
    eventTime: '2:00 PM - 5:00 PM',
    registrationDate: '2025-08-10',
    status: 'completed',
    paymentStatus: 'paid',
    attendanceStatus: 'attended',
    eventType: 'workshop',
    price: 65.00,
    canCancel: false,
  },
  {
    id: '3',
    eventTitle: 'Community Social Hour',
    eventDate: '2025-08-18',
    eventTime: '6:00 PM - 8:00 PM',
    registrationDate: '2025-08-15',
    status: 'completed',
    paymentStatus: 'free',
    attendanceStatus: 'attended',
    eventType: 'social',
    price: 0,
    canCancel: false,
  },
  {
    id: '4',
    eventTitle: 'Rope Art Performance Evening',
    eventDate: '2025-07-20',
    eventTime: '7:30 PM - 9:30 PM',
    registrationDate: '2025-07-15',
    status: 'completed',
    paymentStatus: 'paid',
    attendanceStatus: 'no-show',
    eventType: 'performance',
    price: 25.00,
    canCancel: false,
  },
  {
    id: '5',
    eventTitle: 'Safety and Consent Workshop',
    eventDate: '2025-09-20',
    eventTime: '1:00 PM - 3:00 PM',
    registrationDate: '2025-08-25',
    status: 'upcoming',
    paymentStatus: 'pending',
    attendanceStatus: 'pending',
    eventType: 'class',
    price: 40.00,
    canCancel: true,
  },
];

/**
 * Registration History and Management Page
 * Comprehensive interface for viewing and managing event registrations
 */
export const RegistrationsPage: React.FC = () => {
  const [activeTab, setActiveTab] = React.useState<string>('all');
  const [searchQuery, setSearchQuery] = React.useState('');
  const [statusFilter, setStatusFilter] = React.useState<string>('all');
  const [sortBy, setSortBy] = React.useState<string>('eventDate');
  const [currentPage, setCurrentPage] = React.useState(1);
  const [selectedRegistration, setSelectedRegistration] = React.useState<EventRegistration | null>(null);
  const [cancelModalOpened, { open: openCancelModal, close: closeCancelModal }] = useDisclosure();
  
  const isMobile = useMediaQuery('(max-width: 768px)');
  const registrationsPerPage = 10;

  // Filter registrations based on active tab and filters
  const filteredRegistrations = React.useMemo(() => {
    let filtered = mockRegistrations;

    // Tab filter
    if (activeTab === 'upcoming') {
      filtered = filtered.filter(reg => reg.status === 'upcoming');
    } else if (activeTab === 'completed') {
      filtered = filtered.filter(reg => reg.status === 'completed');
    }

    // Search filter
    if (searchQuery) {
      filtered = filtered.filter(reg =>
        reg.eventTitle.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    // Status filter
    if (statusFilter !== 'all') {
      filtered = filtered.filter(reg => reg.paymentStatus === statusFilter);
    }

    // Sort
    filtered.sort((a, b) => {
      if (sortBy === 'eventDate') {
        return new Date(b.eventDate).getTime() - new Date(a.eventDate).getTime();
      } else if (sortBy === 'registrationDate') {
        return new Date(b.registrationDate).getTime() - new Date(a.registrationDate).getTime();
      }
      return 0;
    });

    return filtered;
  }, [activeTab, searchQuery, statusFilter, sortBy]);

  // Pagination
  const totalPages = Math.ceil(filteredRegistrations.length / registrationsPerPage);
  const paginatedRegistrations = filteredRegistrations.slice(
    (currentPage - 1) * registrationsPerPage,
    currentPage * registrationsPerPage
  );

  const getStatusBadge = (registration: EventRegistration) => {
    const { status, paymentStatus, attendanceStatus } = registration;
    
    if (status === 'upcoming') {
      return {
        text: paymentStatus === 'paid' ? 'Confirmed' : paymentStatus === 'pending' ? 'Payment Pending' : 'Free Event',
        color: paymentStatus === 'paid' ? '#228B22' : paymentStatus === 'pending' ? '#DAA520' : '#9D4EDD',
      };
    }
    
    if (status === 'completed') {
      return {
        text: attendanceStatus === 'attended' ? 'Attended' : 'No Show',
        color: attendanceStatus === 'attended' ? '#228B22' : '#DC143C',
      };
    }
    
    return { text: 'Cancelled', color: '#8B8680' };
  };

  const handleCancelRegistration = (registration: EventRegistration) => {
    setSelectedRegistration(registration);
    openCancelModal();
  };

  const confirmCancelRegistration = () => {
    if (selectedRegistration) {
      console.log('Canceling registration:', selectedRegistration.id);
      // TODO: Implement cancellation API call
    }
    closeCancelModal();
    setSelectedRegistration(null);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== new Date().getFullYear() ? 'numeric' : undefined,
    });
  };

  const formatCurrency = (amount: number) => {
    return amount === 0 ? 'Free' : `$${amount.toFixed(2)}`;
  };

  // Statistics
  const stats = React.useMemo(() => {
    const total = mockRegistrations.length;
    const attended = mockRegistrations.filter(r => r.attendanceStatus === 'attended').length;
    const upcoming = mockRegistrations.filter(r => r.status === 'upcoming').length;
    const totalSpent = mockRegistrations
      .filter(r => r.paymentStatus === 'paid')
      .reduce((sum, r) => sum + r.price, 0);

    return { total, attended, upcoming, totalSpent };
  }, []);

  return (
    <DashboardLayout data-testid="page-registrations">
      <Box>
        <Title
          order={1}
          mb="xl"
          style={{
            fontFamily: "'Montserrat', sans-serif",
            fontSize: '28px',
            fontWeight: 800,
            color: '#880124',
            textTransform: 'uppercase',
            letterSpacing: '-0.5px',
          }}
        >
          Registration History
        </Title>

        {/* Statistics Cards */}
        <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md" mb="xl">
          <Card
            style={{
              background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
              border: '1px solid rgba(183, 109, 117, 0.2)',
            }}
          >
            <Text size="xs" c="dimmed" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Total Events
            </Text>
            <Text size="xl" fw={700} style={{ color: '#880124' }}>
              {stats.total}
            </Text>
          </Card>

          <Card
            style={{
              background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
              border: '1px solid rgba(183, 109, 117, 0.2)',
            }}
          >
            <Text size="xs" c="dimmed" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Attended
            </Text>
            <Text size="xl" fw={700} style={{ color: '#228B22' }}>
              {stats.attended}
            </Text>
          </Card>

          <Card
            style={{
              background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
              border: '1px solid rgba(183, 109, 117, 0.2)',
            }}
          >
            <Text size="xs" c="dimmed" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Upcoming
            </Text>
            <Text size="xl" fw={700} style={{ color: '#DAA520' }}>
              {stats.upcoming}
            </Text>
          </Card>

          <Card
            style={{
              background: 'linear-gradient(135deg, #f8f4e6, #e8ddd4)',
              border: '1px solid rgba(183, 109, 117, 0.2)',
            }}
          >
            <Text size="xs" c="dimmed" style={{ textTransform: 'uppercase', letterSpacing: '0.5px' }}>
              Total Spent
            </Text>
            <Text size="xl" fw={700} style={{ color: '#9D4EDD' }}>
              ${stats.totalSpent.toFixed(2)}
            </Text>
          </Card>
        </SimpleGrid>

        {/* Filters */}
        <Paper
          mb="md"
          p="md"
          style={{
            background: '#FFF8F0',
            border: '1px solid rgba(183, 109, 117, 0.2)',
          }}
        >
          <Group gap="md">
            <TextInput
              placeholder="Search events..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{ minWidth: '200px' }}
            />

            <Select
              placeholder="Payment Status"
              data={[
                { value: 'all', label: 'All Payments' },
                { value: 'paid', label: 'Paid' },
                { value: 'pending', label: 'Pending' },
                { value: 'free', label: 'Free' },
                { value: 'refunded', label: 'Refunded' },
              ]}
              value={statusFilter}
              onChange={(value) => setStatusFilter(value || 'all')}
              style={{ minWidth: '150px' }}
            />

            <Select
              placeholder="Sort By"
              data={[
                { value: 'eventDate', label: 'Event Date' },
                { value: 'registrationDate', label: 'Registration Date' },
              ]}
              value={sortBy}
              onChange={(value) => setSortBy(value || 'eventDate')}
              style={{ minWidth: '150px' }}
            />
          </Group>
        </Paper>

        {/* Tabs */}
        <Tabs value={activeTab} onChange={setActiveTab}>
          <Tabs.List>
            <Tabs.Tab
              value="all"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              All ({mockRegistrations.length})
            </Tabs.Tab>
            <Tabs.Tab
              value="upcoming"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Upcoming ({stats.upcoming})
            </Tabs.Tab>
            <Tabs.Tab
              value="completed"
              style={{
                fontFamily: "'Montserrat', sans-serif",
                fontWeight: 600,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
              }}
            >
              Completed ({mockRegistrations.filter(r => r.status === 'completed').length})
            </Tabs.Tab>
          </Tabs.List>

          {/* All Registrations */}
          <Tabs.Panel value="all" pt="md">
            {isMobile ? (
              // Mobile Card View
              <Stack gap="sm">
                {paginatedRegistrations.map((registration) => {
                  const statusBadge = getStatusBadge(registration);
                  return (
                    <Card
                      key={registration.id}
                      style={{
                        background: '#FFF8F0',
                        border: '1px solid rgba(183, 109, 117, 0.15)',
                      }}
                    >
                      <Group justify="space-between" mb="xs">
                        <Text fw={600} style={{ color: '#2B2B2B' }}>
                          {registration.eventTitle}
                        </Text>
                        <Badge
                          size="sm"
                          style={{
                            backgroundColor: `${statusBadge.color}20`,
                            color: statusBadge.color,
                            border: `1px solid ${statusBadge.color}40`,
                          }}
                        >
                          {statusBadge.text}
                        </Badge>
                      </Group>

                      <Stack gap="xs">
                        <Group justify="space-between">
                          <Text size="sm" c="dimmed">Date</Text>
                          <Text size="sm">{formatDate(registration.eventDate)}</Text>
                        </Group>
                        <Group justify="space-between">
                          <Text size="sm" c="dimmed">Time</Text>
                          <Text size="sm">{registration.eventTime}</Text>
                        </Group>
                        <Group justify="space-between">
                          <Text size="sm" c="dimmed">Price</Text>
                          <Text size="sm" fw={600}>{formatCurrency(registration.price)}</Text>
                        </Group>
                      </Stack>

                      {registration.canCancel && (
                        <Button
                          size="xs"
                          variant="light"
                          color="red"
                          mt="sm"
                          onClick={() => handleCancelRegistration(registration)}
                        >
                          Cancel Registration
                        </Button>
                      )}
                    </Card>
                  );
                })}
              </Stack>
            ) : (
              // Desktop Table View
              <Table data-testid="list-registrations" striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Event</Table.Th>
                    <Table.Th>Date & Time</Table.Th>
                    <Table.Th>Status</Table.Th>
                    <Table.Th>Price</Table.Th>
                    <Table.Th>Registered</Table.Th>
                    <Table.Th>Actions</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {paginatedRegistrations.map((registration) => {
                    const statusBadge = getStatusBadge(registration);
                    return (
                      <Table.Tr key={registration.id}>
                        <Table.Td>
                          <Text fw={600}>{registration.eventTitle}</Text>
                          <Text size="xs" c="dimmed" style={{ textTransform: 'capitalize' }}>
                            {registration.eventType}
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Text>{formatDate(registration.eventDate)}</Text>
                          <Text size="xs" c="dimmed">{registration.eventTime}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Badge
                            size="sm"
                            style={{
                              backgroundColor: `${statusBadge.color}20`,
                              color: statusBadge.color,
                              border: `1px solid ${statusBadge.color}40`,
                            }}
                          >
                            {statusBadge.text}
                          </Badge>
                        </Table.Td>
                        <Table.Td>
                          <Text fw={600}>{formatCurrency(registration.price)}</Text>
                        </Table.Td>
                        <Table.Td>
                          <Text size="sm">{formatDate(registration.registrationDate)}</Text>
                        </Table.Td>
                        <Table.Td>
                          {registration.canCancel ? (
                            <Button
                              data-testid="button-cancel-registration"
                              size="xs"
                              variant="light"
                              color="red"
                              onClick={() => handleCancelRegistration(registration)}
                            >
                              Cancel
                            </Button>
                          ) : (
                            <Text size="xs" c="dimmed">—</Text>
                          )}
                        </Table.Td>
                      </Table.Tr>
                    );
                  })}
                </Table.Tbody>
              </Table>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <Group justify="center" mt="xl">
                <Pagination
                  value={currentPage}
                  onChange={setCurrentPage}
                  total={totalPages}
                  color="wcr"
                />
              </Group>
            )}

            {filteredRegistrations.length === 0 && (
              <Box style={{ textAlign: 'center', padding: '40px' }}>
                <Text size="xl" style={{ fontSize: '48px', marginBottom: '16px', opacity: 0.3 }}>
                  📅
                </Text>
                <Title order={3} mb="sm" style={{ color: '#8B8680' }}>
                  No registrations found
                </Title>
                <Text c="dimmed">
                  {searchQuery || statusFilter !== 'all'
                    ? 'Try adjusting your search filters'
                    : 'Participate in events to see your history here'}
                </Text>
              </Box>
            )}
          </Tabs.Panel>

          {/* Upcoming Registrations */}
          <Tabs.Panel value="upcoming" pt="md">
            <Stack gap="sm">
              {paginatedRegistrations.filter(r => r.status === 'upcoming').map((registration) => {
                const statusBadge = getStatusBadge(registration);
                return (
                  <Card
                    key={registration.id}
                    style={{
                      background: '#FFF8F0',
                      border: '1px solid rgba(183, 109, 117, 0.15)',
                      borderLeft: '4px solid #DAA520',
                    }}
                  >
                    <Group justify="space-between" mb="sm">
                      <div>
                        <Text fw={700} style={{ color: '#2B2B2B', marginBottom: '4px' }}>
                          {registration.eventTitle}
                        </Text>
                        <Text size="sm" c="dimmed">
                          {formatDate(registration.eventDate)} • {registration.eventTime}
                        </Text>
                      </div>
                      <Badge
                        size="lg"
                        style={{
                          backgroundColor: `${statusBadge.color}20`,
                          color: statusBadge.color,
                          border: `1px solid ${statusBadge.color}40`,
                        }}
                      >
                        {statusBadge.text}
                      </Badge>
                    </Group>

                    <Group justify="space-between" align="center">
                      <Group gap="lg">
                        <Text size="sm">
                          <Text component="span" c="dimmed">Price:</Text>{' '}
                          <Text component="span" fw={600}>{formatCurrency(registration.price)}</Text>
                        </Text>
                        <Text size="sm">
                          <Text component="span" c="dimmed">Joined:</Text>{' '}
                          {formatDate(registration.registrationDate)}
                        </Text>
                      </Group>

                      {registration.canCancel && (
                        <Button
                          size="sm"
                          variant="light"
                          color="red"
                          onClick={() => handleCancelRegistration(registration)}
                        >
                          Cancel Registration
                        </Button>
                      )}
                    </Group>
                  </Card>
                );
              })}
            </Stack>
          </Tabs.Panel>

          {/* Completed Registrations */}
          <Tabs.Panel value="completed" pt="md">
            <Stack gap="sm">
              {paginatedRegistrations.filter(r => r.status === 'completed').map((registration) => {
                const statusBadge = getStatusBadge(registration);
                const borderColor = registration.attendanceStatus === 'attended' ? '#228B22' : '#DC143C';
                
                return (
                  <Card
                    key={registration.id}
                    style={{
                      background: '#FFF8F0',
                      border: '1px solid rgba(183, 109, 117, 0.15)',
                      borderLeft: `4px solid ${borderColor}`,
                      opacity: registration.attendanceStatus === 'no-show' ? 0.7 : 1,
                    }}
                  >
                    <Group justify="space-between" mb="sm">
                      <div>
                        <Text fw={700} style={{ color: '#2B2B2B', marginBottom: '4px' }}>
                          {registration.eventTitle}
                        </Text>
                        <Text size="sm" c="dimmed">
                          {formatDate(registration.eventDate)} • {registration.eventTime}
                        </Text>
                      </div>
                      <Badge
                        size="lg"
                        style={{
                          backgroundColor: `${statusBadge.color}20`,
                          color: statusBadge.color,
                          border: `1px solid ${statusBadge.color}40`,
                        }}
                      >
                        {statusBadge.text}
                      </Badge>
                    </Group>

                    <Group justify="space-between">
                      <Text size="sm">
                        <Text component="span" c="dimmed">Price:</Text>{' '}
                        <Text component="span" fw={600}>{formatCurrency(registration.price)}</Text>
                      </Text>
                      <Text size="sm" c="dimmed">
                        Joined: {formatDate(registration.registrationDate)}
                      </Text>
                    </Group>
                  </Card>
                );
              })}
            </Stack>
          </Tabs.Panel>
        </Tabs>

        {/* Cancel Registration Modal */}
        <Modal
          opened={cancelModalOpened}
          onClose={closeCancelModal}
          title="Cancel Registration"
          size="md"
        >
          {selectedRegistration && (
            <Stack gap="md">
              <Alert color="orange" variant="light">
                <Text size="sm">
                  You are about to cancel your registration for:{' '}
                  <Text component="span" fw={600}>
                    {selectedRegistration.eventTitle}
                  </Text>
                </Text>
              </Alert>

              <Box>
                <Text size="sm" mb="xs">
                  <strong>Event Date:</strong> {formatDate(selectedRegistration.eventDate)}
                </Text>
                <Text size="sm" mb="xs">
                  <strong>Event Time:</strong> {selectedRegistration.eventTime}
                </Text>
                <Text size="sm" mb="xs">
                  <strong>Registration Price:</strong> {formatCurrency(selectedRegistration.price)}
                </Text>
                {selectedRegistration.refundAmount !== undefined && (
                  <Text size="sm" mb="xs">
                    <strong>Refund Amount:</strong> ${selectedRegistration.refundAmount.toFixed(2)}
                  </Text>
                )}
              </Box>

              <Text size="sm" c="dimmed">
                Cancellation policies apply. Please review the event's specific cancellation
                terms before proceeding.
              </Text>

              <Group justify="flex-end" gap="sm">
                <Button variant="default" onClick={closeCancelModal}>
                  Keep Registration
                </Button>
                <Button color="red" onClick={confirmCancelRegistration}>
                  Cancel Registration
                </Button>
              </Group>
            </Stack>
          )}
        </Modal>
      </Box>
    </DashboardLayout>
  );
};