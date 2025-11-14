import { apiClient } from '../client'
import type { components } from '@witchcityrope/shared-types'

/**
 * ✅ DTO ALIGNMENT STRATEGY COMPLIANT
 * All types imported from @witchcityrope/shared-types (auto-generated from backend DTOs)
 * Source: C# SessionDto and TicketTypeDto via NSwag generation
 */

// Re-export auto-generated types for convenience
export type SessionDto = components['schemas']['SessionDto']
export type TicketTypeDto = components['schemas']['TicketTypeDto']

// Frontend-only types for API requests (not in backend DTOs yet)
// TODO: These should be added to backend and auto-generated
export interface CreateEventSessionDto {
  sessionIdentifier: string
  name: string
  startDateTime: string
  endDateTime: string
  capacity: number
}

export interface UpdateEventSessionDto {
  id: string
  sessionIdentifier?: string
  name?: string
  startDateTime?: string
  endDateTime?: string
  capacity?: number
}

export interface CreateEventTicketTypeDto {
  name: string
  ticketType: 'Single' | 'Couples'
  minPrice: number
  maxPrice: number
  quantityAvailable?: number
  salesEndDate?: string
  sessionIds: string[]
}

export interface UpdateEventTicketTypeDto {
  id: string
  name?: string
  ticketType?: 'Single' | 'Couples'
  minPrice?: number
  maxPrice?: number
  quantityAvailable?: number
  salesEndDate?: string
  sessionIds?: string[]
}

// API Client Functions for Event Session Management
export const eventSessionsApi = {
  // Session Management Endpoints
  async getSessions(eventId: string): Promise<SessionDto[]> {
    const { data } = await apiClient.get<SessionDto[]>(`/api/events/${eventId}/sessions`)
    return data || []
  },

  async createSession(eventId: string, sessionData: CreateEventSessionDto): Promise<SessionDto> {
    const { data } = await apiClient.post<SessionDto>(`/api/events/${eventId}/sessions`, sessionData)
    if (!data) throw new Error('Failed to create session')
    return data
  },

  async updateSession(sessionId: string, sessionData: Partial<UpdateEventSessionDto>): Promise<SessionDto> {
    const { data } = await apiClient.put<SessionDto>(`/api/sessions/${sessionId}`, sessionData)
    if (!data) throw new Error('Failed to update session')
    return data
  },

  async deleteSession(sessionId: string): Promise<void> {
    await apiClient.delete(`/api/sessions/${sessionId}`)
  },

  // Ticket Type Management Endpoints
  async getTicketTypes(eventId: string): Promise<TicketTypeDto[]> {
    const { data } = await apiClient.get<TicketTypeDto[]>(`/api/events/${eventId}/ticket-types`)
    return data || []
  },

  async createTicketType(eventId: string, ticketTypeData: CreateEventTicketTypeDto): Promise<TicketTypeDto> {
    const { data } = await apiClient.post<TicketTypeDto>(`/api/events/${eventId}/ticket-types`, ticketTypeData)
    if (!data) throw new Error('Failed to create ticket type')
    return data
  },

  async updateTicketType(ticketTypeId: string, ticketTypeData: Partial<UpdateEventTicketTypeDto>): Promise<TicketTypeDto> {
    const { data } = await apiClient.put<TicketTypeDto>(`/api/ticket-types/${ticketTypeId}`, ticketTypeData)
    if (!data) throw new Error('Failed to update ticket type')
    return data
  },

  async deleteTicketType(ticketTypeId: string): Promise<void> {
    await apiClient.delete(`/api/ticket-types/${ticketTypeId}`)
  }
}
