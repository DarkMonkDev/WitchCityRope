# Technology Research: Authorize.net Accept.js for React + TypeScript
<!-- Last Updated: 2026-02-22 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary

**Decision Required**: How to integrate Authorize.net Accept.js tokenized credit card processing into the WitchCityRope React + TypeScript application.

**Recommendation**: Use the `react-acceptjs` npm package (TypeScript-native, hook-based) for frontend tokenization with a custom payment form, paired with the `AuthorizeNet` .NET SDK on the backend to process payment nonces. This gives WitchCityRope full control over the payment form UX while keeping card data off our servers entirely.

**Confidence Level**: High (90%)

**Key Factors**:
1. Card numbers NEVER touch the WitchCityRope server (PCI SAQ A-EP eligible)
2. Full UI control with custom Mantine-styled payment form
3. Proven React hook (`useAcceptJs`) with TypeScript types included

---

## 1. What is Accept.js?

### Overview

Accept.js is a JavaScript library provided by Authorize.net that enables **client-side credit card tokenization**. Instead of your server receiving raw card data, Accept.js sends the card data directly from the user's browser to Authorize.net's servers. Authorize.net returns a **one-time-use payment nonce** (opaque token) that your frontend sends to your backend. Your backend then uses this nonce with the Authorize.net SDK to complete the charge.

### How It Works (The Flow)

```
1. User fills out YOUR custom card form (card number, expiry, CVV)
2. On submit, Accept.js sends card data DIRECTLY to Authorize.net servers
3. Authorize.net validates the card data and returns a payment nonce
4. Your frontend receives the nonce (opaqueData: { dataDescriptor, dataValue })
5. Your frontend sends ONLY the nonce to YOUR backend API
6. Your backend uses the nonce with Authorize.net .NET SDK to charge the card
7. Card number NEVER touches your server at any point
```

### Payment Nonce Details

- **dataDescriptor**: `"COMMON.ACCEPT.INAPP.PAYMENT"` (identifies the token type)
- **dataValue**: An encrypted, opaque token string (the actual nonce)
- **Validity**: The nonce is valid for **15 minutes** after issuance
- **Usage**: One-time use only -- cannot be reused for multiple transactions
- **Compatibility**: Can be used anywhere in the Authorize.net API that accepts a `creditCard` or `bankAccount` payment type

### PCI Compliance Level

| Integration Method | PCI SAQ Level | Your Form? | Card Data Touches Your Server? |
|---|---|---|---|
| **Accept.js (Inline)** | **SAQ A-EP** | Yes, your custom form | No -- sent directly to Authorize.net via JS |
| Accept.js with UI (Hosted Lightbox) | SAQ A | No, Authorize.net popup | No |
| Accept Hosted (iFrame) | SAQ A | No, Authorize.net iFrame | No |
| Raw API (card data in API call) | SAQ D | Yes | YES -- full PCI burden |

**For WitchCityRope**: Accept.js Inline (SAQ A-EP) is the recommended approach. This means:
- You host your own payment form (full UI control with Mantine components)
- Card data is transmitted by JavaScript directly to Authorize.net
- Raw card numbers never reach your server
- SAQ A-EP is significantly lighter than SAQ D (139 fewer requirements)
- You still need HTTPS on all checkout pages (already in place)
- You must protect the integrity of the page serving the payment form (standard web security)

### Accept.js Product Variants

Authorize.net offers four Accept products:

| Product | Description | PCI Level | Best For |
|---|---|---|---|
| **Accept.js (Inline)** | Your form, their tokenization | SAQ A-EP | Custom-branded checkout (RECOMMENDED) |
| **Accept.js with UI** | Their popup lightbox over your page | SAQ A | Quick integration, less UI control |
| **Accept Hosted** | Their iFrame/redirect, their form entirely | SAQ A | Minimal development effort |
| **Accept Customer** | Hosted customer profile management | SAQ A | Saved card/profile management |

**Why Accept.js Inline for WitchCityRope?**
- Full control over form styling (Mantine v7 components, WCR branding)
- Mobile-first responsive design with our design system
- Custom validation and error display matching our UX patterns
- Seamless integration into our React SPA flow (no popup/iframe jank)
- Card data still never touches our server

---

## 2. React Integration

### Available React Packages

#### Option A: `react-acceptjs` (RECOMMENDED)

| Attribute | Value |
|---|---|
| **npm** | `react-acceptjs` |
| **GitHub** | `brendanbond/react-acceptjs` |
| **Stars** | 21 |
| **TypeScript** | Native (written in TypeScript) |
| **React Version** | 16.8+ (hooks-based) |
| **Last Updated** | Active maintenance |
| **Bundle Size** | Lightweight wrapper |
| **Approach** | Hook-based (`useAcceptJs`) + Components |

