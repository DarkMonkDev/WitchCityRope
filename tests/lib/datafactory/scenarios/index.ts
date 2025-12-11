/**
 * Scenario Exports
 *
 * Re-export all scenarios for convenient importing.
 *
 * Scenarios provide pre-configured test data setups for common testing needs.
 * Use scenarios instead of manually creating related entities.
 */

// Complete Event Scenario
export {
  createCompleteEvent,
  cleanupCompleteEvent,
  createTicketedEvent as createCompleteTicketedEvent,
  createWorkshopEvent,
  type CompleteEventData,
  type CompleteEventOptions,
} from './complete-event.scenario';

// Vetting Workflow Scenario
export {
  createVettingWorkflow,
  createPendingVettingApplication,
  createApprovedVettingApplication,
  createRejectedVettingApplication,
  createInReviewVettingApplication,
  cleanupVettingWorkflow,
  type VettingWorkflowData,
  type VettingWorkflowOptions,
} from './vetting-workflow.scenario';

// Volunteer Event Scenario
export {
  createVolunteerEvent,
  createSimpleVolunteerEvent,
  createSinglePositionEvent,
  cleanupVolunteerEvent,
  type VolunteerEventData,
  type VolunteerEventOptions,
  type VolunteerPositionConfig,
} from './volunteer-event.scenario';

// Ticketed Event Scenario
export {
  createTicketedEvent,
  createFreeEvent,
  createPaidEvent,
  createLimitedCapacityEvent,
  createTicketedEventWithPurchase,
  createEventWithAttendees,
  cleanupTicketedEvent,
  cleanupTicketedEventWithPurchase,
  type TicketedEventData,
  type TicketedEventWithPurchaseData,
  type TicketedEventOptions,
} from './ticketed-event.scenario';
