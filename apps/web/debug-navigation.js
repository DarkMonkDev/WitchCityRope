/**
 * Navigation Debug Script
 *
 * Run this script in the browser console on http://localhost:5173/admin/vetting
 * to diagnose navigation issues.
 *
 * Usage:
 * 1. Open http://localhost:5173/admin/vetting
 * 2. Open Chrome DevTools (F12)
 * 3. Go to Console tab
 * 4. Paste this entire script and press Enter
 * 5. Click on a table row
 * 6. Review the debug output
 */

(function() {
    console.log('=== Navigation Debug Script Started ===');

    // Check 1: Look for overlays or elements with high z-index
    function checkForOverlays() {
        console.log('\n--- Check 1: Looking for overlays ---');
        const allElements = document.querySelectorAll('*');
        const overlays = [];

        allElements.forEach(el => {
            const style = window.getComputedStyle(el);
            const zIndex = parseInt(style.zIndex);

            if (zIndex > 1000) {
                overlays.push({
                    element: el,
                    zIndex: zIndex,
                    position: style.position,
                    display: style.display,
                    visibility: style.visibility,
                    pointerEvents: style.pointerEvents
                });
            }
        });

        if (overlays.length > 0) {
            console.warn('⚠️ Found elements with high z-index:', overlays);
        } else {
            console.log('✓ No overlays with high z-index found');
        }

        return overlays;
    }

    // Check 2: Look for modals
    function checkForModals() {
        console.log('\n--- Check 2: Looking for modals ---');
        const modals = document.querySelectorAll('[class*="modal"], [class*="Modal"], [role="dialog"]');

        if (modals.length > 0) {
            console.warn('⚠️ Found modal elements:', modals);
            modals.forEach(modal => {
                const style = window.getComputedStyle(modal);
                console.log('Modal:', {
                    element: modal,
                    display: style.display,
                    visibility: style.visibility,
                    pointerEvents: style.pointerEvents,
                    className: modal.className
                });
            });
        } else {
            console.log('✓ No modal elements found');
        }

        return modals;
    }

    // Check 3: Monitor click events on table rows
    function monitorTableClicks() {
        console.log('\n--- Check 3: Monitoring table clicks ---');
        const table = document.querySelector('[data-testid="applications-table"]');

        if (!table) {
            console.error('✗ Table not found!');
            return;
        }

        console.log('✓ Table found, adding click listener...');

        table.addEventListener('click', (event) => {
            console.log('\n🔍 Table click detected:');
            console.log('  Target:', event.target);
            console.log('  Current target:', event.currentTarget);
            console.log('  Event phase:', event.eventPhase);
            console.log('  Default prevented:', event.defaultPrevented);
            console.log('  Propagation stopped:', event.cancelBubble);

            // Check if the click was on a checkbox
            const isCheckbox = event.target.closest('input[type="checkbox"]');
            console.log('  Is checkbox click:', !!isCheckbox);

            // Find the row
            const row = event.target.closest('tr');
            if (row) {
                console.log('  Row found:', row);
                console.log('  Row data-key:', row.getAttribute('data-key'));
            }
        }, true); // Use capture phase

        console.log('✓ Click listener added');
    }

    // Check 4: Monitor React Router navigation
    function monitorReactRouter() {
        console.log('\n--- Check 4: Monitoring React Router ---');

        // Store original pushState and replaceState
        const originalPushState = history.pushState;
        const originalReplaceState = history.replaceState;

        history.pushState = function(...args) {
            console.log('🔍 history.pushState called:', args);
            return originalPushState.apply(this, args);
        };

        history.replaceState = function(...args) {
            console.log('🔍 history.replaceState called:', args);
            return originalReplaceState.apply(this, args);
        };

        window.addEventListener('popstate', (event) => {
            console.log('🔍 popstate event:', event);
        });

        console.log('✓ React Router monitoring enabled');
    }

    // Check 5: Look for navigation blockers
    function checkForNavigationBlockers() {
        console.log('\n--- Check 5: Checking for navigation blockers ---');

        // Check if there are any beforeunload handlers
        const beforeUnloadHandlers = window.onbeforeunload;
        if (beforeUnloadHandlers) {
            console.warn('⚠️ beforeunload handler found:', beforeUnloadHandlers);
        } else {
            console.log('✓ No beforeunload handlers');
        }

        // Check for event listeners that might block navigation
        const listeners = window.getEventListeners?.(window);
        if (listeners) {
            console.log('Window event listeners:', listeners);
        }
    }

    // Check 6: Check for pointer-events issues
    function checkPointerEvents() {
        console.log('\n--- Check 6: Checking pointer-events ---');
        const table = document.querySelector('[data-testid="applications-table"]');

        if (table) {
            const rows = table.querySelectorAll('tbody tr');
            rows.forEach((row, index) => {
                const style = window.getComputedStyle(row);
                if (style.pointerEvents === 'none') {
                    console.warn(`⚠️ Row ${index} has pointer-events: none`);
                }
            });
            console.log('✓ Pointer events check complete');
        }
    }

    // Run all checks
    console.log('\n=== Running Diagnostics ===\n');

    const overlays = checkForOverlays();
    const modals = checkForModals();
    monitorTableClicks();
    monitorReactRouter();
    checkForNavigationBlockers();
    checkPointerEvents();

    console.log('\n=== Diagnostics Complete ===');
    console.log('Now click on a table row and observe the console output.');

    // Return results for inspection
    return {
        overlays,
        modals,
        message: 'Debug script loaded. Click a table row to see navigation diagnostics.'
    };
})();
