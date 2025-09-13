import React from 'react';
import { Button, ButtonProps } from '@mantine/core';

export interface WCRButtonProps extends Omit<ButtonProps, 'variant' | 'size'> {
  variant?: 'primary' | 'secondary' | 'outline' | 'compact' | 'danger';
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'compact-xs' | 'compact-sm';
  // Explicitly include the important button props that we need
  onClick?: React.MouseEventHandler<HTMLButtonElement>;
  type?: 'button' | 'submit' | 'reset';
  disabled?: boolean;
  loading?: boolean;
  title?: string;
}

/**
 * WitchCityRope standard button component that prevents text cutoff issues
 * 
 * Key fixes applied:
 * - Uses padding instead of fixed heights for proper text display
 * - Proper line-height relative to font size
 * - Ensures text alignment and overflow handling
 * - Consistent styling across the application
 */
export const WCRButton: React.FC<WCRButtonProps> = ({
  variant = 'primary',
  size = 'md',
  children,
  styles,
  ...props
}) => {
  // Base styles that prevent text cutoff
  const baseButtonStyles = {
    root: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      lineHeight: '1.2', // Relative line height prevents cutoff
      fontWeight: 600,
      // Remove any explicit heights that cause cutoff
      height: 'auto',
      minHeight: 'auto',
    }
  };

  // Size-specific styles with padding instead of fixed heights
  const sizeStyles = {
    'compact-xs': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '12px',
        paddingTop: '4px',
        paddingBottom: '4px',
        paddingLeft: '8px',
        paddingRight: '8px',
        minWidth: '50px',
      }
    },
    'compact-sm': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '13px',
        paddingTop: '6px',
        paddingBottom: '6px',
        paddingLeft: '12px',
        paddingRight: '12px',
        minWidth: '60px',
      }
    },
    'xs': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '12px',
        paddingTop: '6px',
        paddingBottom: '6px',
        paddingLeft: '12px',
        paddingRight: '12px',
        minWidth: '70px',
      }
    },
    'sm': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '14px',
        paddingTop: '8px',
        paddingBottom: '8px',
        paddingLeft: '16px',
        paddingRight: '16px',
        minWidth: '90px',
      }
    },
    'md': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '16px',
        paddingTop: '10px',
        paddingBottom: '10px',
        paddingLeft: '20px',
        paddingRight: '20px',
        minWidth: '120px',
      }
    },
    'lg': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '18px',
        paddingTop: '12px',
        paddingBottom: '12px',
        paddingLeft: '24px',
        paddingRight: '24px',
        minWidth: '140px',
      }
    },
    'xl': {
      root: {
        ...baseButtonStyles.root,
        fontSize: '20px',
        paddingTop: '14px',
        paddingBottom: '14px',
        paddingLeft: '28px',
        paddingRight: '28px',
        minWidth: '160px',
      }
    }
  };

  // Variant-specific styles
  const variantStyles = {
    primary: {
      root: {
        backgroundColor: 'var(--mantine-color-wcr-7)',
        color: 'white',
        border: 'none',
        '&:hover': {
          backgroundColor: 'var(--mantine-color-wcr-8)',
        }
      }
    },
    secondary: {
      root: {
        background: 'linear-gradient(135deg, var(--mantine-color-amber-6), #DAA520)',
        color: 'var(--mantine-color-dark-9)',
        border: 'none',
        borderRadius: '12px 6px 12px 6px',
      }
    },
    outline: {
      root: {
        backgroundColor: 'transparent',
        color: 'var(--mantine-color-wcr-7)',
        border: '2px solid var(--mantine-color-wcr-7)',
        '&:hover': {
          backgroundColor: 'var(--mantine-color-wcr-0)',
        }
      }
    },
    compact: {
      root: {
        backgroundColor: 'var(--mantine-color-wcr-7)',
        color: 'white',
        border: 'none',
        fontSize: '14px',
        paddingTop: '6px',
        paddingBottom: '6px',
        paddingLeft: '12px',
        paddingRight: '12px',
        minWidth: '60px',
      }
    },
    danger: {
      root: {
        backgroundColor: 'var(--mantine-color-red-6)',
        color: 'white',
        border: 'none',
        '&:hover': {
          backgroundColor: 'var(--mantine-color-red-7)',
        }
      }
    }
  };

  // Merge all styles - handle function styles properly
  const mergedStyles = (theme: any, props: any, ctx: any) => ({
    root: {
      ...(sizeStyles[size]?.root || sizeStyles.md.root),
      ...(variantStyles[variant]?.root || variantStyles.primary.root),
      ...(typeof styles === 'function' ? styles(theme, props, ctx)?.root : styles?.root || {}),
    }
  });

  return (
    <Button
      {...props}
      styles={mergedStyles}
    >
      {children}
    </Button>
  );
};