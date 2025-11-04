// CheckInInterface - Desktop table-based check-in interface
// Primary staff interface for processing attendee check-ins
// Source: /docs/design/wireframes/event-checkin-visual.html (lines 199-741)

import React, { useState, useCallback, useMemo } from 'react';
import {
  Box,
  Stack,
  Group,
  Text,
  Button,
  Alert,
  Loader,
  Center,
  Modal,
  TextInput,
  Textarea,
  Table,
  Badge
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconPlus } from '@tabler/icons-react';

import { CheckInHeader } from './CheckInHeader';
import { CheckInModal } from './CheckInModal';
import { CompactSyncStatus } from './SyncStatus';
import { checkInTheme } from '../styles/theme';
import { CheckInButton, CheckInButtonState } from './CheckInButton';
import { CashPaymentModal } from './CashPaymentModal';
import { QRPaymentModal } from './QRPaymentModal';

import {
  useEventAttendees,
  useCheckInAttendee,
  useEventDashboard,
  useCreateManualEntry
} from '../hooks/useCheckIn';
import { useOfflineSync } from '../hooks/useOfflineSync';
import { checkinApi } from '../api/checkinApi';

import type {
  CheckInAttendee,
  AttendeeSearchParams,
  ManualEntryData,
  CheckInResponse,
  CheckInDashboard as CheckInDashboardType,
  RegistrationStatus,
  CashPaymentData,
  TicketType
} from '../types/checkin.types';

interface CheckInInterfaceProps {
  eventId: string;
  sessionToken: string;
  eventTitle?: string;
  onNavigateToDashboard?: () => void;
}

/**
 * Manual entry modal for walk-in attendees
 */
function ManualEntryModal({
  isOpen,
  onClose,
  onSubmit,
  isLoading
}: {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: ManualEntryData, notes?: string) => void;
  isLoading: boolean;
}) {
  const [formData, setFormData] = useState<ManualEntryData>({
    name: '',
    email: '',
    phone: '',
    dietaryRestrictions: '',
    accessibilityNeeds: '',
    hasCompletedWaiver: false
  });
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email) {
      notifications.show({
        title: 'Missing Information',
        message: 'Name and email are required',
        color: 'red'
      });
      return;
    }
    onSubmit(formData, notes);
  };

  const handleClose = () => {
    setFormData({
      name: '',
      email: '',
      phone: '',
      dietaryRestrictions: '',
      accessibilityNeeds: '',
      hasCompletedWaiver: false
    });
    setNotes('');
    onClose();
  };

  return (
    <Modal
      opened={isOpen}
      onClose={handleClose}
      title="Manual Entry - Walk-in Attendee"
      size="md"
      data-testid="manual-entry-modal"
      styles={{
        title: {
          fontFamily: checkInTheme.fonts.heading,
          fontWeight: 600
        }
      }}
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label="Full Name"
            name="name"
            data-testid="walk-in-name"
            required
            value={formData.name}
            onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
            placeholder="Enter attendee's name"
          />

          <TextInput
            label="Email Address"
            name="email"
            data-testid="walk-in-email"
            type="email"
            required
            value={formData.email}
            onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
            placeholder="Enter email address"
          />

          <TextInput
            label="Phone Number"
            name="phone"
            data-testid="walk-in-phone"
            value={formData.phone}
            onChange={(e) => setFormData(prev => ({ ...prev, phone: e.target.value }))}
            placeholder="Optional phone number"
          />

          <TextInput
            label="Dietary Restrictions"
            value={formData.dietaryRestrictions}
            onChange={(e) => setFormData(prev => ({ ...prev, dietaryRestrictions: e.target.value }))}
            placeholder="Any dietary needs (optional)"
          />

          <TextInput
            label="Accessibility Needs"
            value={formData.accessibilityNeeds}
            onChange={(e) => setFormData(prev => ({ ...prev, accessibilityNeeds: e.target.value }))}
            placeholder="Any accessibility accommodations (optional)"
          />

          <Textarea
            label="Notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Additional notes about this check-in"
            rows={3}
          />

          <Group>
            <input
              type="checkbox"
              name="hasCompletedWaiver"
              data-testid="walk-in-waiver"
              checked={formData.hasCompletedWaiver}
              onChange={(e) => setFormData(prev => ({ ...prev, hasCompletedWaiver: e.target.checked }))}
              id="waiver-checkbox"
            />
            <label htmlFor="waiver-checkbox">
              <Text size="sm">Waiver completed and signed *</Text>
            </label>
          </Group>

          <Alert color="yellow" variant="light">
            <Text size="sm">
              ⚠️ Waiver must be completed before check-in. Ensure attendee has signed waiver.
            </Text>
          </Alert>

          <Group justify="flex-end" gap="md">
            <Button
              variant="outline"
              onClick={handleClose}
              disabled={isLoading}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              loading={isLoading}
              color="wcr.7"
            >
              Check In Walk-in
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  );
}

