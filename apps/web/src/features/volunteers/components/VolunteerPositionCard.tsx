import { Paper, Group, Text, Badge, Button, Stack, Collapse, Alert, Checkbox } from '@mantine/core';
import { IconClock, IconCheck, IconAlertCircle } from '@tabler/icons-react';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notifications } from '@mantine/notifications';
import type { VolunteerPosition } from '../types/volunteer.types';
import { signupForVolunteerPosition } from '../api/volunteerApi';
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
        eventWaiverAccepted: volunteerTermsAccepted
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

  const isAuthenticated = !!currentUser;

  const formatTime = (timeString?: string) => {
    if (!timeString) return '';
    try {
      const date = new Date(timeString);
      const formatted = date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
      // Make AM/PM lowercase
      return formatted.replace(/AM|PM/g, match => match.toLowerCase());
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

            {!position.hasUserSignedUp && !position.isFullyStaffed && isAuthenticated && (
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

            {!position.hasUserSignedUp && !position.isFullyStaffed && !isAuthenticated && (
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
          </Group>

          {/* Inline Signup Confirmation */}
          <Collapse in={showSignupConfirm}>
            <Alert
              color="blue"
              variant="light"
              icon={<IconAlertCircle size={16} />}
              title="Confirm Volunteer Signup"
              style={{ marginTop: 'var(--space-md)' }}
            >
            <Stack gap="sm">
              <Text size="sm">
                Signing up for this volunteer position will automatically RSVP you to the event if you haven't already.
              </Text>

              <Group gap="md" justify="space-between" align="center" wrap="nowrap">
                {/* Terms of Service Acceptance - only show if user doesn't have existing RSVP/ticket */}
                {needsTermsAcceptance ? (
                  <Group gap="sm" align="center" style={{ flex: 1 }}>
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

                <Group gap="sm">
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
            You're already signed up for this position
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
