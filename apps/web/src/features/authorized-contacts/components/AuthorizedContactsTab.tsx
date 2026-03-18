/**
 * AuthorizedContactsTab - Main tab content for the Authorized Contacts feature
 *
 * This is the 4th tab on the Profile Settings page. It combines:
 * 1. DelegateList - "People who can act on your behalf" (editable)
 * 2. PrincipalList - "People you can act for" (read-only)
 *
 * Layout follows the existing ProfileSettingsPage styling:
 * - White background with 12px border-radius and taupe border
 * - Burgundy heading font
 * - Consistent spacing with other tabs
 *
 * The "Add Contact" button uses progressive disclosure - the search input
 * is hidden until the user clicks "Add Contact".
 */
import React, { useState } from 'react'
import {
  Box,
  Stack,
  Text,
  Button,
  Divider,
  Loader,
  Center,
  Alert,
} from '@mantine/core'
import { IconAlertCircle, IconPlus, IconX } from '@tabler/icons-react'
import { useAuthorizedContacts } from '../api/queries'
import { useCurrentUser } from '../../auth/api/queries'
import { DelegateList } from './DelegateList'
import { PrincipalList } from './PrincipalList'
import { ContactSearchInput } from './ContactSearchInput'

export const AuthorizedContactsTab: React.FC = () => {
  // Progressive disclosure: search input is hidden until user clicks "Add Contact"
  const [showSearch, setShowSearch] = useState(false)

  // Fetch authorized contacts (both delegates and principals)
  const { data: contacts, isLoading, error } = useAuthorizedContacts()

  // Need current user's scene name for self-authorization validation
  const { data: currentUser } = useCurrentUser()

  // Loading state
  if (isLoading) {
    return (
      <Box
        p="md"
        style={{
          background: 'white',
          borderRadius: '12px',
          border: '1px solid var(--color-taupe)',
        }}
      >
        <Center py="xl">
          <Stack align="center" gap="md">
            <Loader size="md" color="burgundy" />
            <Text size="sm" c="dimmed">
              Loading authorized contacts...
            </Text>
          </Stack>
        </Center>
      </Box>
    )
  }

  // Error state
  if (error) {
    return (
      <Box
        p="md"
        style={{
          background: 'white',
          borderRadius: '12px',
          border: '1px solid var(--color-taupe)',
        }}
      >
        <Alert icon={<IconAlertCircle />} color="red" title="Error Loading Contacts">
          <Text size="sm">
            {error instanceof Error
              ? error.message
              : 'Failed to load authorized contacts. Please try again.'}
          </Text>
        </Alert>
      </Box>
    )
  }

  const delegates = contacts?.delegates ?? []
  const principals = contacts?.principals ?? []

  // Collect existing delegate scene names for duplicate validation
  const existingDelegateSceneNames = delegates.map((d) => d.sceneName ?? '')

  return (
    <Box
      p="md"
      style={{
        background: 'white',
        borderRadius: '12px',
        border: '1px solid var(--color-taupe)',
      }}
    >
      <Stack gap="md">
        {/* Section 1: People who can act on your behalf (delegates) */}
        <Box>
          <Text
            fw={600}
            size="lg"
            mb="xs"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
            }}
          >
            People who can act on your behalf
          </Text>
          <Text size="sm" c="dimmed" mb="md">
            These people can purchase event tickets and RSVP on your behalf.
            They cannot accept event waivers for you &mdash; you will always
            need to personally accept before attending.
          </Text>

          {/* Delegate list with remove buttons */}
          <DelegateList delegates={delegates} />

          {/* Add Contact button / search input (progressive disclosure) */}
          <Box mt="md">
            {showSearch ? (
              <Stack gap="xs">
                <ContactSearchInput
                  onContactAdded={() => setShowSearch(false)}
                  existingDelegateSceneNames={existingDelegateSceneNames}
                  currentUserSceneName={currentUser?.sceneName}
                />
                <Button
                  variant="subtle"
                  color="dimmed"
                  size="compact-sm"
                  leftSection={<IconX size={14} />}
                  onClick={() => setShowSearch(false)}
                  data-testid="cancel-add-contact"
                >
                  Cancel
                </Button>
              </Stack>
            ) : (
              <Button
                variant="outline"
                color="burgundy"
                leftSection={<IconPlus size={16} />}
                onClick={() => setShowSearch(true)}
                styles={{
                  root: {
                    borderRadius: '12px 6px 12px 6px',
                    fontFamily: 'var(--font-heading)',
                    fontWeight: 600,
                    textTransform: 'uppercase',
                    height: 'auto',
                    paddingTop: '10px',
                    paddingBottom: '10px',
                    paddingLeft: '20px',
                    paddingRight: '20px',
                    lineHeight: '1.2',
                    display: 'flex',
                    alignItems: 'center',
                  },
                }}
                data-testid="add-contact-button"
              >
                Add Contact
              </Button>
            )}
          </Box>
        </Box>

        {/* Divider between sections */}
        <Divider my="lg" />

        {/* Section 2: People you can act for (principals) */}
        <Box>
          <Text
            fw={600}
            size="lg"
            mb="xs"
            style={{
              fontFamily: 'var(--font-heading)',
              color: 'var(--color-burgundy)',
            }}
          >
            People you can act for
          </Text>
          <Text size="sm" c="dimmed" mb="md">
            These people have authorized you to purchase tickets and RSVP
            on their behalf.
          </Text>

          {/* Principal list (read-only) */}
          <PrincipalList principals={principals} />
        </Box>
      </Stack>
    </Box>
  )
}
