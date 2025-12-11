/**
 * User Factory
 *
 * Create and delete test users via API.
 * Uses existing /api/test-helpers/users endpoint.
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type { CreateUserRequest, UserResponse, CleanupItem } from '../types';

export class UserFactory {
  private client: DataFactoryApiClient;
  private createdUsers: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test user
   *
   * @example
   * const user = await userFactory.create({
   *   email: 'test@example.com',
   *   roles: ['VettedMember']
   * });
   */
  async create(options: CreateUserRequest): Promise<UserResponse> {
    const request = {
      email: options.email,
      password: options.password ?? 'Test123!',
      firstName: options.firstName ?? 'Test',
      lastName: options.lastName ?? 'User',
      roles: options.roles ?? [],
    };

    const response = await this.client.post<typeof request, UserResponse>(
      '/users',
      request
    );

    this.createdUsers.push(response.id);
    return response;
  }

  /**
   * Create a user with verified email
   *
   * @example
   * const user = await userFactory.createVerified({
   *   email: 'verified@example.com',
   *   roles: ['VettedMember']
   * });
   */
  async createVerified(options: CreateUserRequest): Promise<UserResponse> {
    const user = await this.create(options);
    await this.verifyEmail(options.email);
    return user;
  }

  /**
   * Create a user with specific role
   *
   * @example
   * const admin = await userFactory.createWithRole('admin@test.com', 'Admin');
   */
  async createWithRole(email: string, role: string): Promise<UserResponse> {
    return this.createVerified({
      email,
      roles: [role],
    });
  }

  /**
   * Verify a user's email address
   */
  async verifyEmail(email: string): Promise<void> {
    await this.client.post('/verify-email', { email });
  }

  /**
   * Delete a test user
   */
  async delete(userId: string): Promise<void> {
    await this.client.delete('/users', userId);
    this.createdUsers = this.createdUsers.filter((id) => id !== userId);
  }

  /**
   * Get cleanup items for all created users
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdUsers.map((id) => ({ type: 'user' as const, id }));
  }

  /**
   * Cleanup all created users
   */
  async cleanupAll(): Promise<void> {
    for (const userId of [...this.createdUsers]) {
      try {
        await this.delete(userId);
      } catch (error) {
        console.warn(`Failed to cleanup user ${userId}:`, error);
      }
    }
  }

  /**
   * Get count of tracked users
   */
  get count(): number {
    return this.createdUsers.length;
  }
}

/**
 * Convenience function for quick user creation
 *
 * @example
 * const user = await createTestUser(request, { email: 'test@example.com' });
 */
export async function createTestUser(
  request: APIRequestContext,
  options: CreateUserRequest
): Promise<UserResponse> {
  const factory = new UserFactory(request);
  return factory.create(options);
}

/**
 * Convenience function for verified user creation
 *
 * @example
 * const user = await createVerifiedUser(request, { email: 'test@example.com' });
 */
export async function createVerifiedUser(
  request: APIRequestContext,
  options: CreateUserRequest
): Promise<UserResponse> {
  const factory = new UserFactory(request);
  return factory.createVerified(options);
}
