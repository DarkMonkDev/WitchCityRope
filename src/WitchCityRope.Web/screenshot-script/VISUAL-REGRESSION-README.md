# Visual Regression Testing Suite

## Overview

This visual regression testing suite provides automated screenshot capture and comparison capabilities for the WitchCityRope web application. It helps detect unintended visual changes across different pages, viewports, and interactive states.

## Features

- **Multi-viewport Testing**: Desktop (1920x1080), Tablet (768x1024), Mobile (375x812)
- **Interactive State Capture**: Default, hover, focus, and active states
- **Automated Comparison**: Pixel-by-pixel comparison against baseline images
- **Comprehensive Reporting**: HTML, JSON, and Markdown reports with visual diffs
- **Visual Quality Checks**: Detects broken images, layout issues, and rendering problems

## Installation

1. Navigate to the screenshot-script directory:
   ```bash
   cd screenshot-script
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

## Usage

### Running Visual Regression Tests

1. **Capture current screenshots**:
   ```bash
   npm run test:visual
   ```
   This will:
   - Visit all configured pages
   - Capture screenshots for each viewport size
   - Test interactive states (hover, focus, active)
   - Save screenshots to `screenshots/current/`

2. **Compare against baselines**:
   ```bash
   npm run visual:compare
   ```
   This will:
   - Compare current screenshots with baseline images
   - Generate diff images showing changes
   - Create comparison reports

3. **Run full test suite**:
   ```bash
   npm run visual:all
   ```
   This runs both capture and comparison in sequence.

### Managing Baselines

1. **Create initial baselines**:
   ```bash
   npm run visual:baseline
   ```
   This copies current screenshots to the baseline directory.

2. **Update baselines** (after verifying changes are intentional):
   ```bash
   npm run visual:baseline
   ```

## Directory Structure

```
screenshot-script/
├── visual-regression-test.js      # Main test script
├── visual-regression-compare.js   # Comparison tool
├── screenshots/
│   ├── baseline/                 # Baseline images for comparison
│   ├── current/                  # Latest test screenshots
│   └── diff/                     # Difference images
├── reports/
│   ├── visual-regression-results.json
│   ├── visual-regression-report.html
│   ├── visual-regression-summary.md
│   └── visual-comparison-report.html
└── VISUAL-REGRESSION-BASELINE-GUIDE.md
```

## Configuration

Edit the configuration in `visual-regression-test.js`:

```javascript
const config = {
  baseUrl: 'http://localhost:5651',
  viewports: {
    desktop: { width: 1920, height: 1080 },
    tablet: { width: 768, height: 1024 },
    mobile: { width: 375, height: 812 }
  },
  pages: [
    { name: 'homepage', path: '/', waitFor: '.hero-section' },
    // Add more pages...
  ],
  interactiveStates: ['default', 'hover', 'focus', 'active']
};
```

## Report Types

### HTML Report (`reports/visual-regression-report.html`)
- Visual grid of all screenshots
- Organized by page and viewport
- Shows errors and warnings
- Interactive image viewer

### Comparison Report (`reports/visual-comparison-report.html`)
- Side-by-side baseline vs current comparison
- Difference images with highlighted changes
- Pass/fail status for each screenshot
- Percentage difference metrics

### JSON Report (`reports/visual-regression-results.json`)
- Machine-readable test results
- Detailed error information
- Timing and performance data

### Markdown Summary (`reports/visual-regression-summary.md`)
- Quick overview of test results
- Suitable for PR comments
- Lists failures and warnings

## Best Practices

1. **Consistent Environment**: Always run tests in the same environment (OS, browser version)
2. **Review Changes**: Carefully review diff images before updating baselines
3. **Version Control**: Commit baseline images to track visual changes over time
4. **Regular Updates**: Run tests before major deployments
5. **Clean State**: Ensure the application is in a consistent state (logged out, default theme)

## Troubleshooting

### Common Issues

1. **High difference percentages on identical pages**
   - Check for animations or dynamic content
   - Ensure consistent wait times
   - Verify no timestamps or random data

2. **Missing elements in screenshots**
   - Increase wait times for slow-loading content
   - Check network conditions
   - Verify selectors are correct

3. **Canvas module installation issues**
   - On Windows: Install Windows Build Tools
   - On Linux: Install cairo development packages
   - See [node-canvas installation guide](https://github.com/Automattic/node-canvas#installation)

### Debug Mode

Add debug logging by setting environment variable:
```bash
DEBUG=visual-regression npm run test:visual
```

## CI/CD Integration

Example GitHub Actions workflow:

```yaml
name: Visual Regression Tests
on: [push, pull_request]

jobs:
  visual-regression:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Install dependencies
        run: |
          cd screenshot-script
          npm install
      
      - name: Start application
        run: |
          cd ..
          dotnet run &
          sleep 30
      
      - name: Run visual tests
        run: |
          cd screenshot-script
          npm run visual:all
      
      - name: Upload reports
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: visual-regression-reports
          path: screenshot-script/reports/
```

## Advanced Usage

### Custom Interactive States

Add custom interactive states in `visual-regression-test.js`:

```javascript
async applyInteractiveState(page, state) {
  switch(state) {
    case 'custom-state':
      await page.evaluate(() => {
        // Your custom interaction code
      });
      break;
  }
}
```

### Excluding Dynamic Content

Mask dynamic content before screenshots:

```javascript
await page.evaluate(() => {
  // Hide timestamps
  document.querySelectorAll('.timestamp').forEach(el => {
    el.textContent = 'TIMESTAMP';
  });
});
```

### Performance Optimization

Run viewports in parallel (modify test script):

```javascript
const viewportPromises = Object.entries(config.viewports).map(
  async ([name, viewport]) => {
    // Test logic here
  }
);
await Promise.all(viewportPromises);
```

## Contributing

When adding new pages or features:

1. Update the `pages` configuration
2. Add appropriate wait selectors
3. Document expected visual output in the baseline guide
4. Run tests and create new baselines
5. Submit PR with visual regression report

## License

Part of the WitchCityRope project.