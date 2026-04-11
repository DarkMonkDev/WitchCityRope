import React from 'react';
import { Badge } from '@mantine/core';
import {
  getConfigFromStatus,
  type VettingStatus,
} from '../../../../features/vetting/constants/vettingStatusConfig';

interface VettingStatusBadgeProps {
  status: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  'data-testid'?: string;
}

/**
 * Normalize various status-string input formats to a canonical VettingStatus.
 *
 * The badge may receive status strings in several forms depending on which
 * DTO the caller consumes: PascalCase ("UnderReview"), space-separated
 * ("Under Review"), various case combinations, and legacy aliases like
 * "InReview" or "Rejected". This helper maps all of those to the
 * authoritative VettingStatus string used by the config file.
 *
 * Returns null for the "not applied" state and any unknown input — the
 * component's render path falls back to a gray badge with the raw input
 * string in those cases.
 */
function normalizeStatusInput(input: string): VettingStatus | null {
  const compact = input.toLowerCase().replace(/\s+/g, '');
  switch (compact) {
    case 'underreview':
    case 'inreview':
      return 'UnderReview';
    case 'interviewapproved':
      return 'InterviewApproved';
    case 'finalreview':
      return 'FinalReview';
    case 'approved':
      return 'Approved';
    case 'denied':
    case 'rejected':
      return 'Denied';
    case 'onhold':
      return 'OnHold';
    case 'withdrawn':
      return 'Withdrawn';
    default:
      return null;
  }
}

/**
 * Hex colors for the admin badge's custom styling. These are NOT Mantine
 * theme colors — the admin vetting UI uses specific hex values chosen in
 * the original wireframe that don't map cleanly to Mantine's palette.
 *
 * Kept inline in this component rather than in vettingStatusConfig.ts
 * because:
 *   1. The config uses Mantine theme color names (e.g. 'blue', 'green')
 *      which components reference through the Mantine Alert/Badge color
 *      prop — the badge here bypasses that and uses inline styles.
 *   2. These hex values are specific to this one component. No other
 *      file in the codebase uses them in a vetting-status context.
 *   3. Adding hex fields to the main config would force every status
 *      config entry to carry two color representations (theme + hex)
 *      which clutters the data model for a single consumer's benefit.
 *
 * If more components adopt the same hex palette, consider consolidating
 * into the main config then.
 */
const BADGE_HEX_COLORS: Record<VettingStatus, string> = {
  UnderReview: '#868e96',
  InterviewApproved: '#D4AF37',
  FinalReview: '#7950f2',
  Approved: '#51cf66',
  Denied: '#c92a2a',
  OnHold: '#fd7e14',
  Withdrawn: '#868e96',
};

/**
 * CSS class names for wireframe compatibility. Phase 2d migration kept
 * this switch inline rather than deriving from the status name because
 * the wireframe CSS (defined elsewhere) uses specific class names and
 * we don't want to break styles by auto-generating them.
 */
function getStatusCssClass(input: string): string {
  switch (input.toLowerCase()) {
    case 'underreview':
    case 'inreview':
    case 'in review':
      return 'status-under-review';
    case 'interviewapproved':
    case 'interview approved':
      return 'status-interview-approved';
    case 'finalreview':
    case 'final review':
      return 'status-final-review';
    case 'approved':
      return 'status-approved';
    case 'denied':
    case 'rejected':
      return 'status-denied';
    case 'onhold':
    case 'on hold':
      return 'status-on-hold';
    case 'withdrawn':
      return 'status-withdrawn';
    case 'not applied':
      return 'status-not-applied';
    default:
      return 'status-unknown';
  }
}

// Font size lookup for the Mantine-style `size` prop. Small standalone
// table so the size selection doesn't clutter the render.
const FONT_SIZE_BY_SIZE: Record<'xs' | 'sm' | 'md' | 'lg' | 'xl', string> = {
  xs: '10px',
  sm: '12px',
  md: '14px',
  lg: '16px',
  xl: '18px',
};

export const VettingStatusBadge: React.FC<VettingStatusBadgeProps> = ({
  status,
  size = 'sm',
  'data-testid': dataTestId,
}) => {
  // Phase 2d migration: labels now come from the single-source vetting
  // status config. Previously this component had a local switch with its
  // own hand-maintained label strings that could drift from the config's
  // labels. The hex colors and CSS classes stay local because they're
  // specific to this admin badge styling and not shared with other
  // vetting UI components.
  const normalized = normalizeStatusInput(status);
  const config = normalized ? getConfigFromStatus(normalized) : null;

  // Label: prefer the config's label for a known status; otherwise
  // handle the "not applied" sentinel, else fall back to the raw input.
  let label: string;
  if (config) {
    label = config.label;
  } else if (status.toLowerCase() === 'not applied') {
    label = 'Not Applied';
  } else {
    label = status;
  }

  // Background hex: unknown / not-applied statuses fall back to neutral gray.
  const backgroundColor = normalized
    ? BADGE_HEX_COLORS[normalized]
    : status.toLowerCase() === 'not applied'
      ? '#adb5bd'
      : '#868e96';

  return (
    <Badge
      size={size}
      className={getStatusCssClass(status)}
      data-testid={dataTestId}
      style={{
        backgroundColor,
        color: 'white',
        borderRadius: '12px',
        fontWeight: 600,
        fontSize: FONT_SIZE_BY_SIZE[size],
        textTransform: 'uppercase',
        letterSpacing: '0.5px',
        border: 'none',
      }}
    >
      {label}
    </Badge>
  );
};
