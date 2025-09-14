// Payment System TypeScript Types
// Matching backend DTOs for type safety and consistency

/**
 * Payment processing status enumeration
 * Matches: WitchCityRope.Api.Features.Payments.Models.PaymentStatus
 */
export enum PaymentStatus {
  Pending = 0,
  Completed = 1,
  Failed = 2,
  Refunded = 3,
  PartiallyRefunded = 4
}

/**
 * Payment method type enumeration
 * Matches: WitchCityRope.Api.Features.Payments.Models.PaymentMethodType
 */
export enum PaymentMethodType {
  SavedCard = 0,
  NewCard = 1,
  BankTransfer = 2,
  PayPal = 3,
  Venmo = 4
}

/**
 * Refund processing status enumeration
 * Matches: WitchCityRope.Api.Features.Payments.Models.RefundStatus
 */
export enum RefundStatus {
  Pending = 0,
  Completed = 1,
  Failed = 2,
  Cancelled = 3
}

/**
 * API request model for processing payments with sliding scale pricing
 * Matches: WitchCityRope.Api.Features.Payments.Models.Requests.ProcessPaymentApiRequest
 */
export interface ProcessPaymentRequest {
  /** Event registration ID for the payment */
  eventRegistrationId: string;
  /** Original event fee amount (before sliding scale discount) */
  originalAmount: number;
  /** Currency code (default: USD) */
  currency: string;
  /** Sliding scale discount percentage (0-75%) - Honor-based system */
  slidingScalePercentage: number;
  /** Type of payment method being used */
  paymentMethodType: PaymentMethodType;
  /** URL to return to after successful payment */
  returnUrl: string;
  /** URL to return to if payment is cancelled */
  cancelUrl: string;
  /** Saved payment method ID (if using saved payment method) */
  savedPaymentMethodId?: string;
  /** Whether to save the payment method for future use */
  savePaymentMethod: boolean;
  /** Additional metadata for the payment */
  metadata?: Record<string, any>;
}

/**
 * API request model for processing refunds
 * Matches: WitchCityRope.Api.Features.Payments.Models.Requests.ProcessRefundApiRequest
 */
export interface ProcessRefundRequest {
  /** Payment ID to refund */
  paymentId: string;
  /** Refund amount (can be partial) */
  refundAmount: number;
  /** Currency for the refund (should match original payment) */
  currency: string;
  /** Reason for the refund (minimum 10 characters required) */
  refundReason: string;
  /** Additional metadata for the refund */
  metadata: Record<string, unknown>;
}

/**
 * Refund information for payment response
 * Matches: WitchCityRope.Api.Features.Payments.Models.Responses.RefundInfoResponse
 */
export interface RefundInfoResponse {
  /** Total amount refunded */
  refundedAmount: number;
  /** Currency of the refund */
  currency: string;
  /** Formatted refund amount for display */
  displayAmount: string;
  /** When the refund was processed */
  refundedAt?: string;
  /** Reason for the refund */
  refundReason?: string;
  /** Number of refund transactions */
  refundCount: number;
  /** Whether this is a partial or full refund */
  isPartialRefund: boolean;
}

/**
 * API response model for payment information
 * Matches: WitchCityRope.Api.Features.Payments.Models.Responses.PaymentResponse
 */
export interface PaymentResponse {
  /** Payment unique identifier */
  id: string;
  /** Event registration ID this payment is for */
  eventRegistrationId: string;
  /** User who made the payment */
  userId: string;
  /** Final payment amount (after sliding scale discount) */
  amount: number;
  /** Currency code */
  currency: string;
  /** Formatted amount for display (e.g., "$25.00") */
  displayAmount: string;
  /** Original amount before sliding scale discount */
  originalAmount?: number;
  /** Sliding scale discount percentage applied */
  slidingScalePercentage: number;
  /** Discount amount saved through sliding scale */
  discountAmount?: number;
  /** Current payment status */
  status: PaymentStatus;
  /** Human-readable status description */
  statusDescription: string;
  /** Type of payment method used */
  paymentMethodType: PaymentMethodType;
  /** When the payment was successfully processed */
  processedAt?: string;
  /** When the payment record was created */
  createdAt: string;
  /** Refund information (if payment has been refunded) */
  refundInfo?: RefundInfoResponse;
  /** PayPal approval URL for redirecting user to complete payment */
  payPalApprovalUrl?: string;
}

/**
 * API response model for refund information
 * Matches: WitchCityRope.Api.Features.Payments.Models.Responses.RefundResponse
 */
