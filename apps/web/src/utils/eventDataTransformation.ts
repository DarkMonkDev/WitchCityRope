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

  // Include eventType mapping
  if (formData.eventType) {
    // Map frontend eventType to backend format
    updateDto.eventType = formData.eventType === 'class' ? 'Class' : 'Social';
  }

  // Include sessions data (always include even if empty to allow clearing)
  if (formData.sessions !== undefined) {
    updateDto.sessions = formData.sessions.map(session => ({
      id: session.id,
      sessionIdentifier: session.sessionIdentifier, // API requires this field
      name: session.name,
      date: session.date, // API requires this field
      startTime: session.startTime,
      endTime: session.endTime,
      capacity: session.capacity,
      registeredCount: session.registeredCount || 0, // Include registeredCount
      description: (session as any).description || ''
    }));
  }

  // Include ticket types data (always include even if empty to allow clearing)
  if (formData.ticketTypes !== undefined) {
    updateDto.ticketTypes = formData.ticketTypes.map(ticket => ({
      id: ticket.id,
      name: ticket.name,
      type: ticket.type,
      price: ticket.minPrice,
      maxPrice: ticket.maxPrice,
      quantityAvailable: ticket.quantityAvailable,
      sessionIdentifiers: ticket.sessionIdentifiers,
      salesEndDate: ticket.salesEndDate
    }));
  }

  // Include teacher IDs (always include even if empty to allow clearing)
  if (formData.teacherIds !== undefined) {
    updateDto.teacherIds = formData.teacherIds;
  }

  // Include volunteer positions (always include even if empty to allow clearing)
  if (formData.volunteerPositions !== undefined) {
    updateDto.volunteerPositions = formData.volunteerPositions.map(position => ({
      id: position.id,
      title: position.title || '',
      description: position.description || '',
      slotsNeeded: position.slotsNeeded || 0,
      slotsFilled: position.slotsFilled || 0,
      requiresExperience: (position as any).requiresExperience || false,
      requirements: (position as any).requirements || '',
      sessionId: (position as any).sessionId
    }));
  }

  // Include policies if present
  if (formData.policies?.trim()) {
    updateDto.policies = formData.policies.trim();
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
    'eventType',
    'teacherIds',
    'sessions',
    'ticketTypes',
    'volunteerPositions'
  ];

  return fieldsToCheck.some(field => {
    const currentValue = current[field];
    const initialValue = initial[field];
    
    // Handle string fields
    if (typeof currentValue === 'string' && typeof initialValue === 'string') {
      return currentValue.trim() !== initialValue.trim();
    }
    
    // Handle array fields (teacherIds, sessions, ticketTypes, volunteerPositions)
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

  if (current.eventType !== initial.eventType) {
    changes.eventType = current.eventType === 'class' ? 'Class' : 'Social';
  }

  if (current.policies?.trim() !== initial.policies?.trim()) {
    changes.policies = current.policies?.trim();
  }

  // Check array fields - sessions (always include if changed, even if empty)
  const currentSessionsStr = JSON.stringify(current.sessions || []);
  const initialSessionsStr = JSON.stringify(initial.sessions || []);
  if (currentSessionsStr !== initialSessionsStr) {
    changes.sessions = (current.sessions || []).map(session => ({
      id: session.id,
      sessionIdentifier: session.sessionIdentifier, // API requires this field
      name: session.name,
      date: session.date, // API requires this field
      startTime: session.startTime,
      endTime: session.endTime,
      capacity: session.capacity,
      registeredCount: session.registeredCount || 0, // Include registeredCount
      description: (session as any).description || ''
    }));
  }

  // Check array fields - ticket types (always include if changed, even if empty)
  const currentTicketTypesStr = JSON.stringify(current.ticketTypes || []);
  const initialTicketTypesStr = JSON.stringify(initial.ticketTypes || []);
  if (currentTicketTypesStr !== initialTicketTypesStr) {
    changes.ticketTypes = (current.ticketTypes || []).map(ticket => ({
      id: ticket.id,
      name: ticket.name,
      type: ticket.type,
      price: ticket.minPrice,
      maxPrice: ticket.maxPrice,
      quantityAvailable: ticket.quantityAvailable,
      sessionIdentifiers: ticket.sessionIdentifiers,
      salesEndDate: ticket.salesEndDate
    }));
  }

  // Check array fields - teacher IDs (always include if changed, even if empty)
  const currentTeacherIdsStr = JSON.stringify(current.teacherIds || []);
  const initialTeacherIdsStr = JSON.stringify(initial.teacherIds || []);
  if (currentTeacherIdsStr !== initialTeacherIdsStr) {
    changes.teacherIds = current.teacherIds || [];
  }

  // Check array fields - volunteer positions (always include if changed, even if empty)
  const currentVolunteerPositionsStr = JSON.stringify(current.volunteerPositions || []);
  const initialVolunteerPositionsStr = JSON.stringify(initial.volunteerPositions || []);
  if (currentVolunteerPositionsStr !== initialVolunteerPositionsStr) {
    changes.volunteerPositions = (current.volunteerPositions || []).map(position => ({
      id: position.id,
      title: position.title || '',
      description: position.description || '',
      slotsNeeded: position.slotsNeeded || 0,
      slotsFilled: position.slotsFilled || 0,
      requiresExperience: (position as any).requiresExperience || false,
      requirements: (position as any).requirements || '',
      sessionId: (position as any).sessionId
    }));
  }

  // Include publish status if provided
  if (typeof isPublished === 'boolean') {
    changes.isPublished = isPublished;
  }

  return changes;
}