// Blazor E2E Test Helper
// This file provides utilities for E2E tests to properly wait for Blazor initialization

(function() {
    'use strict';
    
    // Track Blazor initialization state
    window.__blazorE2EState = {
        initialized: false,
        startTime: Date.now(),
        initializationTime: null,
        errors: []
    };
    
    // Override console.error to capture Blazor errors
    const originalConsoleError = console.error;
    console.error = function(...args) {
        const errorMessage = args.join(' ');
        if (errorMessage.includes('blazor') || errorMessage.includes('Blazor')) {
            window.__blazorE2EState.errors.push({
                time: Date.now(),
                message: errorMessage
            });
        }
        originalConsoleError.apply(console, args);
    };
    
    // Function to check Blazor state
    window.checkBlazorState = function() {
        const blazor = window.Blazor;
        
        if (!blazor) {
            return {
                status: 'not_loaded',
                message: 'Blazor object not found'
            };
        }
        
        // Check various indicators of Blazor readiness
        const hasInternal = !!blazor._internal;
        const hasNavigationManager = hasInternal && !!blazor._internal.navigationManager;
        const hasCircuitManager = hasInternal && !!blazor._internal.circuitManager;
        const hasConnection = hasInternal && blazor._internal.connection;
        const connectionState = hasConnection ? blazor._internal.connection.state : 'unknown';
        
        // Check for circuit
        let circuitCount = 0;
        if (hasCircuitManager && blazor._internal.circuitManager.circuitsByUri) {
            circuitCount = Object.keys(blazor._internal.circuitManager.circuitsByUri).length;
        }
        
        const isReady = hasNavigationManager && (connectionState === 'Connected' || circuitCount > 0);
        
        return {
            status: isReady ? 'ready' : 'loading',
            details: {
                hasInternal,
                hasNavigationManager,
                hasCircuitManager,
                hasConnection,
                connectionState,
                circuitCount,
                errors: window.__blazorE2EState.errors
            }
        };
    };
    
    // Monitor Blazor initialization
    const monitorInterval = setInterval(function() {
        const state = window.checkBlazorState();
        
        if (state.status === 'ready' && !window.__blazorE2EState.initialized) {
            window.__blazorE2EState.initialized = true;
            window.__blazorE2EState.initializationTime = Date.now() - window.__blazorE2EState.startTime;
            
            // console.log('Blazor E2E Helper: Blazor initialized in', window.__blazorE2EState.initializationTime, 'ms');
            
            // Dispatch event for tests
            window.dispatchEvent(new CustomEvent('blazor:e2e-ready', {
                detail: {
                    initializationTime: window.__blazorE2EState.initializationTime,
                    state: state.details
                }
            }));
            
            clearInterval(monitorInterval);
        }
        
        // Timeout after 60 seconds (increased for Docker environment)
        if (Date.now() - window.__blazorE2EState.startTime > 60000) {
            console.error('Blazor E2E Helper: Initialization timeout');
            window.dispatchEvent(new CustomEvent('blazor:e2e-timeout', {
                detail: {
                    lastState: state,
                    errors: window.__blazorE2EState.errors
                }
            }));
            clearInterval(monitorInterval);
        }
    }, 100);
    
    // Helper function for E2E tests to wait for Blazor
    window.waitForBlazorE2E = function(timeout = 60000) {
        return new Promise((resolve, reject) => {
            const startTime = Date.now();
            
            // Check if already initialized
            if (window.__blazorE2EState.initialized) {
                resolve(window.checkBlazorState());
                return;
            }
            
            // Listen for ready event
            const readyHandler = (event) => {
                window.removeEventListener('blazor:e2e-ready', readyHandler);
                window.removeEventListener('blazor:e2e-timeout', timeoutHandler);
                resolve(window.checkBlazorState());
            };
            
            // Listen for timeout event
            const timeoutHandler = (event) => {
                window.removeEventListener('blazor:e2e-ready', readyHandler);
                window.removeEventListener('blazor:e2e-timeout', timeoutHandler);
                reject(new Error('Blazor initialization timeout: ' + JSON.stringify(event.detail)));
            };
            
            window.addEventListener('blazor:e2e-ready', readyHandler);
            window.addEventListener('blazor:e2e-timeout', timeoutHandler);
            
            // Also set a hard timeout
            setTimeout(() => {
                window.removeEventListener('blazor:e2e-ready', readyHandler);
                window.removeEventListener('blazor:e2e-timeout', timeoutHandler);
                reject(new Error('Blazor E2E wait timeout after ' + timeout + 'ms'));
            }, timeout);
        });
    };
})();