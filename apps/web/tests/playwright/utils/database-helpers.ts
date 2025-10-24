/**
 * Database Helpers for E2E Persistence Testing
 *
 * Provides direct database access to verify that UI actions
 * actually persist data correctly. This catches the category
 * of bugs where UI shows success but database wasn't updated.
 *
 * CRITICAL USE CASE: Both profile update and ticket cancellation
 * bugs would have been caught by these database verification helpers.
 */

import pkg from 'pg';
const { Pool } = pkg;

// Database connection configuration (matches Docker setup)
const DB_CONFIG = {
  host: 'localhost',
  port: 5434,  // Matches Docker PostgreSQL port from docker-compose.dev.yml
  database: 'witchcityrope_dev',
  user: 'postgres',
  password: 'devpass123',
};

// Singleton pool instance for connection reuse
let pool: typeof Pool.prototype | null = null;

/**
 * Get or create database connection pool
 * Uses connection pooling for performance
 */
function getPool() {
  if (!pool) {
    pool = new Pool(DB_CONFIG);
  }
  return pool;
}

/**
 * Execute a database query with type-safe results
 */
async function query<T = any>(sql: string, params: any[] = []): Promise<T[]> {
  const client = await getPool().connect();
  try {
    const result = await client.query(sql, params);
    return result.rows as T[];
  } finally {
    client.release();
  }
}

/**
 * Close all database connections
 * Call this in test teardown
 */
export async function closeDatabaseConnections() {
  if (pool) {
    await pool.end();
    pool = null;
  }
}

// ============================================================================
// PROFILE VERIFICATION HELPERS
// ============================================================================

export interface ProfileFields {
  firstName?: string | null;
  lastName?: string | null;
  bio?: string | null;
  pronouns?: string | null;
  discordName?: string | null;
  fetLifeName?: string | null;
}

/**
 * Verify profile fields in database match expected values
 *
 * CRITICAL: This would have caught the profile update bug where
 * UI showed success but database wasn't updated.
 *
 * @param userId User ID to verify
 * @param expectedFields Expected field values
 * @returns Profile fields from database
 */
export async function verifyProfileFields(
  userId: string,
  expectedFields: ProfileFields
): Promise<ProfileFields & { sceneName: string; email: string }> {
  const sql = `
    SELECT
      "SceneName" as "sceneName",
      "Email" as "email",
      "FirstName" as "firstName",
      "LastName" as "lastName",
      "Bio" as "bio",
      "Pronouns" as "pronouns",
      "DiscordName" as "discordName",
      "FetLifeName" as "fetLifeName"
    FROM "Users"
    WHERE "Id" = $1
  `;

  const rows = await query(sql, [userId]);

  if (rows.length === 0) {
    throw new Error(`User not found in database: ${userId}`);
  }

  const actual = rows[0];

  // Verify each expected field matches actual
  const mismatches: string[] = [];

  for (const [key, expectedValue] of Object.entries(expectedFields)) {
    const actualValue = actual[key];
    if (actualValue !== expectedValue) {
      mismatches.push(
        `${key}: expected "${expectedValue}" but got "${actualValue}"`
      );
    }
  }

  if (mismatches.length > 0) {
    throw new Error(
      `Profile fields mismatch in database:\n${mismatches.join('\n')}`
    );
  }

  return actual;
}

/**
 * Get user ID from email
 * Useful for getting user ID after login
 */
export async function getUserIdFromEmail(email: string): Promise<string> {
  const sql = `SELECT "Id" FROM "Users" WHERE "Email" = $1`;
  const rows = await query<{ Id: string }>(sql, [email]);

  if (rows.length === 0) {
    throw new Error(`User not found with email: ${email}`);
  }

  return rows[0].Id;
}

// ============================================================================
// EVENT PARTICIPATION VERIFICATION HELPERS
// ============================================================================

export interface EventParticipationRecord {
  id: string;
  userId: string;
  eventId: string;
  participationType: 'Ticket' | 'RSVP' | number; // Can be string OR number from database
  status: 1 | 2 | 3 | 4; // ParticipationStatus enum: 1=Active, 2=Cancelled, 3=Refunded, 4=Waitlisted
  createdAt: Date;
  updatedAt: Date;
}

// Helper to convert numeric status to readable name
export function getParticipationStatusName(status: number): string {
  const statusMap: Record<number, string> = {
    1: 'Active',
    2: 'Cancelled',
    3: 'Refunded',
    4: 'Waitlisted'
  };
  return statusMap[status] || `Unknown(${status})`;
}

