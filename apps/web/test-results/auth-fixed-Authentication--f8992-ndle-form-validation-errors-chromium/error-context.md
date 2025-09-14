# Page snapshot

```yaml
- text: "[plugin:vite:import-analysis] Failed to resolve import \"@paypal/react-paypal-js\" from \"src/features/payments/components/PayPalButton.tsx\". Does the file exist? /app/src/features/payments/components/PayPalButton.tsx:16:7 11 | PayPalButtons, 12 | usePayPalScriptReducer 13 | } from \"@paypal/react-paypal-js\"; | ^ 14 | import { IconInfoCircle } from \"@tabler/icons-react\"; 15 | import { PaymentMethodType } from \"../types/payment.types\"; at TransformPluginContext._formatError (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49258:41) at TransformPluginContext.error (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49253:16) at normalizeUrl (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64291:23) at process.processTicksAndRejections (node:internal/process/task_queues:95:5) at async file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64423:39 at async Promise.all (index 2) at async TransformPluginContext.transform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:64350:7) at async PluginContainer.transform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:49099:18) at async loadAndTransform (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:51977:27) at async viteTransformMiddleware (file:///app/node_modules/vite/dist/node/chunks/dep-C6uTJdX2.js:62105:24 Click outside, press Esc key, or fix the code to dismiss. You can also disable this overlay by setting"
- code: server.hmr.overlay
- text: to
- code: "false"
- text: in
- code: vite.config.ts
- text: .
```