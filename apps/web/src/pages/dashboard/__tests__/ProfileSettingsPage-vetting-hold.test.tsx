import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi } from 'vitest';
import { MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { BrowserRouter } from 'react-router-dom';
import { ProfileSettingsPage } from '../ProfileSettingsPage';

// Mock the auth store and API services
vi.mock('@/stores/authStore', () => ({
  useAuthStore: vi.fn(),
}));

vi.mock('@/services/vettingHold.api', () => ({
  vettingHoldService: {
    getHoldStatus: vi.fn(),
    placeMembershipOnHold: vi.fn(),
    requestReinstatement: vi.fn(),
  },
}));

import { useAuthStore } from '@/stores/authStore';
import { vettingHoldService } from '@/services/vettingHold.api';

describe('ProfileSettingsPage - Vetting Hold Integration', () => {
  const mockUser = {
    id: 'user-123',
    email: 'test@example.com',
    sceneName: 'TestUser',
    vettingStatus: 3, // Approved
    role: 'Member',
    isActive: true,
  };

  const renderWithProviders = () => {
    return render(
      <BrowserRouter>
        <MantineProvider>
          <Notifications />
          <ProfileSettingsPage />
        </MantineProvider>
      </BrowserRouter>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuthStore).mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      checkAuth: vi.fn(),
    } as any);
  });

  it('shows "Put Membership On Hold" button when user is Approved', async () => {
    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 3,
      statusName: 'Approved',
      canPlaceOnHold: true,
      canRequestReinstatement: false,
      lastStatusChangeDate: null,
    });

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Put Membership On Hold/i })).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /Reinstate My Membership/i })).not.toBeInTheDocument();
  });

  it('shows "Reinstate My Membership" button when user is OnHold', async () => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 5 }, // OnHold
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 5,
      statusName: 'OnHold',
      canPlaceOnHold: false,
      canRequestReinstatement: true,
      lastStatusChangeDate: '2025-11-01T12:00:00Z',
    });

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Reinstate My Membership/i })).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /Put Membership On Hold/i })).not.toBeInTheDocument();
  });

  it('shows "under review" alert when user is FinalReview', async () => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 2 }, // FinalReview
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 2,
      statusName: 'FinalReview',
      canPlaceOnHold: false,
      canRequestReinstatement: false,
      lastStatusChangeDate: '2025-11-01T12:00:00Z',
    });

    renderWithProviders();

    await waitFor(() => {
      expect(screen.getByText(/Your reinstatement request is under review/i)).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /Put Membership On Hold/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Reinstate My Membership/i })).not.toBeInTheDocument();
  });

  it('hides button for other statuses', async () => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 1 }, // Submitted
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 1,
      statusName: 'Submitted',
      canPlaceOnHold: false,
      canRequestReinstatement: false,
      lastStatusChangeDate: null,
    });

    renderWithProviders();

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /Put Membership On Hold/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /Reinstate My Membership/i })).not.toBeInTheDocument();
    });
  });

  it('opens HoldMembershipModal when hold button clicked', async () => {
    const user = userEvent.setup();
    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 3,
      statusName: 'Approved',
      canPlaceOnHold: true,
      canRequestReinstatement: false,
      lastStatusChangeDate: null,
    });

    renderWithProviders();

    const holdButton = await screen.findByRole('button', { name: /Put Membership On Hold/i });
    await user.click(holdButton);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Put Membership On Hold/i })).toBeInTheDocument();
    });
  });

  it('opens ReinstateMembershipModal when reinstate button clicked', async () => {
    const user = userEvent.setup();
    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 5 }, // OnHold
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 5,
      statusName: 'OnHold',
      canPlaceOnHold: false,
      canRequestReinstatement: true,
      lastStatusChangeDate: '2025-11-01T12:00:00Z',
    });

    renderWithProviders();

    const reinstateButton = await screen.findByRole('button', { name: /Reinstate My Membership/i });
    await user.click(reinstateButton);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /Request Membership Reinstatement/i })).toBeInTheDocument();
    });
  });

  it('refreshes page after successful hold placement', async () => {
    const user = userEvent.setup();
    const mockRefetch = vi.fn();
    const reloadSpy = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { reload: reloadSpy },
      writable: true,
    });

    vi.mocked(useAuthStore).mockReturnValue({
      user: mockUser,
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 3,
      statusName: 'Approved',
      canPlaceOnHold: true,
      canRequestReinstatement: false,
      lastStatusChangeDate: null,
    });

    vi.mocked(vettingHoldService.placeMembershipOnHold).mockResolvedValue({
      newStatus: 5,
      statusName: 'OnHold',
      changedAt: '2025-11-09T12:00:00Z',
    });

    renderWithProviders();

    const holdButton = await screen.findByRole('button', { name: /Put Membership On Hold/i });
    await user.click(holdButton);

    const textarea = await screen.findByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Test reason');

    const confirmButton = screen.getByRole('button', { name: /Confirm/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(mockRefetch).toHaveBeenCalled();
    });
  });

  it('refreshes page after successful reinstatement request', async () => {
    const user = userEvent.setup();
    const mockRefetch = vi.fn();
    const reloadSpy = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { reload: reloadSpy },
      writable: true,
    });

    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 5 }, // OnHold
      isLoading: false,
      error: null,
      refetch: mockRefetch,
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 5,
      statusName: 'OnHold',
      canPlaceOnHold: false,
      canRequestReinstatement: true,
      lastStatusChangeDate: '2025-11-01T12:00:00Z',
    });

    vi.mocked(vettingHoldService.requestReinstatement).mockResolvedValue({
      newStatus: 2,
      statusName: 'FinalReview',
      changedAt: '2025-11-09T12:00:00Z',
    });

    renderWithProviders();

    const reinstateButton = await screen.findByRole('button', { name: /Reinstate My Membership/i });
    await user.click(reinstateButton);

    const textarea = await screen.findByPlaceholderText(/Please explain why/i);
    await user.type(textarea, 'Ready to return');

    const confirmButton = screen.getByRole('button', { name: /Request Reinstatement/i });
    await user.click(confirmButton);

    await waitFor(() => {
      expect(mockRefetch).toHaveBeenCalled();
    });
  });

  it('handles API errors gracefully', async () => {
    const user = userEvent.setup();
    vi.mocked(vettingHoldService.getHoldStatus).mockRejectedValue(new Error('API Error'));

    renderWithProviders();

    // Should not crash, should handle error gracefully
    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /Put Membership On Hold/i })).not.toBeInTheDocument();
    });
  });

  it('shows loading state while fetching hold status', () => {
    vi.mocked(vettingHoldService.getHoldStatus).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 1000))
    );

    renderWithProviders();

    // Should not show buttons while loading
    expect(screen.queryByRole('button', { name: /Put Membership On Hold/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Reinstate My Membership/i })).not.toBeInTheDocument();
  });

  it('displays last status change date when available', async () => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: { ...mockUser, vettingStatus: 5 }, // OnHold
      isLoading: false,
      error: null,
      refetch: vi.fn(),
    } as any);

    vi.mocked(vettingHoldService.getHoldStatus).mockResolvedValue({
      vettingStatus: 5,
      statusName: 'OnHold',
      canPlaceOnHold: false,
      canRequestReinstatement: true,
      lastStatusChangeDate: '2025-11-01T12:00:00Z',
    });

    renderWithProviders();

    await waitFor(() => {
      // Should display last status change date somewhere on the page
      expect(screen.getByText(/2025-11-01/i)).toBeInTheDocument();
    });
  });
});
