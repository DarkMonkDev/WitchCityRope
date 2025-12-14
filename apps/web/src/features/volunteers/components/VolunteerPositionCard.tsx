import { Paper, Group, Text, Badge, Button, Stack, Collapse, Alert, Checkbox } from '@mantine/core';
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
      return await signupForVolunteerPosition(position.id, {
        // If user has existing participation, they've already accepted the waiver
        // Otherwise, use the checkbox state
        eventWaiverAccepted: !needsTermsAcceptance || volunteerTermsAccepted
      });
    },
    onSuccess: (response) => {
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
      style={{
        background: 'white',
        border: '1px solid var(--color-stone-light)',
        borderRadius: '16px',
        padding: 'var(--space-md)',
        transition: 'all 0.3s ease'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.boxShadow = '0 4px 12px rgba(0,0,0,0.08)';
        e.currentTarget.style.transform = 'translateY(-2px)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.boxShadow = 'none';
        e.currentTarget.style.transform = 'translateY(0)';
      }}
    >
      <Stack gap={4}>
        {/* Header - Different layout for mobile vs desktop */}
        {isMobile ? (
          /* Mobile Layout: Title, Date, Description, then Badge+Button row */
          <>
            {/* Title and Signed Up badge */}
            <Group gap="xs" mb={0} align="baseline">
              <h4 style={{ marginBlockStart: 0, marginBlockEnd: 0 }}>
                {position.title}
                {position.sessionName && !position.sessionName.includes('Main Session') && (
                  <> - {position.sessionName}</>
                )}
              </h4>
              {position.hasUserSignedUp && (
                <Badge color="green" variant="light" size="sm">
                  Signed Up
                </Badge>
              )}
            </Group>

            {/* Date and time */}
            {position.sessionStartTime && (
              <Text size="sm" c="dimmed">
                {formatUtcToLocalDate(position.sessionStartTime, eventTimeZone, { weekday: 'long', month: 'short', day: 'numeric' })}
                {(position.startTime || position.endTime) ? (
                  <> · {formatShiftTime(position.startTime)} - {formatShiftTime(position.endTime)}</>
                ) : position.sessionEndTime ? (
                  <> · {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime)}</>
                ) : null}
              </Text>
            )}

            {/* Description */}
            <p style={{ marginBlockStart: '8px', marginBlockEnd: '8px' }}>
              {position.description}
            </p>

            {/* Badge and Button row - vertically centered */}
            <Group justify="space-between" align="center">
              {/* Hide spots filled badge on mobile when user has signed up */}
              {!position.hasUserSignedUp && (
                <Badge color="blue" variant="light" size="lg">
                  ({position.slotsFilled} / {position.slotsNeeded} spots filled)
                </Badge>
              )}

              {/* Show Sign Up button only if user hasn't signed up, position isn't full, AND signup window is open */}
              {!position.hasUserSignedUp && !position.isFullyStaffed && isAuthenticated && position.canSignUp && (
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
                      lineHeight: '1.2',
                      flexShrink: 0
                    }
                  }}
                >
                  Sign Up
                </Button>
              )}

              {!position.hasUserSignedUp && !position.isFullyStaffed && !isAuthenticated && position.canSignUp && (
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
                      lineHeight: '1.2',
                      flexShrink: 0
                    }
                  }}
                >
                  Login to Volunteer
                </Button>
              )}

              {/* Show appropriate message when signup is blocked */}
              {!position.hasUserSignedUp && !position.isFullyStaffed && !position.canSignUp && (
                <Text size="sm" c="dimmed" style={{ textAlign: 'right', flexShrink: 0 }}>
                  {position.signupBlockedReason === 'NoTicketForSession'
                    ? 'Purchase a ticket to volunteer'
                    : 'Signup closed'}
                </Text>
              )}
            </Group>
          </>
        ) : (
          /* Desktop Layout: Original layout */
          <>
            <Group justify="space-between" align="flex-start">
              <div>
                {/* Title and Signed Up badge on first line */}
                <Group gap="xs" mb={0} align="baseline">
                  <h4 style={{ marginBlockStart: 0 }}>
                    {position.title}
                    {/* Only show session name if event has multiple sessions (hide "Main Session" for single-session events) */}
                    {position.sessionName && !position.sessionName.includes('Main Session') && (
                      <> - {position.sessionName}</>
                    )}
                  </h4>
                  {position.hasUserSignedUp && (
                    <Badge
                      color="green"
                      variant="light"
                      size="sm"
                    >
                      Signed Up
                    </Badge>
                  )}
                </Group>
                {/* Date and time on second line - use position shift times if available, otherwise session times */}
                {position.sessionStartTime && (
                  <Text size="sm" c="dimmed">
                    {formatUtcToLocalDate(position.sessionStartTime, eventTimeZone, { weekday: 'long', month: 'short', day: 'numeric' })}
                    {(position.startTime || position.endTime) ? (
                      <> · {formatShiftTime(position.startTime)} - {formatShiftTime(position.endTime)}</>
                    ) : position.sessionEndTime ? (
                      <> · {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime)}</>
                    ) : null}
                  </Text>
                )}
              </div>

              {/* Badge showing spots filled - consistent color */}
              <Badge
                color="blue"
                variant="light"
                size="lg"
              >
                ({position.slotsFilled} / {position.slotsNeeded} spots filled)
              </Badge>
            </Group>

            {/* Description, Sign Up Button, and Confirmation - grouped together */}
            <div>
              <Group align="center" wrap="nowrap" gap="md" mb={0}>
                <p style={{ flex: 1 }}>
                  {position.description}
                </p>

                {/* Show Sign Up button only if user hasn't signed up, position isn't full, AND signup window is open */}
                {!position.hasUserSignedUp && !position.isFullyStaffed && isAuthenticated && position.canSignUp && (
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
                        lineHeight: '1.2',
                        flexShrink: 0
                      }
                    }}
                  >
                    Sign Up
                  </Button>
                )}

                {!position.hasUserSignedUp && !position.isFullyStaffed && !isAuthenticated && position.canSignUp && (
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
                        lineHeight: '1.2',
                        flexShrink: 0
                      }
                    }}
                  >
                    Login to Volunteer
                  </Button>
                )}

                {/* Show appropriate message when signup is blocked */}
                {!position.hasUserSignedUp && !position.isFullyStaffed && !position.canSignUp && (
                  <Text size="sm" c="dimmed" style={{ textAlign: 'right', flexShrink: 0 }}>
                    {position.signupBlockedReason === 'NoTicketForSession'
                      ? 'Purchase a ticket to volunteer'
                      : 'Signup closed'}
                  </Text>
                )}
              </Group>
            </div>
          </>
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
            icon={<IconCheck size={16} />}
            style={isMobile ? {
              marginLeft: 'calc(-1 * var(--space-md))',
              marginRight: 'calc(-1 * var(--space-md))',
              marginBottom: 'calc(-1 * var(--space-md))',
              borderRadius: 0
            } : undefined}
          >
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
