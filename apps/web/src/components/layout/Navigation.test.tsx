/**
 * Navigation Component Tests
 * Tests conditional "How to Join" menu visibility based on vetting status
 */
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { BrowserRouter } from 'react-router-dom';
import { MantineProvider } from '@mantine/core';
import { Navigation } from './Navigation';

// Mock dependencies
vi.mock('../../stores/authStore', () => ({
  useUser: vi.fn(),
  useIsAuthenticated: vi.fn()
}));

vi.mock('../../features/vetting/hooks/useMenuVisibility', () => ({
  useMenuVisibility: vi.fn()
}));

import { useUser, useIsAuthenticated } from '../../stores/authStore';
import { useMenuVisibility } from '../../features/vetting/hooks/useMenuVisibility';

// Wrapper for React Router and Mantine
const renderWithProviders = (component: React.ReactElement) => {
  return render(
    <BrowserRouter>
      <MantineProvider>{component}</MantineProvider>
    </BrowserRouter>
  );
};

describe('Navigation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('How to Join menu visibility', () => {
    it('shows "How to Join" when useMenuVisibility returns true', () => {
      vi.mocked(useUser).mockReturnValue(null);
      vi.mocked(useIsAuthenticated).mockReturnValue(false);
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: true,
        reason: 'User not authenticated'
      });

      renderWithProviders(<Navigation />);

      expect(screen.getByText('How to Join')).toBeInTheDocument();
    });

    it('hides "How to Join" when useMenuVisibility returns false', () => {
      vi.mocked(useUser).mockReturnValue({
        id: 'test-id',
        email: 'test@example.com',
        sceneName: 'Test User',
        role: 'Member'
      } as any);
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: false,
        reason: 'Status Approved requires menu to be hidden'
      });

      renderWithProviders(<Navigation />);

      expect(screen.queryByText('How to Join')).not.toBeInTheDocument();
    });

    it('shows "How to Join" for unauthenticated users', () => {
      vi.mocked(useUser).mockReturnValue(null);
      vi.mocked(useIsAuthenticated).mockReturnValue(false);
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: true,
        reason: 'User not authenticated - show join option'
      });

      renderWithProviders(<Navigation />);

      expect(screen.getByText('How to Join')).toBeInTheDocument();
    });

    it('shows "How to Join" for authenticated users with no application', () => {
      vi.mocked(useUser).mockReturnValue({
        id: 'test-id',
        email: 'test@example.com',
        sceneName: 'Test User',
        role: 'Member'
      } as any);
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: true,
        reason: 'No application exists - show join option'
      });

      renderWithProviders(<Navigation />);

      expect(screen.getByText('How to Join')).toBeInTheDocument();
    });

    it('hides "How to Join" for users with OnHold status', () => {
      vi.mocked(useUser).mockReturnValue({
        id: 'test-id',
        email: 'test@example.com',
        sceneName: 'Test User',
        role: 'Member'
      } as any);
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: false,
        reason: 'Status OnHold requires menu to be hidden'
      });

      renderWithProviders(<Navigation />);

      expect(screen.queryByText('How to Join')).not.toBeInTheDocument();
    });
  });

  describe('other navigation items', () => {
    beforeEach(() => {
      vi.mocked(useMenuVisibility).mockReturnValue({
        shouldShow: true,
        reason: 'Default'
      });
    });

    it('always shows Events & Classes link', () => {
      vi.mocked(useUser).mockReturnValue(null);
      vi.mocked(useIsAuthenticated).mockReturnValue(false);

      renderWithProviders(<Navigation />);

      expect(screen.getByText('Events & Classes')).toBeInTheDocument();
    });

    it('always shows Resources link', () => {
      vi.mocked(useUser).mockReturnValue(null);
      vi.mocked(useIsAuthenticated).mockReturnValue(false);

      renderWithProviders(<Navigation />);

      expect(screen.getByText('Resources')).toBeInTheDocument();
    });

    it('shows Admin link for administrators', () => {
      vi.mocked(useUser).mockReturnValue({
        id: 'admin-id',
        email: 'admin@example.com',
        sceneName: 'Admin User',
        role: 'Administrator'
      } as any);
      vi.mocked(useIsAuthenticated).mockReturnValue(true);

      renderWithProviders(<Navigation />);

      expect(screen.getByTestId('link-admin')).toBeInTheDocument();
    });

    it('hides Admin link for non-administrators', () => {
      vi.mocked(useUser).mockReturnValue({
        id: 'member-id',
        email: 'member@example.com',
        sceneName: 'Member User',
        role: 'Member'
      } as any);
      vi.mocked(useIsAuthenticated).mockReturnValue(true);

      renderWithProviders(<Navigation />);

      expect(screen.queryByTestId('link-admin')).not.toBeInTheDocument();
    });
  });
});
