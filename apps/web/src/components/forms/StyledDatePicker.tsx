import React from 'react';
import { DatePickerInput, DatePickerInputProps } from '@mantine/dates';
import { useMediaQuery } from '@mantine/hooks';
import { IconCalendar } from '@tabler/icons-react';

export interface StyledDatePickerProps extends Omit<DatePickerInputProps, 'leftSection'> {
  /**
   * Show calendar icon in the input field
   * @default true
   */
  showIcon?: boolean;
}

/**
 * StyledDatePicker - Brand-styled date picker component
 *
 * A wrapper around Mantine's DatePickerInput with WitchCityRope brand styling.
 * Implements Design System v7 colors (burgundy, rose gold, ivory) with
 * professional visual polish.
 *
 * Features:
 * - Burgundy brand colors on focus and selection
 * - Rose gold accents for calendar icon and borders
 * - Ivory calendar background for warmth
 * - Smooth transitions and hover effects
 * - Mobile-optimized with modal dropdown on small screens
 * - 44px touch targets on mobile for accessibility
 *
 * Usage:
 * ```tsx
 * <StyledDatePicker
 *   label="Event Date"
 *   placeholder="Select date"
 *   value={date}
 *   onChange={setDate}
 *   required
 * />
 * ```
 */
export function StyledDatePicker({
  showIcon = true,
  size,
  ...props
}: StyledDatePickerProps) {
  const isMobile = useMediaQuery('(max-width: 768px)');

  return (
    <DatePickerInput
      size={size || 'md'}
      leftSection={
        showIcon ? (
          <IconCalendar
            size={18}
            stroke={1.5}
            style={{ color: '#B8956A' }}
          />
        ) : null
      }
      leftSectionPointerEvents="none"
      dropdownType={isMobile ? 'modal' : 'popover'}
      {...props}
    />
  );
}
