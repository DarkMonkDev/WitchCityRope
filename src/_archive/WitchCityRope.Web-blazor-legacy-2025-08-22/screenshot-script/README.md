# Screenshot Instructions for WitchCityRope Website

Since we encountered issues with running Puppeteer in WSL due to missing Chrome dependencies, here are alternative methods to take a screenshot:

## Method 1: PowerShell Script (Windows)

1. Open PowerShell as Administrator
2. Navigate to the screenshot-script directory:
   ```powershell
   cd C:\users\chad\source\repos\WitchCityRope\src\WitchCityRope.Web\screenshot-script
   ```
3. Run the PowerShell script:
   ```powershell
   .\take-screenshot.ps1
   ```

## Method 2: Manual Browser Screenshot

1. Open your browser (Chrome, Edge, or Firefox)
2. Navigate to http://localhost:5651/
3. Use one of these methods:
   - **Chrome/Edge**: Press `Ctrl+Shift+I` to open DevTools, then `Ctrl+Shift+P` and type "screenshot" to capture full page
   - **Firefox**: Right-click and select "Take Screenshot" 
   - **Any browser**: Press `F12`, then in DevTools console run:
     ```javascript
     document.documentElement.requestFullscreen();
     ```
     Then press `Win+PrtScn` for full screen capture

## Method 3: Browser Extensions

1. Install a screenshot extension like:
   - GoFullPage (Chrome/Edge)
   - Fireshot (Chrome/Firefox)
   - Awesome Screenshot (Chrome/Edge)
2. Navigate to http://localhost:5651/
3. Click the extension icon and select "Capture full page"

## Method 4: Windows Built-in Tools

1. Navigate to http://localhost:5651/ in your browser
2. Use one of these:
   - **Snipping Tool**: Press `Win+Shift+S` and select the area
   - **Full Screen**: Press `Win+PrtScn` (saves to Pictures/Screenshots)
   - **Game Bar**: Press `Win+G` then click the camera icon

## Troubleshooting

If http://localhost:5651/ is not loading:
1. Check if the application is running
2. Look for any error messages in the browser console (F12)
3. Try accessing http://127.0.0.1:5651/ instead
4. Check if the port is correct in launchSettings.json

## Expected Page Elements

When taking the screenshot, check for:
- Navigation bar/menu
- Main content area
- Any hero sections or banners
- Footer (if visible)
- Responsive design elements
- Any error messages or loading indicators

Save your screenshots in the `screenshots` folder with descriptive names like:
- `wcr-homepage-full-YYYY-MM-DD.png`
- `wcr-homepage-viewport-YYYY-MM-DD.png`
- `wcr-homepage-mobile-YYYY-MM-DD.png` (if testing responsive)