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

  if (formData.shortDescription?.trim()) {
    updateDto.shortDescription = formData.shortDescription.trim();
  }

  if (formData.fullDescription?.trim()) {
    updateDto.description = formData.fullDescription.trim();
  }

  if (formData.venueId?.trim()) {
    updateDto.venueId = parseInt(formData.venueId.trim(), 10);
  }

  // Include boolean flags (replace eventType)
  if (formData.allowRsvps !== undefined) {
    updateDto.allowRsvps = formData.allowRsvps;
  }
  if (formData.requireTicketPurchase !== undefined) {
    updateDto.requireTicketPurchase = formData.requireTicketPurchase;
  }
  if (formData.vettedMembersOnly !== undefined) {
    updateDto.vettedMembersOnly = formData.vettedMembersOnly;
  }

  // Include sessions data (always include even if empty to allow clearing)
  if (formData.sessions !== undefined) {
    updateDto.sessions = formData.sessions.map(session => ({
      id: session.id,
      sessionIdentifier: session.sessionIdentifier, // API requires this field
      name: session.name,
      startDate: session.startDate, // API requires this field (renamed from date)
      startTime: session.startTime,
      endTime: session.endTime,
      capacity: session.capacity,
      registrationCount: session.registrationCount || 0, // Fixed: use registrationCount
      description: (session as any).description || ''
    }));
  }

  // Include ticket types data (always include even if empty to allow clearing)
  if (formData.ticketTypes !== undefined) {
    updateDto.ticketTypes = formData.ticketTypes.map(ticket => ({
      id: ticket.id,
      name: ticket.name,
      pricingType: ticket.pricingType,
      price: ticket.price ?? 0,
      minPrice: ticket.minPrice ?? undefined,
      maxPrice: ticket.maxPrice ?? undefined,
      defaultPrice: ticket.defaultPrice ?? undefined,
      quantityAvailable: ticket.quantityAvailable ?? 0,
      sessionIdentifiers: ticket.sessionIdentifiers
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
      sessionId: (position as any).sessionId,
      isPublicFacing: position.isPublicFacing ?? true // Default to public-facing
    }));
  }

  // Include policies if present
  if (formData.policies?.trim()) {
    updateDto.policies = formData.policies.trim();
  }

  // Include timing control fields (null is valid - means no restriction)
  if (formData.registrationOpenHours !== undefined) {
    updateDto.registrationOpenHours = formData.registrationOpenHours;
  }
  if (formData.registrationCloseHours !== undefined) {
    updateDto.registrationCloseHours = formData.registrationCloseHours;
  }
  if (formData.cancellationCloseHours !== undefined) {
    updateDto.cancellationCloseHours = formData.cancellationCloseHours;
  }
  if (formData.volunteerRegistrationCloseHours !== undefined) {
    updateDto.volunteerRegistrationCloseHours = formData.volunteerRegistrationCloseHours;
  }
  if (formData.volunteerCancellationCloseHours !== undefined) {
    updateDto.volunteerCancellationCloseHours = formData.volunteerCancellationCloseHours;
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
    'allowRsvps',
    'requireTicketPurchase',
    'vettedMembersOnly',
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

  if (current.shortDescription?.trim() !== initial.shortDescription?.trim()) {
    changes.shortDescription = current.shortDescription?.trim();
  }

  if (current.fullDescription?.trim() !== initial.fullDescription?.trim()) {
    changes.description = current.fullDescription?.trim();
  }

  if (current.venueId?.trim() !== initial.venueId?.trim()) {
    changes.venueId = parseInt(current.venueId?.trim() || '0', 10);
  }

  // Check boolean flags for changes
  if (current.allowRsvps !== initial.allowRsvps) {
    changes.allowRsvps = current.allowRsvps;
  }

  if (current.requireTicketPurchase !== initial.requireTicketPurchase) {
    changes.requireTicketPurchase = current.requireTicketPurchase;
  }

  if (current.vettedMembersOnly !== initial.vettedMembersOnly) {
    changes.vettedMembersOnly = current.vettedMembersOnly;
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
      startDate: session.startDate, // API requires this field (renamed from date)
      startTime: session.startTime,
      endTime: session.endTime,
      capacity: session.capacity,
      registrationCount: session.registrationCount || 0, // Fixed: use registrationCount
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
      pricingType: ticket.pricingType,
      price: ticket.price ?? 0,
      minPrice: ticket.minPrice ?? undefined,
      maxPrice: ticket.maxPrice ?? undefined,
      defaultPrice: ticket.defaultPrice ?? undefined,
      quantityAvailable: ticket.quantityAvailable ?? 0,
      sessionIdentifiers: ticket.sessionIdentifiers
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
      sessionId: (position as any).sessionId,
      isPublicFacing: position.isPublicFacing ?? true // Default to public-facing
    }));
  }

  // Check timing control fields (null is a valid value - means no restriction)
  if (current.registrationOpenHours !== initial.registrationOpenHours) {
    changes.registrationOpenHours = current.registrationOpenHours;
  }
  if (current.registrationCloseHours !== initial.registrationCloseHours) {
    changes.registrationCloseHours = current.registrationCloseHours;
  }
  if (current.cancellationCloseHours !== initial.cancellationCloseHours) {
    changes.cancellationCloseHours = current.cancellationCloseHours;
  }
  if (current.volunteerRegistrationCloseHours !== initial.volunteerRegistrationCloseHours) {
    changes.volunteerRegistrationCloseHours = current.volunteerRegistrationCloseHours;
  }
  if (current.volunteerCancellationCloseHours !== initial.volunteerCancellationCloseHours) {
    changes.volunteerCancellationCloseHours = current.volunteerCancellationCloseHours;
  }

  // Include publish status if provided
  if (typeof isPublished === 'boolean') {
    changes.isPublished = isPublished;
  }

  return changes;
}