**Key Features**:
- `useAcceptJs` hook -- loads Accept.js script, exposes `dispatchData`
- `HostedForm` component -- pre-built lightbox integration
- `AcceptHosted` component -- iFrame/redirect integration
- Full TypeScript type definitions included
- Supports both credit card and bank account (eCheck) tokenization
- Environment switching (SANDBOX/PRODUCTION)

#### Option B: `react-authorize-net`

| Attribute | Value |
|---|---|
| **npm** | `react-authorize-net` |
| **GitHub** | `j-em/react-authorize-net` |
| **Version** | 0.6.0 |
| **TypeScript** | Supported |
| **Approach** | Component-based (render props) |

**Why Not Recommended**: Older pattern (render props vs hooks), less actively maintained, heavier API surface.

#### Option C: Custom Implementation (No Package)

You can load Accept.js manually and call `Accept.dispatchData()` directly. This is viable but re-invents what `react-acceptjs` already provides (script loading, Promise wrapping, TypeScript types).

### Installation

```bash
npm install --save react-acceptjs
```

### The `useAcceptJs` Hook

```typescript
import { useAcceptJs } from 'react-acceptjs';

// Configuration
const authData = {
  apiLoginId: 'YOUR_API_LOGIN_ID',
  clientKey: 'YOUR_PUBLIC_CLIENT_KEY',
};

// In your component
const { dispatchData, loading, error } = useAcceptJs({
  environment: 'SANDBOX', // or 'PRODUCTION'
  authData,
});
```

**What the hook does**:
1. Dynamically loads the Accept.js script from Authorize.net's CDN
2. Handles script loading states (`loading`, `error`)
3. Wraps `Accept.dispatchData()` in a Promise (the original API uses callbacks)
4. Returns typed response data

### TypeScript Type Definitions

The `react-acceptjs` package exports these types:

```typescript
// Payment data you send to Accept.js
type BasicCardData = {
  cardNumber: string;
  month: string;    // "01" through "12"
  year: string;     // "2026" (4-digit)
  cardCode: string; // CVV
};

type BasicBankData = {
  accountNumber: string;
  routingNumber: string;
  nameOnAccount: string;
  accountType: 'checking' | 'savings' | 'businessChecking';
};

type CardPaymentData = { cardData: BasicCardData };
type BankPaymentData = { bankData: BasicBankData };
type PaymentData = CardPaymentData | BankPaymentData;

// Response from Accept.js
type ErrorMessage = {
  code: string;
  text: string;
};

type DispatchDataResponse = {
  opaqueData: {
    dataDescriptor: string; // "COMMON.ACCEPT.INAPP.PAYMENT"
    dataValue: string;      // The encrypted payment nonce
  };
  messages: {
    resultCode: 'Ok' | 'Error';
    message: ErrorMessage[];
  };
};
```

### Complete React Component Pattern

