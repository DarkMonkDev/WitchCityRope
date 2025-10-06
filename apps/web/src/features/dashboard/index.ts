// Dashboard Feature Exports
// Central export file for the dashboard feature

// Components
export { UserDashboard } from './components/UserDashboard';
export { UpcomingEvents } from './components/UpcomingEvents';
export { MembershipStatistics } from './components/MembershipStatistics';

// Hooks
export {
  useUserDashboard,
  useUserEvents,
  useUserStatistics,
  useDashboardData,
  useDashboardError,
  dashboardQueryKeys
} from './hooks/useDashboard';

// API Service
export { dashboardApi, dashboardApiUtils } from './api/dashboardApi';

// Types
export type {
  UserDashboardResponse,
  UserEventsResponse,
  DashboardEventDto,
  UserStatisticsResponse,
  VettingStatus,
  VettingStatusDisplay,
  RegistrationStatusDisplay
} from './types/dashboard.types';

export {
  DashboardUtils
} from './types/dashboard.types';