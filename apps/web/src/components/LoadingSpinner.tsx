import React from 'react'

export const LoadingSpinner: React.FC = () => {
  return (
    <div className="flex justify-center items-center py-8" data-testid="loading-spinner">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      <span className="ml-2">Loading events...</span>
    </div>
  )
}
