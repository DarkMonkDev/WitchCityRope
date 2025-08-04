# Blazor Tests

This directory contains tests specific to Blazor functionality and components.

## Test Files

### blazor-state.spec.ts
- Verifies Blazor framework initialization
- Tests Blazor circuit connection and WebSocket status
- Checks component event bindings and interactivity
- Tests different Blazor render modes (SSR vs Interactive)
- Monitors Blazor-specific errors and diagnostics

## Key Test Scenarios

1. **Blazor Initialization**
   - Waits for `window.Blazor._internal` to be available
   - Verifies circuit manager is established
   - Checks WebSocket connections

2. **Component Interactivity**
   - Tests button click handlers with Blazor bindings
   - Verifies event propagation in Blazor components
   - Checks for `_blazorEvents` attachments

3. **Render Mode Detection**
   - Identifies Static SSR vs Interactive components
   - Checks for render mode attributes
   - Detects SSR markers and interactive markers

4. **Error Diagnostics**
   - Monitors for Blazor error UI visibility
   - Tracks disconnection states
   - Captures Blazor-specific console errors

## Running Tests

```bash
# Run all Blazor tests
npx playwright test blazor/

# Run with headed browser to see Blazor interactions
npx playwright test blazor/ --headed

# Debug specific test
npx playwright test blazor/blazor-state.spec.ts --debug
```

## Troubleshooting

- **No Blazor interactivity**: Check if components have proper render mode (InteractiveServer/InteractiveWebAssembly)
- **Circuit connection failures**: Verify SignalR/WebSocket configuration
- **Event handlers not working**: Ensure Blazor has finished initialization before interacting