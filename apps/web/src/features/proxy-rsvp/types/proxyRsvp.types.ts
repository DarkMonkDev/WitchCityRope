/**
 * Proxy RSVP Types - Using Auto-Generated Types
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
 * Types for the Proxy RSVP feature, which allows delegates to create
 * RSVPs on behalf of their principals (authorized contacts).
 * Aligns with backend DTOs from api-design.md Section 1D.
 */

import type { components } from '@witchcityrope/shared-types'

/**
 * Request to create a proxy RSVP.
 * Source: C# CreateProxyRsvpRequest via NSwag generation
 */
export type CreateProxyRsvpRequest = components['schemas']['CreateProxyRsvpRequest']

/**
 * Response DTO for a proxy RSVP.
 * Source: C# ProxyRsvpDto via NSwag generation
 */
export type ProxyRsvpDto = components['schemas']['ProxyRsvpDto']
