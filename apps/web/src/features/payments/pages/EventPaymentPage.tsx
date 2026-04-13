// Event Payment Page
// Complete payment flow for event registration

import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, useSearchParams, useLocation } from 'react-router-dom';
import {
  Container,
  Stack,
  Group,
  Button,
  Stepper,
  Text,
  Alert,
  LoadingOverlay,
  Paper,
  Box,
  Checkbox,
  Loader,
  Divider
} from '@mantine/core';
import { useMediaQuery } from '@mantine/hooks';
import { IconArrowLeft, IconAlertCircle, IconCheck, IconGift } from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import type { components } from '@witchcityrope/shared-types/generated/api-types';
import { debugLog } from '../../../utils/debug';

import { SlidingScaleSelector } from '../components/SlidingScaleSelector';
import { PaymentForm } from '../components/PaymentForm';
import { PaymentConfirmation } from '../components/PaymentConfirmation';
import { PaymentSummary } from '../components/PaymentSummary';
import { useSlidingScale } from '../hooks/useSlidingScale';
import { useCheckout } from '../../../lib/api/hooks/usePayments';
import type { CheckoutResponse } from '../../../lib/api/services/payments';
import { eventsManagementService } from '../../../api/services/eventsManagement.service';
import { getTicketSessionInfo, getTicketSessionDetails } from '../../../utils/eventUtils';
import type { NonceData } from '../components/checkout/CreditCardForm';
import type { PayPalCheckoutResult } from '../components/PayPalButton';
import { useParticipation } from '../../../hooks/useParticipation';
import { useCurrentUser } from '../../../lib/api/hooks/useAuth';

import type { PaymentEventInfo } from '../types/payment.types';
import { TicketQuantitySelector } from '../../ticket-assignment/components/TicketQuantitySelector';
import { TicketAssignmentRow } from '../../ticket-assignment/components/TicketAssignmentRow';
import { usePrincipalContacts } from '../../authorized-contacts/api/queries';
import type { TicketSelectionItem } from '../../ticket-assignment/types/ticketAssignment.types';

// Use generated types from OpenAPI spec
type EventDto = components["schemas"]["EventDto"];
type TicketTypeDto = components["schemas"]["TicketTypeDto"];
type SessionDto = components["schemas"]["SessionDto"];

/**
 * Main event payment page with complete payment flow
 */
