// Payment System TypeScript Types
// Types used by active payment components

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
  status: number;
  /** Human-readable status description */
  statusDescription: string;
  /** Type of payment method used */
  paymentMethodType: number;
  /** When the payment was successfully processed */
  processedAt?: string;
  /** When the payment record was created */
  createdAt: string;
  /** Refund information (if payment has been refunded) */
  refundInfo?: RefundInfoResponse;
  /** PayPal approval URL for redirecting user to complete payment */
  payPalApprovalUrl?: string;
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
