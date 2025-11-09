import React from 'react';
import { Paper, Stack, Text, Title, Grid, Group, Anchor } from '@mantine/core';

interface PeopleInvolvedCardProps {
  involvedParties?: string;
  witnesses?: string;
  coordinatorName?: string;
  onEditCoordinator: () => void;
  onEditInvolvedParties: () => void;
  onEditWitnesses: () => void;
}

export const PeopleInvolvedCard: React.FC<PeopleInvolvedCardProps> = ({
  involvedParties,
  witnesses,
  coordinatorName,
  onEditCoordinator,
  onEditInvolvedParties,
  onEditWitnesses
}) => {
  return (
    <Paper p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      <Title order={3} style={{ color: '#880124' }} mb="lg">
        People Involved
      </Title>

      <Grid gutter="xl">
        {/* Coordinator */}
        <Grid.Col span={4}>
          <Stack gap="xs">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed" fw={500}>Coordinator</Text>
              {coordinatorName && (
                <Anchor size="sm" onClick={onEditCoordinator} style={{ cursor: 'pointer' }}>
                  edit
                </Anchor>
              )}
            </Group>
            <Text size="sm">
              {coordinatorName || 'None'}
            </Text>
          </Stack>
        </Grid.Col>

        {/* Involved Parties */}
        <Grid.Col span={4}>
          <Stack gap="xs">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed" fw={500}>Involved Parties</Text>
              <Anchor size="sm" onClick={onEditInvolvedParties} style={{ cursor: 'pointer' }}>
                edit
              </Anchor>
            </Group>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
              {involvedParties || 'None'}
            </Text>
          </Stack>
        </Grid.Col>

        {/* Witnesses */}
        <Grid.Col span={4}>
          <Stack gap="xs">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed" fw={500}>Witnesses</Text>
              <Anchor size="sm" onClick={onEditWitnesses} style={{ cursor: 'pointer' }}>
                edit
              </Anchor>
            </Group>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
              {witnesses || 'None'}
            </Text>
          </Stack>
        </Grid.Col>
      </Grid>
    </Paper>
  );
};
