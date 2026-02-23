import { DatePickerInput, type DatePickerInputProps } from '@mantine/dates';
import { useMediaQuery } from '@mantine/hooks';
import { IconCalendar } from '@tabler/icons-react';

export interface StyledDatePickerProps extends Omit<DatePickerInputProps<'default'>, 'leftSection'> {
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

  // StyledDatePicker constrains to 'default' (single date) mode via its props interface.
  // However, DatePickerInput's generic type defaults to the wider DatePickerType union,
  // causing an onChange type mismatch. We cast rest props to resolve this Mantine generics issue.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const restProps = props as any;

  return (
    <DatePickerInput
      size={size || 'md'}
      firstDayOfWeek={0}
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
      {...restProps}
    />
  );
}
