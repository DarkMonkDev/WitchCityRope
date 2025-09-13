// AttendeeList component for CheckIn system
// Mobile-optimized list display with touch-friendly interactions

import React, { useState } from 'react';
import {
  Stack,
  Card,
  Group,
  Text,
  Badge,
  Button,
  ActionIcon,
  Collapse,
  Box,
  Divider,
  Loader,
  Center,
  Alert,
  ScrollArea
} from '@mantine/core';
import { IconChevronDown, IconChevronUp, IconUser, IconPhone, IconMail } from '@tabler/icons-react';
import type { CheckInAttendee, RegistrationStatus } from '../types/checkin.types';
import { STATUS_CONFIGS, TOUCH_TARGETS } from '../types/checkin.types';

interface AttendeeListProps {
  attendees: CheckInAttendee[];
  onCheckIn: (attendee: CheckInAttendee) => void;
  isLoading?: boolean;
  error?: string | null;
  isCheckingIn?: boolean;
  checkingInAttendeeId?: string;
}

interface AttendeeCardProps {
  attendee: CheckInAttendee;
  onCheckIn: (attendee: CheckInAttendee) => void;
  isCheckingIn?: boolean;
  isExpanded?: boolean;
  onToggleExpand?: () => void;
}

/**
 * Individual attendee card with expandable details
 * Optimized for mobile touch interaction
 */
function AttendeeCard({ 
  attendee, 
  onCheckIn, 
  isCheckingIn = false,
  isExpanded = false,
  onToggleExpand 
}: AttendeeCardProps) {
  const statusConfig = STATUS_CONFIGS[attendee.registrationStatus];
  const isCheckedIn = attendee.registrationStatus === 'checked-in';
  const canCheckIn = !isCheckedIn && attendee.hasCompletedWaiver;

  // Important notes that should be highlighted
  const hasImportantNotes = attendee.dietaryRestrictions || 
                            attendee.accessibilityNeeds || 
                            attendee.isFirstTime;

  return (
    <Card 
      shadow="sm" 
      padding="md" 
      radius="md"
      style={{
        minHeight: TOUCH_TARGETS.CARD_MIN_HEIGHT,
        borderLeft: `4px solid ${statusConfig.color}`,
        backgroundColor: isCheckedIn ? '#f8f9fa' : '#ffffff',
        transition: 'all 0.2s ease'
      }}
    >
      <Stack gap="sm">
        {/* Main Info Row */}
        <Group justify="space-between" align="flex-start" wrap="nowrap">
          <Box style={{ flex: 1, minWidth: 0 }}>
            <Group align="center" gap="xs" mb="xs">
              <Text 
                fw={600} 
                size="md"
                style={{ 
                  fontFamily: 'Source Sans 3, sans-serif',
                  color: isCheckedIn ? '#6c757d' : '#212529'
                }}
                truncate
              >
                {attendee.sceneName}
              </Text>
              
              {attendee.isFirstTime && (
                <Badge size="xs" color="blue" variant="filled">
                  First Time
                </Badge>
              )}
            </Group>

            <Text 
              size="sm" 
              c="dimmed" 
              mb="xs"
              truncate
            >
              {attendee.email}
            </Text>

            <Group align="center" gap="xs">
              <Badge 
                color={statusConfig.color}
                variant="filled"
                size="sm"
                style={{
                  borderRadius: '12px 6px 12px 6px'
                }}
              >
                {statusConfig.icon} {statusConfig.label}
              </Badge>
              
              {attendee.waitlistPosition && (
                <Badge color="orange" variant="outline" size="xs">
                  #{attendee.waitlistPosition}
                </Badge>
              )}

              {hasImportantNotes && (
                <Badge color="yellow" variant="outline" size="xs">
                  Notes
                </Badge>
              )}
            </Group>

            {isCheckedIn && attendee.checkInTime && (
              <Text size="xs" c="dimmed" mt="xs">
                Checked in: {new Date(attendee.checkInTime).toLocaleTimeString()}
              </Text>
            )}
          </Box>

          <Group align="center" gap="xs">
            {/* Expand/Collapse Button */}
            {(hasImportantNotes || attendee.emergencyContactName) && onToggleExpand && (
              <ActionIcon
                variant="subtle"
                onClick={onToggleExpand}
                size="sm"
                style={{
                  minWidth: TOUCH_TARGETS.MINIMUM,
                  minHeight: TOUCH_TARGETS.MINIMUM
                }}
                aria-label={isExpanded ? "Hide details" : "Show details"}
              >
                {isExpanded ? <IconChevronUp size={16} /> : <IconChevronDown size={16} />}
              </ActionIcon>
            )}

            {/* Check-in Button */}
            {canCheckIn && (
              <Button
                onClick={() => onCheckIn(attendee)}
                loading={isCheckingIn}
                size="sm"
                color="wcr.7"
                variant="filled"
                style={{
                  minHeight: TOUCH_TARGETS.MINIMUM,
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  fontSize: '12px',
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px'
                }}
              >
                ✅ Check In
              </Button>
            )}

            {!attendee.hasCompletedWaiver && !isCheckedIn && (
              <Badge color="red" variant="outline" size="xs">
                Waiver Required
              </Badge>
            )}
          </Group>
        </Group>

        {/* Expandable Details */}
        <Collapse in={isExpanded}>
          <Divider my="xs" />
          <Stack gap="xs">
            {attendee.ticketNumber && (
              <Group align="center" gap="xs">
                <IconUser size={14} style={{ color: 'var(--mantine-color-dimmed)' }} />
                <Text size="xs" c="dimmed">
                  Ticket: {attendee.ticketNumber}
                </Text>
              </Group>
            )}

            {attendee.pronouns && (
              <Group align="center" gap="xs">
                <Text size="xs" c="dimmed">
                  Pronouns: {attendee.pronouns}
                </Text>
              </Group>
            )}

            {attendee.dietaryRestrictions && (
              <Group align="center" gap="xs">
                <Text size="xs" fw={500} c="orange">
                  🍽️ Dietary: {attendee.dietaryRestrictions}
                </Text>
              </Group>
            )}

            {attendee.accessibilityNeeds && (
              <Group align="center" gap="xs">
                <Text size="xs" fw={500} c="blue">
                  ♿ Accessibility: {attendee.accessibilityNeeds}
                </Text>
              </Group>
            )}

            {attendee.emergencyContactName && (
              <Box>
                <Text size="xs" fw={500} mb="xs" c="red">
                  🚨 Emergency Contact:
                </Text>
                <Group align="center" gap="xs" ml="md">
                  <IconUser size={12} />
                  <Text size="xs">{attendee.emergencyContactName}</Text>
                </Group>
                {attendee.emergencyContactPhone && (
                  <Group align="center" gap="xs" ml="md">
                    <IconPhone size={12} />
                    <Text size="xs">{attendee.emergencyContactPhone}</Text>
                  </Group>
                )}
              </Box>
            )}
          </Stack>
        </Collapse>
      </Stack>
    </Card>
  );
}

