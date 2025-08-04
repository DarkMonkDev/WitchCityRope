/**
 * Data generator utilities for WitchCityRope E2E tests
 * Functions to generate unique test data for users, events, and other entities
 */

import { faker } from '@faker-js/faker';

/**
 * Base interface for generated test data
 */
interface TestDataBase {
  uniqueId: string;
  timestamp: number;
}

/**
 * Generated user data interface
 */
export interface GeneratedUser extends TestDataBase {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  sceneName?: string;
  phoneNumber?: string;
  pronouns?: string;
  fetLifeUsername?: string;
  bio?: string;
}

/**
 * Generated event data interface
 */
export interface GeneratedEvent extends TestDataBase {
  name: string;
  description: string;
  startDate: Date;
  endDate: Date;
  location: string;
  capacity: number;
  price: number;
  eventType: 'Social' | 'Workshop' | 'Private' | 'Special';
  tags: string[];
  requiresVetting: boolean;
}

/**
 * Generated RSVP data interface
 */
export interface GeneratedRSVP extends TestDataBase {
  eventId?: string;
  userId?: string;
  status: 'Attending' | 'Maybe' | 'NotAttending';
  notes?: string;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
}

/**
 * Generated vetting application data interface
 */
export interface GeneratedVettingApplication extends TestDataBase {
  references: Array<{
    name: string;
    email: string;
    relationship: string;
    knownDuration: string;
  }>;
  experience: string;
  interests: string;
  safetyAgreement: boolean;
}

/**
 * Test data generator class
 */
export class TestDataGenerator {
  private static counter = 0;

  /**
   * Generate a unique ID for test data
   */
  static generateUniqueId(prefix: string = 'test'): string {
    const timestamp = Date.now();
    const counter = ++this.counter;
    const random = Math.floor(Math.random() * 1000);
    return `${prefix}-${timestamp}-${counter}-${random}`;
  }

  /**
   * Generate a unique test user
   */
  static generateUser(options?: Partial<GeneratedUser>): GeneratedUser {
    const uniqueId = this.generateUniqueId('user');
    const timestamp = Date.now();

    return {
      uniqueId,
      timestamp,
      email: options?.email || `test.${uniqueId}@witchcityrope.test`,
      password: options?.password || 'TestPass123!@#',
      firstName: options?.firstName || faker.person.firstName(),
      lastName: options?.lastName || faker.person.lastName(),
      sceneName: options?.sceneName || faker.internet.userName(),
      phoneNumber: options?.phoneNumber || faker.phone.number('###-###-####'),
      pronouns: options?.pronouns || faker.helpers.arrayElement(['they/them', 'she/her', 'he/him', 'ze/zir']),
      fetLifeUsername: options?.fetLifeUsername || faker.internet.userName(),
      bio: options?.bio || faker.lorem.paragraph(3),
      ...options
    };
  }

  /**
   * Generate a unique test event
   */
  static generateEvent(options?: Partial<GeneratedEvent>): GeneratedEvent {
    const uniqueId = this.generateUniqueId('event');
    const timestamp = Date.now();
    const startDate = options?.startDate || faker.date.future({ days: 30 });
    const endDate = options?.endDate || new Date(startDate.getTime() + 3 * 60 * 60 * 1000); // 3 hours later

    return {
      uniqueId,
      timestamp,
      name: options?.name || `Test Event ${uniqueId}`,
      description: options?.description || faker.lorem.paragraphs(2),
      startDate,
      endDate,
      location: options?.location || faker.location.streetAddress(),
      capacity: options?.capacity || faker.number.int({ min: 10, max: 50 }),
      price: options?.price || faker.number.float({ min: 0, max: 100, multipleOf: 0.01 }),
      eventType: options?.eventType || faker.helpers.arrayElement(['Social', 'Workshop', 'Private', 'Special']),
      tags: options?.tags || faker.helpers.arrayElements(['rope', 'bondage', 'education', 'social', 'beginner-friendly', 'advanced'], 3),
      requiresVetting: options?.requiresVetting ?? faker.datatype.boolean(),
      ...options
    };
  }

  /**
   * Generate RSVP data
   */
  static generateRSVP(options?: Partial<GeneratedRSVP>): GeneratedRSVP {
    const uniqueId = this.generateUniqueId('rsvp');
    const timestamp = Date.now();

    return {
      uniqueId,
      timestamp,
      status: options?.status || faker.helpers.arrayElement(['Attending', 'Maybe', 'NotAttending']),
      notes: options?.notes || faker.lorem.sentence(),
      dietaryRestrictions: options?.dietaryRestrictions || faker.helpers.arrayElement(['None', 'Vegetarian', 'Vegan', 'Gluten-free', 'Kosher']),
      accessibilityNeeds: options?.accessibilityNeeds || faker.helpers.arrayElement(['None', 'Wheelchair accessible', 'ASL interpreter needed']),
      ...options
    };
  }

