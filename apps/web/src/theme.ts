import { createTheme } from '@mantine/core';

export const wcrTheme = createTheme({
  colors: {
    wcr: [
      '#f8f4e6', // ivory (lightest)
      '#e8ddd4',
      '#d4a5a5', // dustyRose  
      '#c48b8b',
      '#b47171',
      '#a45757',
      '#9b4a75', // plum
      '#880124', // burgundy
      '#6b0119', // darker
      '#2c2c2c'  // charcoal (darkest)
    ]
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, sans-serif',
  headings: {
    fontFamily: 'Bodoni Moda, serif'
  },
  components: {
    Button: {
      defaultProps: {
        fw: 700,
      },
      styles: {
        root: {
          height: '56px',
          fontSize: '18px',
          paddingLeft: '32px',
          paddingRight: '32px',
        }
      }
    },
    TextInput: {
      styles: {
        input: {
          borderColor: 'var(--mantine-color-gray-4)',
          transition: 'border-color 0.2s ease-in-out',
          '&:focus': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          },
          '&:focus-visible': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          }
        }
      }
    },
    PasswordInput: {
      styles: {
        input: {
          borderColor: 'var(--mantine-color-gray-4)',
          transition: 'border-color 0.2s ease-in-out',
          '&:focus': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          },
          '&:focus-visible': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          }
        }
      }
    },
    Textarea: {
      styles: {
        input: {
          borderColor: 'var(--mantine-color-gray-4)',
          transition: 'border-color 0.2s ease-in-out',
          '&:focus': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          },
          '&:focus-visible': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          }
        }
      }
    },
    Select: {
      styles: {
        input: {
          borderColor: 'var(--mantine-color-gray-4)',
          transition: 'border-color 0.2s ease-in-out',
          '&:focus': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          },
          '&:focus-visible': {
            outline: 'none !important',
            outlineWidth: '0 !important',
            outlineStyle: 'none !important',
            outlineColor: 'transparent !important',
            outlineOffset: '0 !important',
            borderColor: 'var(--mantine-color-wcr-6, var(--mantine-color-blue-6)) !important',
            boxShadow: 'none !important'
          }
        }
      }
    }
  }
});