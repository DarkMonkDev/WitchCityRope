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
  port: 5433,
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
  participationType: 'Ticket' | 'RSVP';
  status: 'Registered' | 'Confirmed' | 'Cancelled' | 'NoShow';
  createdAt: Date;
  updatedAt: Date;
}

/**
 * Verify event participation exists in database
 *
 * CRITICAL: This would have caught the ticket cancellation bug
 * where UI showed cancellation but database still showed active ticket.
 *
 * @param userId User ID
 * @param eventId Event ID
 * @param expectedStatus Expected participation status
 * @returns Participation record from database
 */
export async function verifyEventParticipation(
  userId: string,
  eventId: string,
  expectedStatus: 'Registered' | 'Confirmed' | 'Cancelled' | 'NoShow'
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
    if (expectedStatus === 'Cancelled') {
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
    throw new Error(
      `Participation status mismatch: expected "${expectedStatus}" but got "${actual.status}"`
    );
  }

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
 */
export async function cleanupTestUser(email: string): Promise<void> {
  const sql = `DELETE FROM "Users" WHERE "Email" = $1`;
  await query(sql, [email]);
}

// Export all helper functions
export const DatabaseHelpers = {
  verifyProfileFields,
  getUserIdFromEmail,
  verifyEventParticipation,
  verifyNoEventParticipation,
  verifyEventExists,
  getEventsByCreator,
  verifyVettingApplicationStatus,
  verifyAuditLogExists,
  cleanupTestData,
  cleanupTestUser,
  closeDatabaseConnections,
};
