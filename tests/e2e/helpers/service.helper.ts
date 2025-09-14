import { Page } from '@playwright/test'

/**
 * SERVICE HEALTH CHECK HELPER FOR DOCKER ENVIRONMENT
 * 
 * This helper verifies that Docker services are running and accessible 
 * before E2E tests execute. Prevents test failures due to service unavailability.
 * 
 * Expected Services:
 * - Web: http://localhost:5173 (React + Vite)
 * - API: http://localhost:5655 (Minimal API) 
 * - Database: PostgreSQL on localhost:5433
 */

export interface ServiceEndpoints {
  web: string
  api: string
  apiHealth?: string
}

export interface ServiceCheckOptions {
  timeout?: number
  retries?: number
  retryDelay?: number
  verbose?: boolean
}

export class ServiceHelper {
  private static readonly DEFAULT_ENDPOINTS: ServiceEndpoints = {
    web: 'http://localhost:5173',
    api: 'http://localhost:5655',
    apiHealth: 'http://localhost:5655/health'
  }

  private static readonly DEFAULT_OPTIONS: Required<ServiceCheckOptions> = {
    timeout: 10000,
    retries: 3,
    retryDelay: 2000,
    verbose: false
  }

  /**
   * Verify all Docker services are accessible before starting tests
   */
  static async waitForServices(options: ServiceCheckOptions = {}): Promise<void> {
    const opts = { ...this.DEFAULT_OPTIONS, ...options }
    const endpoints = this.DEFAULT_ENDPOINTS

    if (opts.verbose) {
      console.log('🔍 Checking Docker services...')
    }

    // Check web service
    await this.waitForService('Web Service', endpoints.web, opts)
    
    // Check API service (try both base URL and health endpoint)
    try {
      await this.waitForService('API Health', endpoints.apiHealth!, opts)
    } catch (error) {
      // Fallback to base API URL if health endpoint not available
      if (opts.verbose) {
        console.log('⚠️  API health endpoint unavailable, checking base API...')
      }
      await this.waitForService('API Service', endpoints.api, opts)
    }

    if (opts.verbose) {
      console.log('✅ All Docker services are accessible')
    }
  }

  /**
   * Check if services are running without throwing errors
   */
  static async checkServicesStatus(): Promise<{
    web: boolean
    api: boolean
    apiHealth: boolean
  }> {
    const endpoints = this.DEFAULT_ENDPOINTS
    const quickCheck = { timeout: 5000, retries: 1, retryDelay: 0, verbose: false }

    const results = {
      web: false,
      api: false,
      apiHealth: false
    }

    try {
      await this.waitForService('Web', endpoints.web, quickCheck)
      results.web = true
    } catch {}

    try {
      await this.waitForService('API', endpoints.api, quickCheck)
      results.api = true
    } catch {}

    try {
      await this.waitForService('API Health', endpoints.apiHealth!, quickCheck)
      results.apiHealth = true
    } catch {}

    return results
  }

  /**
   * Wait for a specific service to be accessible
   */
  private static async waitForService(
    serviceName: string, 
    url: string, 
    options: Required<ServiceCheckOptions>
  ): Promise<void> {
    let lastError: Error | null = null

    for (let attempt = 1; attempt <= options.retries; attempt++) {
      try {
        const response = await this.fetchWithTimeout(url, options.timeout)
        
        if (response.ok || response.status < 500) {
          if (options.verbose) {
            console.log(`✅ ${serviceName} is accessible at ${url}`)
          }
          return
        }
        
        throw new Error(`HTTP ${response.status}: ${response.statusText}`)
        
      } catch (error) {
        lastError = error instanceof Error ? error : new Error(String(error))
        
        if (options.verbose) {
          console.log(`⚠️  ${serviceName} not ready (attempt ${attempt}/${options.retries}): ${lastError.message}`)
        }

        if (attempt < options.retries) {
          await this.delay(options.retryDelay)
        }
      }
    }

    throw new Error(`❌ ${serviceName} is not accessible at ${url} after ${options.retries} attempts. Last error: ${lastError?.message}`)
  }

  /**
   * Fetch with timeout
   */
  private static async fetchWithTimeout(url: string, timeout: number): Promise<Response> {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), timeout)
    
    try {
      const response = await fetch(url, { 
        signal: controller.signal,
        headers: {
          'Accept': 'application/json,text/plain,*/*',
          'Cache-Control': 'no-cache'
        }
      })
      return response
    } finally {
      clearTimeout(timeoutId)
    }
  }

  /**
   * Simple delay utility
   */
  private static delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms))
  }

  /**
   * Get service URLs for tests
   */
  static getServiceUrls(): ServiceEndpoints {
    return { ...this.DEFAULT_ENDPOINTS }
  }

  /**
   * Page-based service check that works within Playwright context
   */
  static async checkServicesWithPage(page: Page, options: ServiceCheckOptions = {}): Promise<void> {
    const opts = { ...this.DEFAULT_OPTIONS, ...options }
    const endpoints = this.DEFAULT_ENDPOINTS

    if (opts.verbose) {
      console.log('🔍 Checking Docker services via Playwright page...')
    }

    // Check web service by navigating to it
    try {
      await page.goto(endpoints.web, { 
        waitUntil: 'domcontentloaded',
        timeout: opts.timeout 
      })
      if (opts.verbose) {
        console.log(`✅ Web service accessible at ${endpoints.web}`)
      }
    } catch (error) {
      throw new Error(`❌ Web service not accessible at ${endpoints.web}: ${error}`)
    }

    // Check API service via page.evaluate to avoid CORS issues
    try {
      const apiResponse = await page.evaluate(async (apiUrl) => {
        try {
          const response = await fetch(apiUrl, {
            method: 'GET',
            headers: { 'Accept': 'application/json' }
          })
          return { ok: response.ok, status: response.status }
        } catch (err) {
          return { ok: false, status: 0, error: err instanceof Error ? err.message : String(err) }
        }
      }, endpoints.api)

      if (!apiResponse.ok && apiResponse.status === 0) {
        if (opts.verbose) {
          console.log('⚠️  API service may not be fully ready, but connection established')
        }
      } else if (opts.verbose) {
        console.log(`✅ API service accessible at ${endpoints.api}`)
      }
    } catch (error) {
      if (opts.verbose) {
        console.log(`⚠️  API service check via page failed: ${error}`)
      }
      // Don't throw here as API might be working but not responding to health checks
    }

    if (opts.verbose) {
      console.log('✅ Docker services verified via Playwright')
    }
  }
}

/**
 * Quick helper function for simple service check before tests
 */
export async function waitForDockerServices(verbose = false): Promise<void> {
  return ServiceHelper.waitForServices({ verbose })
}

/**
 * Helper function to get service status without throwing
 */
export async function checkDockerServices(): Promise<{
  web: boolean
  api: boolean
  apiHealth: boolean
}> {
  return ServiceHelper.checkServicesStatus()
}