// Helper to convert numeric participation type to readable name
export function getParticipationTypeName(type: number | string): string {
  if (typeof type === 'string') {
    return type; // Already a string like 'RSVP' or 'Ticket'
  }

  // PostgreSQL stores ParticipationType enum as integers
  const typeMap: Record<number, string> = {
    0: 'Ticket',
    1: 'RSVP'
  };
  return typeMap[type] || `Unknown(${type})`;
}

/**
 * Verify event participation exists in database
 *
 * CRITICAL: This would have caught the ticket cancellation bug
 * where UI showed cancellation but database still showed active ticket.
 *
 * @param userId User ID
 * @param eventId Event ID
 * @param expectedStatus Expected participation status (1=Active, 2=Cancelled, 3=Refunded, 4=Waitlisted)
 * @returns Participation record from database
 */
export async function verifyEventParticipation(
  userId: string,
  eventId: string,
  expectedStatus: 1 | 2 | 3 | 4
): Promise<EventParticipationRecord | null> {
  const sql = `
    SELECT
      "Id" as "id",
      "UserId" as "userId",
      "EventId" as "eventId",
      "ParticipationType" as "participationType",
      "Status" as "status",
      "CreatedAt" as "createdAt",
      "UpdatedAt" as "updatedAt"
    FROM "EventParticipations"
    WHERE "UserId" = $1 AND "EventId" = $2
    ORDER BY "UpdatedAt" DESC
    LIMIT 1
  `;

  const rows = await query<EventParticipationRecord>(sql, [userId, eventId]);

  if (rows.length === 0) {
    if (expectedStatus === 2) { // Cancelled
      // If we expect cancelled, check if record was deleted
      // (some systems delete instead of soft-delete)
      return null;
    }
    throw new Error(
      `No participation record found for user ${userId} and event ${eventId}`
    );
  }

  const actual = rows[0];

  if (actual.status !== expectedStatus) {
    const expectedName = getParticipationStatusName(expectedStatus);
    const actualName = getParticipationStatusName(actual.status);
    throw new Error(
      `Participation status mismatch: expected ${expectedStatus} (${expectedName}) but got ${actual.status} (${actualName})`
    );
  }

  console.log(`✅ Database verification: Status is ${actual.status} (${getParticipationStatusName(actual.status)})`);
  console.log(`   Participation Type: ${getParticipationTypeName(actual.participationType)}`);

  return actual;
}

/**
 * Verify event participation does NOT exist
 * Useful for cancelled/deleted participations
 */
export async function verifyNoEventParticipation(
  userId: string,
  eventId: string
): Promise<void> {
  const sql = `
    SELECT COUNT(*) as count
    FROM "EventParticipations"
    WHERE "UserId" = $1 AND "EventId" = $2 AND "Status" != 'Cancelled'
  `;

  const rows = await query<{ count: string }>(sql, [userId, eventId]);
  const count = parseInt(rows[0].count, 10);

  if (count > 0) {
    throw new Error(
      `Expected no active participation but found ${count} record(s)`
    );
  }
}

// ============================================================================
// EVENT VERIFICATION HELPERS
// ============================================================================

export interface EventRecord {
  id: string;
  title: string;
  description: string;
  startDate: Date;
  endDate: Date;
  eventType: string;
  isPublished: boolean;
  capacity: number;
}

/**
 * Verify event exists in database
 *
 * @param eventId Event ID to verify
 * @returns Event record from database
 */
export async function verifyEventExists(eventId: string): Promise<EventRecord> {
  const sql = `
    SELECT
      "Id" as "id",
      "Title" as "title",
      "Description" as "description",
      "StartDate" as "startDate",
      "EndDate" as "endDate",
      "EventType" as "eventType",
      "IsPublished" as "isPublished",
      "Capacity" as "capacity"
    FROM "Events"
    WHERE "Id" = $1
  `;

  const rows = await query<EventRecord>(sql, [eventId]);

  if (rows.length === 0) {
    throw new Error(`Event not found in database: ${eventId}`);
  }

  return rows[0];
}

/**
 * Get all events created by a specific user
 * Useful for admin event creation tests
 */
export async function getEventsByCreator(userId: string): Promise<EventRecord[]> {
  const sql = `
    SELECT
      e."Id" as "id",
      e."Title" as "title",
      e."Description" as "description",
      e."StartDate" as "startDate",
      e."EndDate" as "endDate",
      e."EventType" as "eventType",
      e."IsPublished" as "isPublished",
      e."Capacity" as "capacity"
    FROM "Events" e
    INNER JOIN "EventOrganizers" eo ON e."Id" = eo."EventId"
    WHERE eo."UserId" = $1
    ORDER BY e."CreatedAt" DESC
  `;

  return await query<EventRecord>(sql, [userId]);
}

