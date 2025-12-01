import { Paper, Group, Text, Badge, Button, Stack, Collapse, Alert, Checkbox } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconClock, IconCheck, IconAlertCircle } from '@tabler/icons-react';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import type { VolunteerPosition } from '../types/volunteer.types';
import { signupForVolunteerPosition, cancelVolunteerSignup } from '../api/volunteerApi';
import { useCurrentUser } from '@/lib/api/hooks/useAuth';

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

  // Debug: Log position data to verify time fields
  console.log('VolunteerPositionCard - position data:', {
    id: position.id,
    title: position.title,
    sessionStartTime: position.sessionStartTime,
    sessionEndTime: position.sessionEndTime,
    sessionName: position.sessionName,
    hasSessionTimes: !!(position.sessionStartTime && position.sessionEndTime)
  });

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
      notifications.show({
        title: 'Signup Failed',
        message: error.response?.data?.error || error.message || 'Failed to sign up for volunteer position',
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
      notifications.show({
        title: 'Cancel Failed',
        message: error.response?.data?.error || error.message || 'Failed to cancel volunteer signup',
        color: 'red',
        icon: <IconAlertCircle size={16} />
      });
    }
  });

  const isAuthenticated = !!currentUser;

  const formatTime = (timeString?: string) => {
    if (!timeString) return '';
    try {
      // Use getUTCHours/getUTCMinutes for user-entered times stored as naive UTC
      const date = new Date(timeString);
      const hours = date.getUTCHours();
      const minutes = date.getUTCMinutes();
      const period = hours >= 12 ? 'pm' : 'am';
      const hour12 = hours % 12 || 12;
      const minuteStr = minutes.toString().padStart(2, '0');
      return `${hour12}:${minuteStr} ${period}`;
    } catch {
      return timeString;
    }
  };

  return (
    <Paper
      style={{
        background: 'white',
        border: '1px solid var(--color-stone-light)',
        borderRadius: '16px',
        padding: 'var(--space-lg)',
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
      <Stack gap="md">
        {/* Header */}
        <Group justify="space-between" align="flex-start">
          <div>
            <Group gap="xs" mb={4} align="baseline">
              {/* Only show session name if event has multiple sessions (hide "Main Session" for single-session events) */}
              {position.sessionName && !position.sessionName.includes('Main Session') && (
                <Text
                  size="lg"
                  fw={700}
                  style={{ color: 'var(--color-text)' }}
                >
                  {position.sessionName}
                </Text>
              )}
              <Text
                size="lg"
                fw={700}
                style={{ color: 'var(--color-text)', marginLeft: position.sessionName && !position.sessionName.includes('Main Session') ? '20px' : '0' }}
              >
                {position.title}
              </Text>
              {position.sessionStartTime && position.sessionEndTime && (
                <Text
                  size="lg"
                  fw={700}
                  style={{ color: 'var(--color-text)', marginLeft: '20px' }}
                >
                  {formatTime(position.sessionStartTime)} - {formatTime(position.sessionEndTime)}
                </Text>
              )}
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
          <Group align="flex-start" wrap="nowrap" gap="md" mb={0}>
            <Text size="sm" c="dimmed" style={{ flex: 1 }}>
              {position.description}
            </Text>

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

            {/* Show signup closed message if signup window has closed */}
            {!position.hasUserSignedUp && !position.isFullyStaffed && !position.canSignUp && (
              <Text size="sm" c="dimmed" style={{ textAlign: 'right', flexShrink: 0 }}>
                Signup closed
              </Text>
            )}
          </Group>

          {/* Inline Signup Confirmation */}
          <Collapse in={showSignupConfirm}>
            <Alert
              color="blue"
              variant="light"
              icon={!isMobile ? <IconAlertCircle size={16} /> : undefined}
              title="Confirm Volunteer Signup"
              style={{ marginTop: 'var(--space-md)' }}
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
        </div>

        {/* Already Signed Up State */}
        {position.hasUserSignedUp && (
          <Alert color="green" variant="light" icon={<IconCheck size={16} />}>
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
