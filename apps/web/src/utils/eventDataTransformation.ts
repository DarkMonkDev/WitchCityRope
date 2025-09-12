import type { EventFormData } from '../components/events/EventForm';
import type { UpdateEventDto } from '../lib/api/types/events.types';

/**
 * Convert EventFormData from the form to UpdateEventDto for the API
 * Only includes non-empty fields to support partial updates
 */
export function convertEventFormDataToUpdateDto(
  eventId: string,
  formData: EventFormData,
  isPublished?: boolean
): UpdateEventDto {
  const updateDto: UpdateEventDto = {
    id: eventId,
  };

  // Only include fields that have values (partial updates)
  if (formData.title?.trim()) {
    updateDto.title = formData.title.trim();
  }

  if (formData.fullDescription?.trim()) {
    updateDto.description = formData.fullDescription.trim();
  }

  if (formData.venueId?.trim()) {
    updateDto.location = formData.venueId.trim();
  }

  // Handle optional numeric fields
  // Note: EventFormData doesn't have capacity/price fields yet, but we can extend it later
  // Backend UpdateEventRequest supports: Capacity, Price

  // Handle publish status if provided
  if (typeof isPublished === 'boolean') {
    updateDto.isPublished = isPublished;
  }

  // Handle dates - convert to ISO strings if they exist
  // Note: Current EventFormData doesn't have date fields, but we prepare for them
  // Backend expects: StartDate, EndDate (not StartDateTime/EndDateTime)
  if ('startDate' in formData && formData.startDate && typeof formData.startDate === 'string') {
    updateDto.startDate = new Date(formData.startDate).toISOString();
  }

  if ('endDate' in formData && formData.endDate && typeof formData.endDate === 'string') {
    updateDto.endDate = new Date(formData.endDate).toISOString();
  }

  return updateDto;
}

/**
 * Check if EventFormData has changed from initial values
 * Used to determine if form is dirty and needs saving
 */
export function hasEventFormDataChanged(
  current: EventFormData,
  initial: EventFormData
): boolean {
  const fieldsToCheck: (keyof EventFormData)[] = [
    'title',
    'shortDescription', 
    'fullDescription',
    'policies',
    'venueId',
    'eventType'
  ];

  return fieldsToCheck.some(field => {
    const currentValue = current[field];
    const initialValue = initial[field];
    
    // Handle string fields
    if (typeof currentValue === 'string' && typeof initialValue === 'string') {
      return currentValue.trim() !== initialValue.trim();
    }
    
    // Handle array fields (teacherIds, sessions, ticketTypes)
    if (Array.isArray(currentValue) && Array.isArray(initialValue)) {
      return JSON.stringify(currentValue) !== JSON.stringify(initialValue);
    }
    
    return currentValue !== initialValue;
  });
}

/**
 * Get only the changed fields between current and initial EventFormData
 * Returns partial UpdateEventDto with only changed fields
 */
export function getChangedEventFields(
  eventId: string,
  current: EventFormData,
  initial: EventFormData,
  isPublished?: boolean
): UpdateEventDto {
  const changes: UpdateEventDto = {
    id: eventId,
  };

  // Check each field and only include if changed
  if (current.title?.trim() !== initial.title?.trim()) {
    changes.title = current.title?.trim();
  }

  if (current.fullDescription?.trim() !== initial.fullDescription?.trim()) {
    changes.description = current.fullDescription?.trim();
  }

  if (current.venueId?.trim() !== initial.venueId?.trim()) {
    changes.location = current.venueId?.trim();
  }

  // Include publish status if provided
  if (typeof isPublished === 'boolean') {
    changes.isPublished = isPublished;
  }

  return changes;
}