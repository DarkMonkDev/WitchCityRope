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

export const formatEventDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
};

export const formatEventTime = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
};

/**
 * Format event date and time in compact format
 * Example: "Sunday, Nov 2 - 1:00pm - 4:00pm"
 * @param startDate - Event start date ISO string
 * @param endDate - Event end date ISO string (optional)
 * @returns Formatted date/time string
 */
export const formatEventDateTime = (startDate: string, endDate?: string): string => {
  const start = new Date(startDate);

  // Format date with abbreviated month, no year
  const datePart = start.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'short',
    day: 'numeric'
  });

  // Format start time
  const startTime = start.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  }).toLowerCase();

  // If no end date, just return date + start time
  if (!endDate) {
    return `${datePart} - ${startTime}`;
  }

  // Format end time
  const end = new Date(endDate);
  const endTime = end.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  }).toLowerCase();

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