/**
 * Get events suitable for testing (published, in future)
 *
 * CRITICAL: Use this to find test events instead of scraping UI.
 * Provides reliable event IDs for persistence tests.
 */
export async function getTestableEvents(): Promise<EventRecord[]> {
  const sql = `
    SELECT
      "Id" as "id",
      "Title" as "title",
      "Description" as "description",
      "EventType" as "eventType",
      "StartDate" as "startDate",
      "EndDate" as "endDate",
      "IsPublished" as "isPublished",
      "Capacity" as "capacity"
    FROM "Events"
    WHERE "IsPublished" = true
      AND "StartDate" > NOW()
    ORDER BY "StartDate" ASC
    LIMIT 10
  `;

  return await query<EventRecord>(sql);
}

/**
 * Get first RSVP-type event for testing
 *
 * Use for RSVP lifecycle tests that need a free event.
 */
export async function getFirstRsvpEvent(): Promise<EventRecord | null> {
  const sql = `
    SELECT DISTINCT
      e."Id" as "id",
      e."Title" as "title",
      e."Description" as "description",
      e."EventType" as "eventType",
      e."StartDate" as "startDate",
      e."EndDate" as "endDate",
      e."IsPublished" as "isPublished",
      e."Capacity" as "capacity"
    FROM "Events" e
    INNER JOIN "EventTicketTypes" ett ON e."Id" = ett."EventId"
    WHERE e."IsPublished" = true
      AND e."StartDate" > NOW()
      AND ett."Type" = 'rsvp'
    ORDER BY e."StartDate" ASC
    LIMIT 1
  `;

  const rows = await query<EventRecord>(sql);
  return rows.length > 0 ? rows[0] : null;
}

/**
 * Get first paid ticket event for testing
 *
 * Use for ticket purchase/cancellation tests that need a paid event.
 */
export async function getFirstTicketEvent(): Promise<EventRecord | null> {
  const sql = `
    SELECT DISTINCT
      e."Id" as "id",
      e."Title" as "title",
      e."Description" as "description",
      e."EventType" as "eventType",
      e."StartDate" as "startDate",
      e."EndDate" as "endDate",
      e."IsPublished" as "isPublished",
      e."Capacity" as "capacity"
    FROM "Events" e
    INNER JOIN "EventTicketTypes" ett ON e."Id" = ett."EventId"
    WHERE e."IsPublished" = true
      AND e."StartDate" > NOW()
      AND ett."Type" = 'paid'
    ORDER BY e."StartDate" ASC
    LIMIT 1
  `;

  const rows = await query<EventRecord>(sql);
  return rows.length > 0 ? rows[0] : null;
}

// ============================================================================
// VETTING APPLICATION VERIFICATION HELPERS
// ============================================================================

export interface VettingApplicationRecord {
  id: string;
  userId: string;
  status: string;
  adminNotes?: string | null;
  applicantNotes?: string | null;
  createdAt: Date;
  updatedAt: Date;
}

/**
 * Verify vetting application status in database
 *
 * @param userId User ID
 * @param expectedStatus Expected vetting status
 * @returns Vetting application record
 */
export async function verifyVettingApplicationStatus(
  userId: string,
  expectedStatus: string
): Promise<VettingApplicationRecord> {
  const sql = `
    SELECT
      "Id" as "id",
      "UserId" as "userId",
      "Status" as "status",
      "AdminNotes" as "adminNotes",
      "ApplicantNotes" as "applicantNotes",
      "CreatedAt" as "createdAt",
      "UpdatedAt" as "updatedAt"
    FROM "VettingApplications"
    WHERE "UserId" = $1
    ORDER BY "UpdatedAt" DESC
    LIMIT 1
  `;

  const rows = await query<VettingApplicationRecord>(sql, [userId]);

  if (rows.length === 0) {
    throw new Error(`No vetting application found for user: ${userId}`);
  }

  const actual = rows[0];

  if (actual.status !== expectedStatus) {
    throw new Error(
      `Vetting status mismatch: expected "${expectedStatus}" but got "${actual.status}"`
    );
  }

  return actual;
}

// ============================================================================
// AUDIT LOG VERIFICATION HELPERS
// ============================================================================

/**
 * Verify audit log entry exists for a specific action
 * Useful for ensuring critical actions are logged
 */
