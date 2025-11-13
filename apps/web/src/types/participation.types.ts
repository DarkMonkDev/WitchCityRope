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
 * Create RSVP Request DTO
 * @generated from C# CreateRSVPRequest via OpenAPI
 */
export type CreateRSVPRequest = components['schemas']['CreateRSVPRequest']

export interface ParticipationStatusDto {
  hasRSVP: boolean;
  hasTicket: boolean;
  rsvp?: {
    id: string;
    status: ParticipationStatus;
    createdAt: string;
    canceledAt?: string;
    cancelReason?: string;
  } | null;
  ticket?: {
    id: string;
    status: ParticipationStatus;
    amount: number;
    paymentStatus: string;
    createdAt: string;
    canceledAt?: string;
    cancelReason?: string;
  } | null;
  canRSVP: boolean;
  canPurchaseTicket: boolean;
  capacity?: {
    total: number;
    current: number;
    available: number;
  };
}

export interface UserParticipationDto {
  id: string;
  eventId: string;
  eventTitle: string;
  eventStartDate: string;
  eventEndDate: string;
  eventLocation: string;
  participationType: ParticipationType;
  status: ParticipationStatus;
  participationDate: string;
  notes?: string;
  canCancel: boolean;
}

export enum ParticipationType {
  RSVP = 'RSVP',
  Ticket = 'Ticket'
}

export enum ParticipationStatus {
  Active = 'Active',
  Cancelled = 'Cancelled',
  Refunded = 'Refunded',
  Waitlisted = 'Waitlisted'
}

// Frontend-specific types for UI components
export interface ParticipationCardProps {
  eventId: string;
  eventType: 'social' | 'class';
  participation: ParticipationStatusDto | null;
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