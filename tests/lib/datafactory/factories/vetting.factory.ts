/**
 * Vetting Application Factory
 *
 * Create and delete test vetting applications via API.
 * REQUIRES: Backend endpoint /api/test-helpers/vetting-applications
 */

import type { APIRequestContext } from '@playwright/test';
import { DataFactoryApiClient } from '../api-client';
import type {
  CreateVettingApplicationRequest,
  VettingApplicationResponse,
  VettingStatus,
  CleanupItem,
} from '../types';

/**
 * Convert VettingStatus string to backend integer (WorkflowStatus)
 * Backend: UnderReview = 0, InterviewApproved = 1, FinalReview = 2, Approved = 3, Denied = 4, OnHold = 5, Withdrawn = 6
 * Frontend uses simplified names that map to these values
 */
function vettingStatusToInt(status: VettingStatus | undefined): number {
  const map: Record<VettingStatus, number> = {
    'Pending': 0,      // Maps to UnderReview
    'InReview': 1,     // Maps to InterviewApproved
    'Approved': 3,     // Maps to Approved
    'Rejected': 4,     // Maps to Denied
  };
  return map[status ?? 'Pending'];
}

export class VettingFactory {
  private client: DataFactoryApiClient;
  private createdApplications: string[] = [];

  constructor(request: APIRequestContext) {
    this.client = new DataFactoryApiClient(request);
  }

  /**
   * Create a test vetting application
   *
   * @example
   * const application = await vettingFactory.create({
   *   userId: user.id,
   *   status: 'Pending'
   * });
   */
  async create(
    options: CreateVettingApplicationRequest
  ): Promise<VettingApplicationResponse> {
    const request = {
      userId: options.userId,
      workflowStatus: vettingStatusToInt(options.status),
      experienceDescription: options.notes ?? 'Test application experience',
      whyJoinCommunity: 'Test application - automated testing',
      howDidYouHearAboutUs: 'Automated test suite',
    };

    const response = await this.client.post<
      typeof request,
      VettingApplicationResponse
    >('/vetting-applications', request);

    this.createdApplications.push(response.id);
    return response;
  }

  /**
   * Create a pending vetting application
   */
  async createPending(userId: string): Promise<VettingApplicationResponse> {
    return this.create({
      userId,
      status: 'Pending',
    });
  }

  /**
   * Create an approved vetting application
   */
  async createApproved(userId: string): Promise<VettingApplicationResponse> {
    return this.create({
      userId,
      status: 'Approved',
    });
  }

  /**
   * Create a vetting application with specific status
   */
  async createWithStatus(
    userId: string,
    status: VettingStatus
  ): Promise<VettingApplicationResponse> {
    return this.create({
      userId,
      status,
    });
  }

  /**
   * Delete a test vetting application
   */
  async delete(applicationId: string): Promise<void> {
    await this.client.delete('/vetting-applications', applicationId);
    this.createdApplications = this.createdApplications.filter(
      (id) => id !== applicationId
    );
  }

  /**
   * Get cleanup items for all created applications
   */
  getCleanupItems(): CleanupItem[] {
    return this.createdApplications.map((id) => ({
      type: 'vettingApplication' as const,
      id,
    }));
  }

  /**
   * Cleanup all created applications
   */
  async cleanupAll(): Promise<void> {
    for (const id of [...this.createdApplications]) {
      try {
        await this.delete(id);
      } catch (error) {
        console.warn(`Failed to cleanup vetting application ${id}:`, error);
      }
    }
  }

  /**
   * Get count of tracked applications
   */
  get count(): number {
    return this.createdApplications.length;
  }
}
