// Payment Utility Functions
// Extracted from paymentApi.ts - these are used by active components

/**
 * Utility functions for payment calculations
 */
export const paymentUtils = {
  /**
   * Calculate sliding scale amount
   * @param originalAmount Original price
   * @param discountPercentage Discount percentage (0-75)
   * @returns Final amount and discount info
   */
  calculateSlidingScale(originalAmount: number, discountPercentage: number) {
    // Ensure discount percentage is within valid range
    const validPercentage = Math.max(0, Math.min(75, discountPercentage));

    const discountAmount = originalAmount * (validPercentage / 100);
    const finalAmount = originalAmount - discountAmount;

    return {
      originalAmount,
      discountPercentage: validPercentage,
      discountAmount,
      finalAmount,
      display: {
        original: this.formatCurrency(originalAmount),
        final: this.formatCurrency(finalAmount),
        discount: this.formatCurrency(discountAmount),
        percentage: `${Math.round(validPercentage)}%`
      }
    };
  },

  /**
   * Format amount as currency string
   * @param amount Amount to format
   * @param currency Currency code (default: USD)
   * @returns Formatted currency string
   */
  formatCurrency(amount: number, currency: string = 'USD'): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  },

  /**
   * Validate payment amount for PayPal (must be between $0.01 and $10,000)
   * @param amount Amount in dollars
   * @returns Whether amount is valid for PayPal
   */
  isValidPayPalAmount(amount: number): boolean {
    return amount >= 0.01 && amount <= 10000;
  },

  /**
   * Round amount to PayPal-compatible precision (2 decimal places)
   * @param amount Amount to round
   * @returns Rounded amount
   */
  roundToPayPalPrecision(amount: number): number {
    return Math.round(amount * 100) / 100;
  },

  /**
   * Validate sliding scale percentage
   * @param percentage Percentage to validate
   * @returns Whether percentage is valid
   */
  isValidSlidingScalePercentage(percentage: number): boolean {
    return percentage >= 0 && percentage <= 75;
  },

  /**
   * Get payment status display properties
   * @param status Payment status
   * @returns Display properties for status
   */
  getStatusDisplay(status: string) {
    switch (status.toLowerCase()) {
      case 'pending':
        return {
          color: 'yellow',
          label: 'Processing',
          icon: 'clock'
        };
      case 'completed':
        return {
          color: 'green',
          label: 'Completed',
          icon: 'check'
        };
      case 'failed':
        return {
          color: 'red',
          label: 'Failed',
          icon: 'x'
        };
      case 'refunded':
        return {
          color: 'blue',
          label: 'Refunded',
          icon: 'arrow-back'
        };
      case 'partiallyrefunded':
        return {
          color: 'orange',
          label: 'Partially Refunded',
          icon: 'arrow-back'
        };
      default:
        return {
          color: 'gray',
          label: 'Unknown',
          icon: 'question'
        };
    }
  }
};
