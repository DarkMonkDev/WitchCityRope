import { test, expect, Page } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:5651';
const ADMIN_EMAIL = 'admin@witchcityrope.com';
const ADMIN_PASSWORD = 'Test123!';

// Helper to login
async function login(page: Page) {
    await page.goto(`${BASE_URL}/login`);
    // Wait for Blazor to initialize
    await page.waitForTimeout(2000);
    await page.waitForSelector('#Input_Email', { state: 'visible' });
    await page.fill('#Input_Email', ADMIN_EMAIL);
    await page.fill('#Input_Password', ADMIN_PASSWORD);
    await page.click('.sign-in-btn');
    
    try {
        await page.waitForNavigation({ waitUntil: 'networkidle', timeout: 5000 });
    } catch (error) {
        const url = page.url();
        if (!url.includes('/admin') && !url.includes('/')) {
            throw new Error('Login failed - check credentials');
        }
    }
}

test.describe('Event Form Diagnostics', () => {
    test.setTimeout(60000);
    
    test('diagnose event form structure and submission', async ({ page }) => {
        console.log('🔍 Starting Event Form Diagnostics\n');
        
        // Enable detailed console logging
        page.on('console', msg => {
            console.log(`Browser ${msg.type()}: ${msg.text()}`);
        });
        
        page.on('pageerror', error => {
            console.log('🔴 Page error:', error.message);
        });
        
        // Login
        await login(page);
        
        // Navigate to create event page
        await page.goto(`${BASE_URL}/admin/events/new`);
        await page.waitForSelector('.event-editor-container', { state: 'visible', timeout: 10000 });
        
        // Analyze the form structure
        console.log('\n📋 FORM STRUCTURE ANALYSIS:\n');
        
        const formAnalysis = await page.evaluate(() => {
            const analysis: any = {
                formElement: null,
                inputs: [],
                requiredFields: [],
                buttons: [],
                validationSetup: false
            };
            
            // Find the form
            const form = document.querySelector('form');
            if (form) {
                analysis.formElement = {
                    id: form.id,
                    action: form.action,
                    method: form.method,
                    hasSubmitHandler: form.onsubmit !== null
                };
            }
            
            // Find all inputs
            const inputs = document.querySelectorAll('input, textarea, select');
            inputs.forEach(input => {
                const info: any = {
                    type: input.getAttribute('type') || input.tagName.toLowerCase(),
                    name: input.getAttribute('name'),
                    id: input.id,
                    placeholder: input.getAttribute('placeholder'),
                    required: input.hasAttribute('required'),
                    value: (input as HTMLInputElement).value,
                    className: input.className,
                    bindingAttribute: input.getAttribute('bind') || input.getAttribute('bind-value')
                };
                
                // Check for Blazor binding
                const attributes = Array.from(input.attributes);
                const blazorBindings = attributes.filter(attr => 
                    attr.name.startsWith('_bl_') || 
                    attr.name.includes('bind') ||
                    attr.name.includes('blazor')
                );
                if (blazorBindings.length > 0) {
                    info.blazorBindings = blazorBindings.map(attr => `${attr.name}=${attr.value}`);
                }
                
                analysis.inputs.push(info);
                
                if (input.hasAttribute('required')) {
                    analysis.requiredFields.push({
                        type: info.type,
                        name: info.name || info.id || info.placeholder,
                        hasValue: !!(input as HTMLInputElement).value
                    });
                }
            });
            
            // Find buttons
            const buttons = document.querySelectorAll('button');
            buttons.forEach(btn => {
                analysis.buttons.push({
                    text: btn.textContent?.trim(),
                    type: btn.type,
                    className: btn.className,
                    disabled: btn.disabled,
                    onclick: btn.onclick !== null
                });
            });
            
            // Check for validation
            analysis.validationSetup = document.querySelector('[data-valmsg-for]') !== null ||
                                      document.querySelector('.validation-message') !== null;
            
            return analysis;
        });
        
        console.log('Form Element:', formAnalysis.formElement);
        console.log('\nRequired Fields:');
        formAnalysis.requiredFields.forEach((field: any) => {
            console.log(`  - ${field.name}: ${field.hasValue ? '✓ Has value' : '✗ Empty'}`);
        });
        
        console.log('\nButtons Found:');
        formAnalysis.buttons.forEach((btn: any) => {
            console.log(`  - "${btn.text}" (type: ${btn.type}, disabled: ${btn.disabled})`);
        });
        
        console.log('\nValidation Setup:', formAnalysis.validationSetup ? 'Yes' : 'No');
        
        // Fill minimal required fields
        console.log('\n📝 FILLING REQUIRED FIELDS:\n');
        
        // Event Type
        await page.click('.type-option-compact:first-child');
        console.log('✓ Selected event type');
        
        // Title
        await page.fill('input.form-input[placeholder*="Rope Basics Workshop"]', 'Diagnostic Test Event');
        console.log('✓ Entered title');
        
        // Description - Check what type of editor we have
        const hasRichTextEditor = await page.$('.e-rte-content');
        if (hasRichTextEditor) {
            console.log('✓ Found Syncfusion RichTextEditor');
            await page.evaluate(() => {
                const editor = document.querySelector('.e-rte-content .e-content') as HTMLElement;
                if (editor) {
                    editor.innerHTML = '<p>Test description for diagnostic purposes.</p>';
                    editor.dispatchEvent(new Event('input', { bubbles: true }));
                    editor.dispatchEvent(new Event('change', { bubbles: true }));
                }
            });
        } else {
            // Fallback to textarea
            const textarea = await page.$('textarea');
            if (textarea) {
                await textarea.fill('Test description for diagnostic purposes.');
            }
        }
        console.log('✓ Entered description');
        
        // Dates
        const dateInputs = await page.$$('input[type="datetime-local"]');
        if (dateInputs.length >= 2) {
            const startDate = new Date();
            startDate.setDate(startDate.getDate() + 7);
            const startStr = startDate.toISOString().slice(0, 16);
            
            await dateInputs[0].click();
            await page.keyboard.press('Control+a');
            await page.keyboard.type(startStr);
            console.log('✓ Set start date');
            
            const endDate = new Date(startDate);
            endDate.setHours(endDate.getHours() + 2);
            const endStr = endDate.toISOString().slice(0, 16);
            
            await dateInputs[1].click();
            await page.keyboard.press('Control+a');
            await page.keyboard.type(endStr);
            console.log('✓ Set end date');
        }
        
        // Location
        await page.fill('input.form-input[placeholder="Enter venue name"]', 'Test Venue');
        console.log('✓ Entered location');
        
        // Capacity
        const capacityInput = await page.$('input.form-input[type="number"]');
        if (capacityInput) {
            await capacityInput.click({ clickCount: 3 });
            await page.keyboard.type('20');
            console.log('✓ Set capacity');
        }
        
        // Check field values after filling
        console.log('\n📊 FIELD VALUES AFTER FILLING:\n');
        
        const filledValues = await page.evaluate(() => {
            const values: Record<string, string> = {};
            const inputs = document.querySelectorAll('input, textarea, select');
            inputs.forEach(input => {
                const key = input.getAttribute('placeholder') || 
                          input.getAttribute('name') || 
                          input.id || 
                          input.getAttribute('type') || '';
                if (key && (input as HTMLInputElement).value) {
                    values[key] = (input as HTMLInputElement).value;
                }
            });
            
            // Check RichTextEditor content
            const rteContent = document.querySelector('.e-rte-content .e-content') as HTMLElement;
            if (rteContent) {
                values['RichTextEditor'] = rteContent.innerHTML;
            }
            
            return values;
        });
        
        Object.entries(filledValues).forEach(([key, value]) => {
            console.log(`  ${key}: ${value}`);
        });
        
        // Try to submit
        console.log('\n🚀 ATTEMPTING FORM SUBMISSION:\n');
        
        // Find and click the submit button
        const submitResult = await page.evaluate(() => {
            const buttons = Array.from(document.querySelectorAll('button'));
            const submitBtn = buttons.find(btn => 
                btn.textContent?.includes('Create Event') || 
                btn.textContent?.includes('Save')
            );
            
            if (submitBtn) {
                // Add event listener to track form submission
                const form = document.querySelector('form');
                if (form) {
                    form.addEventListener('submit', (e) => {
                        console.log('Form submit event triggered');
                        (window as any).__formSubmitted = true;
                    });
                }
                
                console.log(`Clicking button: "${submitBtn.textContent?.trim()}"`);
                submitBtn.click();
                return { clicked: true, buttonText: submitBtn.textContent?.trim() };
            }
            
            return { clicked: false, buttonText: null };
        });
        
        console.log(`Submit button clicked: ${submitResult.clicked} (${submitResult.buttonText})`);
        
        // Wait and check results
        await page.waitForTimeout(3000);
        
        const afterSubmit = await page.evaluate(() => {
            return {
                url: window.location.pathname,
                formSubmitted: (window as any).__formSubmitted || false,
                validationErrors: Array.from(document.querySelectorAll('.validation-message, .field-validation-error, .invalid-feedback')).map(el => ({
                    text: el.textContent?.trim(),
                    forField: el.getAttribute('data-valmsg-for') || 'unknown'
                })),
                hasSuccessMessage: document.body.textContent?.includes('successfully') || false,
                blazorErrors: Array.from(document.querySelectorAll('.validation-errors li')).map(el => el.textContent)
            };
        });
        
        console.log('\n📈 SUBMISSION RESULTS:\n');
        console.log(`Current URL: ${afterSubmit.url}`);
        console.log(`Form submitted: ${afterSubmit.formSubmitted}`);
        console.log(`Has success message: ${afterSubmit.hasSuccessMessage}`);
        
        if (afterSubmit.validationErrors.length > 0) {
            console.log('\nValidation Errors:');
            afterSubmit.validationErrors.forEach((err: any) => {
                console.log(`  - ${err.forField}: ${err.text}`);
            });
        }
        
        if (afterSubmit.blazorErrors.length > 0) {
            console.log('\nBlazor Validation Errors:');
            afterSubmit.blazorErrors.forEach((err: any) => {
                console.log(`  - ${err}`);
            });
        }
        
        // Take final screenshot
        await page.screenshot({ path: 'screenshots/diagnose-final-state.png', fullPage: true });
    });
});