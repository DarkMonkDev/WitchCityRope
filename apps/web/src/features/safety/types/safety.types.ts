// Safety System TypeScript Types
// Based on backend API design and UI specifications

export interface SafetyIncidentDto {
  id: string;
  referenceNumber: string;
  reporterId?: string;
  reporterName?: string;
  severity: IncidentSeverity;
  incidentDate: string; // ISO date string
  reportedAt: string; // ISO date string
  location: string;
  description: string;
  involvedParties?: string;
  witnesses?: string;
  contactEmail?: string;
  contactPhone?: string;
  isAnonymous: boolean;
  requestFollowUp: boolean;
  status: IncidentStatus;
  assignedTo?: string;
  assignedUserName?: string;
  auditTrail: AuditLogDto[];
  createdAt: string;
  updatedAt: string;
}

export interface IncidentSummaryDto {
  id: string;
  referenceNumber: string;
  severity: IncidentSeverity;
  incidentDate: string;
  reportedAt: string;
  location: string;
  isAnonymous: boolean;
  status: IncidentStatus;
  assignedTo?: string;
  assignedUserName?: string;
}

export interface SubmitIncidentRequest {
  reporterId?: string;
  severity: IncidentSeverity;
  incidentDate: string; // ISO date string
  location: string;
  description: string;
  involvedParties?: string;
  witnesses?: string;
  isAnonymous: boolean;
  requestFollowUp: boolean;
  contactEmail?: string;
  contactPhone?: string;
}

export interface SubmissionResponse {
  referenceNumber: string;
  trackingUrl: string;
  submittedAt: string;
}

export interface IncidentStatusResponse {
  referenceNumber: string;
  status: string;
  lastUpdated: string;
  canProvideMoreInfo: boolean;
}

export interface UpdateIncidentRequest {
  status?: IncidentStatus;
  assignedTo?: string;
  notes?: string;
}

export interface SafetyDashboardResponse {
  statistics: SafetyStatistics;
  recentIncidents: IncidentSummaryDto[];
  pendingActions: ActionItem[];
}

export interface SafetyStatistics {
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  totalCount: number;
  newCount: number;
  inProgressCount: number;
  resolvedCount: number;
  thisMonth: number;
}

export interface ActionItem {
  incidentId: string;
  referenceNumber: string;
  actionNeeded: string;
  priority: IncidentSeverity;
  dueDate: string;
}

export interface AuditLogDto {
  id: string;
  actionType: string;
  actionDescription: string;
  userId?: string;
  userName?: string;
  createdAt: string;
}

export interface SearchIncidentsRequest {
  status?: IncidentStatus;
  minSeverity?: IncidentSeverity;
  assignedTo?: string;
  dateFrom?: string;
  dateTo?: string;
  searchText?: string;
  page?: number;
  pageSize?: number;
}

// Enums
export enum IncidentSeverity {
  Low = 'Low',
  Medium = 'Medium', 
  High = 'High',
  Critical = 'Critical'
}

export enum IncidentStatus {
  New = 'New',
  InProgress = 'InProgress',
  Resolved = 'Resolved',
  Archived = 'Archived'
}

// Frontend-specific types
export interface IncidentFormData {
  severity: IncidentSeverity;
  incidentDate: string; // Date in YYYY-MM-DD format
  incidentTime: string; // Time in HH:MM format
  location: string;
  description: string;
  involvedParties: string;
  witnesses: string;
  isAnonymous: boolean;
  requestFollowUp: boolean;
  contactEmail: string;
  contactPhone: string;
}

export interface SeverityConfig {
  value: IncidentSeverity;
  label: string;
  color: string;
  description: string;
  icon: string;
}

// Severity configurations for UI
export const SEVERITY_CONFIGS: Record<IncidentSeverity, SeverityConfig> = {
  [IncidentSeverity.Low]: {
    value: IncidentSeverity.Low,
    label: 'Low',
    color: '#228B22',
    description: 'Minor issue, no immediate danger',
    icon: '🟢'
  },
  [IncidentSeverity.Medium]: {
    value: IncidentSeverity.Medium,
    label: 'Medium',
    color: '#DAA520',
    description: 'Concerning issue requiring attention',
    icon: '🟡'
  },
  [IncidentSeverity.High]: {
    value: IncidentSeverity.High,
    label: 'High',
    color: '#DC143C',
    description: 'Serious issue requiring immediate review',
    icon: '🔴'
  },
  [IncidentSeverity.Critical]: {
    value: IncidentSeverity.Critical,
    label: 'Critical',
    color: '#8B0000',
    description: 'Emergency requiring immediate action',
    icon: '🚨'
  }
};

// Status configurations for UI
export const STATUS_CONFIGS: Record<IncidentStatus, { label: string; color: string }> = {
  [IncidentStatus.New]: { label: 'New', color: 'blue' },
  [IncidentStatus.InProgress]: { label: 'In Progress', color: 'yellow' },
  [IncidentStatus.Resolved]: { label: 'Resolved', color: 'green' },
  [IncidentStatus.Archived]: { label: 'Archived', color: 'gray' }
};