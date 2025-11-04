/**
 * Check-In System Color Palette & Design Tokens
 * Source: /docs/design/wireframes/event-checkin-visual.html
 *
 * This theme matches the approved wireframe design EXACTLY.
 * DO NOT modify colors without updating wireframes first.
 */

export const checkInTheme = {
  colors: {
    // Primary Colors
    burgundy: '#880124',
    burgundyDark: '#660119',
    burgundyLight: '#AA0130',

    plum: '#614B79',
    plumDark: '#4A3A5C',
    plumLight: '#7B6296',

    // Warm Metallics
    roseGold: '#B76D75',
    copper: '#B87333',
    brass: '#C9A961',

    // CTA Accent Colors
    electric: '#9D4EDD',
    electricDark: '#7B2CBF',
    amber: '#FFBF00',
    amberDark: '#E6AC00',
    amberLight: '#FFD633',

    // Neutral Colors
    midnight: '#1A1A2E',
    charcoal: '#2B2B2B',
    grayDark: '#2D2D2D',
    gray: '#666666',
    grayLight: '#CCCCCC',
    stone: '#8B8680',
    taupe: '#B8B0A8',
    ivory: '#FFF8F0',
    cream: '#FAF6F2',
    offWhite: '#F5F5F5',

    // Supporting Tones
    dustyRose: '#D4A5A5',

    // Status Colors
    success: '#228B22',
    warning: '#DAA520',
    error: '#DC143C',
    info: '#614B79',

    successLight: '#E6F7F1',
    warningLight: '#FFF4CC',
    errorLight: '#FFE5EC',
    infoLight: '#F0EDFF',
  },

  fonts: {
    display: "'Bodoni Moda', serif",
    heading: "'Montserrat', sans-serif",
    body: "'Source Sans 3', sans-serif",
    accent: "'Satisfy', cursive",
  },

  spacing: {
    xs: '8px',
    sm: '16px',
    md: '24px',
    lg: '32px',
    xl: '40px',
    '2xl': '48px',
    '3xl': '64px',
  },

  gradients: {
    header: 'linear-gradient(135deg, #880124 0%, #614B79 100%)',
    amber: 'linear-gradient(135deg, #FFBF00 0%, #E6AC00 100%)',
    memberBadge: 'linear-gradient(135deg, #B76D75 0%, #B87333 100%)',
  },
} as const

export type CheckInTheme = typeof checkInTheme
