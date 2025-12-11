/**
 * Fixture Exports
 *
 * Playwright fixtures for DataFactory lifecycle management.
 *
 * CHOOSING THE RIGHT FIXTURE:
 *
 * - Use `test.fixture.ts` when:
 *   - Each test needs isolated data
 *   - Tests should not share state
 *   - Automatic cleanup is desired
 *
 * - Use `worker.fixture.ts` when:
 *   - Multiple tests share the same base data (e.g., same event)
 *   - Setup is expensive and should be reused
 *   - You understand cleanup responsibilities
 *
 * @example
 * // For isolated tests (most common)
 * import { test, expect } from '@tests/lib/datafactory/fixtures/test.fixture';
 *
 * test('my test', async ({ df }) => {
 *   const user = await df.users.createVerified({ email: 'test@example.com' });
 *   // Automatically cleaned up after test
 * });
 *
 * @example
 * // For tests sharing data
 * import { test, expect } from '@tests/lib/datafactory/fixtures/worker.fixture';
 *
 * test('test using shared event', async ({ sharedEvent, df }) => {
 *   // sharedEvent is reused across tests
 *   // df is still per-test for test-specific data
 * });
 */

// Test-scoped fixture (recommended for most tests)
export { test as testScopedTest, expect } from './test.fixture';

// Worker-scoped fixture (for shared data scenarios)
export { test as workerScopedTest } from './worker.fixture';

// Re-export types
export type { TestFixture } from './test.fixture';
