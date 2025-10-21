import React from 'react';
import { Table, Text, ActionIcon, Badge } from '@mantine/core';
import { IconX } from '@tabler/icons-react';
import { VolunteerAssignmentDto } from '../../lib/api/hooks/useVolunteerAssignment';

// Export AssignedMember type as alias to VolunteerAssignmentDto for backward compatibility
export type AssignedMember = VolunteerAssignmentDto;

interface AssignedMembersTableProps {
  members: AssignedMember[];
  onRemoveMember: (signupId: string, sceneName: string) => void;
}

export const AssignedMembersTable: React.FC<AssignedMembersTableProps> = ({
  members,
  onRemoveMember,
}) => {
  const handleRemoveClick = (signupId: string, sceneName: string) => {
    if (window.confirm(`Remove ${sceneName} from this position?`)) {
      onRemoveMember(signupId, sceneName);
    }
  };

  if (members.length === 0) {
    return (
      <Text ta="center" c="dimmed" py="xl">
        No members assigned yet. Use the search below to add volunteers.
      </Text>
    );
  }

  return (
    <Table
      striped
      highlightOnHover
      withTableBorder
      data-testid="assigned-members-table"
      style={{
        backgroundColor: 'white',
        borderRadius: '12px',
        overflow: 'hidden',
        boxShadow: '0 4px 20px rgba(0,0,0,0.08)',
      }}
    >
      <Table.Thead style={{ backgroundColor: 'var(--mantine-color-burgundy-6)' }}>
        <Table.Tr>
          <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
            Scene Name
          </Table.Th>
          <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
            Email
          </Table.Th>
          <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
            FetLife
          </Table.Th>
          <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px' }}>
            Discord
          </Table.Th>
          <Table.Th style={{ color: 'white', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '1px', textAlign: 'center' }}>
            Remove
          </Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {members.map((member) => (
          <Table.Tr key={member.signupId} data-testid="assigned-member-row">
            <Table.Td>
              <Text fw={600} c="burgundy" data-testid="member-scene-name">
                {member.sceneName}
              </Text>
            </Table.Td>
            <Table.Td>
              <Text size="sm" data-testid="member-email">
                {member.email}
              </Text>
            </Table.Td>
            <Table.Td>
              <Text size="sm" c={member.fetLifeName ? 'dark' : 'dimmed'} data-testid="member-fetlife">
                {member.fetLifeName || '—'}
              </Text>
            </Table.Td>
            <Table.Td>
              <Text size="sm" c={member.discordName ? 'dark' : 'dimmed'} data-testid="member-discord">
                {member.discordName || '—'}
              </Text>
            </Table.Td>
            <Table.Td style={{ textAlign: 'center' }}>
              <ActionIcon
                variant="filled"
                color="red"
                size="sm"
                onClick={() => handleRemoveClick(member.signupId, member.sceneName)}
                data-testid="button-remove-member"
                aria-label={`Remove ${member.sceneName}`}
              >
                <IconX size={14} />
              </ActionIcon>
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  );
};
