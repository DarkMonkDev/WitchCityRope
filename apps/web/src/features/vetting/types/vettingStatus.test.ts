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
    it('should convert Draft string to enum', () => {
      expect(statusStringToEnum('Draft')).toBe(VettingStatus.Draft);
      expect(statusStringToEnum('Draft')).toBe(0);
    });

    it('should convert Submitted string to enum', () => {
      expect(statusStringToEnum('Submitted')).toBe(VettingStatus.Submitted);
      expect(statusStringToEnum('Submitted')).toBe(1);
    });

    it('should convert UnderReview string to enum', () => {
      expect(statusStringToEnum('UnderReview')).toBe(VettingStatus.UnderReview);
      expect(statusStringToEnum('UnderReview')).toBe(2);
    });

    it('should convert InterviewApproved string to enum', () => {
      expect(statusStringToEnum('InterviewApproved')).toBe(VettingStatus.InterviewApproved);
      expect(statusStringToEnum('InterviewApproved')).toBe(3);
    });

    it('should convert PendingInterview string to enum', () => {
      expect(statusStringToEnum('PendingInterview')).toBe(VettingStatus.PendingInterview);
      expect(statusStringToEnum('PendingInterview')).toBe(4);
    });

    it('should convert InterviewScheduled string to enum', () => {
      expect(statusStringToEnum('InterviewScheduled')).toBe(VettingStatus.InterviewScheduled);
      expect(statusStringToEnum('InterviewScheduled')).toBe(5);
    });

    it('should convert OnHold string to enum', () => {
      expect(statusStringToEnum('OnHold')).toBe(VettingStatus.OnHold);
      expect(statusStringToEnum('OnHold')).toBe(6);
    });

    it('should convert Approved string to enum', () => {
      expect(statusStringToEnum('Approved')).toBe(VettingStatus.Approved);
      expect(statusStringToEnum('Approved')).toBe(7);
    });

    it('should convert Denied string to enum', () => {
      expect(statusStringToEnum('Denied')).toBe(VettingStatus.Denied);
      expect(statusStringToEnum('Denied')).toBe(8);
    });

    it('should convert Withdrawn string to enum', () => {
      expect(statusStringToEnum('Withdrawn')).toBe(VettingStatus.Withdrawn);
      expect(statusStringToEnum('Withdrawn')).toBe(9);
    });
  });

  describe('statusEnumToString', () => {
    it('should convert Draft enum to string', () => {
      expect(statusEnumToString(VettingStatus.Draft)).toBe('Draft');
      expect(statusEnumToString(0)).toBe('Draft');
    });

    it('should convert Submitted enum to string', () => {
      expect(statusEnumToString(VettingStatus.Submitted)).toBe('Submitted');
      expect(statusEnumToString(1)).toBe('Submitted');
    });

    it('should convert UnderReview enum to string', () => {
      expect(statusEnumToString(VettingStatus.UnderReview)).toBe('UnderReview');
      expect(statusEnumToString(2)).toBe('UnderReview');
    });

    it('should convert InterviewApproved enum to string', () => {
      expect(statusEnumToString(VettingStatus.InterviewApproved)).toBe('InterviewApproved');
      expect(statusEnumToString(3)).toBe('InterviewApproved');
    });

    it('should convert PendingInterview enum to string', () => {
      expect(statusEnumToString(VettingStatus.PendingInterview)).toBe('PendingInterview');
      expect(statusEnumToString(4)).toBe('PendingInterview');
    });

    it('should convert InterviewScheduled enum to string', () => {
      expect(statusEnumToString(VettingStatus.InterviewScheduled)).toBe('InterviewScheduled');
      expect(statusEnumToString(5)).toBe('InterviewScheduled');
    });

    it('should convert OnHold enum to string', () => {
      expect(statusEnumToString(VettingStatus.OnHold)).toBe('OnHold');
      expect(statusEnumToString(6)).toBe('OnHold');
    });

    it('should convert Approved enum to string', () => {
      expect(statusEnumToString(VettingStatus.Approved)).toBe('Approved');
      expect(statusEnumToString(7)).toBe('Approved');
    });

    it('should convert Denied enum to string', () => {
      expect(statusEnumToString(VettingStatus.Denied)).toBe('Denied');
      expect(statusEnumToString(8)).toBe('Denied');
    });

    it('should convert Withdrawn enum to string', () => {
      expect(statusEnumToString(VettingStatus.Withdrawn)).toBe('Withdrawn');
      expect(statusEnumToString(9)).toBe('Withdrawn');
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

    it('should return false for Draft status', () => {
      expect(shouldHideMenuForStatus('Draft')).toBe(false);
    });

    it('should return false for Submitted status', () => {
      expect(shouldHideMenuForStatus('Submitted')).toBe(false);
    });

    it('should return false for UnderReview status', () => {
      expect(shouldHideMenuForStatus('UnderReview')).toBe(false);
    });

    it('should return false for InterviewApproved status', () => {
      expect(shouldHideMenuForStatus('InterviewApproved')).toBe(false);
    });

    it('should return false for PendingInterview status', () => {
      expect(shouldHideMenuForStatus('PendingInterview')).toBe(false);
    });

    it('should return false for InterviewScheduled status', () => {
      expect(shouldHideMenuForStatus('InterviewScheduled')).toBe(false);
    });

    it('should return false for Withdrawn status', () => {
      expect(shouldHideMenuForStatus('Withdrawn')).toBe(false);
    });
  });

  describe('Round-trip conversions', () => {
    it('should convert enum to string and back', () => {
      const allStatuses = [
        VettingStatus.Draft,
        VettingStatus.Submitted,
        VettingStatus.UnderReview,
        VettingStatus.InterviewApproved,
        VettingStatus.PendingInterview,
        VettingStatus.InterviewScheduled,
        VettingStatus.OnHold,
        VettingStatus.Approved,
        VettingStatus.Denied,
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
        'Draft',
        'Submitted',
        'UnderReview',
        'InterviewApproved',
        'PendingInterview',
        'InterviewScheduled',
        'OnHold',
        'Approved',
        'Denied',
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
