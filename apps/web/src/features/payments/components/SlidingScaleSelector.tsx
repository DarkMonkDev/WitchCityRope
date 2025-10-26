// Sliding Scale Selector Component
// CRITICAL: Implements dignified sliding scale pricing interface

import React from 'react';
import {
  Box,
  Group,
  Stack,
  Text,
  Title,
  Alert,
  Radio,
  Slider,
  Paper,
  Badge
} from '@mantine/core';
import { IconHeart, IconShieldCheck } from '@tabler/icons-react';
import { useSlidingScale } from '../hooks/useSlidingScale';

interface SlidingScaleSelectorProps {
  /** Base price before sliding scale discount */
  basePrice: number;
  /** Currency code (default: USD) */
  currency?: string;
  /** Initial discount percentage */
  initialPercentage?: number;
  /** Callback when amount changes */
  onAmountChange?: (finalAmount: number, discountPercentage: number) => void;
  /** Whether the selector is disabled */
  disabled?: boolean;
  /** Custom title for the section */
  title?: string;
  /** Force sliding scale mode (skip Full Price option) - for sliding scale ticket types */
  forceSliding?: boolean;
  /** Minimum price for dollar-based sliding scale */
  minPrice?: number;
  /** Maximum price for dollar-based sliding scale */
  maxPrice?: number;
  /** Default/suggested price for dollar-based sliding scale */
  defaultPrice?: number;
}

/**
 * Dignified sliding scale pricing selector
 * Implements community values around economic inclusivity
 */
