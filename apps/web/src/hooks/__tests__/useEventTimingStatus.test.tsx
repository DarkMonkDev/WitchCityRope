import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useEventTimingStatus } from '../useEventTimingStatus';

/**
 * Unit Tests for useEventTimingStatus Hook
 *
 * This hook calculates event timing status based on:
 * - Event start date/time
 * - Pre-start buffer setting (minutes before event when actions are disabled)
 *
 * Business Rules:
 * 1. Past events: All participation actions disabled
 * 2. Within buffer window: Cancellations and new registrations disabled
 * 3. Outside buffer window: All actions allowed
 * 4. Zero buffer: Allow actions until event starts
 *
 * Created: 2025-11-09
 */

// Mock the settings API
vi.mock('../../services/settings.api', () => ({
  getPublicSettings: vi.fn(),
}));

import { getPublicSettings } from '../../services/settings.api';

describe('useEventTimingStatus', () => {
  const mockNow = new Date('2025-11-09T14:00:00Z'); // 2:00 PM UTC

  let queryClient: QueryClient;

  beforeEach(() => {
    // Mock Date.now() to return consistent time
    vi.useFakeTimers();
    vi.setSystemTime(mockNow);

    // Create a new QueryClient for each test
    queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
        },
      },
    });

    // Reset mocks
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
    queryClient.clear();
  });

  // Helper to render hook with QueryClient wrapper
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  describe('Past Event Detection', () => {
    it('should identify event that has already started', async () => {
      const eventStartDateTime = '2025-11-09T13:00:00Z'; // 1 hour ago

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isPastEvent).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
      expect(result.current!.statusMessage).toContain('already started');
    });

    it('should identify event that started exactly at current time', async () => {
      const eventStartDateTime = '2025-11-09T14:00:00Z'; // Now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '0',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isPastEvent).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });

    it('should not identify future event as past event', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z'; // 1 hour from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '0',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isPastEvent).toBe(false);
    });
  });

  describe('Buffer Window Calculation', () => {
    it('should correctly identify event within 30-minute buffer window', async () => {
      const eventStartDateTime = '2025-11-09T14:25:00Z'; // 25 minutes from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
      expect(result.current!.statusMessage).toContain('Cancellations closed');
    });

    it('should correctly identify event outside buffer window', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z'; // 60 minutes from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(false);
      expect(result.current!.canRegister).toBe(true);
      expect(result.current!.canCancel).toBe(true);
    });

    it('should calculate cutoff time correctly', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z'; // 3:00 PM

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Expected cutoff: 3:00 PM - 30 minutes = 2:30 PM
      const expectedCutoff = new Date('2025-11-09T14:30:00Z');
      expect(result.current!.cutoffTime).toEqual(expectedCutoff);
    });

    it('should calculate cutoff time correctly for 60-minute buffer', async () => {
      const eventStartDateTime = '2025-11-09T16:00:00Z'; // 4:00 PM

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '60',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Expected cutoff: 4:00 PM - 60 minutes = 3:00 PM
      const expectedCutoff = new Date('2025-11-09T15:00:00Z');
      expect(result.current!.cutoffTime).toEqual(expectedCutoff);
    });
  });

  describe('Edge Cases', () => {
    it('should handle event starting in 1 minute with 30-minute buffer', async () => {
      const eventStartDateTime = '2025-11-09T14:01:00Z'; // 1 minute from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });

    it('should handle event at exact cutoff boundary (just inside buffer)', async () => {
      const eventStartDateTime = '2025-11-09T14:30:00Z'; // 30 minutes from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Current time: 2:00 PM, Cutoff: 2:00 PM, Event: 2:30 PM
      // At exact cutoff boundary, should be within buffer
      expect(result.current!.isWithinBuffer).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });

    it('should handle event at exact cutoff boundary (just outside buffer)', async () => {
      const eventStartDateTime = '2025-11-09T14:31:00Z'; // 31 minutes from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Current time: 2:00 PM, Cutoff: 2:01 PM, Event: 2:31 PM
      // Just outside cutoff boundary, should NOT be within buffer
      expect(result.current!.isWithinBuffer).toBe(false);
      expect(result.current!.canRegister).toBe(true);
      expect(result.current!.canCancel).toBe(true);
    });
  });

  describe('Zero Buffer (Allow Until Start)', () => {
    it('should allow actions until event starts with zero buffer', async () => {
      const eventStartDateTime = '2025-11-09T14:05:00Z'; // 5 minutes from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '0',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(false);
      expect(result.current!.canRegister).toBe(true);
      expect(result.current!.canCancel).toBe(true);
    });

    it('should calculate cutoff time same as event start with zero buffer', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z';

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '0',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.cutoffTime).toEqual(new Date(eventStartDateTime));
    });

    it('should disable actions at event start time with zero buffer', async () => {
      const eventStartDateTime = '2025-11-09T14:00:00Z'; // Now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '0',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isPastEvent).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });
  });

  describe('Missing or Invalid Settings', () => {
    it('should return null when settings not loaded', () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z';

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      // Before settings load, should return null
      expect(result.current).toBeNull();
    });

    it('should return null when event date missing', async () => {
      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(undefined), {
        wrapper,
      });

      await waitFor(() => expect(getPublicSettings).toHaveBeenCalled());

      // Should return null for missing event date
      expect(result.current).toBeNull();
    });
  });

  describe('Status Messages', () => {
    it('should provide appropriate message for past event', async () => {
      const eventStartDateTime = '2025-11-09T13:00:00Z'; // Past

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.statusMessage).toMatch(/already started|has started/i);
    });

    it('should provide appropriate message for event within buffer', async () => {
      const eventStartDateTime = '2025-11-09T14:20:00Z'; // Within buffer

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.statusMessage).toMatch(/cancellation/i);
    });

    it('should provide empty message for open registration', async () => {
      const eventStartDateTime = '2025-11-09T16:00:00Z'; // Outside buffer

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.statusMessage).toBe('');
    });
  });

  describe('Large Buffer Values', () => {
    it('should handle 24-hour (1440 minutes) buffer correctly', async () => {
      const eventStartDateTime = '2025-11-10T14:00:00Z'; // 24 hours from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '1440',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Current time: 2:00 PM today, Cutoff: 2:00 PM today, Event: 2:00 PM tomorrow
      expect(result.current!.isWithinBuffer).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });

    it('should handle 7-day (10080 minutes) buffer correctly', async () => {
      const eventStartDateTime = '2025-11-16T14:00:00Z'; // 7 days from now

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '10080',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      // Current time: Nov 9, Cutoff: Nov 9, Event: Nov 16
      expect(result.current!.isWithinBuffer).toBe(true);
      expect(result.current!.canRegister).toBe(false);
      expect(result.current!.canCancel).toBe(false);
    });
  });

  describe('Timezone Handling', () => {
    it('should handle UTC event times correctly', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00.000Z'; // Explicit UTC

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(false);
      expect(result.current!.canRegister).toBe(true);
    });

    it('should handle ISO 8601 format without milliseconds', async () => {
      const eventStartDateTime = '2025-11-09T15:00:00Z'; // ISO 8601 without ms

      vi.mocked(getPublicSettings).mockResolvedValue({
        preStartBufferMinutes: '30',
        eventTimeZone: 'America/New_York',
      });

      const { result } = renderHook(() => useEventTimingStatus(eventStartDateTime), {
        wrapper,
      });

      await waitFor(() => expect(result.current).not.toBeNull());

      expect(result.current!.isWithinBuffer).toBe(false);
      expect(result.current!.canRegister).toBe(true);
    });
  });
});
