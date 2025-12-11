/**
 * Complete Event Scenario
 *
 * Creates a fully-configured event with sessions, ticket types, etc.
 * Use for tests that need a complete event setup.
 */

import type { APIRequestContext } from '@playwright/test';
import { EventFactory } from '../factories/event.factory';
import { SessionFactory } from '../factories/session.factory';
import { TicketTypeFactory } from '../factories/ticket-type.factory';
import type {
  EventResponse,
  SessionResponse,
  TicketTypeResponse,
} from '../types';

export interface CompleteEventData {
  event: EventResponse;
  sessions: SessionResponse[];
  ticketTypes: TicketTypeResponse[];
}

export interface CompleteEventOptions {
  /** Event title */
  title?: string;
  /** Number of sessions to create */
  sessionCount?: number;
  /** Price for ticket types */
  ticketPrice?: number;
  /** Quantity available per ticket type */
  ticketsPerSession?: number;
  /** Start date for the event (defaults to tomorrow) */
  startDate?: Date;
}

/**
 * Create a complete event with sessions and ticket types
 *
 * @example
 * const { event, sessions, ticketTypes } = await createCompleteEvent(request, {
 *   title: 'Workshop',
 *   sessionCount: 2,
 *   ticketPrice: 25
 * });
 */
export async function createCompleteEvent(
  request: APIRequestContext,
  options: CompleteEventOptions = {}
): Promise<CompleteEventData> {
  const eventFactory = new EventFactory(request);
  const sessionFactory = new SessionFactory(request);
  const ticketTypeFactory = new TicketTypeFactory(request);

  // Calculate dates
  const now = new Date();
  const startDate = options.startDate ?? new Date(now.getTime() + 24 * 60 * 60 * 1000);
  const endDate = new Date(startDate.getTime() + 8 * 60 * 60 * 1000); // 8 hours

  // Create event
  const event = await eventFactory.create({
    title: options.title ?? `Complete Event ${Date.now()}`,
    startDate,
    endDate,
    status: 'Published',
    isPublic: true,
  });

  // Create sessions
  const sessionCount = options.sessionCount ?? 1;
  const sessions: SessionResponse[] = [];

  for (let i = 0; i < sessionCount; i++) {
    const sessionStart = new Date(startDate.getTime() + i * 2 * 60 * 60 * 1000);
    const session = await sessionFactory.create({
      eventId: event.id,
      title: `Session ${i + 1}`,
      startTime: sessionStart,
      endTime: new Date(sessionStart.getTime() + 90 * 60 * 1000), // 90 minutes
    });
    sessions.push(session);
  }

  // Create ticket types for each session
  const ticketTypes: TicketTypeResponse[] = [];

  for (const session of sessions) {
    const ticketType = await ticketTypeFactory.create({
      sessionId: session.id,
      name: `General Admission - ${session.title}`,
      price: options.ticketPrice ?? 20,
      quantityAvailable: options.ticketsPerSession ?? 50,
    });
    ticketTypes.push(ticketType);
  }

  return { event, sessions, ticketTypes };
}

/**
 * Cleanup a complete event and all related data
 *
 * Note: Deleting the event should cascade delete sessions and ticket types
 * if the backend is implemented correctly.
 */
export async function cleanupCompleteEvent(
  request: APIRequestContext,
  data: CompleteEventData
): Promise<void> {
  const eventFactory = new EventFactory(request);

  // Deleting the event should cascade delete sessions and ticket types
  await eventFactory.delete(data.event.id);
}

/**
 * Create a ticketed event ready for purchase testing
 *
 * Creates:
 * - 1 published event
 * - 1 session with registration
 * - 1 ticket type with specified price and quantity
 */
export async function createTicketedEvent(
  request: APIRequestContext,
  options: {
    title?: string;
    ticketPrice?: number;
    ticketQuantity?: number;
  } = {}
): Promise<CompleteEventData> {
  return createCompleteEvent(request, {
    title: options.title ?? 'Ticketed Event',
    sessionCount: 1,
    ticketPrice: options.ticketPrice ?? 25,
    ticketsPerSession: options.ticketQuantity ?? 100,
  });
}

/**
 * Create a multi-session workshop event
 *
 * Creates:
 * - 1 published event
 * - 3 sessions (morning, afternoon, evening)
 * - 1 ticket type per session
 */
export async function createWorkshopEvent(
  request: APIRequestContext,
  options: { title?: string; ticketPrice?: number } = {}
): Promise<CompleteEventData> {
  return createCompleteEvent(request, {
    title: options.title ?? 'Workshop Day',
    sessionCount: 3,
    ticketPrice: options.ticketPrice ?? 30,
    ticketsPerSession: 20,
  });
}
