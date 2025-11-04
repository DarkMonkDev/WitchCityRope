// CheckInModal component for CheckIn system
// Confirmation modal for check-in process with attendee details and waiver status
// Source: /docs/design/wireframes/event-checkin-visual.html (lines 444-521)

import React from 'react';
import {
  Modal,
  Stack,
  Text,
  Group,
  Button,
  Box,
  Badge,
  Textarea
} from '@mantine/core';
import type { CheckInAttendee } from '../types/checkin.types';
import { checkInTheme } from '../styles/theme';

interface CheckInModalProps {
  isOpen: boolean;
  onClose: () => void;
  attendee: CheckInAttendee | null;
  onConfirm: (attendee: CheckInAttendee) => void;
}

/**
 * Check-in confirmation modal with attendee details
 * Desktop-focused modal design matching wireframe
 */
export function CheckInModal({
  isOpen,
  onClose,
  attendee,
  onConfirm
}: CheckInModalProps) {
  const [notes, setNotes] = React.useState('');

  if (!attendee) return null;

  const handleConfirm = () => {
    onConfirm(attendee);
    setNotes('');
  };

  const handleClose = () => {
    onClose();
    setNotes('');
  };

  return (
    <Modal
      opened={isOpen}
      onClose={handleClose}
      size="md"
      data-testid="checkin-modal"
      styles={{
        header: {
          background: checkInTheme.gradients.header,
          color: checkInTheme.colors.ivory,
          padding: 24
        },
        title: {
          fontFamily: checkInTheme.fonts.heading,
          fontSize: 24,
          fontWeight: 700,
          letterSpacing: '0.5px',
          color: checkInTheme.colors.ivory
        },
        body: {
          padding: 0
        },
        close: {
          color: checkInTheme.colors.ivory,
          '&:hover': {
            background: 'rgba(255,255,255,0.1)'
          }
        }
      }}
      title="Check In Attendee"
    >
      <Stack gap="lg" p="lg">
        {/* Attendee Info Section */}
        <Box style={{
          background: checkInTheme.colors.ivory,
          border: `1px solid ${checkInTheme.colors.roseGold}`,
          borderRadius: 8,
          padding: 24
        }}>
          <Text
            style={{
              fontFamily: checkInTheme.fonts.heading,
              fontSize: 28,
              fontWeight: 700,
              color: checkInTheme.colors.burgundy,
              marginBottom: 4
            }}
          >
            {attendee.sceneName || attendee.email}
          </Text>
          {attendee.pronouns && (
            <Text size="16px" c="dimmed" fs="italic">
              {attendee.pronouns}
            </Text>
          )}
          {attendee.ticketNumber && (
            <Text
              size="14px"
              c="dimmed"
              style={{
                fontFamily: 'monospace',
                marginTop: 8
              }}
            >
              Ticket: {attendee.ticketNumber}
            </Text>
          )}
        </Box>

        {/* Waiver Section */}
        <Box>
          <Text
            style={{
              fontFamily: checkInTheme.fonts.heading,
              fontSize: 16,
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              marginBottom: 12,
              color: attendee.hasCompletedWaiver ? checkInTheme.colors.charcoal : checkInTheme.colors.error
            }}
          >
            {attendee.hasCompletedWaiver ? 'Waiver Status' : 'Waiver Status - NOT SIGNED'}
          </Text>
          {attendee.hasCompletedWaiver ? (
            <Group gap="sm">
              <Badge
                styles={{
                  root: {
                    background: checkInTheme.colors.successLight,
                    color: checkInTheme.colors.success,
                    border: '1px solid',
                    borderColor: checkInTheme.colors.success,
                    fontFamily: checkInTheme.fonts.heading,
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    fontSize: 14,
                    padding: '8px 16px'
                  }
                }}
              >
                ✓ Signed
              </Badge>
              <Text size="sm" c="dimmed">Waiver on file</Text>
            </Group>
          ) : (
            <Button
              fullWidth
              styles={{
                root: {
                  background: `linear-gradient(135deg, ${checkInTheme.colors.error}, #B71C1C)`,
                  color: 'white',
                  fontFamily: checkInTheme.fonts.heading,
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '1px',
                  fontSize: 14,
                  padding: '14px 20px',
                  height: 'auto',
                  '&:hover': {
                    background: `linear-gradient(135deg, #B71C1C, ${checkInTheme.colors.error})`
                  }
                }
              }}
              onClick={() => {
                // TODO: Implement waiver signing flow
                alert('Waiver signing not yet implemented');
              }}
            >
              ✓ Mark Waiver as Signed
            </Button>
          )}
        </Box>

        {/* Payment Section */}
        <Box>
          <Text
            style={{
              fontFamily: checkInTheme.fonts.heading,
              fontSize: 16,
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              marginBottom: 12,
              color: checkInTheme.colors.charcoal
            }}
          >
            💳 Payment Information
          </Text>
          <Badge
            styles={{
              root: {
                background: attendee.paymentStatus === 'Paid'
                  ? checkInTheme.colors.successLight
                  : checkInTheme.colors.errorLight,
                color: attendee.paymentStatus === 'Paid'
                  ? checkInTheme.colors.success
                  : checkInTheme.colors.error,
                border: '1px solid',
                borderColor: attendee.paymentStatus === 'Paid'
                  ? checkInTheme.colors.success
                  : checkInTheme.colors.error,
                fontFamily: checkInTheme.fonts.heading,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                fontSize: 14,
                padding: '8px 16px'
              }
            }}
          >
            {attendee.paymentStatus || 'Unpaid'}
          </Badge>
        </Box>

        {/* Special Notes Section */}
        {(attendee.dietaryRestrictions || attendee.accessibilityNeeds) && (
          <Box>
            <Text
              style={{
                fontFamily: checkInTheme.fonts.heading,
                fontSize: 16,
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '0.5px',
                marginBottom: 12,
                color: checkInTheme.colors.charcoal
              }}
            >
              ⚠️ Important Notes
            </Text>
            <Stack gap="xs">
              {attendee.dietaryRestrictions && (
                <Box
                  style={{
                    background: checkInTheme.colors.warningLight,
                    padding: 12,
                    borderRadius: 6,
                    border: `1px solid ${checkInTheme.colors.warning}`
                  }}
                >
                  <Text size="sm" fw={600}>Dietary Restrictions:</Text>
                  <Text size="sm">{attendee.dietaryRestrictions}</Text>
                </Box>
              )}
              {attendee.accessibilityNeeds && (
                <Box
                  style={{
                    background: checkInTheme.colors.infoLight,
                    padding: 12,
                    borderRadius: 6,
                    border: `1px solid ${checkInTheme.colors.info}`
                  }}
                >
                  <Text size="sm" fw={600}>Accessibility Needs:</Text>
                  <Text size="sm">{attendee.accessibilityNeeds}</Text>
                </Box>
              )}
            </Stack>
          </Box>
        )}

        {/* Notes Section */}
        <Box>
          <Text
            style={{
              fontFamily: checkInTheme.fonts.heading,
              fontSize: 16,
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              marginBottom: 12,
              color: checkInTheme.colors.charcoal
            }}
          >
            📝 Notes (Optional)
          </Text>
          <Textarea
            placeholder="Add any notes about this check-in..."
            rows={3}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            styles={{
              input: {
                border: `1px solid ${checkInTheme.colors.grayLight}`,
                borderRadius: 8,
                fontSize: 14
              }
            }}
          />
        </Box>
      </Stack>

      {/* Footer */}
      <Group
        justify="space-between"
        p="lg"
        style={{
          borderTop: '1px solid #CCCCCC',
          background: checkInTheme.colors.ivory
        }}
      >
        <Button
          variant="outline"
          onClick={handleClose}
          styles={{
            root: {
              border: `2px solid ${checkInTheme.colors.burgundy}`,
              color: checkInTheme.colors.burgundy,
              fontFamily: checkInTheme.fonts.heading,
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.5px',
              fontSize: 14,
              padding: '12px 24px',
              height: 'auto',
              '&:hover': {
                background: checkInTheme.colors.burgundy,
                color: 'white'
              }
            }
          }}
        >
          Cancel
        </Button>
        <Button
          onClick={handleConfirm}
          style={{ flex: 1, maxWidth: '60%' }}
          styles={{
            root: {
              background: checkInTheme.gradients.amber,
              color: checkInTheme.colors.midnight,
              fontFamily: checkInTheme.fonts.heading,
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '1.5px',
              fontSize: 16,
              boxShadow: '0 4px 15px rgba(255, 191, 0, 0.4)',
              height: 'auto',
              padding: '14px 32px',
              '&:hover': {
                boxShadow: '0 6px 20px rgba(255, 191, 0, 0.5)',
                transform: 'translateY(-2px)'
              }
            }
          }}
        >
          Complete Check-in
        </Button>
      </Group>
    </Modal>
  );
}
