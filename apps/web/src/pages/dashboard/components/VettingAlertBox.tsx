/**
 * VettingAlertBox
 * ═══════════════
 *
 * Conditional alert banner shown on the user's dashboard and on the /join
 * page when the user has an in-progress vetting application. Displays a
 * status-appropriate message (and link, where relevant) to tell the user
 * what they need to do next, or what state their application is in.
 *
 * SINGLE SOURCE OF TRUTH
 * ----------------------
 * This component is a DUMB renderer. It does NOT own any copy, colors,
 * titles, or status-key logic. All of that lives in:
 *   apps/web/src/features/vetting/constants/vettingStatusConfig.ts
 *
 * To change an alert title, color, or emoji: edit the config file, not
 * this component. To add a new status, add it to the config Record and
 * TypeScript will force this component to keep compiling.
 *
 * PRIOR BUG (fixed here)
 * ----------------------
 * Before the centralization, this component had a local `alertConfigs`
 * object keyed by stale status names ("Pending", "ApprovedForInterview")
 * from a pre-Calendly version of the backend enum. The backend was
 * refactored (Pending → UnderReview, ApprovedForInterview → InterviewApproved,
 * etc.) but this component was not updated. A later "strict-mode cleanup"
 * commit silenced the resulting TypeScript error with an
 * `as keyof typeof alertConfigs` cast, letting the broken lookup ship
 * silently — the alert never rendered for UnderReview or InterviewApproved
 * users, which was the bug report that prompted this fix.
 *
 * CUSTOM LINKS
 * ------------
 * Two statuses (InterviewApproved, Denied) need a clickable link inline
 * with the alert body. The config exposes a `hasCustomLink: boolean` flag
 * per status. When true, this component reads the relevant DTO field
 * (status.interviewScheduleUrl or status.reapplyInfoUrl) and renders an
 * <Anchor>. If the DTO field is missing at runtime, the component falls
 * back to the plain `message` from the config.
 */

import React from 'react';
import { Alert, Anchor, Box, Text } from '@mantine/core';
import type { VettingStatusDto } from '../../../types/dashboard.types';
import { getConfigFromStatus } from '../../../features/vetting/constants/vettingStatusConfig';

interface VettingAlertBoxProps {
  status: VettingStatusDto;
}

export const VettingAlertBox: React.FC<VettingAlertBoxProps> = ({ status }) => {
  // Single lookup — `getConfigFromStatus` returns null for missing/null/
  // undefined status, and each config's `dashboardAlert` is null for statuses
  // that intentionally don't show an alert (e.g. Approved).
  const config = getConfigFromStatus(status.status);
  if (!config || !config.dashboardAlert) {
    return null;
  }

  const alertDef = config.dashboardAlert;

  // Build the message body. For statuses with a custom link, we render the
  // link inline when the corresponding DTO URL field is present. Otherwise
  // we fall back to the plain message string from the config.
  let messageBody: React.ReactNode = alertDef.message;

  if (alertDef.hasCustomLink) {
    if (status.status === 'InterviewApproved' && status.interviewScheduleUrl) {
      messageBody = (
        <>
          <Anchor href={status.interviewScheduleUrl} c="burgundy" fw={600} td="underline">
            Schedule your vetting interview here
          </Anchor>{' '}
          to complete your membership.
        </>
      );
    } else if (status.status === 'Denied' && status.reapplyInfoUrl) {
      messageBody = (
        <>
          Your membership application was not approved at this time.{' '}
          <Anchor href={status.reapplyInfoUrl} c="burgundy" fw={600} td="underline">
            Learn about reapplying
          </Anchor>
          .
        </>
      );
    }
    // else: fall through to the plain `alertDef.message` already assigned above
  }

  return (
    <Alert
      icon={
        <Box component="span" fz="24px">
          {config.emoji}
        </Box>
      }
      color={config.color}
      // Title is wrapped in a Text element so we can control size/weight
      // independent of Mantine Alert's default (~14-16px). Using `lg`
      // (~18px) makes the headline read as the first thing on the page
      // without dominating the alert body.
      title={
        <Text size="lg" fw={700}>
          {alertDef.title}
        </Text>
      }
      radius="md"
      mb="lg"
      styles={{
        root: {
          borderWidth: '2px',
        },
      }}
    >
      <Text size="sm" style={{ lineHeight: 1.6 }}>
        {messageBody}
      </Text>
    </Alert>
  );
};