```typescript
// /apps/web/src/features/payments/components/CreditCardForm.tsx

import { useState, type FormEvent } from 'react';
import { useAcceptJs, type DispatchDataResponse } from 'react-acceptjs';
import { TextInput, Button, Group, Stack, Alert } from '@mantine/core';

// Environment config -- these are PUBLIC keys, safe for frontend
const AUTHNET_AUTH_DATA = {
  apiLoginId: import.meta.env.VITE_AUTHNET_API_LOGIN_ID,
  clientKey: import.meta.env.VITE_AUTHNET_CLIENT_KEY,
};

const AUTHNET_ENVIRONMENT = import.meta.env.VITE_AUTHNET_ENVIRONMENT as
  | 'SANDBOX'
  | 'PRODUCTION';

interface CreditCardFormProps {
  amount: number;
  onPaymentSuccess: (transactionId: string) => void;
  onPaymentError: (error: string) => void;
}

export function CreditCardForm({
  amount,
  onPaymentSuccess,
  onPaymentError,
}: CreditCardFormProps) {
  // Accept.js hook -- handles script loading and dispatchData
  const { dispatchData, loading, error } = useAcceptJs({
    environment: AUTHNET_ENVIRONMENT,
    authData: AUTHNET_AUTH_DATA,
  });

  // Card form state -- these values NEVER leave the browser
  // They are sent directly to Authorize.net by Accept.js
  const [cardNumber, setCardNumber] = useState('');
  const [expMonth, setExpMonth] = useState('');
  const [expYear, setExpYear] = useState('');
  const [cvv, setCvv] = useState('');
  const [processing, setProcessing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setProcessing(true);
    setErrorMessage(null);

    try {
      // Step 1: Send card data to Authorize.net (NOT to our server)
      const response: DispatchDataResponse = await dispatchData({
        cardData: {
          cardNumber: cardNumber.replace(/\s/g, ''),
          month: expMonth,
          year: expYear,
          cardCode: cvv,
        },
      });

      // Step 2: Check for Accept.js errors
      if (response.messages.resultCode === 'Error') {
        const errorMsg = response.messages.message
          .map((msg) => msg.text)
          .join(', ');
        setErrorMessage(errorMsg);
        onPaymentError(errorMsg);
        return;
      }

      // Step 3: Send ONLY the nonce to OUR backend (card data stays in browser)
      const { dataDescriptor, dataValue } = response.opaqueData;

      const backendResponse = await fetch('/api/payments/charge', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // httpOnly cookie auth
        body: JSON.stringify({
          dataDescriptor,
          dataValue,
          amount,
        }),
      });

      if (!backendResponse.ok) {
        const errorData = await backendResponse.json();
        throw new Error(errorData.message || 'Payment failed');
      }

      const result = await backendResponse.json();
      onPaymentSuccess(result.transactionId);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'An unexpected error occurred';
      setErrorMessage(message);
      onPaymentError(message);
    } finally {
      setProcessing(false);
    }
  };

  if (error) {
    return (
      <Alert color="red" title="Payment System Unavailable">
        Unable to load payment processing. Please try again later.
      </Alert>
    );
  }

  return (
    <form onSubmit={handleSubmit}>
      <Stack gap="md">
        {errorMessage && (
          <Alert color="red" title="Payment Error">
            {errorMessage}
          </Alert>
        )}

        <TextInput
          label="Card Number"
          placeholder="4111 1111 1111 1111"
          value={cardNumber}
          onChange={(e) => setCardNumber(e.currentTarget.value)}
          required
          maxLength={19}
          disabled={loading || processing}
          autoComplete="cc-number"
        />

        <Group grow>
          <TextInput
            label="Expiry Month"
            placeholder="MM"
            value={expMonth}
            onChange={(e) => setExpMonth(e.currentTarget.value)}
            required
            maxLength={2}
            disabled={loading || processing}
            autoComplete="cc-exp-month"
          />
          <TextInput
            label="Expiry Year"
            placeholder="YYYY"
            value={expYear}
            onChange={(e) => setExpYear(e.currentTarget.value)}
            required
            maxLength={4}
            disabled={loading || processing}
            autoComplete="cc-exp-year"
          />
          <TextInput
            label="CVV"
            placeholder="123"
            value={cvv}
            onChange={(e) => setCvv(e.currentTarget.value)}
            required
            maxLength={4}
            disabled={loading || processing}
            autoComplete="cc-csc"
          />
        </Group>

        <Button
          type="submit"
          loading={processing}
          disabled={loading || processing}
          fullWidth
          size="lg"
        >
          {loading
            ? 'Loading payment system...'
            : processing
            ? 'Processing...'
            : `Pay $${amount.toFixed(2)}`}
        </Button>
      </Stack>
    </form>
  );
}
```

### How Accept.js Loads in React

The `react-acceptjs` `useAcceptJs` hook handles script loading automatically:

1. On mount, it creates a `<script>` tag pointing to the appropriate Accept.js URL
2. **Sandbox**: `https://jstest.authorize.net/v1/Accept.js`
3. **Production**: `https://js.authorize.net/v1/Accept.js`
4. The `loading` flag is `true` until the script loads
5. The `error` flag is `true` if the script fails to load
6. Once loaded, `dispatchData` becomes available

If you need to load Accept.js manually (without the package):

```typescript
// Manual script loading (NOT recommended -- use react-acceptjs instead)
useEffect(() => {
  const script = document.createElement('script');
  script.src =
    environment === 'PRODUCTION'
      ? 'https://js.authorize.net/v1/Accept.js'
      : 'https://jstest.authorize.net/v1/Accept.js';
  script.async = true;
  script.onload = () => setLoaded(true);
  script.onerror = () => setError(true);
  document.body.appendChild(script);
  return () => {
    document.body.removeChild(script);
  };
}, [environment]);
```

---

## 3. Accept.js Flow (Detailed)

### Step-by-Step Sequence

