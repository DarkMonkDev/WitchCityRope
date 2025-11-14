/**
 * Dashboard-specific TypeScript types
 *
 * ✅ DTO ALIGNMENT STRATEGY COMPLIANT
 * All types imported from @witchcityrope/shared-types (auto-generated from backend DTOs)
 * Source: C# Dashboard DTOs via NSwag generation
 */

import type { components } from '@witchcityrope/shared-types';

/**
 * User's registered event information for dashboard display
 * Source: C# UserEventDto via NSwag generation
 *
 * CRITICAL: This is NOT PublicEventDto - different fields for dashboard context
 * This is for the user's own events dashboard, NOT public sales page
 */
export type UserEventDto = components['schemas']['UserEventDto'];

/**
 * User's vetting status for alert box display on dashboard
 * Source: C# VettingStatusDto via NSwag generation
 */
export type VettingStatusDto = components['schemas']['VettingStatusDto'];

/**
 * User profile information for settings page
 * Source: C# UserProfileDto via NSwag generation
 */
export type UserProfileDto = components['schemas']['UserProfileDto'];

/**
 * Update profile request DTO
 * Source: C# UpdateProfileDto via NSwag generation
 */
export type UpdateProfileDto = components['schemas']['UpdateProfileDto'];

/**
 * Change password request DTO
 * Source: C# ChangePasswordDto via NSwag generation
 */
export type ChangePasswordDto = components['schemas']['ChangePasswordDto'];
