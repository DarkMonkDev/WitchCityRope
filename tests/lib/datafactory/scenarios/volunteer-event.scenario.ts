/**
 * Volunteer Event Scenario
 *
 * Creates an event with volunteer positions for testing volunteer signup flows.
 */

import type { APIRequestContext } from '@playwright/test';
import { EventFactory } from '../factories/event.factory';
import { SessionFactory } from '../factories/session.factory';
import { VolunteerFactory } from '../factories/volunteer.factory';
import type {
  EventResponse,
  SessionResponse,
  VolunteerPositionResponse,
} from '../types';

export interface VolunteerEventData {
  event: EventResponse;
  session: SessionResponse;
  positions: VolunteerPositionResponse[];
}

export interface VolunteerPositionConfig {
  title: string;
  slots?: number;
}

export interface VolunteerEventOptions {
  /** Event title */
  title?: string;
  /** Volunteer positions to create */
  positions?: VolunteerPositionConfig[];
  /** Start date for the event (defaults to tomorrow) */
  startDate?: Date;
}

/**
 * Default volunteer positions for a typical event
 */
const DEFAULT_POSITIONS: VolunteerPositionConfig[] = [
  { title: 'Door Greeter', slots: 2 },
  { title: 'Setup Helper', slots: 3 },
  { title: 'Cleanup Crew', slots: 3 },
];

/**
 * Create an event with volunteer positions
 *
 * @example
 * const { event, session, positions } = await createVolunteerEvent(request, {
 *   title: 'Community Workshop',
 *   positions: [
 *     { title: 'Door Greeter', slots: 2 },
 *     { title: 'Setup Helper', slots: 3 }
 *   ]
 * });
 */
export async function createVolunteerEvent(
  request: APIRequestContext,
  options: VolunteerEventOptions = {}
): Promise<VolunteerEventData> {
  const eventFactory = new EventFactory(request);
  const sessionFactory = new SessionFactory(request);
  const volunteerFactory = new VolunteerFactory(request);

  // Calculate dates
  const now = new Date();
  const startDate =
    options.startDate ?? new Date(now.getTime() + 24 * 60 * 60 * 1000);
  const endDate = new Date(startDate.getTime() + 4 * 60 * 60 * 1000); // 4 hours

  // Create event
  const event = await eventFactory.create({
    title: options.title ?? `Volunteer Event ${Date.now()}`,
    startDate,
    endDate,
    eventType: 'Social',
    status: 'Published',
    isPublic: true,
  });

  // Create a session
  const session = await sessionFactory.create({
    eventId: event.id,
    title: 'Main Session',
    startTime: startDate,
    endTime: endDate,
  });

  // Create volunteer positions
  const positionConfigs = options.positions ?? DEFAULT_POSITIONS;
  const positions: VolunteerPositionResponse[] = [];

  for (const config of positionConfigs) {
    const position = await volunteerFactory.create({
      eventId: event.id,
      title: config.title,
      slotsAvailable: config.slots ?? 2,
      startTime: startDate,
      endTime: endDate,
    });
    positions.push(position);
  }

  return { event, session, positions };
}

/**
 * Create a simple volunteer event with default positions
 */
export async function createSimpleVolunteerEvent(
  request: APIRequestContext,
  title?: string
): Promise<VolunteerEventData> {
  return createVolunteerEvent(request, { title });
}

/**
 * Create a volunteer event with a single position
 */
export async function createSinglePositionEvent(
  request: APIRequestContext,
  positionTitle: string,
  slots: number = 5
): Promise<VolunteerEventData> {
  return createVolunteerEvent(request, {
    positions: [{ title: positionTitle, slots }],
  });
}

/**
 * Cleanup volunteer event data
 *
 * Note: Deleting the event should cascade delete positions
 */
export async function cleanupVolunteerEvent(
  request: APIRequestContext,
  data: VolunteerEventData
): Promise<void> {
  const eventFactory = new EventFactory(request);

  // Deleting the event cascades to session and positions
  await eventFactory.delete(data.event.id);
}
