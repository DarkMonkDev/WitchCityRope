import { apiClient } from '../client'
import type { ApiResponse } from '../types/api.types'

// TypeScript types for Event Session Matrix - matching backend DTOs
export interface EventSessionDto {
  id: string
  eventId: string
  sessionIdentifier: string // S1, S2, S3, etc.
  name: string
  startDateTime: string // ISO date string
  endDateTime: string // ISO date string  
  capacity: number
  registeredCount: number
  createdAt: string
  updatedAt: string
}

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

export interface EventTicketTypeDto {
  id: string
  eventId: string
  name: string
  pricingType: 'fixed' | 'sliding-scale' // Matches backend enum
  price?: number // For fixed pricing
  minPrice?: number // For sliding scale
  maxPrice?: number // For sliding scale
  defaultPrice?: number // Default/suggested price for sliding scale
  quantityAvailable?: number
  salesEndDate?: string
  sessionIds: string[] // Array of session IDs this ticket type includes
  createdAt: string
  updatedAt: string
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
  async getSessions(eventId: string): Promise<EventSessionDto[]> {
    const { data } = await apiClient.get<ApiResponse<EventSessionDto[]>>(`/api/events/${eventId}/sessions`)
    return data.data || []
  },

  async createSession(eventId: string, sessionData: CreateEventSessionDto): Promise<EventSessionDto> {
    const { data } = await apiClient.post<ApiResponse<EventSessionDto>>(`/api/events/${eventId}/sessions`, sessionData)
    if (!data.data) throw new Error('Failed to create session')
    return data.data
  },

  async updateSession(sessionId: string, sessionData: Partial<UpdateEventSessionDto>): Promise<EventSessionDto> {
    const { data } = await apiClient.put<ApiResponse<EventSessionDto>>(`/api/sessions/${sessionId}`, sessionData)
    if (!data.data) throw new Error('Failed to update session')
    return data.data
  },

  async deleteSession(sessionId: string): Promise<void> {
    await apiClient.delete(`/api/sessions/${sessionId}`)
  },

  // Ticket Type Management Endpoints
  async getTicketTypes(eventId: string): Promise<EventTicketTypeDto[]> {
    const { data } = await apiClient.get<ApiResponse<EventTicketTypeDto[]>>(`/api/events/${eventId}/ticket-types`)
    return data.data || []
  },

  async createTicketType(eventId: string, ticketTypeData: CreateEventTicketTypeDto): Promise<EventTicketTypeDto> {
    const { data } = await apiClient.post<ApiResponse<EventTicketTypeDto>>(`/api/events/${eventId}/ticket-types`, ticketTypeData)
    if (!data.data) throw new Error('Failed to create ticket type')
    return data.data
  },

  async updateTicketType(ticketTypeId: string, ticketTypeData: Partial<UpdateEventTicketTypeDto>): Promise<EventTicketTypeDto> {
    const { data } = await apiClient.put<ApiResponse<EventTicketTypeDto>>(`/api/ticket-types/${ticketTypeId}`, ticketTypeData)
    if (!data.data) throw new Error('Failed to update ticket type')
    return data.data
  },

  async deleteTicketType(ticketTypeId: string): Promise<void> {
    await apiClient.delete(`/api/ticket-types/${ticketTypeId}`)
  }
}