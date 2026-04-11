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
import { Alert, Anchor, Box, Flex, Group, Text } from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import type { VettingStatusDto } from '../../../types/dashboard.types';
import { getConfigFromStatus } from '../../../features/vetting/constants/vettingStatusConfig';

interface VettingAlertBoxProps {
  status: VettingStatusDto;
}

export const VettingAlertBox: React.FC<VettingAlertBoxProps> = ({ status }) => {
  // Responsive layout switch. On mobile (≤991px matching the dashboard
  // convention) the title and body stack vertically; on desktop they sit
  // side-by-side so the alert uses the available horizontal space
  // instead of wasting it with a mostly-empty row.
  const isMobile = useMediaQuery('(max-width: 991px)');

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
      // NOTE: No `icon` or `title` props on the Mantine Alert — both are
      // rendered manually inside a responsive Flex below. The built-in
      // `title` prop forces a stacked title-over-body layout, which
      // prevents the desktop side-by-side layout we want. Rendering
      // everything in the Alert's children gives us full control over
      // the internal arrangement.
      //
      // Title color inheritance: we use `c="var(--alert-color)"` on the
      // title Text so it picks up the Alert's theme color variable the
      // same way Mantine's built-in title element would.
      color={config.color}
      radius="md"
      styles={{
        root: {
          borderWidth: '2px',
          // Halve the alert's default bottom padding (~14px → 7px) for a
          // tighter feel. Also applied to desktop for consistency.
          paddingBottom: '7px',
        },
      }}
    >
      <Flex
        direction={isMobile ? 'column' : 'row'}
        gap={isMobile ? 'xs' : 'md'}
        align={isMobile ? 'stretch' : 'center'}
        wrap="nowrap"
      >
        {/*
          Emoji + title group: always stays together horizontally. On
          desktop this is the left "column" of the Flex; on mobile it's
          the top row. flexShrink: 0 prevents the title block from being
          squeezed when there's a long message next to it on desktop.
        */}
        <Group gap="sm" wrap="nowrap" align="center" style={{ flexShrink: 0 }}>
          <Box
            component="span"
            fz="24px"
            style={{ lineHeight: 1, flexShrink: 0 }}
          >
            {config.emoji}
          </Box>
          <Text size="lg" fw={700} c="var(--alert-color)">
            {alertDef.title}
          </Text>
        </Group>

        {/*
          Body text with inline link. On desktop this sits to the right
          of the title and takes the remaining space; on mobile it drops
          below the title.
        */}
        <Text size="sm" style={{ lineHeight: 1.6 }}>
          {messageBody}
        </Text>
      </Flex>
    </Alert>
  );
};
