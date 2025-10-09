/**
 * Tests for no-silent-api-errors ESLint rule
 *
 * Created: 2025-10-09
 */

import { RuleTester } from 'eslint';
import rule from '../no-silent-api-errors.js';

const ruleTester = new RuleTester({
  parserOptions: {
    ecmaVersion: 2020,
    sourceType: 'module',
  },
});

ruleTester.run('no-silent-api-errors', rule, {
  valid: [
    // Valid: Throwing error on 404
    {
      code: `
        async function getUser(id) {
          try {
            const response = await api.get(\`/users/\${id}\`);
            return response.data;
          } catch (error) {
            if (error.response?.status === 404) {
              throw new Error(\`User with ID "\${id}" was not found.\`);
            }
            throw error;
          }
        }
      `,
    },

    // Valid: Null return with justification comment
    {
      code: `
        async function checkUserExists(id) {
          try {
            await api.get(\`/users/\${id}\`);
            return true;
          } catch (error) {
            // Intentional: Checking existence
            if (error.response?.status === 404) {
              return null;
            }
            throw error;
          }
        }
      `,
    },

    // Valid: Console.error with throw
    {
      code: `
        async function getApplications() {
          try {
            return await api.get('/applications');
          } catch (error) {
            console.error('Failed to fetch applications:', error);
            throw error;
          }
        }
      `,
    },

    // Valid: Mutation with user notification
    {
      code: `
        const mutation = useMutation({
          mutationFn: createEvent,
          onError: (error) => {
            console.error('Failed to create event:', error);
            notifications.show({
              title: 'Error',
              message: error.message,
              color: 'red',
            });
          }
        });
      `,
    },

    // Valid: React Query with proper error handling
    {
      code: `
        export function useVettingApplications(filters) {
          return useQuery({
            queryKey: ['applications', filters],
            queryFn: async () => {
              try {
                const result = await api.getApplications(filters);
                return result;
              } catch (error) {
                console.error('API call failed:', error);
                throw error;
              }
            },
            throwOnError: true,
          });
        }
      `,
    },

    // Valid: Allowed function (configured)
    {
      code: `
        async function checkExistingApplication(userId) {
          try {
            await api.get(\`/applications/\${userId}\`);
            return true;
          } catch (error) {
            if (error.response?.status === 404) {
              return null;
            }
            throw error;
          }
        }
      `,
      options: [{
        allowedFunctions: ['checkExistingApplication'],
      }],
    },

    // Valid: Justified empty return with comment
    {
      code: `
        async function getOptionalProfile(userId) {
          try {
            const response = await api.get(\`/profiles/\${userId}\`);
            return response.data;
          } catch (error) {
            // Intentional: Profile is optional, empty object means no profile
            if (error.response?.status === 404) {
              return {};
            }
            throw error;
          }
        }
      `,
    },

    // Valid: Non-404 status check
    {
      code: `
        async function getUser(id) {
          try {
            const response = await api.get(\`/users/\${id}\`);
            return response.data;
          } catch (error) {
            if (error.response?.status === 401) {
              return null;
            }
            throw error;
          }
        }
      `,
    },

    // Valid: Empty return NOT in catch (legitimate pattern)
    {
      code: `
        function processItems(items) {
          if (!items || items.length === 0) {
            return [];
          }
          return items.map(i => i.name);
        }
      `,
    },
  ],

  invalid: [
    // Invalid: Silent 404 return (empty)
    {
      code: `
        async function getUser(id) {
          try {
            const response = await api.get(\`/users/\${id}\`);
            return response.data;
          } catch (error) {
            if (error.response?.status === 404) {
              return;
            }
            throw error;
          }
        }
      `,
      errors: [
        {
          messageId: 'silentReturn404',
        },
      ],
    },

    // Invalid: Silent 404 return (empty object)
    {
      code: `
        async function getUser(id) {
          try {
            const response = await api.get(\`/users/\${id}\`);
            return response.data;
          } catch (error) {
            if (error.response?.status === 404) {
              return {};
            }
            throw error;
          }
        }
      `,
      errors: [
        {
          messageId: 'silentReturn404',
        },
      ],
    },

    // Invalid: Silent 404 return (empty array)
    {
      code: `
        async function getUsers() {
          try {
            const response = await api.get('/users');
            return response.data;
          } catch (error) {
            if (error.response?.status === 404) {
              return [];
            }
            throw error;
          }
        }
      `,
      errors: [
        {
          messageId: 'silentReturn404',
        },
      ],
    },

    // Invalid: Console.warn with return in catch
    {
      code: `
        async function getApplications() {
          try {
            return await api.get('/applications');
          } catch (error) {
            console.warn('API error:', error);
            return mockData;
          }
        }
      `,
      errors: [
        {
          messageId: 'consoleWarnInCatch',
        },
      ],
    },

    // Invalid: Silent mutation onError (only console.log)
    {
      code: `
        const mutation = useMutation({
          mutationFn: createEvent,
          onError: (error) => {
            console.log(error);
          }
        });
      `,
      errors: [
        {
          messageId: 'silentMutationError',
        },
      ],
    },

    // Invalid: Empty return in catch block
    {
      code: `
        async function getVettingApplications() {
          try {
            return await api.get('/applications');
          } catch (error) {
            return [];
          }
        }
      `,
      errors: [
        {
          messageId: 'emptyReturnOnError',
        },
      ],
    },

    // Invalid: Empty object return in catch block
    {
      code: `
        async function getSettings() {
          try {
            return await api.get('/settings');
          } catch (error) {
            return {};
          }
        }
      `,
      errors: [
        {
          messageId: 'emptyReturnOnError',
        },
      ],
    },

    // Invalid: Null return on 404 without justification
    {
      code: `
        async function getUser(id) {
          try {
            const response = await api.get(\`/users/\${id}\`);
            return response.data;
          } catch (error) {
            if (error.response?.status === 404) {
              return null;
            }
            throw error;
          }
        }
      `,
      errors: [
        {
          messageId: 'silentReturn404',
        },
      ],
    },

    // Invalid: Console.warn with return (masking error)
    {
      code: `
        export function useVettingApplications(filters) {
          return useQuery({
            queryKey: ['applications', filters],
            queryFn: async () => {
              try {
                const result = await api.getApplications(filters);
                return result;
              } catch (error) {
                console.warn('API failed:', error);
                return { items: sampleApplications };
              }
            }
          });
        }
      `,
      errors: [
        {
          messageId: 'consoleWarnInCatch',
        },
      ],
    },
  ],
});

console.log('✅ All tests passed for no-silent-api-errors rule');
