// Event utility functions for formatting and calculations
// Updated 2025-11-30: Added true UTC conversion for session-based timing refactor

// =============================================================================
// UTC CONVERSION FUNCTIONS
// =============================================================================
// These functions handle TRUE UTC conversion (not naive UTC)
// All times are stored as actual UTC in the database
// Display converts UTC back to local time using the global event timezone
// =============================================================================

/**
 * Default timezone for the application (global admin setting)
 * All events are in Salem, MA so Eastern time applies to all
 */
export const DEFAULT_EVENT_TIMEZONE = 'America/New_York';

/**
 * Convert a local date and time to true UTC ISO string for API/database storage
 *
 * @param date - The date object (represents the calendar date)
 * @param hours - Hour in 24-hour format (0-23)
 * @param minutes - Minutes (0-59)
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @returns ISO 8601 UTC string (e.g., "2025-12-01T23:00:00.000Z")
 *
 * @example
 * // User enters 6:00 PM on Dec 1, 2025 (Eastern time)
 * localToUtc(new Date(2025, 11, 1), 18, 0) // Returns "2025-12-01T23:00:00.000Z" (EST = UTC-5)
 */
export function localToUtc(
  date: Date,
  hours: number,
  minutes: number,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string {
  // Create a date string in the target timezone
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hour = String(hours).padStart(2, '0');
  const minute = String(minutes).padStart(2, '0');

  // Format: "2025-12-01T18:00:00" (no timezone suffix - interpreted as local)
  const localDateTimeStr = `${year}-${month}-${day}T${hour}:${minute}:00`;

  // Use Intl.DateTimeFormat to get the UTC offset for this specific date/time
  // This correctly handles DST transitions
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });

  // Create a Date object treating the input as if it were in the target timezone
  // We do this by finding the UTC time that, when displayed in the target timezone,
  // shows the time the user entered

  // Step 1: Create a Date from the local string (browser interprets as local)
  const browserLocalDate = new Date(localDateTimeStr);

  // Step 2: Get what this date looks like in the target timezone
  const partsInTargetTz = formatter.formatToParts(browserLocalDate);
  const getPart = (type: string) => partsInTargetTz.find(p => p.type === type)?.value || '0';

  // Step 3: Calculate the offset between browser local and target timezone
  // by comparing the formatted date in target TZ with the original input
  const targetHour = parseInt(getPart('hour'), 10);
  const targetMinute = parseInt(getPart('minute'), 10);

  // Step 4: Adjust to get the correct UTC time
  // We need to find the UTC time that displays as the user's input in the target timezone
  const offsetMinutes = (hours * 60 + minutes) - (targetHour * 60 + targetMinute);
  const utcDate = new Date(browserLocalDate.getTime() + offsetMinutes * 60 * 1000);

  return utcDate.toISOString();
}

/**
 * Convert a local time string (HH:MM format) and date to true UTC ISO string
 *
 * @param date - The date object (represents the calendar date)
 * @param timeString - Time in "HH:MM" 24-hour format
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @returns ISO 8601 UTC string
 *
 * @example
 * localTimeStringToUtc(new Date(2025, 11, 1), "18:00") // "2025-12-01T23:00:00.000Z"
 */
export function localTimeStringToUtc(
  date: Date,
  timeString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string {
  const [hours, minutes] = timeString.split(':').map(Number);
  return localToUtc(date, hours, minutes, timezone);
}

/**
 * Convert a UTC ISO string to local time components for display
 *
 * @param isoString - ISO 8601 UTC string (e.g., "2025-12-01T23:00:00.000Z")
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @returns Object with hours (24h), minutes, and formatted string
 *
 * @example
 * utcToLocal("2025-12-01T23:00:00.000Z") // { hours: 18, minutes: 0, formatted: "6:00 PM" }
 */
export function utcToLocal(
  isoString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): { hours: number; minutes: number; formatted: string; time24: string } {
  const date = new Date(isoString);

  // Use Intl.DateTimeFormat to get the time in the target timezone
  const timeFormatter = new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });

  const time24Formatter = new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  });

  // Get parts for extracting numeric values
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).formatToParts(date);

  const hourPart = parts.find(p => p.type === 'hour')?.value || '0';
  const minutePart = parts.find(p => p.type === 'minute')?.value || '0';

  return {
    hours: parseInt(hourPart, 10),
    minutes: parseInt(minutePart, 10),
    formatted: timeFormatter.format(date),
    time24: time24Formatter.format(date),
  };
}

