import { createTheme } from '@mantine/core';

// Design System v7 Color Palette - Extracted from homepage-template-v7.html
export const wcrTheme = createTheme({
  colors: {
    // Primary burgundy palette for public events (updated to match design specs)
    wcr: [
      '#f8f4e6', // ivory (lightest)
      '#e8ddd4',
      '#d4a5a5', // dustyRose  
      '#c48b8b',
      '#b47171',
      '#a45757',
      '#9b4a75', // plum
      '#880124', // burgundy (primary)
      '#6b0119', // darker
      '#2c2c2c'  // charcoal (darkest)
    ],
    // Keep existing burgundy for backward compatibility
    burgundy: [
      '#F5E8EA', // lightest tint
      '#E5C2C8',
      '#D4A5A5', // dusty-rose
      '#B47171',
      '#9F1D35', // burgundy-light
      '#880124', // burgundy (primary)
      '#660018', // burgundy-dark
      '#4A0012',
      '#33000C',
      '#1A0006'  // darkest
    ],
    // Warm metallics
    roseGold: [
      '#F7F0F1',
      '#EDD9DB',
      '#E3C2C5',
      '#D9ABAF',
      '#CF9499',
      '#B76D75', // rose-gold
      '#A5616A',
      '#93555F',
      '#814954',
      '#6F3D49'
    ],
    // Electric purple for CTAs
    electric: [
      '#F4EBFA',
      '#E0C4F2',
      '#CC9DEA',
      '#B876E2',
      '#A44FDA',
      '#9D4EDD', // electric
      '#8A44C4',
      '#773AAB',
      '#643092',
      '#512679'
    ],
    // Amber for primary buttons
    amber: [
      '#FFF8E6',
      '#FFEFB8',
      '#FFE58A',
      '#FFDB5C',
      '#FFD12E',
      '#FFBF00', // amber
      '#E6AC00',
      '#CC9900',
      '#B38600',
      '#997300'
    ],
    // Supporting neutrals
    neutrals: [
      '#FFF8F0', // ivory
      '#FAF6F2', // cream
      '#B8B0A8', // taupe
      '#8B8680', // stone
      '#4A4A4A', // smoke
      '#2B2B2B', // charcoal
      '#1A1A2E', // midnight
      '#614B79', // plum
      '#C9A961', // brass
      '#B87333'  // copper
    ]
  },
  primaryColor: 'wcr',
  fontFamily: 'Source Sans 3, sans-serif',
  headings: {
    fontFamily: 'Montserrat, sans-serif', // Updated to match v7
    fontWeight: '800'
  },
  other: {
    // v7 Design Tokens for CSS custom properties
    fontDisplay: 'Bodoni Moda, serif',
    fontAccent: 'Satisfy, cursive',
    
    // Spacing tokens
    spaceXs: '8px',
    spaceSm: '16px',
    spaceMd: '24px',
    spaceLg: '32px',
    spaceXl: '40px',
    space2xl: '48px',
    space3xl: '64px',
    
    // Color tokens (for CSS custom properties)
    colorBurgundy: '#880124',
    colorBurgundyDark: '#660018',
    colorBurgundyLight: '#9F1D35',
    colorRoseGold: '#B76D75',
    colorElectric: '#9D4EDD',
    colorElectricDark: '#7B2CBF',
    colorAmber: '#FFBF00',
    colorAmberDark: '#FF8C00',
    colorDustyRose: '#D4A5A5',
    colorPlum: '#614B79',
    colorMidnight: '#1A1A2E',
    colorCharcoal: '#2B2B2B',
    colorSmoke: '#4A4A4A',
    colorStone: '#8B8680',
    colorTaupe: '#B8B0A8',
    colorIvory: '#FFF8F0',
    colorCream: '#FAF6F2'
  },
  components: {
    Button: {
      defaultProps: {
        fw: 700,
      },
      styles: {
        root: {
          height: '42px',
          padding: '12px 32px',
          borderRadius: '12px 6px 12px 6px', // v7 button corner morphing start
          fontFamily: 'Montserrat, sans-serif',
          fontWeight: 600,
          fontSize: '14px',
          textTransform: 'uppercase',
          letterSpacing: '1.5px',
          transition: 'all 0.3s ease',
          position: 'relative',
          overflow: 'hidden',
          '&:hover': {
            borderRadius: '6px 12px 6px 12px', // v7 button corner morphing end
          }
        },
        label: {
          lineHeight: '1.2',
        }
      }
      // Note: Button variants moved to CSS classes in index.css per v7 design system
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
          '&:focusVisible': {
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
          '&:focusVisible': {
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
          '&:focusVisible': {
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
          '&:focusVisible': {
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