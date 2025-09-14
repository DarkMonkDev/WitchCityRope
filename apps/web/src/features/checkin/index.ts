// CheckIn Feature Index
// Main exports for the CheckIn system

// Components
export { 
  AttendeeSearch, 
  CompactAttendeeSearch,
  AttendeeList,
  CheckInConfirmation,
  CheckInInterface
} from './components';
export { CheckInDashboard } from './components/CheckInDashboard';
export { SyncStatus, CompactSyncStatus, SyncProgress } from './components/SyncStatus';

// Hooks
export * from './hooks/useCheckIn';
export * from './hooks/useOfflineSync';

// Types
export * from './types/checkin.types';

// API
export { checkinApi } from './api/checkinApi';

// Utils
export { offlineStorage } from './utils/offlineStorage';