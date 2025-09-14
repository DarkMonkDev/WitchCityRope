// Payment System Feature Exports
// Central export file for the payments feature

// Components
export { SlidingScaleSelector, CompactSlidingScaleSelector } from './components/SlidingScaleSelector';
export { PaymentForm } from './components/PaymentForm';
export { PayPalButton } from './components/PayPalButton';
export { PaymentConfirmation } from './components/PaymentConfirmation';
export { PaymentSummary, CompactPaymentSummary } from './components/PaymentSummary';

// Pages
export { EventPaymentPage } from './pages/EventPaymentPage';

// Hooks
export { useSlidingScale } from './hooks/useSlidingScale';
export { usePayment, usePaymentHistory, useEventPayments } from './hooks/usePayment';

// API
export { paymentApi, paymentUtils } from './api/paymentApi';

// Types
export type {
  PaymentStatus,
  PaymentMethodType,
  RefundStatus,
  ProcessPaymentRequest,
  ProcessRefundRequest,
  PaymentResponse,
  RefundResponse,
  PaymentStatusResponse,
  SlidingScaleCalculation,
  PaymentFormData,
  PaymentEventInfo,
  PaymentProcessingState,
  PaymentError,
  SavedPaymentMethod
} from './types/payment.types';

// Enums
export { 
  PaymentStatus as PaymentStatusEnum,
  PaymentMethodType as PaymentMethodTypeEnum,
  RefundStatus as RefundStatusEnum 
} from './types/payment.types';