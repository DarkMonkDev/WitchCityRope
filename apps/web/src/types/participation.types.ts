// Participation types based on backend DTOs
// This file provides TypeScript types for RSVP functionality

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

export interface CreateRSVPRequest {
  eventId: string;
  notes?: string;
}

export interface UserParticipationDto {
  id: string;
  eventId: string;
  eventTitle: string;
  eventDate: string;
  eventLocation: string;
  participationType: ParticipationType;
  status: ParticipationStatus;
  amount?: number;
  createdAt: string;
  canceledAt?: string;
  cancelReason?: string;
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