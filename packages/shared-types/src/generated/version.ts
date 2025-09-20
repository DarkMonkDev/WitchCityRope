// Auto-generated version information
export const SHARED_TYPES_VERSION = '1.0.0';
export const GENERATED_AT = '2025-09-20T01:40:15.161Z';
export const API_VERSION = 'v1';

// Runtime version checking utility
export function checkApiCompatibility(serverVersion: string): boolean {
    // For now, just check if major version matches
    const [major] = API_VERSION.substring(1).split('.');
    const [serverMajor] = serverVersion.substring(1).split('.');
    return major === serverMajor;
}
