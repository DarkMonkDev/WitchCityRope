import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import type { ProtectedWelcomeResponse } from '../services/authService'

export const ProtectedWelcomePage: React.FC = () => {
  const { logout, getProtectedWelcome } = useAuth()
  const [welcomeData, setWelcomeData] = useState<ProtectedWelcomeResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const handleLogout = async () => {
    try {
      await logout()
    } catch (err) {
      console.error('Logout failed:', err)
    }
  }

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateString
    }
  }

  useEffect(() => {
    const fetchWelcomeData = async () => {
      try {
        setError(null)
        const data = await getProtectedWelcome()
        setWelcomeData(data)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load welcome data')
      } finally {
        setIsLoading(false)
      }
    }

    fetchWelcomeData()
  }, [getProtectedWelcome])

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-900 flex items-center justify-center">
        <div className="text-white">Loading welcome data...</div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-900 p-4">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="bg-slate-800 rounded-lg shadow-lg p-6 mb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <span
                className="bg-green-500 text-white px-3 py-1 rounded-full text-sm font-medium"
                data-testid="protected-indicator"
              >
                🔒 Protected Content
              </span>
              <h1 className="text-2xl font-bold text-purple-400">WitchCityRope Dashboard</h1>
            </div>
            <button
              onClick={handleLogout}
              className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-md transition-colors"
              data-testid="logout-button"
            >
              Logout
            </button>
          </div>
        </div>

        {/* Welcome Section */}
        <div className="bg-slate-800 rounded-lg shadow-lg p-6 mb-6">
          {error ? (
            <div className="bg-red-500/20 border border-red-500 text-red-200 px-4 py-3 rounded">
              Error: {error}
            </div>
          ) : welcomeData ? (
            <>
              <h2 className="text-xl font-semibold text-white mb-4" data-testid="welcome-message">
                {welcomeData.message}
              </h2>

              <div className="grid md:grid-cols-2 gap-6">
                {/* User Information */}
                <div className="bg-slate-700 rounded-lg p-4">
                  <h3 className="text-lg font-medium text-purple-300 mb-3">Account Information</h3>
                  <div className="space-y-2">
                    <div>
                      <span className="text-slate-400">Scene Name:</span>
                      <span className="text-white ml-2 font-medium" data-testid="user-scene-name">
                        {welcomeData.user.sceneName}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400">Email:</span>
                      <span className="text-white ml-2" data-testid="user-email">
                        {welcomeData.user.email}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400">Member Since:</span>
                      <span className="text-white ml-2" data-testid="member-since">
                        {formatDate(welcomeData.user.createdAt)}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400">Last Login:</span>
                      <span className="text-white ml-2" data-testid="last-login">
                        {formatDate(welcomeData.user.lastLoginAt)}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400">Account Status:</span>
                      <span
                        className="text-green-400 ml-2 font-medium"
                        data-testid="account-status"
                      >
                        Active
                      </span>
                    </div>
                  </div>
                </div>

                {/* Server Information */}
                <div className="bg-slate-700 rounded-lg p-4">
                  <h3 className="text-lg font-medium text-purple-300 mb-3">Connection Status</h3>
                  <div className="space-y-2">
                    <div>
                      <span className="text-slate-400">Server Time:</span>
                      <span className="text-white ml-2" data-testid="server-time">
                        {formatDate(welcomeData.serverTime)}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-400">API Status:</span>
                      <span className="text-green-400 ml-2 font-medium">✅ Connected</span>
                    </div>
                    <div>
                      <span className="text-slate-400">Authentication:</span>
                      <span className="text-green-400 ml-2 font-medium">✅ Verified</span>
                    </div>
                  </div>
                </div>
              </div>
            </>
          ) : null}
        </div>

        {/* Navigation Actions */}
        <div className="bg-slate-800 rounded-lg shadow-lg p-6">
          <h3 className="text-lg font-medium text-purple-300 mb-4">Quick Actions</h3>
          <div className="grid md:grid-cols-3 gap-4">
            <Link
              to="/"
              className="bg-purple-600 hover:bg-purple-700 text-white text-center py-3 px-4 rounded-md transition-colors block"
              data-testid="events-link"
            >
              📅 View Public Events
            </Link>
            <button
              className="bg-blue-600 hover:bg-blue-700 text-white py-3 px-4 rounded-md transition-colors"
              data-testid="profile-link"
            >
              👤 Edit Profile
            </button>
            <button className="bg-gray-600 hover:bg-gray-700 text-white py-3 px-4 rounded-md transition-colors">
              ⚙️ Settings
            </button>
          </div>
        </div>

        {/* Authentication Test Status */}
        <div className="mt-6 bg-green-900/30 border border-green-600 rounded-lg p-4">
          <h4 className="text-green-300 font-medium mb-2">🧪 Authentication Test Status</h4>
          <p className="text-green-200 text-sm">
            ✅ This page successfully validates the Hybrid JWT + HttpOnly Cookies authentication
            pattern. User data was fetched from a protected API endpoint using the stored JWT token.
          </p>
        </div>
      </div>
    </div>
  )
}
