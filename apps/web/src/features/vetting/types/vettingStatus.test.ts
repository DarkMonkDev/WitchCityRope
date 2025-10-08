/**
 * Tests for vetting status types and helpers
 */
import { describe, it, expect } from 'vitest';
import {
  VettingStatus,
  type VettingStatusString,
  statusStringToEnum,
  statusEnumToString,
  shouldHideMenuForStatus
} from './vettingStatus';

describe('VettingStatus Types', () => {
  describe('statusStringToEnum', () => {
    it('should convert UnderReview string to enum', () => {
      expect(statusStringToEnum('UnderReview')).toBe(VettingStatus.UnderReview);
      expect(statusStringToEnum('UnderReview')).toBe(0);
    });

    it('should convert InterviewApproved string to enum', () => {
      expect(statusStringToEnum('InterviewApproved')).toBe(VettingStatus.InterviewApproved);
      expect(statusStringToEnum('InterviewApproved')).toBe(1);
    });

    it('should convert FinalReview string to enum', () => {
      expect(statusStringToEnum('FinalReview')).toBe(VettingStatus.FinalReview);
      expect(statusStringToEnum('FinalReview')).toBe(2);
    });

    it('should convert Approved string to enum', () => {
      expect(statusStringToEnum('Approved')).toBe(VettingStatus.Approved);
      expect(statusStringToEnum('Approved')).toBe(3);
    });

    it('should convert Denied string to enum', () => {
      expect(statusStringToEnum('Denied')).toBe(VettingStatus.Denied);
      expect(statusStringToEnum('Denied')).toBe(4);
    });

    it('should convert OnHold string to enum', () => {
      expect(statusStringToEnum('OnHold')).toBe(VettingStatus.OnHold);
      expect(statusStringToEnum('OnHold')).toBe(5);
    });

    it('should convert Withdrawn string to enum', () => {
      expect(statusStringToEnum('Withdrawn')).toBe(VettingStatus.Withdrawn);
      expect(statusStringToEnum('Withdrawn')).toBe(6);
    });
  });

  describe('statusEnumToString', () => {
    it('should convert UnderReview enum to string', () => {
      expect(statusEnumToString(VettingStatus.UnderReview)).toBe('UnderReview');
      expect(statusEnumToString(0)).toBe('UnderReview');
    });

    it('should convert InterviewApproved enum to string', () => {
      expect(statusEnumToString(VettingStatus.InterviewApproved)).toBe('InterviewApproved');
      expect(statusEnumToString(1)).toBe('InterviewApproved');
    });

    it('should convert FinalReview enum to string', () => {
      expect(statusEnumToString(VettingStatus.FinalReview)).toBe('FinalReview');
      expect(statusEnumToString(2)).toBe('FinalReview');
    });

    it('should convert Approved enum to string', () => {
      expect(statusEnumToString(VettingStatus.Approved)).toBe('Approved');
      expect(statusEnumToString(3)).toBe('Approved');
    });

    it('should convert Denied enum to string', () => {
      expect(statusEnumToString(VettingStatus.Denied)).toBe('Denied');
      expect(statusEnumToString(4)).toBe('Denied');
    });

    it('should convert OnHold enum to string', () => {
      expect(statusEnumToString(VettingStatus.OnHold)).toBe('OnHold');
      expect(statusEnumToString(5)).toBe('OnHold');
    });

    it('should convert Withdrawn enum to string', () => {
      expect(statusEnumToString(VettingStatus.Withdrawn)).toBe('Withdrawn');
      expect(statusEnumToString(6)).toBe('Withdrawn');
    });
  });

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

  describe('Round-trip conversions', () => {
    it('should convert enum to string and back', () => {
      const allStatuses = [
        VettingStatus.UnderReview,
        VettingStatus.InterviewApproved,
        VettingStatus.FinalReview,
        VettingStatus.Approved,
        VettingStatus.Denied,
        VettingStatus.OnHold,
        VettingStatus.Withdrawn
      ];

      allStatuses.forEach(status => {
        const str = statusEnumToString(status);
        const backToEnum = statusStringToEnum(str);
        expect(backToEnum).toBe(status);
      });
    });

    it('should convert string to enum and back', () => {
      const allStatusStrings: VettingStatusString[] = [
        'UnderReview',
        'InterviewApproved',
        'FinalReview',
        'Approved',
        'Denied',
        'OnHold',
        'Withdrawn'
      ];

      allStatusStrings.forEach(str => {
        const enumValue = statusStringToEnum(str);
        const backToString = statusEnumToString(enumValue);
        expect(backToString).toBe(str);
      });
    });
  });
});
