// CheckInConfirmation component for CheckIn system
// Large visual confirmation with attendee details and success animation

import React, { useEffect, useState } from 'react';
import {
  Modal,
  Stack,
  Text,
  Group,
  Button,
  Card,
  Badge,
  Box,
  Divider,
  Center,
  Progress,
  Alert
} from '@mantine/core';
import { IconCheck, IconUser, IconClock, IconNotes } from '@tabler/icons-react';
import type { CheckInAttendee, CheckInResponse } from '../types/checkin.types';
import { TOUCH_TARGETS } from '../types/checkin.types';

interface CheckInConfirmationProps {
  isOpen: boolean;
  onClose: () => void;
  attendee: CheckInAttendee | null;
  checkInResponse: CheckInResponse | null;
  onContinue: () => void;
  onNewCheckIn: () => void;
  autoCloseDelay?: number; // seconds before auto-close
}

/**
 * Success confirmation modal with large visual feedback
 * Optimized for quick mobile interaction at events
 */
export function CheckInConfirmation({
  isOpen,
  onClose,
  attendee,
  checkInResponse,
  onContinue,
  onNewCheckIn,
  autoCloseDelay = 5
}: CheckInConfirmationProps) {
  const [timeLeft, setTimeLeft] = useState(autoCloseDelay);
  const [showAutoClose, setShowAutoClose] = useState(false);

  // Auto-close countdown
  useEffect(() => {
    if (!isOpen || !checkInResponse?.success) {
      setTimeLeft(autoCloseDelay);
      setShowAutoClose(false);
      return;
    }

    // Show countdown after 2 seconds
    const showTimer = setTimeout(() => {
      setShowAutoClose(true);
    }, 2000);

    // Start countdown
    const timer = setInterval(() => {
      setTimeLeft(prev => {
        if (prev <= 1) {
          onContinue();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      clearTimeout(showTimer);
      clearInterval(timer);
    };
  }, [isOpen, checkInResponse?.success, autoCloseDelay, onContinue]);

  // Stop auto-close if user interacts
  const handleUserInteraction = () => {
    setShowAutoClose(false);
    setTimeLeft(autoCloseDelay);
  };

  if (!attendee || !checkInResponse) return null;

  const checkInTime = new Date(checkInResponse.checkInTime);
  const capacity = checkInResponse.currentCapacity;

  return (
    <Modal
      opened={isOpen}
      onClose={onClose}
      size="md"
      centered
      withCloseButton={false}
      overlayProps={{
        opacity: 0.7,
        blur: 3
      }}
      styles={{
        body: { padding: '2rem' }
      }}
    >
      <Stack gap="lg" align="center">
        {checkInResponse.success ? (
          <>
            {/* Success Animation */}
            <Box
              style={{
                width: '120px',
                height: '120px',
                borderRadius: '50%',
                backgroundColor: '#228B22',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                animation: 'pulse 2s ease-in-out',
                boxShadow: '0 0 20px rgba(34, 139, 34, 0.3)'
              }}
            >
              <IconCheck 
                size={60} 
                color="white" 
                stroke={3}
              />
            </Box>

            {/* Success Message */}
            <Stack align="center" gap="xs">
              <Text 
                size="xl" 
                fw={700}
                ta="center"
                style={{
                  fontFamily: 'Bodoni Moda, serif',
                  color: '#228B22'
                }}
              >
                CHECK-IN SUCCESSFUL
              </Text>
              <Text size="sm" c="dimmed" ta="center">
                {checkInResponse.message}
              </Text>
            </Stack>

            {/* Attendee Info Card */}
            <Card 
              w="100%" 
              padding="lg" 
              radius="md"
              style={{
                backgroundColor: '#f8f9fa',
                border: '2px solid #228B22'
              }}
            >
              <Stack gap="md">
                <Group justify="space-between" align="center">
                  <Group align="center" gap="xs">
                    <IconUser size={20} />
                    <Text fw={600} size="lg">
                      {attendee.sceneName}
                    </Text>
                  </Group>
                  
                  {attendee.isFirstTime && (
                    <Badge color="blue" variant="filled">
                      First Time Welcome!
                    </Badge>
                  )}
                </Group>

                <Text size="sm" c="dimmed">
                  {attendee.email}
                </Text>

                <Divider />

                <Group justify="space-between">
                  <Group align="center" gap="xs">
                    <IconClock size={16} />
                    <Text size="sm" fw={500}>
                      Check-in Time:
                    </Text>
                  </Group>
                  <Text size="sm" fw={600}>
                    {checkInTime.toLocaleTimeString()}
                  </Text>
                </Group>

                {/* Important Notes */}
                {(attendee.dietaryRestrictions || attendee.accessibilityNeeds) && (
                  <>
                    <Divider />
                    <Group align="flex-start" gap="xs">
                      <IconNotes size={16} />
                      <Stack gap="xs" style={{ flex: 1 }}>
                        <Text size="sm" fw={500}>Important Notes:</Text>
                        {attendee.dietaryRestrictions && (
                          <Text size="xs" c="orange">
                            🍽️ Dietary: {attendee.dietaryRestrictions}
                          </Text>
                        )}
                        {attendee.accessibilityNeeds && (
                          <Text size="xs" c="blue">
                            ♿ Accessibility: {attendee.accessibilityNeeds}
                          </Text>
                        )}
                      </Stack>
                    </Group>
                  </>
                )}
              </Stack>
            </Card>

            {/* Capacity Info */}
            {capacity && (
              <Card w="100%" padding="md" radius="md">
                <Stack gap="sm">
                  <Group justify="space-between">
                    <Text size="sm" fw={500}>Event Capacity</Text>
                    <Text size="sm" fw={600}>
                      {capacity.checkedInCount} / {capacity.totalCapacity}
                    </Text>
                  </Group>
                  
                  <Progress 
                    value={(capacity.checkedInCount / capacity.totalCapacity) * 100}
                    color={capacity.isAtCapacity ? "red" : "green"}
                    size="lg"
                    radius="md"
                  />
                  
                  <Group justify="space-between">
                    <Text size="xs" c="dimmed">
                      Available: {capacity.availableSpots}
                    </Text>
                    {capacity.waitlistCount > 0 && (
                      <Text size="xs" c="orange">
                        Waitlist: {capacity.waitlistCount}
                      </Text>
                    )}
                  </Group>
                </Stack>
              </Card>
            )}

            {/* Auto-close countdown */}
            {showAutoClose && (
              <Alert 
                color="blue" 
                variant="light"
                style={{ width: '100%' }}
              >
                <Center>
                  <Text size="sm">
                    Auto-returning to list in {timeLeft} seconds
                  </Text>
                </Center>
              </Alert>
            )}

            {/* Action Buttons */}
            <Group justify="center" gap="md" w="100%">
              <Button
                size="lg"
                variant="outline"
                onClick={() => {
                  handleUserInteraction();
                  onNewCheckIn();
                }}
                style={{
                  minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                  minWidth: '120px',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600
                }}
              >
                New Check-In
              </Button>

              <Button
                size="lg"
                variant="filled"
                color="wcr.7"
                onClick={() => {
                  handleUserInteraction();
                  onContinue();
                }}
                style={{
                  minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                  minWidth: '120px',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                  background: 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)'
                }}
              >
                Continue
              </Button>
            </Group>
          </>
        ) : (
          // Error State
          <>
            <Box
              style={{
                width: '120px',
                height: '120px',
                borderRadius: '50%',
                backgroundColor: '#DC143C',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                boxShadow: '0 0 20px rgba(220, 20, 60, 0.3)'
              }}
            >
              <Text size="3rem">❌</Text>
            </Box>

            <Stack align="center" gap="xs">
              <Text 
                size="xl" 
                fw={700}
                ta="center"
                style={{
                  fontFamily: 'Bodoni Moda, serif',
                  color: '#DC143C'
                }}
              >
                CHECK-IN FAILED
              </Text>
              <Text size="sm" c="dimmed" ta="center">
                {checkInResponse.message}
              </Text>
            </Stack>

            <Group justify="center" gap="md" w="100%">
              <Button
                size="lg"
                variant="filled"
                color="red"
                onClick={onClose}
                style={{
                  minHeight: TOUCH_TARGETS.BUTTON_HEIGHT,
                  minWidth: '120px',
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600
                }}
              >
                Try Again
              </Button>
            </Group>
          </>
        )}
      </Stack>

      <style>{`
        @keyframes pulse {
          0% { transform: scale(1); }
          50% { transform: scale(1.1); }
          100% { transform: scale(1); }
        }
      `}</style>
    </Modal>
  );
}