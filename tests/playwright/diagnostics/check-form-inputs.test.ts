import { test, expect } from '@playwright/test';

const BASE_URL = process.env.BASE_URL || 'http://localhost:8280';

test.describe('Form Input Diagnostics', () => {
    test('analyze login form inputs and structure', async ({ page }) => {
        await page.goto(`${BASE_URL}/login`, {
            waitUntil: 'networkidle',
            timeout: 30000
        });
        
        console.log('Getting all input fields...\n');
        
        const inputs = await page.evaluate(() => {
            const inputs = Array.from(document.querySelectorAll('input'));
            return inputs.map(input => ({
                type: input.type,
                name: input.name,
                id: input.id,
                placeholder: input.placeholder,
                className: input.className,
                value: input.value,
                required: input.required,
                disabled: input.disabled,
                readonly: input.readOnly
            }));
        });
        
        console.log('Found inputs:');
        inputs.forEach((input, index) => {
            console.log(`\nInput ${index + 1}:`);
            console.log(`  Type: ${input.type}`);
            console.log(`  Name: ${input.name}`);
            console.log(`  ID: ${input.id}`);
            console.log(`  Placeholder: ${input.placeholder}`);
            console.log(`  Classes: ${input.className}`);
            console.log(`  Required: ${input.required}`);
            console.log(`  Disabled: ${input.disabled}`);
            console.log(`  Readonly: ${input.readonly}`);
        });
        
        // Also check for labels
        console.log('\n\nForm structure:');
        const formStructure = await page.evaluate(() => {
            const form = document.querySelector('form');
            if (!form) return { found: false, structure: [] };
            
            const structure: string[] = [];
            
            // Get form attributes
            structure.push(`Form found: id="${form.id}" action="${form.action}" method="${form.method}"`);
            
            // Get labels
            const labels = form.querySelectorAll('label');
            labels.forEach(label => {
                const forAttr = label.getAttribute('for');
                const text = label.textContent?.trim();
                structure.push(`Label: "${text}" for="${forAttr}"`);
            });
            
            // Get buttons
            const buttons = form.querySelectorAll('button');
            buttons.forEach(button => {
                structure.push(`Button: "${button.textContent?.trim()}" type="${button.type}"`);
            });
            
            // Check for validation elements
            const validationElements = form.querySelectorAll('[data-valmsg-for], .validation-message, .invalid-feedback');
            if (validationElements.length > 0) {
                structure.push(`Validation elements found: ${validationElements.length}`);
            }
            
            return { found: true, structure };
        });
        
        if (formStructure.found) {
            formStructure.structure.forEach(item => console.log(item));
        } else {
            console.log('No form found on the page');
        }
        
        // Check for specific form patterns
        console.log('\n\nForm Pattern Analysis:');
        const patterns = await page.evaluate(() => {
            const analysis: any = {
                hasEmailInput: false,
                hasPasswordInput: false,
                hasSubmitButton: false,
                hasRememberMe: false,
                hasCSRFToken: false,
                isAspNetForm: false,
                isBlazorForm: false
            };
            
            // Check for common patterns
            analysis.hasEmailInput = !!document.querySelector('input[type="email"], input[name*="email" i], input[id*="email" i]');
            analysis.hasPasswordInput = !!document.querySelector('input[type="password"]');
            analysis.hasSubmitButton = !!document.querySelector('button[type="submit"], input[type="submit"]');
            analysis.hasRememberMe = !!document.querySelector('input[type="checkbox"][name*="remember" i]');
            analysis.hasCSRFToken = !!document.querySelector('input[name="__RequestVerificationToken"]');
            
            // Check if it's an ASP.NET form
            analysis.isAspNetForm = !!document.querySelector('input[name*="Input." i]');
            
            // Check if it's a Blazor form
            const form = document.querySelector('form');
            if (form) {
                const attributes = Array.from(form.attributes);
                analysis.isBlazorForm = attributes.some(attr => 
                    attr.name.startsWith('_bl_') || 
                    attr.name.includes('blazor')
                );
            }
            
            return analysis;
        });
        
        console.log('Has Email Input:', patterns.hasEmailInput);
        console.log('Has Password Input:', patterns.hasPasswordInput);
        console.log('Has Submit Button:', patterns.hasSubmitButton);
        console.log('Has Remember Me:', patterns.hasRememberMe);
        console.log('Has CSRF Token:', patterns.hasCSRFToken);
        console.log('Is ASP.NET Form:', patterns.isAspNetForm);
        console.log('Is Blazor Form:', patterns.isBlazorForm);
        
        // Take a screenshot for visual reference
        await page.screenshot({ 
            path: 'screenshots/form-inputs-diagnostic.png', 
            fullPage: true 
        });
        
        // Basic assertions
        expect(inputs.length).toBeGreaterThan(0);
        expect(patterns.hasEmailInput || patterns.hasPasswordInput).toBeTruthy();
    });
});