/**
 * Tests for vetting status types and helpers
 */
import { describe, it, expect } from 'vitest';
import {
  type VettingStatus,
  shouldHideMenuForStatus
} from './vettingStatus';

describe('VettingStatus Types', () => {
  describe('shouldHideMenuForStatus', () => {
    it('should return true for OnHold status', () => {
      expect(shouldHideMenuForStatus('OnHold')).toBe(true);
    });

    it('should return true for Approved status', () => {
      expect(shouldHideMenuForStatus('Approved')).toBe(true);
    });

    it('should return true for Denied status', () => {
      expect(shouldHideMenuForStatus('Denied')).toBe(true);
    });

    it('should return false for UnderReview status', () => {
      expect(shouldHideMenuForStatus('UnderReview')).toBe(false);
    });

    it('should return false for InterviewApproved status', () => {
      expect(shouldHideMenuForStatus('InterviewApproved')).toBe(false);
    });

    it('should return false for FinalReview status', () => {
      expect(shouldHideMenuForStatus('FinalReview')).toBe(false);
    });

    it('should return false for Withdrawn status', () => {
      expect(shouldHideMenuForStatus('Withdrawn')).toBe(false);
    });
  });

  describe('VettingStatus type', () => {
    it('should have correct string values', () => {
      // Test that the type allows valid status strings
      const validStatuses: VettingStatus[] = [
        'UnderReview',
        'InterviewApproved',
        'FinalReview',
        'Approved',
        'Denied',
        'OnHold',
        'Withdrawn'
      ];

      // Just verify the array compiles - it proves the type is correct
      expect(validStatuses).toHaveLength(7);
    });
  });
});
