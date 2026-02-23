import React from 'react';
import { Paper, Stack, Text, Title, Badge, Group, Grid, Anchor } from '@mantine/core';
import { IconLock } from '@tabler/icons-react';
import {
  IncidentType,
  WhereOccurred,
  SpokenToPersonStatus,
  INCIDENT_TYPE_LABELS,
  WHERE_OCCURRED_LABELS,
  SPOKEN_TO_PERSON_LABELS
} from '../types/safety.types';

interface IncidentDetailsCardProps {
  description: string;
  isAnonymous: boolean;
  reporterName?: string;
  reporterEmail?: string;
  requestedFollowUp: boolean;
  incidentDate?: string;
  location?: string;
  contactEmail?: string;
  contactName?: string;
  createdAt: string;
  updatedAt: string;
  // New wireframe fields
  incidentType?: IncidentType;
  whereOccurred?: WhereOccurred;
  eventName?: string;
  hasSpokenToPerson?: SpokenToPersonStatus;
  desiredOutcomes?: string;
  futureInteractionPreference?: string;
  anonymousDuringInvestigation?: boolean;
  anonymousInFinalReport?: boolean;
}

const formatDateTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

export const IncidentDetailsCard: React.FC<IncidentDetailsCardProps> = ({
  description,
  isAnonymous,
  reporterName,
  incidentDate,
  location,
  contactEmail,
  contactName,
  createdAt,
  updatedAt,
  // New wireframe fields
  incidentType,
  whereOccurred,
  eventName,
  hasSpokenToPerson,
  desiredOutcomes,
  futureInteractionPreference,
  anonymousDuringInvestigation,
  anonymousInFinalReport
}) => {
  return (
    <Paper p="xl" radius="md" style={{ border: '1px solid #E0E0E0' }}>
      {/* Header with title and incident type badge */}
      <Group justify="space-between" mb="lg">
        <Title order={3} style={{ color: '#880124' }}>
          Incident Details
        </Title>
        {incidentType && (
          <Badge size="lg" color="orange">{INCIDENT_TYPE_LABELS[incidentType]}</Badge>
        )}
      </Group>

      <Stack gap="md">
        {/* Three-column grid for incident fields */}
        <Grid gutter="xl">
          {/* Row 1: Incident Date | Reporter | Created */}
          {incidentDate && (
            <Grid.Col span={4}>
              <Group gap={4} wrap="nowrap" align="flex-start">
                <Text size="sm" c="dimmed">Incident Date:</Text>
                <Text size="sm" fw={600}>{formatDateTime(incidentDate)}</Text>
              </Group>
            </Grid.Col>
          )}

          <Grid.Col span={4}>
            <Group gap={4} wrap="nowrap" align="flex-start">
              <Text size="sm" c="dimmed">Reporter:</Text>
              {isAnonymous ? (
                <Badge color="gray" leftSection={<IconLock size={12} />} size="sm">
                  Anonymous
                </Badge>
              ) : (
                <Text size="sm" fw={600}>{reporterName || 'Identified Reporter'}</Text>
              )}
            </Group>
          </Grid.Col>

          <Grid.Col span={4}>
            <Group gap={4} wrap="nowrap" align="flex-start">
              <Text size="sm" c="dimmed">Created:</Text>
              <Text size="sm">{formatDateTime(createdAt)}</Text>
            </Group>
          </Grid.Col>

          {/* Row 2: Last Updated | Contact Info */}
          <Grid.Col span={4}>
            <Group gap={4} wrap="nowrap" align="flex-start">
              <Text size="sm" c="dimmed">Last Updated:</Text>
              <Text size="sm">{formatDateTime(updatedAt)}</Text>
            </Group>
          </Grid.Col>

          <Grid.Col span={4}>
            {!isAnonymous && (contactName || contactEmail) ? (
              <Stack gap="xs">
                {contactName && (
                  <Group gap={4} wrap="nowrap" align="flex-start">
                    <Text size="sm" c="dimmed">Contact Info:</Text>
                    <Text size="sm" fw={500}>{contactName}</Text>
                  </Group>
                )}
                {contactEmail && (
                  <Group gap={4} wrap="nowrap" align="flex-start">
                    <Text size="sm" c="dimmed">Email:</Text>
                    <Anchor href={`mailto:${contactEmail}`} size="sm">
                      {contactEmail}
                    </Anchor>
                  </Group>
                )}
              </Stack>
            ) : (
              <div />
            )}
          </Grid.Col>

          {/* Row 3: Where Did This Occur | Location Details | Anonymity Preferences */}
          {whereOccurred && (
            <Grid.Col span={4}>
              <Group gap={4} wrap="nowrap" align="flex-start">
                <Text size="sm" c="dimmed">Where Did This Occur?</Text>
                <Text size="sm" fw={500}>{WHERE_OCCURRED_LABELS[whereOccurred]}</Text>
              </Group>
            </Grid.Col>
          )}

          {location && (
            <Grid.Col span={4}>
              <Group gap={4} wrap="nowrap" align="flex-start">
                <Text size="sm" c="dimmed">Location Details:</Text>
                <Text size="sm" fw={500}>{location}</Text>
              </Group>
            </Grid.Col>
          )}

          <Grid.Col span={4}>
            {(anonymousDuringInvestigation !== undefined || anonymousInFinalReport !== undefined) ? (
              <div>
                <Text size="sm" c="dimmed" mb={4}>Anonymity Preferences</Text>
                <Stack gap="xs">
                  <Badge color={anonymousDuringInvestigation ? "blue" : "gray"} size="sm">
                    {anonymousDuringInvestigation ? "Anonymous During Investigation" : "Identified During Investigation"}
                  </Badge>
                  <Badge color={anonymousInFinalReport ? "blue" : "gray"} size="sm">
                    {anonymousInFinalReport ? "Anonymous In Final Report" : "Identified In Final Report"}
                  </Badge>
                </Stack>
              </div>
            ) : (
              <div />
            )}
          </Grid.Col>
        </Grid>

        {/* Additional fields outside grid */}
        {eventName && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Event Name</Text>
            <Text size="sm" fw={500}>{eventName}</Text>
          </div>
        )}

        {hasSpokenToPerson && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Has Spoken To Person?</Text>
            <Text size="sm" fw={500}>{SPOKEN_TO_PERSON_LABELS[hasSpokenToPerson]}</Text>
          </div>
        )}

        {/* Desired Outcomes */}
        {desiredOutcomes && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Desired Outcomes</Text>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
              {desiredOutcomes}
            </Text>
          </div>
        )}

        {/* Future Interaction Preference */}
        {futureInteractionPreference && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Preferred Future Interactions</Text>
            <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
              {futureInteractionPreference}
            </Text>
          </div>
        )}

        {/* Description - MUST BE LAST FIELD */}
        <div>
          <Text size="xs" c="dimmed" mb={4}>Description</Text>
          <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
            {description}
          </Text>
        </div>
      </Stack>
    </Paper>
  );
};
