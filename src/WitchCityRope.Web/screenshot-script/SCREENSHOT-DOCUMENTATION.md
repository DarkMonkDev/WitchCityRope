# WitchCityRope Screenshot Documentation

## Overview
This documentation covers the methods and syntax for taking screenshots of the WitchCityRope website, including successful approaches and available options.

## Working Methods

### 1. Microsoft Edge Headless Mode (Recommended)

**Syntax:**
```bash
"/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" --headless --disable-gpu --screenshot="C:\\path\\to\\output.png" --window-size=WIDTH,HEIGHT "URL"
```

**Example Commands:**
```bash
# Standard viewport screenshot (1920x1080)
"/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" --headless --disable-gpu --screenshot="C:\\users\\chad\\source\\repos\\WitchCityRope\\src\\WitchCityRope.Web\\screenshot-script\\screenshots\\homepage-edge.png" --window-size=1920,1080 "http://localhost:5651/"

# Full page screenshot (larger viewport)
"/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" --headless --disable-gpu --screenshot="C:\\users\\chad\\source\\repos\\WitchCityRope\\src\\WitchCityRope.Web\\screenshot-script\\screenshots\\homepage-fullpage.png" --window-size=1920,5000 "http://localhost:5651/"
```

**Available Parameters:**
- `--headless`: Run without GUI
- `--disable-gpu`: Disable GPU acceleration (required for headless mode)
- `--screenshot`: Output path (must use Windows path format C:\\ when running from WSL)
- `--window-size`: Viewport dimensions in format WIDTH,HEIGHT
- `--force-device-scale-factor`: Device scale factor (e.g., 2 for retina)
- `--hide-scrollbars`: Hide scrollbars in screenshot
- `--virtual-time-budget`: Maximum wait time in milliseconds

### 2. Puppeteer Scripts (Requires Dependencies)

**Available Scripts:**
- `homepage-screenshot.js`: Full-featured screenshot script with options
- `screenshot-simple.js`: Basic screenshot functionality
- `screenshot.js`: Login page screenshot script
- `screenshot-test.js`: Testing script for troubleshooting

**Usage:**
```bash
# Basic usage
node homepage-screenshot.js

# With options
node homepage-screenshot.js --url "http://localhost:5651/" --width 1920 --height 1080 --fullPage true --format png
```

**Command-line Options for homepage-screenshot.js:**
- `--url`: URL to screenshot
- `--width`: Viewport width
- `--height`: Viewport height
- `--fullPage`: true/false for full page capture
- `--headless`: true/false for headless mode
- `--format`: png or jpeg
- `--quality`: JPEG quality (0-100)
- `--output`: Custom output path
- `--wait`: CSS selector to wait for

### 3. Manual Browser DevTools Method

1. Open the page in Chrome/Edge
2. Press F12 to open Developer Tools
3. Press Ctrl+Shift+P
4. Type "screenshot"
5. Select one of:
   - "Capture full size screenshot" (entire page)
   - "Capture screenshot" (viewport only)
   - "Capture area screenshot" (custom selection)
   - "Capture node screenshot" (specific element)

## Screenshot Quality Analysis

### Homepage Screenshots Captured:

1. **Standard Viewport (1920x1080)**
   - File: `homepage-edge.png`
   - Size: 379KB
   - Quality: Excellent
   - Shows: Hero section with gradient background, navigation, and welcome message

2. **Full Page (1920x5000)**
   - File: `homepage-fullpage.png`
   - Size: 510KB
   - Quality: Excellent
   - Shows: Complete page including:
     - Hero section
     - "Discover the Art of Rope" section
     - "What We Offer" cards (Educational Workshops, Performances, Community Events, Safety First)
     - "Upcoming Events" section
     - "Ready to Join Us?" CTA
     - Footer with links

### Quality Observations:

- **Resolution**: Crystal clear at 1920px width
- **Color Accuracy**: Gradient backgrounds render perfectly
- **Text Rendering**: All text is sharp and readable
- **Layout**: Responsive design captured accurately
- **Performance**: Screenshots generated in ~3 seconds
- **File Size**: Reasonable for full-page captures

## Issues and Limitations

### Puppeteer Issues:
- Missing Linux dependencies for Chromium (libnss3.so, etc.)
- WSL environment limitations for launching bundled Chromium
- Path resolution issues between WSL and Windows

### Edge Headless Limitations:
- Must use Windows path format for output
- Some console errors appear but don't affect output
- Cannot capture pages requiring authentication without additional setup

## Recommendations

1. **For Automated Screenshots**: Use Edge headless mode with Windows paths
2. **For Development**: Use browser DevTools for quick captures
3. **For CI/CD**: Consider Docker with proper Chromium dependencies
4. **For Full Documentation**: Capture multiple viewport sizes:
   - Mobile: 375x667
   - Tablet: 768x1024
   - Desktop: 1920x1080
   - 4K: 3840x2160

## Example Batch Script

```bash
#!/bin/bash
# capture-all-pages.sh

EDGE="/mnt/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe"
BASE_URL="http://localhost:5651"
OUTPUT_DIR="C:\\users\\chad\\source\\repos\\WitchCityRope\\src\\WitchCityRope.Web\\screenshot-script\\screenshots"

pages=(
    "/:homepage"
    "/events:events"
    "/auth/login:login"
    "/dashboard:dashboard"
    "/profile:profile"
)

for page in "${pages[@]}"; do
    IFS=':' read -r path name <<< "$page"
    echo "Capturing $name..."
    "$EDGE" --headless --disable-gpu --screenshot="${OUTPUT_DIR}\\${name}.png" --window-size=1920,1080 "${BASE_URL}${path}"
done
```

## Conclusion

The Microsoft Edge headless mode provides the most reliable method for capturing screenshots in the WSL environment. While Puppeteer offers more features and flexibility, the dependency issues make Edge the practical choice for immediate needs.