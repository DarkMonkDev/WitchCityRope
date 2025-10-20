import React from 'react';
import { Paper, Stack, Text, Title, List } from '@mantine/core';

interface PeopleInvolvedCardProps {
  involvedParties?: string;
  witnesses?: string;
}

export const PeopleInvolvedCard: React.FC<PeopleInvolvedCardProps> = ({
  involvedParties,
  witnesses
}) => {
  const hasContent = involvedParties || witnesses;

  return (
    <Paper p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Title order={3} style={{ color: '#880124' }} mb="lg">
        People Involved
      </Title>

      {hasContent ? (
        <Stack gap="md">
          {involvedParties && (
            <div>
              <Text size="sm" c="dimmed" mb="xs">Involved Parties</Text>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
                {involvedParties}
              </Text>
            </div>
          )}

          {witnesses && (
            <div>
              <Text size="sm" c="dimmed" mb="xs">Witnesses</Text>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
                {witnesses}
              </Text>
            </div>
          )}
        </Stack>
      ) : (
        <Text c="dimmed" size="sm" ta="center" py="md">
          No people documented
        </Text>
      )}
    </Paper>
  );
};