```
Browser (React)                    Authorize.net                Your Backend (.NET)
     |                                  |                            |
     | 1. User fills card form          |                            |
     |                                  |                            |
     | 2. Accept.dispatchData() ------> |                            |
     |    (card#, exp, cvv, authData)   |                            |
     |                                  |                            |
     | 3. <--- payment nonce ---------- |                            |
     |    (dataDescriptor, dataValue)   |                            |
     |                                  |                            |
     | 4. POST /api/payments/charge ----|------------------------->  |
     |    (nonce only, NO card data)    |                            |
     |                                  |                            |
     |                                  | 5. <-- createTransaction   |
     |                                  |    (nonce + amount)        |
     |                                  |                            |
     |                                  | 6. --> transaction result  |
     |                                  |                            |
     | 7. <--- success/failure ---------|--------------------------- |
     |                                  |                            |
```

### What Data Flows Where

| Data | Sent From Browser? | Reaches Your Server? | Reaches Authorize.net? |
|---|---|---|---|
| Card Number | Yes (to Authorize.net only) | NO | Yes |
| Expiry Date | Yes (to Authorize.net only) | NO | Yes |
| CVV | Yes (to Authorize.net only) | NO | Yes |
| Payment Nonce | Yes (to your server) | Yes (opaque token only) | Yes (via your server) |
| Transaction Amount | Yes (to your server) | Yes | Yes (via your server) |
| API Login ID | Yes (public key, embedded in page) | NO | Yes |
| Client Key | Yes (public key, embedded in page) | NO | Yes |
| Transaction Key | NO (never in browser) | Yes (server-side only) | Yes (via your server) |

### Security Architecture

```
BROWSER (Public)                         SERVER (Private)
+----------------------------------+     +----------------------------------+
| API Login ID (public)            |     | API Login ID                     |
| Client Key (public)              |     | Transaction Key (SECRET)         |
| Card form (user input)           |     | Payment processing logic         |
| Accept.js library                |     | AuthorizeNet .NET SDK            |
|                                  |     |                                  |
| Card data -> Authorize.net ONLY  |     | Nonce -> Authorize.net API       |
| Nonce -> Your server             |     | NEVER sees card data             |
+----------------------------------+     +----------------------------------+
```

---

## 4. Backend Integration (.NET)

### NuGet Package

```bash
dotnet add package AuthorizeNet
```

The official SDK is `AuthorizeNet` (currently managed by Visa/Cybersource after their acquisition of Authorize.net).

### C# Code Pattern for Processing a Nonce-Based Payment

```csharp
// /apps/api/Features/Payments/ProcessCreditCardPayment.cs

using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Controllers.Bases;

public class ProcessCreditCardPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessCreditCardPaymentService> _logger;

    public ProcessCreditCardPaymentService(
        IConfiguration configuration,
        ILogger<ProcessCreditCardPaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeWithNonce(
        string dataDescriptor,
        string dataValue,
        decimal amount,
        string? customerEmail = null)
    {
        // Step 1: Configure the API environment
        var environment = _configuration["AuthorizeNet:Environment"] == "Production"
            ? AuthorizeNet.Environment.PRODUCTION
            : AuthorizeNet.Environment.SANDBOX;

        ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = environment;

        // Step 2: Set up merchant authentication (server-side credentials)
        ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication =
            new merchantAuthenticationType
            {
                name = _configuration["AuthorizeNet:ApiLoginId"],
                ItemElementName = ItemChoiceType.transactionKey,
                Item = _configuration["AuthorizeNet:TransactionKey"],
            };

        // Step 3: Create the opaque data payment from the Accept.js nonce
        var opaqueData = new opaqueDataType
        {
            dataDescriptor = dataDescriptor,  // "COMMON.ACCEPT.INAPP.PAYMENT"
            dataValue = dataValue,            // The encrypted nonce from Accept.js
        };

        var paymentType = new paymentType
        {
            Item = opaqueData,  // Use nonce instead of raw card data
        };

        // Step 4: Build the transaction request
        var transactionRequest = new transactionRequestType
        {
            transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
            amount = amount,
            payment = paymentType,
        };

        // Optional: Add customer email for receipt
        if (!string.IsNullOrEmpty(customerEmail))
        {
            transactionRequest.customer = new customerDataType
            {
                email = customerEmail,
            };
        }

        var request = new createTransactionRequest
        {
            transactionRequest = transactionRequest,
        };

        // Step 5: Execute the transaction
        var controller = new createTransactionController(request);
        controller.Execute();

        var response = controller.GetApiResponse();

        // Step 6: Process the response
        if (response == null)
        {
            _logger.LogError("Authorize.net: Null response received");
            return PaymentResult.Failure("No response from payment processor");
        }

        if (response.messages.resultCode == messageTypeEnum.Ok
            && response.transactionResponse != null)
        {
            var transResponse = response.transactionResponse;

            if (transResponse.messages != null)
            {
                _logger.LogInformation(
                    "Authorize.net payment successful. TransactionId: {TransactionId}",
                    transResponse.transId);

                return PaymentResult.Success(
                    transactionId: transResponse.transId,
                    authCode: transResponse.authCode,
                    responseCode: transResponse.responseCode);
            }

            if (transResponse.errors != null)
            {
                var error = transResponse.errors[0];
                _logger.LogWarning(
                    "Authorize.net transaction error: {Code} - {Text}",
                    error.errorCode, error.errorText);

                return PaymentResult.Failure(
                    $"{error.errorCode}: {error.errorText}");
            }
        }

        // API-level error (not transaction-level)
        if (response.messages?.message != null)
        {
            var error = response.messages.message[0];
            _logger.LogWarning(
                "Authorize.net API error: {Code} - {Text}",
                error.code, error.text);

            return PaymentResult.Failure($"{error.code}: {error.text}");
        }

        return PaymentResult.Failure("Unknown payment processing error");
    }
}

// Result type
public record PaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? AuthCode,
    string? ResponseCode,
    string? ErrorMessage)
{
    public static PaymentResult Success(
        string transactionId, string authCode, string responseCode)
        => new(true, transactionId, authCode, responseCode, null);

    public static PaymentResult Failure(string errorMessage)
        => new(false, null, null, null, errorMessage);
}
```

