import React, { useState } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useAuth } from '../hooks/useAuth'

const loginSchema = z.object({
  email: z.string().email('Invalid email format'),
  password: z.string().min(1, 'Password is required'),
})

type LoginFormData = z.infer<typeof loginSchema>

export const LoginPage: React.FC = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, error, clearError } = useAuth()
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = async (data: LoginFormData) => {
    try {
      setIsSubmitting(true)
      clearError()
      await login(data)

      // Redirect to the page they were trying to visit or to welcome page
      const from = location.state?.from?.pathname || '/welcome'
      navigate(from, { replace: true })
    } catch {
      // Error is handled by AuthContext
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen bg-slate-900 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="bg-slate-800 rounded-lg shadow-lg p-8">
          <h1 className="text-2xl font-bold text-purple-400 text-center mb-6">
            Login to WitchCityRope
          </h1>

          <form onSubmit={handleSubmit(onSubmit)} data-testid="login-form">
            {error && (
              <div
                className="bg-red-500/20 border border-red-500 text-red-200 px-4 py-3 rounded mb-4"
                data-testid="login-error"
              >
                {error}
              </div>
            )}

            <div className="mb-4">
              <label htmlFor="email" className="block text-sm font-medium text-slate-300 mb-1">
                Email
              </label>
              <input
                id="email"
                type="email"
                className="w-full px-3 py-2 border border-slate-600 rounded-md bg-slate-700 text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                data-testid="email-input"
                {...register('email')}
              />
              {errors.email && <p className="text-red-400 text-sm mt-1">{errors.email.message}</p>}
            </div>

            <div className="mb-6">
              <label htmlFor="password" className="block text-sm font-medium text-slate-300 mb-1">
                Password
              </label>
              <input
                id="password"
                type="password"
                className="w-full px-3 py-2 border border-slate-600 rounded-md bg-slate-700 text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                data-testid="password-input"
                {...register('password')}
              />
              {errors.password && (
                <p className="text-red-400 text-sm mt-1">{errors.password.message}</p>
              )}
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-purple-800 disabled:cursor-not-allowed text-white font-medium py-2 px-4 rounded-md transition-colors"
              data-testid="login-button"
            >
              {isSubmitting ? 'Logging in...' : 'Login'}
            </button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-slate-400">
              Don't have an account?{' '}
              <Link
                to="/register"
                className="text-purple-400 hover:text-purple-300 transition-colors"
                data-testid="register-link"
              >
                Register here
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
