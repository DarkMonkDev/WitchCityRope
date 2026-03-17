import React, { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MantineProvider } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import { ModalsProvider } from '@mantine/modals'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import '@mantine/dates/styles.css'
import '@mantine/tiptap/styles.css'
import '@mantine/charts/styles.css'
import './styles/datepicker-overrides.css'
import './index.css'
import App from './App.tsx'
import { wcrTheme } from './theme'
import { queryClient } from './lib/api/queryClient'
import { enableMocking } from './mocks'
import { debugLog, debugError } from './utils/debug'
import { initializeErrorReporting, reportError } from './lib/errorReporting'

initializeErrorReporting()

debugLog('🔍 Starting React app initialization...')

// Create error boundary to catch React render errors
interface ErrorBoundaryProps {
  children: React.ReactNode;
}

class ErrorBoundary extends React.Component<ErrorBoundaryProps> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error) {
    debugError('🚨 React Error Boundary caught error:', error)
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: any) {
    debugError('🚨 React Error Boundary - Component stack:', errorInfo)
    reportError({
      message: error.message,
      stack: error.stack,
      type: 'react_error',
      url: window.location.href,
      timestamp: new Date().toISOString(),
      userAgent: navigator.userAgent,
      componentStack: errorInfo?.componentStack,
    })
  }

  render() {
    if ((this.state as any).hasError) {
      return (
        <div style={{ padding: '20px' }}>
          <h1>Something went wrong.</h1>
          <p>Error: {((this.state as any).error?.message || 'Unknown error')}</p>
          <details style={{ marginTop: '10px' }}>
            <summary>Error details</summary>
            <pre style={{ background: '#f0f0f0', padding: '10px', overflow: 'auto' }}>
              {((this.state as any).error?.stack || 'No stack trace available')}
            </pre>
          </details>
        </div>
      )
    }
    return this.props.children
  }
}

// Initialize MSW in the background - don't block React rendering
enableMocking().catch(error => {
  debugError('🚨 MSW initialization failed:', error)
  // Continue with app initialization even if MSW fails
})

debugLog('🔍 Creating React root...')
const root = createRoot(document.getElementById('root')!)

debugLog('🔍 Rendering React app with error boundary...')
root.render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <MantineProvider theme={wcrTheme}>
          <ModalsProvider>
            {/* Authentication state managed by Zustand store (authStore.ts) */}
            {/* Auth operations use TanStack Query mutations (features/auth/api/mutations.ts) */}
            <Notifications position="top-right" zIndex={2000} />
            <App />
            <ReactQueryDevtools initialIsOpen={false} />
          </ModalsProvider>
        </MantineProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>
)

debugLog('🔍 React app render complete!')