/**
 * Format a UTC datetime string to local time display string
 *
 * @param isoString - ISO 8601 UTC string
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @returns Formatted time string (e.g., "6:00 PM")
 *
 * @example
 * formatUtcToLocalTime("2025-12-01T23:00:00.000Z") // "6:00 PM"
 */
export function formatUtcToLocalTime(
  isoString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string {
  return utcToLocal(isoString, timezone).formatted;
}

/**
 * Format a UTC datetime string to local date display string
 *
 * @param isoString - ISO 8601 UTC string
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @param options - Intl.DateTimeFormat options for date formatting
 * @returns Formatted date string
 */
export function formatUtcToLocalDate(
  isoString: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE,
  options: Intl.DateTimeFormatOptions = {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }
): string {
  const date = new Date(isoString);
  return date.toLocaleDateString('en-US', { ...options, timeZone: timezone });
}

/**
 * Format a time range from UTC datetime strings to local display
 *
 * @param startIso - Start time ISO 8601 UTC string
 * @param endIso - End time ISO 8601 UTC string (optional)
 * @param timezone - IANA timezone string (defaults to America/New_York)
 * @returns Formatted time range (e.g., "6:00 PM - 9:00 PM")
 */
export function formatUtcTimeRange(
  startIso: string,
  endIso?: string,
  timezone: string = DEFAULT_EVENT_TIMEZONE
): string {
  const startTime = formatUtcToLocalTime(startIso, timezone);

  if (!endIso) {
    return startTime;
  }

  const endTime = formatUtcToLocalTime(endIso, timezone);
  return `${startTime} - ${endTime}`;
}

// =============================================================================
// LEGACY FUNCTIONS (Deprecated - use UTC functions above)
// =============================================================================
// These functions use "naive UTC" pattern where UTC values represent local time
// They are kept for backward compatibility during migration
// TODO: Remove these after all components are updated to use true UTC
// =============================================================================

export interface PriceStructure {
  type: 'sliding' | 'fixed';
  min?: number;
  max?: number;
  amount?: number;
}

export const formatPrice = (price: PriceStructure): string => {
  if (price.type === 'fixed' && price.amount) {
    return `$${price.amount}`;
  }
  
  if (price.type === 'sliding' && price.min && price.max) {
    return `$${price.min} - $${price.max}`;
  }
  
  return 'Price TBA';
};

export const getCapacityColor = (percentage: number, warningThreshold: number = 80): string => {
  if (percentage >= 100) return 'red';
  if (percentage >= warningThreshold) return 'red';
  if (percentage >= 60) return 'yellow';
  return 'burgundy';
};

export const formatEventDate = (dateString: string, timeZone: string = 'America/New_York'): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    timeZone
  });
};

/**
 * Format date in short format for table display
 * Example: "Sat - Jan, 7"
 * @param dateString - ISO date string
 * @param timeZone - IANA timezone (defaults to America/New_York)
 * @returns Formatted date string (e.g., "Sat - Jan, 7")
 */
export const formatShortDate = (dateString?: string, timeZone: string = 'America/New_York'): string => {
  if (!dateString) return 'TBD';
  const date = new Date(dateString);
  const dayOfWeek = date.toLocaleDateString('en-US', { weekday: 'short', timeZone });
  const month = date.toLocaleDateString('en-US', { month: 'short', timeZone });
  const day = date.toLocaleDateString('en-US', { day: 'numeric', timeZone });
  return `${dayOfWeek} - ${month}, ${day}`;
};

/**
 * Format a time from an ISO datetime string WITHOUT timezone conversion.
 * Use this for user-entered times (like event/session start/end times)
 * that are stored as "naive UTC" - the UTC value represents the local time the user entered.
 *
 * @param dateString - ISO datetime string
 * @returns Formatted time string (e.g., "6:00 PM")
 */
export const formatStoredTime = (dateString: string): string => {
  const date = new Date(dateString);
  const hours = date.getUTCHours();
  const minutes = date.getUTCMinutes();

  // Format in 12-hour format
  const period = hours >= 12 ? 'PM' : 'AM';
  const hour12 = hours % 12 || 12;
  const minuteStr = minutes.toString().padStart(2, '0');

  return `${hour12}:${minuteStr} ${period}`;
};

/**
 * Format a time range from ISO datetime strings WITHOUT timezone conversion.
 * Use this for user-entered times (like event/session start/end times).
 *
 * @param startDateString - ISO datetime string for start time
 * @param endDateString - ISO datetime string for end time (optional)
 * @returns Formatted time range (e.g., "6:00 PM - 9:00 PM")
 */
