/**
 * Participation Types - Using Auto-Generated Types
 *
 * DTO ALIGNMENT STRATEGY - CRITICAL RULES:
 * ════════════════════════════════════════
 * 1. API DTOs (C#) are the SOURCE OF TRUTH
 * 2. TypeScript types are AUTO-GENERATED from OpenAPI spec via @witchcityrope/shared-types
 * 3. NEVER manually create TypeScript interfaces for API request/response data
 * 4. If a type is missing, expose it in the backend API (add .Produces<> to endpoint)
 * 5. Regenerate types: cd packages/shared-types && npm run generate
 *
 * WHY: Prevents type mismatches, ensures type safety, eliminates manual sync work
 * SEE: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 * ════════════════════════════════════════
 */

import type { components } from '@witchcityrope/shared-types'

/**
 * ✅ DTO ALIGNMENT STRATEGY COMPLIANT
 * All backend DTOs imported from @witchcityrope/shared-types (auto-generated)
 */

/**
 * Create RSVP Request DTO
 * Source: C# CreateRSVPRequest via NSwag generation
 */
export type CreateRSVPRequest = components['schemas']['CreateRSVPRequest']

/**
 * Participation Status DTO
 * Source: C# ParticipationStatusDto via NSwag generation
 */
export type ParticipationStatusDto = components['schemas']['ParticipationStatusDto']

/**
 * Enhanced Participation Status DTO (includes capacity and availability info)
 * Source: C# EnhancedParticipationStatusDto via NSwag generation
 */
export type EnhancedParticipationStatusDto = components['schemas']['EnhancedParticipationStatusDto']

/**
 * User Participation DTO
 * Source: C# UserParticipationDto via NSwag generation
 */
export type UserParticipationDto = components['schemas']['UserParticipationDto']

/**
 * Attendance Type Enum (was ParticipationType)
 * Source: C# AttendanceType via NSwag generation
 * Values: "RSVP" | "Ticket"
 */
export type AttendanceType = components['schemas']['AttendanceType']

/**
 * Attendance Status Enum (was ParticipationStatus)
 * Source: C# AttendanceStatus via NSwag generation
 * Values: "Active" | "Cancelled" | "Refunded" | "Waitlisted"
 */
export type AttendanceStatus = components['schemas']['AttendanceStatus']

// Frontend-specific types for UI components
export interface ParticipationCardProps {
  eventId: string;
  eventType: 'social' | 'class';
  participation: EnhancedParticipationStatusDto | null;
  isLoading?: boolean;
  onRSVP: () => void;
  onPurchaseTicket: (amount: number) => void;
  onCancel: (type: 'rsvp' | 'ticket') => void;
}

export interface RSVPConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (notes?: string) => void;
  eventTitle: string;
  isSubmitting?: boolean;
}

export interface CancelParticipationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (reason?: string) => void;
  participationType: 'rsvp' | 'ticket';
  eventTitle: string;
  isSubmitting?: boolean;
}