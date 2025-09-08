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