### How This Differs from Raw Card Data Approach

| Aspect | Raw Card Data (SAQ D) | Accept.js Nonce (SAQ A-EP) |
|---|---|---|
| **Payment object** | `creditCardType` with card number, expiry, CVV | `opaqueDataType` with dataDescriptor + dataValue |
| **Card data on server** | YES -- your server handles raw card data | NO -- only opaque nonce |
| **PCI scope** | Full SAQ D (326+ requirements) | SAQ A-EP (139 requirements) |
| **SDK payment type** | `paymentType { Item = creditCardType }` | `paymentType { Item = opaqueDataType }` |
| **Backend risk** | High -- card data in memory/logs | Low -- nonce is useless if intercepted |

### API Endpoint Pattern

```csharp
// /apps/api/Features/Payments/PaymentEndpoints.cs

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments")
            .RequireAuthorization();

        group.MapPost("/charge", ChargeWithNonce);
    }

    private static async Task<IResult> ChargeWithNonce(
        ChargeRequest request,
        ProcessCreditCardPaymentService paymentService,
        HttpContext httpContext)
    {
        // Validate the request
        if (string.IsNullOrEmpty(request.DataDescriptor)
            || string.IsNullOrEmpty(request.DataValue)
            || request.Amount <= 0)
        {
            return Results.BadRequest("Invalid payment data");
        }

        var userEmail = httpContext.User.FindFirst("email")?.Value;

        var result = await paymentService.ChargeWithNonce(
            request.DataDescriptor,
            request.DataValue,
            request.Amount,
            userEmail);

        if (result.IsSuccess)
        {
            return Results.Ok(new
            {
                transactionId = result.TransactionId,
                authCode = result.AuthCode,
            });
        }

        return Results.BadRequest(new { message = result.ErrorMessage });
    }
}

public record ChargeRequest(
    string DataDescriptor,
    string DataValue,
    decimal Amount);
```

### Configuration (User Secrets / Environment Variables)

```json
// appsettings.json (non-sensitive defaults)
{
  "AuthorizeNet": {
    "Environment": "Sandbox"
  }
}

// User Secrets or Environment Variables (SENSITIVE -- never in source control)
// AuthorizeNet__ApiLoginId = "YOUR_API_LOGIN_ID"
// AuthorizeNet__TransactionKey = "YOUR_TRANSACTION_KEY"
```

Frontend environment variables (these are PUBLIC keys):
```env
# .env or .env.local
VITE_AUTHNET_API_LOGIN_ID=your_api_login_id
VITE_AUTHNET_CLIENT_KEY=your_public_client_key
VITE_AUTHNET_ENVIRONMENT=SANDBOX
```

---

## 5. Accept.js Inline vs Hosted Comparison

### Accept.js Inline (RECOMMENDED for WitchCityRope)

**How it works**: You build your own HTML form. On submit, Accept.js tokenizes the card data via JavaScript and returns a nonce.

**Pros**:
- Full control over form appearance (Mantine components, WCR branding)
- Seamless React SPA experience (no popups, no iframes)
- Custom validation and error display
- Mobile-responsive with your existing design system
- No jarring UI transitions

**Cons**:
- SAQ A-EP (not SAQ A -- slightly more PCI compliance work)
- You must protect the integrity of your checkout page
- You manage form state and validation

### Accept.js with UI (Hosted Lightbox)

