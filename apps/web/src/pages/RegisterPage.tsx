import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useAuth } from '../hooks/useAuth'

const registerSchema = z.object({
  email: z.string().email('Invalid email format'),
  sceneName: z
    .string()
    .min(3, 'Scene name must be at least 3 characters')
    .max(50, 'Scene name must be less than 50 characters')
    .regex(/^[a-zA-Z0-9\s]+$/, 'Scene name can only contain letters, numbers, and spaces'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
    .regex(/[0-9]/, 'Password must contain at least one number')
    .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character'),
})

type RegisterFormData = z.infer<typeof registerSchema>

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate()
  const { register: registerUser, error, clearError } = useAuth()
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setIsSubmitting(true)
      clearError()
      await registerUser(data)

      // Redirect to welcome page after successful registration
      navigate('/welcome', { replace: true })
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
            Join WitchCityRope
          </h1>

          <form onSubmit={handleSubmit(onSubmit)} data-testid="register-form">
            {error && (
              <div
                className="bg-red-500/20 border border-red-500 text-red-200 px-4 py-3 rounded mb-4"
                data-testid="register-error"
              >
                {error}
              </div>
            )}

            <div className="mb-4">
              <label htmlFor="email" className="block text-sm font-medium text-slate-300 mb-1">
                Email *
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

            <div className="mb-4">
              <label htmlFor="sceneName" className="block text-sm font-medium text-slate-300 mb-1">
                Scene Name *
              </label>
              <input
                id="sceneName"
                type="text"
                className="w-full px-3 py-2 border border-slate-600 rounded-md bg-slate-700 text-white focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-purple-500"
                data-testid="scene-name-input"
                {...register('sceneName')}
              />
              {errors.sceneName && (
                <p className="text-red-400 text-sm mt-1">{errors.sceneName.message}</p>
              )}
              <p className="text-slate-400 text-xs mt-1">
                Your display name in the community (3-50 characters)
              </p>
            </div>

            <div className="mb-6">
              <label htmlFor="password" className="block text-sm font-medium text-slate-300 mb-1">
                Password *
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
              <p className="text-slate-400 text-xs mt-1">
                8+ characters with uppercase, lowercase, number, and special character
              </p>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-purple-800 disabled:cursor-not-allowed text-white font-medium py-2 px-4 rounded-md transition-colors"
              data-testid="register-button"
            >
              {isSubmitting ? 'Creating Account...' : 'Create Account'}
            </button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-slate-400">
              Already have an account?{' '}
              <Link
                to="/login"
                className="text-purple-400 hover:text-purple-300 transition-colors"
                data-testid="login-link"
              >
                Login here
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
