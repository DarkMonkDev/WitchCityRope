/**
 * Database helper utilities for WitchCityRope E2E tests
 * PostgreSQL test data setup/cleanup utilities
 */

import { Client } from 'pg';
import { GeneratedUser, GeneratedEvent, TestDataGenerator } from './data-generators';

/**
 * Database configuration interface
 */
export interface DatabaseConfig {
  host: string;
  port: number;
  database: string;
  user: string;
  password: string;
  ssl?: boolean;
}

/**
 * Test database configuration
 */
const defaultTestDbConfig: DatabaseConfig = {
  host: process.env.TEST_DB_HOST || 'localhost',
  port: parseInt(process.env.TEST_DB_PORT || '5432'),
  database: process.env.TEST_DB_NAME || 'witchcityrope_test',
  user: process.env.TEST_DB_USER || 'postgres',
  password: process.env.TEST_DB_PASSWORD || 'postgres',
  ssl: process.env.TEST_DB_SSL === 'true'
};

/**
 * Database helper class for test data management
 */
export class DatabaseHelpers {
  private static client: Client | null = null;
  private static isConnected: boolean = false;
  private static createdUserIds: string[] = [];
  private static createdEventIds: string[] = [];
  private static createdRecordIds: Map<string, string[]> = new Map();

  /**
   * Initialize database connection
   */
  static async connect(config: DatabaseConfig = defaultTestDbConfig): Promise<void> {
    if (this.isConnected && this.client) {
      return;
    }

    this.client = new Client(config);
    
    try {
      await this.client.connect();
      this.isConnected = true;
      console.log('Connected to test database');
    } catch (error) {
      console.error('Failed to connect to test database:', error);
      throw error;
    }
  }

  /**
   * Disconnect from database
   */
  static async disconnect(): Promise<void> {
    if (this.client && this.isConnected) {
      await this.client.end();
      this.client = null;
      this.isConnected = false;
      console.log('Disconnected from test database');
    }
  }

  /**
   * Ensure database connection is active
   */
  private static async ensureConnection(): Promise<void> {
    if (!this.isConnected || !this.client) {
      await this.connect();
    }
  }

  /**
   * Create a test user in the database
   */
  static async createTestUser(userData: Partial<GeneratedUser> = {}): Promise<string> {
    await this.ensureConnection();
    
    const user = TestDataGenerator.generateUser(userData);
    
    try {
      // First create ASP.NET Identity user
      const identityResult = await this.client!.query(
        `INSERT INTO "AspNetUsers" 
         ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", 
          "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", 
          "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", 
          "AccessFailedCount")
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14)
         RETURNING "Id"`,
        [
          user.uniqueId,
          user.email,
          user.email.toUpperCase(),
          user.email,
          user.email.toUpperCase(),
          true, // EmailConfirmed
          // This is a hash for "TestPass123!@#" - in real tests you'd use proper hashing
          'AQAAAAIAAYagAAAAEKjqWuJGFg6aS2kfXw1sP2+VfWGzPgKCkOVqCrFwL6yiRmvnKhDFqMfkr7FepJ8Sgg==',
          this.generateSecurityStamp(),
          this.generateGuid(),
          user.phoneNumber || null,
          false, // PhoneNumberConfirmed
          false, // TwoFactorEnabled
          true,  // LockoutEnabled
          0      // AccessFailedCount
        ]
      );

      const userId = identityResult.rows[0].Id;

      // Then create application user
      await this.client!.query(
        `INSERT INTO "Users" 
         ("Id", "Email", "FirstName", "LastName", "SceneName", "Bio", 
          "Pronouns", "FetLifeUsername", "IsActive", "IsVetted", "VettedDate",
          "CreatedAt", "UpdatedAt")
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)`,
        [
          userId,
          user.email,
          user.firstName,
          user.lastName,
          user.sceneName || null,
          user.bio || null,
          user.pronouns || null,
          user.fetLifeUsername || null,
          true,  // IsActive
          false, // IsVetted (can be overridden)
          null,  // VettedDate
          new Date(),
          new Date()
        ]
      );

      this.createdUserIds.push(userId);
      return userId;
    } catch (error) {
      console.error('Failed to create test user:', error);
      throw error;
    }
  }

  /**
   * Create a test event in the database
   */
  static async createTestEvent(eventData: Partial<GeneratedEvent> = {}, createdByUserId?: string): Promise<string> {
    await this.ensureConnection();
    
    const event = TestDataGenerator.generateEvent(eventData);
    const eventId = TestDataGenerator.generateUniqueId('event');
    
    // If no creator specified, create a default admin user
    if (!createdByUserId) {
      createdByUserId = await this.createTestUser({
        email: `admin-${event.uniqueId}@test.com`,
        firstName: 'Event',
        lastName: 'Creator'
      });
    }

    try {
      const result = await this.client!.query(
        `INSERT INTO "Events" 
         ("Id", "Name", "Description", "StartDate", "EndDate", "Location", 
          "Capacity", "Price", "EventType", "IsPublished", "RequiresVetting",
          "CreatedById", "CreatedAt", "UpdatedAt")
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14)
         RETURNING "Id"`,
        [
          eventId,
          event.name,
          event.description,
          event.startDate,
          event.endDate,
          event.location,
          event.capacity,
          event.price,
          event.eventType,
          true, // IsPublished
          event.requiresVetting,
          createdByUserId,
          new Date(),
          new Date()
        ]
      );

      this.createdEventIds.push(eventId);
      return eventId;
    } catch (error) {
      console.error('Failed to create test event:', error);
      throw error;
    }
  }

