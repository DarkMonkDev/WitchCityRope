// Incident Report Submission Success Component
// Shows confirmation after successful incident submission
// REDESIGNED: 2025-11-16 - Simplified, brand-aligned success experience

import { Box, Paper, Title, Text, Stack } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { useEventTimeZone } from '../../../hooks/useEventTimeZone';

interface SubmissionConfirmationProps {
  submissionResult: {
    referenceNumber: string;
    submittedAt: string;
  };
}

export function SubmissionConfirmation({ submissionResult }: SubmissionConfirmationProps) {
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
      <Box ta="center" mb={isMobile ? 0 : "xl"}>
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
