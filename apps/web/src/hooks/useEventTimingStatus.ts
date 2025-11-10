import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getPublicSettings } from '../services/settings.api';

export interface EventTimingStatus {
  isPastEvent: boolean;
  isWithinBuffer: boolean;
  canRegister: boolean;
  canCancel: boolean;
  statusMessage: string;
  cutoffTime: Date;
}

export const useEventTimingStatus = (eventStartDateTime?: string): EventTimingStatus | null => {
  // Fetch buffer setting
  const { data: settings } = useQuery({
    queryKey: ['publicSettings'],
    queryFn: getPublicSettings,
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  });

  return useMemo(() => {
    if (!eventStartDateTime || !settings) {
      return null;
    }

    const now = new Date();
    const eventStart = new Date(eventStartDateTime);
    const bufferMinutes = parseInt(settings.preStartBufferMinutes, 10) || 0;
    const cutoffTime = new Date(eventStart.getTime() - (bufferMinutes * 60 * 1000));

    const isPastEvent = now >= eventStart;
    const isWithinBuffer = now >= cutoffTime && now < eventStart;
    const canRegister = now < cutoffTime;
    const canCancel = now < cutoffTime;

    let statusMessage = '';
    if (isPastEvent) {
      statusMessage = 'This event has already started.';
    } else if (isWithinBuffer) {
      statusMessage = `Cancellations closed ${bufferMinutes} minutes before event start.`;
    }

    return {
      isPastEvent,
      isWithinBuffer,
      canRegister,
      canCancel,
      statusMessage,
      cutoffTime,
    };
  }, [eventStartDateTime, settings]);
};
