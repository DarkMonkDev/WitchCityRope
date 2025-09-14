// Sliding Scale Calculation Hook
// Manages sliding scale pricing logic and calculations

import { useState, useCallback, useMemo } from 'react';
import { paymentUtils } from '../api/paymentApi';
import type { SlidingScaleCalculation } from '../types/payment.types';

/**
 * Hook for managing sliding scale pricing calculations
 * @param basePrice Original price before sliding scale
 * @param initialPercentage Initial discount percentage (default: 0)
 * @returns Sliding scale state and controls
 */
export const useSlidingScale = (basePrice: number, initialPercentage: number = 0) => {
  // State for current discount percentage
  const [discountPercentage, setDiscountPercentage] = useState(initialPercentage);
  
  // State for whether sliding scale is enabled
  const [isEnabled, setIsEnabled] = useState(initialPercentage > 0);

  /**
   * Calculate current sliding scale values
   */
  const calculation = useMemo((): SlidingScaleCalculation => {
    const effectivePercentage = isEnabled ? discountPercentage : 0;
    return paymentUtils.calculateSlidingScale(basePrice, effectivePercentage);
  }, [basePrice, discountPercentage, isEnabled]);

  /**
   * Update discount percentage with validation
   */
  const updateDiscountPercentage = useCallback((newPercentage: number) => {
    if (paymentUtils.isValidSlidingScalePercentage(newPercentage)) {
      setDiscountPercentage(newPercentage);
    }
  }, []);

  /**
   * Enable sliding scale with specific percentage
   */
  const enableSlidingScale = useCallback((percentage: number = 25) => {
    setIsEnabled(true);
    updateDiscountPercentage(percentage);
  }, [updateDiscountPercentage]);

  /**
   * Disable sliding scale (revert to full price)
   */
  const disableSlidingScale = useCallback(() => {
    setIsEnabled(false);
    setDiscountPercentage(0);
  }, []);

  /**
   * Toggle between full price and sliding scale
   */
  const toggleSlidingScale = useCallback(() => {
    if (isEnabled) {
      disableSlidingScale();
    } else {
      enableSlidingScale();
    }
  }, [isEnabled, enableSlidingScale, disableSlidingScale]);

  /**
   * Reset to initial state
   */
  const reset = useCallback(() => {
    setDiscountPercentage(initialPercentage);
    setIsEnabled(initialPercentage > 0);
  }, [initialPercentage]);

  /**
   * Get preset discount options
   */
  const presetOptions = useMemo(() => [
    { label: 'Full Price', percentage: 0, description: 'Support our community at standard rates' },
    { label: '25% Sliding Scale', percentage: 25, description: '25% community discount' },
    { label: '50% Sliding Scale', percentage: 50, description: '50% community discount' },
    { label: '75% Sliding Scale', percentage: 75, description: 'Maximum community discount' }
  ], []);

  /**
   * Get slider marks for UI display
   */
  const sliderMarks = useMemo(() => [
    { value: 0, label: paymentUtils.formatCurrency(basePrice) },
    { value: 25, label: paymentUtils.formatCurrency(basePrice * 0.75) },
    { value: 50, label: paymentUtils.formatCurrency(basePrice * 0.5) },
    { value: 75, label: paymentUtils.formatCurrency(basePrice * 0.25) }
  ], [basePrice]);

  /**
   * Get community message based on current selection
   */
  const getCommunityMessage = useCallback(() => {
    if (!isEnabled) {
      return "Thank you for supporting our community at full price!";
    }

    if (discountPercentage <= 25) {
      return "Every contribution helps make our events accessible to everyone.";
    }

    if (discountPercentage <= 50) {
      return "Our community thrives when everyone can participate.";
    }

    return "Choose what works for your situation - no questions asked.";
  }, [isEnabled, discountPercentage]);

  return {
    // Current state
    isEnabled,
    discountPercentage,
    calculation,
    
    // Actions
    updateDiscountPercentage,
    enableSlidingScale,
    disableSlidingScale,
    toggleSlidingScale,
    reset,
    
    // UI helpers
    presetOptions,
    sliderMarks,
    getCommunityMessage,
    
    // Computed values
    finalAmount: calculation.finalAmount,
    discountAmount: calculation.discountAmount,
    displayValues: calculation.display
  };
};