**How it works**: The `react-acceptjs` `HostedForm` component renders a button. When clicked, an Authorize.net-hosted lightbox popup appears for card entry.

```typescript
import { HostedForm } from 'react-acceptjs';

<HostedForm
  authData={authData}
  onSubmit={(response) => {
    // response.opaqueData contains the nonce
  }}
  buttonText="Enter Payment Info"
  formHeaderText="WitchCityRope Payment"
/>
```

**Pros**:
- SAQ A compliance (simplest PCI level)
- Authorize.net handles the form entirely
- Less code to maintain

**Cons**:
- Limited styling control (Authorize.net's form appearance)
- Popup/lightbox may feel jarring in SPA
- Cannot use Mantine components
- Mobile experience depends on Authorize.net's implementation
- Poor UX for a modern React application

### Accept Hosted (iFrame)

**How it works**: Your backend requests a form token from Authorize.net. The frontend uses that token to render an Authorize.net-hosted payment form in an iFrame.

```typescript
import { AcceptHosted } from 'react-acceptjs';

<AcceptHosted formToken={tokenFromBackend} integration="iframe">
  <AcceptHosted.Button className="my-button">
    Pay Now
  </AcceptHosted.Button>
  <AcceptHosted.IFrameBackdrop />
  <AcceptHosted.IFrameContainer>
    <AcceptHosted.IFrame />
  </AcceptHosted.IFrameContainer>
</AcceptHosted>
```

**Pros**:
- SAQ A compliance
- Authorize.net handles the entire transaction
- Compound component pattern gives some layout control

**Cons**:
- Requires backend API call to get form token first
- iFrame cross-domain communication complexity
- Requires hosting a "communicator" JavaScript file for cross-domain messaging
- Limited styling
- More complex setup than inline Accept.js

### Recommendation for WitchCityRope

**Accept.js Inline** is the best choice because:
1. Full UI control with Mantine v7 design system
2. Mobile-first responsive design (critical for event attendees)
3. Seamless SPA experience (no popups or iframes)
4. SAQ A-EP is achievable and appropriate for our use case
5. Simplest frontend code (just a hook + form)
6. Best user experience for the community

---

## 6. Security Considerations

### What Data is Sent to Accept.js

The `dispatchData` function sends this data from the browser DIRECTLY to Authorize.net:

```typescript
// This data goes to Authorize.net, NOT to your server
{
  authData: {
    clientKey: 'your_public_client_key',  // Public key -- safe for browser
    apiLoginID: 'your_api_login_id',      // Public identifier -- safe for browser
  },
  cardData: {
    cardNumber: '4111111111111111',  // Goes ONLY to Authorize.net
    month: '12',                      // Goes ONLY to Authorize.net
    year: '2026',                     // Goes ONLY to Authorize.net
    cardCode: '123',                  // Goes ONLY to Authorize.net
  }
}
```

**IMPORTANT**: The `clientKey` and `apiLoginID` are PUBLIC keys. They are meant to be in the browser. The `transactionKey` is the SECRET and must ONLY exist on the server.

### Content Security Policy (CSP) Requirements

If you implement CSP headers, you need to allow these domains:

```
# Production
script-src: https://js.authorize.net
connect-src: https://api.authorize.net

# Sandbox/Development
script-src: https://jstest.authorize.net
connect-src: https://apitest.authorize.net
```

Full CSP header example for Accept.js:

```
Content-Security-Policy:
  script-src 'self' https://js.authorize.net https://jstest.authorize.net;
  connect-src 'self' https://api.authorize.net https://apitest.authorize.net;
```

Note: Authorize.net does not provide official comprehensive CSP documentation. The domains above are based on the Accept.js script URLs and API endpoints. Test thoroughly in your environment.

### HTTPS Requirement

Accept.js **requires HTTPS** on the page where it runs. This is enforced by the library. In development with Docker + Vite proxy, this is handled by the development setup. In production, ensure HTTPS is configured.

### Error Handling

Accept.js returns errors in the response object. Common error codes:

| Error Code | Meaning |
|---|---|
| `E_WC_04` | Missing required fields (card number, expiry, CVV) |
| `E_WC_05` | Invalid card number |
| `E_WC_06` | Invalid expiration date |
| `E_WC_08` | Expiration date is in the past |
| `E_WC_15` | Invalid card code (CVV) |
| `E_WC_17` | Merchant authentication failed |
| `E_WC_20` | Card number exceeds maximum length |
| `E_WC_21` | Invalid authentication values (check API Login ID / Client Key) |

Error handling pattern:

```typescript
const response = await dispatchData({ cardData });

if (response.messages.resultCode === 'Error') {
  // Map Accept.js errors to user-friendly messages
  const errorMap: Record<string, string> = {
    E_WC_04: 'Please provide card number, expiration, and CVV.',
    E_WC_05: 'Please enter a valid card number.',
    E_WC_06: 'Please enter a valid expiration date.',
    E_WC_08: 'Your card has expired. Please use a different card.',
    E_WC_15: 'Please enter a valid security code (CVV).',
    E_WC_17: 'Payment system configuration error. Please contact support.',
    E_WC_21: 'Payment system configuration error. Please contact support.',
  };

  const messages = response.messages.message.map(
    (msg) => errorMap[msg.code] || msg.text
  );
  setError(messages.join(' '));
  return;
}
```

### Nonce Security

- The payment nonce (dataValue) is **encrypted** and **opaque** -- it cannot be reverse-engineered to obtain card data
- It is **one-time use** -- cannot be replayed
- It **expires in 15 minutes** -- time-limited window
- Even if intercepted, it can only be used with the matching merchant credentials

### WitchCityRope-Specific Security Notes

- **No localStorage**: The nonce is held in React state only during the payment flow, never persisted. This aligns with our httpOnly cookie authentication pattern.
- **CSRF Protection**: Payment endpoints should be protected by our existing CSRF token system.
- **Authentication Required**: Payment endpoints must be behind authentication (RequireAuthorization) -- only logged-in users can make payments.
- **Amount Validation**: The backend MUST validate the payment amount against the actual ticket/event price. Never trust the amount sent from the frontend.

---

## 7. Code Examples Summary

### Frontend: Complete Payment Form Component

See Section 2 above for the complete `CreditCardForm.tsx` component with Mantine UI integration.

### Backend: Complete Payment Processing Service

See Section 4 above for the complete `ProcessCreditCardPaymentService.cs` with full error handling.

### Backend: API Endpoint

See Section 4 above for the complete `PaymentEndpoints.cs` with Minimal API pattern.

### Vanilla JavaScript (Reference Only)

For reference, here is the raw Accept.js API without React:

```html
<!-- Load Accept.js -->
<script src="https://jstest.authorize.net/v1/Accept.js"></script>

<script>
function sendPaymentDataToAnet() {
  var secureData = {};
  var authData = {};
  var cardData = {};

  // Card data from your form
  cardData.cardNumber = document.getElementById('cardNumber').value;
  cardData.month = document.getElementById('expMonth').value;
  cardData.year = document.getElementById('expYear').value;
  cardData.cardCode = document.getElementById('cvv').value;
  secureData.cardData = cardData;

  // Public authentication data
  authData.clientKey = 'YOUR_PUBLIC_CLIENT_KEY';
  authData.apiLoginID = 'YOUR_API_LOGIN_ID';
  secureData.authData = authData;

  // Send to Authorize.net (NOT to your server)
  Accept.dispatchData(secureData, responseHandler);
}

function responseHandler(response) {
  if (response.messages.resultCode === 'Error') {
    for (var i = 0; i < response.messages.message.length; i++) {
      console.log(
        response.messages.message[i].code + ': ' +
        response.messages.message[i].text
      );
    }
  } else {
    // Success! Get the payment nonce
    var dataDescriptor = response.opaqueData.dataDescriptor;
    var dataValue = response.opaqueData.dataValue;

    // Send the nonce (NOT card data) to your server
    // POST to your backend with { dataDescriptor, dataValue, amount }
  }
}
</script>
```

---

## 8. Implementation Considerations

### Migration Path for WitchCityRope

1. **Install `react-acceptjs`** in the web app
2. **Create the payment form component** using `useAcceptJs` hook + Mantine UI
3. **Add Authorize.net NuGet package** to the API project (if not already present)
4. **Create the payment processing service** using `opaqueDataType`
5. **Add the payment endpoint** to the Minimal API
6. **Configure environment variables** (frontend public keys, backend secret keys)
7. **Test with sandbox credentials** end-to-end
8. **Switch to production credentials** for launch

### Environment Variables Summary

| Variable | Location | Secret? | Purpose |
|---|---|---|---|
| `VITE_AUTHNET_API_LOGIN_ID` | Frontend .env | No (public) | Identifies the merchant to Accept.js |
| `VITE_AUTHNET_CLIENT_KEY` | Frontend .env | No (public) | Public key for Accept.js tokenization |
| `VITE_AUTHNET_ENVIRONMENT` | Frontend .env | No | SANDBOX or PRODUCTION |
| `AuthorizeNet__ApiLoginId` | Backend secrets | Yes | Server-side merchant authentication |
| `AuthorizeNet__TransactionKey` | Backend secrets | **YES (SECRET)** | Server-side transaction signing |
| `AuthorizeNet__Environment` | Backend config | No | Sandbox or Production |

### Bundle Size Impact

- `react-acceptjs`: ~5-8KB minified (lightweight wrapper)
- Accept.js library itself: Loaded from Authorize.net CDN (not in your bundle)
- Net impact on your bundle: Minimal (~5-8KB)

### Testing Strategy

- **Unit tests**: Mock `useAcceptJs` hook to test form validation and submission flow
- **Integration tests**: Use Authorize.net sandbox with test card numbers
- **E2E tests (Playwright)**: Test the complete payment flow with sandbox credentials
- **Test card numbers**: `4111111111111111` (Visa), `5424000000000015` (Mastercard)

---

## 9. Risk Assessment

### Low Risk
- **Accept.js library loading failure**
  - **Monitoring**: The `useAcceptJs` hook provides `loading` and `error` states
  - **Mitigation**: Show friendly error message, suggest retry or alternative payment

- **Nonce expiration (15-minute window)**
  - **Monitoring**: Track time between nonce generation and backend processing
  - **Mitigation**: Process nonce immediately after receiving it; do not store it

### Medium Risk
- **SAQ A-EP compliance maintenance**
  - **Mitigation**: Document requirements; the key requirement is page integrity (no XSS that could modify the payment form)
  - **Note**: Our existing security practices (CSP, input sanitization, HTTPS) already address most SAQ A-EP requirements

### High Risk
- **None identified** -- Accept.js is a mature, widely-used solution. The `react-acceptjs` package is lightweight and its internals are simple (script loading + Promise wrapper).

---

## 10. Recommendation

### Primary Recommendation: Accept.js Inline with `react-acceptjs`

**Confidence Level**: High (90%)

**Rationale**:
1. **Security**: Card data never touches WitchCityRope servers, reducing PCI scope to SAQ A-EP
2. **UX**: Full control over payment form with Mantine v7 components and WCR branding
3. **Developer Experience**: TypeScript-native hook (`useAcceptJs`) integrates naturally with React patterns
4. **Proven Pattern**: Accept.js is Authorize.net's recommended approach for modern web applications
5. **Lightweight**: ~5-8KB added to bundle; Accept.js itself loads from CDN
6. **Mobile-friendly**: Custom form can be fully responsive for event attendees on phones

**Implementation Priority**: Immediate -- required for ticket sales and event registration payments

### Alternative Recommendations

- **Second Choice**: `HostedForm` component from `react-acceptjs` -- if SAQ A compliance is preferred over UI control, use the hosted lightbox. Less customizable but simpler PCI story.
- **Future Consideration**: Accept Hosted with iFrame -- if requirements change to require SAQ A and the hosted form approach becomes acceptable UX-wise.

---

## Research Sources

- [Authorize.net Accept.js Official Documentation](https://developer.authorize.net/api/reference/features/acceptjs.html)
- [Authorize.net Accept Products Overview](https://developer.authorize.net/api/reference/features/accept.html)
- [react-acceptjs GitHub Repository](https://github.com/brendanbond/react-acceptjs)
- [react-acceptjs npm Package](https://www.npmjs.com/package/react-acceptjs)
- [AuthorizeNet .NET SDK GitHub](https://github.com/AuthorizeNet/sdk-dotnet)
- [Processing payments with Authorize.Net in a Blazor app (End Point Dev, Aug 2025)](https://www.endpointdev.com/blog/2025/08/processing-payments-with-authorize-net-in-a-blazor-app/)
- [Authorize.net Accept SAQ Eligibility White Paper](https://www.authorize.net/content/dam/anet-redesign/documents/coalfire_authorize.net_accept_saq_eligibility_white_paper.pdf)
- [AuthorizeNet accept-sample-app GitHub](https://github.com/AuthorizeNet/accept-sample-app)
- [Authorize.net Error and Response Codes](https://developer.authorize.net/api/reference/features/errorandresponsecodes.html)
- [Authorize.net Hello World Sandbox](https://developer.authorize.net/hello_world.html)
- [Cybersource Developer Community - Accept.js Payment Nonce](https://community.developer.cybersource.com/t5/Integration-and-Testing/Using-the-Accept-js-Payment-Nonce-to-Charge-a-Credit-Card-via/td-p/57014)
- [Authorize.net Accept.js Blog Post - Payment Nonce for All Transaction Types](https://community.developer.authorize.net/t5/The-Authorize-Net-Developer-Blog/Use-Accept-js-Payment-Nonce-for-All-Transaction-Types/ba-p/55199)
