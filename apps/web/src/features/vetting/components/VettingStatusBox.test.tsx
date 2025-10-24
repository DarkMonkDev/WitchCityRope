/**
 * VettingStatusBox Component Tests
 * Tests rendering of vetting status information across all status variants
 */
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { VettingStatusBox } from './VettingStatusBox';
import type { VettingStatus } from '../types/vettingStatus';

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
    it('renders UnderReview status correctly (initial status)', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="UnderReview"
          nextSteps="Your application is being reviewed"
          estimatedDaysRemaining={7}
        />
      );

      expect(screen.getByText('Under Review')).toBeInTheDocument();
      expect(screen.getByText('V-2025-001')).toBeInTheDocument();
      expect(screen.getByText('Your application is being reviewed')).toBeInTheDocument();
      expect(screen.getByText(/Estimated: 7 days remaining/)).toBeInTheDocument();
    });

    it('renders FinalReview status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="FinalReview"
          nextSteps="Final review in progress"
        />
      );

      expect(screen.getByText('Final Review')).toBeInTheDocument();
      expect(screen.getByText('Final review in progress')).toBeInTheDocument();
    });

    it('renders InterviewApproved status correctly', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="InterviewApproved"
          nextSteps="Schedule your interview"
        />
      );

      expect(screen.getByText('Awaiting Interview')).toBeInTheDocument();
      expect(screen.getByText('Schedule your interview')).toBeInTheDocument();
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
        <VettingStatusBox {...baseProps} status="UnderReview" nextSteps={undefined} />
      );

      expect(screen.getByText('V-2025-001')).toBeInTheDocument();
      expect(screen.queryByText('Next Steps:')).not.toBeInTheDocument();
    });

    it('renders without estimatedDaysRemaining', () => {
      renderWithMantine(
        <VettingStatusBox
          {...baseProps}
          status="UnderReview"
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
          status="UnderReview"
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
      const statuses: VettingStatus[] = [
        'UnderReview',
        'InterviewApproved',
        'FinalReview',
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
        <VettingStatusBox {...baseProps} status="UnderReview" statusDescription="Test description" />
      );

      expect(screen.getByText('Test description')).toBeInTheDocument();
    });
  });
});
