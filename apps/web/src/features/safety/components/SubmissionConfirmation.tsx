// Incident Report Submission Success Component
// Shows confirmation after successful incident submission
// REDESIGNED: 2025-11-16 - Simplified, brand-aligned success experience

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
import { IconCopy } from '@tabler/icons-react';
import { useClipboard, useMediaQuery } from '@mantine/hooks';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

interface SubmissionConfirmationProps {
  submissionResult: {
    referenceNumber: string;
    submittedAt: string;
  };
}

export function SubmissionConfirmation({ submissionResult }: SubmissionConfirmationProps) {
  const clipboard = useClipboard({ timeout: 2000 });
  const eventTimeZone = useEventTimeZone();
  const isMobile = useMediaQuery('(max-width: 991px)');

  const formatSubmissionTime = (isoString: string) => {
    const date = new Date(isoString);
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true,
      timeZone: eventTimeZone
    });
  };

  return (
    <Box maw={800} mx="auto" p="md">
      {/* Page Title Section */}
      <Box ta="center" mb="xl">
        <Title
          order={1}
          style={{
            fontFamily: 'var(--font-heading)',
            fontSize: 'clamp(36px, 5vw, 48px)',
            fontWeight: 800,
            textTransform: 'uppercase',
            letterSpacing: '3px',
            color: 'var(--color-burgundy)',
            marginBottom: 'var(--space-sm)'
          }}
        >
          Incident Report Submitted
        </Title>

        <Text
          size="lg"
          style={{
            fontFamily: 'var(--font-body)',
            fontSize: '18px',
            color: 'var(--color-charcoal)',
            fontWeight: 400
          }}
        >
          Submitted on {formatSubmissionTime(submissionResult.submittedAt)}
        </Text>
      </Box>

      {/* Main Confirmation Card */}
      <Paper
        shadow={isMobile ? undefined : "sm"}
        p={isMobile ? 0 : "xl"}
        radius={isMobile ? undefined : "md"}
        style={isMobile ? { backgroundColor: 'transparent' } : {
          borderTop: '4px solid var(--color-burgundy)',
          backgroundColor: '#FFFFFF'
        }}
      >
        <Stack gap="lg" align="center">
          {/* Thank You Message */}
          <div className="html-content" style={{ textAlign: 'center', maxWidth: '500px' }}>
            <h3>Thank you for helping keep our community safe</h3>
          </div>

          {/* Confirmation Message */}
          <Text
            size="lg"
            ta="center"
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: '18px',
              fontWeight: 400,
              color: 'var(--color-charcoal)',
              lineHeight: 1.7,
              maxWidth: '500px'
            }}
          >
            Your safety incident report has been received. The safety team has been
            notified and will review your report promptly.
          </Text>

          {/* Reference Number Display */}
          <Alert
            variant="light"
            color="grape"
            style={{
              width: 'fit-content',
              border: '1px solid var(--color-plum)',
              backgroundColor: 'rgba(97, 75, 121, 0.05)'
            }}
          >
            <Group justify="space-between" align="center" wrap="nowrap">
              <Group gap="sm" align="center" style={{ flex: 1 }}>
                <Text
                  size="sm"
                  fw={600}
                  style={{
                    fontFamily: 'var(--font-heading)',
                    textTransform: 'uppercase',
                    letterSpacing: '0.5px',
                    color: 'var(--color-smoke)'
                  }}
                >
                  Reference Number:
                </Text>
                <Code
                  style={{
                    fontSize: '16px',
                    fontWeight: 700,
                    color: 'var(--color-burgundy)',
                    backgroundColor: 'transparent',
                    border: '1px solid var(--color-taupe)',
                    padding: '4px 8px'
                  }}
                >
                  {submissionResult.referenceNumber}
                </Code>
              </Group>

              <Tooltip
                label={clipboard.copied ? 'Copied!' : 'Copy reference number'}
                position="left"
              >
                <ActionIcon
                  variant="light"
                  color="grape"
                  size="lg"
                  onClick={() => clipboard.copy(submissionResult.referenceNumber)}
                  aria-label="Copy reference number to clipboard"
                  style={{
                    backgroundColor: clipboard.copied
                      ? 'var(--color-plum)'
                      : 'rgba(97, 75, 121, 0.1)',
                    minWidth: '44px',
                    minHeight: '44px'
                  }}
                >
                  <IconCopy
                    size={18}
                    color={clipboard.copied ? '#FFF' : 'var(--color-plum)'}
                  />
                </ActionIcon>
              </Tooltip>
            </Group>
          </Alert>

          {/* What Happens Next */}
          <Text
            size="lg"
            ta="center"
            style={{
              fontFamily: 'var(--font-body)',
              fontSize: '18px',
              color: 'var(--color-charcoal)',
              fontWeight: 400,
              lineHeight: 1.7,
              maxWidth: '500px'
            }}
          >
            The safety team will review your report and may contact you for additional
            information if needed.
          </Text>
        </Stack>
      </Paper>
    </Box>
  );
}
