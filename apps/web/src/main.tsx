import React, { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MantineProvider } from '@mantine/core'
import { Notifications } from '@mantine/notifications'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'
import './index.css'
import App from './App.tsx'
import { wcrTheme } from './theme'
import { queryClient } from './lib/api/queryClient'
import { enableMocking } from './mocks'

console.log('🔍 Starting React app initialization...')

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
    console.error('🚨 React Error Boundary caught error:', error)
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: any) {
    console.error('🚨 React Error Boundary - Component stack:', errorInfo)
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
  console.error('🚨 MSW initialization failed:', error)
  // Continue with app initialization even if MSW fails
})

console.log('🔍 Creating React root...')
const root = createRoot(document.getElementById('root')!)

console.log('🔍 Rendering React app with error boundary...')
root.render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <MantineProvider theme={wcrTheme}>
          <Notifications />
          <App />
          <ReactQueryDevtools initialIsOpen={false} />
        </MantineProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>
)

console.log('🔍 React app render complete!')