  /**
   * Generate vetting application data
   */
  static generateVettingApplication(options?: Partial<GeneratedVettingApplication>): GeneratedVettingApplication {
    const uniqueId = this.generateUniqueId('vetting');
    const timestamp = Date.now();

    const defaultReferences = Array.from({ length: 2 }, () => ({
      name: faker.person.fullName(),
      email: faker.internet.email(),
      relationship: faker.helpers.arrayElement(['Friend', 'Play partner', 'Community member', 'Teacher']),
      knownDuration: faker.helpers.arrayElement(['6 months', '1 year', '2 years', '5+ years'])
    }));

    return {
      uniqueId,
      timestamp,
      references: options?.references || defaultReferences,
      experience: options?.experience || faker.lorem.paragraphs(2),
      interests: options?.interests || faker.lorem.paragraph(),
      safetyAgreement: options?.safetyAgreement ?? true,
      ...options
    };
  }

  /**
   * Generate multiple users at once
   */
  static generateUsers(count: number, options?: Partial<GeneratedUser>): GeneratedUser[] {
    return Array.from({ length: count }, () => this.generateUser(options));
  }

  /**
   * Generate multiple events at once
   */
  static generateEvents(count: number, options?: Partial<GeneratedEvent>): GeneratedEvent[] {
    return Array.from({ length: count }, () => this.generateEvent(options));
  }

  /**
   * Generate a complete test scenario with users and events
   */
  static generateTestScenario() {
    const admin = this.generateUser({
      email: `admin.${this.generateUniqueId()}@witchcityrope.test`,
      firstName: 'Admin',
      lastName: 'User'
    });

    const teacher = this.generateUser({
      email: `teacher.${this.generateUniqueId()}@witchcityrope.test`,
      firstName: 'Teacher',
      lastName: 'User'
    });

    const vettedMember = this.generateUser({
      email: `vetted.${this.generateUniqueId()}@witchcityrope.test`,
      firstName: 'Vetted',
      lastName: 'Member'
    });

    const newMember = this.generateUser({
      email: `new.${this.generateUniqueId()}@witchcityrope.test`,
      firstName: 'New',
      lastName: 'Member'
    });

    const socialEvent = this.generateEvent({
      eventType: 'Social',
      requiresVetting: false,
      name: `Social Meetup ${this.generateUniqueId()}`
    });

    const workshop = this.generateEvent({
      eventType: 'Workshop',
      requiresVetting: true,
      name: `Advanced Workshop ${this.generateUniqueId()}`,
      price: 50
    });

    const privateEvent = this.generateEvent({
      eventType: 'Private',
      requiresVetting: true,
      name: `Private Party ${this.generateUniqueId()}`,
      capacity: 20
    });

    return {
      users: {
        admin,
        teacher,
        vettedMember,
        newMember
      },
      events: {
        socialEvent,
        workshop,
        privateEvent
      }
    };
  }

  /**
   * Clean up generated test data identifiers
   * Returns an array of unique IDs that were generated for cleanup purposes
   */
  static getGeneratedIds(): string[] {
    // In a real implementation, this would track all generated IDs
    // For now, return empty array as placeholder
    return [];
  }
}

/**
 * Utility functions for common test data patterns
 */
export const TestDataPatterns = {
  /**
   * Generate a valid scene name
   */
  validSceneName: () => faker.internet.userName().replace(/[^a-zA-Z0-9_-]/g, ''),

  /**
   * Generate an invalid email
   */
  invalidEmail: () => faker.helpers.arrayElement(['notanemail', 'missing@', '@nodomain', 'spaces in@email.com']),

  /**
   * Generate a weak password
   */
  weakPassword: () => faker.helpers.arrayElement(['weak', '12345', 'password', 'abc123']),

  /**
   * Generate a strong password
   */
  strongPassword: () => {
    const upper = faker.string.alpha({ length: 2, casing: 'upper' });
    const lower = faker.string.alpha({ length: 4, casing: 'lower' });
    const numbers = faker.string.numeric({ length: 2 });
    const special = faker.helpers.arrayElement(['!', '@', '#', '$', '%', '^', '&', '*']);
    return `${upper}${lower}${numbers}${special}`;
  },

  /**
   * Generate a past date
   */
  pastDate: (daysAgo: number = 7) => {
    const date = new Date();
    date.setDate(date.getDate() - daysAgo);
    return date;
  },

  /**
   * Generate a future date
   */
  futureDate: (daysAhead: number = 7) => {
    const date = new Date();
    date.setDate(date.getDate() + daysAhead);
    return date;
  }
};