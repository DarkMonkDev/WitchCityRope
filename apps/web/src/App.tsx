import React from 'react'
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom'
import { AuthProvider, useAuth } from './contexts/AuthContext'
import { ProtectedRoute } from './components/ProtectedRoute'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { ProtectedWelcomePage } from './pages/ProtectedWelcomePage'
import { FormComponentsTest } from './pages/FormComponentsTest'
import MantineFormTest from './pages/MantineFormTest'
import './App.css'

const Navigation: React.FC = () => {
  const { isAuthenticated, user, logout } = useAuth()

  const handleLogout = async () => {
    try {
      await logout()
    } catch (err) {
      console.error('Logout failed:', err)
    }
  }

  return (
    <nav className="bg-slate-800 shadow-lg mb-8">
      <div className="max-w-6xl mx-auto px-4">
        <div className="flex justify-between items-center h-16">
          <div className="flex items-center space-x-4">
            <Link
              to="/"
              className="text-purple-400 hover:text-purple-300 font-bold text-xl transition-colors"
            >
              WitchCityRope
            </Link>
            <Link to="/" className="text-slate-300 hover:text-white transition-colors">
              Events
            </Link>
            <Link to="/form-test" className="text-slate-300 hover:text-white transition-colors">
              Form Test
            </Link>
            <Link to="/mantine-forms" className="text-slate-300 hover:text-white transition-colors">
              Mantine Forms
            </Link>
          </div>

          <div className="flex items-center space-x-4">
            {isAuthenticated ? (
              <>
                <Link to="/welcome" className="text-slate-300 hover:text-white transition-colors">
                  Welcome, {user?.sceneName}
                </Link>
                <button
                  onClick={handleLogout}
                  className="bg-red-600 hover:bg-red-700 text-white px-3 py-1 rounded text-sm transition-colors"
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-slate-300 hover:text-white transition-colors">
                  Login
                </Link>
                <Link
                  to="/register"
                  className="bg-purple-600 hover:bg-purple-700 text-white px-3 py-1 rounded text-sm transition-colors"
                >
                  Register
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  )
}

const AppContent: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-900">
      <Navigation />
      <div className="max-w-6xl mx-auto px-4">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/form-test" element={<FormComponentsTest />} />
          <Route path="/mantine-forms" element={<MantineFormTest />} />
          <Route
            path="/welcome"
            element={
              <ProtectedRoute>
                <ProtectedWelcomePage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </div>
    </div>
  )
}

function App() {
  return (
    <Router>
      <AuthProvider>
        <AppContent />
      </AuthProvider>
    </Router>
  )
}

export default App
// HMR test comment added at Sun Aug 17 04:37:55 PM EDT 2025
