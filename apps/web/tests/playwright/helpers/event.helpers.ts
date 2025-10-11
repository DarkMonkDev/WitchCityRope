/**
 * Event Helpers for E2E Tests
 *
 * Provides dynamic event fetching from API to avoid hardcoded event IDs.
 * CRITICAL: Tests should use real database event IDs, not hardcoded values.
 *
 * Created: 2025-10-11
 * Reason: Database reseeding invalidated hardcoded event IDs
 */

import { Page } from '@playwright/test';

export interface EventData {
  id: string;
  title: string;
  shortDescription?: string;
  description?: string;
  eventType?: string;
  startDate?: string;
  endDate?: string;
}

/**
 * Fetch first active event from API
 *
 * Use this instead of hardcoded event IDs to ensure tests work
 * with any seed data.
 */
export async function getFirstActiveEvent(page: Page): Promise<EventData> {
  const response = await page.request.get('http://localhost:5655/api/events');
  const apiResponse = await response.json();

  // API returns ApiResponse<T> wrapper format
  if (!apiResponse.success || !apiResponse.data || apiResponse.data.length === 0) {
    throw new Error('No events found in database');
  }

  const firstEvent = apiResponse.data[0];
  return {
    id: firstEvent.id,
    title: firstEvent.title,
    shortDescription: firstEvent.shortDescription,
    description: firstEvent.description,
    eventType: firstEvent.eventType,
    startDate: firstEvent.startDate,
    endDate: firstEvent.endDate,
  };
}

/**
 * Fetch event by title (partial match)
 *
 * Useful when you need a specific type of event but ID may change.
 */
export async function getEventByTitle(page: Page, titleContains: string): Promise<EventData> {
  const response = await page.request.get('http://localhost:5655/api/events');
  const apiResponse = await response.json();

  if (!apiResponse.success || !apiResponse.data) {
    throw new Error('Failed to fetch events from API');
  }

  const event = apiResponse.data.find((e: any) =>
    e.title.toLowerCase().includes(titleContains.toLowerCase())
  );

  if (!event) {
    throw new Error(`No event found matching: ${titleContains}`);
  }

  return {
    id: event.id,
    title: event.title,
    shortDescription: event.shortDescription,
    description: event.description,
    eventType: event.eventType,
    startDate: event.startDate,
    endDate: event.endDate,
  };
}

/**
 * Fetch all active events from API
 *
 * Use for tests that need multiple events.
 */
export async function getAllActiveEvents(page: Page): Promise<EventData[]> {
  const response = await page.request.get('http://localhost:5655/api/events');
  const apiResponse = await response.json();

  if (!apiResponse.success || !apiResponse.data) {
    throw new Error('Failed to fetch events from API');
  }

  return apiResponse.data.map((e: any) => ({
    id: e.id,
    title: e.title,
    shortDescription: e.shortDescription,
    description: e.description,
    eventType: e.eventType,
    startDate: e.startDate,
    endDate: e.endDate,
  }));
}

/**
 * Fetch event by ID from API
 *
 * Use for verifying specific event details.
 */
export async function getEventById(page: Page, eventId: string): Promise<EventData> {
  const response = await page.request.get(`http://localhost:5655/api/events/${eventId}`);
  const apiResponse = await response.json();

  if (!apiResponse.success || !apiResponse.data) {
    throw new Error(`Event not found: ${eventId}`);
  }

  const event = apiResponse.data;
  return {
    id: event.id,
    title: event.title,
    shortDescription: event.shortDescription,
    description: event.description,
    eventType: event.eventType,
    startDate: event.startDate,
    endDate: event.endDate,
  };
}

/**
 * Fetch multiple events by type
 *
 * @param page Playwright page object
 * @param count Number of events to fetch
 * @returns Array of event data
 */
export async function getMultipleEvents(page: Page, count: number = 4): Promise<EventData[]> {
  const allEvents = await getAllActiveEvents(page);
  return allEvents.slice(0, count);
}

export const EventHelpers = {
  getFirstActiveEvent,
  getEventByTitle,
  getAllActiveEvents,
  getEventById,
  getMultipleEvents,
};