export const EventPaymentPage: React.FC = () => {
  const { eventId, registrationId: urlRegistrationId } = useParams<{
    eventId: string;
    registrationId: string;
  }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const isMobile = useMediaQuery('(max-width: 991px)');

  // Check if this is a "buy for others" flow (user already has a ticket and is buying for someone else)
  const buyForOthersOnly = (location.state as any)?.buyForOthersOnly === true;

  // Fetch user's participation status from API to determine which sessions they already own.
  // This replaces the old approach of relying on navigation state (location.state.ownedSessionIds)
  // which was stale on page refresh or direct navigation.
  // Uses the same useParticipation hook as EventDetailPage for consistent backend-driven filtering.
  const { data: currentUser } = useCurrentUser();
  const isAuthenticated = !!currentUser;
  const { data: participation, isLoading: participationLoading } = useParticipation(
    eventId || '', isAuthenticated, !!eventId
  );

  // Generate registration ID if not provided in URL
  const [registrationId] = useState(() =>
    urlRegistrationId || `reg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
  );

  // Payment flow state
  const [currentStep, setCurrentStep] = useState(0);
  const [eventInfo, setEventInfo] = useState<PaymentEventInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [completedPayment, setCompletedPayment] = useState<any | null>(null);
  const [ticketTypes, setTicketTypes] = useState<TicketTypeDto[]>([]);
  const [sessions, setSessions] = useState<SessionDto[]>([]);
  const [selectedTicketTypeIds, setSelectedTicketTypeIds] = useState<string[]>([]);
  // Track the price for each selected ticket (ticketId -> price)
  const [ticketPrices, setTicketPrices] = useState<Record<string, number>>({});
  const [checkoutErrorDismissed, setCheckoutErrorDismissed] = useState(false);

  // Multi-ticket quantity and assignment state
  // ticketQuantities: ticketTypeId -> quantity (default 1)
  const [ticketQuantities, setTicketQuantities] = useState<Record<string, number>>({});
  // ticketAssignments: ticketTypeId -> array of contactUserId|null (one per extra ticket)
  const [ticketAssignments, setTicketAssignments] = useState<Record<string, (string | null)[]>>({});

  // Sliding scale management
  const {
    discountPercentage,
    calculation,
    updateDiscountPercentage
  } = useSlidingScale(eventInfo?.basePrice || 0, 0);

  // Fetch authorized contacts (principals) for ticket assignment dropdowns
  // Only fetch when user is authenticated and we have an eventId
  const { data: principalContacts = [] } = usePrincipalContacts(
    isAuthenticated && eventId ? eventId : undefined
  );

  // Unified checkout mutation (ticket + payment in single request)
  const checkout = useCheckout();
  const [idempotencyKey, setIdempotencyKey] = useState(() =>
    `WCR-${crypto.randomUUID().replace(/-/g, '').substring(0, 32)}`
  );

  // Prevent navigation during checkout
  useEffect(() => {
    if (checkout.isPending) {
      const handler = (e: BeforeUnloadEvent) => {
        e.preventDefault();
        e.returnValue = 'Payment is being processed. Are you sure you want to leave?';
      };
      window.addEventListener('beforeunload', handler);
      return () => window.removeEventListener('beforeunload', handler);
    }
  }, [checkout.isPending]);

  /**
   * Load event information from API.
   * Waits for participation data to load so we can filter tickets using
   * the same backend-driven approach as EventDetailPage:
   * 1. canPurchase flag on each TicketTypeDto (timing windows, stock)
   * 2. ownedSessionIds from participation API (user-specific session ownership)
   */
  useEffect(() => {
    const loadEventInfo = async () => {
      try {
        setIsLoading(true);
        setError(null);

        if (!eventId) {
          setError('Event ID is required');
          setIsLoading(false);
          return;
        }

        debugLog('EventPaymentPage: Loading event details for eventId:', eventId);

        // Fetch real event data from API using generated types
        const eventDetails: EventDto = await eventsManagementService.getEventDetails(eventId);

        debugLog('EventPaymentPage: Received event details:', eventDetails);

        // Extract ticket types and sessions
        const eventTicketTypes = eventDetails?.ticketTypes || [];
        const eventSessions = eventDetails?.sessions || [];
        setSessions(eventSessions);

        // --- Backend-driven ticket filtering (same approach as EventDetailPage) ---
        // Uses ownedSessionIds from the participation API (fresh, not from stale nav state)
        // and canPurchase from each TicketTypeDto (backend-driven timing/stock checks).

        // Get owned session IDs from participation API response
        const ownedSessionIds: string[] = participation?.ownedSessionIds?.map(String) || [];

        // Build session code → GUID lookup (same as EventDetailPage lines 180-185)
        const sessionCodeToId: Record<string, string> = {};
        eventSessions.forEach((s: SessionDto) => {
          if (s.sessionIdentifier && s.id) {
            sessionCodeToId[s.sessionIdentifier] = s.id;
          }
        });

        // Check if a ticket type covers ANY session the user already owns
        // (same as EventDetailPage isTicketOwnedByUser, lines 188-195)
        const isTicketOwnedByUser = (tt: TicketTypeDto): boolean => {
          const codes: string[] = tt.sessionIdentifiers || [];
          if (codes.length === 0 || ownedSessionIds.length === 0) return false;
          return codes.some((code: string) => {
            const sessionId = sessionCodeToId[code];
            return sessionId != null && ownedSessionIds.includes(sessionId);
          });
        };

        // Filter tickets using the same logic as EventDetailPage (lines 200-206):
        // 1. Must be purchasable (canPurchase = true, backend-driven timing/stock)
        // 2. Must NOT cover any session the user already owns
        //    EXCEPTION: When buyForOthersOnly, skip the ownership check since the user
        //    is buying for someone else - their own sessions are irrelevant.
        const availableTicketTypes = eventTicketTypes.filter((tt: TicketTypeDto) =>
          tt.canPurchase && (buyForOthersOnly || !isTicketOwnedByUser(tt))
        );

        setTicketTypes(availableTicketTypes);

        debugLog('EventPaymentPage: Owned session IDs (from API):', ownedSessionIds);
        debugLog('EventPaymentPage: All ticket types:', eventTicketTypes.map((t: TicketTypeDto) => `${t.name} (canPurchase=${t.canPurchase})`));
        debugLog('EventPaymentPage: Filtered available tickets:', availableTicketTypes.map(t => t.name));

        // Auto-select ticket(s): if only one ticket, select it automatically; otherwise use URL param or empty
        let initialSelectedIds: string[] = [];
        const initialPrices: Record<string, number> = {};

        if (availableTicketTypes.length === 1) {
          // Only one ticket - auto-select it
          const ticket = availableTicketTypes[0];
          initialSelectedIds = [ticket.id || ''];
          // Set initial price based on ticket type
          const price = ticket.pricingType === 'Fixed'
            ? (ticket.price ?? 0)
            : (ticket.defaultPrice ?? ticket.minPrice ?? 0); // Use default price for sliding scale
          initialPrices[ticket.id || ''] = price;
        } else if (searchParams.get('ticketTypeId')) {
          // URL param provided - select that one (only if it's still available after filtering)
          const ticketId = searchParams.get('ticketTypeId')!;
          const ticket = availableTicketTypes.find(t => t.id === ticketId);
          if (ticket) {
            initialSelectedIds = [ticketId];
            const price = ticket.pricingType === 'Fixed'
              ? (ticket.price ?? 0)
              : (ticket.defaultPrice ?? ticket.minPrice ?? 0);
            initialPrices[ticketId] = price;
          }
        }

        setSelectedTicketTypeIds(initialSelectedIds);
        setTicketPrices(initialPrices);

        // When buyForOthersOnly, initialize assignment arrays for auto-selected tickets
        // even at quantity=1, since every ticket is for an assignee
        if (buyForOthersOnly && initialSelectedIds.length > 0) {
          const initialAssignments: Record<string, (string | null)[]> = {};
          initialSelectedIds.forEach(id => {
            initialAssignments[id] = [null]; // One assignee slot per ticket at quantity=1
          });
          setTicketAssignments(initialAssignments);
        }

        // Calculate base price from first selected ticket (or first available)
        const firstSelectedId = initialSelectedIds[0] || availableTicketTypes[0]?.id;
        const selectedTicket = availableTicketTypes.find((tt) => tt.id === firstSelectedId) || availableTicketTypes[0];
        // Use correct price field based on pricing type
        const basePrice = selectedTicket?.pricingType === 'Fixed'
          ? (selectedTicket?.price ?? 0)
          : (selectedTicket?.minPrice ?? 0);

        // Transform to PaymentEventInfo format
        const paymentEventInfo: PaymentEventInfo = {
          id: eventDetails?.id || '',
          title: eventDetails?.title || '',
          startDateTime: eventDetails?.startDate || new Date().toISOString(),
          endDateTime: eventDetails?.endDate || new Date().toISOString(),
          instructorName: 'Instructor TBD', // teacherIds is currently empty in API response
          location: '', // TODO: Fetch venue name using venueId once venue API is available
          basePrice,
          currency: 'USD',
          registrationId: registrationId
        };

        debugLog('EventPaymentPage: Transformed payment event info:', paymentEventInfo);
        debugLog('EventPaymentPage: Ticket types:', eventTicketTypes);
        debugLog('EventPaymentPage: Selected ticket type IDs:', initialSelectedIds);
        setEventInfo(paymentEventInfo);
      } catch (err: any) {
        console.error('EventPaymentPage: Error loading event info:', err);
        setError(err.message || 'Failed to load event information. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };

    // Wait for participation data before loading event info, so we can
    // properly filter tickets. For unauthenticated users, participationLoading
    // will be false immediately (query is disabled).
    if (!eventId) {
      setError('Missing event information');
      setIsLoading(false);
    } else if (!participationLoading) {
      loadEventInfo();
    }
  }, [eventId, registrationId, participation, participationLoading]);

  /**
   * Handle card nonce ready from CreditCardForm.
   * Sends a single unified checkout request (ticket + payment atomically).
   */
  const handleNonceReady = async (nonceData: NonceData) => {
    if (!eventId) return;

    const totalAmount = Object.values(ticketPrices).reduce((sum, price) => sum + price, 0);
    const ticketIds = selectedTickets.map(t => t.id).filter((id): id is string => !!id);

    if (ticketIds.length === 0) {
      notifications.show({
        title: 'No Tickets Selected',
        message: 'Please select at least one ticket.',
        color: 'red'
      });
      return;
    }

    // Build ticket selections for multi-ticket support
    const ticketSelections = buildTicketSelections();
    const hasMultiTicket = ticketSelections.some(ts => ts.quantity > 1);

    // Generate a fresh idempotency key for THIS attempt.
    // We build it into a LOCAL variable (not read from useState) so that a rapid
    // retry cannot inadvertently reuse the previous attempt's key while React
    // is still batching a pending setIdempotencyKey update. The state value is
    // still kept in sync (setIdempotencyKey below) so the PayPal flow — which
    // reads idempotencyKey via props into PayPalButton — sees an up-to-date key.
    //
    // History: 2026-04-12 incident 01-health-check-2026-04-12 section on Mr J.
    // A real user hit Authorize.NET "Invalid OTS Token" (E00114) on a 2-second
    // retry. Backend logs showed the SAME idempotency key on both attempts
    // ("Previous attempt with same key failed. Allowing retry."), even though
    // the prior version of this function DID call setIdempotencyKey in the
    // catch block. Root cause for the state staleness remains uncertain (see
    // tech-debt BE-14); this local-variable approach eliminates the bug class
    // regardless of the underlying cause.
    const requestIdempotencyKey = `WCR-${crypto.randomUUID().replace(/-/g, '').substring(0, 32)}`;
    setIdempotencyKey(requestIdempotencyKey);

    debugLog('Starting unified checkout:', { eventId, ticketIds, totalAmount, idempotencyKey: requestIdempotencyKey, ticketSelections, buyForOthersOnly });
    setCheckoutErrorDismissed(false);

    try {
      const result: CheckoutResponse = await checkout.mutateAsync({
        eventId,
        ticketTypeIds: ticketIds,
        eventWaiverAccepted: true, // UI enforces checkbox before payment
        nonce: nonceData.nonce,
        dataDescriptor: nonceData.dataDescriptor,
        amount: totalAmount,
        lastFourDigits: nonceData.lastFourDigits,
        cardType: nonceData.cardType,
        idempotencyKey: requestIdempotencyKey,
        // Multi-ticket support: include ticket selections when purchasing multiple
        ...(hasMultiTicket || buyForOthersOnly ? { ticketSelections } : {}),
        // Signal backend to skip purchaser overlap check
        ...(buyForOthersOnly ? { buyForOthersOnly: true } : {}),
      });

      debugLog('Checkout completed:', result);

      // Set confirmation data (PaymentConfirmation expects PaymentResponse shape)
      setCompletedPayment({
        id: result.transactionId || result.confirmationNumber,
        transactionId: result.transactionId,
        amount: result.amountCharged,
        currency: 'USD',
        status: 'completed',
        paymentMethod: 'credit_card',
        paymentMethodType: 1, // Credit Card
        confirmationNumber: result.confirmationNumber,
        cardLast4: nonceData.lastFourDigits,
        slidingScalePercentage: 0,
        createdAt: new Date().toISOString()
      });

      notifications.show({
        title: 'Ticket Purchased Successfully!',
        message: `Confirmation: ${result.confirmationNumber}`,
        color: 'green',
        autoClose: 8000
      });

      setCurrentStep(2);
    } catch {
      // Error display is handled inline on the page (see Step 2 render).
      // No need to regenerate idempotencyKey here — the next call to
      // handleNonceReady will generate a fresh one at the top of the function.
    }
  };

  /**
   * Handle successful PayPal checkout (ticket created + payment captured).
   * The unified PayPal checkout endpoint handles ticket creation, payment capture,
   * attendance activation, and email sending — mirroring the credit card flow.
   */
  const handlePayPalSuccess = async (result: PayPalCheckoutResult) => {
    debugLog('PayPal checkout success:', result);

    setCompletedPayment({
      id: result.captureId,
      transactionId: result.captureId,
      amount: parseFloat(result.amount),
      currency: result.currency,
      status: 'completed',
      paymentMethod: 'paypal',
      paymentMethodType: 2, // PayPal
      confirmationNumber: result.confirmationNumber,
      slidingScalePercentage: 0,
      createdAt: new Date().toISOString()
    });

    // Generate new idempotency key since this one was used
    setIdempotencyKey(`WCR-${crypto.randomUUID().replace(/-/g, '').substring(0, 32)}`);

    notifications.show({
      title: 'Ticket Purchased Successfully!',
      message: result.confirmationNumber
        ? `Confirmation: ${result.confirmationNumber}`
        : 'Your PayPal payment has been processed.',
      color: 'green',
      autoClose: 8000
    });

    setCurrentStep(2);
  };

  /**
   * Handle payment error
   */
  const handlePaymentError = (errorMessage: string) => {
    notifications.show({
      title: 'Payment Failed',
      message: errorMessage,
      color: 'red'
    });
  };

  /**
   * Go back to previous step
   */
  const handleBack = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
      setCompletedPayment(null); // Reset completed payment
    } else {
      navigate(-1);
    }
  };

  /**
   * Continue to next step
   */
  const handleContinue = () => {
    if (currentStep < 2) {
      setCurrentStep(currentStep + 1);
    }
  };

  /**
   * Navigate to user registrations
   */
  const handleViewRegistrations = () => {
    navigate('/dashboard');
  };

  /**
   * Navigate to browse more events
   */
  const handleRegisterMore = () => {
    navigate('/events');
  };

  /**
   * Handle quantity change for a ticket type
   */
  const handleQuantityChange = (ticketTypeId: string, newQuantity: number) => {
    setTicketQuantities(prev => ({
      ...prev,
      [ticketTypeId]: newQuantity,
    }));

    // Adjust the assignments array: keep existing assignments, trim or pad with null
    // When buyForOthersOnly, ALL tickets are for assignees (no purchaser ticket)
    setTicketAssignments(prev => {
      const existingAssignments = prev[ticketTypeId] || [];
      const extraTickets = buyForOthersOnly ? newQuantity : newQuantity - 1;

      if (extraTickets <= 0) {
        // No extra tickets, clear assignments
        const updated = { ...prev };
        delete updated[ticketTypeId];
        return updated;
      }

      // Trim or pad the array
      const newAssignments = existingAssignments.slice(0, extraTickets);
      while (newAssignments.length < extraTickets) {
        newAssignments.push(null);
      }

      return {
        ...prev,
        [ticketTypeId]: newAssignments,
      };
    });

    // Update ticket prices: quantity * per-unit price
    setTicketPrices(prev => {
      const ticket = ticketTypes.find(tt => tt.id === ticketTypeId);
      if (!ticket) return prev;

      // Get the per-unit price (current price / current quantity)
      const currentQty = ticketQuantities[ticketTypeId] || 1;
      const currentTotal = prev[ticketTypeId] || 0;
      const perUnitPrice = currentQty > 0 ? currentTotal / currentQty : 0;

      // If per-unit price is 0, use the ticket's default price
      const unitPrice = perUnitPrice > 0 ? perUnitPrice : (
        ticket.pricingType === 'Fixed'
          ? (ticket.price ?? 0)
          : (ticket.defaultPrice ?? ticket.minPrice ?? 0)
      );

      return {
        ...prev,
        [ticketTypeId]: unitPrice * newQuantity,
      };
    });
  };

  /**
   * Handle assignment change for a specific extra ticket
   */
  const handleAssignmentChange = (ticketTypeId: string, ticketIndex: number, contactUserId: string | null) => {
    setTicketAssignments(prev => {
      const existing = prev[ticketTypeId] || [];
      const updated = [...existing];
      updated[ticketIndex] = contactUserId;
      return {
        ...prev,
        [ticketTypeId]: updated,
      };
    });
  };

  /**
   * Build ticket selections for the checkout request.
   * When buyForOthersOnly, ALL assignments are included (no purchaser ticket skipped).
   */
  const buildTicketSelections = (): TicketSelectionItem[] => {
    return selectedTicketTypeIds.map(ticketTypeId => {
      const quantity = ticketQuantities[ticketTypeId] || 1;
      const assignments = ticketAssignments[ticketTypeId] || [];

      // When buyForOthersOnly, always include assignees since every ticket needs one
      const includeAssignees = buyForOthersOnly || quantity > 1;

      return {
        ticketTypeId,
        quantity,
        assignees: includeAssignees ? assignments : undefined,
      };
    });
  };

  /**
   * Get all selected tickets
   */
  const selectedTickets = ticketTypes.filter((tt) => selectedTicketTypeIds.includes(tt.id || ''));

  /**
   * Determine pricing display based on first selected ticket
   * If multiple tickets, we'll show a summary instead
   */
  const firstSelectedTicket = selectedTickets[0];

  // DEBUG: Log the ticket type to see what value it actually has
  debugLog('🔍 CHECKOUT DEBUG:');
  debugLog('  - Selected ticket IDs:', selectedTicketTypeIds);
  debugLog('  - Selected tickets:', selectedTickets);
  debugLog('  - First selected ticket:', firstSelectedTicket);
  debugLog('  - All ticket types:', ticketTypes);

  // Check if ANY selected ticket has sliding scale pricing
  const hasAnySlidingScaleTicket = selectedTickets.some(ticket => ticket.pricingType === 'SlidingScale');
  const firstSlidingTicket = selectedTickets.find(ticket => ticket.pricingType === 'SlidingScale');
  const isSingleTicketSelected = selectedTickets.length === 1;
  debugLog('  - hasAnySlidingScaleTicket:', hasAnySlidingScaleTicket);
  debugLog('  - firstSlidingTicket:', firstSlidingTicket);
  debugLog('  - isSingleTicketSelected:', isSingleTicketSelected);

  /**
   * Get ticket IDs that should be disabled due to session overlap
   */
  const getDisabledTicketIds = (selectedIds: string[]): Set<string> => {
    const disabledIds = new Set<string>();

    // Get all sessions covered by currently selected tickets
    const coveredSessionIds = new Set<string>();
    selectedIds.forEach(ticketId => {
      const ticket = ticketTypes.find(tt => tt.id === ticketId);
      ticket?.sessionIdentifiers?.forEach(sessionId => {
        coveredSessionIds.add(sessionId);
      });
    });

    // Find tickets that have overlapping sessions (disable them)
    ticketTypes.forEach(ticket => {
      if (selectedIds.includes(ticket.id || '')) return; // Already selected

      const hasOverlap = ticket.sessionIdentifiers?.some(
        sessionId => coveredSessionIds.has(sessionId)
      );

      if (hasOverlap) {
        disabledIds.add(ticket.id || '');
      }
    });

    return disabledIds;
  };

  /**
   * Handle ticket type checkbox toggle
   */
  const handleTicketTypeToggle = (ticketTypeId: string, checked: boolean) => {
    let newSelectedIds: string[];
    const newPrices = { ...ticketPrices };

    if (checked) {
      // Add ticket to selection
      newSelectedIds = [...selectedTicketTypeIds, ticketTypeId];

      // Set initial price for this ticket
      const ticket = ticketTypes.find(tt => tt.id === ticketTypeId);
      if (ticket) {
        const price = ticket.pricingType === 'Fixed'
          ? (ticket.price ?? 0)
          : (ticket.defaultPrice ?? ticket.minPrice ?? 0);
        newPrices[ticketTypeId] = price;
      }

      // When buyForOthersOnly, initialize assignment array for newly selected ticket
      if (buyForOthersOnly) {
        setTicketAssignments(prev => ({
          ...prev,
          [ticketTypeId]: [null], // One assignee slot at quantity=1
        }));
      }
    } else {
      // Remove ticket from selection
      newSelectedIds = selectedTicketTypeIds.filter(id => id !== ticketTypeId);
      // Remove price for this ticket
      delete newPrices[ticketTypeId];

      // Clean up assignments for deselected ticket
      if (buyForOthersOnly) {
        setTicketAssignments(prev => {
          const updated = { ...prev };
          delete updated[ticketTypeId];
          return updated;
        });
      }
    }

    setSelectedTicketTypeIds(newSelectedIds);
    setTicketPrices(newPrices);

    // Update event info with base price from first selected ticket
    if (newSelectedIds.length > 0) {
      const firstTicket = ticketTypes.find((tt) => tt.id === newSelectedIds[0]);
      if (firstTicket && eventInfo) {
        const newBasePrice = newPrices[newSelectedIds[0]] || 0;
        setEventInfo({
          ...eventInfo,
          basePrice: newBasePrice
        });
        updateDiscountPercentage(0); // Reset discount when ticket type changes
      }
    }
  };

  // Session info/details helpers — thin wrappers around shared utilities in eventUtils.ts
  // that bind the local `sessions` state so callers just pass a ticket.
  const getTicketSessions = (ticket: TicketTypeDto) =>
    getTicketSessionInfo(ticket.sessionIdentifiers || [], sessions);

  const getTicketSessionDetailsFull = (ticket: TicketTypeDto) =>
    getTicketSessionDetails(ticket.sessionIdentifiers || [], sessions);

  // Checkout error helper functions
  const getCheckoutErrorTitle = (error: any): string => {
    const data = error?.response?.data;
    if (data?.paymentCharged && data?.refundInitiated) return 'Payment Issue - Refund In Progress';
    if (data?.paymentCharged) return 'Payment Issue - Please Contact Us';
    if (data?.failureStage === 'payment') return 'Payment Declined';
    if (data?.failureStage === 'ticket_creation') return 'Unable to Process Order';
    if (data?.failureStage === 'validation') return 'Checkout Error';
    return 'Checkout Failed';
  };

  const isPaymentCharged = (error: any): boolean => {
    return error?.response?.data?.paymentCharged === true;
  };

  const getCorrelationId = (error: any): string | null => {
    return error?.response?.data?.checkoutCorrelationId || null;
  };

  const getCheckoutErrorMessage = (error: any): string => {
    return error?.response?.data?.detail || error?.message || 'An unexpected error occurred. Please try again.';
  };

  /**
   * Compute effective max quantity for a ticket type, considering:
   * 1. Per-transaction limit (maxQuantityPerPurchase from ticket type)
   * 2. Per-person remaining limit (remainingPerPerson from participation data)
   * 3. Event capacity remaining (capacity.available from participation data)
   *
   * Returns 0 when user cannot purchase (at per-person limit or event at capacity).
   * The TicketQuantitySelector should be disabled when this returns 0.
   */
  const getEffectiveMaxQuantity = (ticketType: TicketTypeDto): number => {
    const perPurchaseMax = ticketType.maxQuantityPerPurchase || 3;

    const remainingPerPerson = participation?.remainingPerPerson;
    const capacityAvailable = participation?.capacity?.available;

    let effectiveMax = perPurchaseMax;

    // Cap by per-person remaining (null/undefined = no limit, 0 = at limit)
    if (remainingPerPerson != null) {
      effectiveMax = Math.min(effectiveMax, remainingPerPerson);
    }

    // Cap by event capacity remaining
    if (capacityAvailable != null) {
      effectiveMax = Math.min(effectiveMax, capacityAvailable);
    }

    return Math.max(0, effectiveMax);
  };

  // Show loading state
  if (isLoading) {
    return (
      <Container size="md" py="xl">
        <LoadingOverlay visible />
        <Text ta="center" c="dimmed">
          Loading payment information...
        </Text>
      </Container>
    );
  }

  // Show error state
  if (error || !eventInfo) {
    return (
      <Container size="md" py="xl">
        <Alert
          icon={<IconAlertCircle />}
          title="Error"
          color="red"
        >
          {error || 'Failed to load payment information'}
        </Alert>
        <Group justify="center" mt="md">
          <Button variant="outline" onClick={() => navigate(-1)}>
            Go Back
          </Button>
        </Group>
      </Container>
    );
  }

  return (
    <Container size="lg" py="xl">
      <Stack gap="md">
        {/* Header - Back button and secure payment notice */}
        <Group justify="space-between" align="center" mt={0} mb={{ base: 0, md: 'xs' }}>
          {/* Hide Back button on confirmation screen (Step 3) */}
          {currentStep < 2 ? (
            <Button
              variant="subtle"
              leftSection={<IconArrowLeft size={16} />}
              onClick={handleBack}
              color="wcr"
            >
              Back
            </Button>
          ) : (
            <Box />
          )}
          <Text c="dimmed" size="sm">
            Secure Payment • SSL Encrypted
          </Text>
        </Group>

        {/* Mobile Step Indicator - Simple Text */}
        <Box hiddenFrom="md" mb={0}>
          <Text
            size="lg"
            fw={500}
            c="wcr.7"
            ta="center"
            className="checkout-step-indicator"
          >
            Step {currentStep + 1} of 3: {
              currentStep === 0
                ? (!hasAnySlidingScaleTicket ? "Ticket Selection" : "Pricing")
                : currentStep === 1
                  ? "Payment"
                  : "Confirmation"
            }
          </Text>
        </Box>

        {/* Desktop/Tablet Progress Stepper - Full Version */}
        <Box visibleFrom="md" mb="1rem">
          <Stepper
            active={currentStep}
            color={currentStep === 2 ? 'green' : '#880124'}
            iconSize={32}
            styles={{
              stepIcon: {
                borderWidth: 2
              }
            }}
          >
            <Stepper.Step
              label={!hasAnySlidingScaleTicket ? "Ticket Selection" : "Pricing"}
              description={!hasAnySlidingScaleTicket ? "Review ticket details" : "Choose your amount"}
            />
            <Stepper.Step
              label="Payment"
              description="Enter payment details"
            />
            <Stepper.Step
              label="Confirmation"
              description="Registration complete"
              icon={currentStep === 2 ? <IconCheck size={18} /> : undefined}
            />
          </Stepper>
        </Box>

        {/* Buy-for-Others Banner */}
        {buyForOthersOnly && (
          <Alert
            icon={<IconGift size={18} />}
            color="grape"
            variant="light"
            radius="md"
          >
            <Text size="sm" fw={500}>
              You're purchasing tickets for someone else. Select the ticket type and choose who to assign each ticket to.
            </Text>
          </Alert>
        )}

        {/* Step Content */}
        <Group align="flex-start" gap="xl">
          {/* Main Content */}
          <Stack gap="md" style={{ flex: 2 }}>
            {/* Step 1: Ticket Type and Pricing Selection */}
            {currentStep === 0 && (
              <>
                {/* No purchasable tickets message — shown when all tickets are
                    filtered out (user owns all sessions, timing windows closed, etc.) */}
                {ticketTypes.length === 0 && (
                  <Alert
                    icon={<IconAlertCircle />}
                    title="No Tickets Available"
                    color="blue"
                  >
                    <Text size="sm">
                      There are no tickets available for purchase right now. This may be because
                      you already have tickets for all available sessions, or the sales window
                      has closed.
                    </Text>
                    <Button
                      variant="outline"
                      color="blue"
                      mt="md"
                      onClick={() => navigate(`/events/${eventId}`)}
                    >
                      Back to Event Details
                    </Button>
                  </Alert>
                )}

                {/* Ticket Type Selection */}
                {ticketTypes.length > 0 && (
                  <Paper
                    p={{ base: 0, md: 'lg' }}
                    pt={{ base: 14, md: 'lg' }}
                    pb={{ base: 'sm', md: 'lg' }}
                    radius="md"
                    mb={{ base: 0, md: 'xs' }}
                    style={{ background: 'var(--mantine-color-gray-0)' }}
                  >
                    <Stack gap="sm">
                      <Text fw={600} size="lg" id="select-tickets-heading">
                        {ticketTypes.length === 1 ? 'Ticket' : 'Select Tickets'}
                      </Text>
                      <Stack gap="md">
                        {ticketTypes.map((tt) => {
                          // Format price display based on pricing type
                          let priceDisplay = '';
                          if (tt.pricingType === 'Fixed') {
                            const price = tt.price ?? 0;
                            priceDisplay = `$${price.toFixed(2)}`;
                          } else if (tt.pricingType === 'SlidingScale') {
                            const minPrice = tt.minPrice ?? 0;
                            const maxPrice = tt.maxPrice ?? 0;
                            priceDisplay = `$${minPrice.toFixed(2)} - $${maxPrice.toFixed(2)}`;
                          } else {
                            priceDisplay = 'Price TBD';
                          }

                          const isSelected = selectedTicketTypeIds.includes(tt.id || '');
                          const showCheckbox = ticketTypes.length > 1;

                          // Check if this ticket should be disabled due to session overlap
                          const disabledTicketIds = getDisabledTicketIds(selectedTicketTypeIds);
                          const isDisabledDueToOverlap = disabledTicketIds.has(tt.id || '');

                          return (
                            <Paper
                              key={tt.id || Math.random()}
                              p="md"
                              radius={isMobile ? 0 : undefined}
                              style={{
                                background: isSelected ? 'rgba(136, 1, 36, 0.05)' : 'white',
                                border: isSelected
                                  ? '2px solid var(--mantine-color-wcr-6)'
                                  : '1px solid var(--mantine-color-gray-3)',
                                cursor: showCheckbox && !isDisabledDueToOverlap ? 'pointer' : 'default',
                                opacity: isDisabledDueToOverlap ? 0.5 : 1,
                              }}
                              onClick={() => showCheckbox && !isDisabledDueToOverlap && handleTicketTypeToggle(tt.id ?? '', !isSelected)}
                            >
                              <Group gap="sm" wrap="nowrap">
                                {showCheckbox && (
                                  <Checkbox
                                    checked={isSelected}
                                    disabled={isDisabledDueToOverlap}
                                    onChange={(e) => {
                                      e.stopPropagation();
                                      handleTicketTypeToggle(tt.id ?? '', e.currentTarget.checked);
                                    }}
                                    color="wcr"
                                    style={{ alignSelf: 'center' }}
                                  />
                                )}
                                <Box style={{ flex: 1 }}>
                                  {/* Row 1: title + price */}
                                  <Group justify="space-between" wrap="nowrap">
                                    <Text fw={600} size="md">{tt.name}</Text>
                                    <Text fw={700} size="lg" c="#880124" style={{ whiteSpace: 'nowrap', flexShrink: 0 }}>
                                      {priceDisplay}
                                    </Text>
                                  </Group>
                                  {/* Row 2: session info */}
                                  {(() => {
                                    const sessionInfo = getTicketSessions(tt);
                                    if (sessionInfo.length > 0) {
                                      return (
                                        <Stack gap={2} mt={4}>
                                          {sessionInfo.map((session, idx) => (
                                            <Text key={idx} size="sm" c="dimmed">
                                              {session.name} - {session.date}
                                            </Text>
                                          ))}
                                        </Stack>
                                      );
                                    }
                                    return null;
                                  })()}
                                  {isDisabledDueToOverlap && (
                                    <Text size="xs" c="dimmed" mt={4}>
                                      Sessions overlap with selected ticket
                                    </Text>
                                  )}
                                  {/* Row 3: quantity selector centered below session info */}
                                  {isSelected && !isDisabledDueToOverlap && (
                                    <Group justify="center" mt="xs" onClick={(e: React.MouseEvent) => e.stopPropagation()}>
                                      <TicketQuantitySelector
                                        quantity={ticketQuantities[tt.id || ''] || 1}
                                        max={getEffectiveMaxQuantity(tt)}
                                        onChange={(qty) => handleQuantityChange(tt.id || '', qty)}
                                      />
                                    </Group>
                                  )}
                                  {/* Info text when user has no contacts but quantity > 1 */}
                                  {isSelected && !isDisabledDueToOverlap &&
                                    (ticketQuantities[tt.id || ''] || 1) > 1 && principalContacts.length === 0 && (
                                    <Text size="xs" c="dimmed" mt={4} ta="center">
                                      Add authorized contacts in Profile Settings to assign tickets to others.
                                    </Text>
                                  )}
                                </Box>
                              </Group>
                            </Paper>
                          );
                        })}
                      </Stack>

                      {/* Ticket Assignment Rows - shown when:
                          - Normal flow: any selected ticket has quantity > 1
                          - Buy-for-others flow: always (every ticket needs an assignee) */}
                      {(buyForOthersOnly
                        ? selectedTickets.length > 0
                        : selectedTickets.some(t => (ticketQuantities[t.id!] || 1) > 1)
                      ) && (
                        <>
                          <Divider my="sm" />
                          <Text fw={600} size="md">
                            Ticket Assignments
                          </Text>
                          <Stack gap="xs">
                            {selectedTickets.flatMap((ticket) => {
                              const quantity = ticketQuantities[ticket.id!] || 1;
                              const assignments = ticketAssignments[ticket.id!] || [];

                              return Array.from({ length: quantity }, (_, i) => {
                                // When buyForOthersOnly, ALL tickets are for assignees (no purchaser row)
                                const isPurchaser = buyForOthersOnly ? false : i === 0;
                                // Assignment index: in normal flow, index 0 is purchaser so assignments start at i-1
                                // In buyForOthersOnly, all indices map directly to assignments array
                                const assignmentIndex = buyForOthersOnly ? i : i - 1;

                                return (
                                  <TicketAssignmentRow
                                    key={`${ticket.id}-${i}`}
                                    ticketNumber={i + 1}
                                    isPurchaserTicket={isPurchaser}
                                    contacts={principalContacts}
                                    selectedContactId={isPurchaser ? null : (assignments[assignmentIndex] ?? null)}
                                    onAssignmentChange={(contactId) => {
                                      if (!isPurchaser) {
                                        handleAssignmentChange(ticket.id!, assignmentIndex, contactId);
                                      }
                                    }}
                                  />
                                );
                              });
                            })}
                          </Stack>
                        </>
                      )}
                    </Stack>
                  </Paper>
                )}

                {/* Sliding Scale Selector - only show if any selected ticket has sliding scale */}
                {hasAnySlidingScaleTicket && firstSlidingTicket && (
                  <>
                    <SlidingScaleSelector
                      basePrice={eventInfo.basePrice}
                      currency={eventInfo.currency}
                      onAmountChange={(amount, percentage) => {
                        updateDiscountPercentage(percentage);
                        // Update all sliding scale tickets' prices in real-time
                        // For multi-ticket: per-unit price * quantity (AD-012: uniform sliding scale)
                        const updatedPrices = { ...ticketPrices };
                        selectedTickets.forEach(ticket => {
                          if (ticket.pricingType === 'SlidingScale') {
                            const quantity = ticketQuantities[ticket.id!] || 1;
                            updatedPrices[ticket.id!] = amount * quantity;
                          }
                        });
                        setTicketPrices(updatedPrices);
                      }}
                      title="Choose Your Payment Amount"
                      forceSliding={true}
                      minPrice={firstSlidingTicket.minPrice ?? undefined}
                      maxPrice={firstSlidingTicket.maxPrice ?? undefined}
                      defaultPrice={firstSlidingTicket.defaultPrice ?? undefined}
                    />
                    {/* Show note when purchasing multiple tickets with sliding scale */}
                    {selectedTickets.some(t => (ticketQuantities[t.id!] || 1) > 1) && (
                      <Text size="sm" c="dimmed" ta="center" mt={-8}>
                        (Applies to all tickets in this purchase)
                      </Text>
                    )}
                  </>
                )}


                <Group justify="center" mt={0}>
                  <Button
                    onClick={handleContinue}
                    size="lg"
                    color="#880124"
                    disabled={selectedTickets.length === 0}
                    styles={(_theme) => ({
                      root: {
                        background: selectedTickets.length === 0
                          ? 'var(--mantine-color-gray-3)'
                          : 'linear-gradient(135deg, #FFB800, #DAA520)',
                        border: 'none',
                        borderRadius: '12px 6px 12px 6px',
                        color: selectedTickets.length === 0 ? 'var(--mantine-color-gray-6)' : '#2C2C2C',
                        fontFamily: 'Montserrat, sans-serif',
                        fontWeight: 600,
                        transition: 'all 0.3s ease',
                        cursor: selectedTickets.length === 0 ? 'not-allowed' : 'pointer',
                        '&:hover': selectedTickets.length > 0 ? {
                          borderRadius: '6px 12px 6px 12px',
                          boxShadow: '0 4px 12px rgba(255, 191, 0, 0.3)',
                          transform: 'translateY(-1px)'
                        } : {}
                      }
                    })}
                  >
                    Continue to Payment
                  </Button>
                </Group>
              </>
            )}

            {/* Step 2: Payment Form */}
            {currentStep === 1 && (
              <Box style={{ padding: isMobile ? '0 16px' : 0 }}>
                {/* Processing indicator */}
                {checkout.isPending && (
                  <Paper p="lg" radius="md" mb="md" style={{ background: 'rgba(25, 113, 194, 0.05)', border: '1px solid rgba(25, 113, 194, 0.2)' }}>
                    <Group>
                      <Loader size="sm" />
                      <Stack gap={2}>
                        <Text fw={600}>Processing your payment...</Text>
                        <Text size="sm" c="dimmed">Please do not close this page or press back.</Text>
                      </Stack>
                    </Group>
                  </Paper>
                )}

                {/* Checkout error display */}
                {checkout.isError && !checkout.isPending && !checkoutErrorDismissed && (
                  <Alert
                    icon={<IconAlertCircle size={18} />}
                    title={getCheckoutErrorTitle(checkout.error)}
                    color={isPaymentCharged(checkout.error) ? 'orange' : 'red'}
                    mb="md"
                  >
                    <Stack gap="xs">
                      <Text size="sm">{getCheckoutErrorMessage(checkout.error)}</Text>
                      {getCorrelationId(checkout.error) && (
                        <Text size="xs" c="dimmed">Reference: {getCorrelationId(checkout.error)}</Text>
                      )}
                      {!isPaymentCharged(checkout.error) && (
                        <Button
                          size="sm"
                          variant="outline"
                          color="red"
                          mt="xs"
                          onClick={() => setCheckoutErrorDismissed(true)}
                        >
                          Dismiss and Try Again
                        </Button>
                      )}
                      {isPaymentCharged(checkout.error) && (
                        <Text size="xs" fw={600} c="orange">
                          If you believe you were charged incorrectly, please contact info@witchcityrope.com
                        </Text>
                      )}
                    </Stack>
                  </Alert>
                )}

                <PaymentForm
                  eventInfo={eventInfo}
                  ticketTypeIds={selectedTicketTypeIds}
                  idempotencyKey={idempotencyKey}
                  eventWaiverAccepted={true}
                  initialSlidingScale={discountPercentage}
                  totalAmount={Object.values(ticketPrices).reduce((sum, price) => sum + price, 0)}
                  ticketSelections={
                    buyForOthersOnly || buildTicketSelections().some(ts => ts.quantity > 1)
                      ? buildTicketSelections()
                      : undefined
                  }
                  buyForOthersOnly={buyForOthersOnly}
                  onNonceReady={handleNonceReady}
                  onPaymentSuccess={handlePayPalSuccess}
                  onPaymentError={handlePaymentError}
                  isCheckoutInProgress={checkout.isPending}
                />
              </Box>
            )}

            {/* Step 3: Confirmation */}
            {currentStep === 2 && completedPayment && (
              <PaymentConfirmation
                payment={completedPayment}
                eventInfo={eventInfo}
                purchasedTickets={selectedTickets.map(ticket => ({
                  id: ticket.id || '',
                  name: ticket.name || '',
                  sessions: getTicketSessionDetailsFull(ticket)
                }))}
                onViewRegistrations={handleViewRegistrations}
                onRegisterMore={handleRegisterMore}
              />
            )}
          </Stack>

          {/* Sidebar - Payment Summary */}
          {currentStep < 2 && (
            <Box style={{ flex: 1, minWidth: 300 }} visibleFrom="md">
              <Box style={{ position: 'sticky', top: 20 }}>
                <PaymentSummary
                  eventInfo={eventInfo}
                  calculation={calculation}
                  selectedTickets={selectedTickets}
                  ticketPrices={ticketPrices}
                  ticketQuantities={ticketQuantities}
                  sessions={sessions}
                  detailed={true}
                />
              </Box>
            </Box>
          )}
        </Group>

        {/* Security Notice */}
        {currentStep < 2 && (
          <Paper p="sm" radius="md" bg="gray.0">
            <Text size="xs" ta="center" c="dimmed">
              🔒 This is a secure 256-bit SSL encrypted payment. 
              Your payment information is protected and never stored on our servers.
            </Text>
          </Paper>
        )}
      </Stack>
    </Container>
  );
};