export const SlidingScaleSelector: React.FC<SlidingScaleSelectorProps> = ({
  basePrice,
  currency = 'USD',
  initialPercentage = 0,
  onAmountChange,
  disabled = false,
  title = 'Pricing Options',
  forceSliding = false,
  minPrice,
  maxPrice,
  defaultPrice
}) => {
  // Determine if we're using dollar-based sliding (have min/max/default prices)
  const isDollarMode = minPrice !== undefined && maxPrice !== undefined && defaultPrice !== undefined;

  // State for dollar amount (used in dollar mode)
  const [dollarAmount, setDollarAmount] = React.useState(defaultPrice || basePrice);

  const {
    isEnabled,
    discountPercentage,
    calculation,
    enableSlidingScale,
    disableSlidingScale,
    updateDiscountPercentage,
    sliderMarks,
    getCommunityMessage
  } = useSlidingScale(basePrice, initialPercentage);

  // Auto-enable sliding scale if forced
  React.useEffect(() => {
    if (forceSliding && !isEnabled) {
      enableSlidingScale(0); // Start at 0% discount (full min price)
    }
  }, [forceSliding, isEnabled, enableSlidingScale]);

  // Initialize dollar amount to default price
  React.useEffect(() => {
    if (isDollarMode && defaultPrice) {
      setDollarAmount(defaultPrice);
    }
  }, [isDollarMode, defaultPrice]);

  // Notify parent of changes
  React.useEffect(() => {
    if (isDollarMode) {
      // In dollar mode, calculate percentage based on selected dollar amount
      const percentageFromMax = maxPrice && minPrice
        ? ((maxPrice - dollarAmount) / (maxPrice - minPrice)) * 100
        : 0;
      onAmountChange?.(dollarAmount, percentageFromMax);
    } else {
      onAmountChange?.(calculation.finalAmount, calculation.discountPercentage);
    }
  }, [isDollarMode, dollarAmount, calculation.finalAmount, calculation.discountPercentage, onAmountChange, minPrice, maxPrice]);

  return (
    <Paper 
      radius="md" 
      p="lg"
      style={(theme) => ({
        backgroundColor: '#FAF6F2', // ivory from design system
        border: `2px solid #D4A5A5`, // dusty rose border
      })}
    >
      <Stack gap="md">
        {/* Section Title */}
        <Title order={3} c="#880124" size="h4">
          {title}
        </Title>

        {/* Price Range Display (for dollar mode) or Standard Price (for percentage mode) */}
        {isDollarMode ? (
          <Group justify="space-between" align="center">
            <Text size="lg" fw={500}>Price Range:</Text>
            <Text size="xl" fw={700} c="#880124">
              ${minPrice?.toFixed(2)} - ${maxPrice?.toFixed(2)}
            </Text>
          </Group>
        ) : (
          <Group justify="space-between" align="center">
            <Text size="lg" fw={500}>Standard Event Fee:</Text>
            <Text size="xl" fw={700} c="#880124">
              {calculation.display.original}
            </Text>
          </Group>
        )}

        {/* Pricing Options */}
        <Stack gap="sm">
          {/* Show radio buttons only if not forcing sliding scale */}
          {!forceSliding && (
            <Radio.Group
              value={isEnabled ? 'sliding' : 'full'}
              onChange={(value) => {
                if (value === 'full') {
                  disableSlidingScale();
                } else {
                  enableSlidingScale(25); // Default to 25% when enabling
                }
              }}
              size="md"
            >
              <Stack gap="md">
                <Radio
                  value="full"
                  label="Full Price"
                  description="Support our community at standard rates"
                  disabled={disabled}
                  color="wcr"
                  styles={{
                    label: { fontWeight: 600, fontSize: '16px' },
                    description: { fontSize: '14px', color: '#6B0119' }
                  }}
                />

                <Radio
                  value="sliding"
                  label="Sliding Scale"
                  description="Pay what works within your means"
                  disabled={disabled}
                  color="wcr"
                  styles={{
                    label: { fontWeight: 600, fontSize: '16px' },
                    description: { fontSize: '14px', color: '#6B0119' }
                  }}
                />
              </Stack>
            </Radio.Group>
          )}

          {/* Sliding Scale Interface - show when enabled OR when forced */}
          {(isEnabled || forceSliding) && (
            <Box
              mt="md"
              p="md"
              style={(theme) => ({
                backgroundColor: 'rgba(255, 255, 255, 0.7)',
                borderRadius: '12px',
                border: '1px solid #D4A5A5'
              })}
            >
              <Stack gap="md">
                {/* Slider */}
                <Box py="md">
                  <Text size="sm" fw={500} mb="xs" c="#880124">
                    Choose your amount:
                  </Text>
                  {isDollarMode ? (
                    // Dollar-based slider
                    <Slider
                      value={dollarAmount}
                      onChange={setDollarAmount}
                      min={minPrice}
                      max={maxPrice}
                      step={1}
                      marks={[
                        { value: minPrice!, label: `$${minPrice?.toFixed(0)}` },
                        { value: defaultPrice!, label: `$${defaultPrice?.toFixed(0)}` },
                        { value: maxPrice!, label: `$${maxPrice?.toFixed(0)}` }
                      ]}
                      disabled={disabled}
                      size="lg"
                      color="#880124"
                      styles={{
                        track: {
                          background: 'linear-gradient(to right, #FAF6F2, #D4A5A5, #880124)',
                          height: '8px'
                        },
                        thumb: {
                          backgroundColor: '#880124',
                          border: '3px solid #FAF6F2',
                          width: '24px',
                          height: '24px',
                          transition: 'all 0.3s ease'
                        },
                        mark: {
                          backgroundColor: '#880124',
                          border: '2px solid #FAF6F2'
                        },
                        markLabel: {
                          fontSize: '12px',
                          color: '#6B0119',
                          fontWeight: 600
                        }
                      }}
                    />
                  ) : (
                    // Percentage-based slider
                    <Slider
                      value={discountPercentage}
                      onChange={updateDiscountPercentage}
                      min={0}
                      max={75}
                      step={5}
                      marks={[
                        { value: 0, label: '0%' },
                        { value: 25, label: '25%' },
                        { value: 50, label: '50%' },
                        { value: 75, label: '75%' }
                      ]}
                      disabled={disabled}
                      size="lg"
                      color="#880124"
                      styles={{
                        track: {
                          background: 'linear-gradient(to right, #880124, #D4A5A5, #FAF6F2)',
                          height: '8px'
                        },
                        thumb: {
                          backgroundColor: '#880124',
                          border: '3px solid #FAF6F2',
                          width: '24px',
                          height: '24px',
                          transition: 'all 0.3s ease'
                        },
                        mark: {
                          backgroundColor: '#880124',
                          border: '2px solid #FAF6F2'
                        },
                        markLabel: {
                          fontSize: '12px',
                          color: '#6B0119',
                          fontWeight: 600
                        }
                      }}
                    />
                  )}
                </Box>

                {/* Amount Display */}
                <Group justify="space-between" align="center">
                  <Box>
                    {isDollarMode ? (
                      <>
                        <Text size="lg" fw={700} c="#880124">
                          Selected Amount: ${dollarAmount.toFixed(2)}
                        </Text>
                        {dollarAmount < (defaultPrice || 0) && (
                          <Text size="sm" c="#6B0119">
                            ${((defaultPrice || 0) - dollarAmount).toFixed(2)} below suggested price
                          </Text>
                        )}
                        {dollarAmount > (defaultPrice || 0) && (
                          <Text size="sm" c="green">
                            ${(dollarAmount - (defaultPrice || 0)).toFixed(2)} above suggested price - Thank you!
                          </Text>
                        )}
                      </>
                    ) : (
                      <>
                        <Text size="lg" fw={700} c="#880124">
                          Selected Amount: {calculation.display.final}
                        </Text>
                        {calculation.discountAmount > 0 && (
                          <Text size="sm" c="#6B0119">
                            Savings Applied: {calculation.display.discount} ({calculation.display.percentage} sliding scale)
                          </Text>
                        )}
                      </>
                    )}
                  </Box>

                  {!isDollarMode && calculation.discountAmount > 0 && (
                    <Badge
                      variant="light"
                      color="green"
                      size="lg"
                    >
                      {calculation.display.percentage} Off
                    </Badge>
                  )}
                  {isDollarMode && dollarAmount > (defaultPrice || 0) && (
                    <Badge
                      variant="light"
                      color="green"
                      size="lg"
                    >
                      Extra Support
                    </Badge>
                  )}
                </Group>
              </Stack>
            </Box>
          )}
        </Stack>

        {/* Community Message */}
        <Alert
          icon={<IconHeart />}
          color="wcr"
          variant="light"
          styles={{
            root: {
              backgroundColor: 'rgba(136, 1, 36, 0.05)',
              border: '1px solid rgba(136, 1, 36, 0.2)'
            },
            icon: {
              color: '#880124'
            },
            message: {
              color: '#6B0119',
              fontStyle: 'italic',
              fontSize: '15px'
            }
          }}
        >
          {isDollarMode ? (
            dollarAmount >= (defaultPrice || 0)
              ? "Thank you for supporting our community at or above the suggested price!"
              : dollarAmount >= (minPrice || 0) + ((defaultPrice || 0) - (minPrice || 0)) / 2
              ? "Every contribution helps make our events accessible to everyone."
              : "Choose what works for your situation - no questions asked."
          ) : (
            getCommunityMessage()
          )}
        </Alert>

        {/* Privacy Notice */}
        <Alert
          icon={<IconShieldCheck />}
          color="blue"
          variant="light"
          styles={{
            root: {
              backgroundColor: 'rgba(59, 130, 246, 0.05)'
            },
            message: {
              fontSize: '13px'
            }
          }}
        >
          <Stack gap={4}>
            <Text fw={500}>Privacy & Dignity Assured</Text>
            <Text size="xs">
              • All sliding scale usage is completely confidential<br />
              • No verification or documentation required<br />
              • Based on honor system and community trust<br />
              • Your choice is private and never shared
            </Text>
          </Stack>
        </Alert>
      </Stack>
    </Paper>
  );
};

