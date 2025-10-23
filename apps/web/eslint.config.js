/**
 * ESLint Configuration for WitchCityRope React Application
 *
 * CRITICAL RULES:
 * 1. DTO Alignment Strategy Enforcement (no-restricted-syntax)
 *    - Prevents manual creation of DTO/Request/Response interfaces
 *    - Enforces use of auto-generated types from @witchcityrope/shared-types
 *    - See: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
 *
 * 2. No Silent API Errors (local/no-silent-api-errors)
 *    - Prevents swallowing API errors with try-catch
 *    - Ensures proper error handling and user feedback
 */

import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import react from 'eslint-plugin-react'
import tseslint from 'typescript-eslint'
import prettier from 'eslint-config-prettier'
import noSilentApiErrors from './eslint-rules/no-silent-api-errors.js'

export default tseslint.config([
  { ignores: ['dist'] },
  {
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      prettier,
    ],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      react,
      'local': {
        rules: {
          'no-silent-api-errors': noSilentApiErrors,
        },
      },
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
      'react/react-in-jsx-scope': 'off',
      'react/prop-types': 'off',
      '@typescript-eslint/explicit-module-boundary-types': 'off',
      '@typescript-eslint/no-explicit-any': 'warn',

      /**
       * DTO Alignment Strategy Enforcement - CRITICAL
       *
       * WHY: During React migration (August 2025), manually-created TypeScript
       * interfaces caused 393 compilation errors due to drift from backend DTOs.
       *
       * WHAT: This rule prevents manual creation of interfaces ending in:
       * - Dto (Data Transfer Objects)
       * - Request (API request models)
       * - Response (API response models)
       *
       * HOW TO FIX:
       * ❌ WRONG:
       *   export interface UserDto { id: string; email: string; }
       *
       * ✅ CORRECT:
       *   import type { components } from '@witchcityrope/shared-types';
       *   export type UserDto = components['schemas']['UserDto'];
       *
       * WHEN TO DISABLE:
       * - Legacy files (automatically excluded if filename contains 'legacy')
       * - Frontend-only types (don't end in Dto/Request/Response)
       * - For one-off exceptions, add: // eslint-disable-next-line no-restricted-syntax
       *   with a comment explaining why it's necessary
       *
       * REFERENCES:
       * - Architecture Doc: /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md
       * - Gold Standard: /apps/web/src/features/admin/vetting/types/vetting.types.ts
       */
      'no-restricted-syntax': [
        'error',
        {
          selector: 'TSInterfaceDeclaration[id.name=/.*Dto$/]',
          message: '❌ FORBIDDEN: Manual DTO interfaces violate DTO Alignment Strategy. Import from @witchcityrope/shared-types instead. See /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md'
        },
        {
          selector: 'TSInterfaceDeclaration[id.name=/.*Request$/]',
          message: '❌ FORBIDDEN: Manual Request interfaces violate DTO Alignment Strategy. Import from @witchcityrope/shared-types instead. See /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md'
        },
        {
          selector: 'TSInterfaceDeclaration[id.name=/.*Response$/]',
          message: '❌ FORBIDDEN: Manual Response interfaces violate DTO Alignment Strategy. Import from @witchcityrope/shared-types instead. See /docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md'
        }
      ],

      // Custom project rules
      'local/no-silent-api-errors': [
        'error',
        {
          // Functions allowed to return null/empty on 404 (existence checks)
          allowedFunctions: [
            'checkExistingApplication',
            'getVettingStatus',
            'checkUserExists',
            'findOptionalResource',
          ],
          // Don't allow console.warn in catch blocks even in dev
          allowConsoleInDev: false,
        },
      ],
    },
    settings: {
      react: {
        version: 'detect',
      },
    },
  },
  // Override for legacy files - temporary exception
  // These files should eventually be migrated to use @witchcityrope/shared-types
  {
    files: [
      '**/legacy*.ts',
      '**/legacy*.tsx',
      '**/*.legacy.ts',
      '**/*.legacy.tsx'
    ],
    rules: {
      'no-restricted-syntax': 'off',
    },
  },
])
