// Submission Confirmation Component
// Shows confirmation screen after successful incident submission

import React from 'react';
import {
  Box,
  Paper,
  Title,
  Text,
  Button,
  Alert,
  Group,
  Stack,
  Code,
  ActionIcon,
  Tooltip
} from '@mantine/core';
import { IconCheck, IconCopy, IconCalendar, IconExternalLink } from '@tabler/icons-react';
import { useClipboard } from '@mantine/hooks';

interface SubmissionConfirmationProps {
  submissionResult: {
    referenceNumber: string;
    trackingUrl: string;
    submittedAt: string;
  };
  onNewReport: () => void;
}

export function SubmissionConfirmation({ submissionResult, onNewReport }: SubmissionConfirmationProps) {
  const clipboard = useClipboard({ timeout: 2000 });
  
  const formatSubmissionTime = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };
  
  return (
    <Box maw={600} mx="auto" p="md">
      <Paper shadow="sm" p="xl" radius="md">
        <Stack gap="lg" align="center">
          {/* Success Icon */}
          <Box
            style={{
              width: 80,
              height: 80,
              borderRadius: '50%',
              backgroundColor: '#E7F5E7',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <IconCheck size={40} color="#22C55E" />
          </Box>
          
          {/* Header */}
          <Box ta="center">
            <Title order={1} size="h2" mb="xs" c="green.7">
              Report Submitted Successfully
            </Title>
            <Text c="dimmed">
              Your safety incident report has been received and is being processed by our safety team.
            </Text>
          </Box>
          
          {/* Reference Number */}
          <Alert 
            variant="light" 
            color="blue" 
            style={{ width: '100%' }}
            icon={<IconCalendar size={16} />}
          >
            <Stack gap="xs">
              <Group justify="space-between" align="center">
                <Box>
                  <Text size="sm" fw={500} mb={2}>Reference Number</Text>
                  <Code size="lg" fw={700}>{submissionResult.referenceNumber}</Code>
                </Box>
                <Tooltip label={clipboard.copied ? 'Copied!' : 'Copy reference number'}>
                  <ActionIcon
                    variant="light"
                    onClick={() => clipboard.copy(submissionResult.referenceNumber)}
                    color={clipboard.copied ? 'green' : 'blue'}
                  >
                    <IconCopy size={16} />
                  </ActionIcon>
                </Tooltip>
              </Group>
              
              <Text size="xs" c="dimmed">
                Submitted on {formatSubmissionTime(submissionResult.submittedAt)}
              </Text>
            </Stack>
          </Alert>
          
          {/* Important Information */}
          <Stack gap="sm" style={{ width: '100%' }}>
            <Title order={3} size="h4" ta="center">Important Information</Title>
            
            <Alert variant="light" color="yellow">
              <Stack gap="xs">
                <Text size="sm" fw={500}>📧 Safety Team Notification</Text>
                <Text size="sm">
                  The safety team has been automatically notified of your report and will review it promptly.
                </Text>
              </Stack>
            </Alert>
            
            <Alert variant="light" color="blue">
              <Stack gap="xs">
                <Text size="sm" fw={500}>🔍 Track Your Report</Text>
                <Text size="sm">
                  Use your reference number to check the status of your report at any time. 
                  Bookmark this page or save your reference number.
                </Text>
              </Stack>
            </Alert>
            
            <Alert variant="light" color="green">
              <Stack gap="xs">
                <Text size="sm" fw={500}>🔒 Privacy Protected</Text>
                <Text size="sm">
                  Your report has been encrypted and stored securely. Only authorized safety team members can access the details.
                </Text>
              </Stack>
            </Alert>
          </Stack>
          
          {/* Action Buttons */}
          <Group justify="center" gap="md" mt="lg">
            <Button
              variant="outline"
              leftSection={<IconExternalLink size={16} />}
              onClick={() => window.open(submissionResult.trackingUrl, '_blank')}
            >
              Track Report Status
            </Button>
            
            <Button
              onClick={onNewReport}
              style={{
                background: 'linear-gradient(135deg, #FFBF00 0%, #DAA520 100%)',
                border: 'none'
              }}
            >
              Submit Another Report
            </Button>
          </Group>
          
          {/* Additional Resources */}
          <Box ta="center" mt="xl">
            <Text size="sm" c="dimmed" mb="xs">
              Need immediate assistance?
            </Text>
            <Group justify="center" gap="xl">
              <Box ta="center">
                <Text size="sm" fw={500}>Safety Team</Text>
                <Text size="sm" c="blue">safety@witchcityrope.com</Text>
              </Box>
              <Box ta="center">
                <Text size="sm" fw={500}>Crisis Support</Text>
                <Text size="sm" c="blue">Available 24/7</Text>
              </Box>
            </Group>
          </Box>
        </Stack>
      </Paper>
    </Box>
  );
}