export interface RefundResponse {
  /** Refund unique identifier */
  id: string;
  /** Original payment ID that was refunded */
  originalPaymentId: string;
  /** Refund amount */
  refundAmount: number;
  /** Currency code */
  currency: string;
  /** Formatted refund amount for display */
  displayAmount: string;
  /** Reason for the refund */
  refundReason: string;
  /** Current refund processing status */
  refundStatus: RefundStatus;
  /** Human-readable status description */
  statusDescription: string;
  /** User who processed the refund */
  processedByUserId: string;
  /** Name/scene name of user who processed refund */
  processedByUserName: string;
  /** When the refund was processed */
  processedAt: string;
  /** When the refund record was created */
  createdAt: string;
}

/**
 * Payment status response for quick status checks
 * Matches: WitchCityRope.Api.Features.Payments.Models.Responses.PaymentStatusResponse
 */
export interface PaymentStatusResponse {
  /** Payment ID */
  paymentId: string;
  /** Event registration ID */
  eventRegistrationId: string;
  /** Current payment status */
  status: PaymentStatus;
  /** Human-readable status description */
  statusDescription: string;
  /** Whether payment is completed */
  isCompleted: boolean;
  /** Whether payment has been refunded */
  isRefunded: boolean;
  /** Final payment amount */
  amount: number;
  /** Currency code */
  currency: string;
  /** When payment was completed (if applicable) */
  processedAt?: string;
}

// Frontend-specific types for UI components

/**
 * Sliding scale calculation result
 */
export interface SlidingScaleCalculation {
  /** Original amount before discount */
  originalAmount: number;
  /** Discount percentage (0-75%) */
  discountPercentage: number;
  /** Final amount after discount */
  finalAmount: number;
  /** Amount saved through sliding scale */
  discountAmount: number;
  /** Formatted display strings */
  display: {
    original: string;
    final: string;
    discount: string;
    percentage: string;
  };
}

/**
 * Payment form data for UI
 */
export interface PaymentFormData {
  /** Cardholder name */
  cardholderName: string;
  /** Whether to save payment method */
  savePaymentMethod: boolean;
  /** Sliding scale percentage selected */
  slidingScalePercentage: number;
  /** Payment method type selection */
  paymentMethodType: PaymentMethodType;
  /** Selected saved payment method ID */
  savedPaymentMethodId?: string;
}

/**
 * Event information for payment flow
 */
export interface PaymentEventInfo {
  /** Event ID */
  id: string;
  /** Event title */
  title: string;
  /** Event start date/time */
  startDateTime: string;
  /** Event end date/time */
  endDateTime: string;
  /** Instructor/teacher name */
  instructorName?: string;
  /** Event location */
  location?: string;
  /** Base price before sliding scale */
  basePrice: number;
  /** Currency code */
  currency: string;
  /** Registration ID for this payment */
  registrationId: string;
}

/**
 * Payment processing state for UI
 */
export interface PaymentProcessingState {
  /** Current step in payment process */
  currentStep: 'selection' | 'payment' | 'processing' | 'confirmation' | 'error';
  /** Whether payment is being processed */
  isProcessing: boolean;
  /** Any error that occurred */
  error?: string;
  /** Success message */
  successMessage?: string;
  /** Payment result if successful */
  paymentResult?: PaymentResponse;
}

/**
 * PayPal-specific types for frontend integration
 */
export interface PayPalOrderRequest {
  /** Payment amount in dollars */
  amount: number;
  /** Currency code */
  currency: string;
  /** Description of the purchase */
  description?: string;
  /** Metadata for the payment */
  metadata?: Record<string, string>;
}

/**
 * Saved payment method information
 */
export interface SavedPaymentMethod {
  /** Payment method ID */
  id: string;
  /** Last 4 digits of card */
  last4: string;
  /** Card brand (visa, mastercard, etc.) */
  brand: string;
  /** Expiration month */
  expMonth: number;
  /** Expiration year */
  expYear: number;
  /** Whether this is the default payment method */
  isDefault: boolean;
  /** When the payment method was saved */
  createdAt: string;
}

/**
 * Payment error types
 */
export interface PaymentError {
  /** Error code */
  code: string;
  /** Human-readable error message */
  message: string;
  /** Error type (card_error, api_error, etc.) */
  type: string;
  /** Whether error is retryable */
  retryable: boolean;
  /** Suggested action for user */
  suggestedAction?: string;
}