/**
 * Main attendee list component with grouping and infinite scroll
 * Optimized for large lists on mobile devices
 */
export function AttendeeList({
  attendees,
  onCheckIn,
  isLoading = false,
  error = null,
  isCheckingIn = false,
  checkingInAttendeeId
}: AttendeeListProps) {
  const [expandedAttendees, setExpandedAttendees] = useState<Set<string>>(new Set());

  const toggleExpanded = (attendeeId: string) => {
    setExpandedAttendees(prev => {
      const newSet = new Set(prev);
      if (newSet.has(attendeeId)) {
        newSet.delete(attendeeId);
      } else {
        newSet.add(attendeeId);
      }
      return newSet;
    });
  };

  // Group attendees by status for better organization
  const groupedAttendees = React.useMemo(() => {
    const groups: Record<RegistrationStatus, CheckInAttendee[]> = {
      'checked-in': [],
      'confirmed': [],
      'waitlist': [],
      'no-show': []
    };

    attendees.forEach(attendee => {
      groups[attendee.registrationStatus].push(attendee);
    });

    // Sort within groups
    Object.keys(groups).forEach(status => {
      groups[status as RegistrationStatus].sort((a, b) => {
        // Prioritize first-time attendees and those with special needs
        const aPriority = (a.isFirstTime ? 1 : 0) + (a.dietaryRestrictions || a.accessibilityNeeds ? 1 : 0);
        const bPriority = (b.isFirstTime ? 1 : 0) + (b.dietaryRestrictions || b.accessibilityNeeds ? 1 : 0);
        
        if (aPriority !== bPriority) return bPriority - aPriority;
        
        // Then by waitlist position if applicable
        if (status === 'waitlist' && a.waitlistPosition && b.waitlistPosition) {
          return a.waitlistPosition - b.waitlistPosition;
        }
        
        // Finally by name
        return a.sceneName.localeCompare(b.sceneName);
      });
    });

    return groups;
  }, [attendees]);

  if (error) {
    return (
      <Alert color="red" title="Error Loading Attendees">
        {error}
      </Alert>
    );
  }

  if (isLoading && attendees.length === 0) {
    return (
      <Center py="xl">
        <Stack align="center" gap="md">
          <Loader size="lg" />
          <Text size="sm" c="dimmed">Loading attendees...</Text>
        </Stack>
      </Center>
    );
  }

  if (attendees.length === 0) {
    return (
      <Center py="xl">
        <Stack align="center" gap="md">
          <Text size="lg" c="dimmed">No attendees found</Text>
          <Text size="sm" c="dimmed">Try adjusting your search or filters</Text>
        </Stack>
      </Center>
    );
  }

  return (
    <ScrollArea.Autosize mah="70vh">
      <Stack gap="md">
        {/* Expected Attendees */}
        {groupedAttendees.confirmed.length > 0 && (
          <Box>
            <Group align="center" gap="xs" mb="sm">
              <Text 
                fw={600} 
                size="sm" 
                c="dimmed"
                style={{ 
                  fontFamily: 'Montserrat, sans-serif',
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                📋 Expected ({groupedAttendees.confirmed.length})
              </Text>
            </Group>
            <Stack gap="xs">
              {groupedAttendees.confirmed.map((attendee) => (
                <AttendeeCard
                  key={attendee.attendeeId}
                  attendee={attendee}
                  onCheckIn={onCheckIn}
                  isCheckingIn={isCheckingIn && checkingInAttendeeId === attendee.attendeeId}
                  isExpanded={expandedAttendees.has(attendee.attendeeId)}
                  onToggleExpand={() => toggleExpanded(attendee.attendeeId)}
                />
              ))}
            </Stack>
          </Box>
        )}

        {/* Waitlist */}
        {groupedAttendees.waitlist.length > 0 && (
          <Box>
            <Group align="center" gap="xs" mb="sm">
              <Text 
                fw={600} 
                size="sm" 
                c="orange"
                style={{ 
                  fontFamily: 'Montserrat, sans-serif',
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                ⚠️ Waitlist ({groupedAttendees.waitlist.length})
              </Text>
            </Group>
            <Stack gap="xs">
              {groupedAttendees.waitlist.map((attendee) => (
                <AttendeeCard
                  key={attendee.attendeeId}
                  attendee={attendee}
                  onCheckIn={onCheckIn}
                  isCheckingIn={isCheckingIn && checkingInAttendeeId === attendee.attendeeId}
                  isExpanded={expandedAttendees.has(attendee.attendeeId)}
                  onToggleExpand={() => toggleExpanded(attendee.attendeeId)}
                />
              ))}
            </Stack>
          </Box>
        )}

        {/* Checked In */}
        {groupedAttendees['checked-in'].length > 0 && (
          <Box>
            <Group align="center" gap="xs" mb="sm">
              <Text 
                fw={600} 
                size="sm" 
                c="green"
                style={{ 
                  fontFamily: 'Montserrat, sans-serif',
                  textTransform: 'uppercase',
                  letterSpacing: '1px'
                }}
              >
                ✅ Checked In ({groupedAttendees['checked-in'].length})
              </Text>
            </Group>
            <Stack gap="xs">
              {groupedAttendees['checked-in'].map((attendee) => (
                <AttendeeCard
                  key={attendee.attendeeId}
                  attendee={attendee}
                  onCheckIn={onCheckIn}
                  isCheckingIn={false}
                  isExpanded={expandedAttendees.has(attendee.attendeeId)}
                  onToggleExpand={() => toggleExpanded(attendee.attendeeId)}
                />
              ))}
            </Stack>
          </Box>
        )}
      </Stack>
    </ScrollArea.Autosize>
  );
}