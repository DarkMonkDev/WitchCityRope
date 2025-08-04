// Fix for blazor.server.js deprecated event warnings that prevent initialization
(function() {
    console.log('Applying Blazor deprecation fixes...');
    
    // Store original addEventListener
    const originalAddEventListener = window.addEventListener;
    const originalRemoveEventListener = window.removeEventListener;
    
    // Track deprecated event listeners to prevent them from breaking
    const deprecatedEventHandlers = new Map();
    
    // Override addEventListener to intercept deprecated events
    window.addEventListener = function(type, listener, options) {
        // Intercept deprecated events from blazor.server.js
        if (type === 'beforeunload' || type === 'unload') {
            console.warn(`Intercepted deprecated ${type} event listener from blazor.server.js`);
            
            // Store the handler but don't actually add it
            if (!deprecatedEventHandlers.has(type)) {
                deprecatedEventHandlers.set(type, []);
            }
            deprecatedEventHandlers.get(type).push(listener);
            
            // For 'beforeunload', use 'pagehide' instead
            if (type === 'beforeunload') {
                return originalAddEventListener.call(this, 'pagehide', listener, options);
            }
            
            // For 'unload', also use 'pagehide' as it's the modern equivalent
            if (type === 'unload') {
                return originalAddEventListener.call(this, 'pagehide', listener, options);
            }
        }
        
        // For all other events, use original behavior
        return originalAddEventListener.call(this, type, listener, options);
    };
    
    // Override removeEventListener to handle our intercepted events
    window.removeEventListener = function(type, listener, options) {
        // Handle deprecated events
        if (type === 'beforeunload' || type === 'unload') {
            const handlers = deprecatedEventHandlers.get(type);
            if (handlers) {
                const index = handlers.indexOf(listener);
                if (index > -1) {
                    handlers.splice(index, 1);
                }
            }
            
            // Remove from pagehide instead
            return originalRemoveEventListener.call(this, 'pagehide', listener, options);
        }
        
        // For all other events, use original behavior
        return originalRemoveEventListener.call(this, type, listener, options);
    };
})();

// Wait for Blazor to be available and ensure it starts
document.addEventListener('DOMContentLoaded', () => {
    let blazorStartAttempts = 0;
    const maxAttempts = 10;
    
    const ensureBlazorStarted = async () => {
        if (window.Blazor) {
            console.log('Blazor object detected, checking state...');
            
            // Check if Blazor has started
            const isStarted = window.Blazor._started || 
                            (window.Blazor._internal && window.Blazor._internal.navigationManager) ||
                            (window.Blazor.start && typeof window.Blazor.start === 'function');
            
            if (!isStarted && blazorStartAttempts < maxAttempts) {
                blazorStartAttempts++;
                console.log(`Attempting to start Blazor (attempt ${blazorStartAttempts}/${maxAttempts})...`);
                
                try {
                    // For Blazor Server, it should auto-start, but we can trigger reconnection
                    if (window.Blazor.reconnect) {
                        await window.Blazor.reconnect();
                    }
                    
                    // For Blazor WebAssembly, manually start if needed
                    if (window.Blazor.start && typeof window.Blazor.start === 'function') {
                        await window.Blazor.start();
                    }
                } catch (error) {
                    console.warn('Error during Blazor start attempt:', error);
                }
                
                // Check again after a delay
                setTimeout(ensureBlazorStarted, 1000);
            } else if (isStarted) {
                console.log('✅ Blazor successfully started:', {
                    loaded: true,
                    started: true,
                    hasInternal: !!window.Blazor._internal,
                    hasNavigationManager: !!(window.Blazor._internal && window.Blazor._internal.navigationManager),
                    hasCircuitManager: !!(window.Blazor._internal && window.Blazor._internal.circuitManager)
                });
                
                // Mark Blazor as started for E2E tests
                window.Blazor._started = true;
            } else {
                console.error('❌ Blazor failed to start after maximum attempts');
            }
        } else {
            console.log('Blazor not yet loaded, checking again...');
            setTimeout(ensureBlazorStarted, 500);
        }
    };
    
    // Start checking after a short delay to allow scripts to load
    setTimeout(ensureBlazorStarted, 500);
});

// Add a global function for E2E tests to check Blazor state
window.isBlazorReady = function() {
    if (!window.Blazor) return false;
    
    return window.Blazor._started || 
           (window.Blazor._internal && window.Blazor._internal.navigationManager) ||
           false;
};