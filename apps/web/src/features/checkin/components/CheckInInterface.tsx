// CheckInInterface - Main mobile-first check-in interface
// Primary staff interface for processing attendee check-ins

import React, { useState, useCallback, useMemo } from 'react';
import {
  Container,
  Stack,
  Group,
  Text,
  Card,
  Badge,
  Button,
  ActionIcon,
  Alert,
  Loader,
  Center,
  Modal,
  TextInput,
  Textarea
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import { IconDashboard, IconPlus, IconSettings } from '@tabler/icons-react';

import { AttendeeSearch } from './AttendeeSearch';
import { AttendeeList } from './AttendeeList';
import { CheckInConfirmation } from './CheckInConfirmation';
import { CheckInDashboard } from './CheckInDashboard';
import { CompactSyncStatus } from './SyncStatus';

import { 
  useEventAttendees, 
  useCheckInAttendee, 
  useEventDashboard,
  useCreateManualEntry 
} from '../hooks/useCheckIn';
import { useOfflineSync } from '../hooks/useOfflineSync';

import type { 
  CheckInAttendee, 
  AttendeeSearchParams,
  RegistrationStatus,
  ManualEntryData,
  CheckInResponse
} from '../types/checkin.types';
import { TOUCH_TARGETS } from '../types/checkin.types';

interface CheckInInterfaceProps {
  eventId: string;
  staffMemberId: string;
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
      styles={{
        title: { 
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 600
        }
      }}
    >
      <form onSubmit={handleSubmit}>
        <Stack gap="md">
          <TextInput
            label="Full Name"
            required
            value={formData.name}
            onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
            placeholder="Enter attendee's name"
          />

          <TextInput
            label="Email Address"
            type="email"
            required
            value={formData.email}
            onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
            placeholder="Enter email address"
          />

          <TextInput
            label="Phone Number"
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
 * Main check-in interface optimized for mobile staff use
 */
export function CheckInInterface({
  eventId,
  staffMemberId,
  eventTitle,
  onNavigateToDashboard
}: CheckInInterfaceProps) {
  // State management
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<RegistrationStatus | 'all'>('all');
  const [selectedAttendee, setSelectedAttendee] = useState<CheckInAttendee | null>(null);
  const [checkInResponse, setCheckInResponse] = useState<CheckInResponse | null>(null);
  const [showDashboard, setShowDashboard] = useState(false);

  // Modal states
  const [confirmationOpened, { open: openConfirmation, close: closeConfirmation }] = useDisclosure(false);
  const [manualEntryOpened, { open: openManualEntry, close: closeManualEntry }] = useDisclosure(false);

  // Offline sync
  const { isOnline, pendingCount } = useOfflineSync();

  // Search parameters
  const searchParams: AttendeeSearchParams = useMemo(() => ({
    eventId,
    search: searchTerm || undefined,
    status: statusFilter === 'all' ? undefined : statusFilter,
    page: 1,
    pageSize: 50
  }), [eventId, searchTerm, statusFilter]);

  // API hooks
  const {
    data: attendeesResponse,
    isLoading: loadingAttendees,
    error: attendeesError,
    refetch: refetchAttendees
  } = useEventAttendees(searchParams);

  const {
    data: dashboard,
    isLoading: loadingDashboard,
    error: dashboardError,
    refetch: refetchDashboard
  } = useEventDashboard(eventId);

  const checkInMutation = useCheckInAttendee(eventId);
  const manualEntryMutation = useCreateManualEntry(eventId);

  // Event handlers
  const handleSearch = useCallback((term: string) => {
    setSearchTerm(term);
  }, []);

  const handleStatusFilter = useCallback((status: RegistrationStatus | 'all') => {
    setStatusFilter(status);
  }, []);

  const handleCheckIn = useCallback(async (attendee: CheckInAttendee) => {
    try {
      const checkInTime = new Date().toISOString();
      const response = await checkInMutation.mutateAsync({
        attendeeId: attendee.attendeeId,
        checkInTime,
        staffMemberId,
        notes: undefined,
        overrideCapacity: false,
        isManualEntry: false
      });

      setSelectedAttendee(attendee);
      setCheckInResponse(response);
      openConfirmation();
    } catch (error) {
      console.error('Check-in failed:', error);
      // Error is handled by the mutation's onError
    }
  }, [checkInMutation, staffMemberId, openConfirmation]);

  const handleManualEntry = useCallback(async (data: ManualEntryData, notes?: string) => {
    try {
      const response = await manualEntryMutation.mutateAsync({
        staffMemberId,
        manualEntryData: data,
        notes
      });

      // Create a temporary attendee object for confirmation
      const tempAttendee: CheckInAttendee = {
        attendeeId: 'manual-' + Date.now(),
        userId: 'manual',
        sceneName: data.name,
        email: data.email,
        registrationStatus: 'checked-in',
        isFirstTime: true, // Assume manual entries are first-time
        hasCompletedWaiver: data.hasCompletedWaiver,
        dietaryRestrictions: data.dietaryRestrictions || undefined,
        accessibilityNeeds: data.accessibilityNeeds || undefined
      };

      setSelectedAttendee(tempAttendee);
      setCheckInResponse(response);
      closeManualEntry();
      openConfirmation();
    } catch (error) {
      console.error('Manual entry failed:', error);
      // Error is handled by the mutation's onError
    }
  }, [manualEntryMutation, staffMemberId, closeManualEntry, openConfirmation]);

  const handleConfirmationContinue = useCallback(() => {
    closeConfirmation();
    setSelectedAttendee(null);
    setCheckInResponse(null);
    refetchAttendees();
  }, [closeConfirmation, refetchAttendees]);

  const handleNewCheckIn = useCallback(() => {
    closeConfirmation();
    setSelectedAttendee(null);
    setCheckInResponse(null);
    setSearchTerm('');
    setStatusFilter('all');
    refetchAttendees();
  }, [closeConfirmation, refetchAttendees]);

  // Dashboard view toggle
  if (showDashboard) {
    return (
      <Container size="lg" py="md">
        <Stack gap="lg">
          <Group justify="space-between" align="center">
            <Button
              variant="outline"
              onClick={() => setShowDashboard(false)}
              size="sm"
            >
              ← Back to Check-In
            </Button>
            <CompactSyncStatus />
          </Group>

          <CheckInDashboard
            dashboard={dashboard}
            isLoading={loadingDashboard}
            error={dashboardError}
            onRefresh={refetchDashboard}
            canExport={true}
          />
        </Stack>
      </Container>
    );
  }

  return (
    <Container size="md" py="md">
      <Stack gap="lg">
        {/* Header */}
        <Card shadow="sm" padding="md" radius="md">
          <Group justify="space-between" align="center">
            <Stack gap="xs">
              <Text 
                size="lg" 
                fw={700}
                style={{ fontFamily: 'Bodoni Moda, serif' }}
                truncate
              >
                {eventTitle || 'Event Check-In'}
              </Text>
              
              {dashboard && (
                <Group align="center" gap="xs">
                  <Badge color="blue" variant="filled">
                    {dashboard.capacity.checkedInCount} / {dashboard.capacity.totalCapacity}
                  </Badge>
                  <Text size="sm" c="dimmed">
                    {dashboard.capacity.availableSpots} spots available
                  </Text>
                </Group>
              )}
            </Stack>

            <Group align="center" gap="xs">
              <CompactSyncStatus />
              
              <ActionIcon
                variant="subtle"
                onClick={() => setShowDashboard(true)}
                size="lg"
                aria-label="View dashboard"
                style={{
                  minWidth: TOUCH_TARGETS.MINIMUM,
                  minHeight: TOUCH_TARGETS.MINIMUM
                }}
              >
                <IconDashboard size={20} />
              </ActionIcon>
            </Group>
          </Group>
        </Card>

        {/* Search Interface */}
        <AttendeeSearch
          onSearch={handleSearch}
          onStatusFilter={handleStatusFilter}
          searchValue={searchTerm}
          statusFilter={statusFilter}
          isLoading={loadingAttendees}
          resultCount={attendeesResponse?.attendees.length}
          placeholder="Search name, email, or ticket #"
        />

        {/* Manual Entry Button */}
        <Button
          leftSection={<IconPlus size={16} />}
          onClick={openManualEntry}
          variant="outline"
          color="blue"
          size="lg"
          fullWidth
          style={{
            minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
            borderRadius: '12px 6px 12px 6px',
            fontFamily: 'Montserrat, sans-serif',
            fontWeight: 600
          }}
        >
          ➕ Manual Entry (Walk-in)
        </Button>

        {/* Offline Alert */}
        {!isOnline && (
          <Alert color="yellow" variant="light">
            <Group align="center" gap="xs">
              <Text size="sm" fw={500}>
                Offline Mode - {pendingCount} actions queued
              </Text>
            </Group>
          </Alert>
        )}

        {/* Attendee List */}
        <AttendeeList
          attendees={attendeesResponse?.attendees || []}
          onCheckIn={handleCheckIn}
          isLoading={loadingAttendees}
          error={attendeesError}
          isCheckingIn={checkInMutation.isPending}
          checkingInAttendeeId={checkInMutation.variables?.attendeeId}
        />

        {/* Check-in Confirmation Modal */}
        <CheckInConfirmation
          isOpen={confirmationOpened}
          onClose={closeConfirmation}
          attendee={selectedAttendee}
          checkInResponse={checkInResponse}
          onContinue={handleConfirmationContinue}
          onNewCheckIn={handleNewCheckIn}
        />

        {/* Manual Entry Modal */}
        <ManualEntryModal
          isOpen={manualEntryOpened}
          onClose={closeManualEntry}
          onSubmit={handleManualEntry}
          isLoading={manualEntryMutation.isPending}
        />
      </Stack>
    </Container>
  );
}