  /**
   * Assign a role to a user
   */
  static async assignUserRole(userId: string, roleName: string): Promise<void> {
    await this.ensureConnection();

    try {
      // First ensure the role exists
      const roleResult = await this.client!.query(
        `SELECT "Id" FROM "AspNetRoles" WHERE "Name" = $1`,
        [roleName]
      );

      let roleId: string;
      if (roleResult.rows.length === 0) {
        // Create the role if it doesn't exist
        const createRoleResult = await this.client!.query(
          `INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
           VALUES ($1, $2, $3, $4)
           RETURNING "Id"`,
          [
            TestDataGenerator.generateUniqueId('role'),
            roleName,
            roleName.toUpperCase(),
            this.generateGuid()
          ]
        );
        roleId = createRoleResult.rows[0].Id;
      } else {
        roleId = roleResult.rows[0].Id;
      }

      // Assign the role to the user
      await this.client!.query(
        `INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
         VALUES ($1, $2)
         ON CONFLICT DO NOTHING`,
        [userId, roleId]
      );
    } catch (error) {
      console.error('Failed to assign user role:', error);
      throw error;
    }
  }

  /**
   * Mark a user as vetted
   */
  static async markUserAsVetted(userId: string): Promise<void> {
    await this.ensureConnection();

    try {
      await this.client!.query(
        `UPDATE "Users" 
         SET "IsVetted" = true, "VettedDate" = $1, "UpdatedAt" = $2
         WHERE "Id" = $3`,
        [new Date(), new Date(), userId]
      );
    } catch (error) {
      console.error('Failed to mark user as vetted:', error);
      throw error;
    }
  }

  /**
   * Create an RSVP for a user to an event
   */
  static async createRSVP(userId: string, eventId: string, status: string = 'Attending'): Promise<string> {
    await this.ensureConnection();

    const rsvpId = TestDataGenerator.generateUniqueId('rsvp');

    try {
      await this.client!.query(
        `INSERT INTO "Registrations" 
         ("Id", "UserId", "EventId", "Status", "RegisteredAt", "CreatedAt", "UpdatedAt")
         VALUES ($1, $2, $3, $4, $5, $6, $7)`,
        [
          rsvpId,
          userId,
          eventId,
          status,
          new Date(),
          new Date(),
          new Date()
        ]
      );

      this.trackCreatedRecord('Registrations', rsvpId);
      return rsvpId;
    } catch (error) {
      console.error('Failed to create RSVP:', error);
      throw error;
    }
  }

  /**
   * Create a complete test scenario with multiple users and events
   */
  static async createTestScenario() {
    await this.ensureConnection();

    const scenario = TestDataGenerator.generateTestScenario();
    const createdData = {
      userIds: {} as Record<string, string>,
      eventIds: {} as Record<string, string>
    };

    try {
      // Create users
      for (const [key, userData] of Object.entries(scenario.users)) {
        const userId = await this.createTestUser(userData);
        createdData.userIds[key] = userId;

        // Assign appropriate roles
        if (key === 'admin') {
          await this.assignUserRole(userId, 'Admin');
        } else if (key === 'teacher') {
          await this.assignUserRole(userId, 'Teacher');
          await this.markUserAsVetted(userId);
        } else if (key === 'vettedMember') {
          await this.assignUserRole(userId, 'Member');
          await this.markUserAsVetted(userId);
        } else {
          await this.assignUserRole(userId, 'Member');
        }
      }

      // Create events
      for (const [key, eventData] of Object.entries(scenario.events)) {
        const eventId = await this.createTestEvent(
          eventData,
          key === 'workshop' ? createdData.userIds.teacher : createdData.userIds.admin
        );
        createdData.eventIds[key] = eventId;
      }

      // Create some RSVPs
      await this.createRSVP(createdData.userIds.vettedMember, createdData.eventIds.socialEvent, 'Attending');
      await this.createRSVP(createdData.userIds.teacher, createdData.eventIds.workshop, 'Attending');

      return createdData;
    } catch (error) {
      console.error('Failed to create test scenario:', error);
      throw error;
    }
  }

  /**
   * Clean up a specific user and related data
   */
  static async cleanupUser(userId: string): Promise<void> {
    await this.ensureConnection();

    try {
      // Delete in reverse order of foreign key dependencies
      const tables = [
        'Registrations',
        'Tickets',
        'Payments',
        'VettingApplications',
        'VettingApplicationReferences',
        'UserClaims',
        'AspNetUserRoles',
        'AspNetUserClaims',
        'AspNetUserLogins',
        'AspNetUserTokens',
        'Users',
        'AspNetUsers'
      ];

      for (const table of tables) {
        const userColumn = table === 'AspNetUsers' ? 'Id' : 'UserId';
        await this.client!.query(
          `DELETE FROM "${table}" WHERE "${userColumn}" = $1`,
          [userId]
        );
      }
    } catch (error) {
      console.error(`Failed to cleanup user ${userId}:`, error);
    }
  }