/**
 * Main check-in interface with desktop table view (KIOSK MODE)
 * Uses session token instead of user authentication
 */
export function CheckInInterface({
  eventId,
  sessionToken,
  eventTitle,
  onNavigateToDashboard
}: CheckInInterfaceProps) {
  // State management
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<RegistrationStatus | 'all'>('all');
  const [selectedAttendee, setSelectedAttendee] = useState<CheckInAttendee | null>(null);
  const [checkInResponse, setCheckInResponse] = useState<CheckInResponse | null>(null);

  // Button state tracking per attendee (for streamlined workflow)
  const [buttonStates, setButtonStates] = useState<Map<string, CheckInButtonState>>(new Map());

  // Selected attendee for payment modals
  const [paymentAttendee, setPaymentAttendee] = useState<CheckInAttendee | null>(null);

  // Ticket types for cash payment modal
  const [ticketTypes, setTicketTypes] = useState<TicketType[]>([]);

  // Modal states
  const [confirmationOpened, { open: openConfirmation, close: closeConfirmation }] = useDisclosure(false);
  const [manualEntryOpened, { open: openManualEntry, close: closeManualEntry }] = useDisclosure(false);
  const [cashPaymentOpened, { open: openCashPayment, close: closeCashPayment }] = useDisclosure(false);
  const [qrPaymentOpened, { open: openQRPayment, close: closeQRPayment }] = useDisclosure(false);

  // Offline sync
  const { isOnline, pendingCount } = useOfflineSync();

  // Load ticket types for event
  React.useEffect(() => {
    const loadTicketTypes = async () => {
      try {
        const types = await checkinApi.getEventTicketTypes(eventId, sessionToken);
        setTicketTypes(types);
      } catch (error) {
        console.error('Failed to load ticket types:', error);
        // Non-critical - payment modal will show empty list
      }
    };

    loadTicketTypes();
  }, [eventId, sessionToken]);

  // Search parameters
  const searchParams: AttendeeSearchParams = useMemo(() => ({
    eventId,
    search: searchTerm || undefined,
    status: statusFilter === 'all' ? undefined : statusFilter,
    page: 1,
    pageSize: 100
  }), [eventId, searchTerm, statusFilter]);

  // API hooks (pass sessionToken for authentication)
  const {
    data: attendeesResponse,
    isLoading: loadingAttendees,
    error: attendeesError,
    refetch: refetchAttendees
  } = useEventAttendees(searchParams, sessionToken);

  const {
    data: dashboard,
    isLoading: loadingDashboard,
    error: dashboardError,
    refetch: refetchDashboard
  } = useEventDashboard(eventId, sessionToken) as {
    data: CheckInDashboardType | undefined;
    isLoading: boolean;
    error: Error | null;
    refetch: () => void;
  };

  const checkInMutation = useCheckInAttendee(eventId, sessionToken);
  const manualEntryMutation = useCreateManualEntry(eventId, sessionToken);

  // Event handlers
  const handleSearch = useCallback((term: string) => {
    setSearchTerm(term);
  }, []);

  const handleStatusFilter = useCallback((status: RegistrationStatus | 'all') => {
    setStatusFilter(status);
  }, []);

  const handleSelectAttendee = useCallback((attendee: CheckInAttendee) => {
    setSelectedAttendee(attendee);
    openConfirmation();
  }, [openConfirmation]);

  const handleCheckIn = useCallback(async (attendee: CheckInAttendee) => {
    try {
      const checkInTime = new Date().toISOString();
      const response = await checkInMutation.mutateAsync({
        attendeeId: attendee.attendeeId,
        checkInTime,
        staffMemberId: undefined, // Kiosk mode - no authenticated staff member
        notes: undefined,
        overrideCapacity: false,
        isManualEntry: false
      }) as CheckInResponse;

      setCheckInResponse(response);
      closeConfirmation();

      notifications.show({
        title: 'Check-in Successful',
        message: `${attendee.sceneName || attendee.email} has been checked in`,
        color: 'green'
      });

      refetchAttendees();
      refetchDashboard();
    } catch (error) {
      console.error('Check-in failed:', error);
    }
  }, [checkInMutation, sessionToken, closeConfirmation, refetchAttendees, refetchDashboard]);

  const handleManualEntry = useCallback(async (data: ManualEntryData, notes?: string) => {
    try {
      const response = await manualEntryMutation.mutateAsync({
        staffMemberId: undefined, // Kiosk mode - no authenticated staff member
        manualEntryData: data,
        notes
      });

      closeManualEntry();

      notifications.show({
        title: 'Walk-in Added',
        message: `${data.name} has been checked in`,
        color: 'green'
      });

      refetchAttendees();
      refetchDashboard();
    } catch (error) {
      console.error('Manual entry failed:', error);
    }
  }, [manualEntryMutation, sessionToken, closeManualEntry, refetchAttendees, refetchDashboard]);

  // Get button state for an attendee
  const getButtonState = useCallback((attendee: CheckInAttendee): CheckInButtonState => {
    // Check if already checked in
    if (attendee.registrationStatus === "CheckedIn") {
      return 'complete';
    }

    // Check for saved state
    const savedState = buttonStates.get(attendee.attendeeId);
    if (savedState) {
      return savedState;
    }

    // Determine initial state based on payment status
    const paymentStatus = (attendee as any).paymentStatus;
    if (paymentStatus === 'rsvp') {
      // RSVP only - payment optional, can skip to covidTest or show paidAtDoor first
      return 'paidAtDoor';
    }

    // Ticket purchased - start with covid test
    return 'covidTest';
  }, [buttonStates]);

  // Filter attendees
  const filteredAttendees = useMemo(() => {
    const attendees = (attendeesResponse as any)?.attendees || [];
    return attendees.filter((attendee: CheckInAttendee) => {
      if (statusFilter === 'all') return true;
      return attendee.registrationStatus === statusFilter;
    });
  }, [attendeesResponse, statusFilter]);

  // Handle button state changes
  const handleButtonStateChange = useCallback((attendeeId: string, newState: CheckInButtonState) => {
    setButtonStates(prev => {
      const updated = new Map(prev);
      updated.set(attendeeId, newState);
      return updated;
    });

    // If final check-in, persist to database
    if (newState === 'complete') {
      const attendee = filteredAttendees.find((a: CheckInAttendee) => a.attendeeId === attendeeId);
      if (attendee) {
        handleCheckIn(attendee);
      }
    }
  }, [filteredAttendees, handleCheckIn]);

  // Handle cash payment modal
  const handleCashPaymentClick = useCallback((attendee: CheckInAttendee) => {
    setPaymentAttendee(attendee);
    openCashPayment();
  }, [openCashPayment]);

  // Handle QR payment modal
  const handleQRPaymentClick = useCallback((attendee: CheckInAttendee) => {
    setPaymentAttendee(attendee);
    openQRPayment();
  }, [openQRPayment]);

  // Submit cash payment (UPDATED for functional spec v2.0)
  const handleCashPaymentSubmit = useCallback(async (data: CashPaymentData) => {
    if (!paymentAttendee) return;

    try {
      // Create ticket purchase via new API endpoint (not standalone payment)
      await checkinApi.createCashTicketPurchase(
        eventId,
        {
          eventId,
          userId: paymentAttendee.userId,
          ticketTypeId: data.ticketTypeId,
          amount: data.amount,
          recordedByStaffId: undefined, // Kiosk mode - extracted from session token by backend
          notes: data.notes
        },
        sessionToken
      );

      // Update button state to covidTest after payment
      setButtonStates(prev => {
        const updated = new Map(prev);
        updated.set(paymentAttendee.attendeeId, 'covidTest');
        return updated;
      });

      // Refresh attendee list to show ticket purchase status
      refetchAttendees();

      closeCashPayment();
      setPaymentAttendee(null);

      notifications.show({
        title: 'Ticket Purchased',
        message: `$${data.amount.toFixed(2)} cash ticket purchased for ${paymentAttendee.sceneName || paymentAttendee.email}`,
        color: 'green'
      });
    } catch (error) {
      console.error('Cash ticket purchase failed:', error);
      notifications.show({
        title: 'Purchase Failed',
        message: 'Failed to create ticket purchase. Please try again.',
        color: 'red'
      });
    }
  }, [paymentAttendee, eventId, sessionToken, closeCashPayment, refetchAttendees]);

  // Handle QR payment completion
  const handleQRPaymentComplete = useCallback(() => {
    if (!paymentAttendee) return;

    // Update button state to covidTest after payment
    setButtonStates(prev => {
      const updated = new Map(prev);
      updated.set(paymentAttendee.attendeeId, 'covidTest');
      return updated;
    });

    closeQRPayment();
    setPaymentAttendee(null);

    notifications.show({
      title: 'Payment Complete',
      message: `Digital payment received for ${paymentAttendee.sceneName || paymentAttendee.email}`,
      color: 'green'
    });
  }, [paymentAttendee, closeQRPayment]);

  // Calculate stats
  const stats = useMemo(() => {
    const attendees = (attendeesResponse as any)?.attendees || [];
    return {
      notArrived: attendees.filter((a: CheckInAttendee) =>
        a.registrationStatus !== "CheckedIn"
      ).length,
      total: dashboard?.capacity.totalCapacity || 0,
      needWaiver: attendees.filter((a: CheckInAttendee) => !a.hasCompletedWaiver).length,
      checkedIn: dashboard?.capacity.checkedInCount || 0
    };
  }, [attendeesResponse, dashboard]);

  return (
    <Box style={{
      minHeight: '100vh',
      background: checkInTheme.colors.cream
    }}>
      {/* Inline styles for clickable rows */}
      <style>{`
        .clickable-row:hover {
          background-color: rgba(139, 121, 94, 0.08) !important;
        }
      `}</style>
      {/* Header Bar */}
      <CheckInHeader
        eventTitle={dashboard?.eventTitle || eventTitle || 'Event Check-In'}
        eventDate={dashboard?.eventDate ? new Date(dashboard.eventDate) : new Date()}
        onExit={() => window.location.href = '/admin/events'}
        checkedInCount={dashboard?.capacity.checkedInCount || 0}
        totalCount={((attendeesResponse as any)?.attendees || []).length}
      />

      {/* Main Content */}
      <Box style={{ maxWidth: 1400, margin: '0 auto', padding: 40 }}>
        {/* Controls Bar - Search + Filter Tabs */}
        <Box style={{
          background: checkInTheme.colors.ivory,
          padding: 24,
          borderRadius: 12,
          marginBottom: 32,
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
          border: `1px solid ${checkInTheme.colors.taupe}`
        }}>
          {/* Search Box */}
          <TextInput
            placeholder="Search by name..."
            value={searchTerm}
            onChange={(e) => handleSearch(e.target.value)}
            style={{ flex: 1, maxWidth: 400 }}
            styles={{
              input: {
                border: '2px solid #B8B0A8',
                borderRadius: 8,
                fontSize: 16,
                background: checkInTheme.colors.cream
              }
            }}
          />
        </Box>

        {/* Add Walk-In Button - HIDDEN (on hold per user request) */}
        {/* <Box style={{ marginBottom: 24 }}>
          <Button
            leftSection={<IconPlus size={20} />}
            onClick={openManualEntry}
            styles={{
              root: {
                background: checkInTheme.gradients.amber,
                color: checkInTheme.colors.midnight,
                fontFamily: checkInTheme.fonts.heading,
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '1px',
                fontSize: 16,
                padding: '14px 32px',
                borderRadius: 8,
                boxShadow: '0 2px 8px rgba(255, 191, 0, 0.3)',
                height: 'auto'
              }
            }}
          >
            Add Walk-In
          </Button>
        </Box> */}

        {/* Offline Alert */}
        {!isOnline && (
          <Alert color="yellow" variant="light" style={{ marginBottom: 24 }}>
            <Group align="center" gap="xs">
              <Text size="sm" fw={500}>
                Offline Mode - {pendingCount} actions queued
              </Text>
            </Group>
          </Alert>
        )}

        {/* Attendee Table */}
        <Box style={{
          background: 'white',
          borderRadius: 12,
          overflow: 'hidden',
          boxShadow: '0 4px 20px rgba(0,0,0,0.08)'
        }}>
          {loadingAttendees ? (
            <Center p="xl">
              <Loader size="lg" />
            </Center>
          ) : attendeesError ? (
            <Alert color="red" m="md">
              Error loading attendees: {attendeesError.message}
            </Alert>
          ) : filteredAttendees.length === 0 ? (
            <Box p="xl" style={{ textAlign: 'center' }}>
              <Text c="dimmed" size="lg">No attendees found</Text>
            </Box>
          ) : (
            <Table striped highlightOnHover withTableBorder>
              <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
                <Table.Tr>
                  <Table.Th style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}>
                    Name
                  </Table.Th>
                  <Table.Th style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}>
                    Pronouns
                  </Table.Th>
                  <Table.Th style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}>
                    Payment
                  </Table.Th>
                  <Table.Th style={{
                    color: 'white',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    letterSpacing: '1px'
                  }}>
                    Status
                  </Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filteredAttendees.map((attendee: CheckInAttendee) => (
                  <Table.Tr
                    key={attendee.attendeeId}
                  >
                    <Table.Td style={{ padding: 8 }}>
                      <Text fw={600} size="16px">
                        {attendee.sceneName || attendee.email}
                      </Text>
                    </Table.Td>
                    <Table.Td style={{ padding: 8 }}>
                      <Text size="14px" c="dimmed">
                        {attendee.pronouns || '—'}
                      </Text>
                    </Table.Td>
                    {/* Payment column - TODO: Make conditional based on event type */}
                    <Table.Td style={{ padding: 8 }}>
                      <Badge
                        styles={{
                          root: {
                            background: (attendee as any).paymentStatus === 'Paid'
                              ? checkInTheme.colors.successLight
                              : checkInTheme.colors.errorLight,
                            color: (attendee as any).paymentStatus === 'Paid'
                              ? checkInTheme.colors.success
                              : checkInTheme.colors.error,
                            border: '1px solid',
                            borderColor: (attendee as any).paymentStatus === 'Paid'
                              ? checkInTheme.colors.success
                              : checkInTheme.colors.error,
                            fontFamily: checkInTheme.fonts.heading,
                            textTransform: 'uppercase',
                            letterSpacing: '0.5px'
                          }
                        }}
                      >
                        {(attendee as any).paymentStatus || 'Unpaid'}
                      </Badge>
                    </Table.Td>
                    {/* Action column - Streamlined check-in button */}
                    <Table.Td style={{ padding: 8 }}>
                      <CheckInButton
                        attendee={{
                          id: attendee.attendeeId,
                          name: attendee.sceneName || attendee.email,
                          pronouns: attendee.pronouns,
                          paymentStatus: (attendee as any).paymentStatus || 'rsvp',
                          isCheckedIn: attendee.registrationStatus === "CheckedIn"
                        }}
                        currentState={getButtonState(attendee)}
                        onStateChange={(newState) => handleButtonStateChange(attendee.attendeeId, newState)}
                        onCashPayment={() => handleCashPaymentClick(attendee)}
                        onQRPayment={() => handleQRPaymentClick(attendee)}
                      />
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Box>
      </Box>

      {/* Check-in Modal */}
      <CheckInModal
        isOpen={confirmationOpened}
        onClose={closeConfirmation}
        attendee={selectedAttendee}
        onConfirm={handleCheckIn}
      />

      {/* Manual Entry Modal */}
      <ManualEntryModal
        isOpen={manualEntryOpened}
        onClose={closeManualEntry}
        onSubmit={handleManualEntry}
        isLoading={manualEntryMutation.isPending}
      />

      {/* Cash Payment Modal (UPDATED - added ticketTypes prop) */}
      {paymentAttendee && (
        <CashPaymentModal
          opened={cashPaymentOpened}
          onClose={closeCashPayment}
          attendee={{
            id: paymentAttendee.attendeeId,
            name: paymentAttendee.sceneName || paymentAttendee.email
          }}
          ticketTypes={ticketTypes}
          onSubmit={handleCashPaymentSubmit}
        />
      )}

      {/* QR Payment Modal (SIMPLIFIED - removed sessionToken and onPaymentComplete) */}
      {paymentAttendee && (
        <QRPaymentModal
          opened={qrPaymentOpened}
          onClose={closeQRPayment}
          attendee={{
            id: paymentAttendee.attendeeId,
            name: paymentAttendee.sceneName || paymentAttendee.email
          }}
          eventId={eventId}
        />
      )}
    </Box>
  );
}
