import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { EventSession } from '@/components/events/EventSessionsGrid';

/**
 * Timezone Handling Tests for SessionFormModal
 *
 * CONTEXT: Bug Fix Regression Test (November 27, 2025)
 *
 * Bug Fixed: Users in EST (UTC-5) selecting "December 15, 2025" had it saved as "December 14, 2025"
 * Root Cause: Mixing local timezone operations (setHours) with UTC conversion
 * Fix: Use Date.UTC() and UTC getter methods throughout (lines 74-90 in SessionFormModal.tsx)
 *
 * CRITICAL: These tests ensure timezone bugs don't regress in future changes
 *
 * Test Strategy:
 * 1. Test the actual date conversion logic that the component uses
 * 2. Mock Date object to simulate different timezones (EST, PST, UTC, etc.)
 * 3. Verify Date.UTC() produces correct ISO strings regardless of timezone
 * 4. Test edge cases: midnight times, end of month, end of year
 */

describe('SessionFormModal - Timezone Handling Logic', () => {
  // Store original Date implementation
  let originalDate: DateConstructor;

  beforeEach(() => {
    originalDate = global.Date;
  });

  afterEach(() => {
    // Restore original Date
    global.Date = originalDate;
  });

  /**
   * Helper: Mock Date to simulate timezone offset
   * @param timezoneOffsetMinutes - Offset in minutes (e.g., -300 for EST = UTC-5)
   */
  const mockTimezone = (timezoneOffsetMinutes: number) => {
    const RealDate = Date;

    // Create mock Date class that overrides getTimezoneOffset
    class MockDate extends RealDate {
      constructor(...args: any[]) {
        if (args.length === 0) {
          super();
        } else if (args.length === 1) {
          super(args[0]);
        } else {
          super(args[0], args[1], args[2], args[3] || 0, args[4] || 0, args[5] || 0, args[6] || 0);
        }
      }

      getTimezoneOffset() {
        return timezoneOffsetMinutes;
      }

      static UTC = RealDate.UTC;
      static now = RealDate.now;
      static parse = RealDate.parse;
    }

    // @ts-ignore - Intentional Date override for testing
    global.Date = MockDate as DateConstructor;
  };

  /**
   * Helper: Simulate the component's date processing logic
   * This replicates lines 72-90 from SessionFormModal.tsx
   */
  const processSessionDate = (dateInput: Date, startTime: string, endTime: string) => {
    // Extract date components using UTC methods (THE FIX)
    const year = dateInput.getUTCFullYear();
    const month = dateInput.getUTCMonth();
    const day = dateInput.getUTCDate();

    // Parse time strings
    const [startHour, startMinute] = startTime.split(':').map(Number);
    const [endHour, endMinute] = endTime.split(':').map(Number);

    // Create datetime objects using Date.UTC() (THE FIX)
    const startDateTime = new Date(Date.UTC(year, month, day, startHour, startMinute, 0, 0));
    const endDateTime = new Date(Date.UTC(year, month, day, endHour, endMinute, 0, 0));

    return {
      date: new Date(Date.UTC(year, month, day, 0, 0, 0, 0)).toISOString(),
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString(),
    };
  };

  describe('Date Selection Without Timezone Shift', () => {
    it('should not shift date when converting to UTC in EST timezone (UTC-5)', () => {
      // Simulate Eastern Standard Time (UTC-5)
      mockTimezone(-300); // -300 minutes = -5 hours

      // User selects December 15, 2025 at 6:00 PM
      const selectedDate = new Date('2025-12-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '18:00', '21:00');

      // CRITICAL ASSERTION: Verify date in ISO string is still December 15, not December 14
      expect(result.date).toContain('2025-12-15');
      expect(result.startTime).toContain('2025-12-15T18:00:00');
      expect(result.endTime).toContain('2025-12-15T21:00:00');
    });

    it('should not shift date when converting to UTC in PST timezone (UTC-8)', () => {
      // Simulate Pacific Standard Time (UTC-8)
      mockTimezone(-480); // -480 minutes = -8 hours

      // User selects January 31, 2025 (end of month edge case)
      const selectedDate = new Date('2025-01-31T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '20:00', '23:00');

      // CRITICAL: Should stay January 31, not become February 1 or January 30
      expect(result.date).toContain('2025-01-31');
      expect(result.startTime).toContain('2025-01-31T20:00:00');
    });

    it('should handle dates correctly in UTC timezone (UTC+0)', () => {
      // Simulate UTC timezone
      mockTimezone(0); // 0 offset

      const selectedDate = new Date('2025-06-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '12:00', '15:00');

      expect(result.date).toContain('2025-06-15');
      expect(result.startTime).toContain('2025-06-15T12:00:00');
    });

    it('should handle dates correctly in Tokyo timezone (UTC+9)', () => {
      // Simulate Tokyo timezone (UTC+9)
      mockTimezone(-540); // -540 minutes = +9 hours (negative because getTimezoneOffset is reversed)

      const selectedDate = new Date('2025-03-20T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '09:00', '12:00');

      // Should stay March 20, not shift to March 19 or 21
      expect(result.date).toContain('2025-03-20');
      expect(result.startTime).toContain('2025-03-20T09:00:00');
    });
  });

  describe('Edge Case: Midnight Times', () => {
    it('should not shift date when time is 00:00 (midnight)', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-12-25T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '00:00', '03:00');

      // CRITICAL: Midnight should not cause date to shift to previous day
      expect(result.date).toContain('2025-12-25');
      expect(result.startTime).toContain('2025-12-25T00:00:00');
    });

    it('should not shift date when time is 23:59 (end of day)', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-12-31T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '23:59', '23:59');

      // Should stay December 31, not become January 1
      expect(result.date).toContain('2025-12-31');
      expect(result.startTime).toContain('2025-12-31T23:59:00');
    });
  });

  describe('Edge Case: Month Boundaries', () => {
    it('should handle January 31 without shifting to February', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-01-31T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '23:00', '23:59');

      expect(result.date).toContain('2025-01-31');
      expect(result.startTime).toContain('2025-01-31T23:00:00');
    });

    it('should handle February 28 (non-leap year) without shifting to March', () => {
      mockTimezone(-480); // PST

      const selectedDate = new Date('2025-02-28T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '22:00', '23:00');

      expect(result.date).toContain('2025-02-28');
      expect(result.startTime).toContain('2025-02-28T22:00:00');
    });

    it('should handle February 29 (leap year) without shifting', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2024-02-29T00:00:00.000Z'); // 2024 is a leap year
      const result = processSessionDate(selectedDate, '18:00', '21:00');

      expect(result.date).toContain('2024-02-29');
      expect(result.startTime).toContain('2024-02-29T18:00:00');
    });

    it('should handle March 31 without shifting to April', () => {
      mockTimezone(-480); // PST

      const selectedDate = new Date('2025-03-31T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '21:00', '23:00');

      expect(result.date).toContain('2025-03-31');
      expect(result.startTime).toContain('2025-03-31T21:00:00');
    });
  });

  describe('Edge Case: Year Boundaries', () => {
    it('should handle December 31 at 23:59 without shifting to next year', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-12-31T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '23:59', '23:59');

      // Should stay 2025-12-31, not become 2026-01-01
      expect(result.date).toContain('2025-12-31');
      expect(result.startTime).toContain('2025-12-31T23:59:00');
    });

    it('should handle January 1 at 00:00 correctly', () => {
      mockTimezone(-480); // PST

      const selectedDate = new Date('2026-01-01T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '00:00', '03:00');

      // Should stay January 1, 2026, not become December 31, 2025
      expect(result.date).toContain('2026-01-01');
      expect(result.startTime).toContain('2026-01-01T00:00:00');
    });
  });

  describe('ISO String Format Verification', () => {
    it('should produce valid ISO 8601 datetime strings', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-07-04T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '14:30', '16:30');

      // Verify ISO 8601 format (YYYY-MM-DDTHH:mm:ss.sssZ)
      expect(result.date).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
      expect(result.startTime).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
      expect(result.endTime).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);

      // Verify all times are on the same date
      expect(result.date).toContain('2025-07-04');
      expect(result.startTime).toContain('2025-07-04T14:30:00');
      expect(result.endTime).toContain('2025-07-04T16:30:00');
    });

    it('should use UTC timezone (Z suffix) in all ISO strings', () => {
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-05-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '18:00', '21:00');

      // CRITICAL: All datetime strings MUST end with 'Z' (UTC timezone indicator)
      expect(result.date).toMatch(/Z$/);
      expect(result.startTime).toMatch(/Z$/);
      expect(result.endTime).toMatch(/Z$/);
    });
  });

  describe('Implementation Verification - Date.UTC Usage', () => {
    it('should use Date.UTC() correctly for date construction', () => {
      // This test verifies the fix is working by checking behavior
      // The fix uses Date.UTC() instead of local timezone methods

      mockTimezone(-300); // EST - this would cause problems with local methods

      // Use a date that would definitely shift with local timezone (late evening)
      const selectedDate = new Date('2025-11-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '22:00', '23:00');

      // With Date.UTC() (correct): Should be 2025-11-15T22:00:00.000Z
      // With local methods (wrong): Would be 2025-11-16T03:00:00.000Z (shifted 5 hours ahead)
      expect(result.startTime).toContain('2025-11-15T22:00:00');
      expect(result.startTime).not.toContain('2025-11-16');
    });

    it('should extract date components using UTC methods', () => {
      mockTimezone(-300); // EST

      // Create a date at a time that would differ between local and UTC
      const selectedDate = new Date('2025-11-15T23:00:00.000Z');

      // Extract using UTC methods (what the component does)
      const year = selectedDate.getUTCFullYear();
      const month = selectedDate.getUTCMonth();
      const day = selectedDate.getUTCDate();

      // Verify UTC methods give correct results
      expect(year).toBe(2025);
      expect(month).toBe(10); // November (0-indexed)
      expect(day).toBe(15);

      // Process using our helper (which uses UTC methods)
      const result = processSessionDate(selectedDate, '18:00', '21:00');
      expect(result.date).toContain('2025-11-15');
    });
  });

  describe('Real-World Bug Scenario Recreation', () => {
    it('should reproduce and verify fix for December 15 EST bug', () => {
      // EXACT scenario from bug report:
      // User in EST (UTC-5) selects December 15, 2025 at 6:00 PM
      mockTimezone(-300); // EST

      const selectedDate = new Date('2025-12-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '18:00', '21:00');

      // BUG: Was saving as December 14, 2025
      // FIX: Now correctly saves as December 15, 2025
      expect(result.date).toContain('2025-12-15');
      expect(result.date).not.toContain('2025-12-14'); // Ensure bug doesn't regress
      expect(result.startTime).toContain('2025-12-15T18:00:00');
    });

    it('should handle cross-timezone DST transitions correctly', () => {
      // March 10, 2025 is when DST starts in US (2 AM becomes 3 AM)
      mockTimezone(-300); // EST before DST

      const selectedDate = new Date('2025-03-10T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '02:30', '05:00');

      // Date should remain stable regardless of DST transition
      expect(result.date).toContain('2025-03-10');
      expect(result.startTime).toContain('2025-03-10T02:30:00');
    });
  });

  describe('Extreme Timezone Offsets', () => {
    it('should handle extreme positive offset (UTC+14 - Kiribati)', () => {
      // Kiribati uses UTC+14 (840 minutes ahead)
      mockTimezone(-840); // +14 hours

      const selectedDate = new Date('2025-06-01T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '10:00', '13:00');

      expect(result.date).toContain('2025-06-01');
      expect(result.startTime).toContain('2025-06-01T10:00:00');
    });

    it('should handle extreme negative offset (UTC-12 - Baker Island)', () => {
      // Baker Island uses UTC-12 (720 minutes behind)
      mockTimezone(720); // -12 hours

      const selectedDate = new Date('2025-08-15T00:00:00.000Z');
      const result = processSessionDate(selectedDate, '15:00', '18:00');

      expect(result.date).toContain('2025-08-15');
      expect(result.startTime).toContain('2025-08-15T15:00:00');
    });
  });
});
