# Page snapshot

```yaml
- text: "[plugin:vite:import-analysis] Failed to resolve import \"@stripe/stripe-js\" from \"src/features/payments/hooks/usePayment.ts\". Does the file exist? /app/src/features/payments/hooks/usePayment.ts:6:27 1 | import { useState, useCallback } from \"react\"; 2 | import { useMutation, useQuery, useQueryClient } from \"@tanstack/react-query\"; 3 | import { loadStripe } from \"@stripe/stripe-js\"; | ^ 4 | import { paymentApi } from \"../api/paymentApi\"; 5 | const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY || \"\"); at TransformPluginContext._formatError (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49258:41) at TransformPluginContext.error (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49253:16) at normalizeUrl (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64291:23) at process.processTicksAndRejections (node:internal/process/task_queues:95:5) at async file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64423:39 at async Promise.all (index 2) at async TransformPluginContext.transform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64350:7) at async PluginContainer.transform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49099:18) at async loadAndTransform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:51977:27 Click outside, press Esc key, or fix the code to dismiss. You can also disable this overlay by setting"
- code: server.hmr.overlay
- text: to
- code: "false"
- text: in
- code: vite.config.ts
- text: .
```