/**
 * Vetting Workflow Scenario
 *
 * Creates a complete vetting application workflow for testing.
 * Includes user creation and vetting application in various states.
 */

import type { APIRequestContext } from '@playwright/test';
import { UserFactory } from '../factories/user.factory';
import { VettingFactory } from '../factories/vetting.factory';
import type {
  UserResponse,
  VettingApplicationResponse,
  VettingStatus,
} from '../types';

export interface VettingWorkflowData {
  user: UserResponse;
  application: VettingApplicationResponse;
}

export interface VettingWorkflowOptions {
  /** Email for the user (will be auto-generated if not provided) */
  email?: string;
  /** Initial vetting status */
  status?: VettingStatus;
  /** User's first name */
  firstName?: string;
  /** User's last name */
  lastName?: string;
}

/**
 * Create a user with a vetting application
 *
 * @example
 * const { user, application } = await createVettingWorkflow(request, {
 *   status: 'Pending'
 * });
 */
export async function createVettingWorkflow(
  request: APIRequestContext,
  options: VettingWorkflowOptions = {}
): Promise<VettingWorkflowData> {
  const userFactory = new UserFactory(request);
  const vettingFactory = new VettingFactory(request);

  // Generate unique email if not provided
  const email =
    options.email ?? `vetting-test-${Date.now()}@test.witchcityrope.com`;

  // Create and verify user
  const user = await userFactory.createVerified({
    email,
    firstName: options.firstName ?? 'Vetting',
    lastName: options.lastName ?? 'Applicant',
  });

  // Create vetting application
  const application = await vettingFactory.create({
    userId: user.id,
    status: options.status ?? 'Pending',
  });

  return { user, application };
}

/**
 * Create a pending vetting application
 */
export async function createPendingVettingApplication(
  request: APIRequestContext,
  email?: string
): Promise<VettingWorkflowData> {
  return createVettingWorkflow(request, {
    email,
    status: 'Pending',
  });
}

/**
 * Create an approved vetting application (vetted member)
 */
export async function createApprovedVettingApplication(
  request: APIRequestContext,
  email?: string
): Promise<VettingWorkflowData> {
  return createVettingWorkflow(request, {
    email,
    status: 'Approved',
  });
}

/**
 * Create a rejected vetting application
 */
export async function createRejectedVettingApplication(
  request: APIRequestContext,
  email?: string
): Promise<VettingWorkflowData> {
  return createVettingWorkflow(request, {
    email,
    status: 'Rejected',
  });
}

/**
 * Create an in-review vetting application
 */
export async function createInReviewVettingApplication(
  request: APIRequestContext,
  email?: string
): Promise<VettingWorkflowData> {
  return createVettingWorkflow(request, {
    email,
    status: 'InReview',
  });
}

/**
 * Cleanup vetting workflow data
 */
export async function cleanupVettingWorkflow(
  request: APIRequestContext,
  data: VettingWorkflowData
): Promise<void> {
  const vettingFactory = new VettingFactory(request);
  const userFactory = new UserFactory(request);

  // Delete application first (depends on user)
  await vettingFactory.delete(data.application.id);
  await userFactory.delete(data.user.id);
}
