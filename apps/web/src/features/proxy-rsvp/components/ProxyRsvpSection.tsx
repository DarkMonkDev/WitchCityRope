/**
 * ProxyRsvpSection
 *
 * Section that appears below the existing RSVP area on the event detail page.
 * Allows a delegate to create an RSVP on behalf of one of their principal contacts.
 *
 * Design reference: ui-design.md Screen 3 - Event Detail Page - Proxy RSVP Section
 *
 * Visibility rules:
 * - Hidden if user is not logged in
 * - Hidden if user has zero eligible authorized contacts for this event
 * - Hidden if event does not allow RSVPs
 * - Visible if user has at least one eligible principal contact
 */

import React, { useState } from 'react';
import {
  Paper,
  Stack,
  Text,
  Select,
  Button,
  Modal,
  Group,
  Divider,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { usePrincipalContacts } from '../../authorized-contacts/api/queries';
import { useCreateProxyRsvp } from '../api/mutations';

interface ProxyRsvpSectionProps {
  /** Event ID */
  eventId: string;
  /** Event title (for confirmation modal) */
  eventTitle: string;
  /** Whether the event allows RSVPs */
  allowRsvps: boolean;
  /** Whether the user is authenticated */
  isAuthenticated: boolean;
  /** Whether the event is restricted to vetted members only */
  vettedMembersOnly?: boolean;
  /** Whether the current user is vetted */
  isVetted?: boolean;
}

export const ProxyRsvpSection: React.FC<ProxyRsvpSectionProps> = ({
  eventId,
  eventTitle,
  allowRsvps,
  isAuthenticated,
  vettedMembersOnly = false,
  isVetted = false,
}) => {
  const [selectedContactId, setSelectedContactId] = useState<string | null>(null);
  const [confirmModalOpen, setConfirmModalOpen] = useState(false);

  // Fetch principal contacts filtered by event eligibility
  const { data: contacts = [], isLoading: contactsLoading } = usePrincipalContacts(
    isAuthenticated ? eventId : undefined
  );

  const createProxyRsvp = useCreateProxyRsvp(eventId);

  // DEBUG: Log props to verify they're arriving correctly
  console.log('[ProxyRsvpSection] Props:', { isAuthenticated, allowRsvps, vettedMembersOnly, isVetted });

  // Don't render if conditions aren't met.
  // Non-vetted users cannot create proxy RSVPs for vetted-only events (BR-035).
  if (!isAuthenticated || !allowRsvps || (vettedMembersOnly && !isVetted)) {
    console.log('[ProxyRsvpSection] Hidden: vettedMembersOnly=' + vettedMembersOnly + ', isVetted=' + isVetted);
    return null;
  }

  // Don't render while loading or if no eligible contacts
  if (contactsLoading || contacts.length === 0) {
    return null;
  }

  // Find the selected contact for display in the modal
  const selectedContact = contacts.find((c) => c.userId === selectedContactId);

  const handleConfirmRsvp = async () => {
    if (!selectedContactId) return;

    try {
      await createProxyRsvp.mutateAsync({
        rsvpForUserId: selectedContactId,
      });

      notifications.show({
        title: 'RSVP Created',
        message: `RSVP created for ${selectedContact?.sceneName}. They'll receive an email to accept.`,
        color: 'green',
        autoClose: 8000,
      });

      // Clear selection and close modal
      setSelectedContactId(null);
      setConfirmModalOpen(false);
    } catch {
      // Error notification is handled by the mutation hook's onError
      setConfirmModalOpen(false);
    }
  };

  const dropdownData = contacts.map((contact) => ({
    value: contact.userId ?? '',
    label: contact.sceneName ?? '',
  }));

  return (
    <>
      <Divider my="md" />

      <Paper
        p="md"
        radius="md"
        style={{
          background: 'var(--mantine-color-gray-0)',
          border: '1px solid var(--mantine-color-gray-3)',
        }}
      >
        <Stack gap="md">
          <Text
            fw={600}
            size="md"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
            }}
          >
            RSVP for someone else
          </Text>

          <Select
            placeholder="Select authorized contact..."
            data={dropdownData}
            value={selectedContactId}
            onChange={setSelectedContactId}
            searchable={contacts.length > 5}
            clearable
            disabled={createProxyRsvp.isPending}
            aria-label="Select a person to RSVP for"
          />

          <Button
            color="#880124"
            disabled={!selectedContactId || createProxyRsvp.isPending}
            loading={createProxyRsvp.isPending}
            onClick={() => setConfirmModalOpen(true)}
            styles={{
              root: {
                borderRadius: '12px 6px 12px 6px',
                fontFamily: 'Montserrat, sans-serif',
                fontWeight: 600,
              },
            }}
          >
            {selectedContact
              ? `RSVP for ${selectedContact.sceneName}`
              : 'Select a person first'}
          </Button>

          <Text size="xs" c="dimmed">
            Only people who have authorized you appear here.
          </Text>
        </Stack>
      </Paper>

      {/* Confirmation Modal */}
      <Modal
        opened={confirmModalOpen}
        onClose={() => setConfirmModalOpen(false)}
        title={`RSVP for ${selectedContact?.sceneName}?`}
        centered
        styles={{
          title: {
            fontFamily: 'var(--font-heading)',
            fontWeight: 700,
            color: 'var(--color-burgundy)',
          },
        }}
      >
        <Stack gap="md">
          <Text size="sm">
            This will create an RSVP for {selectedContact?.sceneName} to attend{' '}
            <Text span fw={600}>
              {eventTitle}
            </Text>
            .
          </Text>

          <Text size="sm" c="dimmed">
            They will need to personally accept the event waiver before their RSVP
            is confirmed.
          </Text>

          <Group justify="flex-end" gap="sm" mt="md">
            <Button
              variant="outline"
              color="gray"
              onClick={() => setConfirmModalOpen(false)}
              disabled={createProxyRsvp.isPending}
            >
              Cancel
            </Button>
            <Button
              color="#880124"
              onClick={handleConfirmRsvp}
              loading={createProxyRsvp.isPending}
              styles={{
                root: {
                  borderRadius: '12px 6px 12px 6px',
                  fontFamily: 'Montserrat, sans-serif',
                  fontWeight: 600,
                },
              }}
            >
              Confirm RSVP
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
};

ProxyRsvpSection.displayName = 'ProxyRsvpSection';
