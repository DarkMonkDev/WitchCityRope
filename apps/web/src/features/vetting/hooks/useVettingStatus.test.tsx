/**
 * Tests for useVettingStatus hook
 * Tests data fetching, caching, and error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useVettingStatus } from './useVettingStatus';
import * as authStore from '../../../stores/authStore';
import type { MyApplicationStatusResponse } from '../types/vettingStatus';

// Mock the auth store
vi.mock('../../../stores/authStore', () => ({
  useIsAuthenticated: vi.fn()
}));

// Mock fetch
const mockFetch = vi.fn();
global.fetch = mockFetch as any;

describe('useVettingStatus', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    // Create fresh QueryClient for EACH test to ensure cache isolation
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false, // Disable retries in tests
          gcTime: 0 // Disable caching between tests
        }
      }
    });

    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  afterEach(() => {
    // Clear all queries from cache to prevent test pollution
    queryClient.clear();
    vi.restoreAllMocks();
  });

  // Create wrapper for React Query
  const createWrapper = () => {
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };

  describe('when user is not authenticated', () => {
    it('should not fetch status', async () => {
      // Arrange
      vi.mocked(authStore.useIsAuthenticated).mockReturnValue(false);

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.data).toBeUndefined();
        expect(result.current.isLoading).toBe(false);
        expect(mockFetch).not.toHaveBeenCalled();
      });
    });
  });

  describe('when user is authenticated', () => {
    beforeEach(() => {
      vi.mocked(authStore.useIsAuthenticated).mockReturnValue(true);
    });

    it('should fetch status successfully for user without application', async () => {
      // Arrange
      const mockResponse: MyApplicationStatusResponse = {
        hasApplication: false,
        application: null
      };

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          data: mockResponse,
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockResponse);
      expect(result.current.data?.hasApplication).toBe(false);
      expect(result.current.data?.application).toBeNull();
      expect(mockFetch).toHaveBeenCalledWith('/api/vetting/status', {
        credentials: 'include'
      });
    });

    it('should fetch status successfully for user with UnderReview application', async () => {
      // Arrange
      const mockResponse: MyApplicationStatusResponse = {
        hasApplication: true,
        application: {
          applicationId: '123e4567-e89b-12d3-a456-426614174000',
          applicationNumber: 'a1b2c3d4',
          status: 'UnderReview',
          statusDescription: 'Application submitted, awaiting review',
          submittedAt: '2025-10-04T12:00:00Z',
          lastUpdated: '2025-10-04T12:00:00Z',
          nextSteps: 'No action needed - we\'ll contact you via email',
          estimatedDaysRemaining: 14
        }
      };

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          data: mockResponse,
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data).toEqual(mockResponse);
      expect(result.current.data?.hasApplication).toBe(true);
      expect(result.current.data?.application?.status).toBe('UnderReview');
      expect(result.current.data?.application?.estimatedDaysRemaining).toBe(14);
    });

    it('should fetch status successfully for user with Approved application', async () => {
      // Arrange
      const mockResponse: MyApplicationStatusResponse = {
        hasApplication: true,
        application: {
          applicationId: '123e4567-e89b-12d3-a456-426614174001',
          applicationNumber: 'a1b2c3d5',
          status: 'Approved',
          statusDescription: 'Vetting approved - you are a vetted member',
          submittedAt: '2025-09-20T10:00:00Z',
          lastUpdated: '2025-10-01T15:30:00Z',
          nextSteps: 'Welcome! Access member resources via dashboard',
          estimatedDaysRemaining: null
        }
      };

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          data: mockResponse,
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act
      const { result} = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      expect(result.current.data?.application?.status).toBe('Approved');
      expect(result.current.data?.application?.estimatedDaysRemaining).toBeNull();
    });

    it('should handle 401 Unauthorized error (fail-open)', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        json: async () => ({
          success: false,
          error: 'Unauthorized',
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      // Fail-open: error should not crash, data should be undefined
      expect(result.current.data).toBeUndefined();
      expect(result.current.error).toBeDefined();
    });

    it('should handle 500 Server Error (fail-open)', async () => {
      // Arrange
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({
          success: false,
          error: 'Internal Server Error',
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.data).toBeUndefined();
      expect(result.current.error).toBeDefined();
    });

    it('should handle network timeout error (fail-open)', async () => {
      // Arrange
      mockFetch.mockRejectedValueOnce(
        new Error('Network timeout')
      );

      // Act
      const { result } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      // Assert
      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      expect(result.current.data).toBeUndefined();
      expect(result.current.error).toBeDefined();
    });

    it('should cache data for 5 minutes (staleTime)', async () => {
      // Arrange
      const mockResponse: MyApplicationStatusResponse = {
        hasApplication: false,
        application: null
      };

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          data: mockResponse,
          timestamp: '2025-10-04T00:00:00Z'
        })
      });

      // Act - First render
      const { result, rerender } = renderHook(() => useVettingStatus(), {
        wrapper: createWrapper()
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Clear the mock call count
      mockFetch.mockClear();

      // Act - Rerender (should use cache)
      rerender();

      // Assert - Should not refetch immediately (cache hit)
      expect(mockFetch).not.toHaveBeenCalled();
      expect(result.current.data).toEqual(mockResponse);
    });
  });

  describe('all vetting status values', () => {
    beforeEach(() => {
      vi.mocked(authStore.useIsAuthenticated).mockReturnValue(true);
    });

    const testStatuses = [
      'UnderReview',
      'InterviewApproved',
      'FinalReview',
      'Approved',
      'Denied',
      'OnHold',
      'Withdrawn'
    ] as const;

    testStatuses.forEach(status => {
      it(`should handle ${status} status correctly`, async () => {
        // Arrange
        const mockResponse: MyApplicationStatusResponse = {
          hasApplication: true,
          application: {
            applicationId: '123e4567-e89b-12d3-a456-426614174000',
            applicationNumber: 'test1234',
            status,
            statusDescription: `Status: ${status}`,
            submittedAt: '2025-10-04T12:00:00Z',
            lastUpdated: '2025-10-04T12:00:00Z',
            nextSteps: 'Test next steps',
            estimatedDaysRemaining: status === 'Approved' ? null : 7
          }
        };

        mockFetch.mockResolvedValue({
          ok: true,
          json: async () => ({
            success: true,
            data: mockResponse,
            timestamp: '2025-10-04T00:00:00Z'
          })
        });

        // Act
        const { result } = renderHook(() => useVettingStatus(), {
          wrapper: createWrapper()
        });

        // Assert
        await waitFor(() => {
          expect(result.current.isSuccess).toBe(true);
        });

        expect(result.current.data?.application?.status).toBe(status);
      });
    });
  });
});
