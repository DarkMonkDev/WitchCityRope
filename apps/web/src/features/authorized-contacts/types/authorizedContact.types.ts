/**
 * Authorized Contacts Types
 *
 * Types for the Authorized Contacts feature, which allows users to designate
 * other members who can purchase tickets and RSVP on their behalf.
 *
 * These are frontend-only types for now since the backend API endpoints
 * have not yet been created. Once backend DTOs exist, these should be
 * replaced with auto-generated types from @witchcityrope/shared-types.
 *
 * Terminology:
 * - Principal: The person granting authority (the account owner)
 * - Delegate: The person receiving authority (can act on principal's behalf)
 */

/** A single authorized contact relationship */
export interface AuthorizedContactDto {
  /** Unique relationship ID */
  id: string
  /** The other user's ID in this relationship */
  userId: string
  /** The other user's scene name (privacy-safe display name per AD-009) */
  sceneName: string
  /** ISO 8601 timestamp of when the relationship was created */
  createdAt: string
}

/** Response from GET /api/authorized-contacts - returns both directions */
export interface AuthorizedContactsListDto {
  /** People I have authorized to act on my behalf (I am the principal) */
  delegates: AuthorizedContactDto[]
  /** People who have authorized me to act on their behalf (I am the delegate) */
  principals: AuthorizedContactDto[]
}

/** Request body for POST /api/authorized-contacts */
export interface AddAuthorizedContactRequest {
  /** The user ID of the person being granted delegate authority */
  delegateUserId: string
}

/** Search result for member scene name lookup */
export interface UserSearchResultDto {
  /** The user's ID */
  userId: string
  /** The user's scene name (only field exposed per AD-009 privacy) */
  sceneName: string
}

/**
 * A principal contact with vetting status.
 * Used in checkout/RSVP flows to show who the current user can act for.
 */
export interface PrincipalContactDto {
  userId: string
  sceneName: string
  isVetted: boolean
}
