import React, { useState, useMemo } from 'react';
import { Select, Text, Group, Stack, Loader } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconSearch } from '@tabler/icons-react';
import { useSearchMembers, UserSearchResultDto } from '../../lib/api/hooks/useVolunteerAssignment';

// Export SearchableMember type for backward compatibility (maps to UserSearchResultDto)
export type SearchableMember = UserSearchResultDto;

interface MemberSearchInputProps {
  excludedMemberIds: string[]; // Members already assigned to this position
  onMemberSelect: (memberId: string) => void;
}

export const MemberSearchInput: React.FC<MemberSearchInputProps> = ({
  excludedMemberIds,
  onMemberSelect,
}) => {
  const [searchValue, setSearchValue] = useState('');
  const [debouncedSearch] = useDebouncedValue(searchValue, 300);

  // Use API hook to search members (only triggers when 3+ characters)
  const { data: searchResults = [], isLoading, error } = useSearchMembers(debouncedSearch);

  // Filter out already assigned members
  const filteredMembers = useMemo(() => {
    return searchResults.filter(
      member => !excludedMemberIds.includes(member.userId)
    );
  }, [searchResults, excludedMemberIds]);

  // Transform members to Select data format with custom rendering info
  const selectData = filteredMembers.map(member => ({
    value: member.userId,
    label: member.sceneName,
    email: member.email,
    discord: member.discordName,
  }));

  const handleSelect = (value: string | null) => {
    if (value) {
      onMemberSelect(value);
      setSearchValue(''); // Clear search after selection
    }
  };

  // Custom render function for Select items
  const renderSelectOption = ({ option }: { option: any }) => (
    <Group gap="xs" wrap="nowrap">
      <Stack gap={2} style={{ flex: 1 }}>
        <Text fw={600} size="sm">
          {option.label}
        </Text>
        <Group gap="md">
          <Text size="xs" c="dimmed">
            {option.email}
          </Text>
          {option.discord && (
            <Text size="xs" c="dimmed">
              Discord: {option.discord}
            </Text>
          )}
        </Group>
      </Stack>
    </Group>
  );

  // Determine appropriate message based on state
  const getNothingFoundMessage = () => {
    if (isLoading) {
      return 'Searching...';
    }
    if (error) {
      return 'Error searching members. Please try again.';
    }
    if (searchValue.trim().length < 3) {
      return 'Type at least 3 characters to search';
    }
    if (debouncedSearch && searchResults.length === 0) {
      return 'No members found matching your search';
    }
    if (filteredMembers.length === 0 && searchResults.length > 0) {
      return 'All matching members are already assigned';
    }
    return 'Start typing to search for members';
  };

  return (
    <Select
      label="Add Member to Position"
      placeholder="Search by name, scene name, or Discord..."
      data={selectData}
      searchable
      searchValue={searchValue}
      onSearchChange={setSearchValue}
      onChange={handleSelect}
      value={null} // Always null so it clears after selection
      clearable
      disabled={isLoading}
      nothingFoundMessage={getNothingFoundMessage()}
      leftSection={isLoading ? <Loader size={16} /> : <IconSearch size={16} />}
      maxDropdownHeight={300}
      renderOption={renderSelectOption}
      data-testid="member-search-input"
      styles={{
        label: { fontWeight: 600, marginBottom: '8px' },
        input: {
          height: '44px',
          fontSize: '14px',
        }
      }}
    />
  );
};
