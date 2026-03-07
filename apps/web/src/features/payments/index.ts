// Payment System Feature Exports
// Central export file for the payments feature

// Components
export { SlidingScaleSelector, CompactSlidingScaleSelector } from './components/SlidingScaleSelector';
export { PaymentForm } from './components/PaymentForm';
export { PayPalButton } from './components/PayPalButton';
export type { PayPalCheckoutResult } from './components/PayPalButton';
export { PaymentConfirmation } from './components/PaymentConfirmation';
export { PaymentSummary, CompactPaymentSummary } from './components/PaymentSummary';

// Pages
export { EventPaymentPage } from './pages/EventPaymentPage';

// Hooks
export { useSlidingScale } from './hooks/useSlidingScale';

// Utilities
export { paymentUtils } from './utils/paymentUtils';

// Types
export type {
  PaymentResponse,
  RefundInfoResponse,
  SlidingScaleCalculation,
  PaymentEventInfo,
} from './types/payment.types';
