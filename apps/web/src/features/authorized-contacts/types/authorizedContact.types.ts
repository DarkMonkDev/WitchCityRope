/**
 * Authorized Contacts Types - Using Auto-Generated Types
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
 * Terminology:
 * - Principal: The person granting authority (the account owner)
 * - Delegate: The person receiving authority (can act on principal's behalf)
 */

import type { components } from '@witchcityrope/shared-types'

/**
 * A single authorized contact relationship.
 * Source: C# AuthorizedContactDto via NSwag generation
 */
export type AuthorizedContactDto = components['schemas']['AuthorizedContactDto']

/**
 * Response from GET /api/authorized-contacts - returns both directions.
 * Source: C# AuthorizedContactsListDto via NSwag generation
 */
export type AuthorizedContactsListDto = components['schemas']['AuthorizedContactsListDto']

/**
 * Request body for POST /api/authorized-contacts.
 * Source: C# AddAuthorizedContactRequest via NSwag generation
 */
export type AddAuthorizedContactRequest = components['schemas']['AddAuthorizedContactRequest']

/**
 * Search result for member scene name lookup (privacy-scoped: scene name only per AD-009).
 * Named ContactSearchResultDto to distinguish from Volunteers' UserSearchResultDto
 * which includes Email, DiscordName, FullName for admin-level search.
 * Source: C# ContactSearchResultDto via NSwag generation
 */
export type ContactSearchResultDto = components['schemas']['ContactSearchResultDto']

/**
 * A principal contact with vetting status.
 * Used in checkout/RSVP flows to show who the current user can act for.
 * Source: C# PrincipalContactDto via NSwag generation
 */
export type PrincipalContactDto = components['schemas']['PrincipalContactDto']
