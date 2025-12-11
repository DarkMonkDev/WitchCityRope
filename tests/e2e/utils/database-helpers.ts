/**
 * Database Helpers - Re-export from canonical location
 *
 * DEPRECATED: This file is kept for backwards compatibility.
 * New code should import from './test-utils/utils/database-helpers'
 *
 * The canonical source is: tests/e2e/test-utils/utils/database-helpers.ts
 */

// Re-export everything from the canonical location
export * from '../test-utils/utils/database-helpers';
export { DatabaseHelpers } from '../test-utils/utils/database-helpers';

// Note: This file can be deleted once all imports are updated
// to use the canonical location directly.
