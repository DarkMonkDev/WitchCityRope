import React from 'react';
import { Stack, Text, Box, Loader, Alert } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { AssignedMembersTable } from './AssignedMembersTable';
import { MemberSearchInput } from './MemberSearchInput';
import {
  usePositionSignups,
  useAssignMember,
  useRemoveAssignment,
} from '../../lib/api/hooks/useVolunteerAssignment';

interface AssignedMembersSectionProps {
  positionId: string;
  positionTitle: string;
}

export const AssignedMembersSection: React.FC<AssignedMembersSectionProps> = ({
  positionId,
  positionTitle,
}) => {
  // Fetch assigned members using real API
  const {
    data: assignedMembers = [],
    isLoading,
    error,
    refetch,
  } = usePositionSignups(positionId);

  // Mutations for adding/removing members
  const assignMemberMutation = useAssignMember();
  const removeAssignmentMutation = useRemoveAssignment();

  /**
   * Handle adding a member to the position
   * Mutation automatically invalidates the query and refreshes the list
   */
  const handleAddMember = async (userId: string) => {
    assignMemberMutation.mutate({ positionId, userId });
  };

  /**
   * Handle removing a member from the position
   * Mutation automatically invalidates the query and refreshes the list
   */
  const handleRemoveMember = async (signupId: string, sceneName: string) => {
    if (window.confirm(`Remove ${sceneName} from this position?`)) {
      removeAssignmentMutation.mutate({ signupId, positionId });
    }
  };

  // Error state
  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error Loading Volunteers">
        <Text>{error.message || 'Failed to load assigned members'}</Text>
        <Text size="sm" c="dimmed" mt="xs">
          Please refresh the page or contact support if the problem persists.
        </Text>
      </Alert>
    );
  }

  // Loading state
  if (isLoading) {
    return (
      <Stack gap="md" mt="xl" data-testid="assigned-members-section">
        <Box>
          <Text size="lg" fw={600} c="burgundy" mb="xs">
            Assigned Volunteers
          </Text>
          <Text size="sm" c="dimmed">
            Manage who is signed up for this position
          </Text>
        </Box>
        <Box ta="center" py="xl">
          <Loader size="md" />
          <Text size="sm" c="dimmed" mt="md">
            Loading assigned members...
          </Text>
        </Box>
      </Stack>
    );
  }

  return (
    <Stack gap="md" mt="xl" data-testid="assigned-members-section">
      <Box>
        <Text size="lg" fw={600} c="burgundy" mb="xs">
          Assigned Volunteers
        </Text>
        <Text size="sm" c="dimmed">
          Manage who is signed up for this position
        </Text>
      </Box>

      <AssignedMembersTable
        members={assignedMembers}
        onRemoveMember={handleRemoveMember}
      />

      <Box mt="md">
        <MemberSearchInput
          excludedMemberIds={assignedMembers.map(m => m.userId)}
          onMemberSelect={handleAddMember}
        />
        <Text size="xs" c="dimmed" mt="xs">
          💡 Tip: Search by scene name, email, or Discord username (minimum 3 characters)
        </Text>
      </Box>
    </Stack>
  );
};
