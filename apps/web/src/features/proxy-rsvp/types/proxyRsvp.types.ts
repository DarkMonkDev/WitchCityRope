/**
 * Proxy RSVP Types
 *
 * Types for the Proxy RSVP feature, which allows delegates to create
 * RSVPs on behalf of their principals (authorized contacts).
 *
 * Aligns with backend DTOs from api-design.md Section 1D.
 */

/** Request to create a proxy RSVP */
export interface CreateProxyRsvpRequest {
  /** The user ID of the person being RSVP'd for (the principal) */
  rsvpForUserId: string;
  /** Optional notes about the RSVP */
  notes?: string;
}

/** Response DTO for a proxy RSVP */
export interface ProxyRsvpDto {
  attendanceId: string;
  eventId: string;
  eventTitle: string;
  status: string;
  rsvpForSceneName: string;
  rsvpForUserId: string;
  createdBySceneName: string;
  createdByUserId: string;
  createdAt: string;
}
