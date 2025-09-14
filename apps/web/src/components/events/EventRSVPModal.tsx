import React, { useState } from 'react';
import {
  Modal,
  Title,
  Text,
  Stack,
  Group,
  Button,
  Card,
  Divider,
  Alert,
  Box,
  Checkbox,
} from '@mantine/core';
import { IconUsers, IconTicket, IconInfoCircle } from '@tabler/icons-react';

export interface RSVPData {
  eventId: string;
  willAttend: boolean;
  alsoWantsToBuyTicket?: boolean;
}

interface EventInfo {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
}

interface EventRSVPModalProps {
  opened: boolean;
  onClose: () => void;
  onRSVP: (rsvpData: RSVPData) => void;
  onPurchaseTicket?: () => void;
  event: EventInfo;
  isRSVPing?: boolean;
  hasTicketTypes?: boolean;
  currentUserRSVP?: {
    hasRSVP: boolean;
    willAttend: boolean;
    hasPurchasedTicket: boolean;
  };
}

/**
 * EventRSVPModal - Modal for RSVP to Social Events (free option)
 * Only used for Social Events - allows free RSVP with optional ticket purchase
 */
export const EventRSVPModal: React.FC<EventRSVPModalProps> = ({
  opened,
  onClose,
  onRSVP,
  onPurchaseTicket,
  event,
  isRSVPing = false,
  hasTicketTypes = false,
  currentUserRSVP,
}) => {
  const [willAttend, setWillAttend] = useState(true);
  const [alsoWantsToBuyTicket, setAlsoWantsToBuyTicket] = useState(false);

  const handleRSVP = () => {
    onRSVP({
      eventId: event.id,
      willAttend,
      alsoWantsToBuyTicket: hasTicketTypes ? alsoWantsToBuyTicket : undefined,
    });
  };

  const handleClose = () => {
    // Reset form on close
    setWillAttend(true);
    setAlsoWantsToBuyTicket(false);
    onClose();
  };

  const handleTicketPurchase = () => {
    handleClose();
    onPurchaseTicket?.();
  };

  const isUpdatingExistingRSVP = currentUserRSVP?.hasRSVP;

  return (
    <Modal
      opened={opened}
      onClose={handleClose}
      title={
        <Group gap="sm">
          <IconUsers size={24} style={{ color: '#880124' }} />
          <Title order={3}>
            {isUpdatingExistingRSVP ? 'Update RSVP' : 'RSVP for Event'}
          </Title>
        </Group>
      }
      size="md"
      centered
    >
      <Stack gap="lg">
        {/* Event Info */}
        <Card p="md" style={{ background: 'rgba(136, 1, 36, 0.05)', border: '1px solid rgba(136, 1, 36, 0.1)' }}>
          <Title order={4} mb="xs">{event.title}</Title>
          <Text size="sm" c="dimmed">
            {new Date(event.startDate).toLocaleDateString('en-US', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
              hour: 'numeric',
              minute: '2-digit'
            })}
          </Text>
        </Card>

        {/* RSVP Information */}
        <Alert icon={<IconInfoCircle />} color="blue" variant="light">
          <Stack gap="sm">
            <Text size="sm" fw={600}>Free RSVP for Social Events</Text>
            <Text size="sm">
              RSVP is free and helps us plan for the right number of attendees. 
              You can always change your RSVP later if plans change.
            </Text>
            {hasTicketTypes && (
              <Text size="sm">
                Want additional perks? You can also purchase tickets for exclusive benefits!
              </Text>
            )}
          </Stack>
        </Alert>

        {/* Current RSVP Status */}
        {currentUserRSVP?.hasRSVP && (
          <Card p="md" style={{ background: 'rgba(40, 167, 69, 0.1)', border: '1px solid rgba(40, 167, 69, 0.3)' }}>
            <Group gap="sm" mb="xs">
              <IconUsers size={16} color="#28a745" />
              <Text fw={600} size="sm" c="green">Current RSVP Status</Text>
            </Group>
            <Text size="sm">
              You are currently RSVP'd as: {currentUserRSVP.willAttend ? 'Will Attend' : 'Cannot Attend'}
            </Text>
            {currentUserRSVP.hasPurchasedTicket && (
              <Text size="sm" c="green">
                ✓ You also have purchased tickets for this event
              </Text>
            )}
          </Card>
        )}

        {/* RSVP Selection */}
        <Stack gap="md">
          <Title order={5}>Will you attend this event?</Title>
          
          <Stack gap="sm">
            <Card
              p="md"
              style={{
                cursor: 'pointer',
                border: willAttend ? '2px solid #880124' : '1px solid rgba(136, 1, 36, 0.1)',
                background: willAttend ? 'rgba(136, 1, 36, 0.05)' : 'white',
              }}
              onClick={() => setWillAttend(true)}
            >
              <Group gap="sm">
                <input
                  type="radio"
                  checked={willAttend}
                  onChange={() => setWillAttend(true)}
                  style={{ accentColor: '#880124' }}
                />
                <Box>
                  <Text fw={600}>Yes, I'll attend</Text>
                  <Text size="sm" c="dimmed">Count me in for this event</Text>
                </Box>
              </Group>
            </Card>

            <Card
              p="md"
              style={{
                cursor: 'pointer',
                border: !willAttend ? '2px solid #880124' : '1px solid rgba(136, 1, 36, 0.1)',
                background: !willAttend ? 'rgba(136, 1, 36, 0.05)' : 'white',
              }}
              onClick={() => setWillAttend(false)}
            >
              <Group gap="sm">
                <input
                  type="radio"
                  checked={!willAttend}
                  onChange={() => setWillAttend(false)}
                  style={{ accentColor: '#880124' }}
                />
                <Box>
                  <Text fw={600}>No, I can't attend</Text>
                  <Text size="sm" c="dimmed">I won't be able to make it</Text>
                </Box>
              </Group>
            </Card>
          </Stack>
        </Stack>

        {/* Optional Ticket Purchase */}
        {hasTicketTypes && willAttend && (
          <Stack gap="md">
            <Divider />
            
            <Box>
              <Group gap="sm" mb="md">
                <IconTicket size={20} />
                <Text fw={600}>Optional Ticket Purchase</Text>
              </Group>
              
              <Checkbox
                checked={alsoWantsToBuyTicket}
                onChange={(event) => setAlsoWantsToBuyTicket(event.currentTarget.checked)}
                label="I'm also interested in purchasing tickets for additional benefits"
                size="sm"
              />
              
              {alsoWantsToBuyTicket && (
                <Alert color="blue" variant="light" mt="sm">
                  <Text size="sm">
                    Great! After confirming your RSVP, you'll be taken to the ticket purchase page 
                    where you can select from available ticket options.
                  </Text>
                </Alert>
              )}
            </Box>
          </Stack>
        )}

        {/* Alternative Ticket Purchase Option */}
        {hasTicketTypes && !alsoWantsToBuyTicket && (
          <Card p="md" style={{ background: 'rgba(255, 193, 7, 0.1)', border: '1px solid rgba(255, 193, 7, 0.3)' }}>
            <Group justify="space-between" align="center">
              <Box>
                <Text fw={600} size="sm">Want Extra Perks?</Text>
                <Text size="sm" c="dimmed">
                  Consider purchasing tickets for exclusive benefits and priority access.
                </Text>
              </Box>
              <Button
                variant="light"
                color="yellow"
                size="sm"
                onClick={handleTicketPurchase}
                leftSection={<IconTicket size={16} />}
              >
                View Tickets
              </Button>
            </Group>
          </Card>
        )}

        {/* Action Buttons */}
        <Group justify="flex-end" gap="md" mt="lg">
          <Button
            variant="subtle"
            onClick={handleClose}
            disabled={isRSVPing}
          >
            Cancel
          </Button>
          
          <Button
            onClick={handleRSVP}
            loading={isRSVPing}
            style={{
              background: willAttend ? '#880124' : '#6C757D',
              color: 'white',
            }}
            leftSection={<IconUsers size={20} />}
          >
            {isUpdatingExistingRSVP ? 'Update' : 'Confirm'} RSVP
            {willAttend && alsoWantsToBuyTicket && ' & Buy Tickets'}
          </Button>
        </Group>

        {/* Additional Info */}
        <Alert color="gray" variant="light">
          <Text size="xs" c="dimmed">
            Note: This is a free RSVP for community planning purposes. 
            You can change your RSVP anytime before the event.
            {hasTicketTypes && ' Ticket purchases are separate and optional.'}
          </Text>
        </Alert>
      </Stack>
    </Modal>
  );
};