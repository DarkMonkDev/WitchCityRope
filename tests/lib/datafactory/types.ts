/**
 * DataFactory Types
 *
 * All types for test data creation/deletion.
 * These types mirror the backend TestHelper DTOs.
 */

// ============================================
// USER TYPES
// ============================================

export interface CreateUserRequest {
  email: string;
  password?: string; // Defaults to Test123!
  sceneName?: string; // Display name in community (required by backend, defaults to firstName)
  firstName?: string;
  lastName?: string;
  role?: string; // Single role: 'Guest', 'Member', 'VettedMember', 'Teacher', 'Admin'
  dateOfBirth?: string; // Format: YYYY-MM-DD
  vettingStatus?: number; // 0=UnderReview, 3=Approved
  bio?: string;
  pronouns?: string;
}

export interface UserResponse {
  id: string;
  email: string;
  sceneName: string;
  firstName: string;
  lastName: string;
  role: string;
}

// ============================================
// EVENT TYPES
// ============================================

export type EventType = 'Class' | 'Social' | 'Performance' | 'Workshop';
export type EventStatus = 'Draft' | 'Published' | 'Cancelled';

export interface CreateEventRequest {
  title: string;
  shortDescription?: string;
  description?: string;
  startDate: Date;
  endDate: Date;
  eventType?: EventType;
  status?: EventStatus;
  isPublic?: boolean;
  venueId?: string;
}

export interface EventResponse {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  status: string;
}

// ============================================
// SESSION TYPES
// ============================================

export interface CreateSessionRequest {
  eventId: string;
  title: string;
  description?: string;
  startTime: Date;
  endTime: Date;
  maxCapacity?: number;
  requiresRegistration?: boolean;
}

export interface SessionResponse {
  id: string;
  eventId: string;
  title: string;
  startTime: string;
  endTime: string;
}

// ============================================
// TICKET TYPE TYPES
// ============================================

export interface CreateTicketTypeRequest {
  /** Single session ID (will be converted to sessionIds array) */
  sessionId?: string;
  /** Multiple session IDs for multi-session tickets */
  sessionIds?: string[];
  /** Event ID (REQUIRED by backend) */
  eventId: string;
  name: string;
  description?: string;
  price: number;
  quantityAvailable?: number;
  isActive?: boolean;
  salesStartDate?: Date;
  salesEndDate?: Date;
}

export interface TicketTypeResponse {
  id: string;
  sessionId?: string;
  sessionIds?: string[];
  name: string;
  price: number;
  quantityAvailable: number;
}

// ============================================
// TICKET PURCHASE TYPES
// ============================================

export interface CreateTicketPurchaseRequest {
  userId: string;
  ticketTypeId: string;
  quantity?: number;
}

export interface TicketPurchaseResponse {
  id: string;
  userId: string;
  ticketTypeId: string;
  quantity: number;
  purchaseDate: string;
}

// ============================================
// VOLUNTEER TYPES
// ============================================

export interface CreateVolunteerPositionRequest {
  eventId: string;
  title: string;
  description?: string;
  slotsAvailable?: number;
  startTime?: Date;
  endTime?: Date;
}

export interface VolunteerPositionResponse {
  id: string;
  eventId: string;
  title: string;
  slotsAvailable: number;
}

// ============================================
// VETTING TYPES
// ============================================

export type VettingStatus = 'Pending' | 'InReview' | 'Approved' | 'Rejected';

export interface CreateVettingApplicationRequest {
  userId: string;
  status?: VettingStatus;
  notes?: string;
}

export interface VettingApplicationResponse {
  id: string;
  userId: string;
  status: string;
  createdAt: string;
}

// ============================================
// CLEANUP TRACKING
// ============================================

export type CleanupType =
  | 'user'
  | 'event'
  | 'session'
  | 'ticketType'
  | 'ticketPurchase'
  | 'volunteerPosition'
  | 'vettingApplication';

export interface CleanupItem {
  type: CleanupType;
  id: string;
}

export interface TestContext {
  cleanupItems: CleanupItem[];
  addCleanup: (item: CleanupItem) => void;
  cleanup: () => Promise<void>;
}
