/**
 * CSRF Token Store - State Management for CSRF Protection
 *
 * Prevents race condition where login attempts execute BEFORE CSRF token is available.
 * Provides centralized state for:
 * - CSRF initialization status (isReady)
 * - Loading state (isLoading)
 * - Error handling
 *
 * Usage:
 * - App.tsx calls initialize() on mount
 * - LoginPage checks isReady before enabling login button
 * - Other forms can check isReady before submitting state-changing requests
 */

import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { initializeCSRFProtection } from '../hooks/useCSRFToken'
import { debugLog, debugError } from '../utils/debug'

interface CSRFState {
  isReady: boolean
  isLoading: boolean
  error: string | null
  initialize: () => Promise<void>
  reset: () => void
}

export const useCSRFStore = create<CSRFState>()(
  devtools(
    (set) => ({
      isReady: false,
      isLoading: false,
      error: null,

      initialize: async () => {
        debugLog('🔍 CSRF Store: Starting initialization...')
        set({ isLoading: true, error: null })

        try {
          await initializeCSRFProtection()
          debugLog('✅ CSRF Store: Initialization complete - token ready')
          set({ isReady: true, isLoading: false })
        } catch (error: any) {
          const errorMessage = error?.message || 'Failed to initialize security'
          debugError('❌ CSRF Store: Initialization failed:', errorMessage)
          set({ isReady: false, isLoading: false, error: errorMessage })
        }
      },

      reset: () => {
        debugLog('🔍 CSRF Store: Resetting state')
        set({ isReady: false, isLoading: false, error: null })
      },
    }),
    { name: 'csrf-store' }
  )
)
