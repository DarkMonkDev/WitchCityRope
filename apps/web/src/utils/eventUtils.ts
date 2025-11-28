// Event utility functions for formatting and calculations

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