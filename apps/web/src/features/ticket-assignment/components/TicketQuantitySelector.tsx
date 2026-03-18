/**
 * TicketQuantitySelector
 *
 * +/- quantity control for multi-ticket purchases.
 * Renders inside the existing ticket card Paper component.
 *
 * Design reference: ui-design.md Screen 2 - Quantity Selector Component Design
 * - [-] and [+] are ActionIcon components, variant="light", color="burgundy", 36px touch-friendly
 * - Quantity displayed between them, fw={700}, size="lg"
 * - Min: 1 (always buying at least one for yourself)
 * - Max: ticketType.maxQuantityPerPurchase (default 3, from API)
 */

import React from 'react';
import { Group, ActionIcon, Text } from '@mantine/core';
import { IconMinus, IconPlus } from '@tabler/icons-react';

interface TicketQuantitySelectorProps {
  /** Current quantity */
  quantity: number;
  /** Minimum quantity (default 1) */
  min?: number;
  /** Maximum quantity (from ticketType.maxQuantityPerPurchase) */
  max: number;
  /** Callback when quantity changes */
  onChange: (quantity: number) => void;
  /** Whether the selector is disabled */
  disabled?: boolean;
}

export const TicketQuantitySelector: React.FC<TicketQuantitySelectorProps> = ({
  quantity,
  min = 1,
  max,
  onChange,
  disabled = false,
}) => {
  const handleDecrement = () => {
    if (quantity > min) {
      onChange(quantity - 1);
    }
  };

  const handleIncrement = () => {
    if (quantity < max) {
      onChange(quantity + 1);
    }
  };

  return (
    <Group gap="xs" align="center" mt="xs">
      <Text size="sm" fw={500} c="dimmed">
        Quantity:
      </Text>
      <ActionIcon
        variant="light"
        color="#880124"
        size={36}
        onClick={handleDecrement}
        disabled={disabled || quantity <= min}
        aria-label="Decrease quantity"
        style={{ minWidth: 36, minHeight: 36 }}
      >
        <IconMinus size={16} />
      </ActionIcon>

      <Text
        fw={700}
        size="lg"
        ta="center"
        style={{ minWidth: 28 }}
        aria-label={`Quantity: ${quantity}`}
      >
        {quantity}
      </Text>

      <ActionIcon
        variant="light"
        color="#880124"
        size={36}
        onClick={handleIncrement}
        disabled={disabled || quantity >= max}
        aria-label="Increase quantity"
        style={{ minWidth: 36, minHeight: 36 }}
      >
        <IconPlus size={16} />
      </ActionIcon>
    </Group>
  );
};

TicketQuantitySelector.displayName = 'TicketQuantitySelector';
