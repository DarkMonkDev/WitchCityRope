import { Paper, Group, Text, Badge, Button, Stack, Collapse, Alert, Checkbox, Box } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconCheck, IconAlertCircle } from '@tabler/icons-react';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import type { VolunteerPosition } from '../types/volunteer.types';
import { signupForVolunteerPosition, cancelVolunteerSignup } from '../api/volunteerApi';
import { useCurrentUser } from '@/lib/api/hooks/useAuth';
import { formatUtcToLocalTime, formatUtcToLocalDate } from '../../../utils/eventUtils';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

interface VolunteerPositionCardProps {
  position: VolunteerPosition;
  hasExistingParticipation?: boolean; // true if user has RSVP or ticket
}

export const VolunteerPositionCard: React.FC<VolunteerPositionCardProps> = ({
  position,
  hasExistingParticipation = false
}) => {
  const [showSignupConfirm, setShowSignupConfirm] = useState(false);
  const [volunteerTermsAccepted, setVolunteerTermsAccepted] = useState(false);
  const { data: currentUser } = useCurrentUser();
  const queryClient = useQueryClient();
  const isMobile = useMediaQuery('(max-width: 768px)');

  // Determine if we need to show ToS checkbox
  // Only show if user hasn't already RSVPed or purchased a ticket
  const needsTermsAcceptance = !hasExistingParticipation;

  const signupMutation = useMutation<any, any, void>({
    mutationFn: async () => {
      return await signupForVolunteerPosition(position.id ?? '', {
        // If user has existing participation, they've already accepted the waiver
        // Otherwise, use the checkbox state
        eventWaiverAccepted: !needsTermsAcceptance || volunteerTermsAccepted
      });
    },
    onSuccess: (response: any) => {
      notifications.show({
        title: 'Success!',
        message: response.message || 'You have been signed up for this volunteer position. You have also been automatically RSVPed to the event.',
        color: 'green',
        icon: <IconCheck size={16} />
      });

      // Invalidate queries to refresh the volunteer positions list
      queryClient.invalidateQueries({ queryKey: ['volunteerPositions', position.eventId] });

      // Invalidate user volunteer shifts to update dashboard and event detail
      queryClient.invalidateQueries({ queryKey: ['userVolunteerShifts'] });

      // Invalidate user events for dashboard (volunteer signup auto-creates RSVP)
      queryClient.invalidateQueries({ queryKey: ['user-events'] });

      // Invalidate participation status for event detail page
      queryClient.invalidateQueries({ queryKey: ['participation', 'event', position.eventId] });

      setShowSignupConfirm(false);
      setVolunteerTermsAccepted(false); // Reset ToS checkbox
    },
    onError: (error: any) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      notifications.show({
        title: 'Signup Failed',
        message: error instanceof Error ? error.message : 'Failed to sign up for volunteer position',
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });
    }
  });

  const cancelMutation = useMutation<void, any, void>({
    mutationFn: async () => {
      if (!position.userSignupId) {
        throw new Error('No signup ID found');
      }
      await cancelVolunteerSignup(position.userSignupId);
    },
    onSuccess: () => {
      notifications.show({
        title: 'Signup Cancelled',
        message: 'Your volunteer signup has been cancelled.',
        color: 'blue',
        icon: <IconCheck size={16} />
      });

      // Invalidate queries to refresh the volunteer positions list
      queryClient.invalidateQueries({ queryKey: ['volunteerPositions', position.eventId] });

      // Invalidate user volunteer shifts to update dashboard
      queryClient.invalidateQueries({ queryKey: ['userVolunteerShifts'] });

      // Invalidate user events for dashboard
      queryClient.invalidateQueries({ queryKey: ['user-events'] });

      // Invalidate participation status for event detail page
      queryClient.invalidateQueries({ queryKey: ['participation', 'event', position.eventId] });
    },
    onError: (error: any) => {
      // apiClient interceptor extracts RFC 9457 message to error.message
      notifications.show({
        title: 'Cancel Failed',
        message: error instanceof Error ? error.message : 'Failed to cancel volunteer signup',
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });
    }
  });

  const isAuthenticated = !!currentUser;
  const eventTimeZone = useEventTimeZone();

  const formatTime = (timeString?: string) => {
    if (!timeString) return '';
    try {
      // Use TRUE UTC to local time conversion
      // See: /docs/guides-setup/datetime-handling-guide.md
      return formatUtcToLocalTime(timeString, eventTimeZone);
    } catch {
      return timeString;
    }
  };

  // Format "HH:mm" military time to "h:mm AM/PM" format for volunteer shift times
  const formatShiftTime = (time?: string) => {
    if (!time) return '';
    const [hours, minutes] = time.split(':').map(Number);
    const period = hours >= 12 ? 'PM' : 'AM';
    const displayHours = hours % 12 || 12; // Convert 0 to 12 for midnight, 13+ to 1-11
    return `${displayHours}:${minutes.toString().padStart(2, '0')} ${period}`;
  };

  return (
    <Paper
      p="md"
      style={{
        background: 'var(--color-cream)',
        border: '1px solid var(--color-plum)',
        borderRadius: '8px'
      }}
    >
      <Stack gap={0}>
        {/* Title row: name on left, status badge on right */}
        <Group justify="space-between" align="center" wrap="nowrap">
          <Text fw={600} size="md">
            {position.title}
            {/* Only show session name if event has multiple sessions (hide "Main Session" for single-session events) */}
            {position.sessionName && !position.sessionName.includes('Main Session') && (
              <> - {position.sessionName}</>
            )}
          </Text>

          {/* Status badge on the right - matches ticket badge placement */}
          {position.hasUserSignedUp ? (
            <Badge color="green" variant="light" style={{ flexShrink: 0 }}>
              Signed Up
            </Badge>
          ) : position.isFullyStaffed ? (
            <Badge color="gray" variant="light" style={{ flexShrink: 0 }}>
              Full
            </Badge>
          ) : position.canSignUp ? (
            <Badge color="green" variant="light" style={{ flexShrink: 0 }}>
              Open
            </Badge>
          ) : (
            <Badge color="gray" variant="light" style={{ flexShrink: 0 }}>
              Closed
            </Badge>
          )}
        </Group>

        {/* Date/time and description — full width, not constrained by badge column */}
        <Box>
          {/* Date and time - use position shift times if available, otherwise session times */}
          {position.sessionStartTime && (
            <Text size="md" c="dimmed">
              {formatUtcToLocalDate(position.sessionStartTime, eventTimeZone, { weekday: 'long', month: 'short', day: 'numeric' })}
              {(position.startTime || position.endTime) ? (
                <> · {formatShiftTime(position.startTime ?? undefined)} - {formatShiftTime(position.endTime ?? undefined)}</>
              ) : position.sessionEndTime ? (
                <> · {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime ?? undefined)}</>
              ) : null}
            </Text>
          )}

          {/* Description - uses default text color for readability */}
          {position.description && (
            <Text size="md" mt={0}>
              {position.description}
            </Text>
          )}
        </Box>

        {/* Bottom row: spots filled (left) and action button (right), vertically centered.
            Hidden when user has already signed up — the green alert below provides sufficient context. */}
        {!position.hasUserSignedUp && (
        <Group justify="space-between" align="center" mt={4}>
          {/* Spots filled count - same font size as title */}
          <Text size="md" c="dimmed">
            {position.slotsFilled} / {position.slotsNeeded} spots filled
          </Text>

          {/* Sign Up / Login / Blocked message on the right */}
          {!position.isFullyStaffed && position.canSignUp ? (
            isAuthenticated ? (
              <Button
                variant="outline"
                color="burgundy"
                size="sm"
                onClick={() => setShowSignupConfirm(!showSignupConfirm)}
                styles={{
                  root: {
                    borderColor: '#880124',
                    color: '#880124',
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2'
                  }
                }}
              >
                Sign Up
              </Button>
            ) : (
              <Button
                component="a"
                href="/login"
                variant="outline"
                color="blue"
                size="sm"
                styles={{
                  root: {
                    fontWeight: 600,
                    height: '44px',
                    paddingTop: '12px',
                    paddingBottom: '12px',
                    fontSize: '14px',
                    lineHeight: '1.2'
                  }
                }}
              >
                Login to Volunteer
              </Button>
            )
          ) : !position.isFullyStaffed && !position.canSignUp ? (
            <Text size="sm" c="dimmed">
              {position.signupBlockedReason === 'NoTicketForSession'
                ? 'Purchase a ticket to volunteer'
                : 'Signup closed'}
            </Text>
          ) : null}
        </Group>
        )}

          {/* Inline Signup Confirmation */}
          <Collapse in={showSignupConfirm}>
            <Alert
              color="blue"
              variant="light"
              icon={!isMobile ? <IconAlertCircle size={16} /> : undefined}
              title="Confirm Volunteer Signup"
              style={{
                marginTop: 'var(--space-md)',
                ...(isMobile && {
                  marginLeft: 'calc(-1 * var(--space-md))',
                  marginRight: 'calc(-1 * var(--space-md))',
                  marginBottom: 'calc(-1 * var(--space-md))',
                  borderRadius: 0
                })
              }}
              styles={{
                body: { paddingLeft: '6px', paddingRight: '6px' }
              }}
            >
            <Stack gap="sm">
              <Text size="sm">
                Signing up for this volunteer position will automatically RSVP you to the event if you haven't already.
              </Text>

              <Group gap="md" justify="space-between" align="center">
                {/* Terms of Service Acceptance - only show if user doesn't have existing RSVP/ticket */}
                {needsTermsAcceptance ? (
                  <Group gap="sm" align="center" style={{ flex: '1 1 auto', minWidth: 0 }}>
                    <Checkbox
                      id="volunteer-terms-checkbox"
                      checked={volunteerTermsAccepted}
                      onChange={(event) => setVolunteerTermsAccepted(event.currentTarget.checked)}
                      size="md"
                      color="var(--color-burgundy)"
                      data-testid="volunteer-terms-checkbox"
                    />
                    <Text
                      component="label"
                      htmlFor="volunteer-terms-checkbox"
                      size="md"
                      style={{
                        cursor: 'pointer',
                        color: '#000000',
                        fontWeight: 700,
                        lineHeight: 1.5
                      }}
                    >
                      I agree to the{' '}
                      <a
                        href="/event-waiver"
                        target="_blank"
                        rel="noopener noreferrer"
                        style={{
                          color: 'var(--color-burgundy)',
                          textDecoration: 'underline',
                          fontWeight: 700
                        }}
                        onClick={(e) => e.stopPropagation()}
                      >
                        Event Waiver
                      </a>
                    </Text>
                  </Group>
                ) : (
                  <div style={{ flex: 1 }} />
                )}

                <Group gap="sm" wrap="nowrap" style={{ flexShrink: 0 }}>
                  <Button
                    size="sm"
                    variant="subtle"
                    onClick={() => {
                      setShowSignupConfirm(false);
                      setVolunteerTermsAccepted(false);
                    }}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '36px',
                        paddingTop: '8px',
                        paddingBottom: '8px',
                        fontSize: '14px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Cancel
                  </Button>
                  <Button
                    size="sm"
                    color="blue"
                    onClick={() => signupMutation.mutate()}
                    loading={signupMutation.isPending}
                    disabled={needsTermsAcceptance && !volunteerTermsAccepted}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '36px',
                        paddingTop: '8px',
                        paddingBottom: '8px',
                        fontSize: '14px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Confirm
                  </Button>
                </Group>
              </Group>
            </Stack>
          </Alert>
        </Collapse>

        {/* Already Signed Up State */}
        {position.hasUserSignedUp && (
          <Alert
            color="green"
            variant="light"
            /* Hide icon on mobile for cleaner compact layout; show on desktop */
            icon={!isMobile ? <IconCheck size={16} /> : undefined}
            p={8}
            style={isMobile ? {
              marginLeft: 'calc(-1 * var(--space-md))',
              marginRight: 'calc(-1 * var(--space-md))',
              marginBottom: 'calc(-1 * var(--space-md))',
              borderRadius: 0
            } : undefined}
            styles={{
              /* Align the icon vertically with the compact 8px padding */
              icon: { marginRight: 8, alignSelf: 'center' },
              body: { alignItems: 'center' }
            }}
          >
            {isMobile ? (
              /* Mobile: centered text and cancel button stacked vertically */
              <Stack gap={4} align="center">
                <Text size="sm" ta="center">You're already signed up for this position</Text>
                {position.canCancel ? (
                  <Button
                    size="xs"
                    variant="subtle"
                    color="red"
                    onClick={() => cancelMutation.mutate()}
                    loading={cancelMutation.isPending}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '32px',
                        paddingTop: '6px',
                        paddingBottom: '6px',
                        fontSize: '12px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Cancel Signup
                  </Button>
                ) : (
                  <Text size="xs" c="dimmed">Cannot cancel</Text>
                )}
              </Stack>
            ) : (
              /* Desktop: text left, cancel button right */
              <Group justify="space-between" align="center">
                <Text size="sm">You're already signed up for this position</Text>
                {position.canCancel ? (
                  <Button
                    size="xs"
                    variant="subtle"
                    color="red"
                    onClick={() => cancelMutation.mutate()}
                    loading={cancelMutation.isPending}
                    styles={{
                      root: {
                        fontWeight: 600,
                        height: '32px',
                        paddingTop: '6px',
                        paddingBottom: '6px',
                        fontSize: '12px',
                        lineHeight: '1.2'
                      }
                    }}
                  >
                    Cancel Signup
                  </Button>
                ) : (
                  <Text size="xs" c="dimmed">Cannot cancel</Text>
                )}
              </Group>
            )}
          </Alert>
        )}

        {/* Fully Staffed State */}
        {position.isFullyStaffed && !position.hasUserSignedUp && (
          <Alert color="gray" variant="light">
            This volunteer position is currently full
          </Alert>
        )}
      </Stack>
    </Paper>
  );
};
