/**
 * Structural type for any object that has role/roles properties.
 * Works with UserDto, MemberDetailsResponse, and any other user-like object.
 */
interface RoleHolder {
  role?: string | null;
  roles?: string[] | null;
}

/**
 * Check if a user has a specific role.
 * Prefers the `roles` array (properly split by backend); falls back to
 * parsing the `role` CSV string for backward compatibility during deployment.
 * Case-insensitive comparison.
 */
export function hasRole(user: RoleHolder | null | undefined, role: string): boolean {
  if (!user) return false;
  const lowerRole = role.toLowerCase();

  // Primary: check roles array (properly split by backend)
  if (user.roles && user.roles.length > 0) {
    return user.roles.some(r => r.toLowerCase() === lowerRole);
  }

  // Fallback: parse comma-separated role string (backward compat during deployment)
  if (user.role) {
    return user.role.split(',').some(r => r.trim().toLowerCase() === lowerRole);
  }

  return false;
}

/**
 * Check if a user has ANY of the specified roles.
 * Useful for checks like "is user an Administrator or Teacher?"
 */
export function hasAnyRole(user: RoleHolder | null | undefined, roles: string[]): boolean {
  return roles.some(role => hasRole(user, role));
}

/**
 * Get all roles as an array.
 * Prefers the `roles` array; falls back to splitting the `role` CSV string.
 */
export function getUserRoles(user: RoleHolder | null | undefined): string[] {
  if (!user) return [];
  if (user.roles && user.roles.length > 0) return user.roles;
  if (user.role) return user.role.split(',').map(r => r.trim()).filter(Boolean);
  return [];
}
