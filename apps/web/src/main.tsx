import { StrictMode } from 'react'
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

async function initializeApp() {
  // Initialize MSW for UI testing if enabled
  await enableMocking()
  
  // Render the React app
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <MantineProvider theme={wcrTheme}>
          <Notifications />
          <App />
          <ReactQueryDevtools initialIsOpen={false} />
        </MantineProvider>
      </QueryClientProvider>
    </StrictMode>
  )
}

initializeApp().catch(console.error)
