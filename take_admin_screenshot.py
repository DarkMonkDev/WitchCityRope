#!/usr/bin/env python3
import asyncio
from playwright.async_api import async_playwright

async def main():
    async with async_playwright() as p:
        # Launch browser
        browser = await p.chromium.launch(headless=True)
        page = await browser.new_page()
        
        try:
            # Navigate to login page
            print("Navigating to login page...")
            await page.goto("http://localhost:5651/login")
            
            # Fill login form
            print("Filling login form...")
            await page.fill('input[type="email"]', 'admin@witchcityrope.com')
            await page.fill('input[type="password"]', 'Test123!')
            
            # Submit login form
            print("Submitting login...")
            await page.click('button[type="submit"]')
            
            # Wait for navigation to complete
            await page.wait_for_load_state('networkidle')
            
            # Navigate to admin dashboard
            print("Navigating to admin dashboard...")
            await page.goto("http://localhost:5651/admin/dashboard")
            
            # Wait for the page to fully load
            print("Waiting for page to load...")
            await page.wait_for_selector('text="Dashboard Overview"', state='visible', timeout=30000)
            
            # Take full page screenshot
            print("Taking screenshot...")
            await page.screenshot(path='admin-dashboard-current-state.png', full_page=True)
            
            print("Screenshot saved as admin-dashboard-current-state.png")
            
        except Exception as e:
            print(f"Error occurred: {e}")
            
        finally:
            # Close browser
            await browser.close()

if __name__ == "__main__":
    asyncio.run(main())