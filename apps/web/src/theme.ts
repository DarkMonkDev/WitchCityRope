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
    }
  }
});