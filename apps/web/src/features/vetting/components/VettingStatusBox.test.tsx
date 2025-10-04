/**
 * VettingStatusBox Component Tests
 * Tests rendering of vetting status information across all status variants
 */
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { VettingStatusBox } from './VettingStatusBox';
import type { VettingStatusString } from '../types/vettingStatus';

// Wrapper for Mantine components
const renderWithMantine = (component: React.ReactElement) => {
  return render(<MantineProvider>{component}</MantineProvider>);
};

describe('VettingStatusBox', () => {
  const baseProps = {
    applicationNumber: 'V-2025-001',
    submittedAt: new Date('2025-10-01T12:00:00Z'),
    lastUpdated: new Date('2025-10-03T12:00:00Z'),
    statusDescription: 'Application is being processed'
  };

  describe('status variant rendering', () => {
    it('renders Draft status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Draft"
          nextSteps="Complete and submit your application"
        />
      );

      expect(screen.getByText('Draft')).toBeInTheDocument();
      expect(screen.getByText('V-2025-001')).toBeInTheDocument();
      expect(screen.getByText('Complete and submit your application')).toBeInTheDocument();
    });

    it('renders Submitted status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Submitted"
          nextSteps="Waiting for initial review"
          estimatedDaysRemaining={7}
        />
      );

      expect(screen.getByText('Submitted')).toBeInTheDocument();
      expect(screen.getByText('Waiting for initial review')).toBeInTheDocument();
      expect(screen.getByText(/Estimated: 7 days remaining/)).toBeInTheDocument();
    });

    it('renders UnderReview status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="UnderReview"
          nextSteps="Review in progress"
        />
      );

      expect(screen.getByText('Under Review')).toBeInTheDocument();
      expect(screen.getByText('Review in progress')).toBeInTheDocument();
    });

    it('renders InterviewApproved status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="InterviewApproved"
          nextSteps="Schedule your interview"
        />
      );

      expect(screen.getByText('Interview Approved')).toBeInTheDocument();
      expect(screen.getByText('Schedule your interview')).toBeInTheDocument();
    });

    it('renders PendingInterview status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="PendingInterview"
          nextSteps="Waiting for interview scheduling"
        />
      );

      expect(screen.getByText('Pending Interview')).toBeInTheDocument();
      expect(screen.getByText('Waiting for interview scheduling')).toBeInTheDocument();
    });

    it('renders InterviewScheduled status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="InterviewScheduled"
          nextSteps="Interview scheduled for October 15, 2025"
        />
      );

      expect(screen.getByText('Interview Scheduled')).toBeInTheDocument();
      expect(screen.getByText('Interview scheduled for October 15, 2025')).toBeInTheDocument();
    });

    it('renders OnHold status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="OnHold"
          nextSteps="Application on hold pending additional information"
        />
      );

      expect(screen.getByText('On Hold')).toBeInTheDocument();
      expect(
        screen.getByText('Application on hold pending additional information')
      ).toBeInTheDocument();
    });

    it('renders Approved status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Approved"
          nextSteps="Welcome to the community!"
        />
      );

      expect(screen.getByText('Approved')).toBeInTheDocument();
      expect(screen.getByText('Welcome to the community!')).toBeInTheDocument();
    });

    it('renders Denied status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Denied"
          nextSteps="You may reapply in 6 months"
        />
      );

      expect(screen.getByText('Denied')).toBeInTheDocument();
      expect(screen.getByText('You may reapply in 6 months')).toBeInTheDocument();
    });

    it('renders Withdrawn status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Withdrawn"
          nextSteps="You may submit a new application at any time"
        />
      );

      expect(screen.getByText('Withdrawn')).toBeInTheDocument();
      expect(screen.getByText('You may submit a new application at any time')).toBeInTheDocument();
    });
  });

  describe('optional fields', () => {
    it('renders without nextSteps', () => {
      renderWithMantine(
        <VettingStatusBox {...baseProps} status="Submitted" nextSteps={undefined} />
      );

      expect(screen.getByText('V-2025-001')).toBeInTheDocument();
      expect(screen.queryByText('Next Steps:')).not.toBeInTheDocument();
    });

    it('renders without estimatedDaysRemaining', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Submitted"
          estimatedDaysRemaining={undefined}
        />
      );

      expect(screen.getByText('V-2025-001')).toBeInTheDocument();
      expect(screen.queryByText(/Estimated:/)).not.toBeInTheDocument();
    });
  });

  describe('date formatting', () => {
    it('formats dates correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="Submitted"
          submittedAt={new Date('2025-10-01T12:00:00Z')}
          lastUpdated={new Date('2025-10-03T12:00:00Z')}
        />
      );

      // Dates should be formatted as locale strings
      expect(screen.getByText(/Submitted:/)).toBeInTheDocument();
      expect(screen.getByText(/Last Updated:/)).toBeInTheDocument();
    });
  });

  describe('required fields', () => {
    it('always renders application number', () => {
      const statuses: VettingStatusString[] = [
        'Draft',
        'Submitted',
        'UnderReview',
        'Approved'
      ];

      statuses.forEach((status) => {
        const { unmount } = renderWithMantine(
          <VettingStatusBox {...baseProps} status={status} />
        );

        expect(screen.getByText('V-2025-001')).toBeInTheDocument();
        unmount();
      });
    });

    it('always renders status description', () => {
      renderWithMantine(
        <VettingStatusBox {...baseProps} status="Submitted" statusDescription="Test description" />
      );

      expect(screen.getByText('Test description')).toBeInTheDocument();
    });
  });
});
