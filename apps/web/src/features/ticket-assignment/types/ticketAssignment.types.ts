/**
 * Ticket Assignment Types - Using Auto-Generated Types
 *
 * DTO ALIGNMENT STRATEGY - CRITICAL RULES:
 * ========================================
 * 1. API DTOs (C#) are the SOURCE OF TRUTH
 * 2. TypeScript types are AUTO-GENERATED from OpenAPI spec via @witchcityrope/shared-types
 * 3. NEVER manually create TypeScript interfaces for API request/response data
 * 4. If a type is missing, expose it in the backend API (add .Produces<> to endpoint)
 * 5. Regenerate types: cd packages/shared-types && npm run generate
 *
 * WHY: Prevents type mismatches, ensures type safety, eliminates manual sync work
 * SEE: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 * ========================================
 *
 * Includes checkout types (Section 1B) and dashboard/assignment types (Sections 1C, 1E).
 */

import type { components } from '@witchcityrope/shared-types'

// ---------------------------------------------------------------------------
// Checkout Types (API Section 1B)
// ---------------------------------------------------------------------------

/**
 * A single ticket selection with quantity and optional assignees.
 * Source: C# TicketSelectionItem via NSwag generation
 */
export type TicketSelectionItem = components['schemas']['TicketSelectionItem']

/**
 * Result of a ticket assignment from checkout response.
 * Source: C# TicketAssignmentResultDto via NSwag generation
 */
export type TicketAssignmentResultDto = components['schemas']['TicketAssignmentResultDto']

// ---------------------------------------------------------------------------
// Assignment Operation Types (API Section 1C)
// ---------------------------------------------------------------------------

/**
 * Response for assignment operations (EP-08, EP-09, EP-10, EP-11).
 * Source: C# TicketAssignmentDto via NSwag generation
 */
export type TicketAssignmentDto = components['schemas']['TicketAssignmentDto']

/**
 * Request body for assigning a ticket (EP-08, EP-11).
 * Source: C# AssignTicketRequest via NSwag generation
 */
export type AssignTicketRequest = components['schemas']['AssignTicketRequest']

/**
 * Request body for accepting an assigned ticket or proxy RSVP (EP-09).
 * Source: C# AcceptAssignmentRequest via NSwag generation
 */
export type AcceptAssignmentRequest = components['schemas']['AcceptAssignmentRequest']

/**
 * Request body for declining - optional reason (EP-10).
 * Source: C# DeclineAssignmentRequest via NSwag generation
 */
export type DeclineAssignmentRequest = components['schemas']['DeclineAssignmentRequest']

// ---------------------------------------------------------------------------
// Dashboard Types (API Section 1E)
// ---------------------------------------------------------------------------

/**
 * Pending assignment for the current user's dashboard (EP-16).
 * Represents a ticket or RSVP that someone purchased/created on behalf of this user
 * and is awaiting their acceptance.
 *
 * GET /api/user/pending-assignments
 * Source: C# PendingAssignmentDto via NSwag generation
 */
export type PendingAssignmentDto = components['schemas']['PendingAssignmentDto']

/**
 * Tickets the current user purchased that are assigned to others or unassigned (EP-17).
 * Used on the purchaser's dashboard to show status and allow assign/reassign actions.
 *
 * GET /api/user/assigned-tickets
 * Source: C# AssignedTicketStatusDto via NSwag generation
 */
export type AssignedTicketStatusDto = components['schemas']['AssignedTicketStatusDto']
