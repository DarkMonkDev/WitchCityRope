/**
 * VettingApplicationPage Tests
 * Tests integration of VettingStatusBox with application form
 */
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import { MantineProvider } from '@mantine/core';
import { VettingApplicationPage } from './VettingApplicationPage';

// Mock dependencies
vi.mock('../hooks/useVettingStatus', () => ({
  useVettingStatus: vi.fn()
}));

vi.mock('../components/VettingApplicationForm', () => ({
  VettingApplicationForm: vi.fn(() => <div data-testid="vetting-form">Form Component</div>)
}));

vi.mock('../components/VettingStatusBox', () => ({
  VettingStatusBox: vi.fn(({ status }) => (
    <div data-testid="status-box">Status: {status}</div>
  ))
}));

import { useVettingStatus } from '../hooks/useVettingStatus';

// Wrapper for React Router and Mantine
const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <MantineProvider>{component}</MantineProvider>
    </BrowserRouter>
  );
};

describe('VettingApplicationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('without existing application', () => {
    it('shows form when user has no application', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: false,
          application: null
        },
        isLoading: false,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('vetting-form')).toBeInTheDocument();
      expect(screen.queryByTestId('status-box')).not.toBeInTheDocument();
    });

    it('shows form while loading status', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('vetting-form')).toBeInTheDocument();
      expect(screen.queryByTestId('status-box')).not.toBeInTheDocument();
    });

    it('shows form on status fetch error', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to fetch')
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('vetting-form')).toBeInTheDocument();
      expect(screen.queryByTestId('status-box')).not.toBeInTheDocument();
    });
  });

  describe('with existing application', () => {
    it('shows status box when user has submitted application', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: true,
          application: {
            applicationId: 'test-id',
            applicationNumber: 'V-2025-001',
            status: 'Submitted',
            statusDescription: 'Application received',
            submittedAt: '2025-10-01T12:00:00Z',
            lastUpdated: '2025-10-03T12:00:00Z',
            nextSteps: 'Waiting for initial review',
            estimatedDaysRemaining: 7
          }
        },
        isLoading: false,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('status-box')).toBeInTheDocument();
      expect(screen.getByText('Status: Submitted')).toBeInTheDocument();
    });

    it('shows status box for Draft status', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: true,
          application: {
            applicationId: 'test-id',
            applicationNumber: 'V-2025-001',
            status: 'Draft',
            statusDescription: 'Application in progress',
            submittedAt: '2025-10-01T12:00:00Z',
            lastUpdated: '2025-10-03T12:00:00Z',
            nextSteps: 'Complete and submit',
            estimatedDaysRemaining: null
          }
        },
        isLoading: false,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('status-box')).toBeInTheDocument();
      expect(screen.getByText('Status: Draft')).toBeInTheDocument();
    });

    it('shows status box for Approved status', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: true,
          application: {
            applicationId: 'test-id',
            applicationNumber: 'V-2025-001',
            status: 'Approved',
            statusDescription: 'Welcome to the community',
            submittedAt: '2025-10-01T12:00:00Z',
            lastUpdated: '2025-10-03T12:00:00Z',
            nextSteps: 'Check your email for next steps',
            estimatedDaysRemaining: null
          }
        },
        isLoading: false,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      expect(screen.getByTestId('status-box')).toBeInTheDocument();
      expect(screen.getByText('Status: Approved')).toBeInTheDocument();
    });

    it('shows form below status box for Draft status', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: true,
          application: {
            applicationId: 'test-id',
            applicationNumber: 'V-2025-001',
            status: 'Draft',
            statusDescription: 'Application in progress',
            submittedAt: '2025-10-01T12:00:00Z',
            lastUpdated: '2025-10-03T12:00:00Z',
            nextSteps: 'Complete and submit',
            estimatedDaysRemaining: null
          }
        },
        isLoading: false,
        error: null
      } as any);

      renderWithProviders(<VettingApplicationPage />);

      // Both status box and form should be visible for Draft
      expect(screen.getByTestId('status-box')).toBeInTheDocument();
      expect(screen.getByTestId('vetting-form')).toBeInTheDocument();
    });
  });

  describe('status box positioning', () => {
    it('renders status box before form when both are present', () => {
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: true,
          application: {
            applicationId: 'test-id',
            applicationNumber: 'V-2025-001',
            status: 'Draft',
            statusDescription: 'Application in progress',
            submittedAt: '2025-10-01T12:00:00Z',
            lastUpdated: '2025-10-03T12:00:00Z',
            nextSteps: 'Complete and submit',
            estimatedDaysRemaining: null
          }
        },
        isLoading: false,
        error: null
      } as any);

      const { container } = renderWithProviders(<VettingApplicationPage />);

      const statusBox = screen.getByTestId('status-box');
      const form = screen.getByTestId('vetting-form');

      // Status box should appear before form in DOM order
      expect(container.innerHTML.indexOf('status-box')).toBeLessThan(
        container.innerHTML.indexOf('vetting-form')
      );
    });
  });
});