/**
 * Compact version for mobile or space-constrained layouts
 */
export const CompactSlidingScaleSelector: React.FC<SlidingScaleSelectorProps> = ({
  basePrice,
  currency = 'USD',
  initialPercentage = 0,
  onAmountChange,
  disabled = false
}) => {
  const {
    isEnabled,
    discountPercentage,
    calculation,
    enableSlidingScale,
    disableSlidingScale,
    updateDiscountPercentage
  } = useSlidingScale(basePrice, initialPercentage);

  // Notify parent of changes
  React.useEffect(() => {
    onAmountChange?.(calculation.finalAmount, calculation.discountPercentage);
  }, [calculation.finalAmount, calculation.discountPercentage, onAmountChange]);

  return (
    <Paper radius="md" p="md" withBorder>
      <Stack gap="sm">
        {/* Price and Toggle */}
        <Group justify="space-between" align="center">
          <Text fw={500}>Event Fee:</Text>
          <Text size="lg" fw={700} c="#880124">
            {calculation.display.final}
          </Text>
        </Group>

        {/* Sliding Scale Toggle */}
        <Radio.Group
          value={isEnabled ? 'sliding' : 'full'}
          onChange={(value) => {
            if (value === 'full') {
              disableSlidingScale();
            } else {
              enableSlidingScale(25);
            }
          }}
        >
          <Group>
            <Radio value="full" label="Full Price" disabled={disabled} />
            <Radio value="sliding" label="Sliding Scale" disabled={disabled} />
          </Group>
        </Radio.Group>

        {/* Compact Slider */}
        {isEnabled && (
          <Box>
            <Slider
              value={discountPercentage}
              onChange={updateDiscountPercentage}
              min={0}
              max={75}
              step={5}
              disabled={disabled}
              color="#880124"
            />
            <Text size="xs" c="dimmed" ta="center" mt={4}>
              Choose what works for your situation
            </Text>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};