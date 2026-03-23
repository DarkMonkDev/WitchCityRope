/**
 * Admin Role-Based Access Control Constants
 *
 * Single source of truth for which roles can access the admin dashboard
 * and which dashboard cards each role can see.
 *
 * Used by:
 * - adminLoader (route protection)
 * - Navigation (admin link visibility)
 * - AdminDashboardPage (card filtering)
 *
 * To add a new admin-capable role:
 * 1. Add the role string to ADMIN_CAPABLE_ROLES
 * 2. Add the role to relevant cards in DASHBOARD_CARD_ROLES
 * 3. Update defense-in-depth checks in any page components the role should access
 */

/**
 * Roles that grant access to the /admin route.
 * Users with any of these roles will see the "Admin" link in navigation
 * and can access the admin dashboard (seeing only their permitted cards).
 *
 * Note: Teacher and DungeonMonitor are intentionally excluded —
 * they have no admin dashboard features.
 */
export const ADMIN_CAPABLE_ROLES: string[] = [
  'Administrator',
  'EventOrganizer',
  'SafetyTeam',
  'VettingTeam',
];

/**
 * Maps each dashboard card title to the roles that can see it.
 * Administrator is included in every card (full access).
 *
 * Card titles must exactly match the `title` field in AdminDashboardPage's
 * dashboardCards array — this coupling is intentional so changes to card
 * titles are caught at development time (card disappears if title mismatches).
 */
export const DASHBOARD_CARD_ROLES: Record<string, string[]> = {
  'Events Management': ['Administrator', 'EventOrganizer'],
  'Member Management': ['Administrator'],
  'Vetting Applications': ['Administrator', 'VettingTeam'],
  'Incident Reports': ['Administrator', 'SafetyTeam'],
  'Email Templates': ['Administrator'],
  'Content Management': ['Administrator'],
  'Reports': ['Administrator'],
  'Settings': ['Administrator'],
};
