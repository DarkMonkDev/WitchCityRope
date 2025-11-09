/**
 * Unit tests for useTeacherProfiles hook
 * Tests teacher profile fetching, parallel requests, and error handling
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../../test/setup';
import { useTeacherProfiles } from '../useTeacherProfiles';
import type { components } from '@witchcityrope/shared-types';

type UserProfileDto = components['schemas']['UserProfileDto'];

describe('useTeacherProfiles', () => {
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

  it('should fetch multiple teacher profiles successfully', async () => {
    const mockProfiles: UserProfileDto[] = [
      {
        userId: 'teacher1',
        email: 'teacher1@test.com',
        sceneName: 'Teacher One',
        firstName: 'First',
        lastName: 'Teacher',
        bio: 'Bio for teacher one',
        pronouns: 'they/them',
        discordName: 'teacher1#1234',
      },
      {
        userId: 'teacher2',
        email: 'teacher2@test.com',
        sceneName: 'Teacher Two',
        firstName: 'Second',
        lastName: 'Teacher',
        bio: 'Bio for teacher two',
        pronouns: 'she/her',
        discordName: 'teacher2#5678',
      },
    ];

    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        return HttpResponse.json({
          success: true,
          data: mockProfiles[0],
          message: 'Profile retrieved successfully',
        });
      }),
      http.get('http://localhost:5655/api/users/teacher2/profile', () => {
        return HttpResponse.json({
          success: true,
          data: mockProfiles[1],
          message: 'Profile retrieved successfully',
        });
      })
    );

    const { result } = renderHook(() => useTeacherProfiles(['teacher1', 'teacher2']), {
      wrapper: createWrapper(),
    });

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toEqual(mockProfiles);
    expect(result.current.data).toHaveLength(2);
  });

  it('should return empty array when teacherIds is undefined', async () => {
    const { result } = renderHook(() => useTeacherProfiles(undefined), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toBeUndefined();
  });

  it('should return empty array when teacherIds is empty', async () => {
    const { result } = renderHook(() => useTeacherProfiles([]), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toBeUndefined();
  });

  it('should filter out null results when a profile fails to load', async () => {
    const mockProfile: UserProfileDto = {
      userId: 'teacher1',
      email: 'teacher1@test.com',
      sceneName: 'Teacher One',
      firstName: 'First',
      lastName: 'Teacher',
      bio: 'Bio for teacher one',
      pronouns: 'they/them',
      discordName: 'teacher1#1234',
    };

    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        return HttpResponse.json({
          success: true,
          data: mockProfile,
          message: 'Profile retrieved successfully',
        });
      }),
      http.get('http://localhost:5655/api/users/teacher2/profile', () => {
        return HttpResponse.json(
          {
            success: false,
            data: null,
            error: 'Profile not found',
            message: 'User profile does not exist',
          },
          { status: 404 }
        );
      })
    );

    const { result } = renderHook(() => useTeacherProfiles(['teacher1', 'teacher2']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Should only return the successful profile
    expect(result.current.data).toHaveLength(1);
    expect(result.current.data?.[0]).toEqual(mockProfile);
  });

  it('should handle all profiles failing gracefully', async () => {
    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        return HttpResponse.json(
          {
            success: false,
            data: null,
            error: 'Profile not found',
          },
          { status: 404 }
        );
      }),
      http.get('http://localhost:5655/api/users/teacher2/profile', () => {
        return HttpResponse.json(
          {
            success: false,
            data: null,
            error: 'Profile not found',
          },
          { status: 404 }
        );
      })
    );

    const { result } = renderHook(() => useTeacherProfiles(['teacher1', 'teacher2']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Should return empty array when all profiles fail
    expect(result.current.data).toEqual([]);
  });

  it('should fetch single teacher profile', async () => {
    const mockProfile: UserProfileDto = {
      userId: 'teacher1',
      email: 'teacher1@test.com',
      sceneName: 'Solo Teacher',
      firstName: 'Solo',
      lastName: 'Teacher',
      bio: 'Teaching rope bondage for 10 years',
      pronouns: 'he/him',
      discordName: 'solo#9999',
    };

    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        return HttpResponse.json({
          success: true,
          data: mockProfile,
          message: 'Profile retrieved successfully',
        });
      })
    );

    const { result } = renderHook(() => useTeacherProfiles(['teacher1']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.data).toHaveLength(1);
    expect(result.current.data?.[0]).toEqual(mockProfile);
  });

  it('should cache results for 10 minutes', async () => {
    const mockProfile: UserProfileDto = {
      userId: 'teacher1',
      email: 'teacher1@test.com',
      sceneName: 'Cached Teacher',
      firstName: 'Cached',
      lastName: 'Teacher',
      bio: 'Cached bio',
      pronouns: 'they/them',
      discordName: 'cached#1111',
    };

    let fetchCount = 0;
    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        fetchCount++;
        return HttpResponse.json({
          success: true,
          data: mockProfile,
          message: 'Profile retrieved successfully',
        });
      })
    );

    // First render - should fetch
    const { result: result1 } = renderHook(() => useTeacherProfiles(['teacher1']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result1.current.isLoading).toBe(false);
    });

    expect(fetchCount).toBe(1);

    // Second render with same QueryClient - should use cache
    const { result: result2 } = renderHook(() => useTeacherProfiles(['teacher1']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result2.current.isLoading).toBe(false);
    });

    // Should still be 1 because second call used cache
    expect(fetchCount).toBe(1);
  });

  it('should log error when profile fetch fails', async () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    server.use(
      http.get('http://localhost:5655/api/users/teacher1/profile', () => {
        return HttpResponse.json(
          {
            success: false,
            data: null,
            error: 'Server error',
          },
          { status: 500 }
        );
      })
    );

    const { result } = renderHook(() => useTeacherProfiles(['teacher1']), {
      wrapper: createWrapper(),
    });

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(consoleErrorSpy).toHaveBeenCalledWith(
      expect.stringContaining('Failed to fetch teacher profile for teacher1'),
      expect.anything()
    );

    consoleErrorSpy.mockRestore();
  });
});