export const formatEventTime = (startDateString: string, endDateString?: string, _timeZone?: string): string => {
  // Note: timeZone parameter kept for backwards compatibility but is ignored
  // We use getUTCHours/getUTCMinutes because user-entered times are stored as "naive UTC"

  const startTime = formatStoredTime(startDateString);

  // If no end date, just return start time
  if (!endDateString) {
    return startTime;
  }

  const endTime = formatStoredTime(endDateString);

  // Return time range format: "6:00 PM - 9:00 PM"
  return `${startTime} - ${endTime}`;
};

/**
 * Format event date and time in compact format
 * Example: "Sunday, Nov 2 - 1:00pm - 4:00pm"
 * @param startDate - Event start date ISO string
 * @param endDate - Event end date ISO string (optional)
 * @param timeZone - IANA timezone (defaults to America/New_York) - used for DATE only, not time
 * @returns Formatted date/time string
 */
export const formatEventDateTime = (startDate: string, endDate?: string, timeZone: string = 'America/New_York'): string => {
  const start = new Date(startDate);

  // Format date with abbreviated month, no year
  // Note: timeZone is used for DATE formatting only (to get correct day of week)
  const datePart = start.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'short',
    day: 'numeric',
    timeZone
  });

  // Format start time using stored UTC values (no timezone conversion)
  const startTime = formatStoredTime(startDate).toLowerCase();

  // If no end date, just return date + start time
  if (!endDate) {
    return `${datePart} - ${startTime}`;
  }

  // Format end time using stored UTC values (no timezone conversion)
  const endTime = formatStoredTime(endDate).toLowerCase();

  return `${datePart} - ${startTime} - ${endTime}`;
};

export const calculateEventDuration = (startDate: string, endDate?: string): string => {
  if (!endDate) return '';
  
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diffMs = end.getTime() - start.getTime();
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffMinutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));
  
  if (diffHours === 0) {
    return `${diffMinutes} min`;
  }
  
  if (diffMinutes === 0) {
    return `${diffHours}h`;
  }
  
  return `${diffHours}h ${diffMinutes}m`;
};

export const getEventTypeColor = (type: string): string => {
  switch (type.toUpperCase()) {
    case 'CLASS':
      return 'green';
    case 'SOCIAL':
      return 'orange';
    case 'MEMBER':
      return 'grape';
    default:
      return 'gray';
  }
};

/**
 * Calculate price range from ticket types array
 * Considers both fixed price tickets and sliding scale tickets
 * @param ticketTypes - Array of ticket type objects with pricing information
 * @returns Formatted price string (e.g., "$15", "$15 - $45", or "Free")
 */
export const calculateEventPriceRange = (ticketTypes: any[]): string => {
  if (!ticketTypes || ticketTypes.length === 0) {
    return 'Free';
  }

  // Collect all possible prices from ticket types
  const prices: number[] = [];

  ticketTypes.forEach((ticket) => {
    // For sliding scale tickets, include both min and max
    if (ticket.minPrice !== null && ticket.minPrice !== undefined) {
      prices.push(ticket.minPrice);
    }
    if (ticket.maxPrice !== null && ticket.maxPrice !== undefined) {
      prices.push(ticket.maxPrice);
    }

    // For fixed price tickets
    if (ticket.price !== null && ticket.price !== undefined) {
      prices.push(ticket.price);
    }
  });

  // If no prices found, it's free
  if (prices.length === 0) {
    return 'Free';
  }

  // Find absolute min and max across all ticket types
  const minPrice = Math.min(...prices);
  const maxPrice = Math.max(...prices);

  // If same price everywhere, show single price
  if (minPrice === maxPrice) {
    return minPrice === 0 ? 'Free' : `$${minPrice.toFixed(0)}`;
  }

  // Show price range
  return `$${minPrice.toFixed(0)} - $${maxPrice.toFixed(0)}`;
};

/**
 * Format abbreviated date for compact display
 * Example: "Sun, Dec 1" or "Sat, Dec 7"
 * @param isoDate - ISO date string
 * @param timezone - IANA timezone (defaults to America/New_York)
 * @returns Abbreviated date string (e.g., "Sun, Dec 1")
 */
export const formatAbbreviatedDate = (isoDate: string, timezone: string = 'America/New_York'): string => {
  const date = new Date(isoDate);
  return date.toLocaleDateString('en-US', {
    timeZone: timezone,
    weekday: 'short',
    month: 'short',
    day: 'numeric'
  });
};