import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff } from 'lucide-react';
import { BaseInput, FormField } from '../components/forms';
import { loginSchema, type LoginFormData } from '../schemas/formSchemas';
import { useAuth } from '../hooks/useAuth';
import { useApiErrorHandler, useFormSubmissionState } from '../hooks/useFormValidation';

/**
 * Example implementation of LoginPage using the new form components
 * This replaces the existing LoginPage.tsx with standardized form components
 */
export const LoginFormExample: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, error, clearError } = useAuth();
  const { mapApiErrorsToForm } = useApiErrorHandler();
  const [showPassword, setShowPassword] = useState(false);

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false
    }
  });

  const formState = useFormSubmissionState(form.formState);

  const onSubmit = async (data: LoginFormData) => {
    try {
      clearError();
      // Add emailOrSceneName field (backend requires it)
      await login({ ...data, emailOrSceneName: data.email });

      // Redirect to the page they were trying to visit or to welcome page
      const from = location.state?.from?.pathname || '/welcome';
      navigate(from, { replace: true });
    } catch (error: any) {
      // Map API errors to form fields
      if (error.response) {
        mapApiErrorsToForm(error.response, form.setError);
      } else {
        form.setError('root', {
          type: 'manual',
          message: error.message || 'Login failed. Please try again.'
        });
      }
    }
  };

  return (
    <div className="min-h-screen bg-slate-900 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="bg-slate-800 rounded-lg shadow-lg p-8">
          <h1 className="text-2xl font-bold text-purple-400 text-center mb-6">
            Login to WitchCityRope
          </h1>

          <form onSubmit={form.handleSubmit(onSubmit)} data-testid="login-form">
            {/* General Form Error */}
            {(error || form.formState.errors.root) && (
              <div
                className="bg-red-500/20 border border-red-500 text-red-200 px-4 py-3 rounded mb-4"
                data-testid="login-error"
              >
                {error || form.formState.errors.root?.message}
              </div>
            )}

            {/* Email Input */}
            <BaseInput
              label="Email Address"
              type="email"
              placeholder="Enter your email"
              required
              autoComplete="email"
              {...form.register('email')}
              error={form.formState.errors.email?.message}
              data-testid="email-input"
            />

            {/* Password Input with Toggle Visibility */}
            <FormField>
              <BaseInput
                label="Password"
                required
                id="password"
                type={showPassword ? 'text' : 'password'}
                placeholder="Enter your password"
                rightSection={
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="text-slate-400 hover:text-slate-300 transition-colors"
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                }
                autoComplete="current-password"
                {...form.register('password')}
                error={form.formState.errors.password?.message}
                data-testid="password-input"
              />
            </FormField>

            {/* Remember Me Checkbox */}
            <FormField className="mb-6">
              <label className="flex items-center text-sm text-slate-300">
                <input
                  type="checkbox"
                  className="mr-2 rounded border-slate-600 bg-slate-700 text-purple-600 focus:ring-purple-500"
                  {...form.register('rememberMe')}
                  data-testid="remember-me-checkbox"
                />
                Remember me for 30 days
              </label>
            </FormField>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={!formState.canSubmit}
              className="w-full bg-purple-600 hover:bg-purple-700 disabled:bg-purple-800 disabled:cursor-not-allowed text-white font-medium py-2 px-4 rounded-md transition-colors"
              data-testid="login-button"
            >
              {formState.isSubmitting ? 'Logging in...' : 'Login'}
            </button>
          </form>

          {/* Links */}
          <div className="mt-6 text-center space-y-2">
            <p className="text-slate-400">
              <Link
                to="/forgot-password"
                className="text-purple-400 hover:text-purple-300 transition-colors"
                data-testid="forgot-password-link"
              >
                Forgot your password?
              </Link>
            </p>
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
  );
};

/**
 * Comparison: Old vs New Implementation
 * 
 * OLD (LoginPage.tsx):
 * - Raw HTML inputs with manual styling
 * - Manual error handling and display
 * - Inconsistent ARIA attributes
 * - No loading states for individual fields
 * - Hardcoded CSS classes
 * 
 * NEW (LoginFormExample.tsx):
 * - Standardized BaseInput components
 * - Automatic error handling and display
 * - Built-in accessibility features
 * - Loading states and icons
 * - Consistent styling across components
 * - Better form state management
 * - Enhanced user experience features (password toggle)
 */