/**
 * Ticketed Event Scenario
 *
 * Creates events with various ticket configurations for testing
 * ticket purchase, cancellation, and refund flows.
 */

import type { APIRequestContext } from '@playwright/test';
import { EventFactory } from '../factories/event.factory';
import { SessionFactory } from '../factories/session.factory';
import { TicketTypeFactory } from '../factories/ticket-type.factory';
import { UserFactory } from '../factories/user.factory';
import { TicketPurchaseFactory } from '../factories/ticket-purchase.factory';
import type {
  EventResponse,
  SessionResponse,
  TicketTypeResponse,
  UserResponse,
  TicketPurchaseResponse,
} from '../types';

export interface TicketedEventData {
  event: EventResponse;
  session: SessionResponse;
  ticketType: TicketTypeResponse;
}

export interface TicketedEventWithPurchaseData extends TicketedEventData {
  user: UserResponse;
  purchase: TicketPurchaseResponse;
}

export interface TicketedEventOptions {
  /** Event title */
  title?: string;
  /** Ticket price in dollars */
  ticketPrice?: number;
  /** Number of tickets available */
  ticketQuantity?: number;
  /** Ticket type name */
  ticketName?: string;
  /** Start date for the event (defaults to tomorrow) */
  startDate?: Date;
}

/**
 * Create a ticketed event ready for purchase testing
 *
 * @example
 * const { event, session, ticketType } = await createTicketedEvent(request, {
 *   title: 'Workshop',
 *   ticketPrice: 25,
 *   ticketQuantity: 50
 * });
 */
export async function createTicketedEvent(
  request: APIRequestContext,
  options: TicketedEventOptions = {}
): Promise<TicketedEventData> {
  const eventFactory = new EventFactory(request);
  const sessionFactory = new SessionFactory(request);
  const ticketTypeFactory = new TicketTypeFactory(request);

  // Calculate dates
  const now = new Date();
  const startDate =
    options.startDate ?? new Date(now.getTime() + 24 * 60 * 60 * 1000);
  const endDate = new Date(startDate.getTime() + 3 * 60 * 60 * 1000); // 3 hours

  // Create event
  const event = await eventFactory.create({
    title: options.title ?? `Ticketed Event ${Date.now()}`,
    startDate,
    endDate,
    eventType: 'Class',
    status: 'Published',
    isPublic: true,
  });

  // Create session
  const session = await sessionFactory.create({
    eventId: event.id,
    title: 'Main Session',
    startTime: startDate,
    endTime: endDate,
    maxCapacity: options.ticketQuantity ?? 50,
    requiresRegistration: true,
  });

  // Create ticket type
  const ticketType = await ticketTypeFactory.create({
    sessionId: session.id,
    name: options.ticketName ?? 'General Admission',
    price: options.ticketPrice ?? 25,
    quantityAvailable: options.ticketQuantity ?? 50,
    isActive: true,
  });

  return { event, session, ticketType };
}

/**
 * Create a free RSVP event (no payment required)
 */
export async function createFreeEvent(
  request: APIRequestContext,
  title?: string
): Promise<TicketedEventData> {
  return createTicketedEvent(request, {
    title: title ?? 'Free Event',
    ticketPrice: 0,
    ticketName: 'Free RSVP',
  });
}

/**
 * Create a paid event with standard pricing
 */
export async function createPaidEvent(
  request: APIRequestContext,
  price: number = 25,
  title?: string
): Promise<TicketedEventData> {
  return createTicketedEvent(request, {
    title: title ?? 'Paid Workshop',
    ticketPrice: price,
  });
}

/**
 * Create a limited capacity event (for testing sold-out scenarios)
 */
export async function createLimitedCapacityEvent(
  request: APIRequestContext,
  capacity: number = 5,
  title?: string
): Promise<TicketedEventData> {
  return createTicketedEvent(request, {
    title: title ?? 'Limited Event',
    ticketQuantity: capacity,
    ticketPrice: 20,
  });
}

/**
 * Create a ticketed event with a user who has purchased a ticket
 *
 * @example
 * const { event, session, ticketType, user, purchase } =
 *   await createTicketedEventWithPurchase(request, {
 *     ticketPrice: 30
 *   });
 */
export async function createTicketedEventWithPurchase(
  request: APIRequestContext,
  options: TicketedEventOptions & { userEmail?: string } = {}
): Promise<TicketedEventWithPurchaseData> {
  const userFactory = new UserFactory(request);
  const ticketPurchaseFactory = new TicketPurchaseFactory(request);

  // Create the event with tickets
  const eventData = await createTicketedEvent(request, options);

  // Create a user
  const email =
    options.userEmail ?? `ticket-buyer-${Date.now()}@test.witchcityrope.com`;
  const user = await userFactory.createVerified({ email });

  // Purchase a ticket
  const purchase = await ticketPurchaseFactory.create({
    userId: user.id,
    ticketTypeId: eventData.ticketType.id,
    quantity: 1,
  });

  return {
    ...eventData,
    user,
    purchase,
  };
}

/**
 * Create an event with multiple attendees (for testing check-in, capacity)
 */
export async function createEventWithAttendees(
  request: APIRequestContext,
  attendeeCount: number = 5,
  options: TicketedEventOptions = {}
): Promise<
  TicketedEventData & { users: UserResponse[]; purchases: TicketPurchaseResponse[] }
> {
  const userFactory = new UserFactory(request);
  const ticketPurchaseFactory = new TicketPurchaseFactory(request);

  // Create the event
  const eventData = await createTicketedEvent(request, {
    ...options,
    ticketQuantity: Math.max(options.ticketQuantity ?? 50, attendeeCount + 10),
  });

  // Create users and purchases
  const users: UserResponse[] = [];
  const purchases: TicketPurchaseResponse[] = [];

  for (let i = 0; i < attendeeCount; i++) {
    const user = await userFactory.createVerified({
      email: `attendee-${i}-${Date.now()}@test.witchcityrope.com`,
    });
    users.push(user);

    const purchase = await ticketPurchaseFactory.create({
      userId: user.id,
      ticketTypeId: eventData.ticketType.id,
      quantity: 1,
    });
    purchases.push(purchase);
  }

  return {
    ...eventData,
    users,
    purchases,
  };
}

/**
 * Cleanup ticketed event data
 */
export async function cleanupTicketedEvent(
  request: APIRequestContext,
  data: TicketedEventData
): Promise<void> {
  const eventFactory = new EventFactory(request);

  // Deleting the event cascades to session and ticket type
  await eventFactory.delete(data.event.id);
}

/**
 * Cleanup ticketed event with purchase data
 */
export async function cleanupTicketedEventWithPurchase(
  request: APIRequestContext,
  data: TicketedEventWithPurchaseData
): Promise<void> {
  const eventFactory = new EventFactory(request);
  const userFactory = new UserFactory(request);
  const ticketPurchaseFactory = new TicketPurchaseFactory(request);

  // Delete purchase first
  await ticketPurchaseFactory.delete(data.purchase.id);

  // Delete event (cascades to session, ticket type)
  await eventFactory.delete(data.event.id);

  // Delete user
  await userFactory.delete(data.user.id);
}
