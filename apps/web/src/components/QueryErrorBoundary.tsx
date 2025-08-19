// components/QueryErrorBoundary.tsx
import { QueryErrorResetBoundary } from '@tanstack/react-query'
import { ErrorBoundary } from 'react-error-boundary'

function QueryErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <QueryErrorResetBoundary>
      {(({ reset }) => (
        <ErrorBoundary
          onReset={reset}
          fallbackRender={({ resetErrorBoundary, error }) => (
            <div className="error-container p-4 border border-red-300 rounded bg-red-50">
              <h2 className="text-lg font-semibold text-red-800">
                Something went wrong
              </h2>
              <p className="text-red-600 mt-2">
                {error.message || 'An unexpected error occurred'}
              </p>
              <button 
                onClick={resetErrorBoundary}
                className="mt-4 px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
              >
                Try again
              </button>
            </div>
          )}
        >
          {children}
        </ErrorBoundary>
      )) as unknown as React.ReactNode}
    </QueryErrorResetBoundary>
  )
}

export default QueryErrorBoundary