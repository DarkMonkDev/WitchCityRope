import type { EventSessionDto, EventTicketTypeDto } from '../services/eventSessions'
import type { EventSession } from '../../../components/events/EventSessionsGrid'
import type { EventTicketType } from '../../../components/events/EventTicketTypesGrid'

/**
 * Converts backend EventSessionDto to frontend EventSession interface
 */
export function convertEventSessionFromDto(dto: EventSessionDto): EventSession {
  const startDateTime = new Date(dto.startDateTime)
  const endDateTime = new Date(dto.endDateTime)
  
  return {
    id: dto.id,
    sessionIdentifier: dto.sessionIdentifier,
    name: dto.name,
    date: startDateTime.toISOString().split('T')[0], // YYYY-MM-DD format
    startTime: startDateTime.toTimeString().slice(0, 5), // HH:MM format
    endTime: endDateTime.toTimeString().slice(0, 5), // HH:MM format
    capacity: dto.capacity,
    registeredCount: dto.registeredCount
  }
}

/**
 * Converts frontend EventSession to backend CreateEventSessionDto
 */
export function convertEventSessionToCreateDto(session: EventSession): { 
  sessionIdentifier: string
  name: string
  startDateTime: string
  endDateTime: string
  capacity: number 
} {
  // Combine date and time strings to create ISO datetime strings
  const startDateTime = new Date(`${session.date}T${session.startTime}:00`)
  const endDateTime = new Date(`${session.date}T${session.endTime}:00`)
  
  return {
    sessionIdentifier: session.sessionIdentifier,
    name: session.name,
    startDateTime: startDateTime.toISOString(),
    endDateTime: endDateTime.toISOString(),
    capacity: session.capacity
  }
}

/**
 * Converts backend EventTicketTypeDto to frontend EventTicketType interface
 */
export function convertEventTicketTypeFromDto(dto: EventTicketTypeDto, sessions: EventSession[] = []): EventTicketType {
  // Convert session IDs to session identifiers
  const sessionIdentifiers = dto.sessionIds.map(sessionId => {
    const session = sessions.find(s => s.id === sessionId)
    return session?.sessionIdentifier || sessionId
  })
  
  return {
    id: dto.id,
    name: dto.name,
    pricingType: dto.pricingType, // Direct mapping as enum values match
    sessionIdentifiers,
    minPrice: dto.minPrice,
    maxPrice: dto.maxPrice,
    quantityAvailable: dto.quantityAvailable,
    salesEndDate: dto.salesEndDate
  }
}

/**
 * Converts frontend EventTicketType to backend CreateEventTicketTypeDto
 */
import type { components } from '@witchcityrope/shared-types'

export function convertEventTicketTypeToCreateDto(
  ticketType: EventTicketType,
  sessions: EventSession[] = []
): {
  name: string
  pricingType: components["schemas"]["PricingType"]
  price?: number
  minPrice?: number
  maxPrice?: number
  defaultPrice?: number
  quantityAvailable?: number
  salesEndDate?: string
  sessionIds: string[]
} {
  // Convert session identifiers to session IDs
  const sessionIds = ticketType.sessionIdentifiers.map(identifier => {
    const session = sessions.find(s => s.sessionIdentifier === identifier)
    return session?.id || identifier
  })

  return {
    name: ticketType.name,
    pricingType: ticketType.pricingType, // Direct mapping as enum values match
    price: ticketType.price,
    minPrice: ticketType.minPrice,
    maxPrice: ticketType.maxPrice,
    defaultPrice: ticketType.defaultPrice,
    quantityAvailable: ticketType.quantityAvailable,
    salesEndDate: ticketType.salesEndDate,
    sessionIds
  }
}

/**
 * Utility to format datetime for display in forms
 */
export function formatDateTimeForInput(isoString: string): { date: string, time: string } {
  const datetime = new Date(isoString)
  return {
    date: datetime.toISOString().split('T')[0], // YYYY-MM-DD
    time: datetime.toTimeString().slice(0, 5) // HH:MM
  }
}

/**
 * Utility to combine date and time inputs into ISO string
 */
export function combineDateTimeToIso(date: string, time: string): string {
  return new Date(`${date}T${time}:00`).toISOString()
}