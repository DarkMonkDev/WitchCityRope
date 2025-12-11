/**
 * DataFactory API Client
 *
 * HTTP client for test helper endpoints.
 * Handles all communication with /api/test-helpers/*
 */

import type { APIRequestContext } from '@playwright/test';

const TEST_HELPERS_BASE = '/api/test-helpers';

export class DataFactoryApiClient {
  constructor(private request: APIRequestContext) {}

  // ============================================
  // GENERIC METHODS
  // ============================================

  async post<TRequest, TResponse>(
    endpoint: string,
    data: TRequest
  ): Promise<TResponse> {
    const response = await this.request.post(
      `${TEST_HELPERS_BASE}${endpoint}`,
      { data }
    );

    if (!response.ok()) {
      const error = await response.text();
      throw new Error(
        `DataFactory API error: POST ${endpoint} - Status ${response.status()} - ${error}`
      );
    }

    return response.json();
  }

  async delete(endpoint: string, id: string): Promise<void> {
    const response = await this.request.delete(
      `${TEST_HELPERS_BASE}${endpoint}/${id}`
    );

    // 404 is acceptable for delete (already deleted)
    if (!response.ok() && response.status() !== 404) {
      const error = await response.text();
      throw new Error(
        `DataFactory API error: DELETE ${endpoint}/${id} - Status ${response.status()} - ${error}`
      );
    }
  }

  // ============================================
  // HEALTH CHECK
  // ============================================

  async healthCheck(): Promise<boolean> {
    try {
      const response = await this.request.get(`${TEST_HELPERS_BASE}/health`);
      return response.ok();
    } catch {
      return false;
    }
  }

  /**
   * Verify test helpers are available before running tests
   */
  async ensureAvailable(): Promise<void> {
    const isHealthy = await this.healthCheck();
    if (!isHealthy) {
      throw new Error(
        'DataFactory: Test helper endpoints are not available. ' +
          'Ensure the API is running in Development or Test environment.'
      );
    }
  }
}

// ============================================
// SINGLETON SUPPORT
// ============================================

let globalClient: DataFactoryApiClient | null = null;

export function setGlobalClient(request: APIRequestContext): void {
  globalClient = new DataFactoryApiClient(request);
}

export function getGlobalClient(): DataFactoryApiClient {
  if (!globalClient) {
    throw new Error(
      'DataFactory API client not initialized. Call setGlobalClient first.'
    );
  }
  return globalClient;
}

export function clearGlobalClient(): void {
  globalClient = null;
}
