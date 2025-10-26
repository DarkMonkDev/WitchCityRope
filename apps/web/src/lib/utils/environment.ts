/**
 * Environment detection utilities for WitchCityRope
 * Used for enabling developer tools like test card auto-fill in non-production environments
 */

/**
 * Checks if the current environment is development or staging (non-production)
 * Used for enabling developer tools like test card auto-fill
 *
 * @returns true if environment is development or staging, false otherwise
 */
export const isNonProduction = (): boolean => {
  const env = import.meta.env.MODE || 'production';
  return env === 'development' || env === 'staging';
};

/**
 * Checks if the current environment is production
 *
 * @returns true if environment is production, false otherwise
 */
export const isProduction = (): boolean => {
  return !isNonProduction();
};

/**
 * Gets PayPal sandbox test card data for development/staging testing
 *
 * Source: https://developer.paypal.com/tools/sandbox/card-testing/
 *
 * Test Card Details:
 * - Card Number: 4111111111111111 (Visa - passes Luhn algorithm)
 * - Card Type: Visa
 * - Expiration: Any future date (using 12/26)
 * - CVV: Any 3-digit number (using 123)
 * - ZIP: Any 5-digit number (using 02101 for Salem, MA area)
 *
 * @returns Object containing PayPal sandbox test card data
 */
export const getPayPalTestCard = () => ({
  cardNumber: '4111111111111111', // Visa test card
  cardholderName: 'Test User',
  expiryDate: '12/26', // Future expiration
  cvv: '123',
  billingZip: '02101' // Boston ZIP (WitchCityRope is in Salem, MA)
});
