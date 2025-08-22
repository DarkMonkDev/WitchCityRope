import { createTheme } from '@mantine/core';

// Design System v7 Color Palette - Extracted from homepage-template-v7.html
export const wcrTheme = createTheme({
  colors: {
    // Primary burgundy palette
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
  primaryColor: 'burgundy',
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
          padding: '14px 32px',
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
        }
      },
      variants: {
        // v7 Primary button (amber gradient)
        'v7-primary': (theme: any) => ({
          root: {
            background: `linear-gradient(135deg, ${theme.other.colorAmber} 0%, ${theme.other.colorAmberDark} 100%)`,
            color: theme.other.colorMidnight,
            boxShadow: '0 4px 15px rgba(255, 191, 0, 0.4)',
            fontWeight: 700,
            '&:hover': {
              boxShadow: '0 6px 25px rgba(255, 191, 0, 0.5)',
              background: `linear-gradient(135deg, ${theme.other.colorAmberDark} 0%, ${theme.other.colorAmber} 100%)`,
            }
          }
        }),
        // v7 Primary Alt button (electric gradient)
        'v7-primary-alt': (theme: any) => ({
          root: {
            background: `linear-gradient(135deg, ${theme.other.colorElectric} 0%, ${theme.other.colorElectricDark} 100%)`,
            color: theme.other.colorIvory,
            boxShadow: '0 4px 15px rgba(157, 78, 221, 0.4)',
            fontWeight: 700,
            '&:hover': {
              boxShadow: '0 6px 25px rgba(157, 78, 221, 0.5)',
              background: `linear-gradient(135deg, ${theme.other.colorElectricDark} 0%, ${theme.other.colorElectric} 100%)`,
            }
          }
        }),
        // v7 Secondary button (burgundy outline)
        'v7-secondary': (theme: any) => ({
          root: {
            background: 'transparent',
            color: theme.other.colorBurgundy,
            border: `2px solid ${theme.other.colorBurgundy}`,
            position: 'relative',
            zIndex: 1,
            '&::before': {
              content: '""',
              position: 'absolute',
              top: 0,
              left: 0,
              width: 0,
              height: '100%',
              background: theme.other.colorBurgundy,
              transition: 'width 0.4s ease',
              zIndex: -1,
            },
            '&:hover': {
              color: theme.other.colorIvory,
              '&::before': {
                width: '100%',
              }
            }
          }
        })
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