/**
 * useMenuVisibility Hook Tests
 * Tests business logic for conditional "How to Join" menu visibility
 */
import { renderHook } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useMenuVisibility } from './useMenuVisibility';
import type { MyApplicationStatusResponse, VettingStatus } from '../types/vettingStatus';

// Mock dependencies
vi.mock('./useVettingStatus', () => ({
  useVettingStatus: vi.fn()
}));

vi.mock('../../../stores/authStore', () => ({
  useIsAuthenticated: vi.fn()
}));

import { useVettingStatus } from './useVettingStatus';
import { useIsAuthenticated } from '../../../stores/authStore';

describe('useMenuVisibility', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('unauthenticated users', () => {
    it('should show menu when user is not authenticated', () => {
      vi.mocked(useIsAuthenticated).mockReturnValue(false);
      vi.mocked(useVettingStatus).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: null
      } as any);

      const { result } = renderHook(() => useMenuVisibility());

      expect(result.current.shouldShow).toBe(true);
      expect(result.current.reason).toBe('User not authenticated - show join option');
    });
  });

  describe('authenticated users without application', () => {
    it('should show menu when user has no application', () => {
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useVettingStatus).mockReturnValue({
        data: {
          hasApplication: false,
          application: null
        },
        isLoading: false,
        error: null
      } as any);

      const { result } = renderHook(() => useMenuVisibility());

      expect(result.current.shouldShow).toBe(true);
      expect(result.current.reason).toBe('No application exists - show join option');
    });
  });

  describe('authenticated users with applications - show menu statuses', () => {
    const showStatuses: VettingStatus[] = [
      'UnderReview',
      'InterviewApproved',
      'FinalReview',
      'Withdrawn'
    ];

    showStatuses.forEach((status) => {
      it(`should show menu for status: ${status}`, () => {
        vi.mocked(useIsAuthenticated).mockReturnValue(true);
        vi.mocked(useVettingStatus).mockReturnValue({
          data: {
            hasApplication: true,
            application: {
              applicationId: 'test-id',
              applicationNumber: 'V-2025-001',
              status,
              statusDescription: 'Test',
              submittedAt: '2025-10-04T00:00:00Z',
              lastUpdated: '2025-10-04T00:00:00Z',
              nextSteps: null,
              estimatedDaysRemaining: null
            }
          },
          isLoading: false,
          error: null
        } as any);

        const { result } = renderHook(() => useMenuVisibility());

        expect(result.current.shouldShow).toBe(true);
        expect(result.current.reason).toBe(`Status ${status} allows menu visibility`);
      });
    });
  });

  describe('authenticated users with applications - hide menu statuses', () => {
    const hideStatuses: VettingStatus[] = ['OnHold', 'Approved', 'Denied'];

    hideStatuses.forEach((status) => {
      it(`should hide menu for status: ${status}`, () => {
        vi.mocked(useIsAuthenticated).mockReturnValue(true);
        vi.mocked(useVettingStatus).mockReturnValue({
          data: {
            hasApplication: true,
            application: {
              applicationId: 'test-id',
              applicationNumber: 'V-2025-001',
              status,
              statusDescription: 'Test',
              submittedAt: '2025-10-04T00:00:00Z',
              lastUpdated: '2025-10-04T00:00:00Z',
              nextSteps: null,
              estimatedDaysRemaining: null
            }
          },
          isLoading: false,
          error: null
        } as any);

        const { result } = renderHook(() => useMenuVisibility());

        expect(result.current.shouldShow).toBe(false);
        expect(result.current.reason).toBe(`Status ${status} requires menu to be hidden`);
      });
    });
  });

  describe('loading and error states', () => {
    it('should show menu while loading (fail-open)', () => {
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useVettingStatus).mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null
      } as any);

      const { result } = renderHook(() => useMenuVisibility());

      expect(result.current.shouldShow).toBe(true);
      expect(result.current.reason).toBe('Loading - show menu (fail-open)');
    });

    it('should show menu on error (fail-open)', () => {
      vi.mocked(useIsAuthenticated).mockReturnValue(true);
      vi.mocked(useVettingStatus).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('API Error')
      } as any);

      const { result } = renderHook(() => useMenuVisibility());

      expect(result.current.shouldShow).toBe(true);
      expect(result.current.reason).toBe('Error fetching status - show menu (fail-open)');
    });
  });
});