  /**
   * Clean up a specific event and related data
   */
  static async cleanupEvent(eventId: string): Promise<void> {
    await this.ensureConnection();

    try {
      // Delete in reverse order of foreign key dependencies
      const tables = [
        'Tickets',
        'Registrations',
        'EventTags',
        'Events'
      ];

      for (const table of tables) {
        const eventColumn = table === 'Events' ? 'Id' : 'EventId';
        await this.client!.query(
          `DELETE FROM "${table}" WHERE "${eventColumn}" = $1`,
          [eventId]
        );
      }
    } catch (error) {
      console.error(`Failed to cleanup event ${eventId}:`, error);
    }
  }

  /**
   * Clean up all test data created during the test session
   */
  static async cleanupAllTestData(): Promise<void> {
    await this.ensureConnection();

    console.log('Cleaning up test data...');

    // Clean up tracked records
    for (const [table, ids] of this.createdRecordIds.entries()) {
      for (const id of ids) {
        try {
          await this.client!.query(`DELETE FROM "${table}" WHERE "Id" = $1`, [id]);
        } catch (error) {
          console.error(`Failed to delete from ${table}:`, error);
        }
      }
    }

    // Clean up events
    for (const eventId of this.createdEventIds) {
      await this.cleanupEvent(eventId);
    }

    // Clean up users
    for (const userId of this.createdUserIds) {
      await this.cleanupUser(userId);
    }

    // Clear tracking arrays
    this.createdUserIds = [];
    this.createdEventIds = [];
    this.createdRecordIds.clear();

    console.log('Test data cleanup completed');
  }

  /**
   * Execute a raw SQL query (use with caution)
   */
  static async executeQuery<T = any>(query: string, params?: any[]): Promise<T[]> {
    await this.ensureConnection();

    try {
      const result = await this.client!.query(query, params);
      return result.rows;
    } catch (error) {
      console.error('Query execution failed:', error);
      throw error;
    }
  }

  /**
   * Check if a record exists in a table
   */
  static async recordExists(table: string, column: string, value: any): Promise<boolean> {
    await this.ensureConnection();

    const result = await this.client!.query(
      `SELECT 1 FROM "${table}" WHERE "${column}" = $1 LIMIT 1`,
      [value]
    );

    return result.rows.length > 0;
  }

  /**
   * Get record by ID
   */
  static async getRecordById<T = any>(table: string, id: string): Promise<T | null> {
    await this.ensureConnection();

    const result = await this.client!.query(
      `SELECT * FROM "${table}" WHERE "Id" = $1`,
      [id]
    );

    return result.rows[0] || null;
  }

  /**
   * Track a created record for cleanup
   */
  private static trackCreatedRecord(table: string, id: string): void {
    if (!this.createdRecordIds.has(table)) {
      this.createdRecordIds.set(table, []);
    }
    this.createdRecordIds.get(table)!.push(id);
  }

  /**
   * Generate a GUID for database operations
   */
  private static generateGuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  /**
   * Generate a security stamp for ASP.NET Identity
   */
  private static generateSecurityStamp(): string {
    return this.generateGuid().replace(/-/g, '').toUpperCase();
  }

  /**
   * Reset database to clean state (use with extreme caution)
   */
  static async resetDatabase(): Promise<void> {
    await this.ensureConnection();

    console.warn('WARNING: Resetting database to clean state');

    try {
      // This should be customized based on your specific needs
      // Only delete test data, not system data
      await this.client!.query(`
        DELETE FROM "Tickets" WHERE "Id" LIKE 'test-%';
        DELETE FROM "Registrations" WHERE "Id" LIKE 'test-%';
        DELETE FROM "EventTags" WHERE "EventId" IN (SELECT "Id" FROM "Events" WHERE "Id" LIKE 'test-%');
        DELETE FROM "Events" WHERE "Id" LIKE 'test-%';
        DELETE FROM "VettingApplicationReferences" WHERE "VettingApplicationId" IN (SELECT "Id" FROM "VettingApplications" WHERE "UserId" LIKE 'test-%');
        DELETE FROM "VettingApplications" WHERE "UserId" LIKE 'test-%';
        DELETE FROM "AspNetUserRoles" WHERE "UserId" LIKE 'test-%';
        DELETE FROM "AspNetUserClaims" WHERE "UserId" LIKE 'test-%';
        DELETE FROM "AspNetUserLogins" WHERE "UserId" LIKE 'test-%';
        DELETE FROM "AspNetUserTokens" WHERE "UserId" LIKE 'test-%';
        DELETE FROM "Users" WHERE "Id" LIKE 'test-%';
        DELETE FROM "AspNetUsers" WHERE "Id" LIKE 'test-%';
      `);

      console.log('Database reset completed');
    } catch (error) {
      console.error('Failed to reset database:', error);
      throw error;
    }
  }
}