export async function verifyAuditLogExists(
  tableName: 'ParticipationHistory' | 'VettingAuditLog' | 'PaymentAuditLog',
  entityId: string,
  action: string
): Promise<boolean> {
  const columnMap = {
    ParticipationHistory: 'ParticipationId',
    VettingAuditLog: 'ApplicationId',
    PaymentAuditLog: 'PaymentId'
  };

  const sql = `
    SELECT COUNT(*) as count
    FROM "${tableName}"
    WHERE "${columnMap[tableName]}" = $1
    AND "Action" ILIKE $2
  `;

  const rows = await query<{ count: string }>(sql, [entityId, `%${action}%`]);
  const count = parseInt(rows[0].count, 10);

  return count > 0;
}

// ============================================================================
// TEST USER CREATION HELPERS (For Race Condition Prevention)
// ============================================================================

export interface TestUserOptions {
  email: string;
  password: string;
  sceneName: string;
  firstName?: string;
  lastName?: string;
  membershipLevel?: 'Guest' | 'Member' | 'VettedMember' | 'Teacher' | 'Admin';
}

export interface TestUser {
  id: string;
  email: string;
  password: string;
  sceneName: string;
}

/**
 * Create a unique test user for profile tests via registration API
 * CRITICAL: Prevents race conditions when multiple tests use same account
 *
 * @param options Test user configuration
 * @returns Test user object with id, email, password
 */
export async function createTestUser(options: TestUserOptions): Promise<TestUser> {
  const {
    email,
    password,
    sceneName,
    firstName = 'Test',
    lastName = 'User',
    membershipLevel = 'Member'
  } = options;

  try {
    // Use TestHelpers API to create user (Development/Test environment only)
    const response = await fetch('http://localhost:5655/api/test-helpers/users', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email,
        password,
        sceneName,
        firstName: firstName || 'Test',
        lastName: lastName || 'User',
        dateOfBirth: '1990-01-01',
        role: membershipLevel,
        isVetted: membershipLevel === 'VettedMember' || false
      })
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to create test user: ${response.status} ${errorText}`);
    }

    const result = await response.json();
    // FIX: API returns user ID in result.data.id, not result.userId
    const userId = result.data?.id;

    if (!userId) {
      throw new Error(`No user ID returned from TestHelpers API: ${JSON.stringify(result)}`);
    }

    console.log(`✅ Created test user via TestHelpers API: ${email} (ID: ${userId})`);

    return {
      id: userId,
      email,
      password,
      sceneName
    };
  } catch (error) {
    console.error(`❌ Failed to create test user:`, error);
    throw error;
  }
}

/**
 * Generate unique test user email with timestamp
 * Ensures no collisions even in parallel test runs
 */
export function generateUniqueTestEmail(prefix: string = 'test-user'): string {
  const timestamp = Date.now();
  const random = Math.floor(Math.random() * 1000);
  return `${prefix}-${timestamp}-${random}@test.witchcityrope.com`;
}

// ============================================================================
// CLEANUP HELPERS FOR TESTS
// ============================================================================

/**
 * Delete test data created during test execution
 * Use in afterEach/afterAll hooks
 */
export async function cleanupTestData(tableName: string, ids: string[]): Promise<void> {
  if (ids.length === 0) return;

  const placeholders = ids.map((_, i) => `$${i + 1}`).join(', ');
  const sql = `DELETE FROM "${tableName}" WHERE "Id" IN (${placeholders})`;

  await query(sql, ids);
}

/**
 * Cleanup test user profile data
 * Accepts either email or user ID for flexible cleanup
 */
export async function cleanupTestUser(emailOrId: string): Promise<void> {
  // Check if it's a UUID (user ID) or email
  const isUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(emailOrId);

  const sql = isUuid
    ? `DELETE FROM "Users" WHERE "Id" = $1`
    : `DELETE FROM "Users" WHERE "Email" = $1`;

  await query(sql, [emailOrId]);
  console.log(`🗑️ Cleaned up test user: ${emailOrId}`);
}

// Export all helper functions
export const DatabaseHelpers = {
  verifyProfileFields,
  getUserIdFromEmail,
  verifyEventParticipation,
  verifyNoEventParticipation,
  verifyEventExists,
  getEventsByCreator,
  getTestableEvents,
  getFirstRsvpEvent,
  getFirstTicketEvent,
  verifyVettingApplicationStatus,
  verifyAuditLogExists,
  createTestUser,
  generateUniqueTestEmail,
  cleanupTestData,
  cleanupTestUser,
  closeDatabaseConnections,
  getParticipationTypeName, // Export new helper
};
