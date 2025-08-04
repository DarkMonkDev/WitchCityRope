import { Page } from '@playwright/test';

// Test configuration
export const config = {
    baseUrl: process.env.TEST_URL || 'http://localhost:5651',
    defaultTimeout: 30000,
    adminEmail: 'admin@witchcityrope.com',
    adminPassword: 'Test123!',
    memberEmail: 'member@witchcityrope.com',
    memberPassword: 'Test123!',
    screenshotDir: 'screenshots',
};

// Color codes for console output
export const colors = {
    reset: '\x1b[0m',
    bright: '\x1b[1m',
    green: '\x1b[32m',
    red: '\x1b[31m',
    yellow: '\x1b[33m',
    blue: '\x1b[34m',
    cyan: '\x1b[36m'
};

/**
 * Log a message with color coding and timestamp
 */
export function log(message: string, type: 'info' | 'success' | 'error' | 'warning' | 'test' = 'info') {
    const timestamp = new Date().toTimeString().substr(0, 8);
    const typeColors = {
        'info': colors.blue,
        'success': colors.green,
        'error': colors.red,
        'warning': colors.yellow,
        'test': colors.cyan
    };
    console.log(`${typeColors[type]}[${timestamp}] ${message}${colors.reset}`);
}

/**
 * Helper to login
 */
export async function login(page: Page, email: string, password: string) {
    await page.goto(`${config.baseUrl}/login`);
    // Wait for Blazor to initialize
    await page.waitForTimeout(2000);
    await page.waitForSelector('#Input_Email', { state: 'visible' });
    await page.fill('#Input_Email', email);
    await page.fill('#Input_Password', password);
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

/**
 * Wait helper
 */
export async function wait(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}