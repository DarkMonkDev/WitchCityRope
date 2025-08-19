import React from 'react'
import { Link } from 'react-router-dom'
import { EventsList } from '../components/EventsList'
import { useUser, useIsAuthenticated } from '../stores/authStore'

export const HomePage: React.FC = () => {
  const user = useUser();
  const isAuthenticated = useIsAuthenticated();

  return (
    <div className="py-8">
      <div className="text-center mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">WitchCityRope Events</h1>
        <h2 className="text-xl text-slate-300 mb-1">Technical Stack Test</h2>
        <p className="text-sm text-slate-400 mb-2">
          This is throwaway code for validating React + API + Database communication
        </p>
        <div className="space-x-3 mb-4">
          <Link
            to="/api-connection-test"
            className="inline-block bg-green-600 hover:bg-green-700 text-white px-3 py-1 rounded text-sm transition-colors"
          >
            🔗 Test Real API Connection
          </Link>
          <Link
            to="/test-msw"
            className="inline-block bg-purple-600 hover:bg-purple-700 text-white px-3 py-1 rounded text-sm transition-colors"
          >
            🧪 Test MSW Mocking
          </Link>
        </div>

        {/* Authentication Status Banner */}
        <div className="max-w-md mx-auto mb-6">
          {isAuthenticated ? (
            <div className="bg-green-900/30 border border-green-600 rounded-lg p-4">
              <p className="text-green-300 font-medium">✅ Welcome back, {user?.sceneName}!</p>
              <p className="text-green-200 text-sm">
                You are logged in and can access protected features.
              </p>
              <Link to="/welcome" className="text-green-300 hover:text-green-200 underline text-sm">
                Go to Dashboard →
              </Link>
            </div>
          ) : (
            <div className="bg-blue-900/30 border border-blue-600 rounded-lg p-4">
              <p className="text-blue-300 font-medium">🔐 Authentication Test Available</p>
              <p className="text-blue-200 text-sm mb-3">
                Test the authentication system by logging in or creating a new account.
              </p>
              <div className="space-x-3">
                <Link
                  to="/login"
                  className="bg-purple-600 hover:bg-purple-700 text-white px-3 py-1 rounded text-sm transition-colors"
                >
                  Login
                </Link>
                <Link
                  to="/register"
                  className="bg-blue-600 hover:bg-blue-700 text-white px-3 py-1 rounded text-sm transition-colors"
                >
                  Register
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>

      <EventsList />
    </div>
  )
}
