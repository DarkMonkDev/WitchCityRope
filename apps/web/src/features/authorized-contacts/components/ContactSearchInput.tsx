/**
 * ContactSearchInput - Live search for adding authorized contacts
 *
 * Progressive disclosure pattern: this component is shown/hidden by
 * the parent when the user clicks "Add Contact". It provides a debounced
 * autocomplete search that shows scene names only (AD-009 privacy).
 *
 * Search fires after 2+ characters with a 300ms debounce.
 * On selecting a result, triggers the add mutation and clears/collapses.
 */
import React, { useState, useCallback, useRef, useEffect } from 'react'
import {
  Autocomplete,
  Loader,
  Text,
  Box,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { notifications } from '@mantine/notifications'
import { IconCheck, IconAlertCircle } from '@tabler/icons-react'
import { useContactSearch } from '../api/queries'
import { useAddAuthorizedContact } from '../api/mutations'
import type { UserSearchResultDto } from '../types/authorizedContact.types'

interface ContactSearchInputProps {
  /** Called after a contact is successfully added, so parent can collapse this input */
  onContactAdded: () => void
  /** Scene names of already-authorized delegates, to show "already authorized" error */
  existingDelegateSceneNames: string[]
  /** The current user's scene name, to prevent self-authorization */
  currentUserSceneName?: string
}

export const ContactSearchInput: React.FC<ContactSearchInputProps> = ({
  onContactAdded,
  existingDelegateSceneNames,
  currentUserSceneName,
}) => {
  const [searchValue, setSearchValue] = useState('')
  const [debouncedSearch] = useDebouncedValue(searchValue, 300)
  const [inlineError, setInlineError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  // Track the scene name of the last-submitted contact for notifications
  const pendingSceneNameRef = useRef<string>('')

  // Query for search results (only fires when debouncedSearch >= 2 chars)
  const { data: searchResults, isLoading: isSearching } = useContactSearch(debouncedSearch)

  // Mutation to add a contact
  const addContactMutation = useAddAuthorizedContact()

  // Auto-focus the input when mounted (progressive disclosure)
  useEffect(() => {
    inputRef.current?.focus()
  }, [])

  // Clear inline error when search value changes
  useEffect(() => {
    if (inlineError) {
      setInlineError(null)
    }
    // Only clear on searchValue change, not on inlineError change
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchValue])

  // Handle mutation success - clear search and collapse
  useEffect(() => {
    if (addContactMutation.isSuccess) {
      const sceneName = pendingSceneNameRef.current
      notifications.show({
        title: 'Contact Added',
        message: `${sceneName} added as authorized contact`,
        color: 'green',
        icon: <IconCheck />,
      })
      setSearchValue('')
      onContactAdded()
    }
  }, [addContactMutation.isSuccess, onContactAdded])

  // Handle mutation error - show inline error and notification
  useEffect(() => {
    if (addContactMutation.isError) {
      const errorMessage =
        addContactMutation.error instanceof Error
          ? addContactMutation.error.message
          : 'Failed to add contact'
      setInlineError(errorMessage)
      notifications.show({
        title: 'Error',
        message: errorMessage,
        color: 'red',
        icon: <IconAlertCircle />,
      })
    }
  }, [addContactMutation.isError, addContactMutation.error])

  // Map search results to Autocomplete data format
  // Store the full result objects for lookup on selection
  const searchResultsMap = useRef<Map<string, UserSearchResultDto>>(new Map())

  const autocompleteData = React.useMemo(() => {
    if (!searchResults || searchResults.length === 0) return []

    searchResultsMap.current.clear()
    return searchResults.map((result) => {
      searchResultsMap.current.set(result.sceneName, result)
      return result.sceneName
    })
  }, [searchResults])

  /**
   * Handle selection of a search result.
   * Validates against self-authorization and duplicate contacts before calling mutation.
   */
  const handleSelect = useCallback(
    (selectedSceneName: string) => {
      const selectedUser = searchResultsMap.current.get(selectedSceneName)
      if (!selectedUser) return

      // Validate: cannot authorize yourself
      if (
        currentUserSceneName &&
        selectedSceneName.toLowerCase() === currentUserSceneName.toLowerCase()
      ) {
        setInlineError('You cannot authorize yourself')
        return
      }

      // Validate: cannot add a duplicate
      if (
        existingDelegateSceneNames.some(
          (name) => name.toLowerCase() === selectedSceneName.toLowerCase()
        )
      ) {
        setInlineError(`${selectedSceneName} is already an authorized contact`)
        return
      }

      // Clear any previous error, track scene name for notification, and submit
      setInlineError(null)
      pendingSceneNameRef.current = selectedSceneName
      addContactMutation.mutate({ targetUserId: selectedUser.userId })
    },
    [addContactMutation, currentUserSceneName, existingDelegateSceneNames]
  )

  // Determine the "no results" message
  const getNoResultsMessage = (): string | undefined => {
    if (debouncedSearch.length < 2) return undefined
    if (isSearching) return undefined
    if (!searchResults || searchResults.length === 0) {
      return `No members found matching "${debouncedSearch}"`
    }
    return undefined
  }

  const noResultsMessage = getNoResultsMessage()

  return (
    <Box
      p="sm"
      style={{
        border: '1px solid var(--color-taupe)',
        borderRadius: '8px',
        backgroundColor: 'var(--color-cream)',
      }}
    >
      <Autocomplete
        ref={inputRef}
        label="Search by scene name, email, FetLife name, or Discord name"
        placeholder="Type at least 2 characters..."
        value={searchValue}
        onChange={setSearchValue}
        onOptionSubmit={handleSelect}
        data={autocompleteData}
        error={inlineError}
        rightSection={
          isSearching || addContactMutation.isPending ? (
            <Loader size="xs" color="burgundy" />
          ) : null
        }
        disabled={addContactMutation.isPending}
        styles={{
          label: {
            fontFamily: 'var(--font-heading)',
            fontWeight: 600,
            color: 'var(--color-burgundy)',
            marginBottom: '4px',
          },
        }}
        data-testid="contact-search-input"
      />

      {/* Show "no results" message when search returns empty */}
      {noResultsMessage && (
        <Text size="sm" c="dimmed" mt="xs">
          {noResultsMessage}
        </Text>
      )}
    </Box>
  )
}
