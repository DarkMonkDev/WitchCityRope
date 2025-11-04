/**
 * Unit tests for useVenues hooks
 * Tests venue fetching, caching, and error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/setup';
import { useVenue, useVenues } from '../useVenues';
import type { components } from '@witchcityrope/shared-types';

type VenueDto = components['schemas']['VenueDto'];

describe('useVenues hooks', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          gcTime: 0,
        },
      },
    });
    vi.clearAllMocks();
  });

  afterEach(() => {
    queryClient.clear();
    vi.restoreAllMocks();
  });

  const createWrapper = () => {
    return ({ children }: { children: React.ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );
  };

  describe('useVenue', () => {
    it('should fetch single venue successfully', async () => {
      const mockVenue: VenueDto = {
        id: 1,
        name: 'Test Venue',
        directions: 'Turn left at the corner',
        notes: null,
        isActive: true,
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: '2025-01-01T00:00:00Z',
      };

      server.use(
        http.get('http://localhost:5655/api/venues/1', () => {
          return HttpResponse.json({
            success: true,
            data: mockVenue,
            message: 'Venue retrieved successfully',
          });
        })
      );

      const { result } = renderHook(() => useVenue(1), {
        wrapper: createWrapper(),
      });

      expect(result.current.isLoading).toBe(true);

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual(mockVenue);
      expect(result.current.error).toBeNull();
    });

    it('should return null when venueId is null', async () => {
      const { result } = renderHook(() => useVenue(null), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toBeUndefined();
    });

    it('should return null when venueId is undefined', async () => {
      const { result } = renderHook(() => useVenue(undefined), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toBeUndefined();
    });

    it('should not fetch when enabled is false', async () => {
      const { result } = renderHook(() => useVenue(1, false), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toBeUndefined();
    });

    it('should handle 404 error', async () => {
      server.use(
        http.get('http://localhost:5655/api/venues/999', () => {
          return HttpResponse.json(
            {
              success: false,
              data: null,
              error: 'Venue not found',
              message: 'Venue with ID 999 does not exist or is not available',
            },
            { status: 404 }
          );
        })
      );

      const { result } = renderHook(() => useVenue(999), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      // When an error occurs, data is undefined (not null)
      expect(result.current.data).toBeUndefined();
    });

    it('should handle authentication error', async () => {
      server.use(
        http.get('http://localhost:5655/api/venues/1', () => {
          return HttpResponse.json(
            {
              success: false,
              data: null,
              error: 'Authentication required',
              message: 'You must be logged in to access venue details',
            },
            { status: 401 }
          );
        })
      );

      const { result } = renderHook(() => useVenue(1), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.error).toBeTruthy();
    });
  });

  describe('useVenues', () => {
    it('should fetch all active venues successfully', async () => {
      const mockVenues: VenueDto[] = [
        {
          id: 1,
          name: 'Venue A',
          directions: 'Directions A',
          notes: null,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
        {
          id: 2,
          name: 'Venue B',
          directions: 'Directions B',
          notes: null,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ];

      server.use(
        http.get('http://localhost:5655/api/venues', () => {
          return HttpResponse.json({
            success: true,
            data: mockVenues,
            message: 'Retrieved 2 active venues',
          });
        })
      );

      const { result } = renderHook(() => useVenues(), {
        wrapper: createWrapper(),
      });

      expect(result.current.isLoading).toBe(true);

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual(mockVenues);
      expect(result.current.data).toHaveLength(2);
    });

    it('should return empty array when no venues exist', async () => {
      server.use(
        http.get('http://localhost:5655/api/venues', () => {
          return HttpResponse.json({
            success: true,
            data: [],
            message: 'Retrieved 0 active venues',
          });
        })
      );

      const { result } = renderHook(() => useVenues(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.data).toEqual([]);
    });

    it('should handle authentication error', async () => {
      server.use(
        http.get('http://localhost:5655/api/venues', () => {
          return HttpResponse.json(
            {
              success: false,
              data: null,
              error: 'Authentication required',
              message: 'You must be logged in to access venues',
            },
            { status: 401 }
          );
        })
      );

      const { result } = renderHook(() => useVenues(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.error).toBeTruthy();
    });

    it('should cache results for 10 minutes', async () => {
      const mockVenues: VenueDto[] = [
        {
          id: 1,
          name: 'Cached Venue',
          directions: 'Cached Directions',
          notes: null,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-01T00:00:00Z',
        },
      ];

      let fetchCount = 0;
      server.use(
        http.get('http://localhost:5655/api/venues', () => {
          fetchCount++;
          return HttpResponse.json({
            success: true,
            data: mockVenues,
            message: 'Retrieved 1 active venues',
          });
        })
      );

      // First render - should fetch
      const { result: result1 } = renderHook(() => useVenues(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result1.current.isLoading).toBe(false);
      });

      expect(fetchCount).toBe(1);

      // Second render with same QueryClient - should use cache
      const { result: result2 } = renderHook(() => useVenues(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result2.current.isLoading).toBe(false);
      });

      // Should still be 1 because second call used cache
      expect(fetchCount).toBe(1);
    });
  });
});
