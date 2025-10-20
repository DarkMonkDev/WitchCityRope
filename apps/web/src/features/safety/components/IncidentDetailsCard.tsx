import React from 'react';
import { Paper, Stack, Text, Title, Badge, Group } from '@mantine/core';
import { IconLock, IconMail, IconCalendar } from '@tabler/icons-react';
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
  reportedAt?: string;
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
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

export const IncidentDetailsCard: React.FC<IncidentDetailsCardProps> = ({
  description,
  isAnonymous,
  reporterName,
  reporterEmail,
  requestedFollowUp,
  incidentDate,
  reportedAt,
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
      <Title order={3} style={{ color: '#880124' }} mb="lg">
        Incident Details
      </Title>

      <Stack gap="md">
        {/* Type of Incident */}
        {incidentType && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Type of Incident</Text>
            <Badge size="lg" color="orange">{INCIDENT_TYPE_LABELS[incidentType]}</Badge>
          </div>
        )}

        {/* Incident Date & Location */}
        <Group gap="xl">
          {incidentDate && (
            <div>
              <Text size="xs" c="dimmed" mb={4}>
                <IconCalendar size={14} style={{ display: 'inline', verticalAlign: 'middle', marginRight: 4 }} />
                Incident Date
              </Text>
              <Text size="sm" fw={600}>{formatDateTime(incidentDate)}</Text>
            </div>
          )}

          {reportedAt && (
            <div>
              <Text size="xs" c="dimmed" mb={4}>Reported On</Text>
              <Text size="sm">{formatDateTime(reportedAt)}</Text>
            </div>
          )}
        </Group>

        {/* Where Occurred */}
        {whereOccurred && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Where Did This Occur?</Text>
            <Text size="sm" fw={500}>{WHERE_OCCURRED_LABELS[whereOccurred]}</Text>
          </div>
        )}

        {/* Event Name */}
        {eventName && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Event Name</Text>
            <Text size="sm" fw={500}>{eventName}</Text>
          </div>
        )}

        {/* Location Details */}
        {location && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Location Details</Text>
            <Text size="sm" fw={500}>{location}</Text>
          </div>
        )}

        {/* Description */}
        <div>
          <Text size="sm" c="dimmed" mb="xs">Description</Text>
          <Text size="sm" style={{ whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
            {description}
          </Text>
        </div>

        {/* Reporter Information */}
        <div>
          <Text size="sm" c="dimmed" mb="xs">Reporter</Text>
          {isAnonymous ? (
            <Group gap="xs">
              <Badge color="gray" leftSection={<IconLock size={12} />}>
                Anonymous Report
              </Badge>
              <Text size="sm" c="dimmed">No follow-up capability</Text>
            </Group>
          ) : (
            <Stack gap="xs">
              <Group gap="xs">
                <Text size="sm" fw={600}>{reporterName || 'Identified Reporter'}</Text>
                {requestedFollowUp && (
                  <Badge color="blue" size="sm" leftSection={<IconMail size={12} />}>
                    Follow-up Requested
                  </Badge>
                )}
              </Group>
              {reporterEmail && (
                <Text size="sm" c="dimmed">{reporterEmail}</Text>
              )}
            </Stack>
          )}
        </div>

        {/* Contact Information (for non-anonymous reports) */}
        {!isAnonymous && (contactName || contactEmail) && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Contact Information</Text>
            <Stack gap="xs">
              {contactName && (
                <Group gap="xs">
                  <Text size="sm" fw={600}>Name:</Text>
                  <Text size="sm">{contactName}</Text>
                </Group>
              )}
              {contactEmail && (
                <Group gap="xs">
                  <IconMail size={16} color="#666" />
                  <Text size="sm">{contactEmail}</Text>
                </Group>
              )}
            </Stack>
          </div>
        )}

        {/* Has Spoken To Person */}
        {hasSpokenToPerson && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Has Spoken To Person Involved?</Text>
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

        {/* Anonymity Preferences */}
        {(anonymousDuringInvestigation !== undefined || anonymousInFinalReport !== undefined) && (
          <div>
            <Text size="xs" c="dimmed" mb={4}>Anonymity Preferences</Text>
            <Stack gap="xs">
              <Group gap="xs">
                <Badge color={anonymousDuringInvestigation ? "blue" : "gray"} size="sm">
                  {anonymousDuringInvestigation ? "Anonymous During Investigation" : "Identified During Investigation"}
                </Badge>
              </Group>
              <Group gap="xs">
                <Badge color={anonymousInFinalReport ? "blue" : "gray"} size="sm">
                  {anonymousInFinalReport ? "Anonymous In Final Report" : "Identified In Final Report"}
                </Badge>
              </Group>
            </Stack>
          </div>
        )}

        {/* Timestamps */}
        <Group gap="xl">
          <div>
            <Text size="xs" c="dimmed" mb={4}>Created</Text>
            <Text size="sm">{formatDateTime(createdAt)}</Text>
          </div>

          <div>
            <Text size="xs" c="dimmed" mb={4}>Last Updated</Text>
            <Text size="sm">{formatDateTime(updatedAt)}</Text>
          </div>
        </Group>
      </Stack>
    </Paper>
  );
};
